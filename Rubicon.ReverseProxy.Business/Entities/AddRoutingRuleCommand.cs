using Rubicon.ReverseProxy.Repository.Interface;
using System.Net;

namespace Rubicon.ReverseProxy.Business.Entities
{
    class AddRoutingRuleCommand : TelnetCommand
    {
        public AddRoutingRuleCommand(ITcpClientActor tcpClientActor)
            : base(tcpClientActor, 'A', "Add")
        {
        }

        public override string Invoke()
        {
            _tcpClientActor.Action(TelnetAction.MoveLeft);

            _tcpClientActor.WriteLine("Enter data for the new rule.");
            var publicHostRequested = RequestEntry("Public Host URL (empty for 'Any'): ");

            var publicPortRequestedEntry = RequestEntry("Public Port: ");
            int publicPortRequested;
            if (!int.TryParse(publicPortRequestedEntry, out publicPortRequested))
                return string.Format("Command aborted. Cannot parse {0} as an integer.", publicPortRequestedEntry);

            var internalTargetAddressEntry = RequestEntry("Internal Target IP: ");
            IPAddress internalTargetAddress;
            if (!IPAddress.TryParse(internalTargetAddressEntry, out internalTargetAddress))
                return string.Format("Command aborted. Cannot parse {0} as a IP Address", internalTargetAddressEntry);

            var internalTargetPortEntry = RequestEntry("Internal Target Port: ");
            int internalTargetPort;
            if (!int.TryParse(internalTargetPortEntry, out internalTargetPort))
                return string.Format("Command aborted. Cannot parse {0} as an integer.", internalTargetPort);

            var redirectRule = new RedirectRule(IPAddress.Any, publicHostRequested, publicPortRequested, internalTargetAddress, internalTargetPort);

            _tcpClientActor.WriteLine(string.Format("The new rule will be {0}:{1} sent to {2}:{3}.", redirectRule.PublicRequestHost, redirectRule.PublicRequestPort, redirectRule.InternalTargetAddress, redirectRule.InternalTargetPort));

            string responseMessage;
            if (ConfirmAction(string.Format("The change will take effect immediately! Are you sure that the rule is to be added?"), out responseMessage))
            {
                RedirectRuleManager.Instance.Add(redirectRule, true);

                responseMessage = string.Format("New rule was added.");
            }
            return responseMessage;

        }
    }
}