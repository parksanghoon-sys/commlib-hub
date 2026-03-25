namespace CommLib.Domain.Configuration;

/// <summary>
/// 요청/응답 타이밍과 용량 설정을 나타냅니다.
/// </summary>
public sealed class RequestResponseOptions
{
    /// <summary>
    /// 기본 요청 제한 시간(밀리초)을 가져옵니다.
    /// </summary>
    public int DefaultTimeoutMs { get; init; } = 2000;
    /// <summary>
    /// 동시에 대기 상태로 유지할 수 있는 최대 요청 수를 가져옵니다.
    /// </summary>
    public int MaxPendingRequests { get; init; } = 100;
}
