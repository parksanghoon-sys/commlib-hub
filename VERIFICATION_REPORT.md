# CommLib 구현 검증 보고서

> 검증일: 2026-05-20
> 검증 모델: Claude Sonnet 4.6 (파이프라인) + Opus 4.7 (설계 재검증)
> 대상 브랜치: `main` (로컬 워킹트리 포함)
> 연관 문서: [DESIGN_REVIEW.md](DESIGN_REVIEW.md)

---

## 1. 7단계 파이프라인 요약

| Phase | 도구 | 결과 | 세부 사항 |
|-------|------|------|---------|
| 1. Build | `dotnet build` | ✅ PASS | 오류 0, 경고 0 |
| 2. Diagnostics | Roslyn MCP | ✅ PASS | warning/error 없음 (CS8019·CS8933 89건은 전부 `hidden` + 생성 파일) |
| 3. Antipatterns | Roslyn MCP | ✅ PASS | 위반 0건 |
| 4. Tests | `dotnet test` | ✅ PASS | 248개 통과 (Unit 114 + Infrastructure 134), 실패 0 |
| 5. Security | 수동 검토 | ✅ PASS | 하드코딩 시크릿 없음 |
| 6. Format | `dotnet format` | ✅ PASS | 포맷 드리프트 없음 |
| 7. Diff Review | `git diff` | ⚠️ WARN | `DeviceSession.cs` 미스테이지드 변경 존재 |

**전체 판정: READY FOR REVIEW** (조건부 — 하단 참조)

---

## 2. Phase별 상세 결과

### Phase 1: Build

```
빌드했습니다.
경고 0개 / 오류 0개
경과 시간: 00:00:11.01
```

### Phase 2: Diagnostics

Roslyn 분석기 진단 89건 전부 `hidden` 심각도(CS8019/CS8933).
- CS8019 — 불필요한 using 지시문 (생성 파일 `.obj/` 포함)
- CS8933 — 전역 using으로 이미 포함된 네임스페이스

실제 소스(`src/`, `tests/`) 내 warning 또는 error 없음.

### Phase 3: Antipatterns

```
Count: 0 / TotalFound: 0
```

async void, sync-over-async, `new HttpClient()`, `DateTime.Now`, 광범위 catch, 로깅 문자열 보간, CancellationToken 누락 — 전부 없음.

### Phase 4: Tests

```
CommLib.Unit.Tests        : 114개 통과, 0개 실패, 0개 건너뜀 (258ms)
CommLib.Infrastructure.Tests: 134개 통과, 0개 실패, 0개 건너뜀 (735ms)
합계                       : 248개 통과
```

### Phase 5: Security

`src/` 전체 `password`, `secret`, `apikey`, `api_key`, `connectionstring` 키워드 검색 결과 없음.

주의사항 (정보성):
- TCP/UDP/Serial 전송은 현재 TLS 없이 평문 통신 — 의도된 설계이며 DECISIONS.md에 명시됨
- 외부 네트워크 노출 시 상위 레이어에서 TLS 래핑 필요

### Phase 6: Format

`dotnet format --verify-no-changes` 출력 없음 = 포맷 완전 일치.

### Phase 7: Diff Review

**미스테이지드 변경 — `DeviceSession.cs`**

```diff
// src/CommLib.Application/Sessions/DeviceSession.cs (unstaged)
- if (_pendingResponses.Count >= _maxPendingRequests)
- {
-     var exception = new InvalidOperationException("Pending request limit has been reached.");
-     pendingEntry.Fail(exception);
-     return Task.FromException(exception);
- }
- if (!_outbound.Writer.TryWrite(request))
- {
-     var exception = new InvalidOperationException("Outbound queue is full.");
-     pendingEntry.Fail(exception);
-     return Task.FromException(exception);
- }
+ if (!TryRegisterPendingRequest(request, pendingEntry, out var registrationFailure))
+     return Task.FromException(registrationFailure!);
```

평가: 내부 로직을 `TryRegisterPendingRequest` / `CompleteSend` private helper로 추출하는 리팩토링. 동작 변화 없음. 이 변경을 커밋할 의사가 있으면 스테이지 후 커밋, 아니면 `git checkout` 권장.

또한 `.gitignore` 스테이지드 변경 1건 존재.

---

## 3. 설계 이슈 vs 구현 교차검증

DESIGN_REVIEW.md의 설계 이슈 7개를 코드에서 직접 확인한 결과입니다.

### 문제 1 — `IDeviceSession`이 내부 수명주기 메서드 공개

**코드 위치**: `src/CommLib.Domain/Messaging/IDeviceSession.cs:39, 45, 52`

**확인**: ✅ 실제 존재

**추가 발견 — 테스트 코드까지 오염됨**:

테스트가 `GetSession()` 반환값으로 내부 메서드를 직접 호출하고 있음:

