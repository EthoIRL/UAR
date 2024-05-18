using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Util;

namespace UAR.Modules;

public class BfMatching : GenericOpticalModule<Mat>
{
    private readonly ORB _detector = new(50);

    private readonly BFMatcher _bfMatcher = new(DistanceType.Hamming2, true);

    private Mat?[]? _descriptorBuffer;
    private VectorOfKeyPoint[]? _keypointBuffer;

    private readonly VectorOfDMatch _matches = new();

    private readonly float _minimumResponse = 0.0005f;
    private readonly int _maximumDistance = 20;

    public (int x, int y)? FindMovementFromFlow()
    {
        _descriptorBuffer ??= new Mat[Backlog];
        _keypointBuffer ??= new VectorOfKeyPoint[Backlog];

        var first = FrameBuffer[0];

        var firstKeyPoints = new VectorOfKeyPoint();
        var firstDescriptor = new Mat();

        _detector.DetectAndCompute(first, null, firstKeyPoints, firstDescriptor, false);

        var secondKeyPoints = _keypointBuffer[^1];
        var secondDescriptor = _descriptorBuffer[^1];

        AddToAndShift(_descriptorBuffer, firstDescriptor);
        AddToAndShift(_keypointBuffer, firstKeyPoints);

        if (secondDescriptor == null || firstKeyPoints.Size == 0 || secondKeyPoints.Size == 0)
        {
            return null;
        }

        _bfMatcher.Match(firstDescriptor, secondDescriptor, _matches);

        double totalX = 0;
        double totalY = 0;
        int divisor = 1;

        for (int i = 0; i < _matches.Size; i++)
        {
            var match = _matches[i];

            if (match.Distance > _maximumDistance || firstKeyPoints[match.QueryIdx].Response < _minimumResponse || secondKeyPoints[match.TrainIdx].Response < _minimumResponse)
            {
                continue;
            }

            var firstP = firstKeyPoints[match.QueryIdx].Point;
            var secondP = secondKeyPoints[match.TrainIdx].Point;

            divisor++;
            totalX += secondP.X - firstP.X;
            totalY += secondP.Y - firstP.Y;
        }

        double avgX = totalX / divisor;
        double avgY = totalY / divisor;

        var intAvg = HandleOverflow(avgX, avgY);

        return (-intAvg.x, -intAvg.y);
    }

    private void AddToAndShift<T>(T[] array, T item)
    {
        for (int i = Backlog - 1; i > 0; i--)
        {
            array[i] = array[i - 1];
        }

        array[0] = item;
    }

    public BfMatching(int frameBacklog) : base(frameBacklog)
    {
    }
}