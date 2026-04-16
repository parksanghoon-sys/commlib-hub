# Current Plan

Date: 2026-04-16

## Goal
Close GitHub issue `#12` with the smallest truthful helper-backed multicast wording fix, stacked on the helper branch without widening into runtime behavior or broader UI-copy changes.

## Confirmed Facts
- This branch is `docs/issue-12-multicast-loopback-wording`, created as a new worktree branch from `feat/issue-9-winui-transport-helper` so the issue `#11` validation-only branch can stay untouched.
- GitHub issue `#12` tracks this wording-only follow-up: `Clarify helper-backed multicast self-loopback behavior in WinUI validation docs`.
- Draft PR `#13` now carries this branch against `feat/issue-9-winui-transport-helper`: `[codex] clarify helper multicast loopback note`.
- The parent helper implementation still lives on GitHub issue `#9` and draft PR `#10`.
- GitHub issue `#11` already proved the relevant runtime behavior before this branch was created:
  - helper `MulticastSend` reaches the WinUI app as inbound traffic
  - helper `MulticastReceive` captures the app outbound multicast frame
  - on one machine with loopback enabled, the WinUI live log can also show an inbound copy of the app's own outbound payload
- The issue `#12` change intentionally stays docs-only:
  - no transport/session/runtime behavior changes
  - no status-copy or localization changes in `AppLocalizer.cs`
  - only `examples/CommLib.Examples.WinUI/README.md` was updated
- The README now tells the truth about helper-backed one-machine multicast validation by:
  - stating that the app can log its own outbound multicast payload as an `Inbound message`
  - clarifying that this self-loopback line is expected rather than a helper failure
  - suggesting distinct helper/app message bodies to distinguish helper traffic from looped-back app traffic quickly

## Next Work Unit
1. Keep PR `#13` reviewable as a wording-only stacked diff on top of PR `#10`.
2. Do not widen this branch into additional localization/status-copy edits unless review shows the README-only clarification is insufficient.
3. Leave the remaining real-pointer-only WinUI confidence question as a separate manual follow-up, not as something to fold into this docs slice.

## Next Slice Design
1. Treat issue `#12` as a README clarification for the helper-backed path, not as a product behavior change.
2. Keep the wording anchored to the already-validated issue `#11` evidence instead of speculating about other multicast environments.
3. If review later shows the app UI itself still needs stronger wording, handle that in a separate tiny branch after this README-only slice.

## Stop / Reassess Conditions
- If reviewer feedback shows the README-only note is not enough to stop confusion, stop and decide whether the next change belongs in `AppLocalizer.cs` or in a broader helper-validation guide instead of expanding this branch ad hoc.
- If the helper branch `#10` changes its multicast contract materially before merge, re-check that this wording still matches the actual stacked base.
