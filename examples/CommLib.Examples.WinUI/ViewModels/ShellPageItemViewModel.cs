using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// ShellPageItemViewModel 타입입니다.
/// </summary>
public sealed class ShellPageItemViewModel : ObservableObject
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;

    /// <summary>
    /// <see cref="ShellPageItemViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public ShellPageItemViewModel(ShellPageKind kind, IAppLocalizer localizer)
    {
        Kind = kind;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// Kind 값을 가져옵니다.
    /// </summary>
    public ShellPageKind Kind { get; }

    /// <summary>
    /// Label 값을 가져옵니다.
    /// </summary>
    public string Label => _localizer.GetShellPageLabel(Kind);

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public string Subtitle => _localizer.GetShellPageSubtitle(Kind);

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Subtitle));
    }
}