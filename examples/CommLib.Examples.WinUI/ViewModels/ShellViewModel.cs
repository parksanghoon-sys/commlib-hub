using CommLib.Examples.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private ShellPageItemViewModel _selectedPage = null!;

    public ShellViewModel(MainViewModel main, SettingsViewModel settings)
    {
        Main = main;
        Settings = settings;
        Pages =
        [
            new ShellPageItemViewModel(ShellPageKind.DeviceLab, "Device Lab", "Operate live transport sessions"),
            new ShellPageItemViewModel(ShellPageKind.Settings, "Settings", "Edit and persist appsettings.json")
        ];

        _selectedPage = Pages[0];
        OpenDeviceLabPageCommand = new RelayCommand(() => SelectedPage = Pages[0]);
        OpenSettingsPageCommand = new RelayCommand(() => SelectedPage = Pages[1]);
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
}
