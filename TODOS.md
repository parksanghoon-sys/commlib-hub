# TODOS

## Execution Context
- Active branch: `feat/issue-21-bounded-inbound-buffering`
- Tracking issue: `#21`
- Parent baseline: current `commlib-hub/main` after the merged hosting/runtime slices
- Branch rule: keep this branch limited to bounded unsolicited inbound buffering, focused infrastructure coverage, and continuity updates

## Current TODOs
- [ ] Publish issue `#21` as a narrow review branch / draft PR on top of `main`.
  Scope: `ConnectionManager`, focused `ConnectionManagerTests`, and only the continuity-file updates needed to describe this clean slice.
  Validation: attach the focused bounded-queue tests plus full infrastructure/unit test runs to the PR.

## Deferred Backlog

### Runtime Hardening & Correctness
### [P1_SOON] Port bootstrap validation/report flow as the next clean runtime slice
- What remains: move profile validation to the connection/bootstrap boundary and add `StartWithReportAsync()` with `DeviceBootstrapReport` / `DeviceBootstrapFailure`.
- Why deferred: issue `#21` intentionally stops at bounded unsolicited inbound buffering so the branch stays reviewable and low-risk.
- Objective: make bootstrap failures explicit and structured without mixing that behavior into the queueing slice.
- Relevant context: older runtime hardening work already identified this as the next natural slice after the bounded queue change.
- Scope: `src/CommLib.Application/Bootstrap`, `DeviceProfileValidator`, `ConnectionManager`, and focused unit/infrastructure coverage.
- Current status: not started on the fresh `main`-based line yet.
- Known blockers/open questions: whether validator placement should stay application-side for one intermediate slice or move directly with the replay.
- Most natural next step: create a new issue + branch from updated `main` after this PR is published.

### [P1_SOON] Decide whether queue-pressure should remain internal backpressure or become a public runtime / hosting signal
- What remains: decide if bounded-queue pressure needs a surfaced event / metric / option beyond the current internal `Wait` behavior.
- Why deferred: the current slice proves correctness first and intentionally avoids widening the hosting/runtime surface.
- Objective: keep the first queue-hardening change small while leaving room for later observability and policy tuning.
- Relevant context: `ConnectionManager` now applies bounded backpressure internally, but no public signal exists yet for operators or hosts.
- Scope: `src/CommLib.Infrastructure/Sessions`, `src/CommLib.Hosting`, diagnostics hooks, and any follow-up tests/docs.
- Current status: intentionally excluded from issue `#21`.
- Known blockers/open questions: whether production callers need a signal immediately or whether logs / metrics alone are sufficient.
- Most natural next step: revisit only after this bounded-queue slice lands and the real host/ops need is clearer.

### [P1_SOON] Replace reflection-based `TrySetResponseResult` in `DeviceSession`
- What remains: remove the runtime `GetMethod(...).Invoke(...)` path from `TrySetResponseResult()` by introducing a typed pending-entry abstraction or another non-reflection completion path.
- Why deferred: it is orthogonal to issue `#21` and should stay a separate correctness/performance slice.
- Objective: eliminate reflection from the hot response-completion path without widening the current runtime queue branch.
- Relevant context: the current application layer still stores pending response completions as `object` in `_pendingResponses`, and the non-generic response completion path uses reflection.
- Scope: `src/CommLib.Application/Sessions/DeviceSession.cs`, focused unit tests, and any internal helper abstraction that replaces the reflection path.
- Current status: still pending after the timeout-cleanup work merged earlier today.
- Known blockers/open questions: whether the replacement should stay as a private nested helper or become a reusable application-layer abstraction.
- Most natural next step: design a private typed pending-entry wrapper and verify it through the existing `DeviceSessionTests`.

### Production Integration & Hosting
### [P1_SOON] Expose `IConnectionEventSink` through DI without coupling callers to `ConnectionManager` internals
- What remains: give `AddCommLibCore()` a DI-friendly way to accept an `IConnectionEventSink` implementation.
- Why deferred: issue `#21` is intentionally runtime-internal only and should not widen the hosting surface.
- Objective: let production callers wire logging and metrics without reflection or internal constructor knowledge.
- Relevant context: `ConnectionManager` already accepts an optional `IConnectionEventSink`, and later queue-pressure work may want the same seam.
- Scope: `src/CommLib.Hosting`, `src/CommLib.Infrastructure/Sessions`, and any resulting interface-boundary adjustment.
- Current status: still deferred outside the current bounded-queue slice.
- Known blockers/open questions: whether the sink should stay in infrastructure or move upward so hosting can reference it more cleanly.
- Most natural next step: revisit after the queue slice and bootstrap slice have landed cleanly.

### API / Contract Truthfulness
### [P1_SOON] Decide whether `ReconnectOptions` naming is still too broad for connect-time retry only
- What remains: choose between doc-only clarification, a staged alias/deprecation path, or a later breaking rename.
- Why deferred: reconnect wording is unrelated to the queueing slice and should not be bundled into issue `#21`.
- Objective: keep the public configuration surface truthful without unnecessary compatibility churn.
- Relevant context: receive-failure behavior is already explicit, but reconnect configuration still only covers connect-time retry semantics.
- Scope: `src/CommLib.Domain/Configuration/ReconnectOptions.cs`, `DeviceProfile`, docs, and any compatibility shim if a staged alias is chosen.
- Current status: still pending as a later clean slice.
- Known blockers/open questions: how much external dependency exists on the current property/type names.
- Most natural next step: inventory references before changing names.

### Repo Hygiene
### [P2_LATER] Normalize `PROGRESS.md` encoding for safe future updates
- What remains: rewrite `PROGRESS.md` into a stable UTF-8 form without losing history.
- Why deferred: it is orthogonal to issue `#21`, and a careless rewrite could damage project memory.
- Objective: make future progress updates safe for normal in-place editing.
- Relevant context: some repo files still require deliberate encoding handling during edits.
- Scope: `PROGRESS.md` only.
- Current status: still deferred.
- Known blockers/open questions: the precise legacy encoding mix and the safest normalization path.
- Most natural next step: back up the file and normalize it in one hygiene-focused branch.

## Completed
- [x] 2026-04-16: ported bounded unsolicited inbound buffering onto `feat/issue-21-bounded-inbound-buffering` with a bounded `Wait` channel, reconnect-safe cleanup, and focused/full validation.
- [x] 2026-04-16: implemented the first split replacement slice for stale PR `#5` on top of `#19`, covering `LengthPrefixed` contract narrowing, max-frame enforcement, terminal receive failure handling, and matching focused/full validation.
- [x] 2026-04-16: rebuilt the stale `#8` hosting line on a fresh `main` base with `AddCommLibCore(IConfiguration)`, `CommLibHostedService`, focused hosted-service tests, and green unit/infrastructure validation.
- [x] 2026-04-16: merged PR `#18` for `DeviceSession` timeout cleanup and moved to the next runtime-facing line on a fresh branch.
