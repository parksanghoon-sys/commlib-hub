# Current Plan

Date: 2026-04-16

## Goal
Deliver the first clean replacement slice for stale PR `#5` on top of PR `#19`, limited to runtime contract hardening around `LengthPrefixed` framing, session failure handling, and terminal receive-failure semantics.

## Confirmed Facts
- Active branch is `feat/runtime-hardening-stack-on-19`, created from `feat/hosting-lifecycle-wiring-main-base` after draft PR `#19` was published as the clean replacement for stale PR `#8`.
- The older PR `#5` is not mergeable against current `main` and still spans too many concerns for a single safe replacement.
- Instead of re-creating all of old `#5` at once, this branch now carries only the first runtime-hardening slice:
  - `ProtocolOptions` is narrowed to the live `LengthPrefixed` contract
  - `ProtocolFactory` passes `MaxFrameLength` into `LengthPrefixedProtocol`
  - `LengthPrefixedProtocol` enforces maximum frame length on encode/decode
  - `DeviceSession` can fail all pending responses on terminal session failure
  - `ConnectionManager` treats background receive failure as terminal, fails pending requests, and hides failed sessions
  - same-device `ConnectAsync()` calls are serialized so transport open does not race
  - sample config and example consumers were updated to the narrower protocol contract
- This branch intentionally does **not** yet pull in the later slices from old PR `#5`:
  - no bounded unsolicited inbound queue yet
  - no `StartWithReportAsync()` / bootstrap report path yet
  - no hosting-level inbound queue capacity or queue-pressure event yet
  - no reconnect doc clarification slice yet
- Validation succeeded on 2026-04-16:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~LengthPrefixedProtocolTests|FullyQualifiedName~ProtocolFactoryTests|FullyQualifiedName~ConnectionManagerTests"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore --filter "FullyQualifiedName~DeviceSessionTests|FullyQualifiedName~DeviceProfileValidatorTests"`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet restore examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj`
  - `dotnet restore examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore -nodeReuse:false -maxcpucount:1`

## Next Work Unit
1. Commit the continuity-file updates for this first split slice.
2. Push this branch and publish it as a stacked draft PR on top of `feat/hosting-lifecycle-wiring-main-base`.
3. Revisit the remaining old `#5` slices one at a time, starting with bounded unsolicited inbound buffering.

## Next Slice Design
1. Keep this branch scoped to the runtime contract hardening already proven by the current tests and example builds.
2. Do not add bounded queue/backpressure, bootstrap report, or reconnect wording changes into this first replacement slice.
3. Treat any gate-lifecycle cleanup or DI-surface work discovered during review as follow-up unless it blocks this slice from staying correct.

## Stop / Reassess Conditions
- If the stacked PR needs to base directly on `main` before `#19` merges, stop and decide whether to restack or wait rather than duplicating the host wiring diff here.
- If new failures appear outside the runtime-contract surface, verify whether another slice from old `#5` leaked into this branch by accident.
