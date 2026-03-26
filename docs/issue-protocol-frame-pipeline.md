# Issue Draft

## Title
feat(infra): add protocol framing pipeline skeleton

## Summary
- `IProtocol`에 frame encode/decode 계약을 추가한다.
- `LengthPrefixedProtocol`에 길이 prefix 기반 프레이밍 구현을 추가한다.
- serializer와 protocol을 연결하는 `MessageFrameEncoder`를 추가한다.
- 관련 infrastructure 테스트를 보강한다.

## Why
- 현재 프로토콜과 serializer는 이름/개별 동작만 있고 실제 송신용 프레임 생성 경로가 없다.
- 실제 TCP/UDP/Serial/Multicast 송신 구현으로 가기 전에 `message -> payload -> frame` 경로를 먼저 고정할 필요가 있다.

## Scope
- Domain protocol contract 확장
- Infrastructure protocol implementation 추가
- Infrastructure tests 추가

## Out of Scope
- 실제 socket/serial I/O 구현
- transport별 connect/send/receive loop
- request/response pipeline과 transport 통합

## Acceptance Criteria
- `IProtocol`이 payload encode와 frame decode를 지원한다.
- `LengthPrefixedProtocol`이 4-byte big-endian length prefix 프레임을 encode/decode 한다.
- `MessageFrameEncoder`가 serializer와 protocol을 조합해 frame을 만든다.
- infrastructure tests가 모두 통과한다.

## Risks / Notes
- 기존 `IProtocol` 계약 변경이므로 이후 protocol 구현체 추가 시 같은 계약을 따라야 한다.
- frame decode는 현재 단일 완전 프레임 단위까지만 다루며 stream reassembly 전체는 후속 작업이다.
