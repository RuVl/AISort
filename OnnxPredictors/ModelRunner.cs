namespace OnnxPredictors;

public enum ModelRunner
{
    Cpu,
    Cuda,
    Tensorrt,
    Rocm,
    Tvm
}