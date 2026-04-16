# TODOS

## Execution Context
- Active branch: `feat/runtime-hardening-stack-on-19`
- Tracking line: first split replacement slice for stale PR `#5`
- Parent baseline: `feat/hosting-lifecycle-wiring-main-base` / draft PR `#19`
- Branch rule: keep this branch limited to runtime contract hardening around `LengthPrefixed`, session failure handling, focused tests, and continuity updates

## Current TODOs
- [ ] Publish the first split replacement for stale PR `#5` as a stacked draft PR on top of `feat/hosting-lifecycle-wiring-main-base`.
  Scope: `ProtocolOptions`, `LengthPrefixedProtocol`, `ProtocolFactory`, `DeviceSession`, `ConnectionManager`, related tests, and only the config/example updates needed to keep those contracts compiling.
  Validation: attach the focused runtime tests, full unit/infrastructure test runs, and the console/WinUI example builds to the stacked PR.

## Deferred Backlog

### Runtime Hardening & Correctness
### [P1_SOON] Port bounded unsolicited inbound buffering as the next clean slice from old PR `#5`
- What remains: replace the unbounded unsolicited inbound queue in `ConnectionManager` with a bounded queue and preserve safe disconnect/reconnect cleanup behavior.
- Why deferred: this branch intentionally stops at runtime contract hardening so the first replacement slice stays reviewable.
- Objective: prevent unbounded unsolicited inbound growth without mixing bootstrap, hosting, or reconnect concerns into the same PR.
- Relevant context: old commit `6130ed4` carried this slice on top of the older runtime-hardening branch; it should now be replayed onto the current stacked clean branch instead.
- Scope: `src/CommLib.Infrastructure/Sessions/ConnectionManager.cs` plus focused infrastructure tests.
- Current status: not yet replayed onto the clean replacement line.
- Known blockers/open questions: whether the first bounded mode should remain backpressure-first (`Wait`) exactly as before or whether review feedback suggests a different first policy.
- Most natural next step: port the bounded-channel slice after this first runtime-contract PR is published.

### [P1_SOON] Replace reflection-based `TrySetResponseResult` in `DeviceSession`
- What remains: remove the runtime `GetMethod(...).Invoke(...)` path from `TrySetResponseResult()` by introducing a typed pending-entry abstraction or another non-reflection completion path.
- Why deferred: the first replacement slice for old PR `#5` already widened `DeviceSession` enough through terminal failure handling.
- Objective: eliminate reflection from the hot response-completion path without widening this branch.
- Relevant context: the current stacked runtime-contract branch still stores pending response completions as `object` in `_pendingResponses`, and the non-generic response completion path uses reflection to finish typed `TaskCompletionSource<TResponse>` instances.
- Scope: `src/CommLib.Application/Sessions/DeviceSession.cs`, focused unit tests, and any internal helper abstraction that replaces the reflection path.
- Current status: reflection is still live on the non-generic path even after pending-response failure handling was added here.
- Known blockers/open questions: whether the replacement should stay as a private nested helper in `DeviceSession` or become a reusable application-layer abstraction.
- Most natural next step: design a private typed pending-entry wrapper and verify it with the existing `DeviceSessionTests` surface.

### [P1_SOON] Port bootstrap validation/report flow as a separate clean slice from old PR `#5`
- What remains: move profile validation to the connection/bootstrap boundary and add `StartWithReportAsync()` with `DeviceBootstrapReport` / `DeviceBootstrapFailure`.
- Why deferred: this is a second independent behavior change and would make the first replacement slice too large.
- Objective: keep bootstrap failure handling explicit without coupling it to queueing or host-wiring concerns.
- Relevant context: old commit `994eeb6` carried this behavior and also moved `DeviceProfileValidator`; it now needs a fresh replay on top of the stacked clean branch.
- Scope: `src/CommLib.Application/Bootstrap`, `DeviceProfileValidator`, `ConnectionManager`, and focused unit/infrastructure coverage.
- Current status: not yet replayed onto the clean replacement line.
- Known blockers/open questions: whether the validator should remain in `CommLib.Application` for one intermediate slice or move directly to `CommLib.Domain.Configuration` during the replay.
- Most natural next step: port this after the bounded-queue slice unless review feedback suggests bootstrap correctness should land first.

