namespace CommLib.Domain.Configuration;

/// <summary>
/// 통신 라이브러리의 루트 설정 객체를 나타냅니다.
/// </summary>
public sealed class CommLibOptions
{
    /// <summary>
    /// 설정에서 읽어온 원시 장치 정의 목록을 가져옵니다.
    /// </summary>
    public List<DeviceProfileRaw> Devices { get; init; } = [];
}
