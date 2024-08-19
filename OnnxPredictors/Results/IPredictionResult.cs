using OnnxPredictors.Labels;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Results;

public interface IPredictionResult
{
    public ILabel Label { get; }
    public Rectangle BoundingBox { get; }
    public float Confidence { get; }
}