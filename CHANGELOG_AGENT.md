# CHANGELOG_AGENT

## 2026-04-03

- Published the WinUI/localization follow-up through PR `#3` and later confirmed it merged into `main`.
- Centralized shared MSBuild defaults in `Directory.Build.props`, package versions in `Directory.Packages.props`, and added `coverlet.collector` only to the two test projects.
- Completed the WinUI example follow-up around localization, mock endpoints, transport-panel collapsing, and the stronger live-log auto-follow behavior.

## 2026-04-09

- Normalized the runtime-hardening delivery onto a clean branch rooted in `commlib-hub/main` so the runtime review unit no longer rides on the raw-hex/bitfield branch lineage.
- Landed the protocol-contract hardening slice:
  - removed inactive `UseCrc`, `Stx`, and `Etx` options from `ProtocolOptions`
  - enforced `MaxFrameLength` in `LengthPrefixedProtocol`
  - passed the configured frame limit through `ProtocolFactory`
  - rejected unsupported protocol types and too-small frame limits up front in `DeviceProfileValidator`
  - cleaned sample/config/example surfaces so they no longer imply inactive framing behavior
- Landed the runtime recovery slice:
  - kept one per-device state object in `ConnectionManager`
  - serialized same-device lifecycle work through per-device gates
  - removed the accidental second `OpenAsync()` during connect
  - treated background receive failure as terminal for that session
  - hid failed sessions from `GetSession()`
  - failed pending response tasks on receive failure, explicit disconnect, and same-device session replacement
- Verified the runtime-hardening slice with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
- Re-ran the production-readiness review and kept the next blockers explicit:
  - unbounded unsolicited inbound buffering
  - missing connect/bootstrap validation policy
  - fail-fast bootstrap semantics
  - thin hosting / observability / secure-transport surface

## 2026-04-10

- Landed the first queue/hosting contract slice:
  - added `CommLib.Hosting.CommLibRuntimeOptions` with `InboundQueueCapacity`
  - kept `AddCommLibCore()` backward compatible and added `AddCommLibCore(Action<CommLibRuntimeOptions>)` for hosting-level queue sizing
  - added a thin public `ConnectionManager` constructor overload so the hosting layer can pass inbound queue capacity without widening `DeviceProfile`
- Added focused unit coverage proving:
  - the default hosting registration keeps inbound queue capacity at `256`
  - a hosting override propagates the configured capacity into the resolved connection manager
- Verified the slice with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
- Landed the bounded unsolicited-inbound slice in `ConnectionManager`:
  - replaced the unbounded per-device inbound queue with a bounded queue
  - chose backpressure-first full behavior (`BoundedChannelFullMode.Wait`) for the first slice
  - kept queue capacity internal (`256`) instead of widening the public contract immediately
- Added focused infrastructure coverage proving:
  - transport reads stop advancing once unsolicited inbound backlog hits queue capacity
  - disconnect still cleans up a receive pump that is blocked waiting for queue capacity
  - reconnect still succeeds after that blocked-writer cleanup path
- Verified the slice with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- Landed the connect/bootstrap validation slice:
  - moved `DeviceProfileValidator` into `CommLib.Domain.Configuration` so runtime callers can validate without adding an infrastructure-to-application dependency
  - enforced profile validation at the start of `ConnectionManager.ConnectAsync()` before transport/protocol/serializer factory work runs
  - kept `DeviceBootstrapper.StartAsync()` fail-fast for compatibility while validating each enabled profile before connect-time side effects
  - added `DeviceBootstrapper.StartWithReportAsync()` plus `DeviceBootstrapReport` / `DeviceBootstrapFailure` for continue-and-report startup behavior
  - updated the console and WinUI examples to consume the validator from its new domain location
- Added focused coverage proving:
  - invalid profiles fail before `ConnectionManager` runtime factories are invoked
  - `DeviceBootstrapper.StartAsync()` rejects invalid profiles before calling the connection manager
  - `StartWithReportAsync()` continues across mixed validation/connect failures and returns explicit success/failure results
- Reviewed the latest runtime-hardening paths and rewrote Korean XML/inline comments around:
  - connect-boundary validation policy
  - fail-fast vs. report-based bootstrap semantics
  - bounded inbound queue backpressure behavior
  - receive-failure propagation and reconnect cleanup intent
  - bootstrap/report unit tests that changed in this slice
- Verified the latest slice with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- One parallel test invocation hit a transient `CommLib.Domain.dll` file-lock issue; a sequential rerun of the same validation commands passed cleanly.

## 2026-04-13

- Reviewed the next hosting/bootstrap question instead of widening the hosting surface by reflex:
  - confirmed `AddCommLibCore()` already registers `DeviceBootstrapper`, so DI callers can explicitly resolve it and choose `StartAsync()` or `StartWithReportAsync()` today
  - decided not to add a hosted-service wrapper or alternate hosting bootstrap abstraction yet because that would also force lifecycle/reporting semantics the repo has not proven
  - promoted queue-pressure signaling to the next current TODO instead of inventing more bootstrap surface area
- Landed the first queue-pressure observability slice without widening profile/hosting configuration:
  - added default no-op `IConnectionEventSink.OnInboundBackpressure(deviceId, queueCapacity)` so existing sink implementations stay source-compatible
  - taught `ConnectionManager` to emit that callback only when the bounded unsolicited inbound queue actually blocks the receive pump
  - kept the signal intentionally best-effort and once-per-pressure-episode instead of inventing queue-depth metrics or health semantics prematurely
- Closed the reconnect-contract truthfulness question with the smallest compatibility-safe move:
  - kept the public names `ReconnectOptions` and `DeviceProfile.Reconnect`
  - clarified in XML docs that the contract is connect-time transport-open retry only
  - updated the console sample README so it no longer implies live-session auto-reconnect semantics
  - deferred any alias or breaking rename until a future stable API/package process actually needs it
- Re-validated the relevant seams with focused tests:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "ConnectionManagerTests" --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --filter "ServiceCollectionExtensionsTests" --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --filter "DeviceBootstrapperTests" --no-restore`
- Verified the queue-pressure signal slice with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "ConnectionManagerTests" --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
