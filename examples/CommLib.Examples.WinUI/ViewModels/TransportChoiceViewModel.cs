using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed record TransportChoiceViewModel(TransportKind Kind, string Label, string Subtitle);
