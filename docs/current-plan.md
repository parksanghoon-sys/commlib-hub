# Current Plan

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## Date
- 2026-05-20

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
- A user-requested generic binary protocol slice added configurable `BinaryFrame` framing alongside `LengthPrefixed`, with start bytes, 1/2/4-byte payload length prefix support, optional CRC16/Modbus checksum support, validator/factory coverage, a preserved pre-registered `IProtocolFactory` escape hatch, README/quick-start documentation, and verified unit/infrastructure/build baselines.
- The user then asked for a broader `Span` / minimal-copy data-management plan across the protocol pipeline. The implementation plan is saved at `docs/superpowers/plans/2026-05-19-span-minimal-copy-pipeline.md`; it has not been implemented yet.
- The latest protocol slice added simple byte-local generic bitfield extraction on top of the existing payload bitfield layer, without moving bit semantics into `BinaryFrame`.

## Next Work Unit
1. Continue the span/minimal-copy protocol pipeline plan only after the user confirms that broader slice; the byte-local generic bitfield helper slice is the latest completed protocol work.

## Not In This Step
- No cleanup of unrelated dirty files.
- No TLS/certificate transport redesign yet.
- No broader metrics/health-check surface redesign yet.
- No post-connect auto-reconnect state-machine redesign yet.
- No broader delimiter/fixed-length/escaping/message-header protocol DSL until a concrete device contract requires it.
- No breaking replacement of the current `IProtocol` or `ISerializer` contracts.
