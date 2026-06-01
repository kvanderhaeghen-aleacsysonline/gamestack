using Avalonia.Controls;
using Avalonia.Input;
using Gamestack.App.ViewModels;

namespace Gamestack.App.Views;

public partial class ExplorerView : UserControl
{
    public ExplorerView() => InitializeComponent();

    // Double-click a folder to open it; double-click a file to download it.
    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ExplorerViewModel vm || vm.SelectedItem is not { } item)
            return;

        if (item.IsFolder)
            vm.OpenCommand.Execute(item);
        else
            vm.DownloadCommand.Execute(item);
    }
}
