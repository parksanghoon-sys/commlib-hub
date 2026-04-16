using CommLib.Application.Bootstrap;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 활성 프로필에 대한 부트스트랩 동작을 검증합니다.
/// </summary>
public sealed class DeviceBootstrapperTests
{
    /// <summary>
    /// 부트스트래퍼가 활성화된 프로필만 연결하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_ConnectsOnlyEnabledProfiles()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000),
            CreateProfile("disabled-1", enabled: false, port: 1001)
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
            CreateProfile("disabled-1", enabled: false, port: 1000)
        };

        await bootstrapper.StartAsync(profiles);

        Assert.Empty(manager.ConnectedIds);
    }

    /// <summary>
    /// 호출자가 전달한 취소 토큰을 연결 관리자까지 그대로 전달하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_PassesCancellationTokenToConnectionManager()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);
        using var cts = new CancellationTokenSource();

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000)
        };

        await bootstrapper.StartAsync(profiles, cts.Token);

        Assert.Equal(cts.Token, manager.LastCancellationToken);
    }

    /// <summary>
    /// 취소가 이미 요청된 상태라면 연결을 시도하지 않고 즉시 중단하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenCancellationAlreadyRequested_ThrowsWithoutConnecting()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000)
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bootstrapper.StartAsync(profiles, cts.Token));

        Assert.Empty(manager.ConnectedIds);
    }

    /// <summary>
    /// 첫 연결 이후 취소가 요청되면 남은 프로필 연결을 더 진행하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenCancellationRequestedBetweenProfiles_StopsRemainingConnections()
    {
        using var cts = new CancellationTokenSource();
        var manager = new FakeConnectionManager
        {
            ConnectAsyncHandler = profile =>
            {
                if (profile.DeviceId == "enabled-1")
                {
                    cts.Cancel();
                }

                return Task.CompletedTask;
            }
        };
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000),
            CreateProfile("enabled-2", enabled: true, port: 1001)
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bootstrapper.StartAsync(profiles, cts.Token));

        Assert.Equal(new[] { "enabled-1" }, manager.ConnectedIds);
    }

    /// <summary>
    /// 여러 활성 프로필이 있으면 입력 순서대로 연결을 시도하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenMultipleEnabledProfiles_ConnectsInInputOrder()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000),
            CreateProfile("disabled-1", enabled: false, port: 1001),
            CreateProfile("enabled-2", enabled: true, port: 1002)
        };

        await bootstrapper.StartAsync(profiles);

        Assert.Equal(new[] { "enabled-1", "enabled-2" }, manager.ConnectedIds);
    }

    [Fact]
    public async Task StartAsync_WhenProfileIsInvalid_ThrowsBeforeConnectionManagerIsCalled()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);
        var profiles = new[]
        {
            CreateInvalidTcpProfile("invalid-1", enabled: true, port: 1000)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.StartAsync(profiles));

        Assert.Contains("[invalid-1] TCP Host is required.", exception.Message);
        Assert.Empty(manager.ConnectedIds);
    }

    /// <summary>
    /// 연결 관리자 예외를 숨기지 않고 호출자에게 전파하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenConnectionManagerThrows_PropagatesException()
    {
        var manager = new FakeConnectionManager
        {
            ConnectAsyncHandler = profile =>
            {
                throw new InvalidOperationException($"connect failed: {profile.DeviceId}");
            }
        };
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.StartAsync(profiles));

        Assert.Contains("enabled-1", exception.Message);
    }

    /// <summary>
    /// 중간 연결이 실패하면 이후 프로필 연결은 시도하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenConnectionFails_StopsProcessingRemainingProfiles()
    {
        var manager = new FakeConnectionManager
        {
            ConnectAsyncHandler = profile =>
            {
                if (profile.DeviceId == "enabled-2")
                {
                    throw new InvalidOperationException("boom");
                }

                return Task.CompletedTask;
            }
        };
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000),
            CreateProfile("enabled-2", enabled: true, port: 1001),
            CreateProfile("enabled-3", enabled: true, port: 1002)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.StartAsync(profiles));

        Assert.Equal(new[] { "enabled-1", "enabled-2" }, manager.ConnectedIds);
    }

    [Fact]
    public async Task StartWithReportAsync_WhenProfilesIncludeValidationAndConnectionFailures_ContinuesAndReturnsReport()
    {
        var manager = new FakeConnectionManager
        {
            ConnectAsyncHandler = profile =>
            {
                if (profile.DeviceId == "enabled-2")
                {
                    throw new InvalidOperationException("boom");
                }

                return Task.CompletedTask;
            }
        };
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            CreateProfile("enabled-1", enabled: true, port: 1000),
            CreateInvalidTcpProfile("invalid-1", enabled: true, port: 1001),
            CreateProfile("enabled-2", enabled: true, port: 1002),
            CreateProfile("enabled-3", enabled: true, port: 1003),
            CreateProfile("disabled-1", enabled: false, port: 1004)
        };

        var report = await bootstrapper.StartWithReportAsync(profiles);

        Assert.Equal(new[] { "enabled-1", "enabled-2", "enabled-3" }, manager.ConnectedIds);
        Assert.Equal(new[] { "enabled-1", "enabled-3" }, report.ConnectedDeviceIds);
        Assert.True(report.HasFailures);
        Assert.Collection(
            report.Failures,
            failure =>
            {
                Assert.Equal("invalid-1", failure.DeviceId);
                Assert.Equal("[invalid-1] TCP Host is required.", failure.Exception.Message);
            },
            failure =>
            {
                Assert.Equal("enabled-2", failure.DeviceId);
                Assert.Equal("boom", failure.Exception.Message);
            });
    }

    private static DeviceProfile CreateProfile(string deviceId, bool enabled, int port)
    {
        return new DeviceProfile
        {
            DeviceId = deviceId,
            DisplayName = deviceId,
            Enabled = enabled,
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = "127.0.0.1",
                Port = port
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    private static DeviceProfile CreateInvalidTcpProfile(string deviceId, bool enabled, int port)
    {
        return new DeviceProfile
        {
            DeviceId = deviceId,
            DisplayName = deviceId,
            Enabled = enabled,
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = string.Empty,
                Port = port
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    /// <summary>
    /// 부트스트랩 테스트에 사용하는 최소 메모리 연결 관리자입니다.
    /// </summary>
    private sealed class FakeConnectionManager : IConnectionManager
    {
        /// <summary>
        /// 연결을 시도한 장치 식별자 목록입니다.
        /// </summary>
        public List<string> ConnectedIds { get; } = new();

        /// <summary>
        /// 마지막으로 전달된 취소 토큰입니다.
        /// </summary>
        public CancellationToken LastCancellationToken { get; private set; }

        /// <summary>
        /// 테스트가 연결 동작을 가로챌 수 있게 하는 핸들러입니다.
        /// </summary>
        public Func<DeviceProfile, Task>? ConnectAsyncHandler { get; init; }

        /// <summary>
        /// 연결 요청 장치 식별자를 기록합니다.
        /// </summary>
        /// <param name="profile">부트스트래퍼가 전달한 장치 프로필입니다.</param>
        /// <param name="cancellationToken">작업 취소에 사용하는 토큰입니다.</param>
        /// <returns>완료 작업입니다.</returns>
        public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
        {
            ConnectedIds.Add(profile.DeviceId);
            LastCancellationToken = cancellationToken;

            return ConnectAsyncHandler?.Invoke(profile) ?? Task.CompletedTask;
        }

        /// <summary>
        /// 부트스트랩 테스트에서는 송신 기능을 사용하지 않습니다.
        /// </summary>
        /// <param name="deviceId">메시지를 보낼 장치 식별자입니다.</param>
        /// <param name="message">전송할 메시지입니다.</param>
        /// <param name="cancellationToken">전송 취소 토큰입니다.</param>
        /// <returns>완료 작업입니다.</returns>
        /// <summary>
        /// 부트스트래퍼 테스트에서는 연결 해제 기능을 사용하지 않습니다.
        /// </summary>
        /// <param name="deviceId">연결 해제할 장치 식별자입니다.</param>
        /// <param name="cancellationToken">연결 해제 취소 토큰입니다.</param>
        /// <returns>항상 완료된 작업을 반환합니다.</returns>
        public Task DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task SendAsync(string deviceId, IMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 가짜 구현에서는 활성 세션을 반환하지 않습니다.
        /// </summary>
        /// <param name="deviceId">조회할 장치 식별자입니다.</param>
        /// <returns>항상 <see langword="null"/>을 반환합니다.</returns>
        /// <summary>
        /// 부트스트래퍼 테스트에서는 수신 기능을 사용하지 않습니다.
        /// </summary>
        /// <param name="deviceId">메시지를 수신할 장치 식별자입니다.</param>
        /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
        /// <returns>항상 <see cref="NotSupportedException"/> 예외를 발생시킵니다.</returns>
        public Task<IMessage> ReceiveAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public IDeviceSession? GetSession(string deviceId) => null;
    }
}
