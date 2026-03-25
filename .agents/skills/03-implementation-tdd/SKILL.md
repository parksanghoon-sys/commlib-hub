# SKILL.md - Implementation TDD

목적:
- TDD 3색 원칙과 OOP/클린아키텍쳐를 지키며 구현한다.

구현 규칙:
- Red → Green → Refactor 순서를 지킨다.
- 메서드 인자가 많아지면 옵션 객체로 묶는다.
- public API 는 의미 중심 이름을 사용한다.
- config 에서 직접 런타임 구현을 생성하지 말고 factory 를 사용한다.
- mapper 와 validator 를 분리한다.
- 추상 타입 직접 JSON 바인딩을 시도하지 않는다.
- thread-safe 를 기본값으로 설계한다.

금지:
- 테스트 없이 코드 추가
- 거대한 클래스 하나로 처리
- Infrastructure 타입을 Domain 에 노출
