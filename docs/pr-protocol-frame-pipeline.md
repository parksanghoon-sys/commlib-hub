# PR Draft

## Title
feat(infra): add protocol frame encoding pipeline

## Summary
- `IProtocol`에 `Encode` / `TryDecode` 계약 추가
- `LengthPrefixedProtocol`에 길이 prefix 프레임 구현 추가
- `MessageFrameEncoder` 추가로 `IMessage -> payload -> frame` 경로 구성
- infrastructure tests 추가

## Why
- 실제 transport 구현 전에 공통 프레이밍 경로를 먼저 고정해 두기 위해
- serializer와 protocol이 분리되어 있어도 송신 경로에서 조합 가능하도록 만들기 위해

## Changed Files
- `src/CommLib.Domain/Protocol/IProtocol.cs`
- `src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs`
- `src/CommLib.Infrastructure/Protocol/MessageFrameEncoder.cs`
- `tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs`
- `tests/CommLib.Infrastructure.Tests/MessageFrameEncoderTests.cs`

## Tests
- `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj`

## Risks / Follow-up
- 아직 실제 transport send/receive loop에는 연결되지 않음
- protocol decode는 현재 단일 frame 추출까지만 담당
- 다음 단계에서 serializer/protocol/transport를 묶는 송신 파이프라인 통합이 필요
