using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// TransportSettingsViewModel 타입입니다.
/// </summary>
public abstract partial class TransportSettingsViewModel : ObservableObject
{
    /// <summary>
    /// Title 값을 가져옵니다.
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public abstract string Subtitle { get; }

    /// <summary>
    /// BuildTransportOptions 작업을 수행합니다.
    /// </summary>
    public abstract TransportOptions BuildTransportOptions();

    /// <summary>
    /// ParseInt 작업을 수행합니다.
    /// </summary>
    protected static int ParseInt(string value, string fieldName)
    {
        if (!int.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"{fieldName} must be an integer.");
        }

        return parsed;
    }

    /// <summary>
    /// ParseOptionalInt 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// NullIfWhiteSpace 작업을 수행합니다.
    /// </summary>
    protected static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}