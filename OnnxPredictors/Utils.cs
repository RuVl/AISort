using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OnnxPredictors;

public static class Utils
{
    public static Image<Rgb24> ResizeImage(Image<Rgb24> image, int targetWidth, int targetHeight)
    {
        return image.Clone(x => x.Resize(targetWidth, targetHeight));
    }

    public static Tensor<float> ExtractPixels(in Image<Rgb24> image)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

        image.ProcessPixelRows(accessor =>
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

    public static RectangleF ScaleBox(RectangleF box, Size from, Size to)
    {
        // Calculate the scaling factors
        float scaleX = (float)to.Width / from.Width;
        float scaleY = (float)to.Height / from.Height;

        // Scale the box's position and size
        float newX = box.X * scaleX;
        float newY = box.Y * scaleY;
        float newWidth = box.Width * scaleX;
        float newHeight = box.Height * scaleY;

        // Return the new scaled RectangleF
        return new RectangleF(newX, newY, newWidth, newHeight);
    }
}