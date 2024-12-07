using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace UAR.Modules;

public class MotionModule: GenericOpticalModule<Mat>
{
    Matrix<float> matrix = new Matrix<float>(2, 3);

    private (int, int) lastMove = (0, 0);
    
    public (int x, int y)? FindMovementFromFlow()
    {
        var first = FrameBuffer[0];
        var second = FrameBuffer[^1];

        // var x = new Mat();

        // float[,] transMatrixFloat = new float[,]
        // {
        //     { 1, 0, 5 },
        //     { 0, 1, 5 }
        // };
        
        //x
        
        // Console.WriteLine($"{Program._remoteState.X} {Program._remoteState.Y}");
        // matrix.Data[0, 2] = -lastMove.Item1;
        // matrix.Data[1, 2] = -lastMove.Item2;
        //
        // CvInvoke.WarpAffine(second, second, matrix, new Size(second.Width, second.Height));
        //
        // second = CvInvoke.WarpAffine(second, np.float32([[1, 0, dx], [0, 1, dy]]), (img2.shape[1], img2.shape[0]))
        
        var x = first - second;
        
        // CvInvoke.AbsDiff(first, second, x);
        
        // var neg = first - second;
        
        MCvScalar lowerWhite = new MCvScalar(75, 75, 75);
        MCvScalar upperWhite = new MCvScalar(255, 255, 255);

        CvInvoke.InRange(x, new ScalarArray(lowerWhite), new ScalarArray(upperWhite), x);

        // CvInvoke.BitwiseAnd(x, x, x);
        
        CvInvoke.Imshow("Negative", x);
        CvInvoke.WaitKey(1);

        lastMove = (Program._remoteState.X, Program._remoteState.Y);
        
        return (0, 0);
    }

    public MotionModule(int frameBacklog) : base(frameBacklog)
    {
        matrix.Data[0, 0] = 1;
        matrix.Data[0, 1] = 0;
        
        matrix.Data[1, 0] = 0;
        matrix.Data[1, 1] = 1;
    }
}