# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-04-17

## Goal
Raise repository-level public/open-source readiness without changing the core runtime contract in `src/`.

## Confirmed Facts
- PR `#25` (`[codex] integrate outstanding runtime and repo follow-ups`) is now merged into `commlib-hub/main`.
- The previously outstanding runtime/messaging/repo-polish slices are now on `main`; only GitHub-side cleanup blockers remain from this integration pass.
- All remote feature/cleanup branches that were part of the older delivery lines have been deleted from `commlib-hub`; only `main` remains on the remote.
- GitHub issues `#21` and `#23` are now closed because their work is already present on `main`.
- `.github/workflows/ci.yml` is still missing from GitHub `main`, but local branch `chore/repo-finish` restores the validated workflow content and verifies it successfully.
- Publishing `chore/repo-finish` is still blocked in this environment:
  - `git push` rejects workflow-file updates because the available PAT lacks `workflow` scope
  - the GitHub app integration returns `403 Resource not accessible by integration` even for remote branch creation
- The root `README.md` is now a public-facing overview instead of an internal scratch document.
- The maintainer has now chosen MIT, and this branch adds the root `LICENSE` file plus `PackageLicenseExpression=MIT` in `Directory.Build.props`.
- Repository/package polish is now in place:
  - central package metadata lives in `Directory.Build.props`
  - package license metadata now uses `MIT`
  - `README.md` is packed into NuGet packages
  - `.github/workflows/ci.yml` has been restored on this branch and still needs to land on `main`
  - root/test project descriptions were normalized to clean English text
- XML documentation generation is now scoped to the packable library projects under `src/` rather than the entire repo, which removed test/example warning noise while keeping library documentation output enabled.
- The remaining library-side XML warnings were fixed in:
  - `src/CommLib.Application/Sessions/DeviceSession.cs`
  - `src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs`
  - `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs`
- Verification completed successfully on this branch with the workflow-aligned commands:
  - `dotnet restore commlib-codex-full.sln`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore`
- Verification also confirms the MIT package metadata path:
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-local-mit -o artifacts/pack-mit`
- The generated `CommLib.Domain.0.1.0-local-mit.nupkg` contains the expected package metadata, including `<license type="expression">MIT</license>`, `<readme>README.md</readme>`, and the repository URL.
- The repo is closer to public-ready, but it is not fully publication-ready yet because:
  - `.github/workflows/ci.yml` is restored only on local branch `chore/repo-finish` and still cannot be published from this environment
  - root internal planning/continuity files now remain by policy as explicitly marked development artifacts

## Next Work Unit
1. Publish local branch `chore/repo-finish` or commits `5655677` and `a58daa3` plus the MIT follow-up from a credential that can update workflow files on GitHub.

## Next Slice Design
1. Keep the next slice at repository/GitHub hygiene scope; do not reopen runtime or WinUI feature work.
2. Treat the remaining work as publication hygiene only: publish the already-prepared branch with a higher-scope credential.
3. Do not reopen root-file relocation, runtime cleanup, or WinUI follow-up work in this branch.

## Stop / Reassess Conditions
- Do not attempt more `git push` or GitHub app branch/file writes for `.github/workflows/ci.yml` from this environment until credentials with workflow-file write capability are available.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
