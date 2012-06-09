using System;
using System.Net.Sockets;
using Rubicon.ReverseProxy.Repository.Interface;
using System.Text;
using System.Collections.Generic;

namespace Rubicon.ReverseProxy.Repository
{
    public class TcpClientActor : ITcpClientActor
    {
        private readonly TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private ASCIIEncoding _encoder;

        private ASCIIEncoding Encoding
        {
            get { return _encoder ?? (_encoder = new ASCIIEncoding()); }
        }

        private NetworkStream NetworkStream
        {
            get { return this._networkStream ?? (this._networkStream = this._tcpClient.GetStream()); }
        }

        public TcpClientActor(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        ~TcpClientActor()
        {
            Stop();
        }

        //NOTE: Commands
        //_tcpClientActor.Write(new byte[] { 0x08, 0x20, 0x08 }); //NOTE: Send a backspace
        //_tcpClientActor.Write(new byte[] { 0x08 }); //NOTE: Move cursor back

        public void Write(byte[] data)
        {
            NetworkStream.Write(data, 0, data.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            NetworkStream.Write(buffer, offset, count);
        }

        public void Write(string data)
        {
            Write(Encoding.GetBytes(data));
        }

        public void WriteLine(string data)
        {
            Write(Encoding.GetBytes(string.Format("{0}{1}", data, Environment.NewLine)));
        }

        public byte ReadByte(int timeout)
        {
            NetworkStream.ReadTimeout = timeout;
            return (byte)NetworkStream.ReadByte();
        }

        public int Read(byte[] buffer, int offset, int count, int timeout)
        {
            NetworkStream.ReadTimeout = timeout;
            return NetworkStream.Read(buffer, offset, count);
        }

        //TODO: Make it possible to use the backspace key for data entry
        //TODO: Disable arrow keys
        //Reads until enter is pressed. (char 13 and 10 is sent)
        public string ReadEntry()
        {
            var bytes = new List<byte>();
            int bPrev = -1;
            while (true)
            {
                var b = NetworkStream.ReadByte();
                if (bPrev == 13 && b == 10) break;

                if (b != 13)
                    bytes.Add((byte)b);

                bPrev = b;
            }
            return Encoding.GetString(bytes.ToArray());
        }

        public void Stop()
        {
            if (_networkStream != null)
            {
                _networkStream.Close();
                _networkStream.Dispose();
            }

            if (_tcpClient != null)
            {
                _tcpClient.Close();
            }
        }

        public void Action(TelnetAction action, int? value)
        {
            //http://ascii-table.com/ansi-escape-sequences-vt-100.php
            //http://web.cs.mun.ca/~michael/c/ascii-table.html

            switch(action)
            {
                case TelnetAction.ClearScreen:
                    Write(new byte[] { 0x1b, 0x5b, 0x32, 0x4a }); //Esc[2J
                    break;
                case TelnetAction.MoveLeft:
                    if (value == null) value = 1; //Default is 1
                    if (value.Value < 1 ) throw new InvalidOperationException("Value must be 1 or larger.");
                    if (value.Value > 9) throw new InvalidOperationException("Values larger than 1 is yet not supported. Needs to be developed.");
                    Write(new byte[] { 0x1b, 0x5b, (byte)(0x30 + value.Value), 0x44 }); //Esc[1D
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Action {0} has not been implemented.", action));
            }
        }
    }
}