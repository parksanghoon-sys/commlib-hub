using System.Text.Json;

namespace CommLib.Domain.Configuration;

/// <summary>
/// JSON 입력에서 직접 읽어온 원시 장치 설정을 나타냅니다.
/// </summary>
public sealed class DeviceProfileRaw
{
    /// <summary>
    /// 장치의 고유 식별자를 가져옵니다.
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;
    /// <summary>
    /// 설정에서 제공한 표시 이름을 가져옵니다.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>
    /// 장치가 활성화되어 있는지 여부를 가져옵니다.
    /// </summary>
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// 전송 형식별 매핑 전에 사용하는 전송 섹션의 원시 JSON 값을 가져옵니다.
    /// </summary>
    public JsonElement Transport { get; init; }
    /// <summary>
    /// 프로토콜 설정 섹션을 가져옵니다.
    /// </summary>
    public ProtocolOptions Protocol { get; init; } = new();
    /// <summary>
    /// 직렬화기 설정 섹션을 가져옵니다.
    /// </summary>
    public SerializerOptions Serializer { get; init; } = new();
    /// <summary>
    /// 재연결 설정 섹션을 가져옵니다.
    /// </summary>
    public ReconnectOptions Reconnect { get; init; } = new();
    /// <summary>
    /// 요청/응답 설정 섹션을 가져옵니다.
    /// </summary>
    public RequestResponseOptions RequestResponse { get; init; } = new();
}
