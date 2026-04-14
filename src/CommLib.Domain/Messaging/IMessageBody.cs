namespace CommLib.Domain.Messaging;

/// <summary>
/// 메시지 본문 텍스트를 함께 전달하는 메시지 계약입니다.
/// </summary>
public interface IMessageBody
{
    /// <summary>
    /// 직렬화 가능한 메시지 본문 텍스트입니다.
    /// </summary>
    string Body { get; }
}
