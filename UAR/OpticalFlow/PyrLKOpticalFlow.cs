﻿using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Size = System.Drawing.Size;

namespace UAR.OpticalFlow;

public class PyrLkOpticalFlow : GenericOpticalFlow<Mat>
{
    private readonly ORB _detector = new(50);

    private readonly Size _pyrlkSize = new(100, 100);
    private readonly int _pyrlkLevel = 7;
    private readonly MCvTermCriteria _pyrlkCrit = new(0.0);

    public (int x, int y)? FindMovementFromFlow()
    {
        var first = FrameBuffer[0];
        var second = FrameBuffer[^1];

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

        CvInvoke.CalcOpticalFlowPyrLK(second, first, prevCorners, _pyrlkSize, _pyrlkLevel, _pyrlkCrit, out var secondFeatures, out var status, out _,
            LKFlowFlag.Default, 0);
        
        double totalX = 0;
        double totalY = 0;
        double divisor = 0;
        
        for (int i = 0; i < secondFeatures.Length; i++)
        {
            if (status[i] == 0)
            {
                continue;
            }
            
            divisor++;
        
            totalX += secondFeatures[i].X - prevCorners[i].X;
            totalY += secondFeatures[i].Y - prevCorners[i].Y;
        }
        
        var deltaX = (int) Math.Floor(totalX / divisor);
        var deltaY = (int) Math.Round(totalY / divisor);

        return (deltaX, deltaY);
    }

    public PyrLkOpticalFlow(int frameBacklog) : base(frameBacklog)
    {
    }
}