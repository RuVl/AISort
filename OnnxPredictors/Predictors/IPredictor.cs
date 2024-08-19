using System;
using OnnxPredictors.Inputs;
using OnnxPredictors.Parsers;
using OnnxPredictors.Results;

namespace OnnxPredictors.Predictors;

public interface IPredictor : IDisposable
{
    IPredictionResult[] Predict(IPredictionInput predictionInput, IPredictionParser predictionParser);
}