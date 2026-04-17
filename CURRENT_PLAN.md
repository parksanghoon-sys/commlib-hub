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
- The GitHub Actions workflow file is not currently present on `main` because the available git/PAT credential could not update `.github/workflows/ci.yml`, and the GitHub app integration also returned `403 Resource not accessible by integration` for contents writes.
- Stale issues `#21` and `#23` are still open on GitHub even though their work is already on `main`, because both the GitHub app and the available PAT lacked issue-write permission in this environment.
- The root `README.md` is now a public-facing overview instead of an internal scratch document.
- Repository/package polish is now in place:
  - central package metadata lives in `Directory.Build.props`
  - `README.md` is packed into NuGet packages
  - `.github/workflows/ci.yml` has already been authored and validated locally, but it still needs to be restored onto `main`
  - root/test project descriptions were normalized to clean English text
- XML documentation generation is now scoped to the packable library projects under `src/` rather than the entire repo, which removed test/example warning noise while keeping library documentation output enabled.
- The remaining library-side XML warnings were fixed in:
  - `src/CommLib.Application/Sessions/DeviceSession.cs`
  - `src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs`
  - `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs`
- Verification completed successfully after the repo-polish updates:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore -v minimal`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore -v minimal`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore -v minimal`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --no-restore -p:PackageVersion=0.1.0-local5 -o artifacts/pack -v minimal`
- The generated `CommLib.Domain.0.1.0-local5.nupkg` still contains the expected package metadata, including `<readme>README.md</readme>` and the repository URL.
- The repo is closer to public-ready, but it is not fully publication-ready yet because:
  - there is still no root `LICENSE`
  - `.github/workflows/ci.yml` is still missing from `main`
  - stale issues `#21` and `#23` are still open on GitHub
  - root internal planning/continuity files now remain by policy as explicitly marked development artifacts

## Next Work Unit
1. Restore `.github/workflows/ci.yml` onto `commlib-hub/main` using credentials that have workflow/file-write permission.
2. Close stale GitHub issues `#21` and `#23` using credentials that have issue-write permission.
3. Choose the repository `LICENSE` with an explicit maintainer decision and add the matching root file.

## Next Slice Design
1. Keep the next slice at repository/GitHub hygiene scope; do not reopen runtime or WinUI feature work.
2. Treat the missing workflow and still-open issues as credential/permission blockers, not as product-code blockers.
3. After a higher-scope credential is available, restore the validated workflow file first, close the stale issues second, then return to the remaining license blocker.

## Stop / Reassess Conditions
- Do not invent a `LICENSE` without an explicit maintainer choice.
- Do not attempt more git or API work on `.github/workflows/ci.yml` or issue state until credentials with the necessary scopes are available.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
