namespace CommLib.Domain.Messaging;

/// <summary>
/// 메시지 큐잉 또는 전송의 비동기 결과를 나타냅니다.
/// </summary>
public interface ISendResult
{
    /// <summary>
    /// 메시지가 전송 대상으로 수락되면 완료되는 작업을 가져옵니다.
    /// </summary>
    Task SendCompletedTask { get; }
}

/// <summary>
/// 형식화된 응답을 기대하는 요청의 비동기 결과를 나타냅니다.
/// </summary>
/// <typeparam name="TResponse">기대하는 응답 메시지 형식입니다.</typeparam>
public interface ISendResult<TResponse> : ISendResult
{
    /// <summary>
    /// 형식화된 응답이 도착하면 완료되는 작업을 가져옵니다.
    /// </summary>
    Task<TResponse> ResponseTask { get; }
}
