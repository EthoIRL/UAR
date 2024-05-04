﻿using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Size = System.Drawing.Size;

namespace UAR.OpticalFlow;

public class PrimOpticalFlow : GenericOpticalFlow<Mat>
{
    // private readonly GFTTDetector _detector = new(25, 0.01, 1, 2, true);
    private readonly ORB _detector = new ORB(50);

    public (int x, int y)? FindMovementFromFlow()
    {
        if (FrameBuffer.Size < Backlog)
        {
            return null;
        }

        var first = FrameBuffer[0];
        var second = FrameBuffer[Backlog-1];

        var points = _detector.Detect(second);
        
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

    public PrimOpticalFlow(int frameBacklog) : base(frameBacklog)
    {
    }
}