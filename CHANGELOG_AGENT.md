# CHANGELOG_AGENT

## 2026-04-03

- Published the WinUI/localization follow-up through PR `#3` and later confirmed it merged into `main`.
- Centralized shared MSBuild defaults in `Directory.Build.props`, package versions in `Directory.Packages.props`, and added `coverlet.collector` only to the two test projects.
- Completed the WinUI example follow-up around localization, mock endpoints, transport-panel collapsing, and the stronger live-log auto-follow behavior.

## 2026-04-09

- Normalized the runtime-hardening delivery onto a clean branch rooted in `commlib-hub/main` so the runtime review unit no longer rides on the raw-hex/bitfield branch lineage.
- Landed the protocol-contract hardening slice:
  - removed inactive `UseCrc`, `Stx`, and `Etx` options from `ProtocolOptions`
  - enforced `MaxFrameLength` in `LengthPrefixedProtocol`
  - passed the configured frame limit through `ProtocolFactory`
  - rejected unsupported protocol types and too-small frame limits up front in `DeviceProfileValidator`
  - cleaned sample/config/example surfaces so they no longer imply inactive framing behavior
- Landed the runtime recovery slice:
  - kept one per-device state object in `ConnectionManager`
  - serialized same-device lifecycle work through per-device gates
  - removed the accidental second `OpenAsync()` during connect
  - treated background receive failure as terminal for that session
  - hid failed sessions from `GetSession()`
  - failed pending response tasks on receive failure, explicit disconnect, and same-device session replacement
- Verified the runtime-hardening slice with:
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore`
- Re-ran the production-readiness review and kept the next blockers explicit:
  - unbounded unsolicited inbound buffering
  - missing connect/bootstrap validation policy
  - fail-fast bootstrap semantics
  - thin hosting / observability / secure-transport surface
