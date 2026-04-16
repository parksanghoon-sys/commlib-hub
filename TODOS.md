# TODOS

## Execution Context
- Active branch: `fix/issue-17-device-session-timeout-cleanup`
- Tracking issue: GitHub issue `#17` (`Fix stale DeviceSession timeout waits after session disposal`)
- Parent baseline: current `commlib-hub/main` after helper/docs merges `#14` and `#16`
- Branch rule: keep this branch limited to `DeviceSession` timeout-wait cleanup, focused tests, and continuity updates

## Current TODOs
- [ ] Publish issue `#17` as a narrow review branch once the local timeout-cleanup fix and continuity updates are committed.
  Scope: `src/CommLib.Application/Sessions/DeviceSession.cs`, `tests/CommLib.Unit.Tests/DeviceSessionTests.cs`, and the root continuity files.
  Validation: keep the existing local proof (`DeviceSessionTests`, `CommLib.Unit.Tests`, focused `ConnectionManagerTests`) attached to the PR body.

## Deferred Backlog

### Runtime Hardening & Correctness
### [P1_SOON] Replace reflection-based `TrySetResponseResult` in `DeviceSession`
- What remains: remove the runtime `GetMethod(...).Invoke(...)` path from `TrySetResponseResult()` by introducing a typed pending-entry abstraction or another non-reflection completion path.
- Why deferred: issue `#17` intentionally stayed narrow and fixed only timeout-wait cleanup.
- Objective: eliminate reflection from the hot response-completion path without widening this branch.
- Relevant context: current `main` still stores pending response completions as `object` in `_pendingResponses`, and the non-generic response completion path uses reflection to finish typed `TaskCompletionSource<TResponse>` instances.
- Scope: `src/CommLib.Application/Sessions/DeviceSession.cs`, focused unit tests, and any internal helper abstraction that replaces the reflection path.
- Current status: reflection is still live on the non-generic path; timeout waits are now cleaned up separately on this branch.
- Known blockers/open questions: whether the replacement should stay as a private nested helper in `DeviceSession` or become a reusable application-layer abstraction.
- Most natural next step: design a private typed pending-entry wrapper and verify it with the existing `DeviceSessionTests` surface.

### Production Integration & Hosting
### [P1_SOON] Expose `IConnectionEventSink` through DI without coupling callers to `ConnectionManager` internals
- What remains: give `AddCommLibCore()` a DI-friendly way to accept an `IConnectionEventSink` implementation.
- Why deferred: this is a runtime-surface change and should not be mixed into issue `#17`.
- Objective: let production callers wire logging and metrics without reflection or internal constructor knowledge.
- Relevant context: `ConnectionManager` already accepts an optional `IConnectionEventSink`, but the hosting layer still does not surface it.
- Scope: `src/CommLib.Hosting`, `src/CommLib.Infrastructure/Sessions`, and any resulting interface-boundary adjustment.
- Current status: still deferred outside this timeout-only branch.
- Known blockers/open questions: whether the sink should stay in infrastructure or move upward so hosting can reference it more cleanly.
- Most natural next step: revisit this after the current timeout fix is out for review.

### API / Contract Truthfulness
### [P1_SOON] Decide whether `ReconnectOptions` naming is still too broad for connect-time retry only
- What remains: choose between doc-only clarification, a staged alias/deprecation path, or a later breaking rename.
- Why deferred: the current behavior is explicit, but issue `#17` is a narrower correctness fix and should not widen into public contract churn.
- Objective: keep the public configuration surface truthful without unnecessary compatibility churn.
- Relevant context: the runtime still treats post-connect receive failure as terminal and `ReconnectOptions` still affects connect-time transport-open retry only.
- Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, `DeviceProfile`, docs, and any compatibility shim if a staged alias is chosen.
- Current status: still pending on a separate branch/PR.
- Known blockers/open questions: how much external dependency exists on the current property/type names.
- Most natural next step: inventory references before changing names.

### Review & Delivery
### [P1_SOON] Resolve the longer-lived open review lines on top of current `main`
- What remains: review and merge or replace the still-open PRs `#5`, `#8`, `#7`, and `#6`.
- Why deferred: issue `#17` is a small correctness fix that should stay publishable on its own.
- Objective: reduce the amount of long-lived branch state that is still open after the helper/docs line landed.
- Relevant context: `#14` and `#16` are now merged; the remaining open lines are older runtime/hosting/rawhex branches.
- Scope: GitHub PR management plus any minimal rebasing/replacement work needed per line.
- Current status: all four PRs remain open at the time this branch was created.
- Known blockers/open questions: whether each line is still reviewable as-is against current `main` or now needs a clean replacement branch.
- Most natural next step: once issue `#17` is out, re-evaluate PR `#8` and PR `#5` first because they are the next runtime-facing lines.

### Repo Hygiene
### [P2_LATER] Normalize `PROGRESS.md` encoding for safe future updates
- What remains: rewrite `PROGRESS.md` into a stable UTF-8 form without losing history.
- Why deferred: it is orthogonal to issue `#17`, and a careless rewrite could damage project memory.
- Objective: make future progress updates safe for normal in-place editing.
- Relevant context: `apply_patch` still rejects in-place edits on some worktrees because `PROGRESS.md` contains non-UTF-8 or mixed-encoding content.
- Scope: `PROGRESS.md` only.
- Current status: still deferred.
- Known blockers/open questions: the precise legacy encoding mix and the safest normalization path.
- Most natural next step: back up the file and normalize it in one deliberate hygiene-focused branch.

### GitHub Hygiene
### [P3_NICE] Close stale issue `#11` once GitHub permissions allow it
- What remains: close GitHub issue `#11`, which describes the already-completed helper-backed WinUI validation pass.
- Why deferred: the current PAT could not close or comment on issues even though PR merge operations succeeded.
- Objective: keep the repo issue list aligned with actual completed work.
- Relevant context: issue `#11` is the only remaining open issue after issues `#9` and `#12` were auto-closed by merged PRs.
- Scope: GitHub issue state only.
- Current status: still open because of PAT permission limits, not because validation remains incomplete.
- Known blockers/open questions: whether the token permissions will be widened or whether this must be closed manually in the browser.
- Most natural next step: close it manually or with a higher-permission token the next time GitHub hygiene is touched.

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
