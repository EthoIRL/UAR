using System;
using System.Drawing;
using CircularBuffer;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;

namespace UAR;

public class PrimOpticalFlow
{
    private readonly CircularBuffer<Image<Gray, byte>> _frameBuffer;
    private readonly int backlog;

    public PrimOpticalFlow(int frameBacklog)
    {
        backlog = frameBacklog;
        _frameBuffer = new CircularBuffer<Image<Gray, byte>>(frameBacklog);
    }

    public void AddFrame(Image<Gray, byte> frame)
    {
        _frameBuffer.PushFront(frame);
    }

    // private readonly GFTTDetector _gfttDetector = new(25, 0.01, 1, 2, true);
    private readonly ORB _gfttDetector = new ORB(50);

    public (int x, int y)? FindFlowNearest()
    {
        if (_frameBuffer.Size < backlog)
        {
            return null;
        }

        var first = _frameBuffer[0];
        var second = _frameBuffer[backlog-1];

        if (first.Data == second.Data)
        {
            return null;
        }

        var points = _gfttDetector.Detect(second);
        
        if (points.Length == 0)
        {
            return null;
        }
        
        PointF[] prevCorners = new PointF[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            prevCorners[i] = points[i].Point;
        }
        CvInvoke.CalcOpticalFlowPyrLK(second, first, prevCorners, new Size(100, 100), 7, new MCvTermCriteria(0.0), out var secondFeatures, out var status, out var trackError,
            LKFlowFlag.LKGetMinEigenvals, 0);
        
        double[] relativeX = new double[secondFeatures.Length];
        double[] relativeY = new double[secondFeatures.Length];
        
        for (int i = 0; i < secondFeatures.Length; i++)
        {
            var featureStatus = status[i];
            if (featureStatus == 1)
            {
                relativeX[i] = (secondFeatures[i].X - prevCorners[i].X);
                relativeY[i] = (secondFeatures[i].Y - prevCorners[i].Y);
            }
        }

        var deltaX = (int) Math.Floor(relativeX.Average());
        
        var deltaY = relativeY.Average();
        // Crude fix for bouncing issue either way it only needs to move in one vertical direction
        if (deltaY < 0)
        {
            deltaY = 0;
            // deltaY *= -1;
        }

        return (deltaX, (int) Math.Round(deltaY));
    }
}