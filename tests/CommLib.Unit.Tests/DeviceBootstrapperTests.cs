using CommLib.Application.Bootstrap;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 활성화 여부에 따른 부트스트랩 동작을 검증합니다.
/// </summary>
public sealed class DeviceBootstrapperTests
{
    /// <summary>
    /// 부트스트랩이 활성화된 프로필만 연결하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_ConnectsOnlyEnabledProfiles()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            new DeviceProfile
            {
                DeviceId = "enabled-1",
                DisplayName = "Enabled 1",
                Enabled = true,
                Transport = new TcpClientTransportOptions { Type = "TcpClient", Host = "127.0.0.1", Port = 1000 },
                Protocol = new ProtocolOptions(),
                Serializer = new SerializerOptions()
            },
            new DeviceProfile
            {
                DeviceId = "disabled-1",
                DisplayName = "Disabled 1",
                Enabled = false,
                Transport = new TcpClientTransportOptions { Type = "TcpClient", Host = "127.0.0.1", Port = 1001 },
                Protocol = new ProtocolOptions(),
                Serializer = new SerializerOptions()
            }
        };

        await bootstrapper.StartAsync(profiles);

        Assert.Single(manager.ConnectedIds);
        Assert.Contains("enabled-1", manager.ConnectedIds);
    }

    /// <summary>
    /// 모든 프로필이 비활성화되어 있으면 연결을 시도하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenAllProfilesAreDisabled_DoesNotConnect()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            new DeviceProfile
            {
                DeviceId = "disabled-1",
                DisplayName = "Disabled 1",
                Enabled = false,
                Transport = new TcpClientTransportOptions { Type = "TcpClient", Host = "127.0.0.1", Port = 1000 },
                Protocol = new ProtocolOptions(),
                Serializer = new SerializerOptions()
            }
        };

        await bootstrapper.StartAsync(profiles);

        Assert.Empty(manager.ConnectedIds);
    }

    /// <summary>
    /// 호출 시 전달한 취소 토큰이 연결 관리자에도 그대로 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_PassesCancellationTokenToConnectionManager()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);
        using var cts = new CancellationTokenSource();

        var profiles = new[]
        {
            new DeviceProfile
            {
                DeviceId = "enabled-1",
                DisplayName = "Enabled 1",
                Enabled = true,
                Transport = new TcpClientTransportOptions { Type = "TcpClient", Host = "127.0.0.1", Port = 1000 },
                Protocol = new ProtocolOptions(),
                Serializer = new SerializerOptions()
            }
        };

        await bootstrapper.StartAsync(profiles, cts.Token);

        Assert.Equal(cts.Token, manager.LastCancellationToken);
    }

    /// <summary>
    /// 부트스트랩 테스트에 사용하는 최소한의 인메모리 연결 관리자입니다.
    /// </summary>
    private sealed class FakeConnectionManager : IConnectionManager
    {
        /// <summary>
        /// <see cref="ConnectAsync(DeviceProfile, CancellationToken)"/> 에 전달된 장치 식별자 목록을 가져옵니다.
        /// </summary>
        public List<string> ConnectedIds { get; } = new();

        /// <summary>
        /// 마지막으로 전달된 취소 토큰을 가져옵니다.
        /// </summary>
        public CancellationToken LastCancellationToken { get; private set; }

        /// <summary>
        /// 연결 요청된 장치 식별자를 기록합니다.
        /// </summary>
        /// <param name="profile">부트스트래퍼가 전달한 장치 프로필입니다.</param>
        /// <param name="cancellationToken">작업 취소에 사용할 토큰입니다.</param>
        /// <returns>완료된 작업입니다.</returns>
        public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
        {
            ConnectedIds.Add(profile.DeviceId);
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 가짜 구현에서는 활성 세션을 반환하지 않습니다.
        /// </summary>
        /// <param name="deviceId">조회할 장치 식별자입니다.</param>
        /// <returns>항상 <see langword="null"/> 을 반환합니다.</returns>
        public IDeviceSession? GetSession(string deviceId) => null;
    }
}
