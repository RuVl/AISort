using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;
using OnnxPredictors.Inputs;
using OnnxPredictors.Labels;
using OnnxPredictors.Parsers;
using OnnxPredictors.Results;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Predictors;

/// <summary>
///     Use Yolo model with one input as image and one output
/// </summary>
public class YoloPredictor : BasePredictor
{
    private const float Overlap = 0.45f;

    protected YoloPredictor(string modelPath, ModelRunner runner, bool debug = false) : base(modelPath, runner, debug)
    {
        // Get model's labels
        string jsonLabels = Session.ModelMetadata.CustomMetadataMap.First(kvp => kvp.Key == "names").Value;

        // Deserialize labels to Dictionary<int, string>
        var labels = JsonConvert.DeserializeObject<Dictionary<int, string>>(jsonLabels) ??
                     throw new ArgumentException("Labels not found", nameof(modelPath));

        // Convert Dictionary<int, string> to YoloLabel[]
        Labels = labels.Select(ILabel (kvp) => new YoloLabel { Id = kvp.Key, Name = kvp.Value }).ToArray();

        // Get input image size
        string jsonImageSize = Session.ModelMetadata.CustomMetadataMap.First(kvp => kvp.Key == "imgsz").Value;

        int[] imgsz = JsonConvert.DeserializeObject<int[]>(jsonImageSize) ??
                      throw new ArgumentException("Input image size not found", nameof(modelPath));

        InputSize = new Size(imgsz[0], imgsz[1]);
    }

    private YoloPredictor()
    {
    }

    public IReadOnlyDictionary<string, NodeMetadata> InputMetadata => Session.InputMetadata;
    public IReadOnlyDictionary<string, NodeMetadata> OutputMetadata => Session.OutputMetadata;

    public Size InputSize { get; protected init; }

    public static YoloPredictor Create(string modelPath, ModelRunner modelRunner = ModelRunner.Cpu, bool debug = false)
    {
        return new YoloPredictor(modelPath, modelRunner, debug);
    }

    public override IPredictionResult[] Predict(IPredictionInput predictionInput, IPredictionParser predictionParser = null)
    {
        predictionParser ??= GetParser(predictionInput);

        var inputs = new List<NamedOnnxValue>(InputMetadata.Count);

        foreach ((string name, var nodeMetadata) in InputMetadata)
        {
            // If width or height is -1
            if (nodeMetadata.Dimensions is [.., int h, int w])
            {
                if (h == -1) nodeMetadata.Dimensions[^2] = InputSize.Height;
                if (w == -1) nodeMetadata.Dimensions[^1] = InputSize.Width;
            }
            
            inputs.Add(predictionInput.Parse2Named(name, nodeMetadata));
        }

        using var outputs = Session.Run(inputs);
        
        var predictions = outputs.SelectMany(predictionParser.Parse).ToArray();

       return Suppress(predictions);
    }

    public override async Task<IPredictionResult[]> PredictAsync(IPredictionInput predictionInput, IPredictionParser predictionParser = null)
    {
        predictionParser ??= GetParser(predictionInput);

        var inputs = new List<OrtValue>(InputMetadata.Count);

        foreach ((string name, var nodeMetadata) in InputMetadata)
            inputs.Add(predictionInput.Parse(name, nodeMetadata));

        try
        {
            var output = AllocateTensors(OutputMetadata);

            var outputs = await Session.RunAsync(
                null,
                InputMetadata.Select(kp => kp.Key).ToImmutableList(),
                inputs,
                OutputMetadata.Select(kp => kp.Key).ToImmutableList(),
                output);

            var predictions = outputs.SelectMany(predictionParser.Parse).ToArray();
            return Suppress(predictions);
        }
        finally
        {
            inputs.ForEach(i => i.Dispose());
        }
    }

    protected IPredictionParser GetParser(IPredictionInput predictionInput)
    {
        return predictionInput switch
        {
            OneImageInput oneImageInput => YoloParser.Create(this, oneImageInput),
            BatchImagesInput batchImagesInput => YoloParser.Create(this, batchImagesInput),
            _ => throw new ArgumentException("Only one image is supported if parser not provided", nameof(predictionInput))
        };
    }

    protected IPredictionResult[] Suppress(IPredictionResult[] predictions)
    {
        return predictions
            .GroupBy(p => p.PredictionId)
            .SelectMany(group =>
                group.Select(prediction =>
                    group.Where(other =>
                        {
                            var intersection = RectangleF.Intersect(prediction.BoundingBox, other.BoundingBox);
                            float intersectionArea = intersection.Width * intersection.Height;

                            if (intersectionArea == 0) return false;

                            float unionArea = prediction.BoundingBox.Width * prediction.BoundingBox.Height +
                                other.BoundingBox.Width * other.BoundingBox.Height - intersectionArea;

                            float overlap = intersectionArea / unionArea;

                            return overlap >= Overlap;
                        })
                        .OrderByDescending(other => other.Confidence)
                        .First()
                ).Distinct()
            ).ToArray();
    }


    public override object Clone()
    {
        return new YoloPredictor
        {
            Session = GetSession(),
            ModelPath = ModelPath,
            Runner = Runner,
            Labels = Labels,
            InputSize = InputSize
        };
    }

    private static ReadOnlyCollection<OrtValue> AllocateTensors(IReadOnlyDictionary<string, NodeMetadata> metadata)
    {
        var values = new List<OrtValue>();

        foreach (var kv in metadata)
        {
            var meta = kv.Value;
            if (!meta.IsTensor)
            {
                throw new ArgumentException("Only tensor input is supported");
            }

            long[] shape = Array.ConvertAll(meta.Dimensions, Convert.ToInt64);
            var ortValue = OrtValue.CreateAllocatedTensorValue(OrtAllocator.DefaultInstance, meta.ElementDataType, shape);
            values.Add(ortValue);
        }

        return values.AsReadOnly();
    }
}