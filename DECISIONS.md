# DECISIONS

> Internal development continuity file for active repository maintenance.
> Not part of the public CommLib runtime or package contract.

## 2026-04-13 - Deliver the validated bitfield / WinUI follow-up as stacked feature PRs, not on the mixed runtime branch
- Context: the active `feat/runtime-readiness-hardening` worktree still carried validated but uncommitted serializer/composer, WinUI, and focused-test changes that belonged to the raw-hex / bitfield line rather than to the next runtime-hardening slice. A clean runtime branch and draft PR `#5` already existed, so committing more work here would have recreated the same mixed review problem.
- Decision: preserve the mixed worktree as local scratch only, replay the validated feature files onto clean feature branches, and publish them as draft PR `#7` (`feat/bitfield-endianness` -> `main`) plus stacked draft PR `#6` (`feat/bitfield-schema-log-enrichment` -> `feat/bitfield-endianness`).
- Why: this keeps serializer/composer review scope narrow, protects the clean runtime-hardening review line, and avoids forcing reviewers to untangle feature follow-up from production-hardening work.
- Consequences: future runtime hardening should continue only on `feat/runtime-hardening-clean-base`, while this mixed worktree should be treated as disposable local context until it is deliberately cleaned or recreated.

## 2026-04-13 - Keep report-based bootstrap as an application-level opt-in, not a new hosting contract
- Context: after queue sizing became configurable through `CommLib.Hosting`, the next open question was whether hosting/DI should also add a new wrapper, hosted service, or alternate entry point for `DeviceBootstrapper.StartWithReportAsync()`. `AddCommLibCore()` already registers `DeviceBootstrapper`.
- Decision: keep `StartWithReportAsync()` as an application-level opt-in for now. Do not add a new hosting wrapper or hosted bootstrap abstraction yet; callers that need partial-startup reporting can resolve `DeviceBootstrapper` from DI and call the report-based path explicitly.
- Why: this preserves the thin hosting surface and avoids prematurely choosing lifecycle/reporting semantics such as when bootstrap runs, how partial failure is surfaced, and whether startup should block application readiness.
- Consequences: the next runtime question shifts to queue-pressure signaling rather than bootstrap surface expansion, and any future hosting bootstrap abstraction should be driven by a concrete deployment/orchestration requirement instead of by API symmetry alone.

## 2026-04-10 - Expose inbound queue sizing through hosting only and keep pressure signaling internal for now
- Context: after bounded unsolicited-inbound buffering landed, the next design question was whether queue sizing should stay a private `ConnectionManager` detail, move into `DeviceProfile`, or become part of the hosting/runtime contract. Real deployment evidence still did not justify per-device queue tuning, but a single private constant was too rigid for different hosting environments.
- Decision: add `CommLib.Hosting.CommLibRuntimeOptions` with `InboundQueueCapacity`, keep `AddCommLibCore()` backward compatible while adding a hosting-level configure overload, and pass the configured capacity into `ConnectionManager` through a thin public constructor overload. Do not widen `DeviceProfile`, and do not expose queue-pressure signaling yet.
- Why: this is the smallest structurally correct way to make queue sizing adjustable per deployment environment without pretending queue pressure already has a well-designed public observability contract.
- Consequences: hosting callers can now tune inbound queue capacity without touching per-device configuration, the core runtime remains honest about its backpressure-first behavior, and any future pressure signal must be designed as an explicit follow-up instead of leaking out accidentally.

## 2026-04-08 - Harden `ConnectionManager` with one per-device state object and same-device lifecycle serialization
- Context: the production-readiness review found that `ConnectionManager` was the highest-value first hardening target because it kept active state in multiple unsynchronized dictionaries and also opened transports twice during connect.
- Decision: create branch `feat/runtime-readiness-hardening`, consolidate runtime state into a single per-device connection object inside `ConnectionManager`, serialize same-device `ConnectAsync` / `DisconnectAsync` operations with per-device gates, and surface background receive-pump failures as `DeviceConnectionException(..., "receive", ...)` instead of leaving raw transport exceptions to leak without device context.
- Why: this is the smallest structurally correct slice that materially improves lifecycle correctness without widening into broader reconnect orchestration, hosting integration, or transport/protocol redesign.
- Consequences: the library is safer under same-device concurrent lifecycle access and clearer when runtime receive fails, but automatic reconnect/state-machine policy is still a separate follow-up decision.

## 2026-04-08 - Treat the current library as a strong foundation, not an industrial-ready runtime yet
- Context: the user explicitly asked whether the current repo is already suitable for industrial/real deployment. The codebase now has clean transport/protocol/serializer separation, configuration validation, connect-time retry, raw-hex/bitfield foundations, and passing unit/infrastructure suites, but the latest review also surfaced lifecycle and ops gaps.
- Decision: do not describe the current repo as industrial/runtime-ready yet. Treat it as a solid extensible foundation and sample-integrated toolkit until `ConnectionManager` concurrency semantics, runtime failure recovery, and the intended diagnostics/security integration surface are made explicit.
- Why: that framing matches the current evidence better than either dismissing the architecture or overclaiming production readiness.
- Consequences: if production readiness becomes the next objective, the first hardening slice should target connection/session lifecycle behavior before more UI/schema expansion, and any current production adoption should assume project-specific wrappers for diagnostics, health, and network-security concerns.

## 2026-04-08 - Use config-backed WinUI session-log enrichment as the first real `BitFieldSchema` consumer
- Context: the repo already had `SerializerOptions.BitFieldSchema`, schema validation, and schema-based compose/inspect helpers, but no live runtime path actually consumed that schema yet. The next planned step explicitly preferred config-backed inbound inspection/log enrichment over adding a schema editor or widening the transport/protocol layers.
- Decision: preserve an optional `BitFieldSchema` in the WinUI `messageComposer` appsettings model, thread it into the live `DeviceProfile`, and use `DeviceLabSessionService` log formatting as the first runtime consumer by appending decoded field summaries to inbound/outbound `RawHex` logs.
- Why: this is the smallest structurally correct path that proves the schema seam in a real session without adding new transport behavior, a second compose flow, or an in-app schema-editing UX.
- Consequences: the first live schema experience is now JSON-configured rather than UI-authored, log enrichment must stay non-fatal when schema inspection fails, and the next follow-up should be a focused live validation pass before considering any richer schema editor or additional consumers.

