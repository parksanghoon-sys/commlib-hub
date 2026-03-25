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

### 6. 현재 미커밋 상태
- [ ] `.agents/skills/06-commit-governance/SKILL.md` 변경 정리 필요
- [ ] `docs/current-plan.md` 변경 정리 필요
- [ ] `tests/CommLib.Unit.Tests`에서 인프라 테스트 삭제 변경 정리 필요
- [ ] `tests/CommLib.Infrastructure.Tests` 신규 파일 추가 변경 정리 필요
- [ ] `commlib-codex-full.sln` 변경 커밋 필요
- [ ] `tests/CommLib.Unit.Tests/DeviceBootstrapperTests.cs` 추가 테스트 변경 커밋 필요

### 7. 내일 할 일
- [ ] `ConnectionManager` 테스트를 더 작은 책임 단위로 계속 분리
- [ ] `TransportFactoryTests`를 입력별/예외별 작은 단위 검증 중심으로 재점검
- [ ] 인프라 테스트 재배치 작업을 마무리하고 테스트 변경만 별도 커밋
- [ ] `DeviceProfileMapper.cs`의 `CS8506` 컴파일 오류 수정
- [ ] `CommLib.Infrastructure.Tests`와 나머지 테스트 프로젝트 경계를 다시 한 번 검토
- [ ] 브랜치 기반으로 작은 기능 단위 작업 후 push/PR 흐름 시범 적용
