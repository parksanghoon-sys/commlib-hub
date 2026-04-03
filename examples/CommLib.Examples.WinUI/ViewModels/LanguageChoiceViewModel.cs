using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class LanguageChoiceViewModel : ObservableObject
{
    private readonly IAppLocalizer _localizer;

    public LanguageChoiceViewModel(AppLanguageMode mode, IAppLocalizer localizer)
    {
        Mode = mode;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public AppLanguageMode Mode { get; }

    public string Label => _localizer.GetLanguageLabel(Mode);

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
    }
}
