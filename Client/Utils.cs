using System.IO;
using OnnxPredictors.Results;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Media = System.Windows.Media;
using Imaging = System.Windows.Media.Imaging;

namespace Client;

public class Utils
{
    public static readonly Font BoxTitleFont = SystemFonts.CreateFont("Arial", 16);
    public static readonly Color TextColor = Color.LightGray;
    public static readonly Pen BoxPen = Pens.Solid(Color.Red, 2);

    public static void DrawBoundingBoxes(Image img, IPredictionResult[] results)
    {
        foreach (var result in results)
        {
            img.Mutate(ctx => ctx.Draw(BoxPen, result.BoundingBox));

            var label = $"{result.Label.Name} ({result.Confidence:P0})";
            var textPosition = new Point(result.BoundingBox.X, result.BoundingBox.Y - 20);
            img.Mutate(ctx => ctx.DrawText(label, BoxTitleFont, TextColor, textPosition));
        }
    }

    public static Image DrawBoundingBoxes(string filePath, IPredictionResult[] results)
    {
        var image = Image.Load<Rgba32>(filePath);
        DrawBoundingBoxes(image, results);
        return image;
    }
    
    public static Media.ImageSource ConvertImageSharpToImageSource(Image imageSharp)
    {
        using var stream = new MemoryStream();
        
        // Сохранение ImageSharp Image в поток в формате PNG
        imageSharp.SaveAsPng(stream);
        stream.Seek(0, SeekOrigin.Begin);

        // Конвертация потока в BitmapImage
        var bitmapImage = new Imaging.BitmapImage();
        
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.CacheOption = Imaging.BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();

        return bitmapImage;
    }
}