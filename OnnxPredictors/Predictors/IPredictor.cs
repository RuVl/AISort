using OnnxPredictors.Inputs;
using OnnxPredictors.Parsers;
using OnnxPredictors.Results;

namespace OnnxPredictors.Predictors;

public interface IPredictor : IDisposable, ICloneable
{
    ModelRunner Runner { get; }

    IPredictionResult[] Predict(IPredictionInput predictionInput, IPredictionParser predictionParser);
}