## 2026-04-03 - Prioritize localization foundation before other WinUI follow-ups
- Context: planned follow-up work includes Korean UI mode, content transition animation, mouse-wheel scroll fixes, and `win-x64` crash investigation. The current WinUI code still hard-codes user-facing strings across multiple views and view models.
- Decision: make localization foundation the next implementation unit before animation or runtime-specific investigation.
- Why: it is the safest evidence-backed work unit, directly supports the planned Korean UI mode, and reduces duplicated edits across the same UI surfaces.
- Consequences: shell/page/button/status copy should move behind a resource or localization service; animation and `win-x64` investigation remain deferred.

## 2026-04-03 - Keep both `docs/current-plan.md` and root `CURRENT_PLAN.md` in sync for now
- Context: repo hooks point at `docs/current-plan.md`, while the newer session continuity rules expect a root `CURRENT_PLAN.md`.
- Decision: maintain both documents until the repo standard is unified.
- Why: this avoids breaking the current hook workflow while still creating a canonical root state file for newer continuity rules.
- Consequences: future planning changes should update both documents together, or explicitly retire one path as part of a dedicated cleanup.

## 2026-04-03 - Split localization ownership between views and runtime services/view models
- Context: the WinUI example builds its pages in code, with a mix of static field labels/buttons and dynamic connection/session status text emitted from view models and `DeviceLabSessionService`.
- Decision: keep static labels/button copy localized in the views through a shared `IAppLocalizer`, while localizing dynamic status/log text at the view-model/service layer where those messages originate.
- Why: this is the smallest structurally correct change for the current codebase and avoids forcing a broad binding refactor across every static field label.
- Consequences: future localization work should continue to put static surface copy near the view composition code, and keep runtime status/log copy localized where the message is produced.

## 2026-04-03 - Forward text-input mouse-wheel events to the page ScrollViewer instead of refactoring the WinUI page layout
- Context: `DeviceLabView` and `SettingsView` each use a single page-level `ScrollViewer` around many `TextBox` controls, including multiline inputs. The interaction issue to address was mouse-wheel scrolling getting stuck on text-input surfaces, and the current terminal session could not justify a broader layout rewrite.
- Decision: add a small shared `PointerWheelScrollBridge` and attach it to the page `TextBox` controls so wheel input is forwarded to the parent page `ScrollViewer`.
- Why: this is the smallest evidence-backed change for the current code structure, aligns with WinUI guidance that mouse-wheel chaining is not covered the same way as touch/manipulation chaining, and avoids destabilizing the conservative code-built page layout.
- Consequences: page scrolling should remain available while the pointer is over text inputs on both pages; a follow-up manual pointer-device pass should confirm whether the live log needs more nuanced conditional forwarding later.

## 2026-04-03 - Remove the stale `win-x86` default workaround and restore `win-x64`
- Context: earlier project notes said the local `win-x64` path crashed inside `Microsoft.UI.Xaml.dll`, so the sample had been pinned to `win-x86`. Fresh checks on 2026-04-03 showed direct `win-x64` launch, `dotnet run -r win-x64 --no-build`, and explicit `win-x86` all staying alive for 12 seconds with no Application Error / Windows Error Reporting / .NET Runtime events.
- Decision: restore `examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj` to default `win-x64` and update the README instead of keeping the x86 pin based on stale evidence.
- Why: keeping the workaround would encode an unsupported assumption into the project after the failure stopped reproducing on the current machine and branch state.
- Consequences: `win-x64` becomes the default developer path again; if the native startup fault reappears later, future investigation should capture fresh runtime evidence before reintroducing an architecture workaround.

## 2026-04-03 - Keep page transitions inside `AppShellView` and preserve the existing dual-host layout
- Context: the WinUI shell already uses two child hosts with direct visibility toggling to avoid the earlier blank-screen issue. The user wanted smoother page switching for `Device Lab` and `Settings`, but a larger navigation/container rewrite would add more risk than value.
- Decision: add a conservative queued fade + horizontal slide transition directly inside `AppShellView`, while keeping both page hosts alive and avoiding any broader navigation abstraction change.
- Why: this gives noticeably smoother tab switching with the smallest structurally safe change and keeps the previously stabilized page host model intact.
- Consequences: future tuning should stay in the transition timing/distance layer first; only a proven limitation should justify moving to a different navigation container or page-composition model.

## 2026-04-03 - Keep the live log on the existing `LogText` binding and make the viewer itself scroll/auto-follow
- Context: the WinUI example already exposes runtime logs as a single `LogText` string bound into `DeviceLabView`. The immediate problem was long or repeated real-time log output getting visually clipped instead of behaving like an operator log console.
- Decision: keep the current `LogText` aggregation path, but turn the live-log surface into a dedicated read-only multiline `TextBox` with no-wrap lines, its own horizontal/vertical scrollbars, and end-of-document auto-follow on updates. Do not forward the live-log wheel input back to the page `ScrollViewer`.
- Why: this is the smallest structurally correct fix for the current code shape, solves the clipping problem now, and avoids widening scope into a list-based log viewer refactor with new item templates and virtualization concerns.
- Consequences: the live log now behaves like a scrollable console while preserving the current view-model contract; if future work needs filtering, severity styling, or very large log volumes, that should be handled as a deliberate viewer redesign instead of ad-hoc tweaks.

