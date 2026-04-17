namespace CommLib.Domain.Configuration;

/// <summary>
/// 단일 장치 엔드포인트에 대한 검증된 런타임 설정을 나타냅니다.
/// </summary>
public sealed class DeviceProfile
{
    /// <summary>
    /// 장치의 고유 식별자를 가져옵니다.
    /// </summary>
    public required string DeviceId { get; init; }
    /// <summary>
    /// 운영자 화면이나 로그에 표시할 이름을 가져옵니다.
    /// </summary>
    public required string DisplayName { get; init; }
    /// <summary>
    /// 부트스트랩 시 장치를 시작해야 하는지 여부를 가져옵니다.
    /// </summary>
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// 장치에 대한 검증된 전송 설정을 가져옵니다.
    /// </summary>
    public required TransportOptions Transport { get; init; }
    /// <summary>
    /// 이 장치의 메시지에 적용할 프로토콜 설정을 가져옵니다.
    /// </summary>
    public required ProtocolOptions Protocol { get; init; }
    /// <summary>
    /// 송신 메시지 인코딩에 사용할 직렬화기 설정을 가져옵니다.
    /// </summary>
    public required SerializerOptions Serializer { get; init; }
    /// <summary>
    /// <c>ConnectAsync()</c> 초기 연결 단계에서만 적용할 재시도 설정을 가져옵니다.
    /// </summary>
    public ReconnectOptions Reconnect { get; init; } = new();
    /// <summary>
    /// 이 장치의 요청/응답 흐름 제어 설정을 가져옵니다.
    /// </summary>
    public RequestResponseOptions RequestResponse { get; init; } = new();
}
