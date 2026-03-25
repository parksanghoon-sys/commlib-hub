namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents the root configuration object for the communication library.
/// </summary>
public sealed class CommLibOptions
{
    /// <summary>
    /// Gets the collection of raw device definitions loaded from configuration.
    /// </summary>
    public List<DeviceProfileRaw> Devices { get; init; } = [];
}
