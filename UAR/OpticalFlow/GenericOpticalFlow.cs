using CircularBuffer;

namespace UAR.OpticalFlow;

public abstract class GenericOpticalFlow<T> where T : class
{
    public readonly CircularBuffer<T> FrameBuffer;
    public readonly int Backlog;

    public GenericOpticalFlow(int frameBacklog = 2)
    {
        Backlog = frameBacklog;
        FrameBuffer = new CircularBuffer<T>(frameBacklog);
    }

    public void AddFrame(T frame)
    {
        FrameBuffer.PushFront(frame);
    }
}