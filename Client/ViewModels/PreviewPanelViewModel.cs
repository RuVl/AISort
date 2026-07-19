using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Client.Models;
using OnnxPredictors.Results;

namespace Client.ViewModels;

public class PreviewPanelViewModel : INotifyPropertyChanged
{
    private readonly FileViewModel _fileViewModel;

    public FileViewModel FileViewModel
    {
        get => _fileViewModel;
        init
        {
            _fileViewModel = value;
            _fileViewModel.PropertyChanged += FileVMUpdate;
        }
    }

    public BitmapSource SelectedImage
    {
        get
        {
            if (_fileViewModel.SelectedStatusFile?.Type != FileType.Image) return null;

            // Fix DPI problem
            // https://www.codeproject.com/Articles/5360403/How-to-Make-WPF-Behave-like-Windows-when-Dealing-w
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(_fileViewModel.SelectedStatusFile.FilePath);
            bitmapImage.EndInit();
            
            var dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
            
            if (Math.Abs(bitmapImage.DpiX - dpi.PixelsPerInchX) < .01 && Math.Abs(bitmapImage.DpiY - dpi.PixelsPerInchY) < .01)
                return bitmapImage;
            
            var pf = PixelFormats.Bgr32;
            int rawStride = (bitmapImage.PixelWidth * pf.BitsPerPixel + 7) / 8;
            var rawImage = new byte[rawStride * bitmapImage.PixelHeight];
            
            bitmapImage.CopyPixels(rawImage, rawStride, 0);
            
            return BitmapSource.Create(
                bitmapImage.PixelWidth, bitmapImage.PixelHeight,
                dpi.PixelsPerInchX, dpi.PixelsPerInchY,
                pf, null, rawImage, rawStride
            );
        }
    }

    public ObservableCollection<IPredictionResult> PredictionResults => _fileViewModel.SelectedStatusFile.PredictionResults;

    public event PropertyChangedEventHandler PropertyChanged;

    private async void FileVMUpdate(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not FileViewModel fileViewModel) return;
        if (e.PropertyName != nameof(fileViewModel.SelectedStatusFile)) return;

        OnPropertyChanged(nameof(SelectedImage));

        var statusFile = fileViewModel.SelectedStatusFile;
        if (statusFile is null) return;

        if (!fileViewModel.PreviewFile.CanExecute(null)) return;
        await Task.Run(() => fileViewModel.PreviewFile.Execute(null));

        OnPropertyChanged(nameof(PredictionResults));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}