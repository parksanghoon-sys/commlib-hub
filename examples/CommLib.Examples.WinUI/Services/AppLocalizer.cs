using System.Globalization;
using CommLib.Domain.Configuration;
using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// AppLocalizer 타입입니다.
/// </summary>
public sealed class AppLocalizer : IAppLocalizer
{
    // WinUI 화면이 대부분 코드로 구성되어 있어서 XAML resource보다 key-value 사전을 직접 들고 가는 편이 단순하다.
    // 키는 View / ViewModel / Service 어디에서 써도 같게 유지하고, 실제 언어 문자열만 여기에서 바꾼다.
    /// <summary>
    /// EnglishStrings 값을 나타냅니다.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> EnglishStrings = new Dictionary<string, string>
    {
        ["window.title"] = "CommLib Device Lab",
        ["shell.appTitle"] = "CommLib Control Center",
        ["shell.nav.deviceLab"] = "Device Lab",
        ["shell.nav.settings"] = "Settings",
        ["deviceLab.hero.title"] = "Device Lab",
        ["deviceLab.hero.description"] = "TCP, UDP, Multicast, and Serial transport sessions can be opened here with shared MVVM settings.",
        ["deviceLab.section.sessionSetup"] = "Session Setup",
        ["deviceLab.section.transportSettings"] = "Transport Settings",
        ["deviceLab.section.mockEndpoint"] = "Mock Endpoint",
        ["deviceLab.section.messageComposer"] = "Message Composer",
        ["deviceLab.section.liveLog"] = "Live Log",
        ["deviceLab.mockEndpoint.description"] = "Start a local peer here so the current transport can be tested without a separate server process.",
        ["settings.hero.title"] = "Settings",
        ["settings.hero.description"] = "Edit defaults here. These values are shared with Device Lab and persisted to appsettings.json.",
        ["settings.section.general"] = "General",
        ["settings.section.messageDefaults"] = "Message Defaults",
        ["settings.section.transportPresets"] = "Transport Presets",
        ["settings.section.persistence"] = "Persistence",
        ["settings.field.selectedTransport"] = "Selected Transport",
        ["field.languageMode"] = "Language Mode",
        ["field.transport"] = "Transport",
        ["field.deviceId"] = "Device Id",
        ["field.displayName"] = "Display Name",
        ["field.defaultTimeoutMs"] = "Default Timeout (ms)",
        ["field.maxPendingRequests"] = "Max Pending Requests",
        ["field.host"] = "Host",
        ["field.port"] = "Port",
        ["field.connectTimeoutMs"] = "Connect Timeout (ms)",
        ["field.bufferSize"] = "Buffer Size",
        ["field.localPort"] = "Local Port",
        ["field.remoteHost"] = "Remote Host",
        ["field.remotePort"] = "Remote Port",
        ["field.groupAddress"] = "Group Address",
        ["field.ttl"] = "TTL",
        ["field.localInterface"] = "Local Interface",
        ["field.portName"] = "Port Name",
        ["field.baudRate"] = "Baud Rate",
        ["field.dataBits"] = "Data Bits",
        ["field.parity"] = "Parity",
        ["field.stopBits"] = "Stop Bits",
        ["field.turnGapMs"] = "Turn Gap (ms)",
        ["field.readBufferSize"] = "Read Buffer Size",
        ["field.writeBufferSize"] = "Write Buffer Size",
        ["field.messageId"] = "Message Id",
        ["field.serializer"] = "Serializer",
        ["field.body"] = "Body",
        ["check.noDelay"] = "Disable Nagle (NoDelay)",
        ["check.loopback"] = "Enable loopback",
        ["check.halfDuplex"] = "Use half-duplex timing",
        ["button.connect"] = "Connect",
        ["button.disconnect"] = "Disconnect",
        ["button.startMockEndpoint"] = "Start Mock",
        ["button.stopMockEndpoint"] = "Stop Mock",
        ["button.send"] = "Send",
        ["button.clearLog"] = "Clear Log",
        ["button.save"] = "Save",
        ["button.reload"] = "Reload",
        ["button.restoreDefaults"] = "Restore Defaults",
        ["mockEndpoint.status.idle.title"] = "Mock peer stopped",
        ["mockEndpoint.status.idle.detail"] = "Start a local {0} peer, then connect the session to loopback.",
        ["mockEndpoint.status.running.title"] = "{0} mock peer running",
        ["mockEndpoint.status.running.tcpDetail"] = "Listening on {0}:{1} and echoing stream traffic. Host was pinned to loopback for this session.",
        ["mockEndpoint.status.running.udpDetail"] = "Listening on {0}:{1} and echoing datagrams. Remote host was pinned to loopback for this session.",
        ["mockEndpoint.status.running.multicastDetail"] = "Joined {0}:{1} and replying back to the sender port. On one machine, multicast loopback can show both self traffic and peer echo.",
        ["mockEndpoint.status.unavailable.title"] = "Mock peer unavailable",
        ["mockEndpoint.status.unavailable.serialDetail"] = "Serial still needs a paired COM port or hardware loopback. An in-app peer is only available for TCP, UDP, and Multicast.",
        ["mockEndpoint.status.startFailed.title"] = "Mock peer start failed",
        ["mockEndpoint.status.stopFailed.title"] = "Mock peer stop failed",
        ["main.status.disconnected"] = "Disconnected",
        ["main.status.connecting"] = "Connecting",
        ["main.status.disconnecting"] = "Disconnecting",
        ["main.status.sending"] = "Sending",
        ["main.status.connected"] = "Connected",
        ["main.detail.readyFirstSession"] = "Ready for the first device session.",
        ["main.detail.negotiatingTransport"] = "Negotiating transport and starting receive loop.",
        ["main.detail.connectionFailedBeforeOnline"] = "Connection failed before the session came online.",
        ["main.detail.shuttingDownTransport"] = "Shutting down transport and cleaning background work.",
        ["main.detail.disconnectError"] = "Disconnect ended with an error.",
        ["main.detail.writingOutbound"] = "Writing the outbound frame to the active transport.",
        ["main.detail.readyNextMessage"] = "Session is online and ready for the next message.",
        ["main.detail.onlineAfterSendFailure"] = "Session remained online after the send failure.",
        ["main.detail.sessionNotActive"] = "Session is not active.",
        ["main.log.ready.title"] = "Ready",
        ["main.log.ready.message"] = "Choose a transport and open a session.",
        ["main.log.connectFailed.title"] = "Connect failed",
        ["main.log.disconnectFailed.title"] = "Disconnect failed",
        ["main.log.mockStarted.title"] = "Mock peer started",
        ["main.log.mockStopped.title"] = "Mock peer stopped",
        ["main.log.mockStopped.message"] = "The local {0} peer was stopped.",
        ["main.log.mockStartFailed.title"] = "Mock peer start failed",
        ["main.log.mockStopFailed.title"] = "Mock peer stop failed",
        ["main.log.sendFailed.title"] = "Send failed",
        ["main.log.consoleCleared.title"] = "Console cleared",
        ["main.log.consoleCleared.message"] = "New traffic will appear here.",
        ["settings.status.ready.title"] = "Persistence Ready",
        ["settings.status.ready.detail"] = "Review defaults, then save them to appsettings.json.",
        ["settings.status.saved.title"] = "Settings Saved",
        ["settings.status.saved.detail"] = "Updated {0} at {1}.",
        ["settings.status.saveFailed.title"] = "Save Failed",
        ["settings.status.reloaded.title"] = "Settings Reloaded",
        ["settings.status.reloaded.detail"] = "Loaded settings from {0}.",
        ["settings.status.reloadFailed.title"] = "Reload Failed",
        ["settings.status.defaultsRestored.title"] = "Defaults Restored",
        ["settings.status.defaultsRestored.detail"] = "The in-memory defaults are back. Save to persist them to appsettings.json.",
        ["session.state.connected"] = "Connected: {0}",
        ["session.state.disconnected"] = "Disconnected",
        ["session.detail.readyNextTransport"] = "Ready for the next transport session.",
        ["session.log.sessionOnline.title"] = "Session online",
        ["session.log.sessionOnline.message"] = "{0} is ready for traffic.",
        ["session.log.sessionOffline.title"] = "Session offline",
        ["session.log.sessionOffline.message"] = "Transport closed and receive loop stopped.",
        ["session.log.outbound.title"] = "Outbound message",
        ["session.log.outbound.message"] = "id={0}, body=\"{1}\"",
        ["session.log.inbound.title"] = "Inbound message",
        ["session.log.inbound.message"] = "id={0}, body=\"{1}\"",
        ["session.payload.fieldsSuffix"] = "fields[{0}]",
        ["session.payload.schemaErrorSuffix"] = "schema decode failed: {0}",
        ["session.log.receiveLoopStopped.title"] = "Receive loop stopped",
        ["session.event.connectAttempt.title"] = "Connect attempt",
        ["session.event.connectAttempt.message"] = "{0} ({1}/{2})",
        ["session.event.retryScheduled.title"] = "Retry scheduled",
        ["session.event.retryScheduled.message"] = "{0} attempt {1} failed, retry in {2:0} ms: {3}",
        ["session.event.connectSucceeded.title"] = "Connect succeeded",
        ["session.event.connectSucceeded.message"] = "{0} connected on attempt {1}.",
        ["session.event.operationFailed.title"] = "Connection operation failed",
        ["session.event.operationFailed.message"] = "{0} {1}: {2}",
        ["session.error.noActiveSession"] = "No active device session exists.",
        ["composer.error.invalidRawHex"] = "Raw hex mode expects hexadecimal byte pairs like \"DE AD BE EF\".",
        ["transport.detail.tcp"] = "TCP {0}:{1}",
        ["transport.detail.udp"] = "UDP local={0}, remote={1}:{2}",
        ["transport.detail.multicast"] = "Multicast {0}:{1}",
        ["transport.detail.serial"] = "Serial {0} @ {1}",
        ["transport.detail.generic"] = "Transport {0}"
    };

