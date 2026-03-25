# 진행 현황

## 2026-03-25

### 1. 오늘 확인한 내용
- [x] 저장소 기본 구조를 확인함
- [x] `src/CommLib.Domain`, `src/CommLib.Application`, `src/CommLib.Infrastructure`, `src/CommLib.Hosting` 레이어가 구성되어 있음
- [x] 테스트 프로젝트가 분리되어 있으며 Application / Infrastructure 테스트가 존재함
- [x] 현재 구현 베이스라인을 확인함
- [x] 작업 전 워크트리 상태를 점검함
- [x] `obj/`, `bin/` 등 생성 산출물이 커밋 대상과 섞이지 않도록 주의가 필요함

### 2. 현재 구현 상태 요약
- [x] Domain 레이어에 설정 모델과 주요 계약 인터페이스가 정의되어 있음
- [x] Application 레이어에 Mapper, Validator, Bootstrapper, PendingRequestStore가 존재함
- [x] Infrastructure 레이어에 TransportFactory, Protocol 관련 구성요소, ConnectionManager, Stub Transport가 존재함
- [x] Mapper, Validator, Bootstrapper, TransportFactory에 대한 기본 테스트가 준비되어 있음

### 3. 우선 수행 계획
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] 현재 저장소가 정상 빌드/테스트 가능한 기준 상태인지 먼저 검증
- [ ] 저장소 가이드에서 요구하는 `docs/current-plan.md` 필요 여부 확인
- [ ] 필요 시 `docs/current-plan.md`를 생성하거나 현재 계획과 정렬
- [ ] Domain / Application 계약을 검토하여 실제 통신 흐름에 필요한 누락 동작 확인
- [ ] Infrastructure 구현이 스켈레톤 수준에 머물러 있는 부분을 보강
- [ ] 설정 바인딩 흐름을 `appsettings.json` -> Raw Profile -> Mapper -> Validator -> Bootstrapper 기준으로 점검
- [ ] 보안 및 운영 관점 점검 수행

### 4. 세부 점검 항목
- [ ] 전송 계층 수명주기 관리
- [ ] Request / Response 상관관계 처리
- [ ] Timeout / Reconnect 정책 반영 여부
- [ ] Session 책임 경계 정리
- [ ] Stub Transport의 실제 구현 필요 범위 확인
- [ ] ConnectionManager와 Session 연계 구조 검토
- [ ] Protocol / Serializer 조합 경로 검증
- [ ] 잘못된 설정값의 조기 차단
- [ ] 민감하거나 과도한 payload 로그 방지
- [ ] malformed input 및 timeout 처리 확인

### 5. 다음 액션
- [ ] 가장 먼저 빌드와 테스트를 실행해 현재 실패 지점을 확인한 뒤, 수정 우선순위를 결정할 예정

### 6. 메모
- [x] `README.md`와 `AGENT.md` 기준으로 이 저장소는 설정 기반 장치 등록, 레이어 분리, TDD 우선 흐름을 지향함
- [x] 이번 단계에서는 구현보다 계획 수립과 진행 문서 정리에 집중함
