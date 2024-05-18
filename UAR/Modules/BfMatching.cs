using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Util;

namespace UAR.Modules;

public class BfMatching : GenericOpticalModule<Mat>
{
    private readonly ORB _detector = new(50);
    
    private readonly BFMatcher _bfMatcher = new(DistanceType.Hamming2, true);

    private readonly VectorOfKeyPoint _firstKeyPoints = new();
    private readonly VectorOfKeyPoint _secondKeyPoints = new();

    private readonly Mat _firstDescriptor = new();
    private readonly Mat _secondDescriptor = new();
    
    private readonly VectorOfDMatch _matches = new();

    private readonly float _minimumResponse = 0.0005f;
    private readonly int _maximumDistance = 20;

    public (int x, int y)? FindMovementFromFlow()
    {
        var first = FrameBuffer[0];
        var second = FrameBuffer[^1];

        _detector.DetectAndCompute(first, null, _firstKeyPoints, _firstDescriptor, false);
        _detector.DetectAndCompute(second, null, _secondKeyPoints, _secondDescriptor, false);

        if (_firstKeyPoints.Size == 0 || _secondKeyPoints.Size == 0)
        {
            return null;
        }

        _bfMatcher.Match(_firstDescriptor, _secondDescriptor, _matches);

        double totalX = 0;
        double totalY = 0;
        int divisor = 1;
        
        for (int i = 0; i < _matches.Size; i++)
        {
            var match = _matches[i];

            if (match.Distance > _maximumDistance || _firstKeyPoints[match.QueryIdx].Response < _minimumResponse || _secondKeyPoints[match.TrainIdx].Response < _minimumResponse)
            {
                continue;
            }
            
            var firstP = _firstKeyPoints[match.QueryIdx].Point;
            var secondP = _secondKeyPoints[match.TrainIdx].Point;

            divisor++;
            totalX += secondP.X - firstP.X;
            totalY += secondP.Y - firstP.Y;
        }

        double avgX = totalX / divisor;
        double avgY = totalY / divisor;

        var intAvg = HandleOverflow(avgX, avgY);

        return (-intAvg.x, -intAvg.y);
    }

    public BfMatching(int frameBacklog) : base(frameBacklog)
    {
    }
}