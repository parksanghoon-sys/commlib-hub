namespace CommLib.Domain.Protocol;

/// <summary>
/// Defines the protocol abstraction applied to device message framing.
/// </summary>
public interface IProtocol
{
    /// <summary>
    /// Gets the protocol name.
    /// </summary>
    string Name { get; }
}
