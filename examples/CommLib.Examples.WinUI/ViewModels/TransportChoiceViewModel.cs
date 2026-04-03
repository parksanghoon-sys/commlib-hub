using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class TransportChoiceViewModel : ObservableObject
{
    private readonly IAppLocalizer _localizer;

    public TransportChoiceViewModel(TransportKind kind, IAppLocalizer localizer)
    {
        Kind = kind;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public TransportKind Kind { get; }

    public string Label => _localizer.GetTransportLabel(Kind);

    public string Subtitle => _localizer.GetTransportSubtitle(Kind);

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Subtitle));
    }
}
