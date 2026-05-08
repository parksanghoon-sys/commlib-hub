# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-04-18

## Current Scope
- Continue from the truthful `main` baseline after the reconnect/bootstrap follow-up bundle landed
- Keep the next step close to the current WinUI validation context instead of widening into a larger runtime redesign

## Confirmed State
- `main` now includes the publication baseline, the `DeviceSession` cleanup, the quick-start guide, the inbound-frame seam cleanup, and the reconnect/bootstrap follow-up bundle through `ef00908`.
- The latest live validation pass closed the older UDP / Multicast / transport-panel confidence gap without requiring product-code changes:
  - `Device Lab` and `Settings` both switched transport-specific panels correctly
  - the UDP in-app mock completed a real connect/send/echo roundtrip
  - the multicast in-app mock completed a real connect/send roundtrip, and the external `MulticastReceive` helper also observed the outbound frame
  - the existing multicast status copy already remained sufficient on this machine
- The later WinUI `RawHex` / `BitFieldSchema` pass found one concrete bug, not a broader contract failure:
  - `messageComposer.bitFieldSchema` already loaded from runtime settings, but `MainViewModel.BuildProfile()` was not forwarding it into `SerializerOptions`
  - forwarding that property was enough to make the existing schema-log path work
  - the live TCP in-app mock pass then produced the expected outbound and inbound `fields[prefix=170, register=4660, tail=127]` summaries for `AA 12 34 7F`
- Verification for the latest bundle passed with:
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceBootstrapperTests`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - live WinUI UIAutomation-assisted passes for UDP mock roundtrip, multicast mock roundtrip, helper multicast receive, transport-panel switching, and a TCP `RawHex` / schema-log roundtrip

## Next Work Unit
1. Package the now-proven WinUI live-validation workflow into repo-owned helper/docs so future sessions can rerun the transport and `RawHex` / schema checks without rediscovery.

## Not In This Step
- No new repository-publication cleanup
- No queue-pressure, reconnect-orchestration, or observability/TLS redesign
- No new transport/protocol family expansion
