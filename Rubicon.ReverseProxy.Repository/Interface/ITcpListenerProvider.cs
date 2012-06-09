using System.Net;

namespace Rubicon.ReverseProxy.Repository.Interface
{
    public interface ITcpListenerProvider
    {
        IPEndPoint LocalEndpoint { get; }
        void Start(int port);
        void Stop();
        ITcpClientActor WaitForTcpClient();
    }
}