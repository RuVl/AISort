using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OnnxPredictors.Results;
using Point = System.Windows.Point;

namespace Client;

public static class Utils
{
    public static readonly Typeface BoxTitleTypeface = new("Arial");
    public static readonly SolidColorBrush TextBrush = Brushes.Red;
    public static readonly Pen BoxPen = new(Brushes.Red, 2);

    public static void DrawBoundingBoxes(DrawingContext drawingContext, IEnumerable<IPredictionResult> results)
    {
        foreach (var result in results)
        {
            // Рисуем рамку
            var rect = new Rect(result.BoundingBox.X, result.BoundingBox.Y, result.BoundingBox.Width, result.BoundingBox.Height);
            drawingContext.DrawRectangle(null, BoxPen, rect);

            // Формируем текст
            var label = $"{result.Label.Name} ({result.Confidence:P0})";
            var formattedText = new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                BoxTitleTypeface,
                16,
                TextBrush,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip
            );

            // Позиция текста
            var textPosition = new Point(result.BoundingBox.X, result.BoundingBox.Y - 20);
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
        DrawBoundingBoxes(drawingContext, results);

        return drawingVisual;
    }

    public static RenderTargetBitmap RenderImageWithBoundingBoxes(BitmapImage bitmap, IEnumerable<IPredictionResult> results)
    {
        var drawingVisual = DrawBoundingBoxes(bitmap, results);

        // Преобразуем визуал в RenderTargetBitmap для отображения в WPF
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
        image.Freeze();

        // var bitmap = (BitmapSource)new ImageSourceConverter().ConvertFrom(bgra);

        return RenderImageWithBoundingBoxes(image, results);
    }
}