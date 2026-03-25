using CommLib.Application.Sessions;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Sessions;

/// <summary>
/// 연결된 장치에 대한 전송 생성과 인메모리 세션 등록을 관리합니다.
/// </summary>
public sealed class ConnectionManager : IConnectionManager
{
    /// <summary>
    /// 장치 전송을 초기화할 때 사용하는 전송 팩토리를 저장합니다.
    /// </summary>
    private readonly ITransportFactory _transportFactory;
    /// <summary>
    /// 장치 식별자를 기준으로 활성 세션을 저장합니다.
    /// </summary>
    private readonly Dictionary<string, IDeviceSession> _sessions = new();

    /// <summary>
    /// <see cref="ConnectionManager"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="transportFactory">장치 프로필에 맞는 전송을 생성하는 팩토리입니다.</param>
    public ConnectionManager(ITransportFactory transportFactory)
    {
        _transportFactory = transportFactory;
    }

    /// <summary>
    /// 지정한 장치 프로필에 대한 전송을 만들고 세션을 등록합니다.
    /// </summary>
    /// <param name="profile">연결할 장치 프로필입니다.</param>
    /// <param name="cancellationToken">연결 작업을 취소할 수 있는 토큰입니다.</param>
    /// <returns>세션 등록이 끝나면 완료되는 작업입니다.</returns>
    public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        _ = _transportFactory.Create(profile.Transport);
        _sessions[profile.DeviceId] = new DeviceSession(profile.DeviceId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 장치 식별자로 활성 세션을 조회합니다.
    /// </summary>
    /// <param name="deviceId">조회할 장치 세션의 식별자입니다.</param>
    /// <returns>활성 세션이 있으면 반환하고, 없으면 <see langword="null"/> 을 반환합니다.</returns>
    public IDeviceSession? GetSession(string deviceId)
    {
        _sessions.TryGetValue(deviceId, out var session);
        return session;
    }
}
