using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Rubicon.ReverseProxy.TcpTestServer
{
    class Communicator
    {
        private bool _running = true;
        private readonly TcpClient _client;
        private readonly int _connectionIndex;

        public Communicator(TcpClient client)
        {
            _client = client;
            _connectionIndex = InfoNode.GetIndex();
        }

        public void HandleTcpCom()
        {
            Task readerTask = null;
            Task writerTask = null;

            try
            {
                InfoNode.ConnectionCounter++;
                Console.WriteLine("{0}. Connection opened.", this._connectionIndex);

                readerTask = new Task(ReadData);
                writerTask = new Task(WriteData);

                writerTask.Start();
                readerTask.Start();

                Task.WaitAll(new[] { writerTask, readerTask });
            }
            catch (System.IO.IOException exp)
            {
                Console.WriteLine("{0}. [Swallow] {1}", this._connectionIndex, exp.Message);
            }
            catch (AggregateException exp)
            {
                foreach (var exception in exp.InnerExceptions)
                    Console.WriteLine("{0}. [Swallow] {1}", this._connectionIndex, exception.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine("{0}. [Throw] {1}", this._connectionIndex, exp.Message);
                throw;
            }
            finally
            {
                try
                {
                    Console.WriteLine("{0}. Ending connection.", this._connectionIndex);
                    InfoNode.ConnectionCounter--;

                    if (_client != null) _client.Close();

                    //Make sure 
                    Task.WaitAll(new[] { writerTask, readerTask });

                    readerTask.Dispose();
                    writerTask.Dispose();

                    Console.WriteLine("{0}. Connection terminated.", this._connectionIndex);
                }
                catch (AggregateException exp)
                {
                    foreach(var exception in exp.InnerExceptions)
                        Console.WriteLine("FATAL EXCEPTION! [Throw] {0}", exception.Message);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("FATAL EXCEPTION! [Throw] {0}", exp.Message);
                }
            }
        }

        private void WriteData()
        {
            NetworkStream networkStream = null;
            try
            {
                Console.WriteLine("{0}. Start writing data to client.", this._connectionIndex);

                var e = new ASCIIEncoding();
                networkStream = _client.GetStream();
                networkStream.WriteTimeout = 1000;

                if (!networkStream.CanWrite)
                {
                    Console.WriteLine("{0}. Cannot use the network stream to write data to.", _connectionIndex);
                    return;
                }

                while (_running)
                {
                    var data = GetRandomString();
                    var buffer = e.GetBytes(data);
                    networkStream.Write(buffer, 0, buffer.Length);
                    InfoNode.TotalBytesWritten += buffer.Length;
                }
            }
            catch (System.IO.IOException)
            {
                //Expected when the client closes
                //exp = {"Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host."}
                //Console.WriteLine("{0}. [Swallow] {1}", this._connectionIndex, exp.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine("{0}. [Throw] {1}", this._connectionIndex, exp.Message);
                throw;
            }
            finally
            {
                networkStream.Close();
                networkStream.Dispose();

                _running = false; 
                Console.WriteLine("{0}. End writing data to client.", this._connectionIndex);
            }
        }

        private void ReadData()
        {
            NetworkStream networkStream = null;
            try
            {
                Console.WriteLine("{0}. Start reading data from client.", this._connectionIndex);

                networkStream = _client.GetStream();
                networkStream.ReadTimeout = 1000;

                if (!networkStream.CanRead)
                {
                    Console.WriteLine("{0}. Cannot use the network stream to read data from.", _connectionIndex);
                    return;
                }

                while (_running)
                {
                    var buffer = new byte[128];
                    var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    InfoNode.TotalBytesRead += bytesRead;
                }
            }
            catch (System.IO.IOException)
            {
                //Expected when the clien closes
                //exp = {"Unable to read data from the transport connection: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond."}
                //Console.WriteLine("{0}. [Swallow] {1}", this._connectionIndex, exp.Message);
            }
            catch (Exception exp)
            {
                Console.WriteLine("{0}. [Throw] {1}", this._connectionIndex, exp.Message);
                throw;
            }
            finally
            {
                networkStream.Close();
                networkStream.Dispose();

                _running = false;
                Console.WriteLine("{0}. End reading data from client.", this._connectionIndex);
            }
        }

        private static readonly Random _rng = new Random();
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string GetRandomString(int? size = null)
        {
            lock (_rng)
            {
                var buffer = new char[size ?? _rng.Next(100)];

                for (var i = 0; i < buffer.Length; i++) 
                    buffer[i] = _chars[_rng.Next(_chars.Length)];

                return new string(buffer);
            }
        }
    }
}