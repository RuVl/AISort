using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Views;

public partial class FileView : UserControl
{
    public FileView()
    {
        InitializeComponent();
    }

    private void DataGrid_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid { SelectedItems.Count: 1 } grid) return;
        if (grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) is DataGridRow { IsMouseOver: false } dgr)
            dgr.IsSelected = false;
    }
}