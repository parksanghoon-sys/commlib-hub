using System.Reflection;
using CommLib.Domain.Messaging;
using CommLib.Hosting;
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

    private static int GetInboundQueueCapacity(IConnectionManager manager)
    {
        var field = manager.GetType().GetField("_inboundQueueCapacity", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<int>(field!.GetValue(manager));
    }
}
