# Current Plan

## Date
- 2026-04-09

## Current Scope
- Use `feat/runtime-readiness-hardening` to harden the runtime core one safe slice at a time
- Keep the already-landed raw-hex/bitfield work intact while moving from lifecycle correctness into explicit runtime recovery and session-shutdown semantics

## Confirmed State
- `AGENT.md` is the active repository rules file; `AGENT_RULES.md` is not present at the repo root.
- Branch is now `feat/runtime-readiness-hardening`, created on 2026-04-08 from the current `feat/bitfield-endianness` worktree so runtime hardening can continue without dropping the in-progress bitfield work.
- The raw-hex library/application surface remains in place:
  - `IBinaryMessagePayload`
  - binary message models
  - `MessagePayloadFormatter`
  - `RawHexSerializer`
  - `SerializerFactory` support for `RawHex`
  - persisted WinUI serializer choice plus localized raw-hex validation
  - automated raw-hex roundtrip coverage and the earlier live WinUI TCP proof
- The bitfield/schema foundation remains in place:
  - `BitFieldDefinition`
  - `BitFieldCodec`
  - `BitFieldPayloadSchema`, `BitFieldPayloadField`, `BitFieldScalarKind`
  - `BitFieldFieldAssignment` / `BitFieldFieldValue`
  - `BitFieldPayloadSchemaValidator`
  - `BitFieldPayloadSchemaCodec`
  - optional `SerializerOptions.BitFieldSchema`
  - `DeviceProfileValidator` enforcement that schema usage is currently valid only with `RawHex`
  - schema-driven `OutboundMessageComposer`
- The endian-aware scope is still intentionally narrow:
  - `BitFieldEndianness` exists on definitions and schema fields
  - `LittleEndian` preserves the existing `payload[0]` LSB = bit `0` rule
  - `BigEndian` is supported only for byte-aligned whole-byte multi-byte fields
  - MSB-first numbering and partial-byte big-endian multi-byte fields remain deferred
- The first real runtime consumer landed on 2026-04-08:
  - WinUI message-composer settings now preserve an optional `BitFieldSchema` from `appsettings.json`
  - `MainViewModel.BuildProfile()` now threads that schema into the live `SerializerOptions`
  - `DeviceLabSessionService` now enriches inbound/outbound logs with decoded field summaries or non-fatal schema decode warnings
  - `MessagePayloadFormatter.TryFormatBitFieldSummary()` now provides a reusable schema summary seam for logs
- Focused validation added in this step:
  - formatter tests for schema summaries and safe schema/payload mismatch handling
  - codec tests for big-endian non-zero offsets and signed big-endian reads
  - schema codec tests for mixed-endian offset fields plus signed big-endian values
- Verification completed with:
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-build`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-build`
  - `dotnet build commlib-codex-full.sln --no-restore`
- One earlier parallel `dotnet` validation attempt hit a transient file lock on `CommLib.Domain.dll`; the subsequent sequential rerun succeeded.
- A 2026-04-08 production-readiness review found that the repo is a strong foundation, but not yet something we should call industrial/runtime-ready because `ConnectionManager` lifecycle state is not synchronized, reconnect behavior stops at connect-time retry, the hosting surface is still minimal, and protocol/serializer extension still goes through central switch factories.
- The first hardening slice against that review landed on 2026-04-08:
  - `ConnectionManager` now keeps one per-device connection state object instead of multiple parallel dictionaries
  - same-device lifecycle operations are serialized through per-device gates
  - `ConnectAsync()` no longer re-opens an already-opened transport
  - background receive failures now surface as `DeviceConnectionException(..., "receive", ...)` and are reported through `IConnectionEventSink`
  - infrastructure tests now cover single-open behavior, same-device concurrent connect serialization, and sticky receive-failure surfacing
- Verification for the hardening slice completed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build src/CommLib.Infrastructure/CommLib.Infrastructure.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-build`
  - `dotnet build commlib-codex-full.sln --no-restore`
- One earlier parallel validation attempt hit a transient file lock on `CommLib.Infrastructure.dll`; the sequential rerun succeeded.
- The protocol-contract alignment slice landed on 2026-04-09:
  - `ProtocolOptions` now exposes only active `LengthPrefixed` settings
  - `LengthPrefixedProtocol` now enforces `MaxFrameLength` on both encode and decode
  - `ProtocolFactory` now passes the configured frame limit into the runtime protocol instance
  - `DeviceProfileValidator` now rejects unsupported protocol types before connect-time work starts
  - sample config/example code no longer advertise inactive CRC/STX/ETX behavior
- Verification for the protocol-contract slice completed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
- The runtime recovery semantics slice landed on 2026-04-09 without widening into core auto-reconnect:
  - post-connect receive failure is terminal inside `ConnectionManager`
  - failed sessions are hidden from `GetSession()` and reject further send/manual-inbound work
  - pending response tasks now fail immediately on receive failure, explicit disconnect, and same-device session replacement
  - `ReconnectOptions` is now explicitly treated as connect-time transport-open retry only
- Verification for the runtime recovery slice completed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
- One attempted parallel `dotnet` validation on 2026-04-09 hit a transient `CommLib.Domain.dll` file lock; the subsequent sequential rerun succeeded.
- `PROGRESS.md` still needs separate encoding normalization before normal in-place automation is safe.

## Next Work Unit
1. Re-check whether the public `ReconnectOptions` / `DeviceProfile.Reconnect` naming is still the right contract now that runtime behavior is explicitly connect-time retry only.
2. Choose between doc-only clarification, a non-breaking alias, or a future rename for that retry contract.
3. Revisit richer hosting concerns such as diagnostics, health checks, or TLS-facing transport options only after that retry-contract naming settles.

## Deferred / Not For This Step
- Keep any core auto-reconnect/state-machine work deferred until a real deployment requirement justifies widening beyond the current terminal-session policy.
- Resume the older UDP / Multicast / pointer-session manual validation pass after the current runtime-hardening slice yields.
- `PROGRESS.md` full encoding normalization stays separate repo hygiene work.
- A future CRC/STX/ETX framing family stays deferred until a concrete device contract requires a new protocol implementation.
- Full templated-control theming stays deferred until WinUI default-style key coverage is mapped safely.
- A WinUI schema editor stays deferred until the config-backed consumer is live-validated and review feedback proves it necessary.
- MSB-first bit numbering and partial-byte big-endian multi-byte fields stay deferred until a real device contract requires them.
