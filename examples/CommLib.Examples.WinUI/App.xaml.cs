using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.ViewModels;
using CommLib.Examples.WinUI.Views;
using CommLib.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CommLib.Examples.WinUI;

public sealed class App : Microsoft.UI.Xaml.Application
{
    private ServiceProvider? _serviceProvider;
    private Window? _window;

    public App()
    {
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        services.AddCommLibCore();
        services.AddSingleton<IUiDispatcher>(_ =>
            new DispatcherQueueUiDispatcher(
                DispatcherQueue.GetForCurrentThread() ??
                throw new InvalidOperationException("No DispatcherQueue is available.")));
        services.AddSingleton<IDeviceLabSessionService, DeviceLabSessionService>();
        services.AddSingleton<TcpTransportSettingsViewModel>();
        services.AddSingleton<UdpTransportSettingsViewModel>();
        services.AddSingleton<MulticastTransportSettingsViewModel>();
        services.AddSingleton<SerialTransportSettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DeviceLabView>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
        _window = _serviceProvider.GetRequiredService<MainWindow>();
        _window.Activate();
    }
}
