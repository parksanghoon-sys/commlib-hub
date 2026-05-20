using CommLib.Application.Sessions;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// <see cref="DeviceSession"/>이 transport 송신 큐가 아니라 pending 요청/응답 상태만 관리하는지 검증합니다.
/// </summary>
public sealed class DeviceSessionTests
{
    [Fact]
    /// <summary>
    /// 생성자에 전달한 장치 식별자가 세션의 식별자로 보존되는지 확인합니다.
    /// </summary>
    public void Constructor_SetsDeviceId()
    {
        var session = new DeviceSession("device-1");

        Assert.Equal("device-1", session.DeviceId);
    }

    [Fact]
    /// <summary>
    /// 요청을 등록하면 응답 task가 대기 상태로 만들어지고 pending 개수가 증가하는지 확인합니다.
    /// </summary>
    public void TryRegisterPendingResponse_RequestMessage_ReturnsPendingResponseTask()
    {
        // 준비: 응답을 기다릴 새 세션과 요청 메시지를 만듭니다.
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);

        // 실행: 요청을 pending 응답 목록에 등록합니다.
        var registered = session.TryRegisterPendingResponse<FakeRequestMessage, FakeResponseMessage>(
            request,
            timeout: null,
            out var responseTask,
            out var failure);

        // 검증: 등록은 성공하고, 응답 task는 아직 완료되지 않은 대기 상태여야 합니다.
        Assert.True(registered);
        Assert.Null(failure);
        Assert.False(responseTask.IsCompleted);
        Assert.Equal(1, session.PendingRequestCount);
    }

    [Fact]
    /// <summary>
    /// 같은 correlation id와 같은 응답 형식이 도착하면 pending 응답 task를 완료하고 목록에서 제거하는지 확인합니다.
    /// </summary>
    public async Task TryCompleteResponse_MatchingResponse_CompletesTaskAndRemovesPendingEntry()
    {
        // 준비: 요청을 먼저 pending 상태로 등록하고 같은 correlation id의 응답을 만듭니다.
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request);
        var response = new FakeResponseMessage(11) { CorrelationId = request.CorrelationId };

        // 실행: typed 응답 완료 경로를 호출합니다.
        var completed = session.TryCompleteResponse(response);
        var completedResponse = await responseTask;

        // 검증: 응답 task가 주입한 응답으로 완료되고 pending 목록은 비어야 합니다.
        Assert.True(completed);
        Assert.Same(response, completedResponse);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    /// <summary>
    /// 응답 형식을 compile-time에 모르는 경로에서도 matching response가 pending task를 완료하는지 확인합니다.
    /// </summary>
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
    /// <summary>
    /// 응답이 timeout보다 먼저 완료되면 timeout 감시가 취소되어 나중에 task를 실패시키지 않는지 확인합니다.
    /// </summary>
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
    /// <summary>
    /// 등록된 correlation id가 없는 응답은 완료 처리하지 않고 무시하는지 확인합니다.
    /// </summary>
    public void TryCompleteResponse_UnknownCorrelationId_ReturnsFalse()
    {
        var session = new DeviceSession("device-1");

        var completed = session.TryCompleteResponse(new FakeResponseMessage(1));

        Assert.False(completed);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    /// <summary>
    /// correlation id는 같지만 응답 형식이 다르면 pending entry를 제거하지 않고 유지하는지 확인합니다.
    /// </summary>
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
    /// <summary>
    /// 요청별 timeout이 지나면 응답 task가 timeout 예외로 실패하고 pending 목록에서 제거되는지 확인합니다.
    /// </summary>
    public async Task TryRegisterPendingResponse_WithTimeout_FailsResponseTaskAndRemovesPendingEntry()
    {
        // 준비 및 실행: 짧은 timeout을 가진 pending 요청을 등록합니다.
        var session = new DeviceSession("device-1");
        var responseTask = RegisterPending<FakeResponseMessage>(
            session,
            new FakeRequestMessage(20),
            TimeSpan.FromMilliseconds(50));

        // 검증: timeout 만료 후 응답 task는 TimeoutException으로 실패하고 pending 목록은 정리됩니다.
        var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await responseTask);

        Assert.Contains("Timed out waiting for response", exception.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    /// <summary>
    /// 요청별 timeout을 생략하면 세션 생성 시 설정한 기본 timeout을 사용하는지 확인합니다.
    /// </summary>
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
    /// <summary>
    /// pending 요청 수가 설정 한도에 도달하면 추가 요청 등록을 실패시키고 기존 pending은 유지하는지 확인합니다.
    /// </summary>
    public async Task TryRegisterPendingResponse_WhenPendingLimitReached_FailsWithoutRegisteringAdditionalPending()
    {
        // 준비: 최대 pending 요청 수가 1개인 세션에 첫 번째 요청을 등록합니다.
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

        // 실행: 두 번째 요청 등록을 시도합니다.
        var registered = session.TryRegisterPendingResponse<FakeRequestMessage, FakeResponseMessage>(
            secondRequest,
            timeout: null,
            out var secondResponseTask,
            out var failure);

        // 검증: 두 번째 요청은 실패하고, 첫 번째 pending 요청은 정상 응답으로 완료할 수 있어야 합니다.
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
    /// <summary>
    /// 세션 전체 실패가 발생하면 모든 pending 응답 task를 같은 예외로 실패 처리하는지 확인합니다.
    /// </summary>
    public async Task FailPendingResponses_PendingRequestsFailImmediatelyAndClearPendingEntries()
    {
        // 준비: 두 개의 요청을 pending 상태로 둡니다.
        var session = new DeviceSession("device-1");
        var first = RegisterPending<FakeResponseMessage>(session, new FakeRequestMessage(40));
        var second = RegisterPending<FakeResponseMessage>(session, new FakeRequestMessage(41));

        // 실행: 연결 종료나 receive pump 실패와 같은 세션 전체 실패를 전달합니다.
        session.FailPendingResponses(new InvalidOperationException("session failed"));

        // 검증: 두 응답 task가 모두 같은 실패 원인으로 종료되고 pending 목록은 비어야 합니다.
        var firstException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await first);
        var secondException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await second);

        Assert.Equal("session failed", firstException.Message);
        Assert.Equal("session failed", secondException.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    [Fact]
    /// <summary>
    /// 송신 실패처럼 특정 요청만 실패해야 하는 경우 해당 correlation id의 pending task만 실패시키는지 확인합니다.
    /// </summary>
    public async Task TryFailPendingResponse_MatchingCorrelation_FailsResponseTaskAndRemovesPendingEntry()
    {
        // 준비: 단일 요청을 pending 상태로 등록합니다.
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(50);
        var responseTask = RegisterPending<FakeResponseMessage>(session, request);

        // 실행: 같은 correlation id의 pending 요청을 실패 처리합니다.
        var failed = session.TryFailPendingResponse(request.CorrelationId, new InvalidOperationException("send failed"));

        // 검증: 응답 task는 전달한 예외로 실패하고 pending 목록에서 제거됩니다.
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
        // 테스트 본문이 pending 등록의 반복 준비 과정을 직접 다시 쓰지 않도록 공통 helper로 모읍니다.
        var registered = session.TryRegisterPendingResponse<FakeRequestMessage, TResponse>(
            request,
            timeout,
            out var responseTask,
            out var failure);

        Assert.True(registered);
        Assert.Null(failure);
        return responseTask;
    }

    /// <summary>
    /// pending 등록 테스트에 사용하는 최소 요청 메시지입니다.
    /// </summary>
    /// <param name="MessageId">테스트에서 구분용으로 사용하는 메시지 식별자입니다.</param>
    private sealed record FakeRequestMessage(ushort MessageId) : IRequestMessage
    {
        /// <summary>
        /// 요청과 응답을 연결하는 correlation id입니다.
        /// </summary>
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }

    /// <summary>
    /// 정상 응답 완료 테스트에 사용하는 최소 응답 메시지입니다.
    /// </summary>
    /// <param name="MessageId">테스트에서 구분용으로 사용하는 메시지 식별자입니다.</param>
    private sealed record FakeResponseMessage(ushort MessageId) : IResponseMessage
    {
        /// <summary>
        /// 원본 요청과 연결되는 correlation id입니다.
        /// </summary>
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// 테스트 응답의 성공 여부를 나타냅니다.
        /// </summary>
        public bool IsSuccess { get; init; } = true;
    }

    /// <summary>
    /// correlation id는 같지만 기대 응답 형식이 다른 경우를 검증하기 위한 응답 메시지입니다.
    /// </summary>
    /// <param name="MessageId">테스트에서 구분용으로 사용하는 메시지 식별자입니다.</param>
    private sealed record OtherResponseMessage(ushort MessageId) : IResponseMessage
    {
        /// <summary>
        /// 원본 요청과 연결되는 correlation id입니다.
        /// </summary>
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// 테스트 응답의 성공 여부를 나타냅니다.
        /// </summary>
        public bool IsSuccess { get; init; } = true;
    }
}
