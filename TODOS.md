# TODOS

## Execution Context
- Baseline after this integration: local `main` refreshed from `commlib-hub/main`.
- Temporary landing branch used for the merge: `integration/main-refresh-20260414`.
- Active implementation branch: `feat/hosting-lifecycle-wiring`.
- GitHub issue search on 2026-04-16 found no existing repo issue for the hosting lifecycle wiring slice, so this branch name plus `TODOS.md` remain the active tracking handle.

## Current TODOs
- [ ] Re-evaluate whether `ReconnectOptions` naming is still too broad for connect-time retry only on the next fresh branch after `feat/hosting-lifecycle-wiring`.
  Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, docs/sample config, compatibility notes.
  Objective: keep the public contract truthful without widening this hosting branch into a broader runtime-policy refactor.
  Validation: add focused contract/docs/tests as needed, then re-run `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`.

## Deferred Backlog

### Runtime Hardening & Correctness
### [P2_LATER] Consider whether hosted startup should later offer a report-returning policy in addition to the new fail-fast default
- What remains: decide whether Generic Host integration eventually needs an option that uses `DeviceBootstrapper.StartWithReportAsync()` instead of the current fail-fast `StartAsync()` path.
- Why deferred: the first hosting slice intentionally stayed small and aligned with normal host-start semantics; there is still no concrete deployment need for partial startup.
- Objective: avoid overfitting the host wrapper today while leaving a clear path if operators later need "start what you can and report the rest" behavior.
- Relevant context: `CommLibHostedService` currently maps enabled raw profiles and reuses `DeviceBootstrapper.StartAsync()`, so the first invalid/failed enabled profile fails host startup immediately. `DeviceBootstrapper.StartWithReportAsync()` already exists if later requirements justify a configurable reporting mode.
- Scope: `src/CommLib.Hosting`, `src/CommLib.Application/Bootstrap`, docs, focused tests.
- Current status: fail-fast startup is the only built-in Generic Host behavior.
- Known blockers/open questions: whether any real deployment wants partial startup strongly enough to justify additional runtime options and reporting surface.
- Most natural next step: wait for a concrete hosting/operator requirement before widening the host wrapper contract.

### API / Contract Truthfulness
### [P1_SOON] Re-evaluate whether `ReconnectOptions` naming is still too broad for connect-time retry only
- What remains: decide whether doc-only clarification is enough or whether a staged alias/deprecation path is warranted.
- Why deferred: the new hosting slice is complete, but it is still better to handle contract naming in its own narrow follow-up branch.
- Objective: keep the public contract truthful without creating unnecessary churn before a stable package surface exists.
- Relevant context: runtime recovery remains terminal after receive-pump failure; `ReconnectOptions` still governs connect-time `OpenAsync()` retries only.
- Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, docs/sample config, compatibility shims if needed.
- Current status: naming still suggests broader live-session recovery than the implementation actually provides.
- Known blockers/open questions: whether external consumers already depend on the current JSON/property names.
- Most natural next step: inventory references and decide between docs-only clarification, staged aliasing, or later breaking rename.

### Production Integration & Hosting
### [P2_LATER] Decide the production surface for diagnostics, health, and secure transport
- What remains: decide whether logging, metrics, health checks, and TLS/certificate-aware TCP options belong in core, hosting, or a later integration package.
- Why deferred: the repo is now structurally stronger, but expected deployment constraints still are not concrete enough to justify widening the surface immediately.
- Objective: improve operability without forcing every future concern into the core runtime package.
- Relevant context: `IConnectionEventSink` and inbound backpressure events now exist, and Generic Host lifecycle wiring now exists for configuration-bound registration, but there is still no first-class `ILogger`, metrics, health-check, or TLS surface.
- Scope: `src/CommLib.Hosting`, `src/CommLib.Infrastructure`, transport options, future integration seams.
- Current status: diagnostics are callback-oriented and secure-transport concerns are still out of scope.
- Known blockers/open questions: target deployment environment, certificate ownership, and whether callback-based observability is sufficient.
- Most natural next step: collect real operator/deployment requirements before adding more public surface.

