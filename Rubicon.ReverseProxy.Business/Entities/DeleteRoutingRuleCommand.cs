using System.Linq;
using Rubicon.ReverseProxy.Repository.Interface;

namespace Rubicon.ReverseProxy.Business.Entities
{
    class DeleteRoutingRuleCommand : TelnetCommand
    {
        public DeleteRoutingRuleCommand(ITcpClientActor tcpClientActor)
            : base(tcpClientActor, 'D', "Delete")
        {
        }

        public override string Invoke()
        {
            _tcpClientActor.Action(TelnetAction.MoveLeft);

            var responseEntry = RequestEntry("Enter the number of the rule to delete: ");

            int index;
            if (!int.TryParse(responseEntry, out index))
                return "Invalid entry. You have to enter a numerical value.";

            var rules = RedirectRuleManager.Instance.Items.ToArray();
            if (index < 0 || index >= rules.Count())
                return string.Format("Rule {0} does not exist.", index);

            string responseMessage;
            if (ConfirmAction(string.Format("The change will take effect immediately! Are you sure that rule {0} is to be deleted?", index), out responseMessage))
            {
                RedirectRuleManager.Instance.Remove(rules[index]);

                responseMessage = string.Format("Rule {0} was deleted.", index);
            }
            return responseMessage;
        }
    }
}