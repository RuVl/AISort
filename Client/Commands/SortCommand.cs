using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Client.Models;
using Client.Properties;
using Client.ViewModels;
using OnnxPredictors.Inputs;

namespace Client.Commands;

public class SortCommand(FileViewModel viewModel) : ICommand
{
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter) => viewModel.StatusFiles.Count > 0 && Config.Instance.Predictor is not null;

    public void Execute(object parameter)
    {
        if (Config.Instance.Predictor is null)
            return;

        // Copy settings for session
        var settings = (SettingsPropertyValueCollection)Settings.Default.PropertyValues.Clone();
        var outputDirectory = (string)settings["OutputDirectory"].PropertyValue;
        settings.SetReadOnly();

        if (parameter is StatusFile parameterStatusFile)
        {
            Process(parameterStatusFile, settings);
            return;
        }

        if (parameter is not null)
        {
            throw new ArgumentException("Invalid type of parameter", nameof(parameter));
        }

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        try
        {
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Settings.Default.MaxParallelTasks };

            Parallel.ForEach(viewModel.StatusFiles, parallelOptions, statusFile => Process(statusFile, settings));
        }
        catch (Exception ex)
        {
            Logging.Instance.AppLogger.Error(ex, "Critical error in SortCommand");
            throw;
        }
    }

    private async void Process(StatusFile statusFile, SettingsPropertyValueCollection settings)
    {
        statusFile.Status = ProcessStatus.Processing;

        var input = new OneImageInput(statusFile.Filename);
        var results = statusFile.PredictionResults = Config.Instance.Predictor.Predict(input, null);

        // Change predict status
        if (results.Length == 0)
        {
            statusFile.Status = ProcessStatus.NotFound;
            if (!(bool)settings["CopyIfNotFound"].PropertyValue) return;
        }
        else statusFile.Status = ProcessStatus.Found;

        await Task.Run(() =>
        {
            var rootDirectory = (string)settings["OutputDirectory"].PropertyValue;
            string originalFilename = Path.GetFileName(statusFile.Filename);
            string outputDirectory = rootDirectory;

            // Sort to folders by predicted labels
            if ((bool)settings["SortByLabels"].PropertyValue)
            {
                if (statusFile.Status == ProcessStatus.Found)
                {
                    var uniqueResultsByLabel = statusFile.PredictionResults.GroupBy(r => r.Label.Name).Select(r => r.First());

                    // Copy to all labels folder
                    foreach (var predictionResult in uniqueResultsByLabel)
                    {
                        string label = predictionResult.Label.Name ?? "";
                        string outputDir = Path.Combine(rootDirectory, label);

                        if (!Directory.Exists(outputDir))
                            Directory.CreateDirectory(outputDir);

                        File.Copy(statusFile.Filename, Path.Combine(outputDir, originalFilename), true);
                    }
                    
                    return;
                }

                var notFoundLabel = (string)settings["NotFoundLabel"].PropertyValue;
                outputDirectory = Path.Combine(rootDirectory, notFoundLabel);

                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
            }
            
            File.Copy(statusFile.Filename, Path.Combine(outputDirectory, originalFilename), true);
        });
    }
}