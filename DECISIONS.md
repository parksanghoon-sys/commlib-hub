# DECISIONS

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

## 2026-04-14 - Refresh local `main` through one temporary integration branch instead of merging the old mixed branch directly
- Context: the original mixed branch was not a safe delivery vehicle because it blended earlier raw-hex/bitfield history, runtime hardening work, and the later 2026-04-14 maintenance/docs commits. The user explicitly asked to merge to `main` once and restart cleanly.
- Decision: create `integration/main-refresh-20260414` from `commlib-hub/main`, merge the already-clean feature/runtime branches there, cherry-pick the new 2026-04-14 work-unit commits on top, and use that single integration branch as the source for refreshing local `main`.
- Why: this preserves narrow work-unit commits, avoids reusing the dirty mixed branch as a delivery line, and gives the requested one-time `main` merge without discarding already split review lines.
- Consequences: refreshed `main` becomes the new baseline, and the next implementation should start from a fresh branch whose only scope is the remaining `DeviceSession` timeout-cancellation cleanup.

## 2026-04-16 - Implement the WinUI local transport helper as a thin script over console commands, not as a second protocol implementation
- Context: the deferred WinUI validation-helper backlog item needed a repeatable local peer path for TCP/UDP/multicast, but the repo already had a console example that owned the real `LengthPrefixed + NoOpSerializer` framing logic.
- Decision: add external-peer-oriented `tcp-echo-server` / `udp-echo-server` commands to `CommLib.Examples.Console`, then wrap those commands plus the existing multicast send/receive commands with `scripts/Start-WinUiTransportValidation.ps1`.
- Why: this keeps one code path for frame encoding/decoding, avoids re-implementing protocol behavior in PowerShell, and still gives WinUI validation a one-command entry point.
- Consequences: future live UDP / multicast WinUI passes can reuse the helper immediately, while any richer orchestration needs can be evaluated later as console-example evolution rather than script-side protocol duplication.

## 2026-04-16 - Keep the helper branch narrow and document its serializer scope honestly instead of expanding it to RawHex immediately
- Context: the first helper branch reused the console sample's peer path, which is built on `LengthPrefixed + NoOpSerializer`. A design review pointed out that the README could otherwise make the helper look like a general external-peer path for any WinUI serializer mode.
- Decision: do not widen PR `#10` into serializer-aware peer expansion. Instead, document clearly that the helper is currently for `AutoBinary` validation, and direct `RawHex` validation to the in-app mock endpoint or another RawHex-speaking peer.
- Why: this preserves the branch's narrow scope and keeps the helper truthful without dragging a second serializer dimension into a docs/helper-only slice.
- Consequences: the next live WinUI validation pass can still use the helper safely for `AutoBinary`, while any future need for a `RawHex` external peer becomes its own explicit follow-up rather than an accidental extension of this PR.

## 2026-04-16 - Fix helper-backed multicast confusion in the README first, not in runtime or live UI copy
- Context: issue `#11` proved the multicast helper path is correct, but one-machine loopback means the WinUI live log can show an inbound copy of the app's own outbound multicast payload. The gap was operator truthfulness in the helper-backed docs rather than a transport defect.
- Decision: handle issue `#12` as a README-only wording change on a tiny stacked branch (`docs/issue-12-multicast-loopback-wording`) and keep `AppLocalizer.cs` / runtime behavior untouched for now.
- Why: this is the smallest structurally correct fix. The confusion appears first in the helper usage guidance, so clarifying the README is enough to tell the truth without widening into product text churn before review asks for it.
- Consequences: draft PR `#13` now carries only the README note and usage tip; if reviewers still want stronger wording in the live UI, that should happen in a separate follow-up branch rather than by expanding this docs slice retroactively.
