# Current Plan

Date: 2026-04-09

## Goal
Land the runtime-hardening slice on a clean base rooted in `commlib-hub/main`, with truthful length-prefixed protocol contracts and explicit terminal session-failure semantics now in place.

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
- Verification completed for the runtime-hardening slice with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
- The next production-readiness blockers remain:
  - unbounded unsolicited inbound buffering inside `ConnectionManager`
  - profile validation still not being enforced consistently at the connect/bootstrap boundary
  - bootstrap policy still stopping the whole startup on the first connect failure
  - intentionally thin hosting / observability / secure-transport surface

## Next Work Unit
1. Replace the unbounded unsolicited inbound queue in `ConnectionManager` with a bounded queue and explicit overflow/backpressure behavior.
2. After runtime memory behavior is explicit, enforce profile validation at the runtime connect/bootstrap boundary and choose the startup policy for partial failures.
3. Revisit hosting diagnostics, health, and secure transport options only after runtime memory and startup semantics are explicit.

## Stop / Reassess Conditions
- If bounded inbound buffering pressures API/config surface too early, keep the first slice small and prefer internal defaults plus focused tests before widening hosting options.
- If connect-boundary validation or bootstrap policy work begins to require a larger hosting redesign, pause and choose the smallest mergeable boundary first.
- If later delivery cleanup would require force-pushing review history, prefer a replacement branch/PR over rewriting a branch that is already under review.
