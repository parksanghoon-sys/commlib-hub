namespace CommLib.Domain.Messaging;

/// <summary>
/// 모든 메시지가 공통으로 따르는 최소 계약을 나타냅니다.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// 메시지 형식 또는 명령 식별자를 가져옵니다.
    /// </summary>
    ushort MessageId { get; }
}
