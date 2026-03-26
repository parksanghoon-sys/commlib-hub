# Current Plan

## Scope
- config json 기반 장치 등록 구조
- TCP / UDP / Serial / Multicast 옵션 바인딩
- raw → profile 매핑
- validator
- transport factory
- bootstrapper
- connection manager skeleton

## Architecture Review
- Domain: 계약 정의
- Application: mapping / validation / bootstrap / session orchestration
- Infrastructure: factory / transport / serializer / protocol skeleton
- Hosting: DI 및 config binding
- Clean Architecture 방향 유지 확인

## TDD Plan
1. DeviceProfileMapper / Validator / Bootstrapper 기본 테스트 유지
2. ConnectionManager 경계 조건 테스트 보강
3. DeviceSession / SendResult 단위 테스트 보강
4. TransportFactory 입력별 / 예외별 테스트 보강
5. 문서와 테스트 프로젝트 경계 정리
6. 다음 기능 구현 전 리팩터링 포인트 점검

## Security Notes
- invalid port / host / serial name 검증
- max frame length 검증
- pending request 상한 검증
- unknown transport type 거부
- payload 전체 로그 금지
- fail-fast validation

## Commit Plan
- test(infra): infrastructure 테스트 프로젝트 재배치
- fix(application): mapper 빌드 오류와 validator 테스트 정합성 복구
- test: session / connection 경계 조건 테스트 추가
- test(infra): transport factory 테스트 세분화
- docs: 진행 현황과 다음 작업 갱신
## Branch / Issue / PR Notes
- 현재 브랜치 목적이 단일 작업 주제와 맞는지 매 작업 전 확인
- 가능하면 이슈 단위 브랜치 사용: `type/issue-<id>-short-topic`
- PR 준비 시 변경 목적, 테스트 결과, 잔여 리스크를 함께 정리
- 다음 세션으로 넘길 항목은 PROGRESS와 PR 관점에서 동시에 추적
