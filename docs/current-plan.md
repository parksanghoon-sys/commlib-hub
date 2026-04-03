# Current Plan

## Date
- 2026-04-03

## Current Scope
- WinUI example follow-up after localization foundation, wheel-scroll forwarding, restored `win-x64` default startup, conservative page transitions, the new scrollable auto-follow live log, the live `DeviceLabTheme` hookup, the new in-app mock peer path, and repo-level package/version centralization
- Next priority: validate the remaining UDP/Multicast mock flows and confirm transport-panel visibility in a real pointer session

## Confirmed State
- Branch is now `feat/winui-localization-foundation`.
- English/Korean language mode is persisted in the WinUI example settings.
- Shell, Device Lab, Settings, and connection/session status copy now go through the example localizer.
- `PointerWheelScrollBridge` forwards text-input wheel events to the page `ScrollViewer` in `DeviceLab` and `Settings`.
- `AppShellView` animates page switches with a conservative fade + horizontal slide while keeping the existing dual-host layout.
- `DeviceLabView` live log keeps its own scrolling and auto-follows the newest log entry.
- The live-log follower now caches the inner `ScrollViewer` and explicitly scrolls it to the document end after each text update.
- `DeviceLabTheme` is now actually consumed by `AppShellView`, `DeviceLabView`, and `SettingsView` for shared backgrounds, card chrome, and typography instead of sitting unused.
- `DeviceLabView` and `SettingsView` now show only the currently selected transport panel instead of every transport preset at once.
- `DeviceLabView` now exposes an in-app `Mock Endpoint` card:
  - TCP and UDP pin loopback-friendly settings and start local echo peers
  - Multicast joins the selected group locally and replies back to the sender port for one-machine checks
  - Serial still requires an external paired COM environment
- `Directory.Build.props` now owns the shared `Nullable` / `ImplicitUsings` defaults, and `Directory.Packages.props` centrally manages the repo's package versions.
- The two test projects now reference `coverlet.collector` through the centralized package version file, and local `XPlat Code Coverage` runs produced Cobertura XML outputs successfully.
- The main WinUI example files now include Korean comments for non-obvious bootstrap, transition, logging, session, and mock-peer logic.
- Verification completed with:
  - `dotnet build commlib-codex-full.sln`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet test commlib-codex-full.sln`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --collect:"XPlat Code Coverage"`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --collect:"XPlat Code Coverage"`
  - a `win-x64` UI Automation smoke that found the localized window, confirmed the UDP-only `Local Port` label was hidden on the default TCP screen, invoked `Start Mock`, and completed a raw TCP echo roundtrip to `127.0.0.1:7001`
  - a follow-up `win-x64` UI Automation smoke that sent 12 TCP messages and confirmed the visible log range still reached the document end after the explicit end-scroll change
- `PROGRESS.md` still needs encoding normalization before safe in-place automated edits, although a UTF-8 append path worked for the 2026-04-03 daily log.

## Next Work Unit
1. Manually validate UDP and Multicast through the new in-app mock peer card.
2. Confirm transport-panel visibility while switching TCP / UDP / Multicast / Serial in both `Device Lab` and `Settings`.
3. Only if the multicast UX is too confusing on one machine, adjust the status copy or behavior with the smallest safe change.

## Deferred / Not For This Step
- `PROGRESS.md` full encoding normalization as separate repo hygiene work so future in-place edits do not need append-only handling.
- Full templated-control theming stays deferred until the WinUI default-style key coverage is mapped safely for this app shape.
