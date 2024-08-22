using System.Windows.Input;
using Client.Models;
using Client.ViewModels;
using Ookii.Dialogs.Wpf;

namespace Client.Commands;

public class AddFileCommand(FileViewModel viewModel) : ICommand
{
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter)
    {
        return !SortCommand.IsIdle;
    }

    public void Execute(object parameter)
    {
        string filter = string.Join(';', StatusFile.AllowedExtensions);

        var dialog = new VistaOpenFileDialog
        {
            Title = "Выберите файл",
            Multiselect = false,
            CheckFileExists = true,
            Filter = $"Image Files ({filter})|{filter}"
        };

        if (dialog.ShowDialog() != true)
            return;

        viewModel.StatusFiles.Add(new StatusFile(dialog.FileName));
    }
}