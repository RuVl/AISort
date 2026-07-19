using Microsoft.ML.OnnxRuntime;

namespace OnnxPredictors.Inputs;

public interface IPredictionInput
{
    OrtValue Parse(string name, NodeMetadata metadata);

    NamedOnnxValue Parse2Named(string name, NodeMetadata metadata);
}