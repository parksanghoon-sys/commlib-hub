# Current Plan

Date: 2026-04-13

## Goal
Continue production-readiness hardening on the clean branch, now that bounded unsolicited inbound buffering, explicit connect/bootstrap validation, hosting-level queue sizing, the first queue-pressure signal, and reconnect-contract clarification are all landed while report-based bootstrap remains an application-level opt-in. The next safe expansion point now depends on concrete deployment requirements for diagnostics/security or on external pressure for a future reconnect rename.

## Confirmed Facts
- The repository continuity rules currently point at `AGENT.md`; a root `AGENT_RULES.md` file is not present.
- Active branch is `feat/runtime-hardening-clean-base`, created from `commlib-hub/main` to replace the overly broad runtime-hardening delivery line.
- The clean branch keeps the already-merged WinUI/localization baseline from PR `#3` and adds only the runtime-hardening slice.
- The landed runtime-hardening behavior now includes:
  - `ConnectionManager` keeps one per-device state object instead of multiple parallel dictionaries.
  - same-device `ConnectAsync` / `DisconnectAsync` calls are serialized through per-device gates.
  - `ConnectAsync()` no longer re-opens a transport that was already opened successfully.
  - background receive-pump failures surface as `DeviceConnectionException(deviceId, "receive", ...)` and become terminal for that session.
  - `GetSession()` hides failed sessions, and later send/manual-inbound work rethrows the stored receive failure instead of acting on a dead session.
  - pending response tasks now fail immediately on receive failure, explicit disconnect, and same-device session replacement.
  - `ProtocolOptions` now exposes only the live `LengthPrefixed` contract (`Type` and `MaxFrameLength`).
  - `LengthPrefixedProtocol` enforces `MaxFrameLength` on both encode and decode.
  - `ProtocolFactory` now passes the configured frame limit into the runtime protocol instance and accepts the built-in protocol name case-insensitively.
  - `DeviceProfileValidator` now rejects unsupported protocol types and too-small frame limits before runtime connection work starts.
  - sample config/example code no longer advertise inactive CRC/STX/ETX framing behavior.
- The bounded unsolicited-inbound slice landed on 2026-04-10:
  - `ConnectionManager` no longer uses `Channel.CreateUnbounded<InboundEnvelope>()` for unmatched inbound messages.
  - each device connection now gets a bounded inbound queue with backpressure-first full behavior (`BoundedChannelFullMode.Wait`).
  - the first slice keeps queue capacity as an internal runtime default (`256`) instead of widening the public profile/hosting surface.
  - same-device disconnect and reconnect now cleanly cancel a receive pump that is blocked waiting for inbound queue capacity.
  - infrastructure tests now prove transport reads stop advancing past queue pressure until the consumer drains messages, and that blocked-writer cleanup still allows reconnect.
- The connect/bootstrap validation slice also landed on 2026-04-10:
  - `DeviceProfileValidator` now lives in `CommLib.Domain.Configuration` so runtime callers can reuse validation without introducing an infrastructure-to-application dependency.
  - `ConnectionManager.ConnectAsync()` now validates `DeviceProfile` before transport/protocol/serializer factories or transport-open retry work runs.
  - `DeviceBootstrapper.StartAsync()` remains the compatibility fail-fast path, but now validates each enabled profile before any runtime side effects start.
  - `DeviceBootstrapper.StartWithReportAsync()` now provides an explicit continue-and-report bootstrap path via `DeviceBootstrapReport` and `DeviceBootstrapFailure`.
  - focused unit/infrastructure tests now prove invalid profiles fail before runtime factories run and that mixed validation/connect failures still produce a usable aggregate bootstrap report.
- The queue/hosting contract slice landed on 2026-04-10:
  - `CommLib.Hosting` now exposes `CommLibRuntimeOptions` with `InboundQueueCapacity`.
  - `AddCommLibCore()` keeps the old default path, while `AddCommLibCore(Action<CommLibRuntimeOptions>)` now lets hosting callers override inbound queue capacity without widening `DeviceProfile`.
  - `ConnectionManager` now has a thin public constructor overload so the hosting layer can pass inbound queue capacity without relying on internal-only seams.
  - focused unit tests now prove the hosting registration uses the default capacity (`256`) and propagates an override into the resolved connection manager.
- The queue-pressure observability slice landed on 2026-04-13:
  - `IConnectionEventSink` now exposes a default no-op `OnInboundBackpressure(deviceId, queueCapacity)` callback so existing external implementations are not forced to change.
  - `ConnectionManager` now emits that callback when the bounded unsolicited inbound queue fills and the receive pump actually blocks waiting for consumer drain.
  - the signal is intentionally best-effort and once-per-pressure-episode rather than a new queue-metrics subsystem.
  - focused infrastructure tests now prove the signal fires once per pressure episode while the existing bounded-queue backpressure behavior still holds.
- The reconnect-contract clarification slice landed on 2026-04-13:
  - the repo keeps the public names `ReconnectOptions` and `DeviceProfile.Reconnect` for compatibility instead of adding an alias or a breaking rename now.
  - XML docs and sample-facing README text now explicitly describe the contract as connect-time transport-open retry only.
  - live-session receive failure remains terminal until a higher layer explicitly reconnects.
- The report-based bootstrap review completed on 2026-04-13:
  - `AddCommLibCore()` already registers `DeviceBootstrapper`, so DI callers can explicitly resolve it and choose `StartAsync()` or `StartWithReportAsync()` today.
  - no extra hosting wrapper or hosted bootstrap abstraction is justified yet because that would also choose lifecycle/reporting semantics the repo still has not proven.
  - the richer bootstrap report path therefore stays an application-level opt-in for now rather than a new hosting contract.
- Latest verification for the clean runtime-hardening branch completed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- Focused verification completed on 2026-04-13:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "ConnectionManagerTests" --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --filter "ServiceCollectionExtensionsTests" --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --filter "DeviceBootstrapperTests" --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- The next production-readiness blockers remain:
  - queue pressure now has a best-effort event-sink signal, but richer metrics/counters/health semantics are still intentionally absent
  - intentionally thin hosting / observability / secure-transport surface
  - any future reconnect rename still needs real external API pressure before compatibility churn is justified

## Next Work Unit
1. Keep the new `IConnectionEventSink.OnInboundBackpressure(...)` signal as the current queue-pressure contract unless real operator requirements justify richer metrics/counters.
2. Revisit the production integration surface for diagnostics, health, and secure transport only when the target deployment environment is explicit enough to choose the right owner.
3. Consider a future reconnect rename only if external package consumers or a stable public API process justify compatibility churn beyond the current doc-only clarification.

## Stop / Reassess Conditions
- If exposing more queue controls starts to widen `DeviceProfile` without a concrete deployment need, keep the contract in `CommLib.Hosting`.
- If real device traffic or operator feedback shows `256` is not a defensible default inbound capacity, revisit the hosting default and whether the best-effort pressure callback needs a richer metrics/counters contract.
- If reconnect naming pressure returns, prefer doc-only clarification or a staged alias before a breaking rename unless the repo has already committed to a stable package migration.
- If later delivery cleanup would require force-pushing review history, prefer a replacement branch/PR over rewriting a branch that is already under review.
