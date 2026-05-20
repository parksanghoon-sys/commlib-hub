# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-05-20

## Goal
Continue commercial-readiness hardening from the current repository state while keeping each slice small, verified, and release-trackable.

## Confirmed Facts
- The current checkout is `codex/span-minimal-copy-pipeline`, with local commits not yet pushed to `commlib-hub/main` and pre-existing dirty/untracked work still present:
  - `.gitignore`
  - `DESIGN_REVIEW.md`
  - `VERIFICATION_REPORT.md`
  - `docs/superpowers/plans/2026-05-08-reconnect-types-constants.md`
  - `todo.md`
- The 2026-05-18 commercial-readiness review found that CommLib is suitable for controlled/internal pilot usage, but not yet a full external production-grade commercial release without more release governance, security, observability, and recovery-policy hardening.
- The first commercial-readiness implementation slice is complete:
  - `DeviceProfileValidator` now rejects non-positive TCP `ConnectTimeoutMs` and `BufferSize` values before runtime transport creation.
  - `DeviceProfileValidatorTests` covers both new TCP validation failures.
- The second commercial-readiness implementation slice is complete:
  - `.github/workflows/ci.yml` now restores with NuGet audit warnings promoted to errors for `NU1901` through `NU1904`.
  - `.github/workflows/ci.yml` now runs a NuGet vulnerability audit listing step for visibility.
  - `.github/workflows/ci.yml` now packs the four library projects under `src/` with a CI-only package version.
  - CI intentionally packs `CommLib.Domain`, `CommLib.Application`, `CommLib.Infrastructure`, and `CommLib.Hosting` directly instead of solution-level packing, because solution-level pack also creates example packages.
- The user-requested cleanup slice is complete:
  - `DeviceLabTheme` now keeps only the brush/text/border resources actively consumed by the code-built WinUI views.
  - Dormant templated-control style keys/helpers were pruned instead of preserving an unverified future style surface.
  - `DeviceLabTheme.Get<T>()` no longer accepts an unused `FrameworkElement owner` argument.
- The user-requested generic binary protocol first slice is complete:
  - `ProtocolOptions` now supports `ProtocolTypes.BinaryFrame` alongside `LengthPrefixed`.
  - `BinaryFrameProtocol` can compose/decode configurable start bytes, 1/2/4-byte payload length prefixes, and optional CRC16/Modbus checksums.
  - Payload bit management remains in `BitFieldPayloadSchema` / `BitFieldPayloadSchemaCodec` instead of being folded into protocol framing.
  - `AddCommLibCore()` now preserves a caller pre-registered `IProtocolFactory` so custom C# protocol implementations remain the escape hatch.
- The user then asked to plan and implement a broader `Span` / minimal-copy data-management pass across the protocol pipeline:
  - implementation plan saved at `docs/superpowers/plans/2026-05-19-span-minimal-copy-pipeline.md`
  - additive optional contracts now exist for zero-copy protocol decode, frame layout writing, and span serializer output
  - `LengthPrefixedProtocol` and `BinaryFrameProtocol` now support memory decode plus span-based frame construction
  - `MessageFrameDecoder`, `TransportMessageReceiver`, `MessageFrameEncoder`, `RawHexSerializer`, and `NoOpSerializer` now use the fast paths when available while preserving legacy `IProtocol` / `ISerializer` methods
- The user asked to add simpler byte/start-bit/end-bit based generic value extraction without making the bitfield layer more complex:
  - `BitFieldDefinition.FromByteBits(...)` centralizes byte-local bit range conversion into `BitOffset` / `BitLength`
  - `BitFieldCodec.ReadUnsigned<T>(...)` and `ReadSigned<T>(...)` provide typed integral extraction while reusing the existing unsigned/signed bit reader
  - `BinaryFrameProtocol` remains a frame-envelope layer; bit-level interpretation stays in payload helpers
- The user asked to implement the highest-priority `DESIGN_REVIEW.md` findings:
  - `IDeviceSession` now exposes only the public send-oriented session contract.
  - `ConnectionManager.GetSession()` returns a public session facade instead of the mutable runtime `DeviceSession`.
  - `ConnectionManager.SendAsync(...)` sends directly through `TransportMessageSender`; the old session outbound queue hop was removed.
  - `DeviceSession` is now a request/response pending tracker, not a transport send queue.
