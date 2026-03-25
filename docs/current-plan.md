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

## TDD Plan
1. DeviceProfileMapper tests
2. DeviceProfileValidator tests
3. DeviceBootstrapper tests
4. ConnectionManager enabled profile connect tests
5. 최소 구현
6. Refactor

## Security Notes
- invalid port / host / serial name 검증
- max frame length 검증
- pending request 상한 검증
- unknown transport type 거부
- payload 전체 로그 금지
- fail-fast validation

## Commit Plan
- feat(domain): add configuration and messaging contracts
- test(application): add mapper validator bootstrapper tests
- feat(application): add mapper validator bootstrapper and session skeleton
- feat(infra): add transport and factory skeleton
- docs(agent): add codex workflow and hooks
