using CommLib.Examples.WinUI.ViewModels;
using CommLib.Examples.WinUI.Views;
using Microsoft.UI.Xaml;

namespace CommLib.Examples.WinUI;

public sealed class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, DeviceLabView deviceLabView)
    {
        ViewModel = viewModel;
        Title = "CommLib Device Lab";
        Content = deviceLabView;
        Closed += OnClosed;
    }

    public MainViewModel ViewModel { get; }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await ViewModel.DisposeAsync().ConfigureAwait(false);
    }
}
