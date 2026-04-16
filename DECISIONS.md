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

## 2026-04-16 - Cancel `DeviceSession` timeout waits through per-request registrations instead of letting orphaned `Task.Delay` calls run to completion
- Context: on current `main`, `DeviceSession` launched timeout work with fire-and-forget `Task.Delay(timeout)` calls. When a response completed early, the pending request entry disappeared, but the timeout task still stayed alive until the full delay elapsed.
- Decision: keep the fix local to `DeviceSession` by tracking a private `CancellationTokenSource` per timed request. Timed requests now register that token source on send, response completion cancels and disposes it immediately, and the timeout path disposes it after removing the pending request.
- Why: this is the smallest structurally correct fix for issue `#17`; it stops stale timeout waits without widening the branch into the separate reflection-removal refactor or a larger session shutdown redesign.
- Consequences: successful responses no longer leave unnecessary timeout waits alive, focused tests can assert that timeout registrations return to zero, and any broader pending-entry abstraction can remain a later dedicated follow-up.
