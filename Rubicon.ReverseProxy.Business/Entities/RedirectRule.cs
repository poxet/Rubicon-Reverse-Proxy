using System.Net;

namespace Rubicon.ReverseProxy.Business.Entities
{
    class RedirectRule
    {
        public RedirectRule(IPAddress callingClient, 
            string publicHostRequested, int publicPortRequested,
            IPAddress internalTargetAddress, int internalTargetPort)
        {
            //From where was the call sent
            CallingClient = callingClient;                              //If there is a special rule depending on the client's IP

            //What was actually requested
            PublicRequestHost = publicHostRequested ?? string.Empty;    //The host that was requested, only possible for HTTP requests. Set to null for any other protocol.
            PublicRequestPort = publicPortRequested;                    //Port used for the external public incoming request

            //Target to redirect traffic to
            InternalTargetAddress = internalTargetAddress;
            InternalTargetPort = internalTargetPort;
        }

        public IPAddress CallingClient { get; private set; }
        public string PublicRequestHost { get; private set; }
        public int PublicRequestPort { get; private set; }
        public IPAddress InternalTargetAddress { get; private set; }
        public int InternalTargetPort { get; private set; }
    }
}