using System;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;

namespace OnnxPredictors.Inputs;

public class OneImageInput : IPredictionInput
{
    public string Name { get; }
    public Image InputImage { get; }

    public OneImageInput(Image inputImage, string name = null)
    {
        Name = name;
        InputImage = inputImage;
    }

    public OneImageInput(string filename, string name = null)
    {
        Name = name;
        InputImage = Image.Load(filename);
    }

    public NamedOnnxValue Parse(string name, NodeMetadata metadata)
    {
        if (Name != null && name != Name)
        {
            throw new ArgumentException($"{name} is not equals input's name", nameof(name));
        }

        if (metadata.Dimensions is not [.., int modelHeight, int modelWidth])
        {
            throw new ArgumentException("Metadata Dimensions is not acceptable", nameof(metadata));
        }

        var resized = InputImage.Width != modelWidth || InputImage.Height != modelHeight
            ? Utils.ResizeImage(InputImage, modelWidth, modelHeight)
            : InputImage;
        
        return NamedOnnxValue.CreateFromTensor(name, Utils.ExtractPixels(resized));
    }
}