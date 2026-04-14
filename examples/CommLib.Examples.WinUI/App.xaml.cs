using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.Styles;
using CommLib.Examples.WinUI.ViewModels;
using CommLib.Examples.WinUI.Views;
using CommLib.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CommLib.Examples.WinUI;

/// <summary>
/// App 타입입니다.
/// </summary>
public sealed partial class App : Microsoft.UI.Xaml.Application
{
    /// <summary>
    /// _serviceProvider 값을 나타냅니다.
    /// </summary>
    private ServiceProvider? _serviceProvider;
    /// <summary>
    /// _window 값을 나타냅니다.
    /// </summary>
    private Window? _window;

    /// <summary>
    /// <see cref="App"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// OnLaunched 작업을 수행합니다.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 이 예제는 App에서만 DI 그래프를 만들고, 이후 화면/서비스는 모두 컨테이너에서 꺼내 쓰는 단순한 구조를 유지한다.
        // WinUI 예제에서 Application.Resources를 너무 이른 시점에 건드리면 런타임이 불안정해질 수 있어서
        // 여기서는 서비스 구성과 설정 초기화만 처리하고 실제 리소스 접근은 각 View가 늦게 수행하게 둔다.
        var services = new ServiceCollection();
        services.AddCommLibCore();
        services.AddSingleton<IAppLocalizer, AppLocalizer>();
        services.AddSingleton<IUiDispatcher>(_ =>
            new DispatcherQueueUiDispatcher(
                DispatcherQueue.GetForCurrentThread() ??
                throw new InvalidOperationException("No DispatcherQueue is available.")));
        services.AddSingleton<IDeviceLabSettingsStore, JsonDeviceLabSettingsStore>();
        services.AddSingleton<IDeviceLabSessionService, DeviceLabSessionService>();
        services.AddSingleton<ILocalMockEndpointService, LocalMockEndpointService>();
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

    /// <summary>
    /// InitializeSettings 작업을 수행합니다.
    /// </summary>
    private static void InitializeSettings(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<DeviceLabSettingsViewModel>();
        var settingsStore = serviceProvider.GetRequiredService<IDeviceLabSettingsStore>();

        try
        {
            // 런타임 설정 파일이 없거나 일부 값이 비어 있어도 앱이 즉시 실행 가능해야 하므로
            // 먼저 저장본을 적용하고, 정상 로드가 끝나면 현재 스냅샷을 다시 저장해 출력 폴더의 설정 형식을 정규화한다.
            var snapshot = settingsStore.LoadAsync().GetAwaiter().GetResult();
            settings.Apply(snapshot);
            settingsStore.SaveAsync(settings.CreateSnapshot()).GetAwaiter().GetResult();
        }
        catch
        {
            // 설정 파일 문제로 앱이 뜨지 않는 상황을 피하기 위해 초기 부팅은 항상 기본값으로 복구 가능해야 한다.
            settings.ResetToDefaults();
        }
    }
}