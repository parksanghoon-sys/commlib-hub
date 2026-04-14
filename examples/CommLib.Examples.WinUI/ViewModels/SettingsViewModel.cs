using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// SettingsViewModel 타입입니다.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;
    /// <summary>
    /// _settingsStore 값을 나타냅니다.
    /// </summary>
    private readonly IDeviceLabSettingsStore _settingsStore;
    /// <summary>
    /// _statusTitle 값을 나타냅니다.
    /// </summary>
    private string _statusTitle = string.Empty;
    /// <summary>
    /// _statusDetail 값을 나타냅니다.
    /// </summary>
    private string _statusDetail = string.Empty;
    /// <summary>
    /// _statusTitleKey 값을 나타냅니다.
    /// </summary>
    private string _statusTitleKey = "settings.status.ready.title";
    /// <summary>
    /// _statusDetailKey 값을 나타냅니다.
    /// </summary>
    private string _statusDetailKey = "settings.status.ready.detail";
    /// <summary>
    /// _statusTitleArgs 값을 나타냅니다.
    /// </summary>
    private object[] _statusTitleArgs = [];
    /// <summary>
    /// _statusDetailArgs 값을 나타냅니다.
    /// </summary>
    private object[] _statusDetailArgs = [];
    /// <summary>
    /// _rawStatusDetail 값을 나타냅니다.
    /// </summary>
    private string _rawStatusDetail = string.Empty;
    /// <summary>
    /// _usesRawStatusDetail 값을 나타냅니다.
    /// </summary>
    private bool _usesRawStatusDetail;
    /// <summary>
    /// _isBusy 값을 나타냅니다.
    /// </summary>
    private bool _isBusy;

    /// <summary>
    /// <see cref="SettingsViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public SettingsViewModel(
        DeviceLabSettingsViewModel settings,
        IDeviceLabSettingsStore settingsStore,
        IAppLocalizer localizer)
    {
        Settings = settings;
        _settingsStore = settingsStore;
        _localizer = localizer;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, CanEditSettings);
        ReloadSettingsCommand = new AsyncRelayCommand(ReloadSettingsAsync, CanEditSettings);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults, CanEditSettings);
        ApplyLocalizedStatus();
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Settings 값을 가져옵니다.
    /// </summary>
    public DeviceLabSettingsViewModel Settings { get; }

    /// <summary>
    /// SaveSettingsCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand SaveSettingsCommand { get; }

    /// <summary>
    /// ReloadSettingsCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand ReloadSettingsCommand { get; }

    /// <summary>
    /// ResetDefaultsCommand 값을 가져옵니다.
    /// </summary>
    public RelayCommand ResetDefaultsCommand { get; }

    /// <summary>
    /// SettingsFilePath 값을 가져옵니다.
    /// </summary>
    public string SettingsFilePath => _settingsStore.FilePath;

    /// <summary>
    /// StatusTitle 값을 가져옵니다.
    /// </summary>
    public string StatusTitle
    {
        get => _statusTitle;
        set => SetProperty(ref _statusTitle, value);
    }

    /// <summary>
    /// StatusDetail 값을 가져옵니다.
    /// </summary>
    public string StatusDetail
    {
        get => _statusDetail;
        set => SetProperty(ref _statusDetail, value);
    }

    /// <summary>
    /// IsBusy 값을 가져옵니다.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                SaveSettingsCommand.NotifyCanExecuteChanged();
                ReloadSettingsCommand.NotifyCanExecuteChanged();
                ResetDefaultsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// CanEditSettings 작업을 수행합니다.
    /// </summary>
    private bool CanEditSettings()
    {
        return !IsBusy;
    }

    /// <summary>
    /// SaveSettingsAsync 작업을 수행합니다.
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        IsBusy = true;

        try
        {
            await _settingsStore.SaveAsync(Settings.CreateSnapshot());
            SetLocalizedStatus(
                "settings.status.saved.title",
                "settings.status.saved.detail",
                detailArgs:
                [
                    Path.GetFileName(_settingsStore.FilePath),
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ]);
        }
        catch (Exception exception)
        {
            SetStatusWithRawDetail("settings.status.saveFailed.title", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ReloadSettingsAsync 작업을 수행합니다.
    /// </summary>
    private async Task ReloadSettingsAsync()
    {
        IsBusy = true;

        try
        {
            var settings = await _settingsStore.LoadAsync();
            Settings.Apply(settings);
            SetLocalizedStatus(
                "settings.status.reloaded.title",
                "settings.status.reloaded.detail",
                detailArgs: [Path.GetFileName(_settingsStore.FilePath)]);
        }
        catch (Exception exception)
        {
            SetStatusWithRawDetail("settings.status.reloadFailed.title", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// ResetDefaults 작업을 수행합니다.
    /// </summary>
    private void ResetDefaults()
    {
        Settings.ResetToDefaults();
        SetLocalizedStatus("settings.status.defaultsRestored.title", "settings.status.defaultsRestored.detail");
    }

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        ApplyLocalizedStatus();
    }

    /// <summary>
    /// SetLocalizedStatus 작업을 수행합니다.
    /// </summary>
    private void SetLocalizedStatus(
        string titleKey,
        string detailKey,
        object[]? titleArgs = null,
        object[]? detailArgs = null)
    {
        _statusTitleKey = titleKey;
        _statusDetailKey = detailKey;
        _statusTitleArgs = titleArgs ?? [];
        _statusDetailArgs = detailArgs ?? [];
        _usesRawStatusDetail = false;
        _rawStatusDetail = string.Empty;
        ApplyLocalizedStatus();
    }

    /// <summary>
    /// SetStatusWithRawDetail 작업을 수행합니다.
    /// </summary>
    private void SetStatusWithRawDetail(string titleKey, string rawDetail, object[]? titleArgs = null)
    {
        _statusTitleKey = titleKey;
        _statusTitleArgs = titleArgs ?? [];
        _usesRawStatusDetail = true;
        _rawStatusDetail = rawDetail;
        ApplyLocalizedStatus();
    }

    /// <summary>
    /// ApplyLocalizedStatus 작업을 수행합니다.
    /// </summary>
    private void ApplyLocalizedStatus()
    {
        StatusTitle = _localizer.Format(_statusTitleKey, _statusTitleArgs);
        StatusDetail = _usesRawStatusDetail
            ? _rawStatusDetail
            : _localizer.Format(_statusDetailKey, _statusDetailArgs);
    }
}