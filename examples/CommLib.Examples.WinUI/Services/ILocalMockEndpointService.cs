using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// LocalMockEndpointRequest 레코드입니다.
/// </summary>
public sealed record LocalMockEndpointRequest(
    TransportKind TransportKind,
    int Port,
    string? Address = null,
    string? LocalInterface = null);

/// <summary>
/// LocalMockEndpointBinding 레코드입니다.
/// </summary>
public sealed record LocalMockEndpointBinding(
    TransportKind TransportKind,
    string Address,
    int Port);

/// <summary>
/// ILocalMockEndpointService 계약을 정의하는 인터페이스입니다.
/// </summary>
public interface ILocalMockEndpointService : IAsyncDisposable
{
    /// <summary>
    /// 지정한 요청에 맞는 로컬 mock endpoint를 시작합니다.
    /// </summary>
    Task<LocalMockEndpointBinding> StartAsync(
        LocalMockEndpointRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 실행 중인 로컬 mock endpoint를 중지합니다.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