- The user asked to make the recent session-boundary implementation comments clear and Korean:
  - Korean XML comments now describe the public session contract, pending-response lifecycle, facade responsibilities, and request send execution order.
  - Session-related tests now include Korean purpose comments and key prepare/execute/verify flow comments.
- Verification for this slice passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceProfileValidatorTests`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore`
  - `dotnet build commlib-codex-full.sln --configuration Release --no-restore`
  - `dotnet restore commlib-codex-full.sln -p:NuGetAudit=true -p:NuGetAuditLevel=low -warnaserror:NU1901,NU1902,NU1903,NU1904`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet pack src/CommLib.Application/CommLib.Application.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet pack src/CommLib.Infrastructure/CommLib.Infrastructure.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet pack src/CommLib.Hosting/CommLib.Hosting.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --configuration Release --no-restore`
- Verification for the generic binary protocol slice passed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "BinaryFrameProtocolTests|ProtocolFactoryTests"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter "DeviceProfileValidatorTests"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter "ServiceCollectionExtensionsTests.AddCommLibCore_WithPreRegisteredProtocolFactory_PreservesCustomFactory"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore`
  - `dotnet build commlib-codex-full.sln --configuration Release --no-restore`
- Verification for the session API/outbound-queue cleanup passed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "IDeviceSession_PublicContractDoesNotExposeRuntimeLifecycleMethods|GetSession_Send_DoesNotLeaveOutboundMessageForNextManagerSend|GetSession_RequestResponseSend_SendsRequestThroughTransportAndCompletesResponse"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceSessionTests`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet build commlib-codex-full.sln --configuration Release --no-restore`
- Verification for the Korean comment clarity pass passed with:
  - `dotnet build commlib-codex-full.sln --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --no-build`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --no-build`
- Verification for the span/minimal-copy protocol pipeline passed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests|MessageFrameDecoderTests|MessageFrameEncoderTests|TransportMessageReceiverTests|RawHexSerializerTests|NoOpSerializerTests"`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet build commlib-codex-full.sln --configuration Release --no-restore`
  - `rg -n "public interface IZeroCopyProtocol|public interface IFrameEncodingProtocol|public interface ISpanSerializer|public readonly record struct ProtocolDecodeResult|public readonly record struct ProtocolFrameLayout" src/CommLib.Domain -S`
  - `git diff --check`
- NuGet vulnerability audit was checked with `dotnet list commlib-codex-full.sln package --vulnerable --include-transitive` and reported no vulnerable packages from the configured sources.

## Next Work Unit
1. Resume the first production diagnostics slice over the existing `IConnectionEventSink` seam, unless the user redirects to publish/push the current span branch first.

## Next Slice Design
1. Keep the next slice observability-focused and opt-in; do not redesign transport or reconnect behavior at the same time.
2. Reuse the existing `IConnectionEventSink` seam before introducing a new diagnostics abstraction.
3. Do not touch the pre-existing `DeviceSession.cs`, `.claude/`, `docs/superpowers/`, or `todo.md` changes unless the user explicitly asks to reconcile them.
4. Keep TLS, health checks, metrics, and auto-reconnect as separate production-integration decisions unless the diagnostics slice exposes a direct dependency.
5. If continuing protocol work, extend `BinaryFrame` only from concrete device requirements; do not turn payload bit schemas into frame protocol state.
6. For the span/minimal-copy pipeline, prefer additive opt-in interfaces and built-in fast paths over breaking existing custom protocol/serializer implementations.
7. Do not reintroduce a session outbound queue unless a real asynchronous producer/consumer use case appears; direct transport send is now the runtime path.

## Stop / Reassess Conditions
- If adding logging requires a package-boundary or public-API decision beyond a small adapter, stop and record the decision point instead of guessing.
- If unrelated dirty files overlap with the release-governance edits, stop and ask how the user wants those files handled.
