using CommLib.Application.Bootstrap;
using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Microsoft.Extensions.Hosting;

namespace CommLib.Hosting;

/// <summary>
/// Generic Host 시작과 종료 시점에 CommLib 장치 부트스트랩과 정리를 연결합니다.
/// </summary>
internal sealed class CommLibHostedService : IHostedService
{
    private readonly DeviceBootstrapper _bootstrapper;
    private readonly IConnectionManager _connectionManager;
    private readonly CommLibOptions _options;

    /// <summary>
    /// <see cref="CommLibHostedService"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="bootstrapper">호스트 시작 시 장치 연결을 수행할 부트스트래퍼입니다.</param>
    /// <param name="connectionManager">호스트 종료 시 연결 정리를 수행할 연결 관리자입니다.</param>
    /// <param name="options">설정 파일에서 바인딩한 CommLib 루트 옵션입니다.</param>
    public CommLibHostedService(
        DeviceBootstrapper bootstrapper,
        IConnectionManager connectionManager,
        CommLibOptions options)
    {
        _bootstrapper = bootstrapper;
        _connectionManager = connectionManager;
        _options = options;
    }

    /// <summary>
    /// 활성화된 장치 프로필만 매핑한 뒤 호스트 시작과 함께 연결합니다.
    /// </summary>
    /// <param name="cancellationToken">호스트 시작 취소 토큰입니다.</param>
    /// <returns>장치 부트스트랩 작업입니다.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var profiles = _options.Devices
            .Where(static raw => raw.Enabled)
            .Select(DeviceProfileMapper.Map)
            .ToArray();

        return _bootstrapper.StartAsync(profiles, cancellationToken);
    }

    /// <summary>
    /// 호스트 종료 시 활성 CommLib 연결 리소스를 비동기로 정리합니다.
    /// </summary>
    /// <param name="cancellationToken">호스트 종료 취소 토큰입니다.</param>
    /// <returns>연결 정리 작업입니다.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connectionManager is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}
