using CommLib.Domain.Configuration;

namespace CommLib.Domain.Transport;

/// <summary>
/// Creates transport instances from validated transport configuration.
/// </summary>
public interface ITransportFactory
{
    /// <summary>
    /// Creates a transport implementation for the specified options.
    /// </summary>
    /// <param name="options">The validated transport options.</param>
    /// <returns>A transport implementation matching the options type.</returns>
    ITransport Create(TransportOptions options);
}
