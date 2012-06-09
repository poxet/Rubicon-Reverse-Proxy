using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Globalization;

namespace Rubicon.ReverseProxy.Business.Entities
{
    sealed class RedirectRuleManager
    {
        public static RedirectRuleManager _instance;
        private readonly List<RedirectRule> _redirectRules = new List<RedirectRule>();

        public IEnumerable<RedirectRule> Items { get { return this._redirectRules; } }

        public static RedirectRuleManager Instance { get { return _instance ?? (_instance = new RedirectRuleManager()); } }

        #region Events


        public class RedirectRuleAddedEventArgs : EventArgs
        {
            public RedirectRule RedirectRule { get; private set; }

            public RedirectRuleAddedEventArgs(RedirectRule redirectRule)
            {
                this.RedirectRule = redirectRule;
            }
        }

        public class RedirectRuleRemovedEventArgs : EventArgs
        {
            public RedirectRule RedirectRule { get; private set; }

            public RedirectRuleRemovedEventArgs(RedirectRule redirectRule)
            {
                this.RedirectRule = redirectRule;
            }
        }

        public static event EventHandler<RedirectRuleAddedEventArgs> RedirectRuleAddedEvent;
        public static event EventHandler<RedirectRuleRemovedEventArgs> RedirectRuleRemovedEvent;

        private void OnRedirectRuleAddedEvent(RedirectRuleAddedEventArgs e)
        {
            var handler = RedirectRuleAddedEvent;
            if (handler != null) handler(this, e);
        }

        private void OnRedirectRuleRemovedEvent(RedirectRuleRemovedEventArgs e)
        {
            var handler = RedirectRuleRemovedEvent;
            if (handler != null) handler(this, e);
        }


        #endregion

        private RedirectRuleManager()
        {
            
        }

        public void Add(RedirectRule redirectRule, bool save)
        {
            //Check so that incoming rule is not duplicated in the redirect rule
            if ( this._redirectRules.Any(x => x.PublicRequestHost == redirectRule.PublicRequestHost && x.PublicRequestPort == redirectRule.PublicRequestPort) )
                throw new InvalidOperationException(string.Format("There is already a redirect rule for host {0} and port {1}.", string.IsNullOrEmpty(redirectRule.PublicRequestHost) ? "'Any'" : redirectRule.PublicRequestHost, redirectRule.PublicRequestPort));

            _redirectRules.Add(redirectRule);
            PerformaceCounters.Instance.RedirectRuleCounterIncrement();
            OnRedirectRuleAddedEvent(new RedirectRuleAddedEventArgs(redirectRule));

            if (save)
                Save();
        }

        public bool Remove(RedirectRule redirectRule)
        {
            if (!_redirectRules.Remove(redirectRule)) 
                return false;

            PerformaceCounters.Instance.RedirectRuleCounterDecrement();
            OnRedirectRuleRemovedEvent(new RedirectRuleRemovedEventArgs(redirectRule));

            Save();

            return true;
        }

        public void Load()
        {
            var redirectRuleConfigFile = System.Configuration.ConfigurationManager.AppSettings["RedirectRuleConfigFile"];
            if (System.IO.File.Exists(redirectRuleConfigFile))
            {
                var xmd = new System.Xml.XmlDocument();
                xmd.Load(redirectRuleConfigFile);

                var nodes = xmd.GetElementsByTagName("RedirectRule");
                foreach (System.Xml.XmlElement xme in nodes)
                {
                    var callingClient = IPAddress.Any; //Not yet supported "CallingClient"
                    var requestHost = xme.Attributes["RequestHost"].Value;
                    var requestPort = int.Parse(xme.Attributes["RequestPort"].Value);
                    var targetAddress = xme.Attributes["TargetAddress"].Value;
                    var targetPort = int.Parse(xme.Attributes["TargetPort"].Value);

                    Instance.Add(new RedirectRule(callingClient, requestHost, requestPort, IPAddress.Parse(targetAddress), targetPort), false);                    
                }
            }

            //Append sample redirect rules
            //Instance.Add(new RedirectRule(IPAddress.Any, null, 3001, IPAddress.Parse("127.0.0.1"), 3002), false);
            //Instance.Add(new RedirectRule(IPAddress.Any, "ws1.test.com", 3003, IPAddress.Parse("127.0.0.1"), 3002), false);
            //Save();
        }

        public void Save()
        {
            var xmd = new System.Xml.XmlDocument();
            var xmeRoot = xmd.CreateElement("RedirectRules");
            xmd.AppendChild(xmeRoot);

            foreach (var redirectRule in _redirectRules)
            {
                var xme = xmd.CreateElement("RedirectRule");
                xmeRoot.AppendChild(xme);

                //xme.SetAttribute("CallingClient", redirectRule.CallingClient.Address.ToString(CultureInfo.InvariantCulture));
                xme.SetAttribute("RequestHost", redirectRule.PublicRequestHost);
                xme.SetAttribute("RequestPort", redirectRule.PublicRequestPort.ToString(CultureInfo.InvariantCulture));
                xme.SetAttribute("TargetAddress", redirectRule.InternalTargetAddress.ToString());
                xme.SetAttribute("TargetPort", redirectRule.InternalTargetPort.ToString(CultureInfo.InvariantCulture));
            }

            var redirectRuleConfigFile = System.Configuration.ConfigurationManager.AppSettings["RedirectRuleConfigFile"];
            xmd.Save(redirectRuleConfigFile);
        }
    }
}