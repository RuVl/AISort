using OnnxPredictors.Labels;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Results;

public interface IPredictionResult
{
    public int PredictionId { get; }

    public ILabel Label { get; }

    public RectangleF BoundingBox { get; }

    public float Confidence { get; }
}