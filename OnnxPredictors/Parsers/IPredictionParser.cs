using Microsoft.ML.OnnxRuntime;
using OnnxPredictors.Results;

namespace OnnxPredictors.Parsers;

public interface IPredictionParser
{
    float MinConfidence { get; }

    IPredictionResult[] Parse(NamedOnnxValue outputOnnxValue);
}