using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Views;

public partial class PreviewPanelView : UserControl
{
    private const double MaxScale = 3.0;
    private const double MinScale = 0.2;
    private double _initialScale = 1;

    private bool _isDragging;

    private ScaleTransform _scaleTransform = new();

    private Point _startPoint;
    private TranslateTransform _translateTransform = new();

    public PreviewPanelView()
    {
        InitializeComponent();

        SizeChanged += Canvas_SizeChanged;

        ImageTransformGroup.Children = [_scaleTransform, _translateTransform];
    }

    public TransformGroup ImageTransformGroup { get; } = new();

    private void ResetTransform(Canvas canvas, Image image)
    {
        _initialScale = image.Source is not null
            ? double.Min(canvas.RenderSize.Width / image.Source.Width, canvas.RenderSize.Height / image.Source.Height)
            : 1;

        ImageTransformGroup.Children[0] = _scaleTransform = new ScaleTransform(_initialScale, _initialScale);
        ImageTransformGroup.Children[1] = _translateTransform = new TranslateTransform(0, 0);
    }

    private void OnImageSourceChanged(object sender, DataTransferEventArgs e)
    {
        if (e.Source is not Image image) return;
        if (image.Parent is not Canvas canvas) return;

        ResetTransform(canvas, image);
        ConstrainImagePosition(canvas, image);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var canvas = sender as Canvas;
        var image = canvas?.Children[0] as Image;
        if (image?.RenderTransform != ImageTransformGroup) return;

        double zoom = e.Delta > 0 ? .1 : -.1;
        double newScale = _scaleTransform.ScaleX + zoom;

        // Ограничения на масштабирование
        if (newScale is < MinScale or > MaxScale)
            return;

        // Центрирование относительно центра изображения
        var mousePosition = e.GetPosition(image);
        if (ImageTransformGroup.Inverse != null)
        {
            var relative = ImageTransformGroup.Inverse.Transform(mousePosition);

            _scaleTransform.ScaleX = newScale;
            _scaleTransform.ScaleY = newScale;

            var newMousePosition = ImageTransformGroup.Transform(relative);

            _translateTransform.X -= newMousePosition.X - mousePosition.X;
            _translateTransform.Y -= newMousePosition.Y - mousePosition.Y;
        }

        // Ограничить перемещение
        ConstrainImagePosition(canvas, image);

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

        ConstrainImagePosition(canvas, image);
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Canvas canvas) return;
        if (canvas.Children[0] is not Image image) return;

        ConstrainImagePosition(canvas, image);
    }

    private void ConstrainImagePosition(Canvas canvas, Image image)
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

    private void Binding_OnTargetUpdated(object sender, DataTransferEventArgs e)
    {
        if (e.Source is not Image image) return;
        if (image.Source is null) return;

        Debug.WriteLine($"Original ({Test.Source.Width}, {Test.Source.Height}), Processed ({image.Source.Width}, {image.Source.Height})");
    }
}