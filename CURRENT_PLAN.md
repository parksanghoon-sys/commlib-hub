# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-04-18

## Goal
Resume the highest-priority runtime/application follow-up work from a truthful, publication-ready repository baseline.

## Confirmed Facts
- `main` now includes the publication baseline, the `DeviceSession` pending-entry cleanup, the quick-start guide, the inbound-frame seam cleanup, and the reconnect/bootstrap follow-up bundle through commit `ef00908`.
- The reconnect/bootstrap follow-up bundle is now the new baseline on `main`:
  - public docs and sample READMEs state explicitly that `Reconnect` applies only to transport-open retries inside the initial `ConnectAsync()` path
  - `DeviceBootstrapper.StartAsync()` validates all enabled profiles first, then starts their `ConnectAsync()` calls concurrently
  - `DeviceBootstrapper.StartWithReportAsync()` still keeps the earlier continue-and-report semantics
- A live WinUI validation pass has now closed the earlier UDP / Multicast / transport-panel confidence gap without requiring product-code changes:
  - `Device Lab` transport switching worked across `TCP`, `UDP`, `Multicast`, and `Serial`
  - `Settings` transport switching reflected the same shared selection state and showed the expected transport-specific fields
  - the UDP in-app mock path completed a real connect/send/echo roundtrip with the expected live-log entries
  - the multicast in-app mock path completed a real connect/send roundtrip, and the external `MulticastReceive` helper also observed the outbound frame
  - on this machine the multicast live log showed one inbound line, while the existing status copy already surfaced the potential self-traffic / peer-echo note for other loopback environments
- A later live WinUI `RawHex` / `BitFieldSchema` pass exposed one real wiring gap instead of a broader design problem:
  - `messageComposer.bitFieldSchema` was loading into `DeviceLabSettingsViewModel`, but `MainViewModel.BuildProfile()` was not forwarding it into `SerializerOptions`
  - forwarding `Settings.BitFieldSchema` into `SerializerOptions.BitFieldSchema` was the only code change needed to make the existing config-backed schema-log path work end-to-end
  - after that fix, a live TCP in-app mock roundtrip produced the expected outbound and inbound `fields[prefix=170, register=4660, tail=127]` summaries for `AA 12 34 7F`
  - the WinUI README now includes the validated runtime `messageComposer.bitFieldSchema` example instead of leaving that path implicit
- Verification for the latest live/runtime follow-up bundle passed with:
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceBootstrapperTests`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - live WinUI UIAutomation-assisted passes for UDP mock roundtrip, multicast mock roundtrip, external multicast helper receive, `Device Lab` / `Settings` transport switching, and a TCP `RawHex` / `BitFieldSchema` roundtrip with schema-enriched logs

## Next Work Unit
1. Package the now-proven WinUI live-validation workflow into the repo-owned helper/doc path so future sessions can rerun UDP, multicast, and `RawHex` / schema checks without rediscovering ad-hoc local steps.

## Next Slice Design
1. Keep the next slice WinUI-local and reuse-first: extend the existing local validation guidance instead of inventing a new runtime or serializer surface.
2. Treat queue-pressure signaling as still deferred because there is still no concrete operator requirement for a stronger public/runtime signal.
3. Keep `docs/quick-start.md` as the canonical getting-started/test-run entry point and avoid scattering new duplicate command blocks.
4. Continue to branch fresh from `commlib-hub/main`, not from preserved mixed or publication branches.

## Stop / Reassess Conditions
- If packaging the WinUI validation workflow starts demanding brittle UI-automation infrastructure or a larger app redesign, stop at the smallest documentation-only capture instead of widening the helper scope.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