## 2026-04-03 - Use `DeviceLabTheme` as a shared code-side palette/typography source, not as an app-wide templated-control style rollout
- Context: `examples/CommLib.Examples.WinUI/Styles/DeviceLabTheme.cs` existed but was not consumed anywhere. A first attempt to force a broader theme rollout uncovered two runtime risks: touching `Application.Resources` too early in `App` startup, and eagerly creating templated-control styles that depended on missing default WinUI keys such as `DefaultListViewStyle`.
- Decision: connect `DeviceLabTheme` to the live code-built views through `DeviceLabTheme.Shared` and direct lookups for safe brush/text/border resources in `AppShellView`, `DeviceLabView`, and `SettingsView`. Do not ship a full templated-control style rollout in this step.
- Why: this makes the theme real immediately, keeps the example running on `win-x64`, and avoids expanding the task into a fragile runtime-style investigation while the user’s main objective is to have the existing UI actually honor the shared theme surface.
- Consequences: the WinUI example now has a real shared styling source for backgrounds, card chrome, and typography, while the broader control-style surface is explicitly deferred until the available default style keys and startup-safe initialization path are mapped with evidence.

## 2026-04-03 - Drive transport-panel visibility from the existing selected-transport state
- Context: after the WinUI example grew to support TCP, UDP, Multicast, and Serial in both `DeviceLabView` and `SettingsView`, every transport panel remained visible at once. The user wanted the UI to show only the currently relevant transport path, but the existing settings view model already exposed `SelectedTransport` plus `IsTcpSelected` / `IsUdpSelected` / `IsMulticastSelected` / `IsSerialSelected`.
- Decision: bind each transport panel visibility directly to those existing selection-derived booleans through `BooleanToVisibilityConverter` instead of introducing a second checkbox/radio source of truth.
- Why: this is the smallest structurally correct change, preserves a single source of truth for the active transport, and avoids duplicated UI state that could drift away from the actual session configuration.
- Consequences: both WinUI pages now collapse down to the selected transport preset, and future visibility behavior should continue to hang off the current transport-selection state unless there is a proven need for a different UX model.

## 2026-04-03 - Put local mock peers inside `Device Lab` and normalize loopback-friendly settings when they start
- Context: validating TCP/UDP/Multicast from the WinUI example previously required a second console process or external server, which made the sample harder to exercise directly from the UI. The user explicitly wanted mock peer endpoints to be openable from the UI itself.
- Decision: add a small in-process `ILocalMockEndpointService` / `LocalMockEndpointService`, surface it through `MainViewModel` and a `Mock Endpoint` card on `DeviceLabView`, and normalize loopback-friendly settings when the user starts a mock peer.
- Why: this keeps the test aid close to the operator workflow, avoids external-process orchestration from the UI, and makes local validation much faster while staying inside the current MVVM + DI structure.
- Consequences: TCP/UDP/Multicast can now be exercised directly from the WinUI app on one machine; Serial still remains external-only, and single-machine multicast behavior may still need small UX clarification if the live log shows both self loopback traffic and peer echo.

## 2026-04-03 - Drive live-log auto-follow by the inner `ScrollViewer`, not selection movement alone
- Context: the WinUI live log already moved the caret to the end of the read-only multiline `TextBox`, but the user still wanted a stronger guarantee that the newest line stays visible as logs grow.
- Decision: keep the existing read-only `TextBox` log surface, but cache its inner `ScrollViewer` and explicitly drive that viewer to `ScrollableHeight` after each text update in addition to selecting the end of the text.
- Why: this is the smallest reliable change for the current control choice and avoids widening scope into a different log viewer control or template rewrite.
- Consequences: the log now follows the latest entry more deterministically even without relying on focus/caret behavior, while preserving the current `LogText` binding contract.

## 2026-04-03 - Add Korean comments to high-value WinUI paths, not line-by-line boilerplate
- Context: the user asked for Korean comments in the WinUI project, but the example already has many straightforward property assignments and code-built UI sections where translation-style comments would add noise faster than they add value.
- Decision: add Korean explanatory comments to the non-obvious WinUI paths only, focusing on lifecycle boundaries, runtime tradeoffs, state ownership, threading, visual-tree behavior, and mock-peer/runtime quirks.
- Why: this keeps the project approachable for Korean readers without turning the code into a wall of fragile comments that merely restate obvious syntax.
- Consequences: the main bootstrap/view/service/theme files are now easier to onboard from, while future comment additions should continue to prioritize intent and constraints over line-by-line narration.

## 2026-04-03 - Centralize shared package versions and default MSBuild properties at the repo root
- Context: package versions such as `System.IO.Ports`, `Microsoft.NET.Test.Sdk`, `xunit`, `CommunityToolkit.Mvvm`, and `Microsoft.WindowsAppSDK` were duplicated across multiple project files, and nearly every `.csproj` repeated the same `Nullable` / `ImplicitUsings` settings.
- Decision: add a root `Directory.Packages.props` for package versions and a root `Directory.Build.props` for shared `Nullable` / `ImplicitUsings`, then remove the duplicated inline version/property declarations from each project file.
- Why: this is the smallest structurally correct way to make package/version updates consistent across the repo without changing project-specific frameworks, runtime identifiers, or packaging behavior.
- Consequences: future package upgrades should usually happen in one place, while individual `.csproj` files stay focused on project-specific concerns such as references, targets, runtime settings, and packability.

## 2026-04-03 - Add `coverlet.collector` only to test projects, not every project
- Context: after central package management was introduced, the next request was to install `coverlet.collector` across the repo. `coverlet.collector` is a VSTest data collector used during test execution, not a general runtime dependency for library or example projects.
- Decision: add the package version once in `Directory.Packages.props`, then reference it only from `CommLib.Unit.Tests` and `CommLib.Infrastructure.Tests` with `PrivateAssets=all`.
- Why: this matches the package's actual execution model, keeps non-test projects free of irrelevant dependencies, and still gives the repo-wide coverage behavior the user is aiming for because all executable tests now support `--collect:"XPlat Code Coverage"`.
- Consequences: future coverage collection commands should run at the test-project or solution test layer; if new test projects are added later, they should opt into `coverlet.collector` the same way.

