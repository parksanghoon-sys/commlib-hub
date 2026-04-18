# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-04-18

## Current Scope
- Resume the next runtime/application follow-up work from a truthful, publication-ready repository baseline
- Keep the next work focused on a narrow code-local slice rather than more repo-publication cleanup

## Confirmed State
- `main` now includes the publication baseline, the `DeviceSession` pending-entry cleanup, and the merged quick-start guide at `f6c3cfd`.
- Active work continues on fresh branch `cleanup/inbound-frame-seam` from that baseline.
- `ConnectionManager.TryHandleInboundFrame(...)` was only used by the repo's infrastructure tests, so the ambiguous dual-entry inbound seam has now been narrowed from `public` to `internal`.
- The existing `IConnectionEventSink` registration path through `AddCommLibCore()` is now explicitly proven by unit coverage and documented in `docs/quick-start.md`.
- Verification for this slice passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter ServiceCollectionExtensionsTests`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`

## Next Work Unit
1. Reassess the public `ReconnectOptions` / `DeviceProfile.Reconnect` contract and decide whether the current connect-time-only semantics need stronger doc-only clarification or a staged alias/deprecation path.

## Not In This Step
- No new repository-publication cleanup
- No new WinUI validation or transport-feature work
- No broad observability/TLS surface expansion beyond the already-supported `IConnectionEventSink` seam
