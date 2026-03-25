namespace CommLib.Domain.Messaging;

public interface ISendResult
{
    Task SendCompletedTask { get; }
}

public interface ISendResult<TResponse> : ISendResult
{
    Task<TResponse> ResponseTask { get; }
}
