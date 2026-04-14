namespace CommLib.Domain.Messaging;

/// <summary>
/// 이전 요청과 상관관계를 가지는 응답 메시지를 나타냅니다.
/// </summary>
public interface IResponseMessage : IMessage
{
    /// <summary>
    /// 원본 요청의 상관관계 식별자를 가져옵니다.
    /// </summary>
    Guid CorrelationId { get; }
    /// <summary>
    /// 응답이 성공 결과를 의미하는지 여부를 가져옵니다.
    /// </summary>
    bool IsSuccess { get; }
}
