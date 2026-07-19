using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OnnxPredictors;

public static class Utils
{
    public static void ResizeImage(Image<Rgb24> image, int targetWidth, int targetHeight, ResizeMode mode = ResizeMode.BoxPad)
    {
        var options = new ResizeOptions
        {
            Size = new Size
            {
                Height = targetHeight,
                Width = targetWidth
            },
            TargetRectangle = new Rectangle(0, 0, targetWidth, targetHeight),
            Mode = mode
        };

        image.Mutate(x => x.Resize(options));
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

    public static RectangleF ScaleBoxFromBoxPad(RectangleF box, Size from, Size to)
    {
        // Calculate ratio
        float ratioWidth = (float)to.Width / from.Width;
        float ratioHeight = (float)to.Height / from.Height;
        float ratio = Math.Max(ratioWidth, ratioHeight);

        // Scale by ratio
        float scaledX = box.X * ratio;
        float scaledY = box.Y * ratio;
        float scaledWidth = box.Width * ratio;
        float scaledHeight = box.Height * ratio;

        // Calculate paddings to center the rectangle
        float padX = (to.Width - from.Width * ratio) / 2;
        float padY = (to.Height - from.Height * ratio) / 2;

        scaledX += padX;
        scaledY += padY;

        return new RectangleF(scaledX, scaledY, scaledWidth, scaledHeight);
    }
}