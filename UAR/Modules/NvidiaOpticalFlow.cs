using Emgu.CV;
using Emgu.CV.Cuda;

namespace UAR.Modules;

public class NvidiaOpticalModule : GenericOpticalModule<GpuMat>
{
    private NvidiaOpticalFlow_2_0? _nvopf;
    
    private GpuMat _result = new();
    private GpuMat _floatMat = new();
    private Mat _flowMat = new();
    private Matrix<float>? _matrixFlow;
    
    public (int x, int y)? FindMovementFromFlow()
    {
        var first = FrameBuffer[0];
        var second = FrameBuffer[^1];

        _nvopf ??= new NvidiaOpticalFlow_2_0(first.Size, NvidiaOpticalFlow_2_0.PerfLevel.Slow, NvidiaOpticalFlow_2_0.OutputVectorGridSize.Size4);
        
        _nvopf.Calc(first, second, _result);
        _nvopf.ConvertToFloat(_result, _floatMat);

        _floatMat.Download(_flowMat);
        
        _matrixFlow ??= new Matrix<float>(_flowMat.Rows, _flowMat.Cols, _flowMat.NumberOfChannels, _flowMat.DataPointer, 0);
        var matrixSplit = _matrixFlow.Split();

        double avgX = matrixSplit[0].Sum / (_flowMat.Rows * _flowMat.Cols);
        double avgY = matrixSplit[1].Sum / (_flowMat.Rows * _flowMat.Cols);

        // if (avgY is < 2.5 and > 0)
        // {
        //      avgY *= -1;
        // }
        
        if (SameSign(avgX, _lastAvgX))
        {
            avgX += _lastAvgX;
        }
        
        if (SameSign(avgY, _lastAvgY))
        {
            avgY += _lastAvgY;
        }
        
        _lastAvgX = (matrixSplit[0].Sum / (_flowMat.Rows * _flowMat.Cols)) * 1.2;
        _lastAvgY = (matrixSplit[0].Sum / (_flowMat.Rows * _flowMat.Cols)) * 1.2;
        
        var intAvg = HandleOverflow(avgX, avgY);

        return (-intAvg.x, -intAvg.y);
    }

    public NvidiaOpticalModule(int frameBacklog) : base(frameBacklog)
    {
    }
}