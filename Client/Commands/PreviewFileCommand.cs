using System.Windows.Input;
using Client.Properties;
using Client.ViewModels;

namespace Client.Commands;

public class PreviewFileCommand(FileViewModel viewModel) : ICommand
{
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter)
    {
        return viewModel.SelectedStatusFile is not null && Config.Predictor is not null;
    }

    public void Execute(object parameter)
    {
        Logging.DefaultLogger.Info($"Predict preview for file: {viewModel.SelectedStatusFile.FilePath}. " +
                                   $"Use {Settings.Default.AiRunner:G} to run the predictor.");

        viewModel.SelectedStatusFile.Predict(Config.Predictor);
    }
}