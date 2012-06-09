using Rubicon.ReverseProxy.Repository.Interface;
using System;
using System.Net.Sockets;
using System.Text;
using Rubicon.ReverseProxy.Repository;
using System.Collections.Generic;
using Rubicon.ReverseProxy.Business.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Tharga.Support.Client.Base;

namespace Rubicon.ReverseProxy.Business
{
    class TcpActor
    {
        #region Event


        public class TcpActorStoppedEventArgs : EventArgs
        {
            public TcpActor TcpActor { get; private set; }

            public TcpActorStoppedEventArgs(TcpActor tcpActor)
            {
                TcpActor = tcpActor;
            }
        }

        public static event EventHandler<TcpActorStoppedEventArgs> TcpActorStoppedEvent;

        public static void OnTcpActorStoppedEvent(TcpActorStoppedEventArgs e)
        {
            var handler = TcpActorStoppedEvent;
            if (handler != null)
                handler(null, e);
        }


        #endregion

        private enum TcpActorState
        {
            NotStarted,
            Running,
            RequestStop,
            Stopped,            
        }

        private static readonly object _syncRoot = new object();
        private static int _connectionCounter;
        private int _connectionIndex;

        private TcpActorState _state = TcpActorState.NotStarted;
        private readonly ITcpClientActor _tcpClientActor;
        private readonly IEnumerable<RedirectRule> _redirectRules;
        private ITcpClientActor _tcpTargetActor;
        private readonly AutoResetEvent _targetRecievedEvent = new AutoResetEvent(false);
        private const int MessageChunkSize = 2048;
        private string _staticResponse;

        private RedirectRule _currentRedirectRule;

        private int _totalTx;
        private int _totalRx;

        private Task _engineTask;       //This task is active as long as the 'Engine' function is running.
        private Task _fromClientTask;
        private Task _fromTargetTask;

        public TcpActor(ITcpClientActor tcpClientActor, IEnumerable<RedirectRule> redirectInfos)
        {
            RedirectRuleManager.RedirectRuleRemovedEvent += RedirectRuleManager_RedirectRuleRemovedEvent;

            _tcpClientActor = tcpClientActor;
            _redirectRules = redirectInfos;
        }

        ~TcpActor()
        {
            Stop();

            _engineTask.Wait();
            _engineTask.Dispose();
        }
        
        void RedirectRuleManager_RedirectRuleRemovedEvent(object sender, RedirectRuleManager.RedirectRuleRemovedEventArgs e)
        {
            try
            {
                if (_currentRedirectRule == e.RedirectRule)
                    Stop();
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception, false, LogHelper.ExceptionSeverity.ErrorException);
            }
        }

        public void Start()
        {
            if (_state != TcpActorState.NotStarted) 
                throw new InvalidOperationException(string.Format("The actor must have the state 'NotStarted' when calling Start. State {0} is not valid.", _state));
            _state = TcpActorState.Running;

            lock (_syncRoot)
            {
                _connectionIndex = _connectionCounter;
                _connectionCounter++;
            }

            _engineTask = new Task(Engine);
            _engineTask.Start();
        }

        public void Stop()
        {
            if (_state != TcpActorState.Stopped) return;
            if (_state != TcpActorState.RequestStop && _state != TcpActorState.Running)
                throw new InvalidOperationException(string.Format("The actor must have the state 'RequestStop' or 'Running' when calling Stop. State {0} is not valid.", _state));
            _state = TcpActorState.Stopped;

            _fromClientTask.Dispose();
            _fromTargetTask.Dispose();

            _tcpClientActor.Stop();
            if (_tcpTargetActor != null) 
                _tcpTargetActor.Stop();

            OnTcpActorStoppedEvent(new TcpActorStoppedEventArgs(this));
        }

