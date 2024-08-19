using Microsoft.ML.OnnxRuntime;

namespace OnnxPredictors.Inputs;

public interface IPredictionInput
{
    NamedOnnxValue Parse(string name, NodeMetadata metadata);
}