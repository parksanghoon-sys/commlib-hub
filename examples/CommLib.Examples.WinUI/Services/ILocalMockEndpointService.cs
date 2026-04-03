using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

public sealed record LocalMockEndpointRequest(
    TransportKind TransportKind,
    int Port,
    string? Address = null,
    string? LocalInterface = null);

public sealed record LocalMockEndpointBinding(
    TransportKind TransportKind,
    string Address,
    int Port);

public interface ILocalMockEndpointService : IAsyncDisposable
{
    Task<LocalMockEndpointBinding> StartAsync(
        LocalMockEndpointRequest request,
        CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
