using System.Threading;
using Rubicon.ReverseProxy.Repository;
using Rubicon.ReverseProxy.Repository.Interface;
using System;
using Tharga.Support.Client.Base;

namespace Rubicon.ReverseProxy.Business
{
    sealed class TelnetServer
    {
        private static readonly object _syncRoot = new object();
        private int? _telnetPort;
        private bool _running;
        private static TelnetServer _instance;
        private readonly Thread _listenerThread;
        private readonly ITcpListenerProvider _listener;

        internal int TelnetPort
        {
            get
            {
                if (_telnetPort == null)
                {
                    int tp;
                    var telnetPort = System.Configuration.ConfigurationManager.AppSettings["TelnetConfigPort"];
                    if (!int.TryParse(telnetPort, out tp)) 
                        tp = 23; //Default port for telnet is 23

                    _telnetPort = tp;
                }
                return _telnetPort.Value;
            }
        }

        public static TelnetServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new TelnetServer();
                        }
                    }
                }
                return _instance;
            }
        }

        private TelnetServer()
        {
            _listenerThread = new Thread(ListenForTelnetClients);
            _listener = TelnetListenerProvider.Instance;
        }

        ~TelnetServer()
        {
            Stop();
        }

        public void Start()
        {
            try
            {
                _listener.Start(TelnetPort);
                _listenerThread.Start();
                _running = true;

                LogHelper.LogMessage(string.Format("Telnet server for configuration is running on port {0}.",TelnetPort), Issue.IssueLevel.Information);
            }
            catch(System.Net.Sockets.SocketException exception)
            {
                //Only one usage of each socket address (protocol/network address/port) is normally permitted
                LogHelper.LogMessage(string.Format("Telnet server for configuration could not be started.{1}Try using a different port by setting TelnetConfigPort in appSettings section in the config file.{1}{0}", exception.Message, Environment.NewLine), Issue.IssueLevel.Error);
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                if (!_running) return;
                _running = false;

                if (_listener != null) 
                    _listener.Stop();

                if (_listenerThread != null) 
                    _listenerThread.Join();
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
                throw;
            }
        }

        private void ListenForTelnetClients()
        {
            while (_running)
            {
                var tcpClientActor = _listener.WaitForTcpClient();

                if (tcpClientActor != null)
                {
                    var telnetActor = new TelnetActor(tcpClientActor);
                    telnetActor.HandleTelnetClientCommAsync();
                }
            }
        }
    }
}