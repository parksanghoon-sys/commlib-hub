# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-05-18

## Current Scope
- Continue commercial-readiness hardening from the current repository state.
- Keep the active work unit narrow, verified, and release-trackable.
- Avoid touching pre-existing dirty/untracked files unless they directly overlap with the selected slice.

## Confirmed State
- The current checkout is `main`, with local commits not yet pushed to `commlib-hub/main`.
- Pre-existing dirty/untracked work remains in:
  - `src/CommLib.Application/Sessions/DeviceSession.cs`
  - `.claude/`
  - `docs/superpowers/`
  - `todo.md`
- The first production-hardening slice added TCP runtime-option validation:
  - reject non-positive `TcpClientTransportOptions.ConnectTimeoutMs`
  - reject non-positive `TcpClientTransportOptions.BufferSize`
  - cover both failures in `DeviceProfileValidatorTests`
- The second production-hardening slice added release-pipeline guardrails:
  - CI restore promotes NuGet audit warnings `NU1901` through `NU1904` to errors
  - CI runs `dotnet list ... package --vulnerable --include-transitive`
  - CI packs the four library projects under `src/` with `PackageVersion=0.1.0-ci`
  - CI avoids solution-level pack because that also creates example packages
- A user-requested cleanup slice pruned dormant WinUI `DeviceLabTheme` templated-control helpers and removed the unused owner argument from `DeviceLabTheme.Get<T>()`.
- Verification passed with focused validator tests, full unit tests, full infrastructure tests, console Release build, full solution Release build, four library pack commands, a NuGet vulnerability audit, and a WinUI Release build.

## Next Work Unit
1. Add the first production diagnostics slice, likely an `ILogger`-backed `IConnectionEventSink` adapter or equivalent hosting-level guidance.

## Not In This Step
- No cleanup of unrelated dirty files.
- No TLS/certificate transport redesign yet.
- No broader metrics/health-check surface redesign yet.
- No post-connect auto-reconnect state-machine redesign yet.