### [P1_SOON] Resume the older UDP / Multicast / real-pointer WinUI validation pass after the raw-hex composer work
- What remains: re-run the manual UDP and Multicast mock endpoint checks, step through TCP / UDP / Multicast / Serial panel switching on both `Device Lab` and `Settings`, and only re-check transition feel / wheel-scroll / live-log manual scrolling if that pass surfaces a concrete regression.
- Why deferred: raw-hex TCP is now wired, covered by automated lower-stack tests, validated through a live WinUI roundtrip, and the active implementation priority has shifted to the first bitfield foundation slice.
- Objective: close the remaining interactive WinUI confidence gaps without mixing them into the current serializer-layer implementation slice.
- Relevant context: transport-panel collapsing, live-log auto-follow, in-app mock peers, and the earlier TCP automation smoke are already in place; the remaining gaps are real-pointer confirmation plus UDP/Multicast behavior in the live app.
- Scope: `examples/CommLib.Examples.WinUI/Views/DeviceLabView.cs`, `examples/CommLib.Examples.WinUI/Views/SettingsView.cs`, `examples/CommLib.Examples.WinUI/ViewModels/MainViewModel.cs`, `examples/CommLib.Examples.WinUI/Services/LocalMockEndpointService.cs`, and any resulting docs/status text.
- Current status: TCP mock flow already has earlier automation smoke coverage, the raw-hex TCP transport/session path now has focused infrastructure roundtrip coverage, and a live WinUI raw-hex TCP pass is complete. UDP / Multicast and real-pointer confirmation are still manual-only.
- Known blockers/open questions: whether multicast still needs clearer UX copy once the broader live pass happens, and whether any real-pointer issue appears that did not show up in automation.
- Most natural next step: run one live `Device Lab` session that focuses on UDP, Multicast, panel switching, and real-pointer behavior rather than repeating the now-covered raw-hex TCP path.

### [P1_SOON] Clarify single-machine multicast mock UX if duplicate inbound lines feel confusing
- What remains: decide whether the new in-app multicast mock flow needs stronger status/log copy, a dedicated note in the UI, or a small behavior tweak for one-machine validation sessions.
- Why deferred: the implementation is in place and TCP has been smoke-validated, but the remaining manual multicast pass has not yet confirmed whether seeing both self loopback traffic and peer echo is intuitive enough.
- Objective: make the multicast mock path understandable during local operator testing without overcomplicating the transport layer.
- Relevant context: `LocalMockEndpointService` now joins the selected multicast group and replies back to the sender port so a single machine can act as both sender and mock peer; depending on socket loopback behavior, the live log may still show more than one inbound event per send.
- Scope: `examples/CommLib.Examples.WinUI/Services/LocalMockEndpointService.cs`, `examples/CommLib.Examples.WinUI/ViewModels/MainViewModel.cs`, `examples/CommLib.Examples.WinUI/Services/AppLocalizer.cs`, and `examples/CommLib.Examples.WinUI/Views/DeviceLabView.cs`.
- Current status: status text already warns about self traffic plus peer echo, but the UX has not been manually judged yet in the real app.
- Known blockers/open questions: how the local NIC / multicast loopback behavior presents on this machine during a full WinUI send/receive session, and whether the current status text is enough.
- Most natural next step: run the manual multicast mock validation from `Device Lab`, capture the exact live-log behavior, then either keep the current wording or tighten it with the smallest safe UI-only change.

### [P2_LATER] Safely map full `DeviceLabTheme` templated-control styles before broad rollout
- What remains: decide whether to prune the currently unused templated-control style helpers in `DeviceLabTheme` or reintroduce them with verified WinUI default-style keys and a startup-safe initialization path.
- Why deferred: during this session, a broader theme rollout exposed startup failures when the theme dictionary eagerly created styles that depended on missing default resource keys such as `DefaultListViewStyle`.
- Objective: either make the full theme surface safe and real for templated controls, or remove the dormant pieces so the theme stays aligned with the actual UI contract.
- Relevant context: the current successful hookup uses `DeviceLabTheme.Shared` for live brush/text/border resources in `AppShellView`, `DeviceLabView`, and `SettingsView`; broad app-resource merging and eager templated-control style creation proved too risky for the current step.
- Scope: `examples/CommLib.Examples.WinUI/Styles/DeviceLabTheme.cs` plus any future consumer updates in the WinUI views.
- Current status: the app now runs successfully with the safe subset, but keys/methods for broader control styles still exist as future design-space rather than live behavior.
- Known blockers/open questions: which default WinUI style keys are actually available in this app/runtime combination, and whether a code-built WinUI example should keep those style definitions at all.
- Most natural next step: inventory the actual default style keys available at runtime, then either delete the dormant helpers or add back only the verified ones behind a small focused validation pass.

