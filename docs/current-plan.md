# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-04-17

## Current Scope
- Resume the next runtime/application follow-up work from a truthful, publication-ready repository baseline
- Keep the next work focused on a narrow code-local slice rather than more repo-publication cleanup

## Confirmed State
- PR `#25` merged the outstanding integration batch into `commlib-hub/main`.
- Remote feature/cleanup branches have been deleted from `commlib-hub`; only `main` remains on the remote.
- Open PR count is now zero.
- PR `#26` merged the MIT/license/root-policy cleanup into `main`.
- Issues `#21` and `#23` are now closed because their work is already on `main`.
- `.github/workflows/ci.yml` has now been restored on top of the updated `main` baseline through the dedicated minimal branch `chore/restore-ci-workflow`.
- MIT has now been chosen and this branch adds the root `LICENSE` file plus `PackageLicenseExpression=MIT`.
- Root `README.md` is now a public-facing overview with honest contract notes.
- Package metadata is centralized in `Directory.Build.props`, and `README.md` is packed into NuGet packages.
- A Windows CI workflow is now part of the live `main` branch state.
- XML documentation output is enabled only for packable library projects under `src/`, not for tests/examples.
- The remaining library XML warning gaps were fixed in `DeviceSession`, `LengthPrefixedProtocol`, and `ConnectionManager`.
- Verification passed on this branch with:
  - `dotnet restore commlib-codex-full.sln`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-local-mit -o artifacts/pack-mit`
- The generated `CommLib.Domain.0.1.0-local-mit.nupkg` contains the expected MIT license expression, readme metadata, and repository metadata.
- The repository-level public/open-source readiness cleanup is now complete.
- `docs/quick-start.md` now provides the single root-linked quick guide for restore/build, test runs, example entry points, and baseline host/manual usage.
- `DeviceSession` pending-response tracking is now simplified on branch `cleanup/device-session-pending-entry`:
  - typed pending entries replaced reflection-based completion/exception dispatch
  - redundant pending-store and timeout-registry state were removed
  - focused unit and infrastructure tests pass after the cleanup

## Next Work Unit
1. Reassess `ConnectionManager.TryHandleInboundFrame` and remove or internalize the ambiguous dual-entry inbound path if no real external caller depends on it.

## Not In This Step
- No new runtime/API hardening
- No new WinUI/feature work
- No guessed legal/license text
