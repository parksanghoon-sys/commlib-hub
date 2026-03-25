namespace CommLib.Application.Pipeline;

public sealed class PendingRequestStore
{
    private readonly Dictionary<Guid, DateTimeOffset> _pending = new();

    public void Register(Guid correlationId)
    {
        _pending[correlationId] = DateTimeOffset.UtcNow;
    }

    public bool Exists(Guid correlationId)
    {
        return _pending.ContainsKey(correlationId);
    }

    public void Complete(Guid correlationId)
    {
        _pending.Remove(correlationId);
    }
}
