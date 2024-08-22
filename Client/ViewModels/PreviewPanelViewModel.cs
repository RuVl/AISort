using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using Client.Models;

namespace Client.ViewModels;

public class PreviewPanelViewModel : INotifyPropertyChanged
{
    private readonly FileViewModel _fileViewModel;

    private ImageSource _processedImage;
    public string SelectedImagePath => _fileViewModel.SelectedStatusFile?.Type == FileType.Image ? _fileViewModel.SelectedStatusFile.FilePath : null;

    public ImageSource ProcessedImage
    {
        get => _processedImage;
        private set => SetField(ref _processedImage, value);
    }

    public FileViewModel FileViewModel
    {
        get => _fileViewModel;
        init
        {
            _fileViewModel = value;
            _fileViewModel.PropertyChanged += FileVMUpdate;
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;

    private void FileVMUpdate(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not FileViewModel fileViewModel) return;
        if (e.PropertyName != nameof(fileViewModel.SelectedStatusFile)) return;

        OnPropertyChanged(nameof(SelectedImagePath));

        ProcessedImage = null;

        var statusFile = fileViewModel.SelectedStatusFile;
        if (statusFile is null) return;

        if (statusFile.PredictionResults is null)
        {
            if (!fileViewModel.PreviewFile.CanExecute(null)) return;
            // await Task.Run(() => fileViewModel.PreviewFile.Execute(null));
            fileViewModel.PreviewFile.Execute(null);
        }

        switch (statusFile.Type)
        {
            case FileType.Image:
            {
                var processedImage = Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    string filename = statusFile.FilePath;
                    var image = Utils.RenderImageWithBoundingBoxes(filename, statusFile.PredictionResults);
                    return image;
                });

                ProcessedImage = processedImage;
                break;
            }
            case FileType.Pdf:
            {
                // TODO: Add pdf viewer
                break;
            }
        }
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