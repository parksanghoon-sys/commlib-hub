using System.Threading.Channels;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// 단일 장치와의 송신/응답 대기를 추적하는 인메모리 세션입니다.
/// </summary>
public sealed class DeviceSession : IDeviceSession
{
    private readonly Channel<IMessage> _outbound = Channel.CreateBounded<IMessage>(64);
    private readonly Dictionary<Guid, PendingResponseEntry> _pendingResponses = new();
    private readonly object _syncRoot = new();
    private readonly int _maxPendingRequests;
    private readonly TimeSpan? _defaultResponseTimeout;

    /// <summary>
    /// <see cref="DeviceSession"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="deviceId">세션과 연결된 장치 식별자입니다.</param>
    /// <param name="requestResponse">요청-응답 추적 제한과 기본 timeout 설정입니다.</param>
    public DeviceSession(string deviceId, RequestResponseOptions? requestResponse = null)
    {
        var options = requestResponse ?? new RequestResponseOptions();
        if (options.MaxPendingRequests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestResponse), "MaxPendingRequests must be greater than 0.");
        }

        DeviceId = deviceId;
        _maxPendingRequests = options.MaxPendingRequests;
        _defaultResponseTimeout = options.DefaultTimeoutMs > 0
            ? TimeSpan.FromMilliseconds(options.DefaultTimeoutMs)
            : null;
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
        var pendingEntry = new PendingResponseEntry<TResponse>();
        var sendCompletedTask = SendRequest(request, pendingEntry, timeout);
        return new SendResult<TResponse>(sendCompletedTask, pendingEntry.ResponseTask);
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
        PendingResponseEntry<TResponse>? pendingEntry;

        lock (_syncRoot)
        {
            if (!_pendingResponses.TryGetValue(response.CorrelationId, out var pending) ||
                pending is not PendingResponseEntry<TResponse> typedPending)
            {
                return false;
            }

            _pendingResponses.Remove(response.CorrelationId);
            pendingEntry = typedPending;
        }

        return pendingEntry.TryCompleteTyped(response);
    }

    /// <summary>
    /// 수신된 응답을 대기 중인 요청과 연결해 완료 처리합니다.
    /// </summary>
    /// <param name="response">완료 처리할 응답 메시지입니다.</param>
    /// <returns>대기 중인 요청을 찾아 완료했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryCompleteResponse(IResponseMessage response)
    {
        PendingResponseEntry? pendingEntry;

        lock (_syncRoot)
        {
            if (!_pendingResponses.TryGetValue(response.CorrelationId, out pendingEntry) ||
                !pendingEntry.CanComplete(response))
            {
                return false;
            }

            _pendingResponses.Remove(response.CorrelationId);
        }

        return pendingEntry.TryComplete(response);
    }

    /// <summary>
    /// 세션 실패 등으로 더 이상 응답을 기다릴 수 없을 때 모든 pending 요청을 실패 처리합니다.
    /// </summary>
    /// <param name="exception">각 pending 응답 작업에 전달할 예외입니다.</param>
    public void FailPendingResponses(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        PendingResponseEntry[] pendingResponses;

        lock (_syncRoot)
        {
            if (_pendingResponses.Count == 0)
            {
                return;
            }

            pendingResponses = _pendingResponses.Values.ToArray();
            _pendingResponses.Clear();
        }

        foreach (var pending in pendingResponses)
        {
            pending.Fail(exception);
        }
    }

    private Task SendRequest<TRequest, TResponse>(
        TRequest request,
        PendingResponseEntry<TResponse> pendingEntry,
        TimeSpan? timeout)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        var responseTimeout = timeout ?? _defaultResponseTimeout;

        lock (_syncRoot)
        {
            if (_pendingResponses.Count >= _maxPendingRequests)
            {
                var exception = new InvalidOperationException("Pending request limit has been reached.");
                pendingEntry.Fail(exception);
                return Task.FromException(exception);
            }

            if (!_outbound.Writer.TryWrite(request))
            {
                var exception = new InvalidOperationException("Outbound queue is full.");
                pendingEntry.Fail(exception);
                return Task.FromException(exception);
            }

            _pendingResponses[request.CorrelationId] = pendingEntry;
        }

        if (responseTimeout is { } effectiveResponseTimeout && effectiveResponseTimeout > TimeSpan.Zero)
        {
            var cancellationToken = pendingEntry.RegisterTimeout();
            _ = HandleTimeoutAsync(request.CorrelationId, effectiveResponseTimeout, pendingEntry, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task HandleTimeoutAsync<TResponse>(
        Guid correlationId,
        TimeSpan timeout,
        PendingResponseEntry<TResponse> pendingEntry,
        CancellationToken cancellationToken)
        where TResponse : IResponseMessage
    {
        try
        {
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (!_pendingResponses.Remove(correlationId))
            {
                return;
            }
        }

        pendingEntry.Timeout(correlationId);
    }

    private abstract class PendingResponseEntry
    {
        private CancellationTokenSource? _timeoutRegistration;

        public CancellationToken RegisterTimeout()
        {
            _timeoutRegistration = new CancellationTokenSource();
            return _timeoutRegistration.Token;
        }

        public abstract bool CanComplete(IResponseMessage response);

        public abstract bool TryComplete(IResponseMessage response);

        public abstract void Fail(Exception exception);

        public abstract void Timeout(Guid correlationId);

        protected void CancelAndDisposeTimeoutRegistration()
        {
            if (_timeoutRegistration is null)
            {
                return;
            }

            _timeoutRegistration.Cancel();
            _timeoutRegistration.Dispose();
            _timeoutRegistration = null;
        }

        protected void DisposeTimeoutRegistration()
        {
            _timeoutRegistration?.Dispose();
            _timeoutRegistration = null;
        }
    }

    private sealed class PendingResponseEntry<TResponse> : PendingResponseEntry
        where TResponse : IResponseMessage
    {
        private readonly TaskCompletionSource<TResponse> _responseTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TResponse> ResponseTask => _responseTcs.Task;

        public override bool CanComplete(IResponseMessage response)
        {
            return response is TResponse;
        }

        public bool TryCompleteTyped(TResponse response)
        {
            CancelAndDisposeTimeoutRegistration();
            return _responseTcs.TrySetResult(response);
        }

        public override bool TryComplete(IResponseMessage response)
        {
            return response is TResponse typedResponse && TryCompleteTyped(typedResponse);
        }

        public override void Fail(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            CancelAndDisposeTimeoutRegistration();
            _responseTcs.TrySetException(exception);
        }

        public override void Timeout(Guid correlationId)
        {
            DisposeTimeoutRegistration();
            _responseTcs.TrySetException(new TimeoutException($"Timed out waiting for response to correlation '{correlationId}'."));
        }
    }
}
