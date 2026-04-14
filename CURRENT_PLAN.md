# Current Plan

Date: 2026-04-14

## Goal
Use the temporary integration line to refresh local `main` exactly once, then restart from a fresh post-merge branch with the smallest remaining correctness fix.

## Confirmed Facts
- The repository continuity rules currently point at `AGENT.md`; a root `AGENT_RULES.md` file is not present.
- `integration/main-refresh-20260414` is a temporary landing branch rooted in `commlib-hub/main`.
- The integration line now contains:
  - `feat/bitfield-endianness`
  - `feat/bitfield-schema-log-enrichment`
  - `feat/runtime-hardening-clean-base`
  - `fix(runtime): reclaim disconnected device operation gates`
  - `docs(repo): add Korean XML documentation`
- The runtime/hosting line already includes the earlier clean hardening work:
  - bounded unsolicited inbound buffering
  - `IConnectionEventSink.OnInboundBackpressure`
  - `CommLibRuntimeOptions.InboundQueueCapacity`
  - connect-boundary `DeviceProfileValidator` enforcement
  - `DeviceBootstrapper.StartWithReportAsync()`
- The remaining correctness gap confirmed in the merged code is still in `DeviceSession`:
  - `HandleTimeoutAsync()` still uses `Task.Delay(timeout)` without a cancellation token
  - timeout background tasks can outlive session disposal unless that path is cleaned up explicitly
- Focused validation on the integration line succeeded sequentially:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet restore examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`

## Next Work Unit
1. Merge `integration/main-refresh-20260414` into local `main` as the one refresh step the user requested.
2. Create a fresh branch from the updated `main`.
3. On that new branch, fix the `DeviceSession` timeout-cancellation cleanup bug with focused unit coverage.

## Next Slice Design
1. Keep the next branch narrow: `src/CommLib.Application/Sessions/DeviceSession.cs` plus the smallest supporting test updates.
2. Do not widen that slice into reconnect policy, hosted-service wiring, or more serializer/WinUI work.
3. Prefer a fix that cancels timeout tasks at the same lifecycle boundary that removes/fails pending responses.

## Stop / Reassess Conditions
- If the `DeviceSession` timeout fix starts pressuring a broader pending-response abstraction change, split that reflection cleanup into a later follow-up instead of combining both.
- If a fresh post-merge branch reveals any missing integration validation outside the timeout path, record it explicitly in `TODOS.md` rather than widening the first restart slice.
