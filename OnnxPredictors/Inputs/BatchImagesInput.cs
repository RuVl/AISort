using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OnnxPredictors.Inputs;

public class BatchImagesInput(string name = null) : IPredictionInput
{
    public string Name { get; } = name;

    private List<Image<Rgb24>> InputImages { get; } = [];

    public List<Size> OriginalSizes { get; } = [];

    public OrtValue Parse(string name, NodeMetadata metadata)
    {
        CheckAndPrepare(name, metadata);
        var tensor = ExtractPixels() as DenseTensor<float>;
        return OrtValue.CreateTensorValueFromMemory(tensor!.Buffer.ToArray(), [InputImages.Count, 3, InputImages[0].Height, InputImages[0].Width]);
    }

    public NamedOnnxValue Parse2Named(string name, NodeMetadata metadata)
    {
        CheckAndPrepare(name, metadata);
        return NamedOnnxValue.CreateFromTensor(name, ExtractPixels());
    }

    private void CheckAndPrepare(string name, NodeMetadata metadata)
    {
        if (Name != null && name != Name) throw new ArgumentException($"{name} is not equals input's name", nameof(name));

        if (InputImages.Count < 1) throw new ArgumentException("Input must have at least one image");
        
        if (metadata.Dimensions is not [.., int modelHeight, int modelWidth])
            throw new ArgumentException("Metadata Dimensions is not acceptable", nameof(metadata));

        foreach (var image in InputImages.Where(image => image.Width != modelWidth || image.Height != modelHeight))
            Utils.ResizeImage(image, modelWidth, modelHeight);
    }

    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    private Tensor<float> ExtractPixels()
    {
        var tensor = new DenseTensor<float>(new[] { InputImages.Count, 3, InputImages[0].Height, InputImages[0].Width });

        for (var i = 0; i < InputImages.Count; i++)
            InputImages[i].ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (var x = 0; x < pixelRow.Length; x++)
                    {
                        ref var pixel = ref pixelRow[x];
                        tensor[i, 0, y, x] = pixel.R / 255.0F; // r
                        tensor[i, 1, y, x] = pixel.G / 255.0F; // g
                        tensor[i, 2, y, x] = pixel.B / 255.0F; // b
                    }
                }
            });

        return tensor;
    }

    public void Add(string filepath)
    {
        var image = Image.Load<Rgb24>(filepath);
        InputImages.Add(image);
        OriginalSizes.Add(image.Size);
    }

    public void Add(ReadOnlySpan<byte> bytes, int width, int height)
    {
        var image = Image.LoadPixelData<Rgb24>(bytes, width, height);
        InputImages.Add(image);
        OriginalSizes.Add(image.Size);
    }

    public void Add<T>(ReadOnlySpan<byte> bytes, int width, int height) where T : unmanaged, IPixel<T>
    {
        using var image = Image.LoadPixelData<T>(bytes, width, height);
        InputImages.Add(image.CloneAs<Rgb24>());
        OriginalSizes.Add(image.Size);
    }

    public void Add(OneImageInput imageInput)
    {
        InputImages.Add(imageInput.InputImage);
        OriginalSizes.Add(imageInput.OriginalSize);
    }
}