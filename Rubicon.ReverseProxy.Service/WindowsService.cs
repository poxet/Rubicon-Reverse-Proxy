using System.ServiceProcess;

namespace Rubicon.ReverseProxy.Service
{
    public sealed class WindowsService : ServiceBase
    {
        internal const string Name = "Rubicon Reverse Proxy";
        private readonly Business.ReversedProxyServer _reversedProxyServer;

        public WindowsService()
        {
            ServiceName = Name;
            EventLog.Log = "Application";

            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = true;
            CanStop = true;

            _reversedProxyServer = new Business.ReversedProxyServer();
        }

        static void Main()
        {
            ServiceBase.Run(new WindowsService());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            _reversedProxyServer.Start();
        }

        protected override void OnStop()
        {
            base.OnStop();

            _reversedProxyServer.Stop();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();

            _reversedProxyServer.Stop();
        }

        protected override void OnCustomCommand(int command)
        {
            //  A custom command can be sent to a service by using this method:
            //#  int command = 128; //Some Arbitrary number between 128 & 256
            //#  ServiceController sc = new ServiceController("NameOfService");
            //#  sc.ExecuteCommand(command);

            base.OnCustomCommand(command);
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }
    }
}
