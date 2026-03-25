namespace CommLib.Domain.Transport;

/// <summary>
/// Represents a concrete transport mechanism capable of moving bytes to and from a device.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// Gets the human-readable transport name.
    /// </summary>
    string Name { get; }
}
