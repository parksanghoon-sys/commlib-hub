# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-04-18

## Current Scope
- Resume the next runtime/application follow-up work from a truthful, publication-ready repository baseline
- Keep the next work focused on the smallest safe next slice after the reconnect/bootstrap follow-up bundle

## Confirmed State
- `main` now includes the publication baseline, the `DeviceSession` cleanup, the quick-start guide, and the inbound-frame seam cleanup through `63c89a4`.
- The current follow-up branch now adds two more truthful follow-ups on top of that baseline:
  - `ReconnectOptions` is documented more explicitly as connect-time transport-open retry only
  - `DeviceBootstrapper.StartAsync()` now validates enabled profiles first and starts their connection attempts concurrently
- `StartWithReportAsync()` still keeps its continue-and-report semantics, while concurrent `StartAsync()` failures now aggregate only when more than one connection fails at once.
- Verification for this slice passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceBootstrapperTests`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`

## Next Work Unit
1. Resume the deferred WinUI manual validation pass for UDP / Multicast / real-pointer behavior and only adjust UI/status copy if that live pass exposes a real confusion point.

## Not In This Step
- No new repository-publication cleanup
- No broader reconnect-state-machine, queue-pressure, or observability/TLS redesign
- No new transport/protocol family expansion
