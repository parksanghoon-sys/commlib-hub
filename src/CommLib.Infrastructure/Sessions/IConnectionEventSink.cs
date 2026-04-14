namespace CommLib.Infrastructure.Sessions;

/// <summary>
/// 연결 수명주기 이벤트를 관찰해 로깅이나 메트릭 수집에 연결할 수 있게 합니다.
/// </summary>
public interface IConnectionEventSink
{
    /// <summary>
    /// 연결 시도를 시작할 때 호출됩니다.
    /// </summary>
    /// <param name="deviceId">대상 디바이스 식별자입니다.</param>
    /// <param name="attemptNumber">현재 시도 번호입니다.</param>
    /// <param name="totalAttempts">예정된 전체 시도 횟수입니다.</param>
    void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts);

    /// <summary>
    /// 다음 연결 재시도를 예약할 때 호출됩니다.
    /// </summary>
    /// <param name="deviceId">대상 디바이스 식별자입니다.</param>
    /// <param name="attemptNumber">실패한 시도 번호입니다.</param>
    /// <param name="delay">다음 시도 전 대기 시간입니다.</param>
    /// <param name="exception">실패 원인입니다.</param>
    void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception);

    /// <summary>
    /// 연결이 성공했을 때 호출됩니다.
    /// </summary>
    /// <param name="deviceId">대상 디바이스 식별자입니다.</param>
    /// <param name="attemptNumber">성공한 시도 번호입니다.</param>
    void OnConnectSucceeded(string deviceId, int attemptNumber);

    /// <summary>
    /// 연결 수명주기 작업이 최종 실패했을 때 호출됩니다.
    /// </summary>
    /// <param name="deviceId">대상 디바이스 식별자입니다.</param>
    /// <param name="operation">실패한 작업 이름입니다.</param>
    /// <param name="exception">호출자에게 전달될 예외입니다.</param>
    void OnOperationFailed(string deviceId, string operation, Exception exception);

    /// <summary>
    /// 비요청 inbound queue가 가득 차서 receive pump가 소비자 drain을 기다리기 시작할 때 알립니다.
    /// 기본 구현은 no-op이며, pressure episode를 관측해야 할 때만 override합니다.
    /// </summary>
    /// <param name="deviceId">pressure가 발생한 장치 식별자입니다.</param>
    /// <param name="queueCapacity">현재 장치 inbound queue capacity입니다.</param>
    void OnInboundBackpressure(string deviceId, int queueCapacity)
    {
    }
}

/// <summary>
/// NullConnectionEventSink 타입입니다.
/// </summary>
internal sealed class NullConnectionEventSink : IConnectionEventSink
{
    /// <summary>
    /// Instance 값을 가져옵니다.
    /// </summary>
    public static NullConnectionEventSink Instance { get; } = new();

    /// <summary>
    /// OnConnectAttempt 작업을 수행합니다.
    /// </summary>
    public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
    {
    }

    /// <summary>
    /// OnConnectRetryScheduled 작업을 수행합니다.
    /// </summary>
    public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
    {
    }

    /// <summary>
    /// OnConnectSucceeded 작업을 수행합니다.
    /// </summary>
    public void OnConnectSucceeded(string deviceId, int attemptNumber)
    {
    }

    /// <summary>
    /// OnOperationFailed 작업을 수행합니다.
    /// </summary>
    public void OnOperationFailed(string deviceId, string operation, Exception exception)
    {
    }
}
