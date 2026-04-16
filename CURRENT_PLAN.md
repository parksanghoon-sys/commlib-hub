# Current Plan

Date: 2026-04-16

## Goal
Deliver a clean replacement for stale PR `#8` on top of the current `main`, limited to configuration-bound `AddCommLibCore(...)` registration plus Generic Host lifecycle wiring.

## Confirmed Facts
- Active branch is `feat/hosting-lifecycle-wiring-main-base`, created from the current `commlib-hub/main` after PR `#18` merged the `DeviceSession` timeout cleanup.
- The older PR `#8` is no longer a safe merge candidate:
  - GitHub reports it as not mergeable against current `main`
  - its diff has widened far past hosting lifecycle wiring and now includes many unrelated files
- The clean replacement on this branch intentionally keeps only the hosting-lifecycle slice:
  - `AddCommLibCore(this IServiceCollection, IConfiguration)` binds `CommLibOptions`
  - `CommLibHostedService` starts enabled configured device profiles through `DeviceBootstrapper`
  - `CommLibHostedService` disposes the connection manager on host stop
  - focused unit coverage exercises the hosted service and configuration registration path
- This branch does **not** pull in the not-yet-merged runtime-surface pieces from PR `#5`:
  - no `CommLibRuntimeOptions`
  - no inbound queue-capacity wiring
  - no broader diagnostics or `IConnectionEventSink` surface work
- Validation succeeded on 2026-04-16:
  - `dotnet restore tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore --filter "FullyQualifiedName~CommLibHostedServiceTests"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`

## Next Work Unit
1. Commit the hosting-lifecycle code/test slice separately from the continuity-file updates.
2. Publish this branch as the clean replacement for PR `#8`.
3. Close or supersede the stale PR `#8`, then move to the next runtime-facing open line on a fresh branch.

## Next Slice Design
1. Keep this branch limited to hosting registration, hosted-service lifecycle hookup, and the tests required to prove that path.
2. Do not widen into runtime-hardening work from PR `#5`, especially queue-capacity, reconnect, or observability surface changes.
3. Treat any follow-up around `IConnectionEventSink`, health checks, or richer host options as separate backlog items after this replacement is published.

## Stop / Reassess Conditions
- If the clean replacement starts needing `CommLibRuntimeOptions` or other PR `#5` dependencies to stay coherent, stop and split the lines instead of re-coupling them.
- If validation starts failing outside the hosting/unit surface, verify whether the branch accidentally picked up unrelated changes.
