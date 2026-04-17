# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-04-17

## Current Scope
- Finish the repository-level public-ready cleanup without changing the runtime contract in `src/`
- Keep the work focused on GitHub cleanup, repo-facing docs, package metadata, and publication blockers

## Confirmed State
- PR `#25` merged the outstanding integration batch into `commlib-hub/main`.
- Remote feature/cleanup branches have been deleted from `commlib-hub`; only `main` remains on the remote.
- Open PR count is now zero.
- The validated `.github/workflows/ci.yml` is still missing from `main` because the available credentials in this environment could not write workflow files.
- Stale issues `#21` and `#23` are still open only because the available GitHub app/PAT could not update issue state.
- Root `README.md` is now a public-facing overview with honest contract notes.
- Package metadata is centralized in `Directory.Build.props`, and `README.md` is packed into NuGet packages.
- A Windows CI workflow has already been authored and validated locally; it still needs to be restored onto `main`.
- XML documentation output is enabled only for packable library projects under `src/`, not for tests/examples.
- The remaining library XML warning gaps were fixed in `DeviceSession`, `LengthPrefixedProtocol`, and `ConnectionManager`.
- Verification passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore -v minimal`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore -v minimal`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore -v minimal`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --no-restore -p:PackageVersion=0.1.0-local5 -o artifacts/pack -v minimal`
- The repo is not fully publication-ready yet because `LICENSE` is still missing, `.github/workflows/ci.yml` is still absent from `main`, and issues `#21` / `#23` are still open.

## Next Work Unit
1. Restore `.github/workflows/ci.yml` onto `main` with credentials that can write workflow files.
2. Close stale issues `#21` and `#23` with credentials that can write issue state.
3. Return to the remaining publication blocker: choose the root `LICENSE` and add the matching file.

## Not In This Step
- No new runtime/API hardening
- No new WinUI/feature work
- No guessed legal/license text
