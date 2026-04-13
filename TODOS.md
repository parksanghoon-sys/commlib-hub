# TODOS

## Current TODOs
- [ ] Decide whether `ReconnectOptions` needs a clearer connect-time retry contract.
  Scope: determine whether the current public naming should stay as doc-only clarification, gain a staged alias/deprecation path, or be left for a later breaking rename.
  Validation: choose the compatibility boundary first, then add only the minimal docs/code/tests needed for the selected path.

## Deferred Backlog

### [P2_LATER] Consider richer queue-pressure diagnostics only if real deployments need more than the new callback
- What remains: decide whether queue pressure eventually needs metrics, counters, health reporting, or a stronger contract than the current event-sink callback.
- Why deferred: the current runtime now emits a best-effort `IConnectionEventSink.OnInboundBackpressure(deviceId, queueCapacity)` signal when the receive pump actually blocks on a full bounded queue, and there is still no concrete operator requirement for more than that.
- Objective: keep the first queue-pressure observability surface narrow and non-noisy while leaving a clear path if production deployments need stronger telemetry.
- Relevant context: `ConnectionManager` uses a bounded unsolicited inbound queue with `BoundedChannelFullMode.Wait`, hosting can override `InboundQueueCapacity` through `CommLibRuntimeOptions`, and the new callback intentionally fires once per pressure episode rather than streaming queue-depth metrics.
- Scope: `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs`, `src/CommLib.Infrastructure/Sessions/IConnectionEventSink.cs`, `src/CommLib.Hosting`, and any future diagnostics/health integration docs.
- Current status: queue pressure is now observable through `IConnectionEventSink`, but there is still no built-in metrics/counters/health surface.
- Known blockers/open questions: whether operators need queue depth trends versus simple pressure episodes, whether any future signal should be best-effort or guaranteed, and whether such telemetry belongs in core, hosting, or a future integration package.
- Most natural next step: wait for a concrete deployment/ops requirement, then add only the smallest stronger telemetry contract that requirement justifies.

### [P1_SOON] Decide whether `ReconnectOptions` needs a clearer connect-time retry contract
- What remains: determine whether keeping the public `ReconnectOptions` / `DeviceProfile.Reconnect` naming is still acceptable now that runtime behavior is explicitly connect-time retry only, or whether the repo should add a non-breaking alias/deprecation path or plan a future rename.
- Why deferred: the runtime policy is now explicit and tested, but the public naming still suggests broader live-session auto-reconnect semantics than the implementation actually provides.
- Objective: keep configuration and API naming truthful without breaking existing consumers unnecessarily.
- Relevant context: `ConnectionManager` now treats post-connect receive failure as terminal, hides failed sessions, and fails pending requests immediately on receive failure, explicit disconnect, and same-device session replacement; `ReconnectOptions` currently only affects connect-time `OpenAsync()` retry behavior inside `CreateOpenedTransportAsync()`.
- Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, `src/CommLib.Domain/Configuration/DeviceProfile.cs`, `src/CommLib.Domain/Configuration/DeviceProfileValidator.cs`, config/docs samples, and any compatibility shim that becomes necessary.
- Current status: the runtime contract is explicit, but the public type/property names remain `ReconnectOptions` and `Reconnect`.
- Known blockers/open questions: whether external consumers already depend on the current JSON/property names, whether a doc-only clarification is sufficient for now, and whether a rename is worth the churn before a stable external package surface exists.
- Most natural next step: inventory references and choose between doc-only clarification, a staged alias, or a later breaking rename.

### [P2_LATER] Consider core-library auto-reconnect only if a real deployment needs runtime reconnect orchestration
- What remains: design a real runtime recovery/state-machine path only if a concrete deployment needs the core library itself to reopen transports and restore live sessions after a post-connect failure.
- Why deferred: the current hardening pass intentionally chose terminal failed sessions plus immediate pending-request failure rather than widening into implicit reconnect orchestration without a proven requirement.
- Objective: avoid half-designed recovery behavior in the core library while leaving a clear expansion path if a real deployment needs it.
- Relevant context: `ConnectionManager` now keeps failed sessions terminal, `GetSession()` hides them until an explicit disconnect/reconnect, and `ReconnectOptions` remains connect-time retry only.
- Scope: `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs`, connection/session state modeling, tests, hosting/orchestration boundaries, and possibly user-visible status surfaces.
- Current status: the core runtime now has explicit terminal-session behavior rather than implicit or partial auto-reconnect.
- Known blockers/open questions: who should own reconnect orchestration, whether requests survive or fail across reconnect, and whether a higher application/hosting layer is the better owner of recovery policy.
- Most natural next step: wait for a concrete deployment requirement, then design reconnect semantics as a dedicated slice.

