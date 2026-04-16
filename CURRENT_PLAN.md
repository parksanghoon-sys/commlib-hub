# Current Plan

Date: 2026-04-16

## Goal
Close GitHub issue `#9` by adding a reusable local WinUI transport validation helper without widening into new transport/runtime contracts.

## Confirmed Facts
- This branch is `feat/issue-9-winui-transport-helper`, created from local `main` after the separate `commlib-codex-full` worktree was found dirty with preserved state-file edits.
- GitHub issue `#9` now tracks this helper follow-up: `Add reusable local WinUI transport validation helper`.
- `examples/CommLib.Examples.WinUI/README.md` previously pointed contributors at ad-hoc `CommLib.Examples.Console` commands for local peer setup.
- `examples/CommLib.Examples.Console` already contained the shared framing/serialization logic and multicast send/receive commands, so duplicating protocol behavior in PowerShell would be unnecessary.
- The current implementation on this branch now adds:
  - external-peer-only `tcp-echo-server` and `udp-echo-server` console commands
  - `scripts/Start-WinUiTransportValidation.ps1` as the repo-owned entry point for TCP/UDP echo and multicast send/receive flows
  - README guidance in both the WinUI and console examples
- Focused validation succeeded sequentially:
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj`
  - `powershell -ExecutionPolicy Bypass -File scripts/Start-WinUiTransportValidation.ps1 -Mode TcpEcho -NoBuild -TimeoutMs 200`
  - `powershell -ExecutionPolicy Bypass -File scripts/Start-WinUiTransportValidation.ps1 -Mode UdpEcho -NoBuild -TimeoutMs 200`
  - `powershell -ExecutionPolicy Bypass -File scripts/Start-WinUiTransportValidation.ps1 -Mode MulticastSend -NoBuild -Port 7004 -Message "helper smoke"`
- A focused `MulticastReceive` smoke without traffic still exits non-zero on timeout by design, and the README now calls that out explicitly.

## Next Work Unit
1. Review the helper diff for branch-scope discipline and keep this branch limited to the issue `#9` validation-helper slice.
2. Commit the helper plus continuity updates on this branch.
3. Push the branch and open a draft PR linked to issue `#9`.

## Next Slice Design
1. Keep the helper implementation as a thin wrapper over the existing console sample rather than introducing a second framing implementation in PowerShell.
2. Limit docs changes to the WinUI and console READMEs that directly explain the helper.
3. Leave the remaining live UDP / multicast / real-pointer WinUI validation pass as a later follow-up that can now reuse this helper.

## Stop / Reassess Conditions
- If helper expectations start demanding richer orchestration than a thin script plus console commands can provide, stop and re-evaluate whether the console example should grow dedicated validation subcommands instead of turning the script into a second protocol implementation.
- If a future live WinUI pass shows multicast ergonomics are still confusing, address that as a separate UI/docs slice rather than widening this helper branch.
