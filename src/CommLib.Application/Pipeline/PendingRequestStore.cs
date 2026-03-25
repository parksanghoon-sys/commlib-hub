namespace CommLib.Application.Pipeline;

/// <summary>
/// Tracks request correlation identifiers that are waiting for responses.
/// </summary>
public sealed class PendingRequestStore
{
    /// <summary>
    /// Stores pending request identifiers with their registration timestamp.
    /// </summary>
    private readonly Dictionary<Guid, DateTimeOffset> _pending = new();

    /// <summary>
    /// Registers a correlation identifier as pending.
    /// </summary>
    /// <param name="correlationId">The request correlation identifier to track.</param>
    public void Register(Guid correlationId)
    {
        _pending[correlationId] = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Determines whether the specified correlation identifier is currently pending.
    /// </summary>
    /// <param name="correlationId">The request correlation identifier to check.</param>
    /// <returns><see langword="true"/> if the request is pending; otherwise, <see langword="false"/>.</returns>
    public bool Exists(Guid correlationId)
    {
        return _pending.ContainsKey(correlationId);
    }

    /// <summary>
    /// Removes a correlation identifier from the pending set.
    /// </summary>
    /// <param name="correlationId">The request correlation identifier to complete.</param>
    public void Complete(Guid correlationId)
    {
        _pending.Remove(correlationId);
    }
}
