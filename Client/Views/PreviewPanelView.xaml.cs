using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Client.ViewModels;

namespace Client.Views;

public partial class PreviewPanelView : UserControl
{
    private const double MaxScale = 3.0;
    private const double MinScale = 0.2;

    private Point _startPoint;
    private bool _isDragging;

    private ScaleTransform _scaleTransform = new();
    private TranslateTransform _translateTransform = new();

    private readonly TransformGroup _transformGroup;

    public PreviewPanelView()
    {
        InitializeComponent();

        SizeChanged += PreviewPanel_SizeChanged;

        OriginalImage.RenderTransform = ProcessedImage.RenderTransform = _transformGroup = new TransformGroup
        {
            Children = { _scaleTransform, _translateTransform }
        };

        if (DataContext is FileViewModel fileViewModel)
            fileViewModel.PropertyChanged += OriginalImage_OnSourceUpdated;
    }

    private void ResetTransforms()
    {
        _transformGroup.Children[0] = _scaleTransform = new ScaleTransform();
        _transformGroup.Children[1] = _translateTransform = new TranslateTransform();
    }

    private async void OriginalImage_OnSourceUpdated(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not FileViewModel fileViewModel) return;
        if (e.PropertyName != nameof(fileViewModel.SelectedStatusFile)) return;
        
        ProcessedImage.Source = null;
        
        var statusFile = fileViewModel.SelectedStatusFile;
        if (statusFile is null) return;
        
        ResetTransforms();

        if (statusFile.PredictionResults is null)
        {
            if (!fileViewModel.PreviewFile.CanExecute(null)) return;
            await Task.Run(() => fileViewModel.PreviewFile.Execute(null));
        }

        var processedImage = Dispatcher.Invoke(() =>
        {
            string filename = statusFile.Filename;
            var image = Utils.DrawBoundingBoxes(filename, statusFile.PredictionResults);
            return Utils.ConvertImageSharpToImageSource(image);
        });

        ProcessedImage.Source = processedImage;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var canvas = sender as Canvas;
        var image = canvas?.Children[0] as Image;
        if (image?.RenderTransform != _transformGroup) return;

        double zoom = e.Delta > 0 ? .1 : -.1;
        double newScale = _scaleTransform.ScaleX + zoom;

        // Ограничения на масштабирование
        if (newScale is < MinScale or > MaxScale)
            return;

        // Центрирование относительно центра изображения
        var mousePosition = e.GetPosition(image);
        if (_transformGroup.Inverse != null)
        {
            var relative = _transformGroup.Inverse.Transform(mousePosition);

            _scaleTransform.ScaleX = newScale;
            _scaleTransform.ScaleY = newScale;

            var newMousePosition = _transformGroup.Transform(relative);

            _translateTransform.X -= newMousePosition.X - mousePosition.X;
            _translateTransform.Y -= newMousePosition.Y - mousePosition.Y;
        }

        // Ограничить перемещение
        ConstrainImagePosition(canvas, image, _translateTransform, _scaleTransform);
        
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

        if (image.RenderTransform != _transformGroup) return;

        var position = e.GetPosition(this);
        var delta = Point.Subtract(position, _startPoint);

        _startPoint = position;

        _translateTransform.X += delta.X;
        _translateTransform.Y += delta.Y;

        ConstrainImagePosition(canvas, image, _translateTransform, _scaleTransform);
    }

    private void PreviewPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ConstrainImagePosition((Canvas)OriginalImage.Parent, OriginalImage, _translateTransform, _scaleTransform);
    }

    private static void ConstrainImagePosition(Canvas canvas, Image image, TranslateTransform translateTransform,
        ScaleTransform scaleTransform)
    {
        var imageBounds = new Size(image.ActualWidth * scaleTransform.ScaleX,
            image.ActualHeight * scaleTransform.ScaleY);
        var canvasBounds = canvas.RenderSize;

        translateTransform.X = imageBounds.Width > canvasBounds.Width
            ? Math.Min(Math.Max(translateTransform.X, canvasBounds.Width - imageBounds.Width), 0)
            : (canvasBounds.Width - imageBounds.Width) / 2;

        translateTransform.Y = imageBounds.Height > canvasBounds.Height
            ? Math.Min(Math.Max(translateTransform.Y, canvasBounds.Height - imageBounds.Height), 0)
            : (canvasBounds.Height - imageBounds.Height) / 2;
    }
}