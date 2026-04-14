using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// SerializerChoiceViewModel 타입입니다.
/// </summary>
public sealed class SerializerChoiceViewModel : ObservableObject
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;

    /// <summary>
    /// <see cref="SerializerChoiceViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public SerializerChoiceViewModel(string type, IAppLocalizer localizer)
    {
        Type = type;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Type 값을 가져옵니다.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Label 값을 가져옵니다.
    /// </summary>
    public string Label => _localizer.GetSerializerLabel(Type);

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public string Subtitle => _localizer.GetSerializerSubtitle(Type);

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Subtitle));
    }
}