using Emgu.CV;
using Emgu.CV.Structure;

namespace UAR.Modules;

public class PixelMatching : GenericOpticalModule<Mat>
{
    private static int KernelWindow = 7;
    private static readonly float MaximumDissimilarity = 0.000f;
    
    public (int x, int y)? FindMovementFromFlow()
    {
        var latest = FrameBuffer[0].ToImage<Bgr, byte>();
        var oldest = FrameBuffer[^1].ToImage<Bgr, byte>();

        var latestData = latest.Data;
        var oldestData = oldest.Data;

        var xMovement = 0;
        var yMovement = 0;
        double divisor = 1;

        for (int x = KernelWindow; x < latest.Rows; x += KernelWindow)
        {
            for (int y = KernelWindow; y < latest.Cols; y += KernelWindow)
            {
                var blueOldest = oldestData[x, y, 0];
                var greenOldest = oldestData[x, y, 1];
                var redOldest = oldestData[x, y, 2];
                var percentage = (blueOldest + greenOldest + redOldest) / 765.0;

                var pixel = WindowSearch(latestData, oldest, percentage, x, y);
                if (pixel != null)
                {
                    divisor++;

                    xMovement += (pixel.Value.x - x);
                    yMovement += (pixel.Value.y - y);
                }
            }
        }

        // var deltaX = (int) Math.Ceiling(xMovement / divisor);
        // var deltaY = (int) Math.Ceiling(yMovement / divisor);
        
        var intAvg = HandleOverflow(xMovement / divisor, yMovement / divisor);
        
        return (intAvg.x, intAvg.y);
    }

    public (int x, int y)? WindowSearch(byte[,,] latestData, Image<Bgr, byte> oldest, double percentage, int x, int y)
    {
        for (int xWindow = x; xWindow < x + KernelWindow; xWindow++)
        {
            if (xWindow >= oldest.Rows)
            {
                break;
            }
        
            for (int yWindow = y; yWindow < y + KernelWindow; yWindow++)
            {
                if (yWindow >= oldest.Cols)
                {
                    continue;
                }
        
                var blueLatest = latestData[xWindow, yWindow, 0];
                var greenLatest = latestData[xWindow, yWindow, 1];
                var redLatest = latestData[xWindow, yWindow, 2];
        
                var pe = (blueLatest + greenLatest + redLatest) / 765.0;
                        
                if (pe == percentage || Math.Abs(pe - percentage) < MaximumDissimilarity)
                {
                    return (xWindow, yWindow);
                }
            }
        }

        return null;
    }

    public PixelMatching(int frameBacklog) : base(frameBacklog)
    {
        UseFullColor = true;
    }
}