## 2026-04-03 - Use `gh` CLI fallback when the GitHub connector cannot create the PR
- Context: after the branch was pushed, the GitHub connector's pull-request creation call returned `403 Resource not accessible by integration` for `parksanghoon-sys/commlib-hub`.
- Decision: keep the connector as the preferred path, but fall back to authenticated `gh` CLI PR creation when connector permissions block the operation.
- Why: this preserves the intended publish flow without stalling on integration-scope limitations that are outside the repository code itself.
- Consequences: future publish steps can still complete end-to-end from this environment as long as `gh` remains installed and authenticated, even if connector PR-creation permissions stay limited.

## 2026-04-06 - Put raw hex/bitfield payload support in the serializer/composer path, not the transport or framing layer
- Context: a follow-up design request asked whether the project can support payloads expressed as raw hex and messages whose semantics live in byte/bit fields. The current codebase already separates `IProtocol` frame-boundary work from `ISerializer` payload shaping, and the WinUI example still composes outbound data as a plain-text body.
- Decision: if this capability is implemented, keep `IProtocol` focused on frame extraction/encoding and add the new behavior under serializer/payload-codec plus message-composer/UI configuration layers.
- Why: bitfield semantics belong to payload interpretation, not to transport or frame boundaries, and reusing the existing protocol/session pipeline is the smallest structurally correct change.
- Consequences: the first implementation slice should target raw-hex payload roundtrips with focused serializer/factory/UI changes; declarative bitfield schema mapping should be added only after that smaller path is validated.

## 2026-04-06 - Prefer a reusable binary-payload seam over a one-off `RawHex` serializer bolt-on
- Context: a second design review found that adding only `RawHex` to the current `SerializerFactory` would work for one immediate use case, but the surrounding seams are still too closed: `SerializerOptions` and `ProtocolOptions` are flat classes, the factories are hard-coded switches, and most example/logging paths still assume `string Body`.
- Decision: keep the overall work in the serializer/composer layer, but shape it around a reusable binary payload representation plus cleaner typed/polymorphic serializer and protocol options rather than a single special-case serializer.
- Why: this gives the project a path for raw hex, future bitfield schema mapping, and additional device-specific codecs without repeatedly changing the same central classes and WinUI assumptions.
- Consequences: the next implementation plan should likely start with option-model/factory seam cleanup plus a raw-binary message/formatter path, then add the first raw-hex codec on top of that seam.

## 2026-04-06 - Start the raw-hex implementation with binary message contracts and a bridge serializer before WinUI compose-mode UI
- Context: the user asked to continue implementation from the raw hex / bitfield design, but the current codebase still assumed text bodies in message models and display/logging paths. Jumping straight to WinUI mode selection without a binary-capable core contract would have pushed more serializer-specific branching into the app layer.
- Decision: implement the first TDD slice as new binary-capable message contracts (`IBinaryMessagePayload` plus binary message models), a shared `MessagePayloadFormatter`, and a `RawHexSerializer` that can bridge both direct binary payloads and the current hex-text compose input style.
- Why: this is the smallest structurally correct slice that unlocks raw payload roundtrips, keeps transport/protocol untouched, and gives the next WinUI step a real backend contract instead of a UI-only toggle with no binary message foundation.
- Consequences: binary inbound payloads can now render safely in logs and examples, `SerializerFactory` can create `RawHex`, and the next step should focus on explicit WinUI composer/settings wiring plus end-to-end manual validation; schema-driven bitfield mapping remains deferred.

## 2026-04-06 - Keep serializer choice fixed for the active session and treat mid-session selector edits as next-session changes
- Context: the WinUI example now persists and exposes serializer choice, but `ConnectionManager` still creates the active serializer only during `ConnectAsync`. Letting the send path immediately obey a changed selector while connected would desynchronize UI composition from the already-open session serializer.
- Decision: capture the serializer type from the connected `DeviceProfile` and compose outbound messages against that active serializer for the lifetime of the session. If the user changes the serializer selector while still connected, the new choice is stored for the next connect rather than mutating the live session behavior.
- Why: this preserves protocol correctness with the smallest safe change and avoids pretending the session's framing/serialization contract can be hot-swapped after connection without a deliberate reconnect boundary.
- Consequences: current sends stay aligned with the live session serializer, while a future UX refinement may still choose to disable the serializer selector during connection or surface a clearer "applies on next session" hint if operators find the current behavior ambiguous.

## 2026-04-06 - Rotate raw-hex work onto its own feature branch instead of continuing on the merged WinUI branch
- Context: the earlier WinUI localization/device-lab work was already merged through PR `#3`, but raw-hex serializer/composer development had started locally while still on `feat/winui-localization-foundation`.
- Decision: move the ongoing raw-hex work onto `feat/rawhex-compose-flow` and continue future changes there.
- Why: once the work expanded into a new feature area, keeping it on the old merged branch would blur scope, history, and review boundaries.
- Consequences: future raw-hex and later bitfield-adjacent work should continue from the dedicated branch lineage unless a new feature split justifies another branch rotation.

## 2026-04-06 - Keep WinUI async command continuations on the UI context when they update observable state
- Context: during the live raw-hex TCP WinUI validation, connection succeeded and logs updated, but buttons such as `Send` remained disabled. The WinUI `MainViewModel` command handlers and `SettingsViewModel` save/reload handlers were awaiting service/store calls with `ConfigureAwait(false)` and then mutating observable properties/command state.
- Decision: in WinUI command handlers that update observable UI state after awaiting, do not use `ConfigureAwait(false)`; let the continuation resume on the captured UI context.
- Why: the WinUI layer owns button/status/property updates, and resuming those continuations off the UI context creates command-enable/state propagation bugs even when the underlying operation succeeded.
- Consequences: service/infrastructure layers can still use `ConfigureAwait(false)` freely, but UI-facing command handlers should keep their post-await state changes on the UI context unless they explicitly marshal back through the dispatcher.

