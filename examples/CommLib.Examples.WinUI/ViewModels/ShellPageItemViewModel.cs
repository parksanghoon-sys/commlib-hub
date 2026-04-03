using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class ShellPageItemViewModel : ObservableObject
{
    private readonly IAppLocalizer _localizer;

    public ShellPageItemViewModel(ShellPageKind kind, IAppLocalizer localizer)
    {
        Kind = kind;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public ShellPageKind Kind { get; }

    public string Label => _localizer.GetShellPageLabel(Kind);

    public string Subtitle => _localizer.GetShellPageSubtitle(Kind);

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Subtitle));
    }
}
