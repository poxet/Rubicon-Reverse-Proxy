namespace Rubicon.ReverseProxy.Repository.Interface
{
    public enum TelnetAction
    {
        ClearScreen,
        MoveLeft,
    }

    public interface ITcpClientActor
    {
        void Write(byte[] data);
        void Write(byte[] buffer, int offset, int count);
        void Write(string data);
        void WriteLine(string data);

        byte ReadByte(int timeout = -1);
        int Read(byte[] buffer, int offset, int count, int timeout = -1);
        string ReadEntry();

        void Stop();
        void Action(TelnetAction action, int? value = null);
    }
}