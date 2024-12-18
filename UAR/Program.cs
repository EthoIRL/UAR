﻿using System;
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

    private static readonly bool TapFireFix = false;
    private static bool _allowBypass;

    private static readonly bool WaitForAim = false;
    private static readonly int WaitMs = 250;
    private static readonly Stopwatch Aimwatch = new();

    private static bool _lastLeftButton;
    private static bool _lastRightButton;

    //
    // Provide your own mouse controlling system
    //
    private static readonly Socket MouseSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private static readonly IPEndPoint ControllerEndPoint = new(IPAddress.Parse("192.168.68.54"), 7483);
    public static RemoteState _remoteState = null!;

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

        MouseSocket.Connect(ControllerEndPoint);

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
            CvInvoke.CvtColor(LocalImage, OpticalModule.FrameBuffer[^1], OpticalModule.UseFullColor ? ColorConversion.Bgra2Bgr : ColorConversion.Bgra2Gray);
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

                var mousePacket = PreparePacket(deltaX, deltaY);
                MouseSocket.Send(mousePacket);
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
        return new[]
        {
            (byte) (deltaX & 0xFF), (byte) (deltaX >> 8), (byte) (deltaY & 0xFF), (byte) (deltaY >> 8)
        };
    }
}