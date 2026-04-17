namespace CommLib.Domain.Configuration;

/// <summary>
/// <c>ConnectionManager.ConnectAsync()</c> 중 transport-open 재시도 정책을 나타냅니다.
/// </summary>
public sealed class ReconnectOptions
{
    /// <summary>
    /// 연결 재시도 전략 이름을 가져옵니다.
    /// </summary>
    public string Type { get; init; } = "None";
    /// <summary>
    /// 최초 transport-open 시도 이후 추가 재시도 횟수를 가져옵니다.
    /// </summary>
    public int MaxAttempts { get; init; }
    /// <summary>
    /// 백오프 기반 전략에서 사용하는 기본 지연 시간(밀리초)을 가져옵니다.
    /// </summary>
    public int BaseDelayMs { get; init; } = 500;
    /// <summary>
    /// 재시도 백오프의 최대 지연 시간(밀리초)을 가져옵니다.
    /// </summary>
    public int MaxDelayMs { get; init; } = 10000;
    /// <summary>
    /// 간격 기반 전략에서 사용하는 고정 연결 재시도 간격(밀리초)을 가져옵니다.
    /// </summary>
    public int IntervalMs { get; init; } = 3000;
}
