using Emgu.CV.Cuda;

namespace UAR.OpticalFlow;

public abstract class GenericOpticalFlow<T> where T : class, new()
{
    public readonly T[] FrameBuffer;
    public readonly int Backlog;

    public readonly bool IsGpuMat;

    private double _overflowX;
    private double _overflowY;

    protected GenericOpticalFlow(int frameBacklog = 2)
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
            FrameBuffer[i] = FrameBuffer[i-1];
        }
        
        FrameBuffer[0] = frame;
    }
    
    protected (int x, int y) HandleOverflow(double x, double y)
    {
        int intOverflowX = (int) Math.Floor(_overflowX);
        int intOverflowY = (int) Math.Floor(_overflowY);
    
        x += intOverflowX;
        y += intOverflowY;
    
        _overflowX -= intOverflowX;
        _overflowY -= intOverflowY;
    
        var intAvgX = (int) Math.Floor(x);
        var intAvgY = (int) Math.Floor(y);
    
        _overflowX += x - intAvgX;
        _overflowY += y - intAvgY;
    
        return (intAvgX, intAvgY);
    }
}