namespace CommLib.Domain.Messaging;

/// <summary>
/// 기본 메시지 식별자와 선택적 본문을 담는 공용 메시지 모델입니다.
/// </summary>
/// <param name="MessageId">메시지 식별자입니다.</param>
/// <param name="Body">선택적 메시지 본문입니다.</param>
public sealed record MessageModel(ushort MessageId, string Body = "") : IMessage, IMessageBody;

/// <summary>
/// raw binary payload를 직접 담는 공용 메시지 모델입니다.
/// </summary>
/// <param name="MessageId">메시지 식별자입니다.</param>
/// <param name="Payload">선택적 raw payload 바이트입니다.</param>
public sealed record BinaryMessageModel(ushort MessageId, ReadOnlyMemory<byte> Payload = default) : IMessage, IBinaryMessagePayload;

/// <summary>
/// 상관관계 식별자와 선택적 본문을 담는 공용 요청 메시지 모델입니다.
/// </summary>
/// <param name="MessageId">메시지 식별자입니다.</param>
/// <param name="CorrelationId">요청 상관관계 식별자입니다.</param>
/// <param name="Body">선택적 메시지 본문입니다.</param>
public sealed record RequestMessageModel(ushort MessageId, Guid CorrelationId, string Body = "") : IRequestMessage, IMessageBody;

/// <summary>
/// raw binary payload를 직접 담는 공용 요청 메시지 모델입니다.
/// </summary>
/// <param name="MessageId">메시지 식별자입니다.</param>
/// <param name="CorrelationId">요청 상관관계 식별자입니다.</param>
/// <param name="Payload">선택적 raw payload 바이트입니다.</param>
public sealed record BinaryRequestMessageModel(
    ushort MessageId,
    Guid CorrelationId,
    ReadOnlyMemory<byte> Payload = default) : IRequestMessage, IBinaryMessagePayload;

/// <summary>
/// 상관관계 식별자, 성공 여부, 선택적 본문을 담는 공용 응답 메시지 모델입니다.
/// </summary>
/// <param name="MessageId">메시지 식별자입니다.</param>
/// <param name="CorrelationId">응답 상관관계 식별자입니다.</param>
/// <param name="IsSuccess">응답 성공 여부입니다.</param>
/// <param name="Body">선택적 메시지 본문입니다.</param>
public sealed record ResponseMessageModel(
    ushort MessageId,
    Guid CorrelationId,
    bool IsSuccess,
    string Body = "") : IResponseMessage, IMessageBody;

/// <summary>
/// raw binary payload를 직접 담는 공용 응답 메시지 모델입니다.
/// </summary>
/// <param name="MessageId">메시지 식별자입니다.</param>
/// <param name="CorrelationId">응답 상관관계 식별자입니다.</param>
/// <param name="IsSuccess">응답 성공 여부입니다.</param>
/// <param name="Payload">선택적 raw payload 바이트입니다.</param>
public sealed record BinaryResponseMessageModel(
    ushort MessageId,
    Guid CorrelationId,
    bool IsSuccess,
    ReadOnlyMemory<byte> Payload = default) : IResponseMessage, IBinaryMessagePayload;
