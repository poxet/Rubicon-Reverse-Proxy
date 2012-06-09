using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Rubicon.ReverseProxy.Service
{
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        public WindowsServiceInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
            serviceInstaller.DisplayName = "Rubicon Reverse Proxy Service";
            serviceInstaller.Description = "Reverse Proxy Service that redirects TCP traffic.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of WindowsService.cs
            serviceInstaller.ServiceName = WindowsService.Name;

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }
    }
}