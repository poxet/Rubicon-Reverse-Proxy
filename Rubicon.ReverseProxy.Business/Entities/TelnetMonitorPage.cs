using Rubicon.ReverseProxy.Repository.Interface;
using System;

namespace Rubicon.ReverseProxy.Business.Entities
{
    sealed class TelnetMonitorPage : TelnetPage
    {
        private readonly ITcpClientActor _tcpClientActor;

        public TelnetMonitorPage(char key, ITcpClientActor tcpClientActor)
            : base(key, "Event Monitor")
        {
            _tcpClientActor = tcpClientActor;
        }

        void LogHelper_MessageEvent(object sender, LogHelper.MessageEventArgs e)
        {
            try
            {
                _tcpClientActor.WriteLine(e.Message);
            }
            catch (Exception exp)
            {
                LogHelper.LogException(exp, false, LogHelper.ExceptionSeverity.ErrorException);
            }
        }

        public override string GetPageContent()
        {
            _tcpClientActor.Action(TelnetAction.ClearScreen);
            _tcpClientActor.WriteLine("Monitoring all events in the reverse proxy server.");
            _tcpClientActor.WriteLine("[Press enter to exit the monitor...]");
            LogHelper.MessageEvent += LogHelper_MessageEvent;
            _tcpClientActor.ReadEntry();
            LogHelper.MessageEvent -= LogHelper_MessageEvent;
            return null;
        }
    }
}