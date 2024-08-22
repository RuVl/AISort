using System.IO;
using System.Windows.Input;
using Client.Models;
using Client.ViewModels;
using Ookii.Dialogs.Wpf;

namespace Client.Commands;

public class AddFolderCommand(FileViewModel viewModel) : ICommand
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
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Выберите папку",
            Multiselect = false,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != true)
            return;

        foreach (string ext in StatusFile.AllowedExtensions)
        {
            string[] files = Directory.GetFiles(dialog.SelectedPath, ext);
            foreach (string filename in files)
            {
                var sf = new StatusFile(filename);
                if (viewModel.StatusFiles.Contains(sf))
                    continue;

                viewModel.StatusFiles.Add(sf);
            }
        }
    }
}