using Rubicon.ReverseProxy.Business.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using Tharga.Support.Client.Base;

namespace Rubicon.ReverseProxy.Business
{
    public sealed class ReversedProxyServer
    {
        private static readonly object _syncRoot = new object();
        private readonly Mutex _serviceLock;
        private readonly List<PortListener> Listeners = new List<PortListener>();
        private bool _stopped;

        public ReversedProxyServer()
        {
            try
            {
                _serviceLock = new Mutex(false, "Rubicon.ReverseProxy.Instance");
            }
            catch (Exception exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.FatalException);
                throw;
            }
        }

        ~ReversedProxyServer()
        {
            Stop();
        }

        public void Start()
        {
            try
            {
                LogHelper.LogMessage("Starting", Issue.IssueLevel.Information);

                if (!_serviceLock.WaitOne(0, false))
                    throw new InvalidOperationException("Rubicon Reverse Proxy is already running and cannot be started multiple times on the same machine.");

                RedirectRuleManager.RedirectRuleAddedEvent += this.RedirectRuleManager_RedirectRuleAddedEvent;
                RedirectRuleManager.RedirectRuleRemovedEvent += this.RedirectRuleManager_RedirectRuleRemovedEvent;

                LogHelper.StartSessionAsync();
                PerformaceCounters.Instance.Initiate();
                RedirectRuleManager.Instance.Load();

                TelnetServer.Instance.Start();

                LogHelper.LogMessage("Started", Issue.IssueLevel.Information);
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
                throw;
            }
        }

        private void RedirectRuleManager_RedirectRuleRemovedEvent(object sender, RedirectRuleManager.RedirectRuleRemovedEventArgs e)
        {
            try
            {
                var listener = Listeners.FirstOrDefault(x => x.PublicPortRequested == e.RedirectRule.PublicRequestPort);
                if (listener == null) throw new InvalidOperationException(string.Format("Trying to remove a rule but cannot find a listener for the port {0}.", e.RedirectRule.PublicRequestPort));

                //Remove the rule from the listener, and remove the entire listener if there are no rules left
                listener.Remove(e.RedirectRule);
                if (listener.RuleCount == 0)
                {
                    Listeners.Remove(listener);
                    listener.Stop();
                }
            }
            catch (Exception exception)
            {
                exception.Data.Add("PublicRequestHost", e.RedirectRule.PublicRequestHost);
                exception.Data.Add("PublicRequestPort", e.RedirectRule.PublicRequestPort);
                exception.Data.Add("InternalTargetAddress", e.RedirectRule.InternalTargetAddress);
                exception.Data.Add("InternalTargetPort", e.RedirectRule.InternalTargetPort);

                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
            }
        }

        private void RedirectRuleManager_RedirectRuleAddedEvent(object sender, RedirectRuleManager.RedirectRuleAddedEventArgs e)
        {
            try
            {
                var listener = Listeners.FirstOrDefault(x => x.PublicPortRequested == e.RedirectRule.PublicRequestPort);
                if (listener == null)
                {
                    listener = PortListener.Create(e.RedirectRule);
                    listener.Start();
                    Listeners.Add(listener);
                }
                else
                {
                    listener.Add(e.RedirectRule);
                }
            }
            catch (Exception exception)
            {
                exception.Data.Add("PublicRequestHost", e.RedirectRule.PublicRequestHost);
                exception.Data.Add("PublicRequestPort", e.RedirectRule.PublicRequestPort);
                exception.Data.Add("InternalTargetAddress", e.RedirectRule.InternalTargetAddress);
                exception.Data.Add("InternalTargetPort", e.RedirectRule.InternalTargetPort);

                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
            }
        }

        public void Stop()
        {
            try
            {
                if (_stopped) return;

                lock (_syncRoot)
                {
                    if (_stopped) return;

                    LogHelper.LogMessage("Ending", Issue.IssueLevel.Information);

                    LogHelper.EndSession();

                    TelnetServer.Instance.Stop();

                    foreach (var listener in Listeners) listener.Stop();

                    PerformaceCounters.Instance.Reset();

                    _serviceLock.Dispose();
                    LogHelper.LogMessage("Ended", Issue.IssueLevel.Information);

                    _stopped = true;
                }
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
                throw;
            }
        }
    }
}
