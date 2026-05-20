namespace CommLib.Domain.Messaging;

/// <summary>
/// 연결된 장치 세션에서 외부 호출자에게 허용되는 공개 송신 계약을 정의합니다.
/// </summary>
public interface IDeviceSession
{
    /// <summary>
    /// 현재 세션이 연결된 장치 식별자를 가져옵니다.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// 상관 응답을 기다리지 않는 단방향 메시지를 전송합니다.
    /// </summary>
    /// <param name="message">전송할 outbound 메시지입니다.</param>
    /// <returns>실제 transport 송신이 끝나면 완료되는 송신 결과입니다.</returns>
    ISendResult Send(IMessage message);

    /// <summary>
    /// 요청 메시지를 전송하고 같은 correlation id를 가진 응답 완료까지 추적합니다.
    /// </summary>
    /// <typeparam name="TRequest">전송할 요청 메시지의 구체 형식입니다.</typeparam>
    /// <typeparam name="TResponse">수신을 기대하는 응답 메시지의 구체 형식입니다.</typeparam>
    /// <param name="request">전송할 outbound 요청 메시지입니다.</param>
    /// <param name="timeout">기본 응답 대기 시간을 덮어쓸 선택적 timeout입니다.</param>
    /// <returns>transport 송신 완료와 correlation 응답 완료를 각각 관찰할 수 있는 송신 결과입니다.</returns>
    ISendResult<TResponse> Send<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage;
}
