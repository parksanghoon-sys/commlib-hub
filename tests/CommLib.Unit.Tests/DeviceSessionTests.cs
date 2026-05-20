using CommLib.Application.Sessions;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

public sealed class DeviceSessionTests
{
    [Fact]
    public void Constructor_SetsDeviceId()
    {
        var session = new DeviceSession("device-1");

        Assert.Equal("device-1", session.DeviceId);
    }

    [Fact]
    public void TryRegisterPendingResponse_RequestMessage_ReturnsPendingResponseTask()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);

        var registered = session.TryRegisterPendingResponse<FakeRequestMessage, FakeResponseMessage>(
            request,
            timeout: null,
            out var responseTask,
            out var failure);

        Assert.True(registered);
        Assert.Null(failure);
        Assert.False(responseTask.IsCompleted);
        Assert.Equal(1, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryCompleteResponse_MatchingResponse_CompletesTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request);
        var response = new FakeResponseMessage(11) { CorrelationId = request.CorrelationId };

        var completed = session.TryCompleteResponse(response);
        var completedResponse = await responseTask;

        Assert.True(completed);
        Assert.Same(response, completedResponse);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryCompleteResponse_NonGenericOverload_CompletesTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request);
        IResponseMessage response = new FakeResponseMessage(11) { CorrelationId = request.CorrelationId };

        var completed = session.TryCompleteResponse(response);
        var completedResponse = await responseTask;

        Assert.True(completed);
        Assert.Equal((ushort)11, completedResponse.MessageId);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryCompleteResponse_CompletesResponse_CancelsPendingTimeoutRegistration()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(12);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request, TimeSpan.FromSeconds(5));
        var response = new FakeResponseMessage(13) { CorrelationId = request.CorrelationId };

        var completed = session.TryCompleteResponse(response);
        var completedResponse = await responseTask;
        await Task.Delay(100);

        Assert.True(completed);
        Assert.Same(response, completedResponse);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public void TryCompleteResponse_UnknownCorrelationId_ReturnsFalse()
    {
        var session = new DeviceSession("device-1");

        var completed = session.TryCompleteResponse(new FakeResponseMessage(1));

        Assert.False(completed);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryCompleteResponse_MismatchedResponseType_KeepsPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(14);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request);
        IResponseMessage mismatchedResponse = new OtherResponseMessage(15) { CorrelationId = request.CorrelationId };

        var completed = session.TryCompleteResponse(mismatchedResponse);

        Assert.False(completed);
        Assert.Equal(1, session.PendingRequestCount);
        Assert.False(responseTask.IsCompleted);

        var matchedResponse = new FakeResponseMessage(16) { CorrelationId = request.CorrelationId };
        Assert.True(session.TryCompleteResponse(matchedResponse));
        Assert.Same(matchedResponse, await responseTask);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryRegisterPendingResponse_WithTimeout_FailsResponseTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var responseTask = RegisterPending<FakeResponseMessage>(
            session,
            new FakeRequestMessage(20),
            TimeSpan.FromMilliseconds(50));

        var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await responseTask);

        Assert.Contains("Timed out waiting for response", exception.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryRegisterPendingResponse_WithoutExplicitTimeout_UsesConfiguredDefaultTimeout()
    {
        var session = new DeviceSession(
            "device-1",
            new CommLib.Domain.Configuration.RequestResponseOptions
            {
                DefaultTimeoutMs = 50,
                MaxPendingRequests = 8
            });
        var responseTask = RegisterPending<FakeResponseMessage>(session, new FakeRequestMessage(21));

        var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await responseTask);

        Assert.Contains("Timed out waiting for response", exception.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryRegisterPendingResponse_WhenPendingLimitReached_FailsWithoutRegisteringAdditionalPending()
    {
        var session = new DeviceSession(
            "device-1",
            new CommLib.Domain.Configuration.RequestResponseOptions
            {
                DefaultTimeoutMs = 0,
                MaxPendingRequests = 1
            });
        var firstRequest = new FakeRequestMessage(30);
        var firstResponseTask = RegisterPending<FakeResponseMessage>(session, firstRequest);
        var secondRequest = new FakeRequestMessage(31);

        var registered = session.TryRegisterPendingResponse<FakeRequestMessage, FakeResponseMessage>(
            secondRequest,
            timeout: null,
            out var secondResponseTask,
            out var failure);

        Assert.False(registered);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await secondResponseTask);
        Assert.Same(failure, exception);
        Assert.Equal(1, session.PendingRequestCount);

        var completed = session.TryCompleteResponse(new FakeResponseMessage(32)
        {
            CorrelationId = firstRequest.CorrelationId
        });

        Assert.True(completed);
        Assert.False(firstResponseTask.IsFaulted);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task FailPendingResponses_PendingRequestsFailImmediatelyAndClearPendingEntries()
    {
        var session = new DeviceSession("device-1");
        var first = RegisterPending<FakeResponseMessage>(session, new FakeRequestMessage(40));
        var second = RegisterPending<FakeResponseMessage>(session, new FakeRequestMessage(41));

        session.FailPendingResponses(new InvalidOperationException("session failed"));

        var firstException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);
        var secondException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await second);

        Assert.Equal("session failed", firstException.Message);
        Assert.Equal("session failed", secondException.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    public async Task TryFailPendingResponse_MatchingCorrelation_FailsResponseTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(50);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request);

        var failed = session.TryFailPendingResponse(request.CorrelationId, new InvalidOperationException("send failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await responseTask);
        Assert.True(failed);
        Assert.Equal("send failed", exception.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    private static Task<TResponse> RegisterPending<TResponse>(
        DeviceSession session,
        FakeRequestMessage request,
        TimeSpan? timeout = null)
        where TResponse : IResponseMessage
    {
        var registered = session.TryRegisterPendingResponse<FakeRequestMessage, TResponse>(
            request,
            timeout,
            out var responseTask,
            out var failure);

        Assert.True(registered);
        Assert.Null(failure);
        return responseTask;
    }

    private sealed record FakeRequestMessage(ushort MessageId) : IRequestMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }

    private sealed record FakeResponseMessage(ushort MessageId) : IResponseMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        public bool IsSuccess { get; init; } = true;
    }

    private sealed record OtherResponseMessage(ushort MessageId) : IResponseMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        public bool IsSuccess { get; init; } = true;
    }
}
