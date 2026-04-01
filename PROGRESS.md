# 진행 현황

## 2026-03-25

### 1. 오늘 확인한 내용
- [x] 저장소 기본 구조 확인
- [x] `src/CommLib.Domain`, `src/CommLib.Application`, `src/CommLib.Infrastructure`, `src/CommLib.Hosting` 레이어 구성 확인
- [x] 테스트 프로젝트 구성과 현재 테스트 파일 배치 확인
- [x] 현재 구현 베이스라인과 워크트리 상태 확인
- [x] `obj/`, `bin/` 같은 생성 산출물은 커밋 대상에서 제외해야 함을 확인

### 2. 오늘 완료한 작업
- [x] C#/.NET 프로젝트용 `.gitignore` 추가
- [x] 솔루션 파일 `commlib-codex-full.sln` 추가
- [x] 주요 Domain / Application / Infrastructure / Hosting 코드에 XML 주석 작성
- [x] 테스트 코드에도 XML 주석 작성
- [x] XML 주석을 한글 기준으로 정리
- [x] 새 C# 파일과 새 클래스 작성 시 한글 XML 주석을 남기도록 `AGENT.md`와 구현 스킬 규칙 보강
- [x] 각 프로젝트의 목적이 보이도록 `.csproj` 설명 추가
- [x] 원격 저장소 `commlib-hub` 연결 및 `main` 브랜치 최초 push 완료

### 3. 테스트 관련 진행 내용
- [x] 테스트를 더 작은 단위로 쪼개야 한다는 기준을 다시 정리
- [x] `ConnectionManagerTests`를 fake 기반의 더 작은 단위 테스트로 분리 시작
- [x] `DeviceBootstrapperTests`에 비활성 프로필 미연결 검증 추가
- [x] `DeviceBootstrapperTests`에 취소 토큰 전달 검증 추가
- [x] `ConnectionManagerTests`에 같은 장치 재연결 시 세션 교체 여부 검증 추가
- [x] `TransportFactoryTests`는 인프라 구현 테스트로 유지하는 방향 정리

