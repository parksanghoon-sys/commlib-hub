# Current Plan

Date: 2026-04-16

## Goal
Keep `feat/hosting-lifecycle-wiring` limited to configuration-bound Generic Host lifecycle wiring for `AddCommLibCore()`, with the branch ready for commit/review and the next contract-truthfulness slice queued separately.

## Confirmed Facts
- The repository continuity rules currently point at `AGENT.md`; a root `AGENT_RULES.md` file is not present.
- Active branch is `feat/hosting-lifecycle-wiring`, created fresh from refreshed local `main`.
- A GitHub issue search against `parksanghoon-sys/commlib-hub` found no existing issue that already tracks this host-lifecycle wiring slice, so the branch name plus `TODOS.md` remain the active tracking handle.
- `CommLib.Hosting` now has the first Generic Host wiring path:
  - `AddCommLibCore(IConfiguration, ...)` binds `CommLibOptions` from either the root configuration or the `CommLib` section
  - `AddCommLibCore(IConfiguration, ...)` registers `CommLibHostedService` only for the configuration-bound hosting path
  - `CommLibHostedService` maps only enabled raw device profiles before startup, then reuses `DeviceBootstrapper.StartAsync()` for fail-fast startup
  - `CommLibHostedService.StopAsync()` reuses `ConnectionManager.DisposeAsync()` through `IAsyncDisposable` cleanup
- Focused validation succeeded:
  - `dotnet restore tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`

## Next Work Unit
1. Commit this branch as the dedicated hosting-lifecycle slice.
2. If the user wants publication, push this branch and open a PR from it as its own narrow review unit.
3. After this branch lands, start a fresh branch for `ReconnectOptions` naming truthfulness.

## Next Slice Design
1. Do not add reconnect-policy renaming, diagnostics, or TLS surface work to `feat/hosting-lifecycle-wiring`.
2. Keep the next branch focused on whether `ReconnectOptions` should stay as docs-only clarification or move toward staged aliasing/deprecation.
3. Treat configurable hosted startup/reporting policy as a later follow-up unless a concrete deployment requirement appears.

## Stop / Reassess Conditions
- If publishing this branch would require mixing unrelated state-file churn from another worktree, stop and keep this branch local until the publish path is clean.
- If `ReconnectOptions` review uncovers an already-consumed external config contract, split doc clarification from any compatibility alias/deprecation work.
