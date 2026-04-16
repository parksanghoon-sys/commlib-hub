# Current Plan

## Date
- 2026-04-16

## Current Scope
- Close issue `#9` with a reusable local WinUI transport validation helper.
- Keep this branch limited to helper commands, one PowerShell wrapper, and README guidance.

## Confirmed State
- Branch: `feat/issue-9-winui-transport-helper`
- Issue: `#9 Add reusable local WinUI transport validation helper`
- Draft PR: `#10 [codex] add WinUI transport validation helper`
- The console sample now exposes `tcp-echo-server` and `udp-echo-server` for external WinUI/manual validation.
- `scripts/Start-WinUiTransportValidation.ps1` now wraps the console sample for TCP/UDP echo plus multicast send/receive.
- The helper docs now state that this path is `AutoBinary` / `NoOpSerializer` only for now; `RawHex` still needs a different peer path.
- Focused validation succeeded with the console build plus TCP/UDP/helper smoke runs.

## Next Work Unit
1. Keep PR `#10` narrowly scoped to the helper branch.
2. After it lands, use the helper during the deferred live UDP / multicast WinUI validation pass.
3. Split any later multicast UX wording follow-up into its own branch if the live pass still justifies it.

## Deferred / Not For This Step
- No transport/runtime contract changes.
- No additional WinUI UI behavior changes.
- No broader runtime-hosting backlog work on this branch.
