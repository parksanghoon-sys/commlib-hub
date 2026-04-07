using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

public interface IAppLocalizer
{
    event EventHandler? LanguageChanged;

    AppLanguageMode CurrentLanguage { get; set; }

    string Get(string key);

    string Format(string key, params object?[] args);

    string GetShellPageLabel(ShellPageKind kind);

    string GetShellPageSubtitle(ShellPageKind kind);

    string GetTransportLabel(TransportKind kind);

    string GetTransportSubtitle(TransportKind kind);

    string GetSerializerLabel(string serializerType);

    string GetSerializerSubtitle(string serializerType);

    string GetLanguageLabel(AppLanguageMode mode);
}
