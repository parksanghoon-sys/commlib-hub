# SKILL.md - Commit Governance

목적:

- 기능별 커밋과 검증 완료 상태를 강제한다.

규칙:

- 한 커밋에 한 목적
- test 없는 feat 커밋 금지
- 큰 변경은 plan/review 없이 커밋 금지
- security 변경은 명시
- plan 없는 큰 변경 금지
- 실패 테스트가 남아있는 상태에서 커밋 금지
- 커밋 메시지 한글로 작성

커밋 예:

- feat(domain): add transport configuration contracts
- test(application): add device profile validator tests
- feat(application): add mapper validator bootstrapper
