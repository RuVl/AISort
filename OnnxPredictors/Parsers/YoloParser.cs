using System.Collections.Concurrent;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OnnxPredictors.Inputs;
using OnnxPredictors.Labels;
using OnnxPredictors.Predictors;
using OnnxPredictors.Results;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Parsers;

public class YoloParser(ILabel[] labels, Size imageSize, Size modelSize) : IPredictionParser
{
    public string Name { get; init; } = null;

    public float MinConfidence { get; init; } = 0.20f;

    public IPredictionResult[] Parse(NamedOnnxValue outputOnnxValue)
    {
        if (Name != null && outputOnnxValue.Name != Name)
            throw new ArgumentException($"{outputOnnxValue.Name} is not equals output's name", nameof(outputOnnxValue));

        if (outputOnnxValue.ValueType != OnnxValueType.ONNX_TYPE_TENSOR)
            throw new ArgumentException($"Only tensors are supported for {outputOnnxValue.Name}", nameof(outputOnnxValue));

        var output = (DenseTensor<float>)outputOnnxValue.Value;

        var result = new ConcurrentBag<IPredictionResult>();

        // For each batch
        Parallel.For(0, output.Dimensions[0], i =>
        {
            Parallel.For(0, output.Dimensions[2], k =>
            {
                // The first four is rectangle center, width and height
                (float x0, float y0) = (output[i, 0, k], output[i, 1, k]);
                (float w, float h) = (output[i, 2, k], output[i, 3, k]);

                var outputBox = new RectangleF(x0 - w / 2, y0 - h / 2, w, h);
                var box = (Rectangle)Utils.ScaleBox(outputBox, modelSize, imageSize);

                // The last is confidence for each label
                for (var j = 4; j < output.Dimensions[1]; j++)
                {
                    float confidence = output[i, j, k];

                    // Skip low confidence values
                    if (confidence < MinConfidence) return;

                    result.Add(new YoloResult
                    {
                        Label = labels[j - 4],
                        BoundingBox = box,
                        Confidence = confidence
                    });
                }
            });
        });

        return result.ToArray();
    }

    public static YoloParser Create(YoloPredictor yoloPredictor, OneImageInput imageInput)
    {
        return new YoloParser(yoloPredictor.Labels, imageInput.InputImage.Size, yoloPredictor.InputSize);
    }
}