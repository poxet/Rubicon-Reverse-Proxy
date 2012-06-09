using System;

namespace Rubicon.ReverseProxy.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Press any key to terminate the reverse proxy server...");

                Business.ReversedProxyServer reversedProxyServer = null;
                try
                {
                    reversedProxyServer = new Business.ReversedProxyServer();
                    reversedProxyServer.Start();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
                finally
                {
                    Console.ReadKey();
                    
                    if (reversedProxyServer != null) 
                        reversedProxyServer.Stop();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadKey();
            }
        }
    }
}
