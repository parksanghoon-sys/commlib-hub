using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// ShellViewModel 타입입니다.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;
    /// <summary>
    /// _selectedPage 값을 나타냅니다.
    /// </summary>
    private ShellPageItemViewModel _selectedPage = null!;

    /// <summary>
    /// <see cref="ShellViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
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

    /// <summary>
    /// Main 값을 가져옵니다.
    /// </summary>
    public MainViewModel Main { get; }

    /// <summary>
    /// Settings 값을 가져옵니다.
    /// </summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// Pages 값을 가져옵니다.
    /// </summary>
    public IReadOnlyList<ShellPageItemViewModel> Pages { get; }

    /// <summary>
    /// OpenDeviceLabPageCommand 값을 가져옵니다.
    /// </summary>
    public RelayCommand OpenDeviceLabPageCommand { get; }

    /// <summary>
    /// OpenSettingsPageCommand 값을 가져옵니다.
    /// </summary>
    public RelayCommand OpenSettingsPageCommand { get; }

    /// <summary>
    /// SelectedPage 값을 가져옵니다.
    /// </summary>
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

    /// <summary>
    /// IsDeviceLabSelected 값을 가져옵니다.
    /// </summary>
    public bool IsDeviceLabSelected => SelectedPage.Kind == ShellPageKind.DeviceLab;

    /// <summary>
    /// IsSettingsSelected 값을 가져옵니다.
    /// </summary>
    public bool IsSettingsSelected => SelectedPage.Kind == ShellPageKind.Settings;

    /// <summary>
    /// CurrentPageTitle 값을 가져옵니다.
    /// </summary>
    public string CurrentPageTitle => SelectedPage.Label;

    /// <summary>
    /// CurrentPageSubtitle 값을 가져옵니다.
    /// </summary>
    public string CurrentPageSubtitle => SelectedPage.Subtitle;

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(CurrentPageTitle));
        OnPropertyChanged(nameof(CurrentPageSubtitle));
    }
}