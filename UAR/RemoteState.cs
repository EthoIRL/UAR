using System.Net;
using System.Net.Sockets;

namespace UAR;

public class RemoteState
{
    public bool LeftButton;
    public bool RightButton;
    public bool MiddleButton;
    public bool FourButton;
    public bool FiveButton;

    public int X;
    public int Y;

    private readonly Socket _listener;

    public RemoteState(IPAddress hostAddress)
    {
        var localEndpoint = new IPEndPoint(hostAddress, 7484);

        _listener = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _listener.Blocking = false;
        _listener.Bind(localEndpoint);
    }

    public void StartListening()
    {
        Span<byte> bytes = GC.AllocateArray<byte>(9, true);

        while (true)
        {
            if (_listener.Available != 0)
            {
                var received = _listener.Receive(bytes);

                if (received != 0)
                {
                    LeftButton = bytes[0] > 0;
                    RightButton = bytes[1] > 0;
                    MiddleButton = bytes[2] > 0;
                    FourButton = bytes[3] > 0;
                    FiveButton = bytes[4] > 0;

                    X = (short) (bytes[5] | bytes[6] << 8);
                    Y = (short) (bytes[7] | bytes[8] << 8);
                }
            }
        }
    }
}