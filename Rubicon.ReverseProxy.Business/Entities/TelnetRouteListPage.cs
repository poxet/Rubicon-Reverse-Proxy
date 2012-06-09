using System.Linq;
using System.Text;
using Rubicon.ReverseProxy.Repository.Interface;

namespace Rubicon.ReverseProxy.Business.Entities
{
    sealed class TelnetRouteListPage : TelnetPage
    {
        public TelnetRouteListPage(char key, ITcpClientActor tcpClientActor)
            : base(key, "Reverse Proxy Routing Rules")
        {
            Commands.Add(new AddRoutingRuleCommand(tcpClientActor));
            Commands.Add(new DeleteRoutingRuleCommand(tcpClientActor));
        }

        public override string GetPageContent()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Empty);
            var rules = RedirectRuleManager.Instance.Items.ToArray();
            for (var i = 0; i < rules.Count(); i++)
            {
                var item = rules[i];
                sb.AppendLine(string.Format("{4}. {0}:{1} --> {2}:{3}", string.IsNullOrEmpty(item.PublicRequestHost) ? "'Any'" : item.PublicRequestHost, item.PublicRequestPort, item.InternalTargetAddress, item.InternalTargetPort, i));
            }

            return sb.ToString();
        }
    }
}