## 2026-04-06 - Start bitfield support with an LSB-first payload convention and a low-level field codec
- Context: the next requested direction after raw-hex validation was to begin thinking in bit units, but the repo still lacked even a minimal shared definition for named bit ranges or a proven read/write seam above raw payload bytes.
- Decision: start with `BitFieldDefinition` plus `BitFieldCodec`, using the convention that `payload[0]` LSB is bit `0`. Support unsigned reads, signed reads via sign extension, and unsigned in-place writes up to 64 bits in this first slice.
- Why: this is the smallest structurally correct foundation that can support future schema-backed compose/inspect work without dragging transport/protocol changes, UI schema editing, or multiple competing bit-numbering semantics into the first implementation.
- Consequences: future schema-backed layers can reuse one proven low-level bit-addressing rule immediately, while alternate bit numbering and richer value types remain explicit follow-up decisions instead of implicit assumptions.

## 2026-04-07 - Model the first schema-backed bitfield layer as payload metadata above `RawHex`, not as a new transport/protocol concern
- Context: after the low-level `BitFieldDefinition` / `BitFieldCodec` slice landed, the repo still lacked a reusable schema model, schema validation, and a settings seam that could carry those definitions without widening into a WinUI schema editor or a transport rewrite.
- Decision: add `BitFieldPayloadSchema` plus `BitFieldPayloadField`, `BitFieldScalarKind`, `BitFieldPayloadSchemaValidator`, and `BitFieldPayloadSchemaCodec` above the low-level codec, expose that schema through optional `SerializerOptions.BitFieldSchema`, and treat schema usage as valid only with `RawHex` in the current validator. Use `decimal` for compose assignments and inspect results so the first schema API can cover the full signed and unsigned 64-bit scalar range without introducing a heavier numeric abstraction yet.
- Why: this is the smallest structurally correct way to create a reusable schema-backed compose/inspect seam that stays in the serializer/composer layer, keeps configuration binding straightforward, and avoids mixing schema semantics into transport or frame-boundary code.
- Consequences: the project now has a config-friendly payload schema model and a validated compose/inspect helper that future UI or config-backed consumers can reuse directly; the next slice should choose one real consumer of `SerializerOptions.BitFieldSchema` before any broader typed serializer-options refactor or WinUI schema-editor work.

## 2026-04-07 - Add endianness as byte-order metadata on bitfield definitions without changing the existing bit numbering rule
- Context: after the schema-backed bitfield slice landed, the next request was to make the feature branch endianness-aware. The existing low-level seam already defined `bit 0` as the LSB of the first payload byte, and changing that numbering rule immediately would have widened the semantic surface much more than necessary.
- Decision: create branch `feat/bitfield-endianness`, add `BitFieldEndianness` to both `BitFieldDefinition` and `BitFieldPayloadField`, and teach `BitFieldCodec` to support `BigEndian` as byte order for multi-byte fields while keeping `payload[0]` LSB = bit `0` unchanged. Restrict `BigEndian` multi-byte support to byte-aligned fields whose lengths are multiples of 8; keep sub-byte fields and partial-byte multi-byte big-endian semantics out of scope for now.
- Why: this is the smallest structurally correct way to make the bitfield layer endian-aware for realistic register-style payloads without reopening the more fundamental question of MSB-first bit numbering inside a byte.
- Consequences: the repo can now compose and inspect endian-aware whole-byte multi-byte fields through the schema and low-level codec layers, while any future need for MSB-first bit numbering or partial-byte big-endian multi-byte fields remains an explicit follow-up decision instead of an implicit half-defined behavior.

## 2026-04-09 - Make `ProtocolOptions` truthful to the live length-prefixed runtime contract
- Context: `ProtocolOptions` still exposed `UseCrc`, `Stx`, and `Etx`, and the root sample config even advertised `StxEtx`, but the runtime only shipped `LengthPrefixedProtocol` and `ProtocolFactory` ignored every option except `Type`. `MaxFrameLength` was validated in `DeviceProfileValidator`, yet the live protocol implementation did not enforce it during encode/decode.
- Decision: keep the current runtime protocol surface narrow and truthful instead of widening into a second framing family. Remove inactive CRC/STX/ETX knobs from `ProtocolOptions`, make `LengthPrefixedProtocol` enforce `MaxFrameLength`, pass that limit through `ProtocolFactory`, and reject unsupported protocol types early in `DeviceProfileValidator`. Clean the sample config/example code so they no longer imply unsupported framing behavior.
- Why: this is the smallest structurally correct change that turns configuration and runtime behavior into the same contract without introducing a half-designed delimiter/CRC protocol just to preserve old config fields.
- Consequences: the repo now tells the truth about its framing support, sample config is less misleading, and any future CRC or STX/ETX work should be handled as an explicit new protocol-expansion slice driven by a real device contract instead of by dormant settings.

## 2026-04-09 - Keep post-connect receive failure terminal in the core library and fail pending requests on session shutdown
- Context: after the first `ConnectionManager` hardening slice and the protocol-contract cleanup, the next unresolved runtime question was whether a live session whose background receive pump fails should auto-reconnect, remain terminal, or delegate recovery to a higher orchestration layer. At the same time, pending request tasks could otherwise hang forever on receive failure, explicit disconnect, or same-device session replacement.
- Decision: keep the core-library policy intentionally conservative. A post-connect background receive failure now makes that session terminal inside `ConnectionManager`; failed sessions are hidden from `GetSession()`, later send/manual-inbound calls rethrow the stored receive failure, and pending response tasks fail immediately on receive failure, explicit disconnect, and same-device session replacement. Keep `ReconnectOptions` as a connect-time transport-open retry contract only for now instead of interpreting it as live-session auto-recovery.
- Why: this is the smallest structurally correct runtime contract that removes ambiguity and hanging request tasks without widening into a half-designed reconnect state machine, resend policy, or user-visible orchestration model.
- Consequences: callers now get immediate failure instead of silently waiting on dead sessions, manual reconnect stays an explicit higher-layer action, and any future automatic runtime recovery must be designed as a separate slice with clear semantics for session identity and in-flight requests.

