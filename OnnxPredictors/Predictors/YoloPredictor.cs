using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Newtonsoft.Json;
using OnnxPredictors.Inputs;
using OnnxPredictors.Labels;
using OnnxPredictors.Parsers;
using OnnxPredictors.Results;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Predictors;

/// <summary>
/// Use Yolo model with one input as image and one output
/// </summary>
public class YoloPredictor : BasePredictor
{
    public IReadOnlyDictionary<string, NodeMetadata> InputMetadata => Session.InputMetadata;
    public IReadOnlyDictionary<string, NodeMetadata> OutputMetadata => Session.OutputMetadata;

    public Size InputSize { get; }
    
    private float Overlap { get; set; } = 0.45f;
    
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

    public static YoloPredictor Create(string modelPath, ModelRunner modelRunner = ModelRunner.Cpu, bool debug = false)
    {
        return new YoloPredictor(modelPath, modelRunner, debug);
    }

    public override IPredictionResult[] Predict(IPredictionInput predictionInput, IPredictionParser predictionParser = null)
    {
        predictionParser ??= predictionInput switch
        {
            OneImageInput oneImageInput => new YoloParser(Labels, oneImageInput.InputImage.Size, InputSize),
            _ => throw new ArgumentException("Only one image is supported if parser not provided", nameof(predictionInput))
        };
        
        var inputs = new List<NamedOnnxValue>(InputMetadata.Count);

        foreach ((string name, var nodeMetadata) in InputMetadata)
        {
            inputs.Add(predictionInput.Parse(name, nodeMetadata));
        }
        
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = Session.Run(inputs);

        var predictions = outputs.SelectMany(predictionParser.Parse).ToArray();

        return Suppress(predictions);
    }
    
    protected IPredictionResult[] Suppress(IPredictionResult[] predictions)
    {
        return predictions.Select(pred1 => predictions.MinBy(pred2 =>
            {
                var (rect1, rect2) = (pred1.BoundingBox, pred2.BoundingBox);

                var intersection = Rectangle.Intersect(rect1, rect2);

                float intArea = intersection.Width * intersection.Height; // intersection area
                float unionArea = rect1.Width * rect1.Height + rect2.Width * rect2.Height - intArea; // union area
                float overlap = intArea / unionArea; // overlap ratio

                return overlap < Overlap ? float.PositiveInfinity : pred2.Confidence;
            })!).Distinct().ToArray();
    }
}