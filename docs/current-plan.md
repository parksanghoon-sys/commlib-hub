# Current Plan

## Date
- 2026-04-16

## Current Scope
- Keep `feat/hosting-lifecycle-wiring` limited to configuration-bound Generic Host lifecycle wiring for `AddCommLibCore()`.
- Leave reconnect naming, diagnostics/health/TLS, and WinUI/manual follow-up outside this branch.

## Confirmed State
- `AGENT.md` remains the active repository rules file at the repo root.
- `AddCommLibCore(IConfiguration, ...)` now binds `CommLibOptions` from either the root config or the `CommLib` section.
- The configuration-bound registration path now adds `CommLibHostedService`, which:
  - maps only enabled raw device profiles
  - reuses `DeviceBootstrapper.StartAsync()` for fail-fast startup
  - reuses `ConnectionManager.DisposeAsync()` for host-stop cleanup
- Focused validation succeeded with:
  - `dotnet restore tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`

## Next Work Unit
1. Commit `feat/hosting-lifecycle-wiring` as its own reviewable slice.
2. Publish/push it only if the user wants a PR right now.
3. After it lands, move to a fresh branch for `ReconnectOptions` naming truthfulness.

## Deferred / Not For This Step
- Configurable hosted startup/reporting policy stays deferred.
- Reconnect naming truthfulness stays deferred to the next branch.
- Broader diagnostics / health / TLS surface stays deferred.
- Extra serializer/WinUI follow-up stays deferred.
