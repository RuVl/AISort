using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OnnxPredictors.Inputs;

public class OneImageInput : IPredictionInput
{
    private OneImageInput(Image<Rgb24> inputImage, string name)
    {
        Name = name;
        InputImage = inputImage;
    }

    public string Name { get; }
    public Image<Rgb24> InputImage { get; }

    public NamedOnnxValue Parse(string name, NodeMetadata metadata)
    {
        if (Name != null && name != Name) throw new ArgumentException($"{name} is not equals input's name", nameof(name));

        if (metadata.Dimensions is not [.., int modelHeight, int modelWidth])
            throw new ArgumentException("Metadata Dimensions is not acceptable", nameof(metadata));

        var resized = InputImage.Width != modelWidth || InputImage.Height != modelHeight
            ? Utils.ResizeImage(InputImage, modelWidth, modelHeight)
            : InputImage;

        return NamedOnnxValue.CreateFromTensor(name, Utils.ExtractPixels(resized));
    }

    public static OneImageInput FromFilepath(string filepath, string name = null)
    {
        return new OneImageInput(Image.Load<Rgb24>(filepath), name);
    }

    public static OneImageInput FromBytes(ReadOnlySpan<byte> bytes, int width, int height, string name = null)
    {
        return new OneImageInput(Image.LoadPixelData<Rgb24>(bytes, width, height), name);
    }

    public static OneImageInput FromBytes<T>(ReadOnlySpan<byte> bytes, int width, int height, string name = null) where T : unmanaged, IPixel<T>
    {
        using var image = Image.LoadPixelData<T>(bytes, width, height);
        var rgbaImage = image.CloneAs<Rgb24>();
        return new OneImageInput(rgbaImage, name);
    }
}