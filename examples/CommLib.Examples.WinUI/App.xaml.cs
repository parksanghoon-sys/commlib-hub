using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.ViewModels;
using CommLib.Examples.WinUI.Views;
using CommLib.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CommLib.Examples.WinUI;

public sealed partial class App : Microsoft.UI.Xaml.Application
{
    private ServiceProvider? _serviceProvider;
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        services.AddCommLibCore();
        services.AddSingleton<IUiDispatcher>(_ =>
            new DispatcherQueueUiDispatcher(
                DispatcherQueue.GetForCurrentThread() ??
                throw new InvalidOperationException("No DispatcherQueue is available.")));
        services.AddSingleton<IDeviceLabSettingsStore, JsonDeviceLabSettingsStore>();
        services.AddSingleton<IDeviceLabSessionService, DeviceLabSessionService>();
        services.AddSingleton<TcpTransportSettingsViewModel>();
        services.AddSingleton<UdpTransportSettingsViewModel>();
        services.AddSingleton<MulticastTransportSettingsViewModel>();
        services.AddSingleton<SerialTransportSettingsViewModel>();
        services.AddSingleton<DeviceLabSettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<DeviceLabView>();
        services.AddSingleton<SettingsView>();
        services.AddSingleton<AppShellView>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
        InitializeSettings(_serviceProvider);
        _window = _serviceProvider.GetRequiredService<MainWindow>();
        _window.Activate();
    }

    private static void InitializeSettings(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<DeviceLabSettingsViewModel>();
        var settingsStore = serviceProvider.GetRequiredService<IDeviceLabSettingsStore>();

        try
        {
            var snapshot = settingsStore.LoadAsync().GetAwaiter().GetResult();
            settings.Apply(snapshot);
            settingsStore.SaveAsync(settings.CreateSnapshot()).GetAwaiter().GetResult();
        }
        catch
        {
            settings.ResetToDefaults();
        }
    }
}
