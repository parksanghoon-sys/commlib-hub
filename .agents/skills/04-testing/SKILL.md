# SKILL.md - Testing

목적:
- 구현 전에 실패 테스트를 만들고, 구현 후 회귀를 방지한다.

필수 테스트:
- raw transport type mapping
- unknown transport type rejection
- invalid tcp port rejection
- invalid serial config rejection
- bootstrapper enabled device only
- factory correct transport creation

추가:
- 보안 검증 실패 케이스
- 경계값 테스트