```
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:214   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:286   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:422   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:510   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:551   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:711   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:800   session.TryDequeueOutbound(out _)
tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs:1085  session.TryDequeueOutbound(out _)

tests/CommLib.Unit.Tests/DeviceSessionTests.cs:32    session.FailPendingResponses(...)
tests/CommLib.Unit.Tests/DeviceSessionTests.cs:68    session.TryDequeueOutbound(...)
tests/CommLib.Unit.Tests/DeviceSessionTests.cs:106   session.TryCompleteResponse(...)
tests/CommLib.Unit.Tests/DeviceSessionTests.cs:125   session.TryCompleteResponse(...)
tests/CommLib.Unit.Tests/DeviceSessionTests.cs:144   session.TryCompleteResponse(...)
tests/CommLib.Unit.Tests/DeviceSessionTests.cs:161   session.TryCompleteResponse(...)
tests/CommLib.Unit.Tests/DeviceSessionTests.cs:178   session.TryCompleteResponse(...)
```

→ `IDeviceSession` 분리 시 위 호출 지점 모두 리팩토링 필요. 예상보다 영향 범위가 넓음.

### 문제 2 — `ConnectionManager`가 `DeviceSession` 직접 생성

**코드 위치**: `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs:127`

```csharp
var session = new DeviceSession(profile.DeviceId, profile.RequestResponse);
```

**확인**: ✅ 실제 존재. Factory 없음.

### 문제 3 — 이중 큐 왕복 (`SendFromSessionAsync`)

**코드 위치**: `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs:329-345`

**확인**: ✅ 실제 존재.

### 문제 4 — `ProtocolOptions` discriminated union

**코드 위치**: `src/CommLib.Domain/Configuration/ProtocolOptions.cs`

**확인**: ✅ 실제 존재. `BinaryFrame` 속성이 `Type == "LengthPrefixed"`일 때도 항상 포함됨.

### 문제 5 — `IConnectionEventSink`가 Infrastructure 네임스페이스

**코드 위치**: `src/CommLib.Infrastructure/Sessions/IConnectionEventSink.cs:1`

```csharp
namespace CommLib.Infrastructure.Sessions; // ← Infrastructure에 위치
public interface IConnectionEventSink { ... }
internal sealed class NullConnectionEventSink : IConnectionEventSink { ... }
```

**확인**: ✅ 실제 존재. `NullConnectionEventSink`가 `internal`이라 사용자 확장도 불편함.

### 문제 6 — fire-and-forget Task

**코드 위치**: `src/CommLib.Application/Sessions/DeviceSession.cs:189`

```csharp
_ = HandleTimeoutAsync(request.CorrelationId, effectiveResponseTimeout, pendingEntry, cancellationToken);
```

**확인**: ✅ 실제 존재.

### 문제 7 — outbound 큐 용량 하드코딩

**코드 위치**: `src/CommLib.Application/Sessions/DeviceSession.cs:12`

```csharp
private readonly Channel<IMessage> _outbound = Channel.CreateBounded<IMessage>(64);
```

**확인**: ✅ 실제 존재.

---

## 4. 설계 이슈 종합 검증표

| # | 문제 | 코드 존재 | 테스트 영향 | 우선순위 |
|---|------|---------|------------|---------|
| 1 | `IDeviceSession` 내부 메서드 공개 | ✅ 확인 | 16개 호출 지점 오염 | 🔴 높음 |
| 2 | `DeviceSession` 직접 생성 (factory 부재) | ✅ 확인 | 간접 영향 | 🟡 중간 |
| 3 | `SendFromSessionAsync` 이중 큐 왕복 | ✅ 확인 | 없음 | 🟡 중간 |
| 4 | `ProtocolOptions` discriminated union | ✅ 확인 | 없음 | 🟡 중간 |
| 5 | `IConnectionEventSink` Infrastructure 위치 | ✅ 확인 | 없음 | 🟡 중간 |
| 6 | fire-and-forget `HandleTimeoutAsync` | ✅ 확인 | 없음 | 🟢 낮음 |
| 7 | outbound 큐 용량 하드코딩 | ✅ 확인 | 없음 | 🟢 낮음 |

**7개 이슈 모두 코드에서 직접 확인됨. 이론적 추측이 아닌 실제 구현 문제.**

---

## 5. 조건부 통과 항목

1. **`DeviceSession.cs` 미스테이지드 변경 처리**
   - 의도한 리팩토링이면 → `git add` + 커밋
   - 임시 변경이면 → `git checkout src/CommLib.Application/Sessions/DeviceSession.cs`

2. **설계 이슈 1 개선 계획 수립**
   - `IDeviceSession` 분리가 테스트 16개 호출 지점에 영향을 주므로, 수정 전 범위를 인지하고 진행 필요

---

## 6. 권장 다음 단계

```
1. git add src/CommLib.Application/Sessions/DeviceSession.cs
   git commit -m "refactor(session): extract TryRegisterPendingRequest and CompleteSend"

2. IDeviceSession 인터페이스 분리 (문제 1)
   → IDeviceSession (public: DeviceId, Send x2)
   → IDeviceSessionLifecycle (internal: TryCompleteResponse, FailPendingResponses, TryDequeueOutbound)

3. ConnectionManagerTests.cs의 TryDequeueOutbound 호출을
   internal 인터페이스 또는 friend assembly 경계로 이동
```

