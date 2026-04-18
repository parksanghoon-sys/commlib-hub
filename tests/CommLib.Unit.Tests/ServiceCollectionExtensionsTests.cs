using System.Reflection;
using CommLib.Domain.Messaging;
using CommLib.Hosting;
using CommLib.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 호스팅 레이어의 런타임 옵션 wiring을 검증합니다.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// 기본 등록 경로가 기본 inbound queue capacity를 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task AddCommLibCore_WithoutOverride_UsesDefaultInboundQueueCapacity()
    {
        var services = new ServiceCollection();
        services.AddCommLibCore();

        await using var serviceProvider = services.BuildServiceProvider();
        var runtimeOptions = serviceProvider.GetRequiredService<CommLibRuntimeOptions>();
        var manager = serviceProvider.GetRequiredService<IConnectionManager>();

        Assert.Equal(256, runtimeOptions.InboundQueueCapacity);
        Assert.Equal(256, GetInboundQueueCapacity(manager));
    }

    /// <summary>
    /// override 경로가 지정한 inbound queue capacity를 연결 관리자까지 전달하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task AddCommLibCore_WithOverride_UsesConfiguredInboundQueueCapacity()
    {
        var services = new ServiceCollection();
        services.AddCommLibCore(options => options.InboundQueueCapacity = 8);

        await using var serviceProvider = services.BuildServiceProvider();
        var runtimeOptions = serviceProvider.GetRequiredService<CommLibRuntimeOptions>();
        var manager = serviceProvider.GetRequiredService<IConnectionManager>();

        Assert.Equal(8, runtimeOptions.InboundQueueCapacity);
        Assert.Equal(8, GetInboundQueueCapacity(manager));
    }

    /// <summary>
    /// caller가 등록한 connection-event sink가 DI를 통해 연결 관리자로 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task AddCommLibCore_WithRegisteredConnectionEventSink_UsesRegisteredSink()
    {
        var services = new ServiceCollection();
        var sink = new RecordingConnectionEventSink();
        services.AddSingleton<IConnectionEventSink>(sink);
        services.AddCommLibCore();

        await using var serviceProvider = services.BuildServiceProvider();
        var manager = serviceProvider.GetRequiredService<IConnectionManager>();

        Assert.Same(sink, GetConnectionEventSink(manager));
    }

    private static int GetInboundQueueCapacity(IConnectionManager manager)
    {
        var field = manager.GetType().GetField("_inboundQueueCapacity", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<int>(field!.GetValue(manager));
    }

    private static IConnectionEventSink GetConnectionEventSink(IConnectionManager manager)
    {
        var field = manager.GetType().GetField("_eventSink", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<IConnectionEventSink>(field!.GetValue(manager));
    }

    private sealed class RecordingConnectionEventSink : IConnectionEventSink
    {
        public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
        {
        }

        public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
        {
        }

        public void OnConnectSucceeded(string deviceId, int attemptNumber)
        {
        }

        public void OnOperationFailed(string deviceId, string operation, Exception exception)
        {
        }
    }
}
