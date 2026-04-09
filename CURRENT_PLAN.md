# Current Plan

Date: 2026-04-09

## Goal
Use `feat/runtime-readiness-hardening` to harden the runtime core one safe slice at a time, with `ConnectionManager` lifecycle correctness, truthful length-prefixed protocol contracts, and explicit terminal session-shutdown semantics now landed while keeping the already-landed raw-hex/bitfield work intact.

## Confirmed Facts
- The repository continuity rules currently point at `AGENT.md`; a root `AGENT_RULES.md` file is not present.
- Active branch is `feat/runtime-readiness-hardening`, created on 2026-04-08 from the current `feat/bitfield-endianness` worktree so the production-readiness hardening can continue without discarding the in-progress bitfield/runtime changes.
- GitHub PR `#3` (`[codex] localize and streamline the WinUI device lab`) was already merged into `main`; the ongoing raw-hex and bitfield work remains isolated on the dedicated feature branch.
- The earlier WinUI follow-up work remains in place:
  - persisted English/Korean `AppLanguageMode`
  - localized shell / Device Lab / Settings / status copy
  - pointer-wheel forwarding for page text inputs
  - conservative Device Lab <-> Settings transition
  - scrollable auto-follow live log
  - active `DeviceLabTheme` hookup
  - collapsed transport-specific panels
  - in-app TCP / UDP / Multicast mock endpoint support
  - repo-level package/build centralization and coverage collector support
- The raw hex / bitfield direction still belongs in the serializer/composer path, not in transport creation or frame-boundary protocol logic.
- The raw-hex foundation from 2026-04-06 remains in place:
  - `IBinaryMessagePayload`
  - `BinaryMessageModel`, `BinaryRequestMessageModel`, and `BinaryResponseMessageModel`
  - `MessagePayloadFormatter`
  - `RawHexSerializer`
  - `SerializerFactory` support for `RawHex`
  - WinUI serializer selection plus localized raw-hex validation
  - transport-level and live WinUI raw-hex TCP roundtrip proof
- The low-level bitfield and schema foundation from 2026-04-06 to 2026-04-07 remains in place:
  - `BitFieldDefinition`
  - `BitFieldCodec`
  - `BitFieldPayloadSchema`, `BitFieldPayloadField`, and `BitFieldScalarKind`
  - `BitFieldFieldAssignment` and `BitFieldFieldValue`
  - `BitFieldPayloadSchemaValidator`
  - `BitFieldPayloadSchemaCodec`
  - optional `SerializerOptions.BitFieldSchema`
  - `DeviceProfileValidator` enforcement that schema usage is currently valid only with `RawHex`
  - `OutboundMessageComposer` overload for schema-driven binary payload composition
- The endianness follow-up remains intentionally narrow:
  - `BitFieldEndianness` now exists on both `BitFieldDefinition` and `BitFieldPayloadField`
  - the low-level codec still defines `bit 0` as the LSB of the first payload byte
  - `LittleEndian` remains the default behavior and preserves all existing callers
  - `BigEndian` is supported only for byte-aligned multi-byte fields that use a whole-byte length
  - schema compose/inspect paths honor that endianness metadata through `ToDefinition()`
  - MSB-first bit numbering and partial-byte big-endian multi-byte fields remain out of scope
- The first real runtime consumer of `SerializerOptions.BitFieldSchema` landed on 2026-04-08 without widening into a schema editor:
  - `MessageComposerAppSettings` / `DeviceLabSettingsViewModel` now preserve an optional `BitFieldSchema` from `appsettings.json`
  - `MainViewModel.BuildProfile()` now threads that schema into the live `SerializerOptions`
  - `DeviceLabSessionService` now appends schema-decoded field summaries, or a non-fatal schema decode warning, to inbound/outbound WinUI session logs
  - `MessagePayloadFormatter.TryFormatBitFieldSummary()` now provides a reusable log-formatting seam above schema inspection
- Focused validation for that consumer is in place:
  - `MessagePayloadFormatterTests` now cover schema summary generation and safe schema/payload mismatch handling
  - `BitFieldCodecTests` now cover big-endian non-zero byte offsets and signed reads
  - `BitFieldPayloadSchemaCodecTests` now cover mixed-endian schemas with big-endian offset fields and signed big-endian fields
- Verification completed on 2026-04-08:
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-build`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-build`
  - `dotnet build commlib-codex-full.sln --no-restore`