        private void Engine()
        {
            try
            {
                PerformaceCounters.Instance.ConnectionCounterIncrement();
                LogHelper.ShowMessage(string.Format("Connection {0} requested.", _connectionIndex));

                _fromClientTask = new Task(FromClientToTarget);
                _fromTargetTask = new Task(FromTargetToClient);

                _fromClientTask.Start();
                _fromTargetTask.Start();

                Task.WaitAll(new[] { _fromClientTask, _fromTargetTask });
            }
            catch (AggregateException exception)
            {
                foreach(var exp in exception.InnerExceptions)
                    LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
            catch (Exception exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
            finally
            {
                Stop();

                PerformaceCounters.Instance.ConnectionCounterDecrement();
                LogHelper.ShowMessage(string.Format("Connection {0} closed. (Rx: {1}, Tx: {2})", _connectionIndex, _totalRx, _totalTx));
            }
        }

        private void FromClientToTarget()
        {
            try
            {
                LogHelper.ShowMessage(string.Format("Connection {0} started to read.", _connectionIndex));
                var messageChunk = new byte[MessageChunkSize];

                while (_state == TcpActorState.Running)
                {
                    var bytesRead = _tcpClientActor.Read(messageChunk, 0, messageChunk.Length);
                    PerformaceCounters.Instance.Rx(bytesRead);
                    _totalRx += bytesRead;

                    //If there is no _tcpTargetActor, try to get one.
                    if (_tcpTargetActor == null)
                    {
                        _currentRedirectRule = GetTargetInfo(messageChunk, bytesRead);
                        if (_currentRedirectRule != null)
                            _tcpTargetActor = new TcpClientActor(new TcpClient(_currentRedirectRule.InternalTargetAddress.ToString(), _currentRedirectRule.InternalTargetPort));
                        _targetRecievedEvent.Set();
                    }

                    if (_tcpTargetActor == null)
                    {
                        //If there still is no target actor, exit the loop.
                        LogHelper.LogMessage("The reader could not find a target actor to forward the traffic to.", Issue.IssueLevel.Warning);
                        break;
                    }

                    _tcpTargetActor.Write(messageChunk, 0, bytesRead);

                    //TODO: Here is the place to log throughput from client to server. At this point data has been transferred from the client to the target.
                }
            }
            catch (System.IO.IOException exp)
            {
                //Expected when the client closes.
                //exp = {"Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."}
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.InformationException);
            }
            catch (ObjectDisposedException exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
            catch (InvalidOperationException exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
            catch (SocketException exp)
            {
                //Expected when there is no server listening.
                LogHelper.LogException(exp, true, LogHelper.ExceptionSeverity.WarningException);
                _staticResponse = exp.Message; //This message is sent back as response to the client
                _targetRecievedEvent.Set();
            }
            catch (Exception exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
                throw;
            }
            finally
            {
                _state = TcpActorState.RequestStop;
                LogHelper.ShowMessage(string.Format("Connection {0} stopped to read. (Rx: {1})", _connectionIndex, _totalRx));
            }
        }

        private void FromTargetToClient()
        {
            try
            {
                //Wait for response
                while (_state == TcpActorState.Running && _tcpTargetActor == null && _staticResponse == null)
                {
                    LogHelper.ShowMessage(string.Format("Connection {0} is waiting for information about the internal target.", _connectionIndex));
                    _targetRecievedEvent.WaitOne();
                }

                if (_tcpTargetActor != null)
                {
                    LogHelper.ShowMessage(string.Format("Connection {0} started to transfer data. ({1}:{2} --> {3}:{4})",
                        _connectionIndex, string.IsNullOrEmpty(_currentRedirectRule.PublicRequestHost) ? "'Any'" : _currentRedirectRule.PublicRequestHost,
                        _currentRedirectRule.PublicRequestPort, _currentRedirectRule.InternalTargetAddress, _currentRedirectRule.InternalTargetPort));

                    var messageChunk = new byte[MessageChunkSize];

                    while (_state == TcpActorState.Running)
                    {
                        var bytesRead = 0;
                        try
                        {
                            bytesRead = _tcpTargetActor.Read(messageChunk, 0, messageChunk.Length);
                            PerformaceCounters.Instance.Tx(bytesRead);
                            _totalTx += bytesRead;
                        }
                        catch (System.IO.IOException exp)
                        {
                            //Expected when the server closes
                            //exp = {"Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."}
                            //Console.WriteLine("{0}. [Swallow] {1}", _connectionIndex, exp.Message);
                            LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.InformationException);
                        }

                        if (bytesRead > 0)
                            _tcpClientActor.Write(messageChunk, 0, bytesRead);

                        //TODO: Here is the place to log throughput from server back to client. At this point response data has been transferred from the target back to the client.
                    }
                }
                else if (!string.IsNullOrEmpty(_staticResponse))
                {
                    _fromClientTask.Wait(); //Wait until the reader have completed
                    _tcpClientActor.Write(_staticResponse);

                    LogHelper.ShowMessage(string.Format("Connection {0} returned a static response. ({1})", _connectionIndex, _staticResponse));

                    PerformaceCounters.Instance.Tx(_staticResponse.Length);
                    _totalTx += _staticResponse.Length;
                }
                else
                {
                    //In this case, just close the connection
                    LogHelper.LogMessage("There is no target actor to send back a response to, and no static response.", Issue.IssueLevel.Warning);
                }
            }
            catch (System.IO.IOException exp)
            {
                //Expected when the client closes.
                //exp = {"Unable to write data to the transport connection: An established connection was aborted by the software in your host machine."}
                LogHelper.LogException(exp,false, LogHelper.ExceptionSeverity.InformationException);
            }
            catch (Exception exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
                throw;
            }
            finally
            {
                _state = TcpActorState.RequestStop;
                LogHelper.ShowMessage(string.Format("Connection {0} stopped to transfer data. (Tx: {1})", _connectionIndex, _totalTx));
            }
        }

        private RedirectRule GetTargetInfo(byte[] messageChunk, int bytesRead)
        {
            try
            {
                var encoder = new ASCIIEncoding();
                var data = encoder.GetString(messageChunk, 0, bytesRead);

                var host = string.Empty;
                var port = 80;

                const string Signature = "Host:";
                var pos = data.IndexOf(Signature, StringComparison.Ordinal);
                if (pos != -1)
                {
                    var pe = data.IndexOf(Environment.NewLine, pos, StringComparison.Ordinal);
                    var targetAddress = data.Substring(pos + Signature.Length + 1, pe - pos - Signature.Length - 1).Split(':');
                    host = targetAddress[0];

                    if (targetAddress.Length > 1)
                    {
                        if (!int.TryParse(targetAddress[1], out port)) port = 80;
                    }
                }

                //var pos1 = data.IndexOf("GET");
                //var pe1 = data.IndexOf(Environment.NewLine, pos1);
                //var page = data.Substring(pos1 + 4, pe1 - pos1 - 4);

                //Console.WriteLine("Message on its way to {0} page {1}.", host, page);

                //Client ---- PublicPort ----> RequestedHost ---- LocalPort ----> LocalTargetHost
                //Client IP: Any
                //PublicPort: PublicPort
                //RequestedHost: host

                //Look up the following using the client, PublicPort and RequestedHost.
                //LocalPort: LOOK UP IN SETTING
                //LocalTargetHost: LOOK UP IN SETTING


                var redirectRule = this._redirectRules.SingleOrDefault(x => x.PublicRequestHost == host && x.PublicRequestPort == port);

                if (redirectRule == null)
                    redirectRule = this._redirectRules.SingleOrDefault(x => string.IsNullOrEmpty(x.PublicRequestHost));

                if (redirectRule == null)
                {
                    _staticResponse = string.Format("There is no redirect rule for host {0} and port {1}.", string.IsNullOrEmpty(host) ? "'Any'" : host, port);
                    LogHelper.LogMessage(_staticResponse, Issue.IssueLevel.Warning);
                    return null;
                }

                return redirectRule;
            }
            catch (ArgumentOutOfRangeException exp)
            {
                var invalidOperationException = new InvalidOperationException("Message chunk size could be too small.", exp);
                invalidOperationException.Data.Add("MessageChunkSize", MessageChunkSize);
                throw invalidOperationException;
            }
        }
    }
}