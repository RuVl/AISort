using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
using Client.Commands;
using Client.Properties;
using OnnxPredictors;
using OnnxPredictors.Predictors;
using Ookii.Dialogs.Wpf;

namespace Client.ViewModels;

public class ControlPanelViewModel : INotifyPropertyChanged
{
    public string ModelPath
    {
        get => Path.GetFullPath(Settings.Default.ModelPath);
        set
        {
            if (!Path.Exists(value)) return;
            
            try
            {
                Config.Instance.Predictor = YoloPredictor.Create(value, AiRunner, Settings.Default.EnableLogging);
                SetField(value);
            }
            catch
            {
                // ignored
            }
        }
    }

    public ModelRunner AiRunner
    {
        get => Settings.Default.AiRunner;
        set => SetField(value);
    }

    public string OutputDirectory
    {
        get => Path.GetFullPath(Settings.Default.OutputDirectory);
        set
        {
            if (!Directory.Exists(value)) return;
            SetField(value);
        }
    }

    public bool SortByLabels
    {
        get => Settings.Default.SortByLabels;
        set
        {
            SetField(value);
            if (!value) CopyIfNotFound = false;
        }
    }

    public bool CopyIfNotFound
    {
        get => Settings.Default.CopyIfNotFound;
        set => SetField(value);
    }
    
    public ICommand SelectModelPathCommand { get; }
    public ICommand SelectOutputDirectoryCommand { get; }

    public ModelRunner[] SupportedModels { get; } = Enum.GetValues<ModelRunner>().Where(ModelHelpers.IsRunnerAvailable).ToArray();

    public ControlPanelViewModel()
    {
        SelectModelPathCommand = new RelayCommand(_ =>
        {
            var dialog = new VistaOpenFileDialog
            {
                Title = "Select AI Model Path",
                Multiselect = false,
                CheckFileExists = true,
                Filter = "AI Models Files (*.onnx)|*.onnx"
            };

            if (dialog.ShowDialog() != true)
                return;

            ModelPath = dialog.FileName;
        });
        
        SelectOutputDirectoryCommand = new RelayCommand(_ =>
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select output directory",
                UseDescriptionForTitle = true,
                InitialDirectory = OutputDirectory,
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
                OutputDirectory = dialog.SelectedPath;
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals((T)Settings.Default[propertyName], value)) return false;
        
        Settings.Default[propertyName] = value;
        Settings.Default.Save();
        
        OnPropertyChanged(propertyName);
        return true;
    }
}