using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using OnnxPredictors.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Client;

public static class Utils
{
    public static readonly Typeface BoxTitleTypeface = new("Arial");
    public static readonly SolidColorBrush TextBrush = Brushes.Red;
    public static readonly Pen BoxPen = new(Brushes.Red, 10);

    public static void DrawBoundingBoxes(DrawingContext drawingContext, IEnumerable<IPredictionResult> results, Size bitmapSize)
    {
        double fontSize = (bitmapSize.Width + bitmapSize.Height) / 60f;
        
        foreach (var result in results)
        {
            // Draw box
            var rect = new Rect(result.BoundingBox.X, result.BoundingBox.Y, result.BoundingBox.Width, result.BoundingBox.Height);
            drawingContext.DrawRectangle(null, BoxPen, rect, new RectAnimation
            {
                From = new Rect(0,0,0,0),      // Начальная ширина
                To = rect,       // Конечная ширина
                Duration = TimeSpan.FromSeconds(2),  // Длительность анимации
                AutoReverse = true,  // Анимация вернется в начальное состояние после завершения
                RepeatBehavior = RepeatBehavior.Forever  // Зацикливание
            }.CreateClock());

            // Create text
            var formattedText = new FormattedText(
                $"{result.Label.Name} ({result.Confidence:P0})",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                BoxTitleTypeface,
                fontSize,
                TextBrush,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip
            );

            // Draw text
            var textPosition = new Point(result.BoundingBox.X + 5, result.BoundingBox.Y + 5);
            drawingContext.DrawText(formattedText, textPosition);
        }
    }

    public static DrawingVisual DrawBoundingBoxes(BitmapSource bitmap, IEnumerable<IPredictionResult> results)
    {
        var drawingVisual = new DrawingVisual();
        using var drawingContext = drawingVisual.RenderOpen();

        // Рисуем изображение
        drawingContext.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));

        // Рисуем рамки и текст
        DrawBoundingBoxes(drawingContext, results, new Size(bitmap.PixelWidth, bitmap.PixelHeight));

        return drawingVisual;
    }

    public static RenderTargetBitmap RenderImageWithBoundingBoxes(BitmapImage bitmap, IEnumerable<IPredictionResult> results)
    {
        var drawingVisual = DrawBoundingBoxes(bitmap, results);

        var renderTarget = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, PixelFormats.Pbgra32);
        renderTarget.Render(drawingVisual);

        return renderTarget;
    }

    public static RenderTargetBitmap RenderImageWithBoundingBoxes(string filePath, IEnumerable<IPredictionResult> results)
    {
        var bitmap = new BitmapImage(new Uri(filePath));
        return RenderImageWithBoundingBoxes(bitmap, results);
    }

    public static RenderTargetBitmap RenderImageWithBoundingBoxes(byte[] bytes, IEnumerable<IPredictionResult> results)
    {
        using var ms = new MemoryStream(bytes);
        var image = new BitmapImage();

        image.BeginInit();
        image.StreamSource = ms;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();

        return RenderImageWithBoundingBoxes(image, results);
    }
    
    public static ImageSource ConvertImageSharpToWpfImageSource(Image<Rgb24> imageSharpImage)
    {
        // Создайте MemoryStream
        using var stream = new MemoryStream();
        
        // Сохраните изображение ImageSharp в MemoryStream в формате PNG
        imageSharpImage.SaveAsPng(stream);
        
        // Переместите указатель потока в начало
        stream.Position = 0;

        // Создайте BitmapImage из MemoryStream
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();

        // Возвращайте ImageSource
        return bitmapImage;
    }

}