# TODOS

## Current TODOs
- [ ] Manually validate the new UDP in-app mock peer flow from the current `win-x64` WinUI app.
- [ ] Manually validate the new Multicast in-app mock peer flow and decide whether the single-machine self-echo + peer-echo behavior needs clearer UX copy.
- [ ] Step through TCP / UDP / Multicast / Serial selection on both `Device Lab` and `Settings` with a real pointer session and confirm only the selected transport panel stays visible.
- [ ] Re-check transition feel, wheel-scroll behavior, and live-log manual scrolling only if the transport-panel/manual mock pass exposes a concrete regression.

## Deferred Backlog

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

## Completed
- [x] 2026-04-03: added `coverlet.collector` to both test projects through `Directory.Packages.props` and verified `XPlat Code Coverage` output generation for unit and infrastructure tests.
- [x] 2026-04-03: centralized shared MSBuild defaults into `Directory.Build.props` and moved package versions into `Directory.Packages.props`, removing inline package-version declarations from the project files.
- [x] 2026-04-03: re-validated the repo after central package management with `dotnet build commlib-codex-full.sln` and `dotnet test commlib-codex-full.sln --no-build`.
- [x] 2026-04-03: added Korean explanatory comments to the main WinUI example files, focusing on non-obvious DI/bootstrap, shell transition, wheel forwarding, live-log auto-follow, session-state, and local mock-peer paths instead of line-by-line boilerplate comments.
- [x] 2026-04-03: strengthened the WinUI live-log auto-follow by caching the log `TextBox` inner `ScrollViewer`, forcing it to `ScrollableHeight` after each text update, and re-verifying with a `win-x64` automation pass that still reached the document end after 12 sends.
- [x] 2026-04-03: collapsed the WinUI transport settings in both `DeviceLabView` and `SettingsView` so only the currently selected transport panel stays visible.
- [x] 2026-04-03: added an in-app `Mock Endpoint` card plus `ILocalMockEndpointService` / `LocalMockEndpointService` so the WinUI example can start local TCP/UDP/Multicast peers without a second process.
- [x] 2026-04-03: verified the new mock-peer path with `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`, `dotnet test commlib-codex-full.sln`, and a `win-x64` UI Automation smoke that invoked `Start Mock`, confirmed the default TCP screen hid the UDP-only `Local Port` label, and completed a raw TCP echo roundtrip to `127.0.0.1:7001`.
- [x] 2026-04-03: created the example-focused branch `feat/winui-localization-foundation`.
- [x] 2026-04-03: introduced WinUI localization foundation with persisted `AppLanguageMode`, `IAppLocalizer`/`AppLocalizer`, and localized shell/page/transport/language choices.
- [x] 2026-04-03: moved shell, Device Lab, Settings, and main connection/session status copy onto the localization path.
- [x] 2026-04-03: verified the localization pass with `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`, `dotnet test commlib-codex-full.sln`, and a 12-second `win-x86` smoke run.
- [x] 2026-04-03: added `PointerWheelScrollBridge` and routed `TextBox` mouse-wheel input back to the page `ScrollViewer` in `DeviceLabView` and `SettingsView`.
- [x] 2026-04-03: re-verified the wheel-scroll change with `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`, `dotnet test commlib-codex-full.sln`, and a focused UI Automation smoke run that confirmed `Settings` navigation plus root scroll movement.
- [x] 2026-04-03: attempted to reproduce the earlier local `win-x64` startup crash, but direct `win-x64` launch and `dotnet run -r win-x64 --no-build` both stayed alive for 12 seconds with no Application Error / Windows Error Reporting / .NET Runtime events.
- [x] 2026-04-03: restored the WinUI example default runtime from `win-x86` back to `win-x64`, updated the README note, and re-validated default `win-x64` plus explicit `win-x86` smoke runs.
- [x] 2026-04-03: added a conservative `AppShellView` fade + horizontal slide transition for `Device Lab` <-> `Settings` while keeping the existing dual-host page structure.
- [x] 2026-04-03: verified the transition change with `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`, `dotnet test commlib-codex-full.sln`, and direct `win-x64` / `win-x86` page-switch smoke runs that kept the app alive.
- [x] 2026-04-03: changed the `DeviceLabView` live log into a scrollable read-only multiline log surface with no-wrap lines and end-of-document auto-follow, then re-verified it with WinUI build/tests plus a local `win-x64` TCP echo automation pass that kept the visible log range pinned to the latest entry at 20 lines.
- [x] 2026-04-03: turned `DeviceLabTheme` into a live shared source for WinUI backgrounds, card chrome, and typography in `AppShellView`, `DeviceLabView`, and `SettingsView`, then re-verified the example with build/tests plus a successful direct `win-x64` launch and UI Automation window detection.