### 4. 테스트 프로젝트 구조 관련 판단
- [x] `ConnectionManager`, `TransportFactory` 테스트는 인프라 구현 테스트이므로 `CommLib.Infrastructure.Tests`로 두는 방향이 더 적절하다고 판단
- [x] `DeviceBootstrapper`, `DeviceProfileMapper`, `DeviceProfileValidator`는 애플리케이션 성격이 강하므로 `Infrastructure.Tests`로 옮기지 않는 편이 더 적절하다고 판단
- [x] `tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 재생성
- [x] `tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs` 재배치 시작
- [x] `tests/CommLib.Infrastructure.Tests/TransportFactoryTests.cs` 재배치 시작
- [x] `commlib-codex-full.sln`에 `CommLib.Infrastructure.Tests` 프로젝트 재등록

### 5. 확인한 문제 / 리스크
- [x] `DeviceProfileMapper.cs`에 `CS8506` 컴파일 오류가 있어 전체 테스트 실행이 현재 막혀 있음
- [x] 일부 한글 주석은 콘솔 출력 인코딩 때문에 깨져 보이지만 파일 자체는 한글 주석 기준으로 유지 중
- [x] 아직 테스트 구조 재배치 작업이 끝나지 않아 워크트리에 미커밋 변경이 남아 있음

### 6. 추가 완료 작업
- [x] `DeviceProfileMapper.cs`의 `CS8506` 빌드 오류 수정
- [x] `DeviceProfileValidatorTests`를 현재 정적 검증 API에 맞게 정리
- [x] `ConnectionManagerTests`에 타 장치 세션 유지 검증 추가
- [x] `ConnectionManagerTests`에 전송 생성 실패 시 세션 미등록 검증 추가
- [x] `DeviceSessionTests` 신규 추가
- [x] `SendResultTests` 신규 추가
- [x] `TransportFactoryTests`에 새 인스턴스 생성 검증 추가
- [x] `TransportFactoryTests`에 미지원 형식 예외 메시지 검증 추가

### 7. 검증 결과
- [x] `dotnet build commlib-codex-full.sln` 통과
- [x] `dotnet test commlib-codex-full.sln` 통과
- [x] `CommLib.Unit.Tests` 13개 테스트 통과 확인
- [x] `CommLib.Infrastructure.Tests` 14개 테스트 통과 확인

### 8. 최근 커밋
- [x] `21553db` `wip(test): 인프라 테스트 재배치 진행`
- [x] `09af57d` `fix(application): restore mapper build and validator test`
- [x] `525b4ce` `test: add session and connection edge case coverage`
- [x] `f7753e9` `test(infra): tighten transport factory coverage`

### 9. 현재 상태
- [x] 현재 워킹트리는 추적 대상 변경 없이 깨끗함
- [x] `CommLib.Infrastructure.Tests` / `CommLib.Unit.Tests` 분리 기준이 코드와 테스트에 반영됨

### 10. 다음 작업 후보
- [ ] `DeviceBootstrapper` 추가 경계 조건 검토
- [ ] `DeviceProfileValidator` 보안성 검증 항목 확장
- [ ] `CommLib.Infrastructure.Tests`와 `CommLib.Unit.Tests` 경계 재검토
- [ ] 브랜치 단위 `push/PR` 흐름 시범 적용
## 2026-03-26

### 1. 오늘 완료한 작업
- [x] `DeviceSessionTests`에 요청 큐 포화 상태에서 `ResponseTask`가 미완료 상태로 유지되는지 검증하는 테스트 추가
- [x] 요청 전송 실패 시 `SendCompletedTask`와 `ResponseTask`의 상태가 분리되어 유지되는 현재 동작을 테스트로 문서화

### 2. 검증 결과
- [x] `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj` 통과
- [x] `CommLib.Unit.Tests` 22개 테스트 통과 확인

### 3. 현재 남은 상태
- [x] 워크트리 기준 미커밋 변경은 `tests/CommLib.Unit.Tests/DeviceSessionTests.cs` 1건만 존재
- [x] 최근 테스트 작업 흐름은 `6766e9b`, `5c053ff`, `93cc8bd`, `a805b21` 커밋까지 이어진 상태
- [x] `PROGRESS.md`에 오늘 기준 진행 상황과 후속 작업 정리

### 4. 남은 잔여 일감
- [ ] `DeviceSession`의 큐 포화 시 요청 추적 엔트리가 실제로 누수되지 않는지 구현과 테스트를 함께 재점검
- [ ] `DeviceBootstrapper` 경계 조건 테스트를 더 보강해 연결 순서, 예외 전파, 취소 흐름 누락 케이스 점검
- [ ] `DeviceProfileValidator` 검증 규칙의 경계값/조합 케이스 추가
- [ ] `CommLib.Unit.Tests`와 `CommLib.Infrastructure.Tests` 사이 책임 경계를 다시 점검해 테스트 위치가 흔들리는 케이스 정리
- [ ] 현재 단일 미커밋 변경을 기준으로 다음 커밋 메시지 단위 정리 후 `push/PR` 흐름 진행
### 5. 실제 구현 / TDD 남은 양 추정
- [x] 추정 기준: 현재 소스와 테스트 커버리지를 기준으로 남은 범위를 체감 공정으로 정리
- [ ] 실제 구현 남은 양: 전체 프로젝트 기준으로는 기본 골격이 갖춰져 있어 약 `20~30%` 정도 남은 상태로 판단
- [ ] 실제 구현 남은 양: `DeviceSession` 요청-응답 흐름만 기준으로 보면 응답 완료, 추적 저장소 연계, timeout/cleanup 경로가 아직 없어 약 `50~60%` 남은 상태로 판단
- [ ] TDD 남은 양: 현재는 기본 성공/실패/일부 경계 케이스까지 확보됐고, 전체 테스트 주도 보강 범위는 약 `35~40%` 남은 상태로 판단
- [ ] TDD 우선 대상: `DeviceSession` 응답 완료/타임아웃/정리 흐름 -> `DeviceBootstrapper` 예외 및 취소 경계 -> `DeviceProfileValidator` 조합/경계값 확장
### 6. 작업 업데이트 (DeviceSession)
- [x] `DeviceSession`에 pending 요청 수 추적 상태 관리 추가
- [x] 요청 응답 완료를 처리하는 `TryCompleteResponse<TResponse>` 경로 추가
- [x] 요청 응답 timeout 시 pending 정리와 `TimeoutException` 반환 경로 추가
- [x] 큐 포화 상태에서 요청 전송 실패 시 응답 task도 함께 실패하고 pending 누수가 없는지 테스트 보강
- [x] `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj` 재실행 통과 (`25`개 테스트)

### 7. 작업 업데이트 (Governance / Bootstrapper)
- [x] `AGENT.md`에 브랜치 전략, issue 연결, PR 체크 규칙을 명시해 다음 세션에서도 반복 적용되도록 정리
- [x] `docs/current-plan.md`에 branch / issue / PR 메모 추가
- [x] `DeviceBootstrapperTests`에 연결 실패 예외 전파 검증 추가
- [x] `DeviceBootstrapperTests`에 중간 연결 실패 시 후속 프로필 중단 검증 추가
- [x] `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj` 재실행 통과 (`27`개 테스트)

### 8. 작업 업데이트 (DeviceProfileValidator)
- [x] `DeviceProfileValidatorTests`에 필수 식별자/표시명 검증 케이스 추가
- [x] `DeviceProfileValidatorTests`에 TCP host, UDP local/remote 조합, Multicast TTL, unsupported transport 경계 케이스 추가
- [x] `DeviceProfileValidator`에 UDP `RemoteHost`/`RemotePort` 동시 설정 규칙 추가
- [x] `DeviceProfileValidator`에 Multicast TTL 양수 검증 추가
- [x] `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj` 재실행 통과 (`37`개 테스트)

### 9. 작업 업데이트 (Protocol / Transport / Connection Pipeline)
- [x] `feat/protocol-frame-pipeline` 브랜치 생성 후 protocol frame pipeline 전용 이슈/PR 초안 문서 추가
- [x] `IProtocol`, `LengthPrefixedProtocol`, `MessageFrameEncoder`, `MessageFrameDecoder`를 추가/확장해 `message -> payload -> frame -> payload -> message` 경로 골격 구성
- [x] `ITransport`에 `SendAsync`와 `ReceiveAsync` 계약을 추가하고 `StubTransports`를 송수신 기록형 구현으로 확장
- [x] `IProtocolFactory`, `ISerializerFactory`, `ConnectionManager.SendAsync`, `ConnectionManager.ReceiveAsync`, `ConnectionManager.TryHandleInboundFrame`까지 연결해 장치별 송수신 파이프라인 골격 구성
- [x] `DeviceSession` outbound dequeue와 응답 완료 경로를 `ConnectionManager` 수신 처리와 연결
- [x] 인프라 테스트 보강: `LengthPrefixedProtocolTests`, `MessageFrameEncoderTests`, `MessageFrameDecoderTests`, `StubTransportsTests`, `TransportMessageSenderTests`, `TransportMessageReceiverTests`, `ConnectionManagerTests`
- [x] 검증 완료: `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj` 통과 (`39`개 테스트)
- [x] 검증 완료: `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 통과 (`48`개 테스트)
- [x] 현재 미커밋 변경은 session/connection inbound 통합과 transport receive 골격 관련 파일에만 존재

