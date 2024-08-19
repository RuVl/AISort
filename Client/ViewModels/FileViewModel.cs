using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Client.Commands;
using Client.Models;

namespace Client.ViewModels;

public class FileViewModel : INotifyPropertyChanged
{
    private StatusFile _selectedStatusFile;

    public StatusFile SelectedStatusFile
    {
        get => _selectedStatusFile;
        set => SetField(ref _selectedStatusFile, value);
    }

    public ObservableCollection<StatusFile> StatusFiles { get; } = [];

    public ICommand AddStatusFile { get; }
    public ICommand AddFolderFiles { get; }
    public ICommand RemoveStatusFile { get; }
    public ICommand ClearStatusFiles { get; }
    public ICommand PreviewFile { get; }
    public ICommand SortFiles { get; }

    public FileViewModel()
    {
        AddStatusFile = new AddFileCommand(this);
        AddFolderFiles = new AddFolderCommand(this);
        
        RemoveStatusFile = new RelayCommand(_ => StatusFiles.Remove(SelectedStatusFile!), _ => SelectedStatusFile != null);
        ClearStatusFiles = new RelayCommand(_ => StatusFiles.Clear(), _ => StatusFiles.Count > 0);

        PreviewFile = new PreviewFileCommand(this);
        SortFiles = new SortCommand(this);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}