### Production Integration & Hosting
### [P1_SOON] Expose `IConnectionEventSink` through DI without coupling callers to `ConnectionManager` internals
- What remains: give `AddCommLibCore()` a DI-friendly way to accept an `IConnectionEventSink` implementation.
- Why deferred: `#19` intentionally stays at Generic Host start/stop wiring only, and this first runtime slice also avoids widening the hosting surface.
- Objective: let production callers wire logging and metrics without reflection or internal constructor knowledge.
- Relevant context: `ConnectionManager` already accepts an optional `IConnectionEventSink`, and later old-`#5` slices also depend on that seam for queue-pressure events.
- Scope: `src/CommLib.Hosting`, `src/CommLib.Infrastructure/Sessions`, and any resulting interface-boundary adjustment.
- Current status: still deferred outside the current clean host/runtime split.
- Known blockers/open questions: whether the sink should stay in infrastructure or move upward so hosting can reference it more cleanly.
- Most natural next step: revisit this after the replacement PR for `#8` is published and the hosting layer is back on a clean base.

### API / Contract Truthfulness
### [P1_SOON] Decide whether `ReconnectOptions` naming is still too broad for connect-time retry only
- What remains: choose between doc-only clarification, a staged alias/deprecation path, or a later breaking rename.
- Why deferred: the reconnect wording belongs to a later old-`#5` slice and is not necessary for the first runtime-contract replacement to stand up.
- Objective: keep the public configuration surface truthful without unnecessary compatibility churn.
- Relevant context: the runtime-contract slice now makes terminal receive-failure behavior more explicit, which increases the value of clarifying that `ReconnectOptions` still covers connect-time retry only.
- Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, `DeviceProfile`, docs, and any compatibility shim if a staged alias is chosen.
- Current status: still pending as a later clean slice after the core behavior change lands.
- Known blockers/open questions: how much external dependency exists on the current property/type names.
- Most natural next step: inventory references before changing names.

### Review & Delivery
### [P1_SOON] Resolve the longer-lived open review lines on top of current `main`
- What remains: publish this first split replacement for old `#5`, then continue replacing the remaining old-`#5` slices before returning to PRs `#7` and `#6`.
- Why deferred: this branch should stay publishable as one narrow runtime slice before more old-`#5` behavior is layered on.
- Objective: reduce the amount of long-lived branch state that is still open after the helper/docs line landed.
- Relevant context: `#14`, `#16`, and `#18` are merged; `#19` is now the clean replacement for old `#8`; old `#5`, `#7`, and `#6` still remain open, but `#5` is being decomposed into smaller clean replacements.
- Scope: GitHub PR management plus any minimal rebasing/replacement work needed per line.
- Current status: `#19` is open as the clean `#8` replacement; the first split replacement for old `#5` is now implemented locally on this branch; `#7` and `#6` remain untouched after that.
- Known blockers/open questions: whether later old-`#5` slices should keep stacking on `#19` / this branch or wait until `#19` lands on `main`.
- Most natural next step: publish this stacked slice, then port the bounded inbound queue slice next.

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
- [x] 2026-04-16: implemented the first split replacement slice for stale PR `#5` on top of `#19`, covering `LengthPrefixed` contract narrowing, max-frame enforcement, terminal receive failure handling, and matching focused/full validation.
- [x] 2026-04-16: rebuilt the stale `#8` hosting line on a fresh `main` base with `AddCommLibCore(IConfiguration)`, `CommLibHostedService`, focused hosted-service tests, and green unit/infrastructure validation.
- [x] 2026-04-16: merged PR `#18` for `DeviceSession` timeout cleanup and moved to the next runtime-facing line on a fresh branch.
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
