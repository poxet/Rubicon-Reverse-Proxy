using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Rubicon.ReverseProxy.TcpTestServer
{
    class Program
    {
        private static bool _running = true;
        private const int _defaultPort = 3000;
        private static TcpListener _tcpListener;
        private static readonly List<Task> _tcpHandlers = new List<Task>();

        static void Main(string[] args)
        {
            //Read input parameters
            var port = _defaultPort;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out port)) 
                    port = _defaultPort;
            }

            Console.Title = string.Format("Rubicon.ReverseProxy.TcpTestServer port={0}", port);

            //Start a listener
            try
            {
                var listenerTask = new Task(() => Listen(port));
                listenerTask.Start();

                Console.WriteLine("Press any key to stop server...");
                Console.ReadKey();

                _tcpListener.Stop();
                _running = false;

                listenerTask.Wait();
                listenerTask.Dispose();
            }
            catch (Exception exp)
            {
                Console.WriteLine("Main. [Swallow] {0}", exp.Message);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void Listen(int port)
        {
            try
            {
                Console.WriteLine("Listening for traffic on port {0}.", port);
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _tcpListener.Start();

                while (_running)
                {
                    var client = _tcpListener.AcceptTcpClient();
                    var cmn = new Communicator(client);                    
                    var tcpHandler = new Task(cmn.HandleTcpCom);
                    _tcpHandlers.Add(tcpHandler);
                    tcpHandler.Start();
                }
            }
            catch (SocketException exp)
            {
                //Closing listener
                Console.WriteLine("Listener. [Swallow] {0}", exp.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Listener. [Throw] {0}", exp.Message);
                throw;
            }
            finally
            {
                foreach(var tcpHandler in _tcpHandlers)
                    tcpHandler.Dispose();

                _tcpListener.Stop();
                Console.WriteLine("Stopped listening for traffic on port {0}.", port);
            }
        }

        private static void DisplayStatus()
        {
            while (_running)
            {
                Thread.Sleep(2000);
                Console.WriteLine("Rx: {0}, Tx: {1}, Connections: {2}", InfoNode.TotalBytesRead, InfoNode.TotalBytesWritten, InfoNode.ConnectionCounter);
            }
        }
    }

    static class InfoNode
    {
        public static int TotalBytesWritten;
        public static int TotalBytesRead;
        public static int ConnectionCounter;
        
        private static int _connectionIndexer;

        public static int GetIndex()
        {
            return _connectionIndexer++;
        }
    }
}