### [P2_LATER] Add a new framing family only when a concrete device contract requires non-length-prefixed behavior
- What remains: design and implement a dedicated protocol family for CRC, STX/ETX delimiters, or other non-length-prefixed framing only if a real device contract requires it.
- Why deferred: the current runtime now enforces a truthful `LengthPrefixed` contract, and this slice intentionally removed inactive knobs instead of pretending those semantics already exist.
- Objective: preserve honest runtime/configuration contracts while keeping a clear expansion path for future framing requirements.
- Relevant context: `ProtocolOptions` now carries only `Type` and `MaxFrameLength`, `LengthPrefixedProtocol` now enforces that frame limit directly, and the root sample config no longer advertises `UseCrc`, `Stx`, or `Etx`.
- Scope: `src/CommLib.Domain/Configuration/ProtocolOptions.cs`, `src/CommLib.Infrastructure/Factories/ProtocolFactory.cs`, `src/CommLib.Infrastructure/Protocol`, focused tests, and any config/docs samples that would expose the new framing family.
- Current status: no delimited or CRC-backed framing implementation exists in the runtime library.
- Known blockers/open questions: the actual target frame contract, checksum semantics, delimiter escaping rules, and whether framing should stay payload-agnostic under the current serializer boundary.
- Most natural next step: wait for a concrete device/protocol contract, then design the new framing family as its own dedicated slice.

### [P2_LATER] Decide the production integration surface for diagnostics, health, and secure network transport
- What remains: decide whether the core library, the hosting package, or a future integration package should own structured logging, metrics, health checks, and TLS/certificate-aware transport options.
- Why deferred: the current review established that these hooks are not first-class yet, but it did not establish the target deployment model strongly enough to justify widening the library immediately.
- Objective: give production adopters a clear ops/security path without forcing every concern into the core transport/session package prematurely.
- Relevant context: `AddCommLibCore()` currently registers only factories, `IConnectionManager`, and `DeviceBootstrapper`; `TcpTransport` currently uses raw `TcpClient`/`NetworkStream`; `IConnectionEventSink` is a useful seam, but nothing wires it to first-class logging/metrics/health integration by default.
- Scope: `src/CommLib.Hosting/ServiceCollectionExtensions.cs`, `src/CommLib.Infrastructure/Transport/TcpTransport.cs`, transport option models under `src/CommLib.Domain/Configuration`, and any future hosting/integration package boundaries.
- Current status: the repo has useful validation and test coverage, but no built-in `ILogger`, metrics, health-check, or TLS/certificate surface in `src/`.
- Known blockers/open questions: whether the intended deployments are controlled/air-gapped enough that TLS stays out of scope, and whether diagnostics should remain callback-based through `IConnectionEventSink` or grow into a stronger hosting integration story.
- Most natural next step: pin down the expected deployment environment first, then add only the smallest production-facing diagnostics/security surface that materially supports it.

### [P2_LATER] Clarify single-machine multicast mock UX if duplicate inbound lines feel confusing
- What remains: decide whether the in-app multicast mock flow needs stronger status/log copy, a dedicated note in the UI, or a small behavior tweak for one-machine validation sessions.
- Why deferred: the implementation is in place and TCP has been smoke-validated, but the remaining manual multicast pass has not yet confirmed whether seeing both self loopback traffic and peer echo is intuitive enough.
- Objective: make the multicast mock path understandable during local operator testing without overcomplicating the transport layer.
- Relevant context: `LocalMockEndpointService` now joins the selected multicast group and replies back to the sender port so a single machine can act as both sender and mock peer.
- Scope: `examples/CommLib.Examples.WinUI/Services/LocalMockEndpointService.cs`, `examples/CommLib.Examples.WinUI/ViewModels/MainViewModel.cs`, `examples/CommLib.Examples.WinUI/Services/AppLocalizer.cs`, and `examples/CommLib.Examples.WinUI/Views/DeviceLabView.cs`.
- Current status: status text already warns about self traffic plus peer echo, but the UX has not been manually judged yet in the real app.
- Known blockers/open questions: how the local NIC / multicast loopback behavior presents on this machine during a full WinUI send/receive session, and whether the current status text is enough.
- Most natural next step: run the manual multicast mock validation from `Device Lab`, capture the exact live-log behavior, then either keep the current wording or tighten it with the smallest safe UI-only change.