### [P2_LATER] Add a reusable local WinUI transport validation helper
- What remains: package the local TCP/UDP echo peer and multicast verification flow into a repo-owned helper script or documented workflow tailored for the WinUI example.
- Why deferred: the existing console example plus ad-hoc local echo commands are enough for immediate manual verification, but the setup is still more manual than it should be.
- Objective: make WinUI transport validation repeatable without rediscovering local peer commands every session.
- Relevant context: `examples/CommLib.Examples.Console` already provides `tcp-demo`, `udp-demo`, `multicast-send`, and `multicast-receive`; during this session we also used local TCP/UDP echo peers to support WinUI manual checks.
- Scope: likely `examples/CommLib.Examples.Console`, WinUI README/docs, and possibly a small helper script under `scripts/` or `examples/`.
- Current status: no dedicated helper exists yet; validation instructions are partly in chat and partly in example READMEs.
- Known blockers/open questions: whether the best shape is a PowerShell script, extra console subcommands, or documentation-only guidance.
- Most natural next step: after the current interactive validation pass, capture the exact repeatable commands that felt necessary and package only that minimal workflow.

### [P2_LATER] Normalize `PROGRESS.md` encoding for safe future updates
- What remains: identify the current mixed/non-UTF-8 encoding issue in `PROGRESS.md` and convert it to a stable encoding without losing prior history.
- Why deferred: this does not block the product work itself, and a careless rewrite could damage an important project memory file.
- Objective: make future automated updates to `PROGRESS.md` safe and tool-friendly.
- Relevant context: `apply_patch` rejected an in-place `PROGRESS.md` update on 2026-04-03 because the file stream was not valid UTF-8; we were still able to append the `2026-04-03` daily log as new UTF-8 text without rewriting older bytes.
- Scope: `PROGRESS.md` only.
- Current status: append-only daily logging is workable, but some older/later sections still show encoding corruption depending on the reader and the file is not safe for normal in-place patching.
- Known blockers/open questions: whether the file contains mixed UTF-8 and legacy code-page bytes, and what normalization path preserves the current readable Korean content best.
- Most natural next step: back up the file, detect the dominant encoding per corrupted section, and rewrite once with a verified UTF-8 result so future updates can use normal in-place editing again.

### [P1_SOON] Extend the binary-capable serializer path toward future bitfield schema support without rewriting the transport/protocol layers
- What remains: apply the new schema layer to at least one real runtime consumer, then decide from that concrete usage whether later phases truly need richer typed serializer/protocol options plus broader bitfield-aware schema mapping for byte/bit-oriented devices.
- Why deferred: the repo now has raw-hex transport/session validation, a low-level bitfield codec, a schema model, overlap/range validation, and a serializer-adjacent schema setting seam, but it still does not use that schema in a real config-backed or UI-backed compose/inspect path.
- Objective: support devices whose payload contract is defined in bytes and bit ranges while preserving the existing transport, session, and frame-boundary architecture.
- Relevant context: `IProtocol` still owns frame boundaries only, which remains the intended boundary. The repo now has `IBinaryMessagePayload`, binary message models, `MessagePayloadFormatter`, `RawHexSerializer`, `OutboundMessageComposer`, persisted serializer selection in the WinUI example, `RawHexConnectionManagerRoundtripTests` covering both direct binary payloads and the hex-text bridge over the real TCP/session stack, `BitFieldDefinition`/`BitFieldCodec`, plus the new `BitFieldPayloadSchema` / `BitFieldPayloadSchemaValidator` / `BitFieldPayloadSchemaCodec` layer and optional `SerializerOptions.BitFieldSchema`.
- Scope: `src/CommLib.Domain/Configuration/SerializerOptions.cs`, `src/CommLib.Domain/Configuration/ProtocolOptions.cs`, `src/CommLib.Domain/Messaging`, `src/CommLib.Application/Configuration/DeviceProfileMapper.cs`, `src/CommLib.Application/Configuration/DeviceProfileValidator.cs`, `src/CommLib.Infrastructure/Factories`, any future serializer/schema support under `src/CommLib.Infrastructure`, WinUI composer/settings files under `examples/CommLib.Examples.WinUI`, and focused tests/docs.
- Current status: the repository now supports binary payload message representation, formatter-based binary display, `RawHex` serializer creation, outbound hex parsing, inbound binary message deserialization, persisted WinUI serializer choice, automated raw-hex TCP roundtrip coverage, a live WinUI raw-hex TCP proof, low-level bitfield read/write helpers, and a validated fixed-length signed/unsigned schema-backed compose/inspect layer. The remaining gap before widening is a real consumer that uses the schema at runtime.
- Known blockers/open questions: whether the first real consumer should be inbound inspection/log enrichment or a config-backed outbound helper, whether the current optional schema property is enough or will later pressure a richer typed serializer-options model, whether inbound raw payloads should stay formatter-based or gain richer UI treatment, and how much schema-editing UX the WinUI example should expose in the first bitfield-aware phase.
- Most natural next step: thread `SerializerOptions.BitFieldSchema` into one no-UI consumer, with config-backed inbound inspection/log enrichment preferred over a full schema editor.