    /// <summary>
    /// KoreanStrings 값을 나타냅니다.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KoreanStrings = new Dictionary<string, string>
    {
        ["window.title"] = "CommLib 디바이스 랩",
        ["shell.appTitle"] = "CommLib 컨트롤 센터",
        ["shell.nav.deviceLab"] = "디바이스 랩",
        ["shell.nav.settings"] = "설정",
        ["deviceLab.hero.title"] = "디바이스 랩",
        ["deviceLab.hero.description"] = "공유된 MVVM 설정으로 TCP, UDP, 멀티캐스트, 시리얼 세션을 이 화면에서 열 수 있습니다.",
        ["deviceLab.section.sessionSetup"] = "세션 설정",
        ["deviceLab.section.transportSettings"] = "전송 설정",
        ["deviceLab.section.mockEndpoint"] = "\uBAA8\uC758 \uC5D4\uB4DC\uD3EC\uC778\uD2B8",
        ["deviceLab.section.messageComposer"] = "메시지 작성",
        ["deviceLab.section.liveLog"] = "실시간 로그",
        ["deviceLab.mockEndpoint.description"] = "\uD604\uC7AC \uC120\uD0DD\uD55C \uC804\uC1A1 \uBC29\uC2DD\uC744 \uBCC4\uB3C4 \uC11C\uBC84 \uD504\uB85C\uC138\uC2A4 \uC5C6\uC774 \uD14C\uC2A4\uD2B8\uD560 \uC218 \uC788\uB3C4\uB85D \uB85C\uCEEC peer\uB97C \uC5EC\uAE30\uC11C \uC2DC\uC791\uD569\uB2C8\uB2E4.",
        ["settings.hero.title"] = "설정",
        ["settings.hero.description"] = "기본값을 여기에서 편집하세요. 이 값들은 Device Lab과 공유되며 appsettings.json에 저장됩니다.",
        ["settings.section.general"] = "일반",
        ["settings.section.messageDefaults"] = "메시지 기본값",
        ["settings.section.transportPresets"] = "전송 프리셋",
        ["settings.section.persistence"] = "저장",
        ["settings.field.selectedTransport"] = "선택된 전송 방식",
        ["field.languageMode"] = "언어 모드",
        ["field.transport"] = "전송 방식",
        ["field.deviceId"] = "장치 ID",
        ["field.displayName"] = "표시 이름",
        ["field.defaultTimeoutMs"] = "기본 타임아웃(ms)",
        ["field.maxPendingRequests"] = "최대 대기 요청 수",
        ["field.host"] = "호스트",
        ["field.port"] = "포트",
        ["field.connectTimeoutMs"] = "연결 타임아웃(ms)",
        ["field.bufferSize"] = "버퍼 크기",
        ["field.localPort"] = "로컬 포트",
        ["field.remoteHost"] = "원격 호스트",
        ["field.remotePort"] = "원격 포트",
        ["field.groupAddress"] = "그룹 주소",
        ["field.ttl"] = "TTL",
        ["field.localInterface"] = "로컬 인터페이스",
        ["field.portName"] = "포트 이름",
        ["field.baudRate"] = "보레이트",
        ["field.dataBits"] = "데이터 비트",
        ["field.parity"] = "패리티",
        ["field.stopBits"] = "정지 비트",
        ["field.turnGapMs"] = "턴 갭(ms)",
        ["field.readBufferSize"] = "읽기 버퍼 크기",
        ["field.writeBufferSize"] = "쓰기 버퍼 크기",
        ["field.messageId"] = "메시지 ID",
        ["field.serializer"] = "직렬화기",
        ["field.body"] = "본문",
        ["check.noDelay"] = "네이글 비활성화(NoDelay)",
        ["check.loopback"] = "루프백 사용",
        ["check.halfDuplex"] = "반이중 타이밍 사용",
        ["button.connect"] = "연결",
        ["button.disconnect"] = "연결 해제",
        ["button.startMockEndpoint"] = "\uBAA8\uC758 peer \uC2DC\uC791",
        ["button.stopMockEndpoint"] = "\uBAA8\uC758 peer \uC911\uC9C0",
        ["button.send"] = "전송",
        ["button.clearLog"] = "로그 지우기",
        ["button.save"] = "저장",
        ["button.reload"] = "다시 불러오기",
        ["button.restoreDefaults"] = "기본값 복원",
        ["mockEndpoint.status.idle.title"] = "\uBAA8\uC758 peer \uC911\uC9C0\uB428",
        ["mockEndpoint.status.idle.detail"] = "\uB85C\uCEEC {0} peer\uB97C \uC2DC\uC791\uD55C \uB4A4 loopback\uC73C\uB85C \uC138\uC158\uC744 \uC5F0\uACB0\uD558\uC138\uC694.",
        ["mockEndpoint.status.running.title"] = "{0} \uBAA8\uC758 peer \uC2E4\uD589 \uC911",
        ["mockEndpoint.status.running.tcpDetail"] = "{0}:{1}\uC5D0\uC11C \uC2A4\uD2B8\uB9BC \uD2B8\uB798\uD53D echo\uB97C \uB4E3\uACE0 \uC788\uC2B5\uB2C8\uB2E4. \uD604 \uC138\uC158\uC740 host\uAC00 loopback\uC73C\uB85C \uACE0\uC815\uB429\uB2C8\uB2E4.",
        ["mockEndpoint.status.running.udpDetail"] = "{0}:{1}\uC5D0\uC11C datagram echo\uB97C \uB4E3\uACE0 \uC788\uC2B5\uB2C8\uB2E4. \uD604 \uC138\uC158\uC740 remote host\uAC00 loopback\uC73C\uB85C \uACE0\uC815\uB429\uB2C8\uB2E4.",
        ["mockEndpoint.status.running.multicastDetail"] = "{0}:{1}\uC5D0 \uCC38\uC5EC\uD558\uACE0 \uBCF4\uB0B8 \uC0AC\uB78C\uC758 port\uB85C \uB2E4\uC2DC \uBCF4\uB0C5\uB2C8\uB2E4. \uD55C \uBA38\uC2E0\uC5D0\uC11C\uB294 multicast loopback \uB54C\uBB38\uC5D0 \uC790\uCCB4 \uD2B8\uB798\uD53D\uACFC peer echo\uAC00 \uD568\uAED8 \uBCF4\uC77C \uC218 \uC788\uC2B5\uB2C8\uB2E4.",
        ["mockEndpoint.status.unavailable.title"] = "\uBAA8\uC758 peer \uC0AC\uC6A9 \uBD88\uAC00",
        ["mockEndpoint.status.unavailable.serialDetail"] = "Serial\uC740 \uC9DD\uC744 \uC774\uB8EC COM port \uB610\uB294 \uD558\uB4DC\uC6E8\uC5B4 loopback\uC774 \uD544\uC694\uD569\uB2C8\uB2E4. \uC571 \uB0B4 peer\uB294 TCP, UDP, Multicast\uC5D0\uC11C\uB9CC \uC9C0\uC6D0\uB429\uB2C8\uB2E4.",
        ["mockEndpoint.status.startFailed.title"] = "\uBAA8\uC758 peer \uC2DC\uC791 \uC2E4\uD328",
        ["mockEndpoint.status.stopFailed.title"] = "\uBAA8\uC758 peer \uC911\uC9C0 \uC2E4\uD328",
        ["main.status.disconnected"] = "연결 해제됨",
        ["main.status.connecting"] = "연결 중",
        ["main.status.disconnecting"] = "연결 해제 중",
        ["main.status.sending"] = "전송 중",
        ["main.status.connected"] = "연결됨",
        ["main.detail.readyFirstSession"] = "첫 장치 세션을 시작할 준비가 되었습니다.",
        ["main.detail.negotiatingTransport"] = "전송 방식을 협상하고 수신 루프를 시작하는 중입니다.",
        ["main.detail.connectionFailedBeforeOnline"] = "세션이 올라오기 전에 연결이 실패했습니다.",
        ["main.detail.shuttingDownTransport"] = "전송을 종료하고 백그라운드 작업을 정리하는 중입니다.",
        ["main.detail.disconnectError"] = "연결 해제 중 오류가 발생했습니다.",
        ["main.detail.writingOutbound"] = "활성 전송 경로로 outbound frame을 기록하는 중입니다.",
        ["main.detail.readyNextMessage"] = "세션이 온라인 상태이며 다음 메시지를 보낼 준비가 되었습니다.",
        ["main.detail.onlineAfterSendFailure"] = "전송 실패 이후에도 세션은 온라인 상태를 유지했습니다.",
        ["main.detail.sessionNotActive"] = "현재 활성 세션이 없습니다.",
        ["main.log.ready.title"] = "준비됨",
        ["main.log.ready.message"] = "전송 방식을 고르고 세션을 여세요.",
        ["main.log.connectFailed.title"] = "연결 실패",
        ["main.log.disconnectFailed.title"] = "연결 해제 실패",
        ["main.log.mockStarted.title"] = "\uBAA8\uC758 peer \uC2DC\uC791",
        ["main.log.mockStopped.title"] = "\uBAA8\uC758 peer \uC911\uC9C0",
        ["main.log.mockStopped.message"] = "\uB85C\uCEEC {0} peer\uB97C \uC911\uC9C0\uD588\uC2B5\uB2C8\uB2E4.",
        ["main.log.mockStartFailed.title"] = "\uBAA8\uC758 peer \uC2DC\uC791 \uC2E4\uD328",
        ["main.log.mockStopFailed.title"] = "\uBAA8\uC758 peer \uC911\uC9C0 \uC2E4\uD328",
        ["main.log.sendFailed.title"] = "전송 실패",
        ["main.log.consoleCleared.title"] = "콘솔 초기화",
        ["main.log.consoleCleared.message"] = "새 트래픽이 여기에 표시됩니다.",
        ["settings.status.ready.title"] = "저장 준비 완료",
        ["settings.status.ready.detail"] = "기본값을 검토한 뒤 appsettings.json에 저장하세요.",
        ["settings.status.saved.title"] = "설정 저장 완료",
        ["settings.status.saved.detail"] = "{0} 파일을 {1}에 업데이트했습니다.",
        ["settings.status.saveFailed.title"] = "저장 실패",
        ["settings.status.reloaded.title"] = "설정 다시 불러옴",
        ["settings.status.reloaded.detail"] = "{0}에서 설정을 불러왔습니다.",
        ["settings.status.reloadFailed.title"] = "다시 불러오기 실패",
        ["settings.status.defaultsRestored.title"] = "기본값 복원 완료",
        ["settings.status.defaultsRestored.detail"] = "메모리상의 기본값으로 되돌렸습니다. 영구 반영하려면 저장하세요.",
        ["session.state.connected"] = "연결됨: {0}",
        ["session.state.disconnected"] = "연결 해제됨",
        ["session.detail.readyNextTransport"] = "다음 전송 세션을 시작할 준비가 되었습니다.",
        ["session.log.sessionOnline.title"] = "세션 온라인",
        ["session.log.sessionOnline.message"] = "{0} 세션이 송수신 준비를 마쳤습니다.",
        ["session.log.sessionOffline.title"] = "세션 오프라인",
        ["session.log.sessionOffline.message"] = "전송이 종료되고 수신 루프가 중단되었습니다.",
        ["session.log.outbound.title"] = "아웃바운드 메시지",
        ["session.log.outbound.message"] = "id={0}, 본문=\"{1}\"",
        ["session.log.inbound.title"] = "인바운드 메시지",
        ["session.log.inbound.message"] = "id={0}, 본문=\"{1}\"",
        ["session.log.receiveLoopStopped.title"] = "수신 루프 중단",
        ["session.event.connectAttempt.title"] = "연결 시도",
        ["session.event.connectAttempt.message"] = "{0} ({1}/{2})",
        ["session.event.retryScheduled.title"] = "재시도 예약됨",
        ["session.event.retryScheduled.message"] = "{0}의 {1}번째 시도가 실패했습니다. {2:0}ms 후 다시 시도합니다: {3}",
        ["session.event.connectSucceeded.title"] = "연결 성공",
        ["session.event.connectSucceeded.message"] = "{0} 장치가 {1}번째 시도에서 연결되었습니다.",
        ["session.event.operationFailed.title"] = "연결 작업 실패",
        ["session.event.operationFailed.message"] = "{0} {1}: {2}",
        ["session.error.noActiveSession"] = "현재 활성 디바이스 세션이 없습니다.",
        ["composer.error.invalidRawHex"] = "Raw hex 모드는 \"DE AD BE EF\" 같은 16진수 바이트 쌍이 필요합니다.",
        ["transport.detail.tcp"] = "TCP {0}:{1}",
        ["transport.detail.udp"] = "UDP 로컬={0}, 원격={1}:{2}",
        ["transport.detail.multicast"] = "멀티캐스트 {0}:{1}",
        ["transport.detail.serial"] = "시리얼 {0} @ {1}",
        ["transport.detail.generic"] = "전송 {0}",
        ["session.payload.fieldsSuffix"] = "\uD544\uB4DC[{0}]",
        ["session.payload.schemaErrorSuffix"] = "\uC2A4\uD0A4\uB9C8 \uD574\uC11D \uC2E4\uD328: {0}"
    };

