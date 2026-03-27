using System.Threading.Channels;
using CommLib.Application.Pipeline;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// 단일 장치와의 송신/응답 대기를 추적하는 인메모리 세션입니다.
/// </summary>
public sealed class DeviceSession : IDeviceSession
{
    private readonly Channel<IMessage> _outbound = Channel.CreateBounded<IMessage>(64);
    private readonly PendingRequestStore _pendingRequestStore = new();
    private readonly Dictionary<Guid, object> _pendingResponses = new();
    private readonly object _syncRoot = new();

    /// <summary>
    /// <see cref="DeviceSession"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="deviceId">세션과 연결된 장치 식별자입니다.</param>
    public DeviceSession(string deviceId)
    {
        DeviceId = deviceId;
    }

    /// <summary>
    /// 세션과 연결된 장치 식별자를 가져옵니다.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// 현재 응답을 기다리는 요청 수를 가져옵니다.
    /// </summary>
    public int PendingRequestCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _pendingResponses.Count;
            }
        }
    }

    /// <summary>
    /// 일반 메시지를 송신 큐에 추가합니다.
    /// </summary>
    /// <param name="message">큐에 추가할 메시지입니다.</param>
    /// <returns>큐 등록 성공 여부를 나타내는 결과입니다.</returns>
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
    /// 요청 메시지를 송신 큐에 추가하고 응답 완료를 추적합니다.
    /// </summary>
    /// <typeparam name="TRequest">요청 메시지 형식입니다.</typeparam>
    /// <typeparam name="TResponse">기대하는 응답 메시지 형식입니다.</typeparam>
    /// <param name="request">큐에 추가할 요청 메시지입니다.</param>
    /// <param name="timeout">선택적 응답 대기 시간입니다.</param>
    /// <returns>송신 완료와 응답 완료를 함께 추적하는 결과입니다.</returns>
    public ISendResult<TResponse> Send<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        var responseTcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sendCompletedTask = SendRequest(request, responseTcs, timeout);
        return new SendResult<TResponse>(sendCompletedTask, responseTcs.Task);
    }

    /// <summary>
    /// 송신 대기열에서 다음 outbound 메시지를 꺼냅니다.
    /// </summary>
    /// <param name="message">꺼낸 outbound 메시지입니다.</param>
    /// <returns>꺼낼 메시지가 있으면 <see langword="true"/>이고, 없으면 <see langword="false"/>입니다.</returns>
    public bool TryDequeueOutbound(out IMessage? message)
    {
        return _outbound.Reader.TryRead(out message);
    }

    /// <summary>
    /// 수신된 응답을 대기 중인 요청과 연결해 완료 처리합니다.
    /// </summary>
    /// <typeparam name="TResponse">완료할 응답 형식입니다.</typeparam>
    /// <param name="response">완료할 응답 메시지입니다.</param>
    /// <returns>대기 중인 요청을 찾아 완료했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryCompleteResponse<TResponse>(TResponse response)
        where TResponse : IResponseMessage
    {
        TaskCompletionSource<TResponse>? responseTcs;

        lock (_syncRoot)
        {
            if (!_pendingResponses.TryGetValue(response.CorrelationId, out var pending) ||
                pending is not TaskCompletionSource<TResponse> typedPending)
            {
                return false;
            }

            _pendingResponses.Remove(response.CorrelationId);
            _pendingRequestStore.Complete(response.CorrelationId);
            responseTcs = typedPending;
        }

        responseTcs.TrySetResult(response);
        return true;
    }

    /// <summary>
    /// 수신된 응답을 대기 중인 요청과 연결해 완료 처리합니다.
    /// </summary>
    /// <param name="response">완료 처리할 응답 메시지입니다.</param>
    /// <returns>대기 중인 요청을 찾아 완료했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryCompleteResponse(IResponseMessage response)
    {
        lock (_syncRoot)
        {
            if (!_pendingResponses.TryGetValue(response.CorrelationId, out var pending))
            {
                return false;
            }

            _pendingResponses.Remove(response.CorrelationId);
            _pendingRequestStore.Complete(response.CorrelationId);

            return pending switch
            {
                TaskCompletionSource<IResponseMessage> typed => typed.TrySetResult(response),
                _ => TrySetResponseResult(pending, response)
            };
        }
    }

    private Task SendRequest<TRequest, TResponse>(
        TRequest request,
        TaskCompletionSource<TResponse> responseTcs,
        TimeSpan? timeout)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        if (!_outbound.Writer.TryWrite(request))
        {
            var exception = new InvalidOperationException("Outbound queue is full.");
            responseTcs.TrySetException(exception);
            return Task.FromException(exception);
        }

        lock (_syncRoot)
        {
            _pendingResponses[request.CorrelationId] = responseTcs;
            _pendingRequestStore.Register(request.CorrelationId);
        }

        if (timeout is { } responseTimeout && responseTimeout > TimeSpan.Zero)
        {
            _ = HandleTimeoutAsync(request.CorrelationId, responseTimeout, responseTcs);
        }

        return Task.CompletedTask;
    }

    private async Task HandleTimeoutAsync<TResponse>(
        Guid correlationId,
        TimeSpan timeout,
        TaskCompletionSource<TResponse> responseTcs)
        where TResponse : IResponseMessage
    {
        await Task.Delay(timeout).ConfigureAwait(false);

        lock (_syncRoot)
        {
            if (!_pendingResponses.Remove(correlationId))
            {
                return;
            }

            _pendingRequestStore.Complete(correlationId);
        }

        responseTcs.TrySetException(new TimeoutException($"Timed out waiting for response to correlation '{correlationId}'."));
    }

    private static bool TrySetResponseResult(object pending, IResponseMessage response)
    {
        var pendingType = pending.GetType();
        if (!pendingType.IsGenericType || pendingType.GetGenericTypeDefinition() != typeof(TaskCompletionSource<>))
        {
            return false;
        }

        var responseType = pendingType.GetGenericArguments()[0];
        if (!responseType.IsInstanceOfType(response))
        {
            return false;
        }

        var trySetResult = pendingType.GetMethod(nameof(TaskCompletionSource<IResponseMessage>.TrySetResult));
        return trySetResult is not null && (bool)(trySetResult.Invoke(pending, new object[] { response }) ?? false);
    }
}