### [P2_LATER] Normalize `PROGRESS.md` encoding for safe future updates
- What remains: identify the current mixed/non-UTF-8 encoding issue in `PROGRESS.md` and convert it to a stable encoding without losing prior history.
- Why deferred: this does not block the product work itself, and a careless rewrite could damage an important project memory file.
- Objective: make future automated updates to `PROGRESS.md` safe and tool-friendly.
- Relevant context: `apply_patch` previously rejected an in-place `PROGRESS.md` update because the file stream was not valid UTF-8; append-only daily logging worked, but normal in-place patching is still risky.
- Scope: `PROGRESS.md` only.
- Current status: append-only updates are workable, but the file is not yet safe for ordinary in-place editing.
- Known blockers/open questions: whether the file contains mixed UTF-8 and legacy code-page bytes, and what normalization path preserves the current readable Korean content best.
- Most natural next step: back up the file, detect the dominant encoding per corrupted section, and rewrite once with a verified UTF-8 result.

## Completed
- [x] 2026-04-13: exposed the first queue-pressure signal without widening hosting/profile contracts by adding a default no-op `IConnectionEventSink.OnInboundBackpressure(deviceId, queueCapacity)` callback and teaching `ConnectionManager` to emit it once per pressure episode when the bounded unsolicited inbound queue actually blocks the receive pump; verified with `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter "ConnectionManagerTests" --no-restore` and `dotnet build commlib-codex-full.sln --no-restore`.
- [x] 2026-04-13: reviewed whether hosting/DI should surface `DeviceBootstrapper.StartWithReportAsync()` as a new bootstrap contract, decided to keep it as an application-level opt-in because `AddCommLibCore()` already registers `DeviceBootstrapper` for explicit DI resolution, and re-validated the relevant seams with focused `ConnectionManagerTests`, `ServiceCollectionExtensionsTests`, and `DeviceBootstrapperTests`.
- [x] 2026-04-10: exposed inbound queue capacity through `CommLib.Hosting.CommLibRuntimeOptions`, kept `AddCommLibCore()` backward compatible while adding an override overload, added a thin public `ConnectionManager` constructor for hosting-only queue sizing, and verified the default/override wiring with focused unit tests.
- [x] 2026-04-10: reviewed the latest runtime-hardening paths and rewrote Korean XML/inline comments in `DeviceBootstrapper`, `DeviceBootstrapReport`, `DeviceBootstrapFailure`, `DeviceProfileValidator`, `ConnectionManager`, and the changed bootstrap/connection tests; verified with `dotnet build commlib-codex-full.sln --no-restore`.
- [x] 2026-04-10: moved `DeviceProfileValidator` into `CommLib.Domain.Configuration`, enforced profile validation at the `ConnectionManager.ConnectAsync()` boundary before runtime factory/open work, kept `DeviceBootstrapper.StartAsync()` as the fail-fast compatibility path, and added `StartWithReportAsync()` plus `DeviceBootstrapReport` / `DeviceBootstrapFailure` for continue-and-report startup; verified with `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`, `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`, and `dotnet build commlib-codex-full.sln --no-restore`.
- [x] 2026-04-10: replaced `ConnectionManager`'s unbounded unsolicited inbound queue with a bounded queue using backpressure-first full behavior, kept the first capacity choice internal (`256`), and added infrastructure coverage proving transport backpressure plus blocked-writer disconnect/reconnect cleanup; verified with `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`, `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`, and `dotnet build commlib-codex-full.sln --no-restore`.
- [x] 2026-04-09: aligned `ProtocolOptions` with the live `LengthPrefixedProtocol` contract by removing inactive `UseCrc` / `Stx` / `Etx` settings, enforcing `MaxFrameLength` during encode/decode, passing that limit through `ProtocolFactory`, rejecting unsupported protocol types up front in `DeviceProfileValidator`, and cleaning the repo samples so they no longer advertise unsupported framing behavior; verified with focused infrastructure/unit tests plus console/WinUI example builds.
- [x] 2026-04-09: made runtime recovery semantics explicit in `ConnectionManager` by treating background receive failure as terminal, hiding failed sessions from `GetSession()`, rethrowing stored receive failures on later send/manual-inbound calls, and failing pending response tasks immediately on receive failure, explicit disconnect, and same-device session replacement; verified with focused infrastructure and unit tests.
- [x] 2026-04-08: completed the first `ConnectionManager` hardening slice by consolidating per-device state, serializing same-device lifecycle operations, removing the accidental second `OpenAsync()` call during connect, and surfacing background receive failures as `DeviceConnectionException(..., \"receive\", ...)`; verified with infrastructure tests and focused builds.
- [x] 2026-04-03: published the WinUI/localization follow-up through PR `#3`, centralized shared package/build configuration, and added coverage collection support to the test projects.
