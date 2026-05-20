using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// Tracks correlated request/response state for a single connected device.
/// </summary>
public sealed class DeviceSession
{
    private readonly Dictionary<Guid, PendingResponseEntry> _pendingResponses = new();
    private readonly object _syncRoot = new();
    private readonly int _maxPendingRequests;
    private readonly TimeSpan? _defaultResponseTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceSession"/> class.
    /// </summary>
    /// <param name="deviceId">The connected device identifier.</param>
    /// <param name="requestResponse">Request/response tracking limits and defaults.</param>
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
    /// Gets the connected device identifier.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// Gets the number of requests currently waiting for a response.
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
    /// Registers a request before it is sent so a later correlated response can complete it.
    /// </summary>
    /// <typeparam name="TRequest">The concrete request message type.</typeparam>
    /// <typeparam name="TResponse">The expected response message type.</typeparam>
    /// <param name="request">The request to track.</param>
    /// <param name="timeout">An optional response timeout.</param>
    /// <param name="responseTask">The response task when registration succeeds, or a failed task when it fails.</param>
    /// <param name="failure">The registration failure, if any.</param>
    /// <returns><see langword="true"/> when the request was registered.</returns>
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
    /// Completes the pending request matching the response correlation id.
    /// </summary>
    /// <typeparam name="TResponse">The concrete response type.</typeparam>
    /// <param name="response">The response to complete.</param>
    /// <returns><see langword="true"/> when a matching pending request was completed.</returns>
    public bool TryCompleteResponse<TResponse>(TResponse response)
        where TResponse : IResponseMessage
    {
        ArgumentNullException.ThrowIfNull(response);
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
    /// Completes the pending request matching the response correlation id.
    /// </summary>
    /// <param name="response">The response to complete.</param>
    /// <returns><see langword="true"/> when a matching pending request was completed.</returns>
    public bool TryCompleteResponse(IResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
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
    /// Fails one pending request if it is still waiting for a response.
    /// </summary>
    /// <param name="correlationId">The pending request correlation id.</param>
    /// <param name="exception">The failure to deliver to the response task.</param>
    /// <returns><see langword="true"/> when a pending request was failed.</returns>
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
    /// Fails every pending response when the owning connection stops.
    /// </summary>
    /// <param name="exception">The failure to deliver to each response task.</param>
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
