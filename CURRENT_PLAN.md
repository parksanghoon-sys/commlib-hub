# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-04-18

## Goal
Resume the highest-priority runtime/application follow-up work from a truthful, publication-ready repository baseline.

## Confirmed Facts
- `main` now contains the repository-publication baseline, the `DeviceSession` pending-entry cleanup, and the root-linked quick-start guide merged at commit `f6c3cfd`.
- Active work now continues on fresh branch `cleanup/inbound-frame-seam` created from that updated `main`.
- `ConnectionManager.TryHandleInboundFrame(...)` was the only remaining ambiguous dual-entry inbound seam on the concrete runtime type, and the only repo callers were the infrastructure tests.
- That method is now `internal` instead of `public`, so the manual frame-decoding path stays available only to in-repo tests through the existing `InternalsVisibleTo("CommLib.Infrastructure.Tests")` boundary.
- The caller-supplied `IConnectionEventSink` DI path is now explicitly proven and documented:
  - `AddCommLibCore()` already resolves a registered `IConnectionEventSink`
  - `docs/quick-start.md` now shows the registration path
  - `ServiceCollectionExtensionsTests` now verifies that a caller-registered sink reaches `ConnectionManager`
- Verification for today's runtime cleanup/documentation slice passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter ServiceCollectionExtensionsTests`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`

## Next Work Unit
1. Reassess the public `ReconnectOptions` / `DeviceProfile.Reconnect` contract and decide whether the current connect-time-only semantics need a stronger doc-only clarification or a staged alias/deprecation path.

## Next Slice Design
1. Keep the next slice contract-focused and evidence-backed: inventory repo-facing references first, then change docs or API shape only if the current naming is still materially misleading.
2. Do not reopen repository-publication cleanup, WinUI validation, or broader observability/TLS work while this contract-truthfulness slice is still unresolved.
3. Keep `docs/quick-start.md` as the canonical getting-started/test-run entry point and avoid scattering new duplicate command blocks.
4. Continue to branch fresh from `commlib-hub/main`, not from preserved mixed or publication branches.

## Stop / Reassess Conditions
- If clarifying `ReconnectOptions` starts forcing a wider breaking-change discussion across package compatibility, config shape, and example UX, stop and split the naming decision from any larger API migration.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
