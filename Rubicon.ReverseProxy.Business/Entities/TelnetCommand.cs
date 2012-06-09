using System;
using System.Text;
using Rubicon.ReverseProxy.Repository.Interface;

namespace Rubicon.ReverseProxy.Business.Entities
{
    abstract class TelnetCommand
    {
        protected ITcpClientActor _tcpClientActor;
        public char Key { get; private set; }
        public string Name { get; private set; }

        protected TelnetCommand(ITcpClientActor tcpClientActor, char key, string name)
        {
            _tcpClientActor = tcpClientActor;
            Key = key;
            Name = name;
        }

        public abstract string Invoke();

        virtual protected bool ConfirmAction(string message, out string response)
        {
            response = null;

            _tcpClientActor.WriteLine(string.Format("{0} (y/n)", message));
            var confirm = _tcpClientActor.ReadByte();
            var encoder = new ASCIIEncoding();
            if (string.Compare(encoder.GetString(new[] { confirm }), "y", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                response = "Command aborted.";
                return false;
            }
            return true;
        }

        virtual protected string RequestEntry(string message)
        {
            _tcpClientActor.Write(message);
            var entry = _tcpClientActor.ReadEntry();
            //_tcpClientActor.WriteLine(string.Empty);
            return entry;
        }
    }
}