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
- Issues `#21` and `#23` are now closed because their work is already on `main`.
- `.github/workflows/ci.yml` is still missing from GitHub `main`, but local branch `chore/repo-finish` restores the validated workflow content and verifies it successfully.
- `chore/repo-finish-publishable` intentionally carries only the non-workflow subset of the repo-publication cleanup so the MIT/license/root-policy changes can still be pushed now.
- Publishing the full `chore/repo-finish` branch is still blocked in this environment because `git push` lacks workflow-file scope and the GitHub app integration cannot create/update refs in this repo.
- MIT has now been chosen and this branch adds the root `LICENSE` file plus `PackageLicenseExpression=MIT`.
- Root `README.md` is now a public-facing overview with honest contract notes.
- Package metadata is centralized in `Directory.Build.props`, and `README.md` is packed into NuGet packages.
- A Windows CI workflow has already been restored on this branch; it still needs to land on `main`.
- XML documentation output is enabled only for packable library projects under `src/`, not for tests/examples.
- The remaining library XML warning gaps were fixed in `DeviceSession`, `LengthPrefixedProtocol`, and `ConnectionManager`.
- Verification passed on this branch with:
  - `dotnet restore commlib-codex-full.sln`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-local-mit -o artifacts/pack-mit`
- The generated `CommLib.Domain.0.1.0-local-mit.nupkg` contains the expected MIT license expression, readme metadata, and repository metadata.
- The repo is not fully publication-ready yet because `.github/workflows/ci.yml` is restored only on local branch `chore/repo-finish`.

## Next Work Unit
1. Land the pushable non-workflow cleanup branch, then publish local branch `chore/repo-finish` with credentials that can update workflow files on GitHub.

## Not In This Step
- No new runtime/API hardening
- No new WinUI/feature work
- No guessed legal/license text
