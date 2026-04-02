using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public abstract partial class TransportSettingsViewModel : ObservableObject
{
    public abstract string Title { get; }

    public abstract string Subtitle { get; }

    public abstract TransportOptions BuildTransportOptions();

    protected static int ParseInt(string value, string fieldName)
    {
        if (!int.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"{fieldName} must be an integer.");
        }

        return parsed;
    }

    protected static int? ParseOptionalInt(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"{fieldName} must be an integer.");
        }

        return parsed;
    }

    protected static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
