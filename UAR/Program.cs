using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UAR.Modules;

namespace UAR;

static class Program
{
    private static readonly (int width, int height) Resolution = (600, 200);

    private static readonly Image<Bgra, byte> LocalImage = new(Resolution.width, Resolution.height);
    private static IntPtr _imageData;

    private static GpuMat? _gpuImage;
    private static int _hasFrames;

    private static readonly PyrLkOpticalModule OpticalModule = new(4);
    private static readonly ScreenCapturer ScreenCapturer = new(0, 0, Resolution.width, Resolution.height);

    private static readonly Socket Socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private static readonly IPAddress Broadcast = IPAddress.Parse("192.168.68.53");
    private static readonly IPEndPoint EndPoint = new(Broadcast, 7483);

    private static RemoteState _remoteState = null!;

    private static readonly bool TapFireFix = false;
    private static bool _allowBypass;

    private static readonly bool WaitForAim = true;
    private static readonly int WaitMs = 250;
    private static readonly Stopwatch Aimwatch = new();

    private static bool _lastLeftButton;
    private static bool _lastRightButton;

    [SupportedOSPlatform("windows")]
    static void Main(string[] args)
    {
        var hostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.ToString().Contains("192")).ToList()[^1];
        Console.WriteLine($"Optical Flow: {OpticalModule.GetType().Name} | Backlog: {OpticalModule.Backlog} | Gpu: {OpticalModule.IsGpuMat} | Host: {hostAddress}");

        if (!CudaInvoke.HasCuda && OpticalModule.IsGpuMat)
        {
            throw new Exception("Emgu.CV cuda runtime is not present. Please recompile using the cuda runtime or switch to a CPU implemented OpticalModule.");
        }

        _remoteState = new RemoteState(hostAddress);
        new Thread(() => _remoteState.StartListening()).Start();

        Socket.Connect(EndPoint);

        GCHandle pinnedArray = GCHandle.Alloc(LocalImage.Data, GCHandleType.Pinned);
        _imageData = pinnedArray.AddrOfPinnedObject();

        ScreenCapturer.StartCapture(_imageData, LocalImage.MIplImage.WidthStep);
    }

    public static void HandleImage()
    {
        if (OpticalModule.IsGpuMat)
        {
            _gpuImage ??= new();

            _gpuImage.Upload(LocalImage);
            CudaInvoke.CvtColor(_gpuImage, OpticalModule.FrameBuffer[^1], ColorConversion.Bgra2Gray);
        }
        else
        {
            CvInvoke.CvtColor(LocalImage, OpticalModule.FrameBuffer[^1], ColorConversion.Bgra2Gray);
        }

        OpticalModule.AddFrame(OpticalModule.FrameBuffer[^1]);

        if (_hasFrames < OpticalModule.Backlog)
        {
            _hasFrames++;
            return;
        }

        var flow = OpticalModule.FindMovementFromFlow();

        if (flow != null && (_remoteState.LeftButton && _remoteState.RightButton || _allowBypass))
        {
            _allowBypass = !_allowBypass && TapFireFix;

            if (!_lastLeftButton || !_lastRightButton)
            {
                Aimwatch.Restart();
            }

            if (Aimwatch.ElapsedMilliseconds > WaitMs && WaitForAim || !WaitForAim || _allowBypass)
            {
                short deltaX = (short) (flow.Value.x + _remoteState.X);
                short deltaY = (short) (flow.Value.y + _remoteState.Y);
                
                var data = PreparePacket(deltaX, deltaY);
                Socket.Send(data);
            }
        }
        else
        {
            OpticalModule.OverflowX = 0;
            OpticalModule.OverflowY = 0;
        }

        _lastLeftButton = _remoteState.LeftButton;
        _lastRightButton = _remoteState.RightButton;

        _remoteState.X = 0;
        _remoteState.Y = 0;
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