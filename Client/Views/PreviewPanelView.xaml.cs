using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Client.ViewModels;
using OnnxPredictors.Results;

namespace Client.Views;

public partial class PreviewPanelView : UserControl
{
    private const double MinZoomFactor = 0.075;
    private const double MaxZoomFactor = 10.0;
    private double _initialScale = 1;

    private bool _isDragging;
    private Point _startPoint;

    private ScaleTransform _scaleTransform = new();
    private TranslateTransform _translateTransform = new();

    public TransformGroup ImageTransformGroup { get; } = new();

    public PreviewPanelView()
    {
        InitializeComponent();

        if (DataContext is PreviewPanelViewModel previewPanelVM)
        {
            previewPanelVM.PropertyChanged += PreviewPanelVMOnPropertyChanged;
        }

        ImageTransformGroup.Children = [_scaleTransform, _translateTransform];
    }

    private void PreviewPanelVMOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not PreviewPanelViewModel previewPanelVM) return;
        if (e.PropertyName != nameof(previewPanelVM.PredictionResults)) return;
        if (previewPanelVM.PredictionResults is null) return;

        foreach (var result in previewPanelVM.PredictionResults)
        {
            var box = new Rect(result.BoundingBox.X, result.BoundingBox.Y,
                result.BoundingBox.Width, result.BoundingBox.Height);

            // Create rectangle
            var rect = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Width = box.Width,
                Height = box.Height,
                RenderTransform = new TransformGroup
                {
                    Children =
                    [
                        new TranslateTransform(box.X, box.Y), // Смещение на позицию рамки
                        ImageTransformGroup
                    ]
                }
            };

            DrawingCanvas.Children.Add(rect);

            // Create text
            var textBlock = new TextBlock
            {
                Text = $"{result.Label.Name} ({result.Confidence:P0})",
                Foreground = Brushes.Red,
                RenderTransform = new TransformGroup
                {
                    Children =
                    [
                        new TranslateTransform(box.X, box.Y), // Смещение на позицию текста
                        ImageTransformGroup
                    ]
                }
            };

            DrawingCanvas.Children.Add(textBlock);

            Debug.WriteLine($"Box: {box.X} + {box.Width}, {box.Y} + {box.Height}");
        }
    }

    private void ResetTransform(Canvas canvas, Image image)
    {
        _initialScale = image.Source is not null
            ? Math.Min(canvas.RenderSize.Width / image.Source.Width, canvas.RenderSize.Height / image.Source.Height)
            : 1;

        ImageTransformGroup.Children[0] = _scaleTransform = new ScaleTransform(_initialScale, _initialScale);
        ImageTransformGroup.Children[1] = _translateTransform = new TranslateTransform(0, 0);
    }

    private void OnImageSourceChanged(object sender, DataTransferEventArgs e)
    {
        if (e.Source is not Image image) return;
        if (image.Parent is not Canvas canvas) return;

        canvas.Children.RemoveRange(1, DrawingCanvas.Children.Count - 1);

        if (image.Source is null) return;

        ResetTransform(canvas, image);
        CenterImage(canvas, image);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not Canvas canvas) return;
        if (canvas.Children[0] is not Image image) return;
        if (image.RenderTransform != ImageTransformGroup) return;

        double zoom = e.Delta > 0 ? 1.1 : 0.9;
        var mousePosition = e.GetPosition(image);

        // Применяем новое масштабирование
        double newScaleX = _scaleTransform.ScaleX * zoom;
        double newScaleY = _scaleTransform.ScaleY * zoom;

        // Ограничиваем масштабирование по обеим осям
        double clampedScaleX = Math.Clamp(newScaleX, _initialScale * MinZoomFactor, _initialScale * MaxZoomFactor);
        double clampedScaleY = Math.Clamp(newScaleY, _initialScale * MinZoomFactor, _initialScale * MaxZoomFactor);

        // Используем масштаб, который лучше подходит для сохранения соотношения сторон
        double finalScale = Math.Min(clampedScaleX, clampedScaleY);

        // Пересчитываем смещение, чтобы позиция мыши оставалась на месте
        _translateTransform.X -= mousePosition.X * (finalScale - _scaleTransform.ScaleX);
        _translateTransform.Y -= mousePosition.Y * (finalScale - _scaleTransform.ScaleY);

        // Обновляем масштаб
        _scaleTransform.ScaleX = finalScale;
        _scaleTransform.ScaleY = finalScale;

        CenterImage(canvas, image);

        e.Handled = true;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        _isDragging = true;
        Mouse.Capture(sender as Canvas);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        Mouse.Capture(null);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        if (sender is not Canvas canvas) return;
        if (canvas.Children[0] is not Image image) return;
        if (image.RenderTransform != ImageTransformGroup) return;

        var position = e.GetPosition(this);
        var delta = Point.Subtract(position, _startPoint);

        _startPoint = position;

        _translateTransform.X += delta.X;
        _translateTransform.Y += delta.Y;

        CenterImage(canvas, image);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Canvas canvas) return;
        if (canvas.Children[0] is not Image image) return;
        if (image.RenderTransform != ImageTransformGroup) return;

        CenterImage(canvas, image);
    }

    private void CenterImage(Canvas canvas, Image image)
    {
        if (image.Source is null) return;

        var imageBounds = new Size(image.Source.Width * _scaleTransform.ScaleX, image.Source.Height * _scaleTransform.ScaleY);
        var canvasBounds = canvas.RenderSize;

        _translateTransform.X = canvasBounds.Width < imageBounds.Width
            ? Math.Clamp(_translateTransform.X, canvasBounds.Width - imageBounds.Width, 0)
            : (canvasBounds.Width - imageBounds.Width) / 2f;


        _translateTransform.Y = canvasBounds.Height < imageBounds.Height
            ? Math.Clamp(_translateTransform.Y, canvasBounds.Height - imageBounds.Height, 0)
            : (canvasBounds.Height - imageBounds.Height) / 2f;
    }
}