using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OnnxPredictors.Inputs;
using OnnxPredictors.Labels;
using OnnxPredictors.Predictors;
using OnnxPredictors.Results;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Parsers;

public class YoloParser(ILabel[] labels, List<Size> imageSize, Size modelSize) : IPredictionParser
{
    public string Name { get; init; } = null;

    public float MinConfidence { get; init; } = 0.20f;

    public IPredictionResult[] Parse(NamedOnnxValue outputOnnxValue)
    {
        if (Name != null && outputOnnxValue.Name != Name)
        {
            throw new ArgumentException($"{outputOnnxValue.Name} is not equals output's name", nameof(outputOnnxValue));
        }

        if (outputOnnxValue.ValueType != OnnxValueType.ONNX_TYPE_TENSOR)
        {
            throw new ArgumentException($"Only tensors are supported for {outputOnnxValue.Name}", nameof(outputOnnxValue));
        }

        var output = (Tensor<float>)outputOnnxValue.Value;
        
        var result = new ConcurrentBag<IPredictionResult>();

        Parallel.For(0, output.Dimensions[0], i =>
        {
            for (var k = 0; k < output.Dimensions[2]; k++)
            {
                // The first four is rectangle center, width and height
                (float x0, float y0) = (output[i, 0, k], output[i, 1, k]);
                (float w, float h) = (output[i, 2, k], output[i, 3, k]);

                var outputBox = new RectangleF(x0 - w / 2, y0 - h / 2, w, h);
                var box = Utils.ScaleBoxFromBoxPad(outputBox, modelSize, imageSize[i]);
                
                // The last is confidence for each label
                for (var j = 4; j < output.Dimensions[1]; j++)
                {
                    float confidence = output[i, j, k];

                    // Skip low confidence values
                    if (confidence < MinConfidence) continue;
                    
                    result.Add(new YoloResult
                    {
                        PredictionId = i,
                        Label = labels[j - 4],
                        BoundingBox = box,
                        Confidence = confidence
                    });
                }
            }
        });

        return result.ToArray();
    }

    public IPredictionResult[] Parse(OrtValue outputOrtValue)
    {
        if (!outputOrtValue.IsTensor)
        {
            throw new ArgumentException("Only tensors are supported", nameof(outputOrtValue));
        }

        var memoryInfo = outputOrtValue.GetTensorMemoryInfo();
        if (memoryInfo.IsClosed || memoryInfo.IsInvalid)
        {
            throw new ChannelClosedException("Memory is closed or invalid");
        }

        var typeAndShape = outputOrtValue.GetTensorTypeAndShape();
        if (typeAndShape.Shape is not [long batch, long borderAndConfidence, long anchors])
        {
            throw new ArgumentException("Tensor's dimensions count must be 3", nameof(outputOrtValue));
        }

        var tensorDataAsSpan = outputOrtValue.GetTensorDataAsSpan<float>();

        var memory = tensorDataAsSpan.ToArray().AsMemory();
        var dimensions = Array.ConvertAll(typeAndShape.Shape, s => (int)s).ToArray().AsSpan();
        
        // ReSharper disable once CollectionNeverUpdated.Local
        var output = new DenseTensor<float>(memory, dimensions);

        var result = new ConcurrentBag<IPredictionResult>();

        Parallel.For(0, (int)batch, i =>
        {
            for (var k = 0; k < anchors; k++)
            {
                // The first four is rectangle center, width and height
                (float x0, float y0) = (output[i, 0, k], output[i, 1, k]);
                (float w, float h) = (output[i, 2, k], output[i, 3, k]);

                var outputBox = new RectangleF(x0 - w / 2, y0 - h / 2, w, h);
                var box = Utils.ScaleBoxFromBoxPad(outputBox, modelSize, imageSize[i]);

                // The last is confidence for each label
                for (var j = 4; j < borderAndConfidence; j++)
                {
                    float confidence = output[i, j, k];

                    // Skip low confidence values
                    if (confidence < MinConfidence) continue;

                    result.Add(new YoloResult
                    {
                        PredictionId = i,
                        Label = labels[j - 4],
                        BoundingBox = box,
                        Confidence = confidence
                    });
                }
            }
        });

        return result.ToArray();
    }

    public static YoloParser Create(YoloPredictor predictor, OneImageInput input)
    {
        return new YoloParser(predictor.Labels, [input.OriginalSize], predictor.InputSize);
    }

    public static YoloParser Create(YoloPredictor predictor, BatchImagesInput input)
    {
        return new YoloParser(predictor.Labels, input.OriginalSizes, predictor.InputSize);
    }
}