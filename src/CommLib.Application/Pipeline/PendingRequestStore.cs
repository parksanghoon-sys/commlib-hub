namespace CommLib.Application.Pipeline;

/// <summary>
/// 응답을 기다리는 요청의 상관관계 식별자를 추적합니다.
/// </summary>
public sealed class PendingRequestStore
{
    /// <summary>
    /// 대기 중인 요청 식별자와 등록 시각을 저장합니다.
    /// </summary>
    private readonly Dictionary<Guid, DateTimeOffset> _pending = new();

    /// <summary>
    /// 상관관계 식별자를 대기 상태로 등록합니다.
    /// </summary>
    /// <param name="correlationId">추적할 요청 상관관계 식별자입니다.</param>
    public void Register(Guid correlationId)
    {
        _pending[correlationId] = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 지정한 상관관계 식별자가 현재 대기 중인지 확인합니다.
    /// </summary>
    /// <param name="correlationId">확인할 요청 상관관계 식별자입니다.</param>
    /// <returns>대기 중이면 <see langword="true"/> 이고, 아니면 <see langword="false"/> 입니다.</returns>
    public bool Exists(Guid correlationId)
    {
        return _pending.ContainsKey(correlationId);
    }

    /// <summary>
    /// 상관관계 식별자를 대기 집합에서 제거합니다.
    /// </summary>
    /// <param name="correlationId">완료 처리할 요청 상관관계 식별자입니다.</param>
    public void Complete(Guid correlationId)
    {
        _pending.Remove(correlationId);
    }
}
