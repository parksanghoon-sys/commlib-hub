using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IAppLocalizer _localizer;
    private readonly IDeviceLabSettingsStore _settingsStore;
    private string _statusTitle = string.Empty;
    private string _statusDetail = string.Empty;
    private string _statusTitleKey = "settings.status.ready.title";
    private string _statusDetailKey = "settings.status.ready.detail";
    private object[] _statusTitleArgs = [];
    private object[] _statusDetailArgs = [];
    private string _rawStatusDetail = string.Empty;
    private bool _usesRawStatusDetail;
    private bool _isBusy;

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

    public DeviceLabSettingsViewModel Settings { get; }

    public AsyncRelayCommand SaveSettingsCommand { get; }

    public AsyncRelayCommand ReloadSettingsCommand { get; }

    public RelayCommand ResetDefaultsCommand { get; }

    public string SettingsFilePath => _settingsStore.FilePath;

    public string StatusTitle
    {
        get => _statusTitle;
        set => SetProperty(ref _statusTitle, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        set => SetProperty(ref _statusDetail, value);
    }

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

    private bool CanEditSettings()
    {
        return !IsBusy;
    }

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

    private void ResetDefaults()
    {
        Settings.ResetToDefaults();
        SetLocalizedStatus("settings.status.defaultsRestored.title", "settings.status.defaultsRestored.detail");
    }

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        ApplyLocalizedStatus();
    }

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

    private void SetStatusWithRawDetail(string titleKey, string rawDetail, object[]? titleArgs = null)
    {
        _statusTitleKey = titleKey;
        _statusTitleArgs = titleArgs ?? [];
        _usesRawStatusDetail = true;
        _rawStatusDetail = rawDetail;
        ApplyLocalizedStatus();
    }

    private void ApplyLocalizedStatus()
    {
        StatusTitle = _localizer.Format(_statusTitleKey, _statusTitleArgs);
        StatusDetail = _usesRawStatusDetail
            ? _rawStatusDetail
            : _localizer.Format(_statusDetailKey, _statusDetailArgs);
    }
}
