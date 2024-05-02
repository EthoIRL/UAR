using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace UAR;

#pragma warning disable CA1416
static class Program
{
    private static readonly (int width, int height) Resolution = (400, 200);
    
    private static readonly Image<Bgra, byte> LocalImage = new(Resolution.width, Resolution.height);
    private static IntPtr _imageData;

    private static readonly PrimOpticalFlow OpticalFlow = new(4);
    private static readonly ScreenCapturer ScreenCapturer = new (0, 0, Resolution.width, Resolution.height);

    private static readonly Socket Socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private static readonly IPAddress Broadcast = IPAddress.Parse("192.168.0.190");
    private static readonly IPEndPoint EndPoint = new(Broadcast, 7483);
    
    [SupportedOSPlatform("windows")]
    static void Main(string[] args)
    {
        Socket.Connect(EndPoint);
        
        GCHandle pinnedArray = GCHandle.Alloc(LocalImage.Data, GCHandleType.Pinned);
        _imageData = pinnedArray.AddrOfPinnedObject();
        
        ScreenCapturer.StartCapture(_imageData, LocalImage.MIplImage.WidthStep);
    }

    public static void HandleImage()
    {
        Image<Gray, byte> gray = new Image<Gray, byte>(LocalImage.Width, LocalImage.Height);
        CvInvoke.CvtColor(LocalImage, gray, ColorConversion.Bgra2Gray);
        
        OpticalFlow.AddFrame(gray);

        var flow = OpticalFlow.FindFlowNearest();
        
        if (flow == null)
        {
            return;
        }
        
        var data = PreparePacket((short)flow.Value.x, (short)flow.Value.y);
        Socket.Send(data);
    }


    private static byte[] PreparePacket(short deltaX, short deltaY, bool ignoreAim = false, bool left = false, bool right = false, bool middle = false)
    {
        FromShort(deltaX, out var byte1, out var byte2);
        FromShort(deltaY, out var byte3, out var byte4);

        return new[]
        {
            byte1, byte2, byte3, byte4,
            (byte) (ignoreAim ? 1 : 0),
            (byte) (left ? 1 : 0),
            (byte) (right ? 1 : 0),
            (byte) (middle ? 1 : 0)
        };
    }

    private static void FromShort(short number, out byte byte1, out byte byte2)
    {
        byte2 = (byte) (number >> 8);
        byte1 = (byte) (number >> 0);
    }
}