---

## 7. Codex 응답 및 현재 판정

> 응답일: 2026-05-20
> 기준 브랜치: `codex/span-minimal-copy-pipeline`
> 기준 상태: `DESIGN_REVIEW.md` 후속 구현, 세션 경계 정리, span/minimal-copy pipeline 구현 이후

### 7.1 종합 의견

이 문서의 검증 방향 자체는 유효했다. 특히 공개 `IDeviceSession`이 런타임 lifecycle 메서드까지 노출하고, `ConnectionManager.SendAsync(...)`가 `DeviceSession` outbound queue를 거쳐 다시 dequeue하는 구조를 지적한 부분은 실제 correctness risk였다.

다만 이 보고서는 작성 시점의 코드 상태를 기준으로 하므로, 현재 브랜치 기준으로는 일부 항목이 이미 해결되어 stale 상태가 되었다. 현재 판단은 다음과 같다.

### 7.2 항목별 현재 상태

| # | 보고서 항목 | 현재 판정 | 근거 / 처리 |
|---|---|---|---|
| 1 | `IDeviceSession` 내부 lifecycle 메서드 공개 | 해결됨 | `IDeviceSession`은 현재 `DeviceId`, `Send(IMessage)`, `Send<TRequest,TResponse>(...)`만 노출한다. `TryCompleteResponse`, `FailPendingResponses`, `TryDequeueOutbound`는 공개 계약에서 제거되었다. |
| 2 | `ConnectionManager`가 `DeviceSession` 직접 생성 | 보류 가능 | 현재는 `ConnectionManager` 내부 런타임 상태로 직접 생성하는 것이 과한 문제는 아니다. 외부 확장 지점이나 테스트 대체 요구가 생기기 전까지 factory 도입은 비용 대비 이점이 작다. |
| 3 | `SendFromSessionAsync` 이중 send 경로 | 해결됨 | `SendFromSessionAsync`와 outbound queue hop이 제거되었고, `ConnectionManager.SendAsync(...)`는 `TransportMessageSender`를 통해 직접 전송한다. |
| 4 | `ProtocolOptions` option bag / discriminated union 문제 | 보류 | `LengthPrefixed`와 `BinaryFrame` 두 계열만 있는 현재 상태에서는 typed option hierarchy로 바꾸기보다 validator/factory guardrail을 유지하는 편이 단순하다. 세 번째 프로토콜 계열이 생길 때 재검토하는 것이 맞다. |
| 5 | `IConnectionEventSink`가 Infrastructure namespace에 위치 | 보류 | 운영 진단 surface로 의미가 있으나, 별도 package/API boundary를 지금 확정하기에는 deployment 요구가 부족하다. 다음 작업 후보는 기존 seam을 재사용한 production diagnostics slice다. |
| 6 | `HandleTimeoutAsync` fire-and-forget | 낮은 우선순위로 보류 | 현재 timeout task는 pending entry의 cancellation token과 `TaskCompletionSource`를 통해 수명과 실패 처리를 통제한다. 별도 background task tracking까지 넣을 만큼의 실제 장애 근거는 아직 없다. |
| 7 | outbound queue capacity hardcoding | 해결됨 | `DeviceSession` outbound queue 자체가 제거되었다. 따라서 capacity `64` 문제도 함께 사라졌다. |

### 7.3 현재 코드 기준 핵심 결론

- 보고서의 Blocker급 핵심 이슈는 `IDeviceSession` 공개 계약과 outbound send queue coupling이었고, 둘 다 현재 구현에서 해소되었다.
- 남은 항목들은 즉시 결함이라기보다 설계 확장 시점의 판단 항목이다.
- 특히 `ProtocolOptions`, `IConnectionEventSink`, timeout task tracking은 지금 넓히면 구조가 더 복잡해질 수 있으므로 deferred backlog로 관리하는 편이 낫다.
- span/minimal-copy pipeline은 별도 additive fast path로 구현되었고, 기존 `IProtocol` / `ISerializer` 호환성은 유지되었다.

### 7.4 검증 근거

현재 브랜치에서 다음 검증이 통과했다.

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests|MessageFrameDecoderTests|MessageFrameEncoderTests|TransportMessageReceiverTests|RawHexSerializerTests|NoOpSerializerTests"
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore
dotnet build commlib-codex-full.sln --configuration Release --no-restore
git diff --check
```

결과:

- focused span pipeline tests: 50 passed
- infrastructure tests: 147 passed
- unit tests: 111 passed
- solution Release build: warning 0, error 0
- `git diff --check`: whitespace error 없음

### 7.5 다음 권장 작업

1. 이 보고서는 현재 상태 기준으로 "검증 완료 + 일부 stale" 문서로 취급한다.
2. 새 Blocker로 다시 올릴 항목은 현재 없다.
3. 다음 실제 구현 후보는 `IConnectionEventSink`를 활용한 production diagnostics slice다.
4. `ProtocolOptions` 재설계, event sink package boundary 이동, timeout task tracking 강화는 구체적인 외부 요구나 장애 사례가 생길 때 별도 slice로 다룬다.
