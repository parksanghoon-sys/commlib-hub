using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// NoOpSerializer의 최소 직렬화/역직렬화 동작을 검증합니다.
/// </summary>
public sealed class NoOpSerializerTests
{
    /// <summary>
    /// 일반 메시지는 타입 태그와 메시지 식별자를 함께 직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Serialize_Message_ReturnsTaggedUtf8Bytes 작업을 수행합니다.
    /// </summary>
    public void Serialize_Message_ReturnsTaggedUtf8Bytes()
    {
        var serializer = new NoOpSerializer();

        var payload = serializer.Serialize(new FakeMessage(12));

        Assert.Equal("message|12|", System.Text.Encoding.UTF8.GetString(payload));
    }

    /// <summary>
    /// 요청 메시지는 상관관계 식별자까지 함께 직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Serialize_RequestMessage_IncludesCorrelationId 작업을 수행합니다.
    /// </summary>
    public void Serialize_RequestMessage_IncludesCorrelationId()
    {
        var serializer = new NoOpSerializer();
        var correlationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var payload = serializer.Serialize(new FakeRequestMessage(12, correlationId));

        Assert.Equal(
            "request|12|11111111-1111-1111-1111-111111111111|",
            System.Text.Encoding.UTF8.GetString(payload));
    }

    /// <summary>
    /// 응답 메시지는 상관관계 식별자와 성공 여부를 함께 직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Serialize_ResponseMessage_IncludesCorrelationIdAndSuccessFlag 작업을 수행합니다.
    /// </summary>
    public void Serialize_ResponseMessage_IncludesCorrelationIdAndSuccessFlag()
    {
        var serializer = new NoOpSerializer();
        var correlationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var payload = serializer.Serialize(new FakeResponseMessage(42, correlationId, false));

        Assert.Equal(
            "response|42|22222222-2222-2222-2222-222222222222|0|",
            System.Text.Encoding.UTF8.GetString(payload));
    }

    /// <summary>
    /// 본문이 있는 메시지는 base64 인코딩된 body까지 함께 직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Serialize_MessageWithBody_IncludesBase64Body 작업을 수행합니다.
    /// </summary>
    public void Serialize_MessageWithBody_IncludesBase64Body()
    {
        var serializer = new NoOpSerializer();

        var payload = serializer.Serialize(new FakeBodyMessage(12, "hello|world"));

        Assert.Equal("message|12|aGVsbG98d29ybGQ=", System.Text.Encoding.UTF8.GetString(payload));
    }

    /// <summary>
    /// 일반 메시지 payload를 메시지로 역직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Deserialize_MessagePayload_ReturnsMessage 작업을 수행합니다.
    /// </summary>
    public void Deserialize_MessagePayload_ReturnsMessage()
    {
        var serializer = new NoOpSerializer();

        var message = Assert.IsType<MessageModel>(
            serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("message|42|")));

        Assert.Equal((ushort)42, message.MessageId);
        Assert.IsNotAssignableFrom<IRequestMessage>(message);
        Assert.IsNotAssignableFrom<IResponseMessage>(message);
    }

    /// <summary>
    /// 요청 payload를 요청 메시지로 역직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Deserialize_RequestPayload_ReturnsRequestMessage 작업을 수행합니다.
    /// </summary>
    public void Deserialize_RequestPayload_ReturnsRequestMessage()
    {
        var serializer = new NoOpSerializer();
        var correlationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var message = Assert.IsType<RequestMessageModel>(
            serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("request|7|33333333-3333-3333-3333-333333333333")));

        Assert.Equal((ushort)7, message.MessageId);
        Assert.Equal(correlationId, message.CorrelationId);
    }

    /// <summary>
    /// 응답 payload를 응답 메시지로 역직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Deserialize_ResponsePayload_ReturnsResponseMessage 작업을 수행합니다.
    /// </summary>
    public void Deserialize_ResponsePayload_ReturnsResponseMessage()
    {
        var serializer = new NoOpSerializer();
        var correlationId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var message = Assert.IsType<ResponseMessageModel>(
            serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("response|9|44444444-4444-4444-4444-444444444444|1")));

        Assert.Equal((ushort)9, message.MessageId);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.True(message.IsSuccess);
    }

    /// <summary>
    /// body가 포함된 payload는 본문까지 복원하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Deserialize_MessagePayloadWithBody_ReturnsMessageBody 작업을 수행합니다.
    /// </summary>
    public void Deserialize_MessagePayloadWithBody_ReturnsMessageBody()
    {
        var serializer = new NoOpSerializer();

        var message = Assert.IsType<MessageModel>(
            serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("message|42|aGVsbG98d29ybGQ=")));

        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal("hello|world", message.Body);
    }

    /// <summary>
    /// 형식이 잘못된 payload는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Deserialize_InvalidPayload_Throws 작업을 수행합니다.
    /// </summary>
    public void Deserialize_InvalidPayload_Throws()
    {
        var serializer = new NoOpSerializer();

        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("request|A")));
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("response|1|bad-guid|1")));
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("response|1|11111111-1111-1111-1111-111111111111|x")));
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(System.Text.Encoding.UTF8.GetBytes("message|1|%%%")));
    }

    /// <summary>
    /// FakeMessage 작업을 수행합니다.
    /// </summary>
    private sealed record FakeMessage(ushort MessageId) : IMessage;
    /// <summary>
    /// FakeBodyMessage 작업을 수행합니다.
    /// </summary>
    private sealed record FakeBodyMessage(ushort MessageId, string Body) : IMessage, IMessageBody;
    /// <summary>
    /// FakeRequestMessage 작업을 수행합니다.
    /// </summary>
    private sealed record FakeRequestMessage(ushort MessageId, Guid CorrelationId) : IRequestMessage;
    /// <summary>
    /// FakeResponseMessage 작업을 수행합니다.
    /// </summary>
    private sealed record FakeResponseMessage(ushort MessageId, Guid CorrelationId, bool IsSuccess) : IResponseMessage;
}
