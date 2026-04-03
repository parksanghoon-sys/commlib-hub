using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private readonly IAppLocalizer _localizer;
    private ShellPageItemViewModel _selectedPage = null!;

    public ShellViewModel(MainViewModel main, SettingsViewModel settings, IAppLocalizer localizer)
    {
        _localizer = localizer;
        Main = main;
        Settings = settings;
        Pages =
        [
            new ShellPageItemViewModel(ShellPageKind.DeviceLab, _localizer),
            new ShellPageItemViewModel(ShellPageKind.Settings, _localizer)
        ];

        _selectedPage = Pages[0];
        OpenDeviceLabPageCommand = new RelayCommand(() => SelectedPage = Pages[0]);
        OpenSettingsPageCommand = new RelayCommand(() => SelectedPage = Pages[1]);
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public MainViewModel Main { get; }

    public SettingsViewModel Settings { get; }

    public IReadOnlyList<ShellPageItemViewModel> Pages { get; }

    public RelayCommand OpenDeviceLabPageCommand { get; }

    public RelayCommand OpenSettingsPageCommand { get; }

    public ShellPageItemViewModel SelectedPage
    {
        get => _selectedPage;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (SetProperty(ref _selectedPage, value))
            {
                OnPropertyChanged(nameof(IsDeviceLabSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
                OnPropertyChanged(nameof(CurrentPageTitle));
                OnPropertyChanged(nameof(CurrentPageSubtitle));
            }
        }
    }

    public bool IsDeviceLabSelected => SelectedPage.Kind == ShellPageKind.DeviceLab;

    public bool IsSettingsSelected => SelectedPage.Kind == ShellPageKind.Settings;

    public string CurrentPageTitle => SelectedPage.Label;

    public string CurrentPageSubtitle => SelectedPage.Subtitle;

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(CurrentPageTitle));
        OnPropertyChanged(nameof(CurrentPageSubtitle));
    }
}
