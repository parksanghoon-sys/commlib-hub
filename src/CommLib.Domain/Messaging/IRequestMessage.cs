namespace CommLib.Domain.Messaging;

/// <summary>
/// 상관관계가 있는 응답을 기대하는 요청 메시지를 나타냅니다.
/// </summary>
public interface IRequestMessage : IMessage
{
    /// <summary>
    /// 요청과 응답을 연결할 때 사용하는 상관관계 식별자를 가져옵니다.
    /// </summary>
    Guid CorrelationId { get; }
}
