using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OnnxPredictors.Inputs;

public class OneImageInput : IPredictionInput
{
    private OneImageInput(Image<Rgb24> inputImage, string name)
    {
        Name = name;
        InputImage = inputImage;
        OriginalSize = InputImage.Size;
    }

    public string Name { get; }

    public Size OriginalSize { get; }

    internal Image<Rgb24> InputImage { get; }

    public OrtValue Parse(string name, NodeMetadata metadata)
    {
        CheckAndPrepare(name, metadata);
        var tensor = ExtractPixels() as DenseTensor<float>;
        return OrtValue.CreateTensorValueFromMemory(tensor!.Buffer.ToArray(), [1, 3, InputImage.Height, InputImage.Width]);
    }

    public NamedOnnxValue Parse2Named(string name, NodeMetadata metadata)
    {
        CheckAndPrepare(name, metadata);
        return NamedOnnxValue.CreateFromTensor(name, ExtractPixels());
    }

    private void CheckAndPrepare(string name, NodeMetadata metadata)
    {
        if (Name != null && name != Name)
        {
            throw new ArgumentException($"{name} is not equals input's name", nameof(name));
        }

        if (metadata.Dimensions is not [.., int modelHeight, int modelWidth])
        {
            throw new ArgumentException("Metadata Dimensions is not acceptable", nameof(metadata));
        }

        if (InputImage.Width != modelWidth || InputImage.Height != modelHeight)
        {
            Utils.ResizeImage(InputImage, modelWidth, modelHeight);
        }
    }

    public static OneImageInput FromFilepath(string filepath, string name = null)
    {
        var options = new DecoderOptions
        {
            SkipMetadata = true,
        };
        var image = Image.Load<Rgb24>(options, filepath);

        return new OneImageInput(image, name);
    }

    public static OneImageInput FromBytes(ReadOnlySpan<byte> bytes, int width, int height, string name = null) =>
        new(Image.LoadPixelData<Rgb24>(bytes, width, height), name);

    public static OneImageInput FromBytes<T>(ReadOnlySpan<byte> bytes, int width, int height, string name = null) where T : unmanaged, IPixel<T>
    {
        using var image = Image.LoadPixelData<T>(bytes, width, height);
        var rgbaImage = image.CloneAs<Rgb24>();
        return new OneImageInput(rgbaImage, name);
    }

    private Tensor<float> ExtractPixels()
    {
        var tensor = new DenseTensor<float>([1, 3, InputImage.Height, InputImage.Width]);

        InputImage.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);

                // pixelRow.Length has the same value as accessor.Width,
                // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                for (var x = 0; x < pixelRow.Length; x++)
                {
                    ref var pixel = ref pixelRow[x];
                    tensor[0, 0, y, x] = pixel.R / 255.0F; // r
                    tensor[0, 1, y, x] = pixel.G / 255.0F; // g
                    tensor[0, 2, y, x] = pixel.B / 255.0F; // b
                }
            }
        });

        return tensor;
    }
}