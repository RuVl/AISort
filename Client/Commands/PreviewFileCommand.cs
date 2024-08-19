using System;
using System.Windows.Input;
using Client.ViewModels;
using OnnxPredictors.Inputs;

namespace Client.Commands;

public class PreviewFileCommand(FileViewModel viewModel) : ICommand
{
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter) => viewModel.SelectedStatusFile is not null && Config.Instance.Predictor is not null;

    public void Execute(object parameter)
    {
        if (Config.Instance.Predictor is null)
            return;
        
        var input = new OneImageInput(viewModel.SelectedStatusFile.Filename);
        viewModel.SelectedStatusFile.PredictionResults = Config.Instance.Predictor.Predict(input, null);
    }
}