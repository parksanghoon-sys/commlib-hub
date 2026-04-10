# Current Plan

Date: 2026-04-10

## Goal
Continue production-readiness hardening on the clean branch, with bounded unsolicited inbound buffering plus explicit connect/bootstrap validation semantics now landed and the next safe slice narrowed to queue/hosting contract decisions.

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
- Latest verification for the clean runtime-hardening branch completed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- The next production-readiness blockers remain:
  - inbound queue capacity and pressure observability are still internal-only defaults rather than an explicit public/runtime contract
  - the richer bootstrap report path is application-level only and is not yet surfaced through hosting/DI conventions
  - intentionally thin hosting / observability / secure-transport surface

## Next Work Unit
1. Decide whether inbound queue capacity and pressure signaling should stay internal or become a real hosting/runtime option now that startup semantics are explicit.
2. Decide whether hosting/DI should expose the new `StartWithReportAsync()` partial-startup path or keep it as an application-level opt-in.
3. Revisit hosting diagnostics, health, and secure transport options only after queue and startup surface decisions are explicit.

## Stop / Reassess Conditions
- If making queue sizing public starts to widen `DeviceProfile` without a concrete deployment need, prefer a hosting/runtime option or keep the current internal default.
- If surfacing `StartWithReportAsync()` through hosting starts to imply a larger bootstrap orchestration redesign, keep the current explicit application-level path and defer the wider hosting shape.
- If real device traffic or operator feedback shows `256` is not a defensible default inbound capacity, revisit whether queue sizing or pressure events need a first-class runtime option rather than silently tuning a private constant.
- If later delivery cleanup would require force-pushing review history, prefer a replacement branch/PR over rewriting a branch that is already under review.