### 10. 내일 할 일
- [ ] 현재 워크트리 변경을 기능 단위로 나눠 커밋 정리
- [ ] `ConnectionManager` 송수신 루프를 실제 transport 수명주기와 연결할지, 별도 background pump로 분리할지 구조 결정
- [ ] `NoOpSerializer` 대신 요청/응답 상관관계와 payload 본문을 실제로 담는 serializer 방향 정리
- [ ] transport별 실제 connect/read/write loop 초안(TCP, UDP, Serial, Multicast) 중 우선순위 1개를 선택해 첫 구현 착수
- [ ] inbound response가 `DeviceSession` pending 완료까지 이어지는 경로를 더 일반화하고 에러/timeout/unknown response 처리 보강

## 2026-03-27

### 1. 오늘 작업 초점
- [ ] `TransportMessageReceiver` 구현을 `ITransport.ReceiveAsync` 계약과 맞춰 마무리
- [ ] `ConnectionManager` inbound 처리 경로와 `TransportMessageReceiver` 연결 방식을 정리
- [ ] `DeviceSession` pending response 완료 흐름이 inbound 수신 결과와 자연스럽게 이어지는지 점검

### 2. 현재 작업 상태
- [x] 워크트리에 `TransportMessageReceiver` / `TransportMessageReceiverTests` 신규 파일이 추가된 상태 확인
- [x] `DeviceSession`, `ConnectionManager`, `ITransport`, 관련 테스트 파일에 미커밋 변경이 이어지고 있는 상태 확인
- [x] 오늘 세션 시작 기준으로 `PROGRESS.md`에는 `2026-03-27` 섹션이 아직 없어 새 날짜 섹션 추가 필요 확인

### 3. 오늘 할 일 정리
- [ ] `src/CommLib.Infrastructure/Transport/TransportMessageReceiver.cs`에서 불완전 frame, decode 실패, cancellation 처리 기준 확정
- [ ] `tests/CommLib.Infrastructure.Tests/TransportMessageReceiverTests.cs`에 정상 수신, decode 실패, 불완전 frame 케이스 보강
- [ ] `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs`가 receiver를 직접 사용해야 하는지, 현재 `TryHandleInboundFrame` 경로를 유지할지 결정
- [ ] `tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs`에서 inbound response 완료, unknown response, 예외 전파 시나리오 점검
- [ ] 관련 인터페이스 변경(`ITransport`, `IConnectionManager`, `IDeviceSession`)이 테스트 책임 경계를 흔들지 않는지 확인

### 4. 다음 세션 전 체크포인트
- [ ] 구현 방향이 정리되면 기능 단위로 커밋 범위를 나누기
- [ ] 최소 `CommLib.Infrastructure.Tests`와 영향받는 `CommLib.Unit.Tests` 실행으로 회귀 확인

