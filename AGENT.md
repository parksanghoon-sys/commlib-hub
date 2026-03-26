# AGENT.md

## 목적

이 저장소는 C# 기반 장치 통신 프레임워크를 구현한다.
현재 범위는 HTTP를 제외한 TCP, UDP, Serial, Multicast 장치를 `appsettings.json` 기반으로 등록하고,
공통 세션, 메시지, 프로토콜 모델로 확장 가능하게 구성하는 것이다.

핵심 목표:
- Clean Architecture 준수
- OOP 기반 인터페이스 설계
- config json 기반 장치 등록
- Channel 기반 thread-safe 파이프라인
- Send / Request-Response / Chain 을 고려한 구조 유지
- TDD 3단계 사이클 준수
- 기능별 커밋 강제
- hook 을 통한 skill gate 강제
- 보안 기본값 강제

---

## 필수 작업 순서

모든 비슷한 작업은 반드시 아래 순서를 따른다.

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

`.agents/hooks` 스크립트는 아래를 강제한다.

- 작업 시작 전 skill gate 수행
- 구현 전 `docs/current-plan.md` 작성 확인
- Domain/Application 변경 시 architecture review 수행 여부 확인
- test 없이 구현 커밋 금지
- security 영향 파일 변경 시 security review 누락 금지

---

## 브랜치 / Issue / PR 운영 규칙

모든 작업은 브랜치, 이슈, PR 흐름을 의식하고 진행한다.

기본 규칙:
- 작업 시작 전 현재 브랜치가 작업 목적과 맞는지 확인한다.
- 새 작업은 가능하면 이슈 기준으로 브랜치를 분리한다.
- 브랜치 이름은 `type/short-topic` 또는 `type/issue-<id>-short-topic` 형식을 우선 사용한다.
- 하나의 브랜치에는 하나의 목적만 담고, unrelated 변경을 섞지 않는다.
- 구현 전에 관련 이슈가 있으면 plan, progress, commit, PR 설명에서 이슈를 추적 가능하게 남긴다.
- 커밋은 PR에서 그대로 읽혀도 맥락이 보이도록 작게 자른다.
- 작업 마무리 시 push 여부와 PR 생성 필요 여부를 항상 한 번 점검한다.

PR 규칙:
- PR 설명에는 목적, 변경 범위, 테스트 결과, 남은 리스크를 반드시 적는다.
- 테스트 추가/수정이 있는 경우 어떤 시나리오를 보강했는지 PR 설명에 적는다.
- 아키텍처나 보안 영향이 있는 변경은 PR에 영향 범위를 명시한다.
- 후속 작업이 남으면 PR 본문 또는 PROGRESS에 TODO를 남겨 다음 세션에서 이어질 수 있게 한다.

에이전트 동작 규칙:
- Codex는 작업 중 브랜치 전략, 이슈 연결, PR 준비 상태를 잊지 않도록 각 주요 작업 단위에서 한 번씩 점검한다.
- 커밋 전에는 "이 변경이 단일 브랜치 목적에 맞는지"를 확인한다.
- 다음 세션으로 넘길 때는 issue/PR 관점의 남은 작업도 함께 기록한다.

---

## 아키텍처 원칙

의존 방향:

```text
Host/Console -> Hosting -> Application -> Domain
                         -> Infrastructure -> Domain
```

금지:
- Domain 에서 Infrastructure 참조
- Application 에서 구체 Transport 직접 `new`
- 설정 바인딩과 실행 로직 혼합
- 무제한 queue
- timeout 없는 request/response 설계
- payload 전체 로그 출력

---

## 현재 범위

### 지원 대상
- TcpClient
- Udp
- Serial
- Multicast

### 후속 확장 후보
- Http / Rest
- gRPC
- WebSocket

---

## config json 원칙

장치 등록은 반드시 `appsettings.json` 의 `CommLib:Devices` 배열을 사용한다.

규칙:
- 추상 타입에 직접 바인딩하지 않는다.
- 먼저 `DeviceProfileRaw` 로 바인딩한다.
- Mapper 로 구체 `TransportOptions` 를 생성한다.
- Validator 로 검증 후 사용한다.
- `Enabled=false` 장치는 연결하지 않는다.

---

## TDD 3단계 원칙

### Red
실패하는 테스트를 먼저 작성한다.

### Green
가장 작은 코드로 테스트를 통과시킨다.

### Refactor
테스트가 유지되는 상태에서만 중복 제거와 구조 개선을 수행한다.

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
- 한 커밋에는 한 목적만 담는다.
- test 커밋과 feat 커밋은 가능하면 분리한다.
- 보안 관련 변경은 `security:` 또는 `fix:` 로 명시한다.
- architecture review 가 필요한 변경은 plan 과 review 없이 커밋하지 않는다.

---

## 보안 기본 원칙

- 입력은 모두 불신한다.
- 최대 프레임 길이를 둔다.
- unknown message id 는 거부한다.
- config 값은 validator 로 검증한다.
- 장치별 endpoint allow-list 정책을 고려한다.
- secret 과 설정 파일을 평문으로 노출하지 않는다.
- malformed frame 반복 시 차단 전략을 둔다.
- correlation 응답은 대상 세션까지 검증한다.

---

## 현재 구현 우선순위

1. Domain 계약
2. config raw -> profile mapper
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
- 기능별 커밋 가능한 상태
- hook 규칙 위반 없음

---

## 주석 규칙

- 새 C# 파일을 만들 때는 클래스, 인터페이스, 레코드, public 메서드, public 프로퍼티에 XML 문서 주석을 작성한다.
- 새 클래스나 인터페이스를 추가할 때 주석은 기본적으로 한글로 작성한다.
- 구현 의도가 바로 드러나지 않는 private 필드나 메서드도 필요하면 한글 XML 주석 또는 짧은 한글 설명 주석을 남긴다.
- 주석은 이름을 반복하지 말고 역할, 사용 시점, 반환 의미가 드러나도록 작성한다.