- A parallel `dotnet` validation attempt briefly hit an output-file lock on `CommLib.Domain.dll`; the subsequent sequential rerun succeeded, so the issue was in the validation invocation pattern rather than the implementation.
- A 2026-04-08 production-readiness review found that the repo is a solid extensible foundation, but not yet something we should label industrial/runtime-ready:
  - strengths: clear transport/protocol/serializer boundaries, strong profile/schema validation, and broad unit/infrastructure coverage across happy paths plus several failure paths
  - current blockers to a stronger readiness claim: `ConnectionManager` keeps lifecycle state in unsynchronized dictionaries, reconnect policy stops at connect-time retries rather than runtime recovery, `AddCommLibCore()` still exposes only a minimal hosting surface, and protocol/serializer extensibility still relies on central switch factories
- The first hardening slice against those findings landed on 2026-04-08 in `ConnectionManager`:
  - active connection state now lives in one per-device state object instead of seven parallel dictionaries
  - same-device `ConnectAsync` / `DisconnectAsync` operations are now serialized through per-device gates
  - `ConnectAsync()` no longer double-opens transports after `CreateOpenedTransportAsync()` already succeeded
  - background receive-pump failures now surface as `DeviceConnectionException(deviceId, "receive", ...)` and are reported through `IConnectionEventSink`
  - infrastructure tests now cover single-open behavior, same-device concurrent connect serialization, and sticky receive-failure surfacing
- Verification for the hardening slice completed on 2026-04-08:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build src/CommLib.Infrastructure/CommLib.Infrastructure.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-build`
  - `dotnet build commlib-codex-full.sln --no-restore`
- One earlier parallel validation attempt briefly hit an output-file lock on `CommLib.Infrastructure.dll`; the subsequent sequential rerun succeeded, so the issue was in the validation invocation pattern rather than the implementation.
- The `ProtocolOptions` truthfulness slice landed on 2026-04-09 without widening into a new framing family:
  - `ProtocolOptions` now exposes only the active `LengthPrefixed` contract (`Type` and `MaxFrameLength`)
  - `LengthPrefixedProtocol` now enforces `MaxFrameLength` during both encode and decode
  - `ProtocolFactory` now passes that limit through to the runtime implementation and accepts the built-in protocol name case-insensitively
  - `DeviceProfileValidator` now fail-fast rejects unsupported protocol types and too-small length-prefixed frame limits before runtime connection work starts
  - repo sample config no longer advertises inactive `UseCrc`, `Stx`, or `Etx` behavior, and the stale `FixedInterval` reconnect label was normalized to the supported `Linear` contract
  - the console example now uses the same frame-length guard in its manual echo helper path so the sample contract matches the runtime library contract
- Verification for the protocol-contract slice completed on 2026-04-09:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
- The runtime recovery semantics slice landed on 2026-04-09 without widening into a core auto-reconnect state machine:
  - a post-connect background receive failure is now terminal for that session inside `ConnectionManager`
  - `SendAsync()` and `TryHandleInboundFrame()` now rethrow the stored receive failure instead of letting a failed session continue acting live
  - `GetSession()` now hides failed sessions until a caller explicitly disconnects or reconnects that device
  - pending response tasks now fail immediately not only on background receive failure, but also on explicit `DisconnectAsync()` and same-device session replacement during reconnect
  - `ReconnectOptions` is now explicitly documented as connect-time transport-open retry only rather than live-session auto-recovery
- Verification for the runtime recovery slice completed on 2026-04-09:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
- One attempted parallel validation on 2026-04-09 hit a transient `CommLib.Domain.dll` file lock; the sequential rerun succeeded, so the issue was in the validation pattern rather than the implementation.
- The earlier `PROGRESS.md` encoding issue still remains deferred; this cycle did not attempt another in-place rewrite.

## Next Work Unit
1. Re-check whether keeping the public `ReconnectOptions` / `DeviceProfile.Reconnect` naming is still the right contract now that the runtime behavior is explicitly connect-time retry only, and choose between doc-only clarification, a non-breaking alias, or a future rename.
2. Only after that retry-contract naming is settled, revisit richer hosting concerns such as diagnostics, health checks, or TLS-facing transport options.
3. Keep any core-library auto-reconnect/state-machine work as a separate future slice only if a real deployment requirement appears.

## Stop / Reassess Conditions
- If runtime recovery semantics begin to require cross-device orchestration, hosted background services, or user-visible state modeling all at once, pause and choose the smallest boundary before coding.
- If reconnect naming cleanup would become a breaking public API/config migration, pause and decide whether a staged alias/deprecation path is better than an immediate rename.
- If a real device needs CRC, STX/ETX delimiters, or another non-length-prefixed framing family, keep that as a separate protocol-expansion slice instead of folding it into the recovery-policy work.
- If a real device needs MSB-first bit numbering or partial-byte big-endian multi-byte fields, keep that as a separate bitfield semantic expansion instead of mixing it into the runtime-hardening branch.
