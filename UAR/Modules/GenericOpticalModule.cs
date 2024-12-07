using Emgu.CV.Cuda;

namespace UAR.Modules;

public abstract class GenericOpticalModule<T> where T : class, new()
{
    public readonly T[] FrameBuffer;
    public readonly int Backlog;

    public readonly bool IsGpuMat;
    public bool UseFullColor;

    public double OverflowX;
    public double OverflowY;

    internal double _lastAvgX;
    internal double _lastAvgY;

    protected GenericOpticalModule(int frameBacklog = 2)
    {
        Backlog = frameBacklog;
        FrameBuffer = new T[frameBacklog];

        IsGpuMat = typeof(T) == typeof(GpuMat);

        for (int i = 0; i < FrameBuffer.Length; i++)
        {
            FrameBuffer[i] = new T();
        }
    }

    public void AddFrame(T frame)
    {
        for (int i = Backlog - 1; i > 0; i--)
        {
            FrameBuffer[i] = FrameBuffer[i - 1];
        }

        FrameBuffer[0] = frame;
    }

    protected (int x, int y) HandleOverflow(double x, double y)
    {
        int intOverflowX = (int)Math.Floor(OverflowX);
        int intOverflowY = (int)Math.Floor(OverflowY);

        x += intOverflowX;
        y += intOverflowY;

        OverflowX -= intOverflowX;
        OverflowY -= intOverflowY;

        var intAvgX = (int)Math.Floor(x);
        var intAvgY = (int)Math.Floor(y);

        OverflowX += x - intAvgX;
        OverflowY += y - intAvgY;

        return (intAvgX, intAvgY);
    }

    protected bool SameSign(double value1, double value2)
    {
        return value1 > 0 && value2 > 0 || value1 < 0 && value2 < 0;
    }
}
