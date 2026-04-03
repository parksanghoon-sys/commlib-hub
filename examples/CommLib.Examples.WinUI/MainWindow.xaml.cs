using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.ViewModels;
using CommLib.Examples.WinUI.Views;
using Microsoft.UI.Xaml;

namespace CommLib.Examples.WinUI;

public sealed class MainWindow : Window
{
    // MainWindow는 실제 UI 조합 로직을 가지지 않고, 창 제목과 종료 시 정리처럼
    // "데스크톱 창 수명주기" 성격의 책임만 들고 있도록 얇게 유지한다.
    private readonly IAppLocalizer _localizer;
    private readonly IDeviceLabSettingsStore _settingsStore;
    private readonly DeviceLabSettingsViewModel _settings;

    public MainWindow(
        MainViewModel viewModel,
        DeviceLabSettingsViewModel settings,
        IAppLocalizer localizer,
        IDeviceLabSettingsStore settingsStore,
        AppShellView appShellView)
    {
        ViewModel = viewModel;
        _settings = settings;
        _localizer = localizer;
        _settingsStore = settingsStore;
        Content = appShellView;
        ApplyLocalizedText();
        _localizer.LanguageChanged += OnLanguageChanged;
        Closed += OnClosed;
    }

    public MainViewModel ViewModel { get; }

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        ApplyLocalizedText();
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        _localizer.LanguageChanged -= OnLanguageChanged;

        try
        {
            // 마지막 세션에서 바꾼 설정을 별도 저장 버튼 없이도 최대한 보존하되,
            // 종료 경로 자체를 막아서는 안 되므로 저장 실패는 조용히 삼킨다.
            await _settingsStore.SaveAsync(_settings.CreateSnapshot()).ConfigureAwait(false);
        }
        catch
        {
        }

        await ViewModel.DisposeAsync().ConfigureAwait(false);
    }

    private void ApplyLocalizedText()
    {
        Title = _localizer.Get("window.title");
    }
}
