# SKILL.md - Architecture Review

목적:
- 설계 변경이 Clean Architecture 와 OOP 원칙을 깨지 않는지 검토한다.

체크리스트:
- Domain 이 Infrastructure 를 참조하지 않는가?
- Application 이 구체 구현에 묶이지 않는가?
- 설정 바인딩과 실행 로직이 분리되는가?
- config json 은 raw DTO → mapper → validator 흐름인가?
- 장치 transport 추가가 switch/factory 확장으로 가능한가?
- timeout / cancellation / thread-safety 고려가 있는가?
- 보안 검증 포인트가 정의되었는가?

산출물:
- 허용
- 수정 필요
- 리스크 요약
