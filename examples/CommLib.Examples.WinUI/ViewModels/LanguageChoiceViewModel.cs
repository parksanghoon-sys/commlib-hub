using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// LanguageChoiceViewModel 타입입니다.
/// </summary>
public sealed class LanguageChoiceViewModel : ObservableObject
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;

    /// <summary>
    /// <see cref="LanguageChoiceViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public LanguageChoiceViewModel(AppLanguageMode mode, IAppLocalizer localizer)
    {
        Mode = mode;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Mode 값을 가져옵니다.
    /// </summary>
    public AppLanguageMode Mode { get; }

    /// <summary>
    /// Label 값을 가져옵니다.
    /// </summary>
    public string Label => _localizer.GetLanguageLabel(Mode);

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
    }
}