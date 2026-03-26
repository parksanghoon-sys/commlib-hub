namespace CommLib.Domain.Messaging;

/// <summary>
/// 단일 장치에 대한 논리적 메시지 세션을 정의합니다.
/// </summary>
public interface IDeviceSession
{
    /// <summary>
    /// 세션과 연결된 장치 식별자를 가져옵니다.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// 응답을 기다리지 않는 메시지를 송신 대기열에 넣습니다.
    /// </summary>
    /// <param name="message">전송할 송신 메시지입니다.</param>
    /// <returns>메시지가 전송 대상으로 수락되면 완료되는 결과입니다.</returns>
    ISendResult Send(IMessage message);

    /// <summary>
    /// 요청 메시지를 대기열에 넣고 전송 완료와 응답 완료를 위한 핸들을 반환합니다.
    /// </summary>
    /// <typeparam name="TRequest">구체적인 요청 메시지 형식입니다.</typeparam>
    /// <typeparam name="TResponse">기대하는 응답 메시지 형식입니다.</typeparam>
    /// <param name="request">전송할 요청 메시지입니다.</param>
    /// <param name="timeout">선택적인 응답 대기 시간 재정의 값입니다.</param>
    /// <returns>전송 완료와 최종 응답을 모두 기다릴 수 있는 결과입니다.</returns>
    ISendResult<TResponse> Send<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage;

    /// <summary>
    /// 송신 대기열에서 다음 outbound 메시지를 꺼냅니다.
    /// </summary>
    /// <param name="message">꺼낸 outbound 메시지입니다.</param>
    /// <returns>꺼낼 메시지가 있으면 <see langword="true"/>이고, 없으면 <see langword="false"/>입니다.</returns>
    bool TryDequeueOutbound(out IMessage? message);
}
