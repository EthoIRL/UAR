﻿using System.Net;
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
    
    private static readonly IPAddress Broadcast = IPAddress.Parse("192.168.0.159");
    private readonly EndPoint _localEndpoint = new IPEndPoint(Broadcast, 7484);

    public RemoteState()
    {
        new Thread(() =>
        {
            using Socket listener = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listener.Blocking = false;
            listener.Bind(_localEndpoint);

            Span<byte> bytes = GC.AllocateArray<byte>(9, true);
        
            while (true)
            {
                if (listener.Available != 0)
                {
                    var received = listener.Receive(bytes);

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
        }).Start();
    }
}