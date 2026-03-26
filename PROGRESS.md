# 진행 현황

## 2026-03-25

### 1. 오늘 확인한 내용
- [x] 저장소 기본 구조 확인
- [x] `src/CommLib.Domain`, `src/CommLib.Application`, `src/CommLib.Infrastructure`, `src/CommLib.Hosting` 레이어 구성 확인
- [x] 테스트 프로젝트 구성과 현재 테스트 파일 배치 확인
- [x] 현재 구현 베이스라인과 워크트리 상태 확인
- [x] `obj/`, `bin/` 같은 생성 산출물은 커밋 대상에서 제외해야 함을 확인

### 2. 오늘 완료한 작업
- [x] C#/.NET 프로젝트용 `.gitignore` 추가
- [x] 솔루션 파일 `commlib-codex-full.sln` 추가
- [x] 주요 Domain / Application / Infrastructure / Hosting 코드에 XML 주석 작성
- [x] 테스트 코드에도 XML 주석 작성
- [x] XML 주석을 한글 기준으로 정리
- [x] 새 C# 파일과 새 클래스 작성 시 한글 XML 주석을 남기도록 `AGENT.md`와 구현 스킬 규칙 보강
- [x] 각 프로젝트의 목적이 보이도록 `.csproj` 설명 추가
- [x] 원격 저장소 `commlib-hub` 연결 및 `main` 브랜치 최초 push 완료

### 3. 테스트 관련 진행 내용
- [x] 테스트를 더 작은 단위로 쪼개야 한다는 기준을 다시 정리
- [x] `ConnectionManagerTests`를 fake 기반의 더 작은 단위 테스트로 분리 시작
- [x] `DeviceBootstrapperTests`에 비활성 프로필 미연결 검증 추가
- [x] `DeviceBootstrapperTests`에 취소 토큰 전달 검증 추가
- [x] `ConnectionManagerTests`에 같은 장치 재연결 시 세션 교체 여부 검증 추가
- [x] `TransportFactoryTests`는 인프라 구현 테스트로 유지하는 방향 정리

### 4. 테스트 프로젝트 구조 관련 판단
- [x] `ConnectionManager`, `TransportFactory` 테스트는 인프라 구현 테스트이므로 `CommLib.Infrastructure.Tests`로 두는 방향이 더 적절하다고 판단
- [x] `DeviceBootstrapper`, `DeviceProfileMapper`, `DeviceProfileValidator`는 애플리케이션 성격이 강하므로 `Infrastructure.Tests`로 옮기지 않는 편이 더 적절하다고 판단
- [x] `tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj` 재생성
- [x] `tests/CommLib.Infrastructure.Tests/ConnectionManagerTests.cs` 재배치 시작
- [x] `tests/CommLib.Infrastructure.Tests/TransportFactoryTests.cs` 재배치 시작
- [x] `commlib-codex-full.sln`에 `CommLib.Infrastructure.Tests` 프로젝트 재등록

### 5. 확인한 문제 / 리스크
- [x] `DeviceProfileMapper.cs`에 `CS8506` 컴파일 오류가 있어 전체 테스트 실행이 현재 막혀 있음
- [x] 일부 한글 주석은 콘솔 출력 인코딩 때문에 깨져 보이지만 파일 자체는 한글 주석 기준으로 유지 중
- [x] 아직 테스트 구조 재배치 작업이 끝나지 않아 워크트리에 미커밋 변경이 남아 있음

### 6. 추가 완료 작업
- [x] `DeviceProfileMapper.cs`의 `CS8506` 빌드 오류 수정
- [x] `DeviceProfileValidatorTests`를 현재 정적 검증 API에 맞게 정리
- [x] `ConnectionManagerTests`에 타 장치 세션 유지 검증 추가
- [x] `ConnectionManagerTests`에 전송 생성 실패 시 세션 미등록 검증 추가
- [x] `DeviceSessionTests` 신규 추가
- [x] `SendResultTests` 신규 추가
- [x] `TransportFactoryTests`에 새 인스턴스 생성 검증 추가
- [x] `TransportFactoryTests`에 미지원 형식 예외 메시지 검증 추가

### 7. 검증 결과
- [x] `dotnet build commlib-codex-full.sln` 통과
- [x] `dotnet test commlib-codex-full.sln` 통과
- [x] `CommLib.Unit.Tests` 13개 테스트 통과 확인
- [x] `CommLib.Infrastructure.Tests` 14개 테스트 통과 확인

### 8. 최근 커밋
- [x] `21553db` `wip(test): 인프라 테스트 재배치 진행`
- [x] `09af57d` `fix(application): restore mapper build and validator test`
- [x] `525b4ce` `test: add session and connection edge case coverage`
- [x] `f7753e9` `test(infra): tighten transport factory coverage`

### 9. 현재 상태
- [x] 현재 워킹트리는 추적 대상 변경 없이 깨끗함
- [x] `CommLib.Infrastructure.Tests` / `CommLib.Unit.Tests` 분리 기준이 코드와 테스트에 반영됨

### 10. 다음 작업 후보
- [ ] `DeviceBootstrapper` 추가 경계 조건 검토
- [ ] `DeviceProfileValidator` 보안성 검증 항목 확장
- [ ] `CommLib.Infrastructure.Tests`와 `CommLib.Unit.Tests` 경계 재검토
- [ ] 브랜치 단위 `push/PR` 흐름 시범 적용