## 2026-04-09 - Normalize the delivery base before adding more production-readiness code
- Context: the runtime-hardening review branch was intentionally created from the in-progress raw-hex / bitfield worktree so work could continue quickly, but a later PR inspection showed that draft PR `#4` therefore includes earlier raw-hex / bitfield commits and files in addition to the runtime-hardening commit. The active checkout also still carries unrelated unstaged bitfield / WinUI follow-up edits.
- Decision: do not keep stacking new production-readiness slices on the current mixed delivery line. Before more hardening implementation, create a clean branch/worktree rooted in `commlib-hub/main` (or an equivalent merged runtime-hardening base) and move the runtime-hardening commits there so future PRs remain narrow and reviewable.
- Why: delivery scope is now a more immediate risk than reconnect naming cleanup; continuing on the mixed branch would make every later production slice harder to review, merge, or revert cleanly.
- Consequences: PR `#4` should be treated as a temporary draft artifact rather than the final delivery vehicle unless its base is normalized, and the next implementation slices should proceed one branch/PR at a time from the cleaned base.

## 2026-04-10 - Start unsolicited inbound hardening with a bounded queue and backpressure-first semantics
- Context: `ConnectionManager` still uses `Channel.CreateUnbounded<InboundEnvelope>()` for unmatched inbound messages, so unsolicited traffic can grow without a hard memory ceiling if the caller consumes `ReceiveAsync()` too slowly. The delivery-base cleanup is now recorded as complete through `feat/runtime-hardening-clean-base` / draft PR `#5`, which makes this the next clean runtime slice.
- Decision: keep the first buffering hardening step inside `ConnectionManager`, replace the unbounded per-device queue with a bounded queue, and prefer backpressure-first full behavior (`BoundedChannelFullMode.Wait`) over drop or disconnect semantics in the first slice. Treat queue capacity as an internal runtime default first; only widen to profile or hosting configuration if implementation evidence shows that one size is not structurally sufficient.
- Why: this is the smallest structurally correct way to put a hard memory ceiling on unsolicited inbound traffic without silently losing messages or mixing bootstrap, hosting, and public configuration redesign into the first fix.
- Consequences: runtime memory becomes predictable, a slow unsolicited-message consumer can intentionally stall further unsolicited reads once the queue is full, and richer overflow policies, public tuning knobs, or pressure-specific event hooks remain explicit follow-up work rather than accidental scope creep.

## 2026-04-13 - Resolve the validated dirty bitfield / WinUI slice before resuming runtime hardening
- Context: the current checkout `feat/runtime-readiness-hardening` still contains uncommitted changes across domain, WinUI, tests, and planning docs that belong to the earlier serializer/composer direction rather than to the next runtime-hardening queue slice. Focused unit tests and the WinUI build now pass for that dirty change set.
- Decision: treat the already-validated dirty bitfield / WinUI diff as today's immediate work unit. Finish it as one coherent slice, or park/split it safely, before switching back to `feat/runtime-hardening-clean-base` for more production-hardening edits.
- Why: starting new runtime-hardening work on this mixed dirty worktree would recreate the same branch-scope problem that already forced the clean delivery-base split.
- Consequences: the bounded unsolicited inbound-buffering work remains the next promoted runtime task, but only after the current serializer/composer follow-up is either landed cleanly or safely moved out of the way.

## 2026-04-14 - Keep per-device operation gates reference-counted so disconnected devices do not retain semaphores
- Context: `ConnectionManager` serialized same-device `ConnectAsync()` / `DisconnectAsync()` calls through `_deviceOperationGates`, but the dictionary only ever grew because gates were created on first use and never removed after the device session was gone.
- Decision: replace the raw `SemaphoreSlim` registry with a private per-device gate entry that owns the semaphore plus a lease count. Increment the lease count before waiting, decrement it on every release or cancelled wait, and remove the gate entry only when the lease count reaches zero and no connection state remains for that device.
- Why: simply removing the semaphore on `DisconnectAsync()` would race with concurrent same-device operations and could create split-brain locking where two different semaphores exist for one `deviceId`. Reference-counted gate entries keep the existing serialization contract while making the registry bounded for disconnected devices.
- Consequences: active devices still retain one gate entry while connected, disconnected devices release their gate entry automatically after the last in-flight operation finishes, and future same-device lifecycle work should continue to acquire/release the gate through the shared helper path rather than touching `_deviceOperationGates` directly.

## 2026-04-14 - Use explicit UTF-8 when bulk-editing Korean C# comments or literals
- Context: the repo-wide Korean XML documentation sweep touched localized WinUI files such as `AppLocalizer.cs`, and an early bulk-edit path that relied on implicit shell/text capture corrupted Korean string literals before verification caught it.
- Decision: when bulk-editing Korean source in this repo, use explicit UTF-8 read/write for file transforms and treat localized WinUI files as a separate verification checkpoint instead of trusting default shell encoding or plain `git show` text capture.
- Why: implicit encoding paths can look fine in ASCII-heavy files while silently corrupting Korean comments or string literals, which then surface later as hard-to-explain compile failures.
- Consequences: future doc/comment sweeps should validate encoding-sensitive files after the transform, and a clean standalone WinUI build is now part of the minimum proof when localized source files were touched.

