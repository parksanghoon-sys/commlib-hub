# Current Plan

## Date
- 2026-04-09

## Current Scope
- Keep the runtime-hardening delivery on a clean branch rooted in `commlib-hub/main`
- Land connection lifecycle hardening, truthful length-prefixed protocol contracts, and terminal session-failure semantics without carrying the raw-hex/bitfield branch lineage

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
- The next meaningful blockers are unbounded inbound buffering, missing connect/bootstrap validation policy, fail-fast bootstrap semantics, and the still-thin hosting/ops/security surface.

## Next Work Unit
1. Add bounded unsolicited inbound buffering and an explicit overflow/backpressure policy in `ConnectionManager`.
2. Enforce profile validation at the connect/bootstrap boundary and choose the startup policy for partial failures.
3. Revisit hosting diagnostics, health, and secure transport concerns once runtime memory and startup semantics are explicit.

## Deferred / Not For This Step
- Core-library auto-reconnect/state-machine work stays deferred until a real deployment requires it.
- `ReconnectOptions` naming cleanup stays behind the first memory/startup hardening slices.
- A new framing family for CRC or STX/ETX stays deferred until a concrete device contract requires it.
