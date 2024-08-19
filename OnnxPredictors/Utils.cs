using System.Diagnostics.CodeAnalysis;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OnnxPredictors;

public static class Utils
{
    public static Image ResizeImage(Image image, int targetWidth, int targetHeight)
    {
        return image.Clone(x => x.Resize(targetWidth, targetHeight));
    }

    public static Tensor<float> ExtractPixels(Image image)
    {
        var tensor = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

        using var img = image.CloneAs<Rgb24>();
        for (var y = 0; y < img.Height; y++)
        {
            var pixelSpan = img.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < img.Width; x++)
            {
                tensor[0, 0, y, x] = pixelSpan[x].R / 255.0F; // r
                tensor[0, 1, y, x] = pixelSpan[x].G / 255.0F; // g
                tensor[0, 2, y, x] = pixelSpan[x].B / 255.0F; // b
            }
        }

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