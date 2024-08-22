using System.ComponentModel;
using System.IO;
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
                InitialDirectory = OutputDirectory
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                OutputDirectory = dialog.SelectedPath;
        });
    }

    public string ModelPath
    {
        get => Path.Exists(Settings.Default.ModelPath) ? Path.GetFullPath(Settings.Default.ModelPath) : "";
        set
        {
            if (!Path.Exists(value)) return;

            try
            {
                Config.Predictor = YoloPredictor.Create(value, AiRunner, Settings.Default.EnableLogging);
                SetSetting(value);
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
        set
        {
            try
            {
                Config.Predictor = YoloPredictor.Create(ModelPath, value, Settings.Default.EnableLogging);
                SetSetting(value);
            }
            catch
            {
                // ignored
            }
        }
    }

    public string OutputDirectory
    {
        get => Path.GetFullPath(Settings.Default.OutputDirectory);
        set
        {
            if (!Directory.Exists(value)) return;
            SetSetting(value);
        }
    }

    public bool SortByLabels
    {
        get => Settings.Default.SortByLabels;
        set
        {
            SetSetting(value);
            if (!value) CopyIfNotFound = false;
        }
    }

    public bool CopyIfNotFound
    {
        get => Settings.Default.CopyIfNotFound;
        set => SetSetting(value);
    }

    public int MaxPossibleParallel => Environment.ProcessorCount;

    public int MaxParallelTasks
    {
        get => Settings.Default.MaxParallelTasks;
        set => SetSetting(value);
    }

    public ICommand SelectModelPathCommand { get; }
    public ICommand SelectOutputDirectoryCommand { get; }

    public ModelRunner[] SupportedModels { get; } = Enum.GetValues<ModelRunner>().Where(ModelHelpers.IsRunnerAvailable).ToArray();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetSetting<T>(T value, string settingName = null, [CallerMemberName] string propertyName = null)
    {
        settingName ??= propertyName;
        if (EqualityComparer<T>.Default.Equals((T)Settings.Default[settingName], value)) return false;

        Logging.DefaultLogger.Info($"Update {settingName} property setting to {value}");

        Settings.Default[settingName] = value;
        Settings.Default.Save();

        OnPropertyChanged(propertyName);
        return true;
    }
}