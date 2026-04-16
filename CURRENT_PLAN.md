# Current Plan

Date: 2026-04-16

## Goal
Deliver issue `#23` as a clean bootstrap-validation/reporting slice on top of the bounded-inbound branch, without mixing queue-pressure signaling, reconnect-contract naming, or DI-surface expansion into the same review line.

## Confirmed Facts
- Active branch is `feat/issue-23-bootstrap-reporting`, created from `feat/issue-21-bounded-inbound-buffering` so this slice can stack directly on draft PR `#22`.
- GitHub tracking for this slice is issue `#23`: `Port bootstrap validation/report flow as a clean runtime slice`.
- This branch now carries only the bootstrap validation/reporting slice:
  - `ConnectionManager.ConnectAsync()` validates `DeviceProfile` before runtime factories or transport-open side effects run
  - `DeviceBootstrapper.StartAsync()` keeps fail-fast behavior for invalid enabled profiles
  - `DeviceBootstrapper.StartWithReportAsync()` continues across validation/connect failures and returns `DeviceBootstrapReport`
  - `DeviceBootstrapFailure` / `DeviceBootstrapReport` capture bootstrap failures without aborting the whole pass
  - console and WinUI example callers no longer need to invoke `DeviceProfileValidator` manually before `ConnectAsync()`
- This branch intentionally does **not** yet add:
  - queue-pressure signaling or hosting-level queue-capacity options
  - `ReconnectOptions` wording changes
  - `DeviceSession` reflection removal
  - `IConnectionEventSink` DI-surface changes
  - `DeviceProfileValidator` namespace relocation to `CommLib.Domain`
- Validation succeeded on 2026-04-16:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet restore examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj`
  - `dotnet restore examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore -nodeReuse:false -maxcpucount:1`

## Next Work Unit
1. Commit the bootstrap validation/reporting slice and the continuity-file updates as separate commits.
2. Push `feat/issue-23-bootstrap-reporting` to `commlib-hub`.
3. Open a stacked draft PR against `feat/issue-21-bounded-inbound-buffering`.

## Next Slice Design
1. Keep this branch reviewable as one bootstrap-correctness/reporting slice.
2. Treat queue-pressure signaling as the next likely runtime/hosting follow-up after this PR is published.
3. Revisit `DeviceProfileValidator` relocation only if review feedback shows the current namespace placement is actively hurting clarity or coupling.

## Stop / Reassess Conditions
- If review feedback insists that bootstrap validation must move to `CommLib.Domain` in the same line, decide explicitly whether to widen this branch or defer the move into a separate configuration-ownership slice.
- If additional behavior is needed outside bootstrap/reporting and direct `ConnectAsync()` validation, reassess whether it belongs here or in the next runtime slice.
