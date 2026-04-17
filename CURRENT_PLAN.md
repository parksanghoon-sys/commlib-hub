# Current Plan

Date: 2026-04-17

## Goal
Raise repository-level public/open-source readiness without changing the core runtime contract in `src/`.

## Confirmed Facts
- The root `README.md` is now a public-facing overview instead of an internal scratch document.
- Repository/package polish is now in place:
  - central package metadata lives in `Directory.Build.props`
  - `README.md` is packed into NuGet packages
  - `.github/workflows/ci.yml` now validates the core libraries, both test projects, and the console example on Windows
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
  - internal planning/continuity files still remain exposed at the repo root
  - `AGENT.md` is still mojibake/corrupted, so it should either be normalized or kept out of the public-facing root policy

## Next Work Unit
1. Choose and add the repository license instead of guessing one automatically.
2. Decide the publication policy for internal root files such as `AGENT.md`, `CURRENT_PLAN.md`, `TODOS.md`, `CHANGELOG_AGENT.md`, `DECISIONS.md`, and `PROGRESS.md`.
3. If those files stay public, normalize `AGENT.md` and any remaining root-facing internal docs so they are at least readable; otherwise move or retire them deliberately.

## Next Slice Design
1. Keep the next slice at repository/release hygiene scope; do not widen it into new runtime or WinUI feature work.
2. Treat license choice as a maintainer/legal decision, not an implementation guess.
3. After the license/root-doc policy is settled, rerun the README/CI/package sanity check and update the root repository notes if the exposure policy changes.

## Stop / Reassess Conditions
- Do not invent a `LICENSE` without an explicit maintainer choice.
- If moving or renaming root internal files would break existing automation or local workflow, stop and document the chosen boundary before editing paths.
- Do not reopen runtime follow-up work from this mixed checkout while repository publication blockers remain unresolved.
