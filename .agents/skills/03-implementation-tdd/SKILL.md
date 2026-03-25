# SKILL.md - Implementation TDD

목적:
- TDD 3단계 사이클과 OOP/레이어드 아키텍처를 지키며 구현한다.

구현 규칙:
- Red -> Green -> Refactor 순서를 지킨다.
- 메서드 인자가 많아지면 옵션 객체로 묶는다.
- public API 는 의미가 분명한 이름만 사용한다.
- config 에서 직접 구체 구현을 생성하지 말고 factory 를 사용한다.
- mapper 와 validator 를 분리한다.
- 추상 타입 직접 JSON 바인딩을 시도하지 않는다.
- thread-safe 를 기본값으로 설계한다.
- 새 C# 파일, 새 클래스, 새 public API 를 추가할 때는 한글 XML 문서 주석을 함께 작성한다.
- 주석은 역할, 사용 맥락, 반환 의미가 드러나야 하며 이름 반복만 하는 설명은 피한다.

금지:
- 테스트 없이 코드 추가
- 여러 책임을 하나로 처리
- Infrastructure 타입을 Domain 으로 역참조
