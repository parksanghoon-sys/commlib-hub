# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-04-18

## Goal
Resume the highest-priority runtime/application follow-up work from a truthful, publication-ready repository baseline.

## Confirmed Facts
- `main` now includes the publication baseline, the `DeviceSession` pending-entry cleanup, the quick-start guide, and the inbound-frame seam cleanup merged through commit `63c89a4`.
- The current follow-up branch `docs/reconnect-contract-clarity` now closes two more gaps on top of that baseline:
  - public docs and sample READMEs now state explicitly that `Reconnect` applies only to transport-open retries inside the initial `ConnectAsync()` path
  - `DeviceBootstrapper.StartAsync()` now validates all enabled profiles first, then starts their `ConnectAsync()` calls concurrently instead of awaiting them serially
- `DeviceBootstrapper.StartWithReportAsync()` intentionally keeps its existing continue-and-report behavior; only the fail-fast `StartAsync()` path changed in this slice.
- Concurrent bootstrap failure reporting is now explicit:
  - a single startup failure is still rethrown directly
  - multiple simultaneous startup failures are surfaced as `AggregateException`
  - a later invalid enabled profile now blocks startup before any connection attempt begins
- Verification for the reconnect/bootstrap follow-up bundle passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceBootstrapperTests`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`

## Next Work Unit
1. Resume the deferred WinUI validation pass for UDP / Multicast / real-pointer behavior, and only tighten UI/status copy if that live pass exposes a concrete operator-facing confusion point.

## Next Slice Design
1. Keep the next slice manual-validation-focused and avoid reopening runtime/bootstrap behavior unless the live WinUI pass proves a real issue.
2. Treat queue-pressure signaling as still deferred because there is still no concrete operator requirement for a stronger public/runtime signal.
3. Keep `docs/quick-start.md` as the canonical getting-started/test-run entry point and avoid scattering new duplicate command blocks.
4. Continue to branch fresh from `commlib-hub/main`, not from preserved mixed or publication branches.

## Stop / Reassess Conditions
- If the WinUI validation pass finds a real transport/runtime bug rather than a UI-only confidence gap, stop and split that runtime fix from the manual-validation slice before widening scope.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
