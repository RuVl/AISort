using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Client.Models;
using Client.Properties;
using Client.ViewModels;
using OnnxPredictors.Inputs;
using SharpVectors.Dom.Svg;

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

        var processingTime = new Stopwatch();
        var predictingTime = new Stopwatch();
        var sortingTime = new Stopwatch();

        processingTime.Start();

        // var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxParallelTasks };

        try
        {
            // Do not block UI
            await Task.Run(() =>
            {
                if (maxParallelTasks == 1)
                {
                    foreach (var sf in viewModel.StatusFiles)
                    {
                        predictingTime.Start();
                        Process(sf);
                        predictingTime.Stop();

                        sortingTime.Start();
                        Sort(sf);
                        sortingTime.Stop();
                    }
                }
                else
                {
                    predictingTime.Start();
                    
                    // Predict by batch
                    var batch = new List<(StatusFile, OneImageInput)>(maxParallelTasks);
                    foreach (var sf in viewModel.StatusFiles.Where(sf => sf.Status is ProcessStatus.None))
                    {
                        foreach (var input in sf.GetPredictionInputs())
                        {
                            if (batch.Count == maxParallelTasks)
                            {
                                ProcessBatch(batch);
                                batch.Clear();
                            }

                            batch.Add((sf, input));
                        }
                    }

                    // Predict last batch
                    if (batch.Count > 0) ProcessBatch(batch);
                    predictingTime.Stop();

                    sortingTime.Start();
                    
                    // Sort all status files
                    foreach (var sf in viewModel.StatusFiles)
                    {
                        Sort(sf);
                    }

                    sortingTime.Stop();
                }
            });
        }
        finally
        {
            processingTime.Stop();
            MessageBox.Show($"Processed {viewModel.ProcessedFiles} files in {processingTime.ElapsedMilliseconds} ms.\n" +
                            $"Ran with {Config.Predictor.Runner:G} and {maxParallelTasks} max threads\n" +
                            $"Predicted in {predictingTime.ElapsedMilliseconds} ms. Sorted in {sortingTime.ElapsedMilliseconds} ms.",
                "Result info", MessageBoxButton.OK);
            IsIdle = false;
        }
    }

    private void Process(StatusFile statusFile)
    {
        Logging.DefaultLogger.Info($"Processing file {statusFile.FilePath}");
        statusFile.Predict(Config.Predictor);
        processed?.Invoke(statusFile);
    }

    private void ProcessBatch(List<(StatusFile, OneImageInput)> batch)
    {
        Logging.DefaultLogger.Info($"Processing files {string.Join(", ", batch.Select(s => s.Item1.FilePath))}");

        var input = new BatchImagesInput();

        foreach (var (_, oneImageInput) in batch)
        {
            input.Add(oneImageInput);
        }

        var batchResults = Config.Predictor.Predict(input, null).GroupBy(r => r.PredictionId);

        foreach (var batchResult in batchResults)
        {
            var (sf, _) = batch[batchResult.Key];
            sf.PredictionResults ??= [];

            foreach (var result in batchResult)
            {
                sf.PredictionResults.Add(result);
            }

            sf.Status = ProcessStatus.Found;
        }

        // Update none statuses to not found
        batch.FindAll(s => s.Item1.Status == ProcessStatus.None).ForEach(s =>
        {
            s.Item1.PredictionResults = [];
            s.Item1.Status = ProcessStatus.NotFound;
        });

        if (processed is null) return;
        foreach (var files in batch.Select(b => b.Item1).Distinct())
        {
            processed(files);
        }
    }

    private void Sort(StatusFile statusFile)
    {
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