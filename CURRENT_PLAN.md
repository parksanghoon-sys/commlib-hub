# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

Date: 2026-05-18

## Goal
Continue commercial-readiness hardening from the current repository state while keeping each slice small, verified, and release-trackable.

## Confirmed Facts
- The current checkout is `main`, with local commits not yet pushed to `commlib-hub/main` and pre-existing dirty/untracked work still present:
  - `src/CommLib.Application/Sessions/DeviceSession.cs`
  - `.claude/`
  - `docs/superpowers/`
  - `todo.md`
- The 2026-05-18 commercial-readiness review found that CommLib is suitable for controlled/internal pilot usage, but not yet a full external production-grade commercial release without more release governance, security, observability, and recovery-policy hardening.
- The first commercial-readiness implementation slice is complete:
  - `DeviceProfileValidator` now rejects non-positive TCP `ConnectTimeoutMs` and `BufferSize` values before runtime transport creation.
  - `DeviceProfileValidatorTests` covers both new TCP validation failures.
- The second commercial-readiness implementation slice is complete:
  - `.github/workflows/ci.yml` now restores with NuGet audit warnings promoted to errors for `NU1901` through `NU1904`.
  - `.github/workflows/ci.yml` now runs a NuGet vulnerability audit listing step for visibility.
  - `.github/workflows/ci.yml` now packs the four library projects under `src/` with a CI-only package version.
  - CI intentionally packs `CommLib.Domain`, `CommLib.Application`, `CommLib.Infrastructure`, and `CommLib.Hosting` directly instead of solution-level packing, because solution-level pack also creates example packages.
- The user-requested cleanup slice is complete:
  - `DeviceLabTheme` now keeps only the brush/text/border resources actively consumed by the code-built WinUI views.
  - Dormant templated-control style keys/helpers were pruned instead of preserving an unverified future style surface.
  - `DeviceLabTheme.Get<T>()` no longer accepts an unused `FrameworkElement owner` argument.
- Verification for this slice passed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter DeviceProfileValidatorTests`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore`
  - `dotnet build commlib-codex-full.sln --configuration Release --no-restore`
  - `dotnet restore commlib-codex-full.sln -p:NuGetAudit=true -p:NuGetAuditLevel=low -warnaserror:NU1901,NU1902,NU1903,NU1904`
  - `dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet pack src/CommLib.Application/CommLib.Application.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet pack src/CommLib.Infrastructure/CommLib.Infrastructure.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet pack src/CommLib.Hosting/CommLib.Hosting.csproj --configuration Release --no-restore -p:PackageVersion=0.1.0-ci -o artifacts/pack-ci`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --configuration Release --no-restore`
- NuGet vulnerability audit was checked with `dotnet list commlib-codex-full.sln package --vulnerable --include-transitive` and reported no vulnerable packages from the configured sources.

## Next Work Unit
1. Add the first production diagnostics slice, likely an `ILogger`-backed `IConnectionEventSink` adapter or equivalent hosting-level guidance, so connect/retry/failure/backpressure events do not depend only on custom app code.

## Next Slice Design
1. Keep the next slice observability-focused and opt-in; do not redesign transport or reconnect behavior at the same time.
2. Reuse the existing `IConnectionEventSink` seam before introducing a new diagnostics abstraction.
3. Do not touch the pre-existing `DeviceSession.cs`, `.claude/`, `docs/superpowers/`, or `todo.md` changes unless the user explicitly asks to reconcile them.
4. Keep TLS, health checks, metrics, and auto-reconnect as separate production-integration decisions unless the diagnostics slice exposes a direct dependency.

## Stop / Reassess Conditions
- If adding logging requires a package-boundary or public-API decision beyond a small adapter, stop and record the decision point instead of guessing.
- If unrelated dirty files overlap with the release-governance edits, stop and ask how the user wants those files handled.
