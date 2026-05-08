namespace CommLib.Domain.Configuration;

/// <summary>
/// Reconnect 전략을 나타내는 문자열 상수 모음입니다.
/// </summary>
public static class ReconnectTypes
{
    /// <summary>재연결을 시도하지 않습니다.</summary>
    public const string None = "None";

    /// <summary>고정 간격으로 재연결을 시도합니다.</summary>
    public const string Linear = "Linear";

    /// <summary>지수 백오프 방식으로 재연결을 시도합니다.</summary>
    public const string Exponential = "Exponential";

    /// <summary><see cref="Exponential"/>의 별칭입니다.</summary>
    public const string Backoff = "Backoff";

    /// <summary><see cref="Exponential"/>의 별칭입니다.</summary>
    public const string ExponentialBackoff = "ExponentialBackoff";
}
