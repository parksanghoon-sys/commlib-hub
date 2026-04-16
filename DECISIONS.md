# DECISIONS

## 2026-04-16 - Stack bootstrap validation/reporting on top of the bounded-inbound branch instead of waiting for `main`
- Context: issue `#23` is the next runtime slice, but issue `#21` / PR `#22` is still open. Waiting for `#22` to merge would stall the current implementation flow even though the bounded-queue slice is already isolated and reviewable.
- Decision: build `feat/issue-23-bootstrap-reporting` on top of `feat/issue-21-bounded-inbound-buffering` and publish it as a stacked PR while `#22` remains open.
- Why: this preserves the branch strategy, keeps each slice small, and avoids folding bootstrap reporting into the bounded-queue branch just because the earlier PR has not merged yet.
- Consequences: PR ordering matters (`#22` first, then the stacked bootstrap PR), and if `#22` merges before review completes, the bootstrap PR can later be retargeted or restacked onto `main`.

## 2026-04-16 - Keep `DeviceProfileValidator` in `CommLib.Application.Configuration` for the bootstrap slice
- Context: the older bootstrap-report commit also moved `DeviceProfileValidator` into `CommLib.Domain.Configuration`. That move is architecturally arguable, but it adds namespace churn across tests and examples beyond the core bootstrap/reporting behavior.
- Decision: keep `DeviceProfileValidator` in `CommLib.Application.Configuration` for issue `#23`, while still enforcing it from both `DeviceBootstrapper` and `ConnectionManager.ConnectAsync()`.
- Why: this is the smallest structurally correct change for the current stack because it gives direct-connect validation and bootstrap reporting now without widening the slice into ownership refactoring.
- Consequences: validator relocation remains explicit deferred work; if review feedback or a later configuration-boundary refactor shows the current placement is too awkward, that move should happen as its own deliberate follow-up.

## 2026-04-16 - Rebuild bounded unsolicited inbound buffering as its own `main`-based runtime slice
- Context: after the earlier PR / issue lines were cleaned up, the next runtime-facing concern still waiting was bounded unsolicited inbound buffering. The old implementation existed inside a much larger runtime-hardening line, but that branch structure had already been decomposed.
- Decision: re-port bounded unsolicited inbound buffering onto a fresh `main`-based branch (`feat/issue-21-bounded-inbound-buffering`) and track it with GitHub issue `#21` instead of folding it into another wider runtime branch.
- Why: this keeps the branch strategy clean, makes review scope obvious, and avoids reintroducing stale stacked-branch coupling after the user cleared the prior PR line.
- Consequences: this slice can be reviewed and merged independently, while bootstrap reporting, queue-pressure signaling, reconnect naming, and other runtime follow-ups remain explicit later work.

## 2026-04-16 - Use `BoundedChannelFullMode.Wait` as the first bounded inbound policy
- Context: the goal of this slice is to prevent unbounded unsolicited inbound growth without inventing a broader delivery policy or public runtime signal in the same change.
- Decision: use a bounded channel with `BoundedChannelFullMode.Wait` so producers backpressure instead of dropping or replacing unsolicited inbound messages.
- Why: `Wait` is the most conservative correctness-first policy for the current library contract because it preserves inbound data while still bounding memory usage.
- Consequences: transport receive can stall behind consumer lag, which is acceptable for this first correctness slice; any later queue-pressure events, alternate drop policies, or host-level tuning stay explicit follow-up design work.
