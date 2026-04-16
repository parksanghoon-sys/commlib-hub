# TODOS

## Execution Context
- Active branch: `feat/issue-23-bootstrap-reporting`
- Tracking issue: `#23`
- Parent baseline: `feat/issue-21-bounded-inbound-buffering` / draft PR `#22`
- Branch rule: keep this branch limited to bootstrap validation/reporting, direct `ConnectAsync()` validation, example cleanup, focused tests, and continuity updates

## Current TODOs
- [ ] Publish issue `#23` as a stacked draft PR on top of `feat/issue-21-bounded-inbound-buffering`.
  Scope: `DeviceBootstrapper`, `ConnectionManager`, bootstrap result types, focused tests, and only the example cleanup needed to remove manual validator calls.
  Validation: attach the unit/infrastructure test runs plus the console/WinUI example builds to the stacked PR.

## Deferred Backlog

### Runtime Hardening & Correctness
### [P1_SOON] Decide whether queue-pressure should remain internal backpressure or become a public runtime / hosting signal
- What remains: decide if bounded-queue pressure needs a surfaced event / metric / option beyond the current internal `Wait` behavior.
- Why deferred: the bounded-queue slice proved correctness first and this bootstrap slice intentionally avoids widening the hosting/runtime surface again.
- Objective: keep queue hardening observable and tunable without diluting the current bootstrap-focused review line.
- Relevant context: `ConnectionManager` now applies bounded backpressure internally, but no public signal exists yet for operators or hosts.
- Scope: `src/CommLib.Infrastructure/Sessions`, `src/CommLib.Hosting`, diagnostics hooks, and any follow-up tests/docs.
- Current status: intentionally excluded from issues `#21` and `#23`.
- Known blockers/open questions: whether production callers need a signal immediately or whether logs / metrics alone are sufficient.
- Most natural next step: revisit after `#22` and this bootstrap slice are published cleanly.

### [P1_SOON] Replace reflection-based `TrySetResponseResult` in `DeviceSession`
- What remains: remove the runtime `GetMethod(...).Invoke(...)` path from `TrySetResponseResult()` by introducing a typed pending-entry abstraction or another non-reflection completion path.
- Why deferred: it is orthogonal to bootstrap reporting and should stay a separate correctness/performance slice.
- Objective: eliminate reflection from the hot response-completion path without widening the current branch.
- Relevant context: the application layer still stores pending response completions as `object` in `_pendingResponses`, and the non-generic response completion path uses reflection.
- Scope: `src/CommLib.Application/Sessions/DeviceSession.cs`, focused unit tests, and any internal helper abstraction that replaces the reflection path.
- Current status: still pending after the timeout-cleanup work merged earlier today.
- Known blockers/open questions: whether the replacement should stay as a private nested helper or become a reusable application-layer abstraction.
- Most natural next step: design a private typed pending-entry wrapper and verify it through the existing `DeviceSessionTests`.

### [P2_LATER] Revisit whether `DeviceProfileValidator` should move into `CommLib.Domain`
- What remains: decide if validator ownership should move from `CommLib.Application.Configuration` to `CommLib.Domain.Configuration`.
- Why deferred: issue `#23` keeps the validator in place so bootstrap/reporting can land without a namespace-churn side quest.
- Objective: reduce conceptual coupling if configuration validation ownership becomes broader than the current bootstrap/runtime entry points.
- Relevant context: `ConnectionManager` now enforces validation at the direct connect boundary, but `CommLib.Infrastructure` already references `CommLib.Application`, so the current placement is workable.
- Scope: validator source file, validator tests/usings, examples, and any resulting project-boundary cleanup.
- Current status: explicitly deferred by design in this branch.
- Known blockers/open questions: whether the current placement causes enough confusion to justify extra churn.
- Most natural next step: reconsider only if review feedback or a later configuration-surface refactor makes the move clearly worthwhile.

### Production Integration & Hosting
### [P1_SOON] Expose `IConnectionEventSink` through DI without coupling callers to `ConnectionManager` internals
- What remains: give `AddCommLibCore()` a DI-friendly way to accept an `IConnectionEventSink` implementation.
- Why deferred: this bootstrap slice is intentionally runtime/bootstrap focused and should not widen the hosting surface.
- Objective: let production callers wire logging and metrics without reflection or internal constructor knowledge.
- Relevant context: `ConnectionManager` already accepts an optional `IConnectionEventSink`, and later queue-pressure work may want the same seam.
- Scope: `src/CommLib.Hosting`, `src/CommLib.Infrastructure/Sessions`, and any resulting interface-boundary adjustment.
- Current status: still deferred outside the current stacked bootstrap line.
- Known blockers/open questions: whether the sink should stay in infrastructure or move upward so hosting can reference it more cleanly.
- Most natural next step: revisit after the runtime queue and bootstrap slices have landed cleanly.

### API / Contract Truthfulness
### [P1_SOON] Decide whether `ReconnectOptions` naming is still too broad for connect-time retry only
- What remains: choose between doc-only clarification, a staged alias/deprecation path, or a later breaking rename.
- Why deferred: reconnect wording is unrelated to bootstrap reporting and should not be bundled into issue `#23`.
- Objective: keep the public configuration surface truthful without unnecessary compatibility churn.
- Relevant context: receive-failure behavior is already explicit, but reconnect configuration still only covers connect-time retry semantics.
- Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, `DeviceProfile`, docs, and any compatibility shim if a staged alias is chosen.
- Current status: still pending as a later clean slice.
- Known blockers/open questions: how much external dependency exists on the current property/type names.
- Most natural next step: inventory references before changing names.

### Repo Hygiene
### [P2_LATER] Normalize `PROGRESS.md` encoding for safe future updates
- What remains: rewrite `PROGRESS.md` into a stable UTF-8 form without losing history.
- Why deferred: it is orthogonal to issue `#23`, and a careless rewrite could damage project memory.
- Objective: make future progress updates safe for normal in-place editing.
- Relevant context: some repo files still require deliberate encoding handling during edits.
- Scope: `PROGRESS.md` only.
- Current status: still deferred.
- Known blockers/open questions: the precise legacy encoding mix and the safest normalization path.
- Most natural next step: back up the file and normalize it in one hygiene-focused branch.

## Completed
- [x] 2026-04-16: added bootstrap validation/reporting on `feat/issue-23-bootstrap-reporting`, including direct `ConnectAsync()` validation, `StartWithReportAsync()`, result types, example cleanup, and validation/build coverage.
- [x] 2026-04-16: ported bounded unsolicited inbound buffering onto `feat/issue-21-bounded-inbound-buffering` with a bounded `Wait` channel, reconnect-safe cleanup, and focused/full validation.
- [x] 2026-04-16: implemented the first split replacement slice for stale PR `#5` on top of `#19`, covering `LengthPrefixed` contract narrowing, max-frame enforcement, terminal receive failure handling, and matching focused/full validation.
- [x] 2026-04-16: rebuilt the stale `#8` hosting line on a fresh `main` base with `AddCommLibCore(IConfiguration)`, `CommLibHostedService`, focused hosted-service tests, and green unit/infrastructure validation.
- [x] 2026-04-16: merged PR `#18` for `DeviceSession` timeout cleanup and moved to the next runtime-facing line on a fresh branch.
