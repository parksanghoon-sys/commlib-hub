# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-04-17

## Goal
Resume the highest-priority runtime/application follow-up work from a truthful, publication-ready repository baseline.

## Confirmed Facts
- PR `#25` (`[codex] integrate outstanding runtime and repo follow-ups`) is now merged into `commlib-hub/main`.
- PR `#26` (`[codex] publish MIT and root policy cleanup`) has already landed on `main`.
- All remote feature/cleanup branches that were part of the older delivery lines have been deleted from `commlib-hub`; only `main` remains on the remote.
- GitHub issues `#21` and `#23` are now closed because their work is already present on `main`.
- The Windows GitHub Actions workflow `.github/workflows/ci.yml` has now been restored on top of the updated `main` baseline through the dedicated minimal branch `chore/restore-ci-workflow`.
- The root `README.md` is now a public-facing overview instead of an internal scratch document.
- The maintainer chose MIT, and `main` now carries the root `LICENSE` file plus `PackageLicenseExpression=MIT` in `Directory.Build.props`.
- Repository/package polish is now in place:
  - central package metadata lives in `Directory.Build.props`
  - package license metadata now uses `MIT`
  - `README.md` is packed into NuGet packages
  - `.github/workflows/ci.yml` is restored on `main`
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
- The repository-level public/open-source readiness cleanup is complete:
  - `main` now has the MIT license, truthful package metadata, and the Windows CI workflow
  - root internal planning/continuity files remain by policy as explicitly marked development artifacts
- `DeviceSession` pending-response tracking is now internally simplified on branch `cleanup/device-session-pending-entry`:
  - reflection-based completion/exception dispatch is replaced with typed pending entries
  - redundant `PendingRequestStore` and separate timeout-registration dictionary are removed
  - mismatched response types no longer drop pending entries silently
- Verification for this cleanup passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`

## Next Work Unit
1. Reassess `ConnectionManager.TryHandleInboundFrame` and remove or internalize the ambiguous dual-entry inbound path if no real external caller depends on it.

## Next Slice Design
1. Keep the next slice code-local and evidence-ready; do not reopen repository-publication work unless the live GitHub state drifts again.
2. Prefer removing or shrinking dead/ambiguous internal runtime paths before widening into hosting, diagnostics, or WinUI follow-up work.
3. Continue to branch fresh from `commlib-hub/main`, not from preserved mixed or publication branches.

## Stop / Reassess Conditions
- If new runtime follow-up work starts touching hosting, observability, or public API boundaries more widely than `ConnectionManager` inbound handling, stop and reassess whether the slice should be split.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
