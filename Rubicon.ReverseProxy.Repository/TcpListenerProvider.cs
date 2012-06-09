using System;
using System.Net;
using Rubicon.ReverseProxy.Repository.Interface;
using System.Net.Sockets;

namespace Rubicon.ReverseProxy.Repository
{
    public class TcpListenerProvider : ITcpListenerProvider
    {
        private TcpListener _listener;

        protected TcpListenerProvider()
        {
            
        }

        ~TcpListenerProvider()
        {
            Stop();
        }

        public IPEndPoint LocalEndpoint
        {
            get
            {
                if (_listener == null) throw new InvalidOperationException("The listener is not running.");
                return (IPEndPoint)_listener.LocalEndpoint;
            }
        }

        public static TcpListenerProvider Create()
        {
            return new TcpListenerProvider();
        }

        public ITcpClientActor WaitForTcpClient()
        {
            try
            {
                if (_listener == null)
                    throw new InvalidOperationException("The listener is not running.");
                var client = _listener.AcceptTcpClient(); //The listener will wait on this line until a connection is recieved.
                return new TcpClientActor(client);
            }
            catch (SocketException)
            {
                //A blocking operation was interrupted by a call to WSACancelBlockingCall
                return null;
            }
        }

        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }

        public void Stop()
        {
            if (_listener != null) 
                _listener.Stop();
        }
    }
}