## 2026-04-17 - Promote `DeviceSession` pending-response abstraction cleanup as the next execution slice
- Context: the older 2026-04-14 planning state still pointed at `HandleTimeoutAsync` cleanup and hosted-service wiring, but `commlib-hub/main` already contains the later runtime follow-up merges from PR `#18`, PR `#19`, PR `#20`, PR `#22`, and PR `#24`. After reconciling that integrated baseline, the remaining open follow-ups include queue-pressure signaling, `IConnectionEventSink` DI surfacing, `DeviceBootstrapper.StartAsync()` parallelization, `TryHandleInboundFrame` public-surface cleanup, and reflection-based pending-response dispatch inside `DeviceSession`.
- Decision: make the next work unit a narrow `DeviceSession` internal cleanup that replaces reflection-based completion/failure dispatch with a private typed pending-entry abstraction. Start that work from `commlib-hub/main` or a fresh feature branch created from it, not from the preserved mixed `feat/runtime-readiness-hardening` checkout.
- Why: this is the highest-confidence, code-local, evidence-ready slice still open after the merged runtime follow-ups. It improves a hot path without requiring unresolved hosting, observability, or deployment-boundary decisions.
- Consequences: queue-pressure signaling stays deferred until a real operator/deployment requirement exists; `IConnectionEventSink` DI surfacing, bootstrap parallelization, and `TryHandleInboundFrame` truthfulness cleanup remain queued behind this smaller slice rather than widening the next step immediately.

## 2026-04-17 - Generate XML documentation only for the public library projects
- Context: moving `GenerateDocumentationFile` into `Directory.Build.props` for the whole repo made `dotnet test` surface a large warning backlog from test/example projects, which hid the actual useful signal from the packable libraries.
- Decision: keep XML documentation generation enabled only for the packable library projects under `src/` and leave tests/examples opt-in instead of globally forcing documentation output everywhere.
- Why: the public library packages benefit from XML docs, but repo-wide warning noise is not worth it when the goal is a clean, truthful public-ready baseline.
- Consequences: `dotnet test` output is readable again, library XML gaps stay visible, and tests/examples will need an explicit opt-in later if the team decides those projects also need generated XML docs.

## 2026-04-17 - Treat the missing repository license as an explicit publication blocker, not an implementation guess
- Context: the repo is now much closer to public-ready after the README/CI/package cleanup, but there is still no root `LICENSE`.
- Decision: do not auto-add MIT, Apache-2.0, or any other license just to make the repo look complete. Record the missing license as an explicit blocker that requires a maintainer choice.
- Why: license selection is a legal/project-governance decision, not a safe code-only default.
- Consequences: the repository can continue improving technically, but it should not be described as fully open-source ready until the maintainer chooses and adds the intended license.

## 2026-04-17 - Finish GitHub-side cleanup only through credentials that can actually write issue state and workflow files
- Context: PR `#25` merged the outstanding integration work into `main`, and normal git pushes were sufficient to publish the integration branch and delete remote branches. However, restoring `.github/workflows/ci.yml` and closing stale issues `#21` / `#23` both failed in this environment because the available GitHub app integration and PAT-backed `gh`/REST calls returned `403 Resource not accessible`.
- Decision: treat the remaining workflow/issue cleanup as a credential-scope problem, not as a product-code problem. Stop after the successful PR merge and remote-branch cleanup, and leave the final GitHub-side cleanup explicit for a later pass with the correct permissions.
- Why: inventing more local branch churn or alternative code paths would not solve the actual blocker; the missing capability is GitHub write scope for workflow files and issue state.
- Consequences: `main` now contains the integrated code and the remote branch list is clean, but `.github/workflows/ci.yml` still needs a higher-scope write path and issues `#21` / `#23` will stay open until that credential is available.

## 2026-04-17 - Keep root continuity files in place for now, but mark them as internal development artifacts
- Context: after PR `#25`, the remaining publication blockers included the missing `LICENSE` plus a decision about whether `AGENT.md`, `CURRENT_PLAN.md`, `TODOS.md`, `CHANGELOG_AGENT.md`, `DECISIONS.md`, and `PROGRESS.md` should stay at the repository root, move elsewhere, or be retired. The repo workflow and continuity instructions still reference these paths directly, and `PROGRESS.md` still has its own encoding-cleanup backlog.
- Decision: keep the existing root paths for now and make the policy explicit instead of moving files prematurely. Mark the continuity files as internal development artifacts in the root-facing docs/files, and defer any relocation until the workflow/automation boundary is intentionally redesigned.
- Why: this is the smallest structurally correct publication cleanup that improves the public repo story without breaking the current local workflow, hook expectations, or continuity instructions.
- Consequences: the repository root is now intentionally honest about which files are internal, public readers are pointed toward `src/`, package metadata, and example READMEs for the real product contract, and future relocation of these files remains possible once automation and encoding constraints are addressed.

## 2026-04-17 - Finish the remaining repo-publication cleanup on a fresh branch from remote main, not on the conflicted local main worktree
- Context: after the root publication-policy documentation pass was prepared on `chore/repo-publication-policy`, the user switched the primary worktree back to local `main`. That local `main` was later found to be in a conflicted, partially merged state and diverged significantly from `commlib-hub/main`, which made it unsafe to continue repository-cleanup work there.
- Decision: create `chore/repo-finish` directly from `commlib-hub/main`, cherry-pick the already-reviewed root publication-policy commit onto it, and restore `.github/workflows/ci.yml` there instead of trying to salvage the conflicted local `main` index.
- Why: this is the smallest safe path that protects the user's unresolved local `main` state while keeping the final repository-publication cleanup branch narrow and reviewable.
- Consequences: the remaining publication cleanup is now isolated on a clean branch, GitHub `main` can receive the workflow restoration through a normal branch/PR flow, and the still-divergent local `main` worktree can be handled separately without blocking repository hygiene.

