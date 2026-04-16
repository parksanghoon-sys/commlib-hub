# DECISIONS

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
