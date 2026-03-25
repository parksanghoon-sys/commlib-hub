using CommLib.Domain.Configuration;

namespace CommLib.Domain.Messaging;

/// <summary>
/// 장치 연결 수명 주기 관리 동작을 정의합니다.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// 지정한 장치 프로필에 대한 연결을 열거나 초기화합니다.
    /// </summary>
    /// <param name="profile">연결할 검증된 장치 프로필입니다.</param>
    /// <param name="cancellationToken">연결 시도를 취소하는 토큰입니다.</param>
    /// <returns>연결 시도 처리가 끝나면 완료되는 작업입니다.</returns>
    Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default);
    /// <summary>
    /// 지정한 장치 식별자에 연결된 활성 세션을 가져옵니다.
    /// </summary>
    /// <param name="deviceId">조회할 장치 식별자입니다.</param>
    /// <returns>활성 장치 세션이 있으면 반환하고, 없으면 <see langword="null"/> 을 반환합니다.</returns>
    IDeviceSession? GetSession(string deviceId);
}
