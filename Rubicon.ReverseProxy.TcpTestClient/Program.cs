using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace Rubicon.ReverseProxy.TcpTestClient
{
    class Program
    {
        private static bool _running = true;
        private const string _defaultAddress = "localhost";
        private const int _defaultPort = 3000;
        private static int _defaultCloseTime = 5000; //-1 is infinite

        private static int _totalBytesRead;
        private static int _totalBytesWritten;

        static void Main(string[] args)
        {
            //Read input parameters
            var address = _defaultAddress;
            if (args.Length > 0)
                address = _defaultAddress;

            var port = _defaultPort;
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out port))
                    port = _defaultPort;
            }

            var closeTime = _defaultCloseTime;
            if (args.Length > 2)
            {
                if (!int.TryParse(args[2], out closeTime))
                    closeTime = _defaultCloseTime;
            }


            Console.Title = string.Format("Rubicon.ReverseProxy.TcpTestClient {0}:{1}", address, port);


            var monitorTask = new Task(DisplayStatus);
            monitorTask.Start();

            try
            {
                while (_running)
                {
                    TcpClient tcpClient = null;
                    NetworkStream networkStream = null;
                    try
                    {
                        Console.WriteLine("Opening a new connection.");
                        _totalBytesRead = 0;
                        _totalBytesWritten = 0;

                        tcpClient = new TcpClient(address, port);
                        networkStream = tcpClient.GetStream();

                        var readerTask = new Task(() => ReadData(networkStream, closeTime));
                        var writerTask = new Task(() => WriteData(networkStream, closeTime));
                        readerTask.Start();
                        writerTask.Start();

                        Task.WaitAll(new[] { readerTask, writerTask });
                    }
                    catch (AggregateException exp)
                    {
                        foreach (var exceptin in exp.InnerExceptions)
                            Console.WriteLine(exceptin.Message);
                        Thread.Sleep(1000);
                    }
                    catch (SocketException exp)
                    {
                        Console.WriteLine(exp.Message);
                        Thread.Sleep(1000);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine(exp.Message);
                        throw;
                    }
                    finally
                    {
                        Console.WriteLine("Session ended (Rx: {0}, Tx: {1})", _totalBytesRead, _totalBytesWritten);
                    }

                    if (networkStream != null) networkStream.Close();
                    if (tcpClient != null) tcpClient.Close();
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
            finally
            {
                Console.WriteLine("Client has terminated.");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void DisplayStatus()
        {
            while (_running)
            {
                Thread.Sleep(2000);
                Console.WriteLine("Rx: {0}, Tx: {1}", _totalBytesRead, _totalBytesWritten);
            }
        }

        private static void ReadData(NetworkStream networkStream, int closeTime)
        {
            Console.WriteLine("Starting to read data.");

            var dtmStart = DateTime.Now;
            try
            {
                networkStream.ReadTimeout = 5000;

                while (_running)
                {
                    var buffer = new byte[128];
                    var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    _totalBytesRead += bytesRead;

                    //var x = new ASCIIEncoding();
                    //Console.WriteLine("Read: {0}", x.GetString(buffer, 0, bytesRead));

                    if (closeTime != -1 && (DateTime.Now - dtmStart).TotalMilliseconds > closeTime)
                        return;
                }
            }
            catch (System.IO.IOException exp)
            {
                //Expected when the server closes
                //exp = {"Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."}
                //Console.WriteLine("Exception. [Swallow] {0}", exp.Message);
                System.Diagnostics.Debug.WriteLine("Exception. [Swallow] {0}", exp.Message);
            }
            catch (InvalidOperationException exp)
            {
                Console.WriteLine("Exception. [Swallow] {0}", exp.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception. [Throw] {0}", exp.Message);
                throw;
            }
            finally
            {
                //Both reader and writer has to terminate. Or no new connection will not be initiated.
                Console.WriteLine("Reader has terminated.");
            }
        }

        private static void WriteData(NetworkStream networkStream, int closeTime)
        {
            var dtmStart = DateTime.Now;
            try
            {
                var e = new ASCIIEncoding();
                networkStream.WriteTimeout = 5000;

                while (_running)
                {
                    var data = GetRandomString();
                    var buffer = e.GetBytes(data);
                    networkStream.Write(buffer, 0, buffer.Length);
                    _totalBytesWritten += buffer.Length;

                    if (closeTime != -1 && (DateTime.Now - dtmStart).TotalMilliseconds > closeTime)
                        return;
                }
            }
            catch (System.IO.IOException)
            {
                //Expected when the server closes
                //exp = {"Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host."}
                //Console.WriteLine(exp.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                throw;
            }
            finally
            {
                //Both reader and writer has to terminate. Or no new connection will not be initiated.
                Console.WriteLine("Writer has terminated.");
            }
        }

        private static readonly Random _rng = new Random();
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string GetRandomString(int? size = null)
        {
            var buffer = new char[size ?? _rng.Next(100)];

            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = _chars[_rng.Next(_chars.Length)];

            return new string(buffer);
        }
    }
}