### 5. 오늘 우선순위 요약
- [ ] `TransportMessageReceiver` 수신/디코드 실패 기준을 먼저 고정
- [ ] `ConnectionManager` inbound 처리와 response 완료 흐름을 receiver 기준으로 정리
- [ ] `TransportMessageReceiverTests`, `ConnectionManagerTests` 보강 후 관련 테스트 실행

### 6. 오늘 진행 업데이트
- [x] `TransportMessageReceiverTests`에 decode 실패 시 `InvalidOperationException` 전파 케이스 추가
- [x] `TransportMessageReceiverTests`에 cancellation 전파 케이스 추가
- [x] `ConnectionManagerTests`에 `ReceiveAsync` 불완전 frame 예외 전파 케이스 추가
- [x] `ConnectionManagerTests`에 미등록 장치 `ReceiveAsync` 예외 케이스 추가
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 통과 (`52`개)

### 7. 오늘 추가 진행
- [x] `feat(infra): add transport receive pipeline` 커밋 생성 (`8778ea7`)
- [x] `refactor(infra): route inbound decode through receiver` 커밋 생성 (`9ef86fb`)
- [x] `ConnectionManager`가 inbound decode를 `TransportMessageReceiver` 한 경로로 사용하도록 정리
- [x] `TransportMessageReceiver`에 `TryDecode` 진입점 추가 후 관련 테스트 보강
- [x] `DeviceBootstrapperTests`의 fake `IConnectionManager`를 최신 `ReceiveAsync` 계약에 맞게 보강
- [x] `NoOpSerializer`가 `request` / `response` 상관관계 정보를 payload에 담도록 확장
- [x] `MessageFrameEncoderTests`, `MessageFrameDecoderTests`, `NoOpSerializerTests`를 새 serializer 형식에 맞게 갱신
- [x] `DeviceBootstrapperTests`의 깨진 XML 주석 문자열 전수 점검 후 손상된 주석 교정
- [x] `ConnectionManagerTests`에 기본 `ProtocolFactory` / `SerializerFactory` 조합 request/response 통합 케이스 추가
- [x] 최소 `NoOpSerializer`는 concrete response 대신 `IResponseMessage` 수준 복원 계약으로 테스트를 정리
- [x] `ConnectionManager`에 장치별 background receive pump와 inbound queue를 추가해 매칭된 응답을 자동 완료하도록 확장
- [x] `ConnectionManagerTests` fake transport를 비동기 inbound queue 기반으로 바꾸고 auto-complete / unmatched response 케이스를 보강
- [x] `IConnectionManager`에 `DisconnectAsync` 계약을 추가하고 `ConnectionManager`가 session/send/receive pump 정리를 수행하도록 확장
- [x] `ConnectionManagerTests`에 disconnect 후 send/receive 차단, reconnect 복구, unknown device 예외 케이스 추가
- [x] `ITransport`에 `CloseAsync` 계약을 추가하고 `ConnectionManager.DisconnectAsync`가 transport close까지 호출하도록 확장
- [x] `StubTransportsTests`, `ConnectionManagerTests`, transport message sender/receiver fake transport를 새 close 계약에 맞게 보강
- [x] disconnect 시 이전 inbound queue는 `drop` 정책으로 완료/비우기 처리하고 reconnect 이후 새 queue로 누수되지 않도록 정리
- [x] `ConnectionManagerTests`에 queued inbound drop 후 reconnect 검증 케이스 추가

### 8. 오늘 검증
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 통과 (`58`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 재통과 (`60`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "FullyQualifiedName~ConnectionManagerTests"` 통과 (`23`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-build` 통과 (`62`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "FullyQualifiedName~ConnectionManagerTests"` 재통과 (`28`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 재통과 (`69`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "FullyQualifiedName~ConnectionManagerTests"` 재통과 (`29`개 테스트)
- [x] `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 재통과 (`70`개 테스트)
- [x] `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj` 통과 (`39`개 테스트)
- [x] `dotnet test commlib-codex-full.sln` 통과

### 9. 다음 작업 메모
- [ ] 현재 미커밋 변경(`NoOpSerializer`, 관련 인프라 테스트, `DeviceBootstrapperTests` 주석 수정) 커밋 정리
- [x] `ConnectionManager` 수신 경로를 background receive pump로 확장
- [x] receive pump 종료/정리 수명주기 1차 버전으로 `DisconnectAsync` 계약 추가
- [x] transport 자체 close 1차 계약으로 `ITransport.CloseAsync` 추가
- [x] disconnect 시 queued inbound는 `drop` 정책으로 정리
- [ ] 실제 socket/serial transport dispose 전략과 close 중 in-flight receive 정리 방식 구체화
- [ ] serializer payload 형식을 실제 요청/응답 본문까지 담는 방향으로 확장할지 결정
