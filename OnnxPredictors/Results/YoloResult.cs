using OnnxPredictors.Labels;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Results;

public class YoloResult : IPredictionResult
{
    public int PredictionId { get; init; } = 0;

    public required ILabel Label { get; init; }

    public required RectangleF BoundingBox { get; init; }

    public required float Confidence { get; init; }
}