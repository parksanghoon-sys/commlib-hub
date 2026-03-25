# SKILL.md - Security Review

목적:
- config 기반 장치 등록 구조의 보안 취약점을 먼저 본다.

체크리스트:
- unknown transport type 거부
- invalid host/port 거부
- max frame length 제한
- plaintext secret 금지 원칙 유지
- endpoint allow-list 확장 가능성
- malformed config 에 대한 fail-fast
- debug log 에 민감정보 노출 금지

결과:
- 승인
- 수정 필요
