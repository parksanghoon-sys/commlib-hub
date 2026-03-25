# AGENT.md

## 목적

이 저장소는 C# 기반 장치 통신 프레임워크를 구현한다.
현재 범위는 HTTP를 제외한 TCP, UDP, Serial, Multicast 장치를 config json으로 등록하고,
공통된 세션/메시지/프로토콜 모델로 사용하는 것이다.

핵심 목표:
- Clean Architecture 유지
- OOP 기반 인터페이스 설계
- config json 기반 장치 등록
- Channel 기반 thread-safe 파이프라인
- Send / Request-Response / Chain 지원 가능한 방향 유지
- TDD 3색 원칙 준수
- 기능별 커밋 강제
- hook 을 통한 skill gate 강제
- 보안 기본값 강제

---

## 필수 작업 순서

모든 비사소한 작업은 반드시 아래 순서를 따른다.

1. `00-skill-gate`
2. `01-architecture-review`
3. `02-planning`
4. `03-implementation-tdd`
5. `04-testing`
6. `05-security-review`
7. `06-commit-governance`

순서를 건너뛰지 않는다.

---

## hook 규칙

`.agents/hooks` 의 스크립트는 아래를 강제한다.

- 작업 시작 전 skill gate 수행
- 구현 전 docs/current-plan.md 작성 확인
- Domain/Application 변경 시 architecture review 선행 여부 확인
- test 없이 구현 커밋 금지
- security 영향 파일 변경 시 security review 누락 금지

---

## 아키텍처 원칙

의존 방향:

```text
Host/Console → Hosting → Application → Domain
                         → Infrastructure → Domain
```

금지:
- Domain → Infrastructure 참조
- Application 에서 구체 Transport 직접 new
- 설정 바인딩과 실행 로직 혼합
- 무제한 queue
- timeout 없는 request/response 제공
- payload 전체 로그 출력

---

## 현재 범위

### 지원 대상
- TcpClient
- Udp
- Serial
- Multicast

### 나중에 추가
- Http / Rest
- gRPC
- WebSocket

---

## config json 정책

장치 등록은 반드시 `appsettings.json` 의 `CommLib:Devices` 배열을 사용한다.

규칙:
- 추상 타입 직접 바인딩 금지
- `DeviceProfileRaw` 로 먼저 바인딩
- Mapper 로 구체 TransportOptions 생성
- Validator 로 검증 후 사용
- `Enabled=false` 장치는 연결하지 않음

---

## TDD 3색 원칙

### Red
실패 테스트를 먼저 작성한다.

### Green
가장 작은 코드로 테스트를 통과시킨다.

### Refactor
테스트가 유지된 상태에서만 중복 제거와 구조 개선을 한다.

금지:
- 테스트 없이 구현 시작
- 여러 기능을 한 번에 구현
- 테스트가 깨진 상태에서 구조 변경

---

## 기능별 커밋 규칙

커밋 형식:

```text
type(scope): summary
```

예시:
- feat(domain): add config-driven device profile contracts
- test(application): add device profile mapping tests
- refactor(infra): extract transport factory
- security(hosting): reject invalid device configuration

규칙:
- 한 커밋에 한 기능
- test 커밋과 feat 커밋은 가능하면 분리
- 보안 관련 변경은 `security:` 또는 `fix:` 명시
- architecture review 가 필요한 변경은 plan 과 review 없이 커밋 금지

---

## 보안 기본 원칙

- 입력은 모두 불신한다.
- 최대 프레임 길이를 둔다.
- unknown message id 는 거부한다.
- config 값은 validator 로 검증한다.
- 장치별 endpoint allow-list 정책을 고려한다.
- secret 은 설정 파일에 평문 저장하지 않는다.
- malformed frame 반복 시 차단 전략을 둔다.
- correlation 응답은 타입/세션까지 검증한다.

---

## 현재 구현 우선순위

1. Domain 계약
2. config raw → profile mapper
3. validator
4. transport factory
5. bootstrapper
6. 최소 session / transport skeleton
7. 단위 테스트
8. hook / skill gate 정착

---

## 완료 조건

다음을 모두 만족해야 완료다.

- plan 문서 존재
- 관련 테스트 존재
- Mapper / Validator 테스트 통과
- 보안 체크리스트 확인
- 기능별 커밋 가능 상태
- hook 규칙 위반 없음
