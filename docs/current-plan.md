# Current Plan

## Date
- 2026-04-13

## Current Scope
- Keep the runtime-hardening delivery on a clean branch rooted in `commlib-hub/main`
- Land runtime hardening one safe slice at a time without carrying the raw-hex/bitfield branch lineage
- Keep reconnect-contract clarification as a docs-only compatibility choice for now

## Confirmed State
- `AGENT.md` is the active repository rules file; `AGENT_RULES.md` is not present at the repo root.
- Branch is now `feat/runtime-hardening-clean-base`, created from `commlib-hub/main` as the clean delivery vehicle for the runtime-hardening slice.
- `ConnectionManager` now:
  - keeps one per-device state object
  - serializes same-device lifecycle operations through per-device gates
  - avoids the earlier accidental second `OpenAsync()` call
  - treats background receive failure as terminal for that session
  - hides failed sessions from `GetSession()`
  - fails pending response tasks on receive failure, explicit disconnect, and same-device session replacement
- `ProtocolOptions` / protocol runtime now match:
  - `ProtocolOptions` exposes only `Type` and `MaxFrameLength`
  - `LengthPrefixedProtocol` enforces `MaxFrameLength`
  - `ProtocolFactory` passes the frame limit into the runtime protocol
  - `DeviceProfileValidator` rejects unsupported protocol types and too-small frame limits before connect-time work
- Sample/config/example surfaces no longer imply inactive CRC/STX/ETX framing support.
- Verification completed with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
- `ConnectionManager` now also uses a bounded unsolicited inbound queue with backpressure-first full behavior instead of an unbounded queue.
- The first queue-capacity slice keeps capacity internal (`256`) and proves blocked-writer disconnect/reconnect cleanup through infrastructure tests.
- `CommLib.Hosting` now exposes `CommLibRuntimeOptions` so hosting callers can override `InboundQueueCapacity` without widening `DeviceProfile`.
- `AddCommLibCore()` keeps the default runtime path, and `AddCommLibCore(Action<CommLibRuntimeOptions>)` now passes queue capacity into the resolved `ConnectionManager`.
- `IConnectionEventSink` now exposes a default no-op `OnInboundBackpressure(deviceId, queueCapacity)` callback, and `ConnectionManager` emits it when the bounded inbound queue actually blocks the receive pump.
- `ReconnectOptions` / `DeviceProfile.Reconnect` keep their existing names for compatibility, but the XML docs and sample docs now explicitly describe them as connect-time transport-open retry only.
- `DeviceProfileValidator` now lives in `CommLib.Domain.Configuration` so validation can be enforced at runtime boundaries without adding an infrastructure-to-application dependency.
- `ConnectionManager.ConnectAsync()` now validates profiles before runtime factories or transport-open work starts.
- `DeviceBootstrapper.StartAsync()` remains fail-fast for compatibility, but now validates before connect-time side effects, and `StartWithReportAsync()` provides an explicit continue-and-report startup path.
- Latest verification completed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- `DeviceBootstrapper` is already registered through `AddCommLibCore()`, so callers can resolve it from DI and choose `StartAsync()` or `StartWithReportAsync()` directly without a new hosting wrapper.
- Focused verification on 2026-04-13 also completed with:
  - `ConnectionManagerTests`
  - `ServiceCollectionExtensionsTests`
  - `DeviceBootstrapperTests`
- The next meaningful blockers are richer queue-pressure diagnostics only if a real deployment needs more than the new event-sink callback, the still-thin hosting/ops/security surface, and any future reconnect rename only if external API pressure makes doc-only clarification insufficient.

## Next Work Unit
1. Keep queue-pressure signaling on the current best-effort `IConnectionEventSink` callback unless deployments prove they need richer metrics/counters.
2. Revisit hosting diagnostics, health, and secure transport concerns only when the target deployment environment is explicit enough to choose the right owner.
3. Revisit a reconnect rename only if later package/API stabilization work makes doc-only clarification insufficient.

## Deferred / Not For This Step
- Core-library auto-reconnect/state-machine work stays deferred until a real deployment requires it.
- Richer queue-pressure metrics, counters, or health semantics stay deferred until a concrete operator requirement appears.
- A new framing family for CRC or STX/ETX stays deferred until a concrete device contract requires it.