### [P2_LATER] Consider alternate bit numbering and richer scalar types after the first schema-backed bitfield slice
- What remains: evaluate whether later devices need MSB-first bit numbering, scaled numeric fields, enums, floats, BCD, or packed string helpers beyond the current unsigned/signed integer foundation.
- Why deferred: the current repo needed a single evidence-backed bitfield convention first, and widening into multiple numbering/data-type semantics before the schema layer exists would add more ambiguity than value.
- Objective: keep the first bitfield implementation small and reviewable while leaving a clear slot for broader device-specific field semantics later.
- Relevant context: the current `BitFieldCodec` uses the convention `payload[0]` LSB = bit `0`, supports up to 64-bit scalar reads, and writes unsigned scalar values in place.
- Scope: `src/CommLib.Domain/Messaging/BitFieldDefinition.cs`, `src/CommLib.Domain/Messaging/BitFieldCodec.cs`, any future schema/value-type files under `src/CommLib.Domain` or `src/CommLib.Application`, and focused tests/docs.
- Current status: the low-level bitfield seam exists and is covered by tests, but alternative numbering and richer value semantics are intentionally out of scope for this first slice.
- Known blockers/open questions: whether real target devices describe bits as LSB-first or MSB-first, and which non-integer field types actually matter enough to justify first-class support.
- Most natural next step: wait until the schema-backed compose/inspect layer exists and at least one real device contract pressures a broader numbering or value-type surface.

## Completed
- [x] 2026-04-16: completed `feat/hosting-lifecycle-wiring` by adding configuration-bound `AddCommLibCore(IConfiguration, ...)` overloads, binding `CommLibOptions` from the root config or `CommLib` section, registering `CommLibHostedService` only on the Generic Host path, reusing `DeviceBootstrapper.StartAsync()` for fail-fast startup, reusing `ConnectionManager.DisposeAsync()` for host-stop cleanup, and validating with `dotnet restore tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj`, `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`, and `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`.
- [x] 2026-04-14: fixed `DeviceSession` timeout cleanup on `feat/device-session-timeout-cleanup` by replacing detached timeout waits with cancellable timeout registrations, cancelling timeout waits on both response completion and `FailPendingResponses()`, exposing an internal delay seam to `CommLib.Unit.Tests` via `InternalsVisibleTo`, and validating with `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore --filter "FullyQualifiedName~DeviceSessionTests"`, `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`, and `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`.
- [x] 2026-04-14: integrated `feat/bitfield-endianness`, `feat/bitfield-schema-log-enrichment`, and `feat/runtime-hardening-clean-base` onto `integration/main-refresh-20260414`, then replayed the 2026-04-14 gate-fix and Korean XML documentation commits on top; verified with `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`, `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`, `dotnet restore examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`, and `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`.
- [x] 2026-04-14: fixed the `_deviceOperationGates` leak in `ConnectionManager` by keeping per-device operation gates reference-counted and removing them once the last lease is released for a disconnected device; added `DisconnectAsync_DistinctDevices_ReleasesUnusedDeviceGates` coverage and verified with `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`.
- [x] 2026-04-14: added Korean XML documentation across the repo's C# source/example/test files, repaired the WinUI-localized `AppLocalizer.cs` string table after the first bulk-edit path corrupted Korean literals, and re-verified with focused tests plus a fresh WinUI restore/build on the clean integration worktree.
