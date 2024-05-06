namespace UAR.OpticalFlow;

public abstract class GenericOpticalFlow<T> where T : class, new()
{
    public readonly T[] FrameBuffer;
    public readonly int Backlog;

    public GenericOpticalFlow(int frameBacklog = 2)
    {
        Backlog = frameBacklog;
        FrameBuffer = new T[frameBacklog];
        
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
    
}