using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IDeviceLabSettingsStore _settingsStore;
    private string _statusTitle = "Persistence Ready";
    private string _statusDetail = "Review defaults, then save them to appsettings.json.";
    private bool _isBusy;

    public SettingsViewModel(
        DeviceLabSettingsViewModel settings,
        IDeviceLabSettingsStore settingsStore)
    {
        Settings = settings;
        _settingsStore = settingsStore;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, CanEditSettings);
        ReloadSettingsCommand = new AsyncRelayCommand(ReloadSettingsAsync, CanEditSettings);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults, CanEditSettings);
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
            await _settingsStore.SaveAsync(Settings.CreateSnapshot()).ConfigureAwait(false);
            StatusTitle = "Settings Saved";
            StatusDetail = $"Updated {Path.GetFileName(_settingsStore.FilePath)} at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}.";
        }
        catch (Exception exception)
        {
            StatusTitle = "Save Failed";
            StatusDetail = exception.Message;
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
            var settings = await _settingsStore.LoadAsync().ConfigureAwait(false);
            Settings.Apply(settings);
            StatusTitle = "Settings Reloaded";
            StatusDetail = $"Loaded settings from {Path.GetFileName(_settingsStore.FilePath)}.";
        }
        catch (Exception exception)
        {
            StatusTitle = "Reload Failed";
            StatusDetail = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetDefaults()
    {
        Settings.ResetToDefaults();
        StatusTitle = "Defaults Restored";
        StatusDetail = "The in-memory defaults are back. Save to persist them to appsettings.json.";
    }
}
