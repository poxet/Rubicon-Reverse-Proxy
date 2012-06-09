using Rubicon.ReverseProxy.Repository.Interface;

namespace Rubicon.ReverseProxy.Repository
{
    public sealed class TelnetListenerProvider : TcpListenerProvider
    {
        private static ITcpListenerProvider _instance;

        private TelnetListenerProvider()
        {

        }

        public static ITcpListenerProvider Instance
        {
            get { return _instance ?? (_instance = new TelnetListenerProvider()); }
            set { _instance = value; }
        }
    }
}