## 2026-04-17 - Treat workflow publication as still blocked until a credential with workflow-file write capability is used
- Context: `chore/repo-finish` now contains the restored `.github/workflows/ci.yml`, the root publication-policy documentation, and green local verification. However, publishing that branch from this environment failed through both available write paths.
- Decision: keep the workflow restoration committed locally on `chore/repo-finish`, but record publication itself as the remaining blocker until a token/account with workflow-file write capability is used. Do not keep retrying the same under-scoped PAT or GitHub app integration from this environment.
- Why: the code/doc work is complete, and repeated failed writes would not add value. The actual missing capability is permission to create/update workflow-file refs on GitHub.
- Consequences: future work should start from the already-prepared local branch/commit (`5655677` on `chore/repo-finish`) and publish it from a higher-scope environment before addressing the still-separate `LICENSE` decision.

## 2026-04-17 - Use MIT for the repository license and package metadata
- Context: after the publication branch was fully prepared except for remote workflow-file publication, the remaining maintainer decision was which repository license to adopt.
- Decision: use MIT, add the standard root `LICENSE` file, and set `PackageLicenseExpression` to `MIT` in `Directory.Build.props` so the NuGet package metadata stays truthful alongside the repo-level license.
- Why: the maintainer explicitly chose MIT, and package metadata should match the repository license instead of leaving the published package surface ambiguous.
- Consequences: the explicit license-choice blocker is now resolved locally, the remaining blocker is only publishing `chore/repo-finish`, and future package builds from this branch will carry MIT license metadata.

## 2026-04-17 - Split publication into a pushable non-workflow branch and a separate local workflow-restoration branch
- Context: `chore/repo-finish` contains the full repository-publication cleanup, including `.github/workflows/ci.yml`, but every available local publication path still resolves to a PAT that GitHub rejects for workflow-file updates. The GitHub app integration is also under-scoped for ref creation in this repo.
- Decision: keep `chore/repo-finish` intact as the local source of truth for the validated workflow restoration, but create `chore/repo-finish-publishable` from `commlib-hub/main` and copy only the non-workflow cleanup onto it so the MIT license, package metadata, root-policy docs, and continuity updates can still be pushed immediately.
- Why: this is the smallest safe way to publish the repo/license cleanup that does not depend on workflow-file write permission, while keeping the still-blocked workflow restoration explicit and reviewable.
- Consequences: GitHub can receive most of the repo-publication cleanup now, but `.github/workflows/ci.yml` still requires a later publish from `chore/repo-finish` or an equivalent higher-scope cherry-pick.

## 2026-04-17 - Finish the last publication step on a minimal workflow-only branch
- Context: after PR `#26` merged the MIT/license/root-policy subset into `main`, the only remaining live diff was the missing `.github/workflows/ci.yml`. Keeping the wider `chore/repo-finish` branch as the delivery vehicle would leave already-landed repo/documentation commits mixed back into the final step.
- Decision: create `chore/restore-ci-workflow` directly from the updated `commlib-hub/main` and carry only the workflow restoration commit there.
- Why: this keeps the final publication step minimal, reviewable, and easy to reason about while avoiding duplicate repo/doc churn on top of the already-merged `main`.
- Consequences: once `chore/restore-ci-workflow` lands, repository-level publication cleanup is complete and the next work can return to the previously queued `DeviceSession` internal cleanup.

## 2026-04-17 - Collapse `DeviceSession` pending-response state into typed private entries
- Context: `DeviceSession` tracked pending responses through three overlapping structures: a dictionary of `object`-typed `TaskCompletionSource<>` instances, a separate timeout-registration dictionary, and `PendingRequestStore`. Completing or failing responses from the non-generic path relied on reflection over `TaskCompletionSource<>`, and a response with the right correlation id but the wrong concrete response type could silently remove the pending entry.
- Decision: replace the object/reflection path with a single dictionary of typed private pending-entry objects that own response completion, timeout registration, and failure propagation. Remove `PendingRequestStore` entirely because it only duplicated the keys already stored in the pending-response map.
- Why: this is the smallest structurally correct cleanup that removes redundant internal state, avoids reflection in a hot path, and makes mismatched response handling truthful instead of silently dropping tracked work.
- Consequences: `DeviceSession` now has one authoritative pending-response representation, tests no longer need to reflect into the old timeout-registration field, and the next cleanup slice can move to other ambiguous runtime paths such as `ConnectionManager.TryHandleInboundFrame`.

## 2026-04-18 - Internalize `ConnectionManager.TryHandleInboundFrame` because it is a repo-only test seam, not a supported public runtime path
- Context: after the `DeviceSession` cleanup landed, the main remaining code-local truthfulness gap was `ConnectionManager.TryHandleInboundFrame(...)`. The method sat on a public concrete runtime type even though `IConnectionManager` never exposed it and the only repo callers were three infrastructure tests.
- Decision: keep the method only as an in-repo test seam and reduce its visibility from `public` to `internal` instead of keeping a second public inbound-entry path alive.
- Why: this is the smallest structurally correct way to keep the runtime contract honest without deleting useful low-level tests or widening the main receive-pump API again.
- Consequences: external callers are steered back to the supported runtime paths (`ConnectAsync`, `SendAsync`, `ReceiveAsync`, `GetSession`), while infrastructure tests can still exercise decode/pending-response behavior directly through the existing friend-assembly boundary.

## 2026-04-18 - Treat caller-registered `IConnectionEventSink` as the supported observability seam for now
- Context: the backlog still described `IConnectionEventSink` as effectively unavailable through DI, but `AddCommLibCore()` already resolves `IConnectionEventSink` from the service provider when constructing `ConnectionManager`. The real missing piece was proof and documentation, not a second hosting API.
- Decision: do not add a new hosting wrapper or alternate connection-manager registration path in this slice. Instead, keep the current caller-registration model as the supported seam, add unit coverage that proves the sink reaches `ConnectionManager`, and document the registration path in `docs/quick-start.md`.
- Why: this closes the practical adoption gap with the smallest change and avoids widening the hosting surface before there is a stronger requirement for a richer observability package boundary.
- Consequences: production callers can already register logging/metrics hooks through DI today, the quick-start guide now shows that path explicitly, and any future question about moving the interface to a higher package layer can be handled separately from basic sink adoption.
