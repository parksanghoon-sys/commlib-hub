# SKILL.md - Skill Gate

목적:
- 작업 시작 시 어떤 skill 을 순서대로 사용해야 하는지 강제한다.

규칙:
1. 먼저 이 skill 을 읽는다.
2. 작업이 Domain/Application/Infrastructure/Testing/Docs 중 무엇을 건드리는지 식별한다.
3. 반드시 아래 순서로 진행한다.
   - architecture-review
   - planning
   - implementation-tdd
   - testing
   - security-review
   - commit-governance
4. 비사소한 변경은 current-plan.md 작성 없이 진행하지 않는다.
5. 테스트 없이 구현 완료로 간주하지 않는다.
