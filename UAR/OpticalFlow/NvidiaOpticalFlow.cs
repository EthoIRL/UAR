using Emgu.CV;
using Emgu.CV.Cuda;

namespace UAR.OpticalFlow;

public class NvidiaOpticalFlow : GenericOpticalFlow<GpuMat>
{
    private NvidiaOpticalFlow_2_0? _nvopf;
    
    private GpuMat _result = new();
    private GpuMat _floatMat = new();
    private Mat _flowMat = new();
    private Matrix<float>? _matrixFlow;

    private double _overflowX;
    private double _overflowY;
    
    public (int x, int y)? FindMovementFromFlow()
    {
        if (FrameBuffer.Size < Backlog)
        {
            return null;
        }

        GpuMat first = FrameBuffer[0]!;
        GpuMat second = FrameBuffer[Backlog - 1]!;

        if (first == second)
        {
            second.Dispose();
            return null;
        }

        _nvopf ??= new NvidiaOpticalFlow_2_0(first.Size, NvidiaOpticalFlow_2_0.PerfLevel.Slow, NvidiaOpticalFlow_2_0.OutputVectorGridSize.Size4);
        
        _nvopf.Calc(first, second, _result);
        _nvopf.ConvertToFloat(_result, _floatMat);

        _floatMat.Download(_flowMat);
        
        _matrixFlow ??= new Matrix<float>(_flowMat.Rows, _flowMat.Cols, _flowMat.NumberOfChannels, _flowMat.DataPointer, 0);
        var matrixSplit = _matrixFlow.Split();

        double sumX = matrixSplit[0].Sum;
        double sumY = matrixSplit[1].Sum;

        double avgX = sumX / (_flowMat.Rows * _flowMat.Cols);
        double avgY = sumY / (_flowMat.Rows * _flowMat.Cols);

        int intOverflowX = (int) Math.Floor(_overflowX);
        int intOverflowY = (int) Math.Floor(_overflowY);

        avgX += intOverflowX;
        avgY += intOverflowY;

        _overflowX -= intOverflowX;
        _overflowY -= intOverflowY;

        var intAvgX = (int) Math.Floor(avgX);
        var intAvgY = (int) Math.Round(avgY);

        _overflowX += avgX - intAvgX;
        _overflowY += avgY - intAvgY;

        second.Dispose();

        return (-intAvgX, -intAvgY);
    }

    public NvidiaOpticalFlow(int frameBacklog) : base(frameBacklog)
    {
    }
}