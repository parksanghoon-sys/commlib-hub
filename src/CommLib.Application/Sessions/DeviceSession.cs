using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// 단일 장치 연결에서 correlation id 기반 요청/응답 대기 상태만 추적하는 런타임 세션입니다.
/// </summary>
public sealed class DeviceSession
{
    private readonly Dictionary<Guid, PendingResponseEntry> _pendingResponses = new();
    private readonly object _syncRoot = new();
    private readonly int _maxPendingRequests;
    private readonly TimeSpan? _defaultResponseTimeout;

    /// <summary>
    /// <see cref="DeviceSession"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="deviceId">연결된 장치 식별자입니다.</param>
    /// <param name="requestResponse">요청/응답 추적 한도와 기본 timeout 설정입니다.</param>
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
    /// 현재 세션이 담당하는 장치 식별자를 가져옵니다.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// 아직 응답을 받지 못해 대기 중인 요청 수를 가져옵니다.
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
    /// 요청을 transport로 보내기 전에 pending 목록에 등록해 이후 응답과 연결할 수 있게 합니다.
    /// </summary>
    /// <typeparam name="TRequest">등록할 요청 메시지의 구체 형식입니다.</typeparam>
    /// <typeparam name="TResponse">완료를 기대하는 응답 메시지의 구체 형식입니다.</typeparam>
    /// <param name="request">추적할 요청 메시지입니다.</param>
    /// <param name="timeout">기본 응답 대기 시간을 덮어쓸 선택적 timeout입니다.</param>
    /// <param name="responseTask">등록 성공 시 응답을 기다리는 작업이고, 등록 실패 시 실패 상태 작업입니다.</param>
    /// <param name="failure">등록에 실패한 경우 그 원인 예외입니다.</param>
    /// <returns>pending 요청으로 정상 등록되면 <see langword="true"/>입니다.</returns>
    public bool TryRegisterPendingResponse<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout,
        out Task<TResponse> responseTask,
        out Exception? failure)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        ArgumentNullException.ThrowIfNull(request);

        var pendingEntry = new PendingResponseEntry<TResponse>();
        var responseTimeout = timeout ?? _defaultResponseTimeout;

        // 1. pending 목록은 receive pump, timeout task, 송신 실패 경로가 함께 접근하므로 잠금 안에서만 갱신합니다.
        lock (_syncRoot)
        {
            if (_pendingResponses.Count >= _maxPendingRequests)
            {
                failure = new InvalidOperationException("Pending request limit has been reached.");
                responseTask = Task.FromException<TResponse>(failure);
                return false;
            }

            if (_pendingResponses.ContainsKey(request.CorrelationId))
            {
                failure = new InvalidOperationException($"Pending request '{request.CorrelationId}' is already registered.");
                responseTask = Task.FromException<TResponse>(failure);
                return false;
            }

            _pendingResponses.Add(request.CorrelationId, pendingEntry);
        }

        // 2. 등록이 끝난 뒤 timeout 감시를 시작합니다. 응답 완료나 실패가 먼저 오면 pending entry가 이 감시를 취소합니다.
        if (responseTimeout is { } effectiveResponseTimeout && effectiveResponseTimeout > TimeSpan.Zero)
        {
            var cancellationToken = pendingEntry.RegisterTimeout();
            _ = HandleTimeoutAsync(request.CorrelationId, effectiveResponseTimeout, pendingEntry, cancellationToken);
        }

        responseTask = pendingEntry.ResponseTask;
        failure = null;
        return true;
    }

    /// <summary>
    /// 응답 형식까지 알고 있는 경로에서 correlation id가 일치하는 pending 요청을 완료합니다.
    /// </summary>
    /// <typeparam name="TResponse">완료할 응답 메시지의 구체 형식입니다.</typeparam>
    /// <param name="response">pending 요청을 완료할 응답 메시지입니다.</param>
    /// <returns>일치하는 pending 요청을 찾아 완료했으면 <see langword="true"/>입니다.</returns>
    public bool TryCompleteResponse<TResponse>(TResponse response)
        where TResponse : IResponseMessage
    {
        ArgumentNullException.ThrowIfNull(response);
        PendingResponseEntry<TResponse>? pendingEntry;

        // 완료 대상 entry를 잠금 안에서 제거한 뒤, 실제 TaskCompletionSource 완료는 잠금 밖에서 수행합니다.
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
    /// 응답 형식을 런타임에 확인해 correlation id가 일치하는 pending 요청을 완료합니다.
    /// </summary>
    /// <param name="response">pending 요청을 완료할 응답 메시지입니다.</param>
    /// <returns>일치하는 pending 요청을 찾아 완료했으면 <see langword="true"/>입니다.</returns>
    public bool TryCompleteResponse(IResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        PendingResponseEntry? pendingEntry;

        // correlation id가 같아도 응답 타입이 다르면 pending entry를 유지해야 다음 올바른 응답이 완료할 수 있습니다.
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
    /// 지정한 correlation id의 pending 요청이 아직 대기 중이면 실패 처리합니다.
    /// </summary>
    /// <param name="correlationId">실패 처리할 pending 요청의 correlation id입니다.</param>
    /// <param name="exception">응답 작업에 전달할 실패 원인입니다.</param>
    /// <returns>대기 중인 요청을 찾아 실패 처리했으면 <see langword="true"/>입니다.</returns>
    public bool TryFailPendingResponse(Guid correlationId, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        PendingResponseEntry? pendingEntry;

        lock (_syncRoot)
        {
            if (!_pendingResponses.TryGetValue(correlationId, out pendingEntry))
            {
                return false;
            }

            _pendingResponses.Remove(correlationId);
        }

        pendingEntry.Fail(exception);
        return true;
    }

    /// <summary>
    /// 연결 종료나 receive pump 실패처럼 세션 전체가 더 이상 응답을 받을 수 없을 때 모든 pending 요청을 실패 처리합니다.
    /// </summary>
    /// <param name="exception">각 pending 응답 작업에 전달할 실패 원인입니다.</param>
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

    private async Task HandleTimeoutAsync<TResponse>(
        Guid correlationId,
        TimeSpan timeout,
        PendingResponseEntry<TResponse> pendingEntry,
        CancellationToken cancellationToken)
        where TResponse : IResponseMessage
    {
        // 1. 지정된 시간 동안 응답 완료, 실패, 세션 종료가 먼저 발생하는지 기다립니다.
        try
        {
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // 2. timeout 시점에도 pending 목록에 남아 있는 요청만 제거하고 timeout 예외로 완료합니다.
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

        /// <summary>
        /// 이 pending 응답에 연결된 timeout 감시용 취소 토큰을 등록합니다.
        /// </summary>
        /// <returns>응답 완료나 실패 시 timeout 감시를 취소하는 데 사용할 토큰입니다.</returns>
        public CancellationToken RegisterTimeout()
        {
            _timeoutRegistration = new CancellationTokenSource();
            return _timeoutRegistration.Token;
        }

        /// <summary>
        /// 수신된 응답이 이 pending entry가 기대하는 응답 형식인지 확인합니다.
        /// </summary>
        /// <param name="response">검사할 응답 메시지입니다.</param>
        /// <returns>이 entry가 완료할 수 있는 응답이면 <see langword="true"/>입니다.</returns>
        public abstract bool CanComplete(IResponseMessage response);

        /// <summary>
        /// 형식이 일치하는 응답으로 응답 작업을 완료합니다.
        /// </summary>
        /// <param name="response">완료에 사용할 응답 메시지입니다.</param>
        /// <returns>응답 작업을 이번 호출에서 완료했으면 <see langword="true"/>입니다.</returns>
        public abstract bool TryComplete(IResponseMessage response);

        /// <summary>
        /// 연결 실패, 송신 실패, 세션 종료 같은 외부 실패를 응답 작업에 전달합니다.
        /// </summary>
        /// <param name="exception">응답 작업에 전달할 실패 원인입니다.</param>
        public abstract void Fail(Exception exception);

        /// <summary>
        /// timeout 만료로 응답 작업을 실패 처리합니다.
        /// </summary>
        /// <param name="correlationId">timeout이 발생한 요청의 correlation id입니다.</param>
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

        /// <summary>
        /// 외부 호출자가 기다리는 타입 안전한 응답 작업을 가져옵니다.
        /// </summary>
        public Task<TResponse> ResponseTask => _responseTcs.Task;

        public override bool CanComplete(IResponseMessage response)
        {
            return response is TResponse;
        }

        public bool TryCompleteTyped(TResponse response)
        {
            // 정상 응답을 받았으므로 timeout 감시를 먼저 취소한 뒤 응답 작업을 완료합니다.
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
