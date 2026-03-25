namespace CommLib.Domain.Configuration;

/// <summary>
/// 장치 재연결 시 사용하는 재시도 정책을 나타냅니다.
/// </summary>
public sealed class ReconnectOptions
{
    /// <summary>
    /// 재연결 전략 이름을 가져옵니다.
    /// </summary>
    public string Type { get; init; } = "None";
    /// <summary>
    /// 최대 재연결 시도 횟수를 가져옵니다.
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
    /// 간격 기반 전략에서 사용하는 고정 재연결 간격(밀리초)을 가져옵니다.
    /// </summary>
    public int IntervalMs { get; init; } = 3000;
}
