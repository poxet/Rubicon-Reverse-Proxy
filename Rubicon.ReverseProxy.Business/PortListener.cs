using System;
using Rubicon.ReverseProxy.Business.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Rubicon.ReverseProxy.Repository;
using Tharga.Support.Client.Base;

namespace Rubicon.ReverseProxy.Business
{
    class PortListener
    {
        private enum PortListenerState 
        { 
            NotStarted,
            Running,
            Stopped,
        }

        private PortListenerState _state = PortListenerState.NotStarted;
        private readonly List<RedirectRule> _redirectRules;
        private readonly Task _listenerTask;
        private readonly TcpListenerProvider _tcpListener;
        private int? _publicPortRequested;

        private readonly List<TcpActor> _tcpActors = new List<TcpActor>();

        public int PublicPortRequested
        {
            get
            {
                if (_publicPortRequested == null)
                {
                    if (this._redirectRules.Count == 0) 
                        throw new InvalidOperationException("A listener should have at least one redirect rule.");
                    _publicPortRequested = this._redirectRules[0].PublicRequestPort;
                }
                return _publicPortRequested.Value;
            }
        }

        private PortListener(RedirectRule redirectRule)
        {
            TcpActor.TcpActorStoppedEvent += this.TcpActor_TcpActorStoppedEvent;

            _listenerTask = new Task(ListenForClients);
            _redirectRules = new List<RedirectRule>();
            Add(redirectRule);
            _tcpListener = TcpListenerProvider.Create();
        }

        void TcpActor_TcpActorStoppedEvent(object sender, TcpActor.TcpActorStoppedEventArgs e)
        {
            _tcpActors.Remove(e.TcpActor);
        }

        ~PortListener()
        {
            if (this._state == PortListenerState.Running) 
                Stop();
        }

        public static PortListener Create(RedirectRule redirectRule)
        {
            return new PortListener(redirectRule);
        }

        public void Start()
        {
            if (_state != PortListenerState.NotStarted) 
                throw new InvalidOperationException(string.Format("The listener must have the state 'NotStarted' when calling Start. State {0} is not valid.",_state));
            _state = PortListenerState.Running;

            _tcpListener.Start(PublicPortRequested);
            _listenerTask.Start();
        }

        public void Stop()
        {
            if (_state != PortListenerState.Stopped) 
                return;
            if (_state != PortListenerState.Running) 
                throw new InvalidOperationException(string.Format("The listener must have the state 'Running' when calling Stop. State {0} is not valid.", _state));
            _state = PortListenerState.Stopped;

            _tcpListener.Stop();

            foreach (var tcpActor in _tcpActors)
                tcpActor.Stop();

            _listenerTask.Wait();
            _listenerTask.Dispose();

            if (_tcpActors.Count > 0)
            {
                var exp = new InvalidOperationException("There are still tcpActors that have not yet been stoped and removed.");
                exp.Data.Add("TcpActors.Count", _tcpActors.Count);
                throw exp;
            }
        }

        private void ListenForClients()
        {
            try
            {
                PerformaceCounters.Instance.ListenerCounterIncrement();
                LogHelper.LogMessage(string.Format("Listening for incoming connections on port {0}.", PublicPortRequested), Issue.IssueLevel.Information);

                while (this._state == PortListenerState.Running)
                {
                    var tcpClientActor = _tcpListener.WaitForTcpClient();

                    if (tcpClientActor != null)
                    {
                        var tcpActor = new TcpActor(tcpClientActor, _redirectRules);
                        _tcpActors.Add(tcpActor);
                        tcpActor.Start();
                    }
                }
            }
            catch (Exception exp)
            {
                if (_tcpListener != null && _tcpListener.LocalEndpoint != null)
                {
                    exp.Data.Add("Address", _tcpListener.LocalEndpoint.Address);
                    exp.Data.Add("Address", _tcpListener.LocalEndpoint.Port);
                }

                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
            finally
            {
                PerformaceCounters.Instance.ListenerCounterDecrement();
                LogHelper.LogMessage(string.Format("Listener with port {0} has terminated.", PublicPortRequested), Issue.IssueLevel.Information);
            }
        }

        public int RuleCount { get { return _redirectRules.Count; } }

        public void Add(RedirectRule redirectRule)
        {
            if (this._redirectRules.Count > 0)
            {
                if (PublicPortRequested != redirectRule.PublicRequestPort) 
                    throw new InvalidOperationException(string.Format("Cannot add redirect rule on different ports to the same listener. Current port is {0} and the requests added port {1}.", PublicPortRequested, redirectRule.PublicRequestPort));
            }

            _redirectRules.Add(redirectRule);
            //LogHelper.LogMessage(string.Format("Host {0} added to listener with port {1}. Redirects to {2}:{3}.", string.IsNullOrEmpty(redirectRule.PublicRequestHost) ? "'Any'" : redirectRule.PublicRequestHost, redirectRule.PublicRequestPort, redirectRule.InternalTargetAddress, redirectRule.InternalTargetPort), Issue.IssueLevel.Information);
        }

        public bool Remove(RedirectRule redirectRule)
        {
            if (!_redirectRules.Remove(redirectRule)) 
                return false;

            //LogHelper.LogMessage(string.Format("Host {0} removed from listener with port {1}. Redirects to {2}:{3}.", string.IsNullOrEmpty(redirectRule.PublicRequestHost) ? "'Any'" : redirectRule.PublicRequestHost, redirectRule.PublicRequestPort, redirectRule.InternalTargetAddress, redirectRule.InternalTargetPort), Issue.IssueLevel.Information);

            return true;
        }
    }
}