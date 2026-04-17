# Current Plan

## Date
- 2026-04-17

## Current Scope
- Finish the repository-level public-ready cleanup without changing the runtime contract in `src/`
- Keep the work focused on repo-facing docs, package metadata, CI, and publication blockers

## Confirmed State
- Root `README.md` is now a public-facing overview with honest contract notes.
- Package metadata is centralized in `Directory.Build.props`, and `README.md` is packed into NuGet packages.
- A Windows CI workflow now validates the core libraries, both test projects, and the console example.
- XML documentation output is enabled only for packable library projects under `src/`, not for tests/examples.
- The remaining library XML warning gaps were fixed in `DeviceSession`, `LengthPrefixedProtocol`, and `ConnectionManager`.
- Verification passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore -v minimal`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore -v minimal`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore -v minimal`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --no-restore -p:PackageVersion=0.1.0-local5 -o artifacts/pack -v minimal`
- The repo is not fully publication-ready yet because `LICENSE` is still missing and the internal planning files remain exposed at the repo root.

## Next Work Unit
1. Add the chosen repository license.
2. Decide whether internal planning files stay at the repo root, move elsewhere, or remain internal-only.
3. Normalize or retire `AGENT.md` if it will remain in a public root layout.

## Not In This Step
- No new runtime/API hardening
- No new WinUI/feature work
- No guessed legal/license text