    /// <summary>
    /// _currentLanguage 값을 나타냅니다.
    /// </summary>
    private AppLanguageMode _currentLanguage;

    /// <summary>
    /// LanguageChanged 이벤트입니다.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>
    /// CurrentLanguage 값을 가져옵니다.
    /// </summary>
    public AppLanguageMode CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value)
            {
                return;
            }

            _currentLanguage = value;
            // 코드 기반 View는 바인딩만으로 모든 정적 문구가 자동 갱신되지 않으므로
            // 언어 변경 이벤트를 올려 각 화면이 등록해 둔 텍스트 업데이트를 다시 실행하게 한다.
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Get 작업을 수행합니다.
    /// </summary>
    public string Get(string key)
    {
        return GetStrings().TryGetValue(key, out var value) ? value : key;
    }

    /// <summary>
    /// Format 작업을 수행합니다.
    /// </summary>
    public string Format(string key, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }

    /// <summary>
    /// GetShellPageLabel 작업을 수행합니다.
    /// </summary>
    public string GetShellPageLabel(ShellPageKind kind)
    {
        // 자주 쓰는 shell/transport/language 라벨은 별도 key 조회보다 switch helper가 더 읽기 쉬워서
        // 작은 파생 문구는 여기서 계산형으로 제공한다.
        return (CurrentLanguage, kind) switch
        {
            (AppLanguageMode.Korean, ShellPageKind.DeviceLab) => "디바이스 랩",
            (AppLanguageMode.Korean, ShellPageKind.Settings) => "설정",
            (_, ShellPageKind.DeviceLab) => "Device Lab",
            (_, ShellPageKind.Settings) => "Settings",
            _ => kind.ToString()
        };
    }

    /// <summary>
    /// GetShellPageSubtitle 작업을 수행합니다.
    /// </summary>
    public string GetShellPageSubtitle(ShellPageKind kind)
    {
        return (CurrentLanguage, kind) switch
        {
            (AppLanguageMode.Korean, ShellPageKind.DeviceLab) => "실시간 전송 세션을 운영합니다",
            (AppLanguageMode.Korean, ShellPageKind.Settings) => "appsettings.json을 편집하고 저장합니다",
            (_, ShellPageKind.DeviceLab) => "Operate live transport sessions",
            (_, ShellPageKind.Settings) => "Edit and persist appsettings.json",
            _ => kind.ToString()
        };
    }

    /// <summary>
    /// GetTransportLabel 작업을 수행합니다.
    /// </summary>
    public string GetTransportLabel(TransportKind kind)
    {
        return (CurrentLanguage, kind) switch
        {
            (AppLanguageMode.Korean, TransportKind.Tcp) => "TCP",
            (AppLanguageMode.Korean, TransportKind.Udp) => "UDP",
            (AppLanguageMode.Korean, TransportKind.Multicast) => "멀티캐스트",
            (AppLanguageMode.Korean, TransportKind.Serial) => "시리얼",
            (_, TransportKind.Tcp) => "TCP",
            (_, TransportKind.Udp) => "UDP",
            (_, TransportKind.Multicast) => "Multicast",
            (_, TransportKind.Serial) => "Serial",
            _ => kind.ToString()
        };
    }

    /// <summary>
    /// GetTransportSubtitle 작업을 수행합니다.
    /// </summary>
    public string GetTransportSubtitle(TransportKind kind)
    {
        return (CurrentLanguage, kind) switch
        {
            (AppLanguageMode.Korean, TransportKind.Tcp) => "신뢰성 있는 소켓 세션",
            (AppLanguageMode.Korean, TransportKind.Udp) => "빠른 데이터그램 전송",
            (AppLanguageMode.Korean, TransportKind.Multicast) => "그룹 브로드캐스트 트래픽",
            (AppLanguageMode.Korean, TransportKind.Serial) => "COM과 루프백 링크",
            (_, TransportKind.Tcp) => "Reliable socket session",
            (_, TransportKind.Udp) => "Fast datagram transport",
            (_, TransportKind.Multicast) => "Group broadcast traffic",
            (_, TransportKind.Serial) => "COM and loopback links",
            _ => kind.ToString()
        };
    }

    /// <summary>
    /// GetSerializerLabel 작업을 수행합니다.
    /// </summary>
    public string GetSerializerLabel(string serializerType)
    {
        return (CurrentLanguage, serializerType) switch
        {
            (AppLanguageMode.Korean, SerializerTypes.AutoBinary) => "텍스트 (AutoBinary)",
            (AppLanguageMode.Korean, SerializerTypes.RawHex) => "Raw Hex",
            (_, SerializerTypes.AutoBinary) => "Text (AutoBinary)",
            (_, SerializerTypes.RawHex) => "Raw Hex",
            _ => serializerType
        };
    }

    /// <summary>
    /// GetSerializerSubtitle 작업을 수행합니다.
    /// </summary>
    public string GetSerializerSubtitle(string serializerType)
    {
        return (CurrentLanguage, serializerType) switch
        {
            (AppLanguageMode.Korean, SerializerTypes.AutoBinary) => "UTF-8 본문을 현재 예제 serializer 형식으로 보냅니다.",
            (AppLanguageMode.Korean, SerializerTypes.RawHex) => "공백 허용 16진수 바이트를 raw payload로 보냅니다.",
            (_, SerializerTypes.AutoBinary) => "Sends UTF-8 body text through the current example serializer format.",
            (_, SerializerTypes.RawHex) => "Sends whitespace-tolerant hexadecimal bytes as the raw payload.",
            _ => serializerType
        };
    }

    /// <summary>
    /// GetLanguageLabel 작업을 수행합니다.
    /// </summary>
    public string GetLanguageLabel(AppLanguageMode mode)
    {
        return (CurrentLanguage, mode) switch
        {
            (AppLanguageMode.Korean, AppLanguageMode.English) => "영어",
            (AppLanguageMode.Korean, AppLanguageMode.Korean) => "한국어",
            (_, AppLanguageMode.English) => "English",
            (_, AppLanguageMode.Korean) => "Korean",
            _ => mode.ToString()
        };
    }

    /// <summary>
    /// GetStrings 작업을 수행합니다.
    /// </summary>
    private IReadOnlyDictionary<string, string> GetStrings()
    {
        return CurrentLanguage == AppLanguageMode.Korean ? KoreanStrings : EnglishStrings;
    }
}