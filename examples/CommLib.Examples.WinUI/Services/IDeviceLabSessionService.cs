using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// IDeviceLabSessionService 계약을 정의하는 인터페이스입니다.
/// </summary>
public interface IDeviceLabSessionService : IAsyncDisposable
{
    /// <summary>
    /// 세션 로그가 추가될 때 발생하는 이벤트입니다.
    /// </summary>
    event EventHandler<LogEntry>? LogEmitted;

    /// <summary>
    /// 연결 상태가 바뀔 때 발생하는 이벤트입니다.
    /// </summary>
    event EventHandler<ConnectionStateSnapshot>? ConnectionStateChanged;

    /// <summary>
    /// 지정한 프로필로 세션 연결을 시작합니다.
    /// </summary>
    Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 현재 세션 연결을 종료합니다.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 활성 세션으로 메시지를 전송합니다.
    /// </summary>
    Task SendAsync(IMessage message, CancellationToken cancellationToken = default);
}
