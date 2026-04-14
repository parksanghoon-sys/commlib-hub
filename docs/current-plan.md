# Current Plan

## Date
- 2026-04-14

## Current Scope
- Refresh local `main` once through `integration/main-refresh-20260414`, then restart from a fresh branch.
- Keep the first post-merge implementation slice limited to the remaining `DeviceSession` timeout-cancellation cleanup.

## Confirmed State
- `AGENT.md` is the active repository rules file; `AGENT_RULES.md` is not present at the repo root.
- The temporary integration line already contains the clean feature/runtime branches plus the two local 2026-04-14 commits:
  - `feat/bitfield-endianness`
  - `feat/bitfield-schema-log-enrichment`
  - `feat/runtime-hardening-clean-base`
  - `fix(runtime): reclaim disconnected device operation gates`
  - `docs(repo): add Korean XML documentation`
- The runtime baseline now includes bounded unsolicited inbound buffering, inbound backpressure events, hosting queue-capacity configuration, connect-boundary profile validation, and `DeviceBootstrapper.StartWithReportAsync()`.
- The next still-open correctness issue remains in `src/CommLib.Application/Sessions/DeviceSession.cs` where `HandleTimeoutAsync()` still calls `Task.Delay(timeout)` without a cancellation token.
- Focused validation on the integration line succeeded with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet restore examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`

## Next Work Unit
1. Merge the temporary integration branch into local `main`.
2. Create a fresh branch from the updated `main`.
3. Fix the `DeviceSession` timeout cleanup path with the smallest supporting unit coverage.

## Deferred / Not For This Step
- Hosted-service wiring stays deferred.
- Reconnect naming truthfulness stays deferred.
- Broader diagnostics / health / TLS surface stays deferred.
- Extra serializer/WinUI follow-up stays deferred until after the timeout cleanup branch lands.
