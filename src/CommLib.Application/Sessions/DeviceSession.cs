using System.Threading.Channels;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// 제한된 크기의 송신 채널을 사용하는 인메모리 장치 세션입니다.
/// </summary>
public sealed class DeviceSession : IDeviceSession
{
    /// <summary>
    /// 전송 파이프라인에서 처리되기를 기다리는 송신 메시지를 저장합니다.
    /// </summary>
    private readonly Channel<IMessage> _outbound = Channel.CreateBounded<IMessage>(64);

    /// <summary>
    /// <see cref="DeviceSession"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="deviceId">세션에 연결된 장치 식별자입니다.</param>
    public DeviceSession(string deviceId)
    {
        DeviceId = deviceId;
    }

    /// <summary>
    /// 이 세션에 연결된 장치 식별자를 가져옵니다.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// 메시지를 송신 채널에 넣으려고 시도합니다.
    /// </summary>
    /// <param name="message">큐에 넣을 송신 메시지입니다.</param>
    /// <returns>메시지가 수락되면 완료되고 큐가 가득 차면 실패하는 전송 결과입니다.</returns>
    public ISendResult Send(IMessage message)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_outbound.Writer.TryWrite(message))
        {
            tcs.TrySetResult();
        }
        else
        {
            tcs.TrySetException(new InvalidOperationException("Outbound queue is full."));
        }

        return new SendResult(tcs.Task);
    }

    /// <summary>
    /// 요청을 큐에 넣고 전송 완료와 응답 대기를 위한 핸들을 반환합니다.
    /// </summary>
    /// <typeparam name="TRequest">요청 메시지 형식입니다.</typeparam>
    /// <typeparam name="TResponse">기대하는 응답 메시지 형식입니다.</typeparam>
    /// <param name="request">큐에 넣을 요청 메시지입니다.</param>
    /// <param name="timeout">선택적인 응답 대기 시간 재정의 값입니다.</param>
    /// <returns>전송과 응답을 모두 기다릴 수 있는 형식화된 전송 결과입니다.</returns>
    public ISendResult<TResponse> Send<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        var sendResult = Send(request);
        var responseTcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        return new SendResult<TResponse>(sendResult.SendCompletedTask, responseTcs.Task);
    }
}
