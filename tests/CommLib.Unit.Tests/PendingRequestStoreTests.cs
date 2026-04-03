using CommLib.Application.Pipeline;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 대기 중 요청 추적 저장소의 기본 동작을 검증합니다.
/// </summary>
public sealed class PendingRequestStoreTests
{
    /// <summary>
    /// 등록한 상관관계 식별자는 존재하는 것으로 조회되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Register_RegisteredCorrelationId_ExistsReturnsTrue()
    {
        var store = new PendingRequestStore();
        var correlationId = Guid.NewGuid();

        store.Register(correlationId);

        Assert.True(store.Exists(correlationId));
    }

    /// <summary>
    /// 완료 처리한 상관관계 식별자는 더 이상 존재하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void Complete_RegisteredCorrelationId_RemovesPendingEntry()
    {
        var store = new PendingRequestStore();
        var correlationId = Guid.NewGuid();
        store.Register(correlationId);

        store.Complete(correlationId);

        Assert.False(store.Exists(correlationId));
    }

    /// <summary>
    /// 등록되지 않은 상관관계 식별자를 완료 처리해도 예외 없이 동작하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Complete_UnregisteredCorrelationId_DoesNotThrow()
    {
        var store = new PendingRequestStore();

        var exception = Record.Exception(() => store.Complete(Guid.NewGuid()));

        Assert.Null(exception);
    }

    /// <summary>
    /// 한 번도 등록하지 않은 상관관계 식별자는 존재하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void Exists_UnregisteredCorrelationId_ReturnsFalse()
    {
        var store = new PendingRequestStore();

        Assert.False(store.Exists(Guid.NewGuid()));
    }
}
