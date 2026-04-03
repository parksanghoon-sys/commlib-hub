# Current Plan

Date: 2026-04-03

## Goal
Keep the WinUI example follow-up moving with the next work unit focused on validating the new in-app mock peer flow across the remaining transports now that transport-specific panels, conservative page transitions, the scrollable auto-follow live log, the live `DeviceLabTheme` hookup, and the new repo-level package management baseline are all in place.

## Confirmed Facts
- Active branch is `feat/winui-localization-foundation`.
- Localization foundation is implemented in the WinUI example:
  - persisted `AppLanguageMode` in `appsettings.json`
  - singleton `IAppLocalizer` / `AppLocalizer`
  - localized shell/page/transport/language choices
  - localized shell, Device Lab, and Settings page copy
  - localized connection/session/status text on the main flow
- `PointerWheelScrollBridge` forwards text-input mouse-wheel events back to the page `ScrollViewer` in `DeviceLabView` and `SettingsView`.
- `AppShellView` animates `Device Lab` <-> `Settings` with a conservative opacity + horizontal slide transition while keeping the existing dual-host page structure.
- `DeviceLabView` live log now uses a dedicated read-only multiline `TextBox` with its own scrollbars, no-wrap log lines, and end-of-document auto-follow.
- The live-log follower now caches the inner `ScrollViewer` and explicitly drives it to `ScrollableHeight` after each text update instead of relying only on moving the text selection to the end.
- `DeviceLabTheme` is now a live shared styling source for `AppShellView`, `DeviceLabView`, and `SettingsView` through safe brush/text/border resources.
- `DeviceLabView` and `SettingsView` now collapse transport settings down to the currently selected transport instead of showing TCP/UDP/Multicast/Serial panels all at once.
- `DeviceLabView` now includes a `Mock Endpoint` card backed by in-process `ILocalMockEndpointService` / `LocalMockEndpointService`.
- Repo-level build/package configuration is now partially centralized:
  - `Directory.Build.props` owns the shared `Nullable` and `ImplicitUsings` defaults
  - `Directory.Packages.props` owns the package versions for WinUI/toolkit, `Microsoft.Extensions`, test packages, `coverlet.collector`, and `System.IO.Ports`
  - project files now keep only project-specific package references without inline version numbers
- Both test projects now reference `coverlet.collector` with `PrivateAssets=all`, and local `XPlat Code Coverage` runs generated Cobertura reports successfully.
- The core WinUI example files now carry Korean comments around non-obvious lifecycle and control-flow areas, including DI bootstrap, shell/page transitions, wheel forwarding, live-log auto-follow, session state ownership, and local mock-peer runtime behavior.
- The local mock peer flow currently behaves as follows:
  - TCP starts a loopback echo listener on the selected TCP port and pins the host to `127.0.0.1`
  - UDP starts a loopback echo listener on the selected UDP remote port and pins the remote host to `127.0.0.1`
  - Multicast joins the selected group/port locally and replies back to the sender port for one-machine testing
  - Serial remains external-only and surfaces as unavailable for the in-app mock flow
- Verification completed on 2026-04-03:
  - `dotnet build commlib-codex-full.sln`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet test commlib-codex-full.sln`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --collect:"XPlat Code Coverage"`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --collect:"XPlat Code Coverage"`
  - a `win-x64` UI Automation smoke that found the running window, located the mock-start button in the localized UI, confirmed the TCP-default screen hid the UDP-only `Local Port` label, invoked `Start Mock`, and completed a raw TCP echo roundtrip to `127.0.0.1:7001` with payload `mock-tcp-ping`
  - a follow-up `win-x64` UI Automation smoke that started the local TCP mock, connected, sent 12 messages, and confirmed the live-log visible text range still reached the document end after the new explicit end-scroll logic
- The earlier local `win-x64` startup crash still does not reproduce on the current branch state, so the sample remains on the restored `win-x64` default path.
- Terminal-driven raw mouse-wheel injection was still not reliable enough to fully prove physical wheel behavior end-to-end, so real-pointer confirmation remains a manual follow-up rather than a closed automation item.
- `PROGRESS.md` still rejects in-place `apply_patch` edits because of encoding corruption, but a date-based UTF-8 append path worked for the 2026-04-03 entry, so the file is usable for additive daily logs while full normalization remains deferred.

## Next Work Unit
1. Manually validate the new UDP in-app mock peer flow from the running `win-x64` WinUI app.
2. Manually validate the new Multicast in-app mock peer flow and decide whether the single-machine self-echo + peer-echo behavior needs clearer UX copy.
3. Step through TCP / UDP / Multicast / Serial selection on both `Device Lab` and `Settings` with a real pointer session to confirm only the active transport panel remains visible.
4. Validate with:
   - interactive `win-x64` transport checks
   - local UDP / Multicast loopback sends through the new mock card
   - targeted `win-x86` spot-check only if a follow-up behavior tweak touches host/runtime-sensitive code

## Stop / Reassess Conditions
- If single-machine multicast produces confusing duplicate inbound lines, prefer clarifying status/log copy or documenting the expected behavior before redesigning the transport path.
- If any collapsed transport panel still appears in UI Automation or the live UI when it should be hidden, inspect the binding/converter path before widening the layout change.
