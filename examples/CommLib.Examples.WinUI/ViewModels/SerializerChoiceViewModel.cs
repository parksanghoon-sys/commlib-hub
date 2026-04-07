using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class SerializerChoiceViewModel : ObservableObject
{
    private readonly IAppLocalizer _localizer;

    public SerializerChoiceViewModel(string type, IAppLocalizer localizer)
    {
        Type = type;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public string Type { get; }

    public string Label => _localizer.GetSerializerLabel(Type);

    public string Subtitle => _localizer.GetSerializerSubtitle(Type);

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Subtitle));
    }
}
