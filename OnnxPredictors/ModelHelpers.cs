using Microsoft.ML.OnnxRuntime;

namespace OnnxPredictors;

public class ModelHelpers
{
    public static bool IsRunnerAvailable(ModelRunner runner)
    {
        try
        {
            switch (runner)
            {
                case ModelRunner.Cpu:
                    break;
                case ModelRunner.Cuda:
                    SessionOptions.MakeSessionOptionWithCudaProvider();
                    break;
                case ModelRunner.Tensorrt:
                    SessionOptions.MakeSessionOptionWithTensorrtProvider();
                    break;
                case ModelRunner.Rocm:
                    SessionOptions.MakeSessionOptionWithRocmProvider();
                    break;
                case ModelRunner.Tvm:
                    SessionOptions.MakeSessionOptionWithTvmProvider();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runner), runner, "Runner not found");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}