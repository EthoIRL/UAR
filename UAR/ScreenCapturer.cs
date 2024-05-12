using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Factory1 = SharpDX.DXGI.Factory1;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace UAR;

public class ScreenCapturer
{
    private static readonly Stopwatch ScWatch = new();
    private static readonly List<long> Timings = new();

    private static int _adapterIndex;
    private static int _displayIndex;
    private static int _outputWidth;
    private static int _outputHeight;
    
    public ScreenCapturer(int adapterIndex, int displayIndex, int outputWidth, int outputHeight)
    {
        _adapterIndex = adapterIndex;
        _displayIndex = displayIndex;
        _outputWidth = outputWidth;
        _outputHeight = outputHeight;
    }
    

    public void StartCapture(IntPtr imgDataPtr, int widthStep)
    {
        using Factory1 factory1 = new Factory1();
        using Adapter1 adapter1 = factory1.GetAdapter1(_adapterIndex);
        using Device device = new Device(adapter1);
        using Output output = adapter1.GetOutput(_displayIndex);
        using Output1 output1 = output.QueryInterface<Output1>();

        Int32 width = output1.Description.DesktopBounds.Right - output1.Description.DesktopBounds.Left;
        Int32 height = output1.Description.DesktopBounds.Bottom - output1.Description.DesktopBounds.Top;

        int centerWidth = width / 2 - _outputWidth / 2;
        int centerHeight = height / 2 - _outputHeight / 2;

        Texture2DDescription texture2DDescription = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = _outputWidth,
            Height = _outputHeight,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 0,
            ArraySize = 1,
            SampleDescription =
            {
                Count = 1,
                Quality = 0
            },
            Usage = ResourceUsage.Staging
        };

        using Texture2D texture2D = new Texture2D(device, texture2DDescription);
        using OutputDuplication outputDuplication = output1.DuplicateOutput(device);

        DataBox dataBox = device.ImmediateContext.MapSubresource(texture2D, 0, MapMode.Read, MapFlags.None);

        ResourceRegion resourceRegion = new ResourceRegion(0, centerHeight, 0, width + _outputWidth, height+centerHeight, 1);
        
        var heightPartitioner = Partitioner.Create(0, _outputHeight);
        
        bool previousState = false;
        
        int bytesPerPixel = _outputWidth * 4;

        while (true)
        {
            ScWatch.Restart();

            if (previousState)
            {
                outputDuplication.ReleaseFrame();
            }

            var status = outputDuplication.TryAcquireNextFrame(0, out var data, out var screenResource);
            previousState = status.Success;

            if (screenResource == null || data.LastPresentTime == 0)
            {
                continue;
            }

            var screenTexture2D = screenResource.QueryInterface<Texture2D>();
            device.ImmediateContext.CopySubresourceRegion(screenTexture2D, 0, resourceRegion, texture2D, 0);

            Parallel.ForEach(heightPartitioner, range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    var dataBoxPointerOffset = dataBox.DataPointer + (y * dataBox.RowPitch);
                    var imagePointerOffset = imgDataPtr + (y * widthStep);

                    Utilities.CopyMemory(imagePointerOffset, dataBoxPointerOffset, bytesPerPixel);
                }
            });

            screenTexture2D.Dispose();
            screenResource.Dispose();

            Program.HandleImage();
            
            ScWatch.Stop();
            if (!ScWatch.IsRunning)
            {
                Timings.Add(ScWatch.ElapsedTicks);
            }

            if (Timings.Count != 0 && Timings.Count % 100 == 0)
            {
                Console.WriteLine($"Timings SC Avg: ({Timings.Average() / 10000.0}, {Timings.Count})");
            }
        }
    }
}