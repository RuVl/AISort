using OnnxPredictors.Labels;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Results;

public class YoloResult : IPredictionResult
{
    public required ILabel Label { get; init; }
    
    public required Rectangle BoundingBox { get; init; }

    public required float Confidence { get; init; }
}