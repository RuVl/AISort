using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Client.Models;
using Client.Properties;
using Client.ViewModels;

namespace Client.Commands;

public class SortCommand(FileViewModel viewModel, Action<StatusFile> processed = null) : ICommand
{
    private SettingsPropertyValueCollection _settings;
    public static bool IsIdle { get; private set; }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter)
    {
        return viewModel.StatusFiles.Count > 0 && Config.Predictor is not null && !IsIdle;
    }

    public async void Execute(object parameter)
    {
        if (Config.Predictor is null || IsIdle)
            return;

        IsIdle = true;

        // Copy settings for session
        _settings = (SettingsPropertyValueCollection)Settings.Default.PropertyValues.Clone();
        _settings.SetReadOnly();

        var outputDirectory = (string)_settings["OutputDirectory"].PropertyValue;
        var maxParallelTasks = (int)_settings["MaxParallelTasks"].PropertyValue;

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxParallelTasks };

        try
        {
            // Do not block UI
            await Task.Run(() =>
            {
                Parallel.ForEach(viewModel.StatusFiles, parallelOptions, sf =>
                {
                    Process(sf);
                    processed?.Invoke(sf);
                });
            });
        }
        finally
        {
            stopwatch.Stop();
            MessageBox.Show($"Processed {viewModel.ProcessedFiles} files in {stopwatch.ElapsedMilliseconds} ms.\n" +
                            $"Ran with {Config.Predictor.Runner:G} and {maxParallelTasks} max threads", "Result info", MessageBoxButton.OK);
            IsIdle = false;
        }
    }

    private void Process(StatusFile statusFile)
    {
        Logging.DefaultLogger.Info($"Processing file {statusFile.FilePath}");
        statusFile.Process(Config.Predictor);

        if (statusFile.Status == ProcessStatus.NotFound && !(bool)_settings["CopyIfNotFound"].PropertyValue)
            return;

        // Coping according prediction result
        string originalFilename = Path.GetFileName(statusFile.FilePath);
        var outputDirectory = (string)_settings["OutputDirectory"].PropertyValue;

        // Sort to folders by predicted labels
        if ((bool)_settings["SortByLabels"].PropertyValue)
        {
            if (statusFile.Status == ProcessStatus.Found)
            {
                var uniqueResultsByLabel = statusFile.PredictionResults.DistinctBy(result => result.Label.Name);

                // Copy to all labels folder
                foreach (var predictionResult in uniqueResultsByLabel)
                {
                    string label = predictionResult.Label.Name ?? "";
                    string outputLabelDirectory = Path.Combine(outputDirectory, label);

                    if (!Directory.Exists(outputLabelDirectory))
                        Directory.CreateDirectory(outputLabelDirectory);

                    File.Copy(statusFile.FilePath, Path.Combine(outputLabelDirectory, originalFilename), true);
                }

                return;
            }

            var notFoundLabel = (string)_settings["NotFoundLabel"].PropertyValue;
            outputDirectory = Path.Combine(outputDirectory, notFoundLabel);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
        }

        File.Copy(statusFile.FilePath, Path.Combine(outputDirectory, originalFilename), true);
    }
}