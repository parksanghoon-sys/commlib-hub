using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.ViewModels;
using CommLib.Examples.WinUI.Views;
using Microsoft.UI.Xaml;

namespace CommLib.Examples.WinUI;

public sealed class MainWindow : Window
{
    private readonly IDeviceLabSettingsStore _settingsStore;
    private readonly DeviceLabSettingsViewModel _settings;

    public MainWindow(
        MainViewModel viewModel,
        DeviceLabSettingsViewModel settings,
        IDeviceLabSettingsStore settingsStore,
        AppShellView appShellView)
    {
        ViewModel = viewModel;
        _settings = settings;
        _settingsStore = settingsStore;
        Title = "CommLib Device Lab";
        Content = appShellView;
        Closed += OnClosed;
    }

    public MainViewModel ViewModel { get; }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        try
        {
            await _settingsStore.SaveAsync(_settings.CreateSnapshot()).ConfigureAwait(false);
        }
        catch
        {
        }

        await ViewModel.DisposeAsync().ConfigureAwait(false);
    }
}
