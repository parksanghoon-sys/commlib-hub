# CHANGELOG_AGENT

## 2026-04-16

- Created GitHub issue `#23` to track bootstrap validation/reporting as the next clean runtime slice stacked on top of the bounded-inbound branch.
- Started `feat/issue-23-bootstrap-reporting` from `feat/issue-21-bounded-inbound-buffering` so the next runtime change could stay isolated and reviewable while `#22` remains open.
- Ported the bootstrap validation/reporting slice only:
  - validated `DeviceProfile` at the direct `ConnectionManager.ConnectAsync()` boundary before runtime factories or transport-open side effects run
  - kept `DeviceBootstrapper.StartAsync()` fail-fast for invalid enabled profiles
  - added `DeviceBootstrapper.StartWithReportAsync()` plus `DeviceBootstrapReport` / `DeviceBootstrapFailure`
  - removed manual `DeviceProfileValidator` calls from the console and WinUI example connection paths so callers can rely on `ConnectAsync()` validation
- Added focused regression coverage for the new bootstrap behavior:
  - `StartAsync_WhenProfileIsInvalid_ThrowsBeforeConnectionManagerIsCalled`
  - `StartWithReportAsync_WhenProfilesIncludeValidationAndConnectionFailures_ContinuesAndReturnsReport`
  - `ConnectAsync_InvalidProfile_ThrowsBeforeRuntimeFactoriesRun`
- Verified the slice with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet restore examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj`
  - `dotnet restore examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj`
  - `dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --no-restore`
  - `dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj --no-restore -nodeReuse:false -maxcpucount:1`

- Created GitHub issue `#21` to track bounded unsolicited inbound buffering as its own clean runtime slice after the earlier PR / issue lines were cleaned up.
- Started `feat/issue-21-bounded-inbound-buffering` from the current `commlib-hub/main` so the next runtime change would stay isolated from preserved dirty worktrees.
- Ported the bounded unsolicited inbound buffering slice only:
  - added a default inbound queue capacity of `256` to `ConnectionManager`
  - let the internal `ConnectionManager` constructor accept an explicit `inboundQueueCapacity`
  - replaced the unsolicited inbound `Channel.CreateUnbounded` path with a bounded channel using `BoundedChannelFullMode.Wait`
  - kept disconnect / reconnect cleanup correct even when the receive pump is blocked on a full inbound queue
- Added focused infrastructure coverage for the new queueing behavior:
  - `ReceivePump_WithBoundedInboundQueue_BackpressuresTransportUntilConsumerDrains`
  - `DisconnectAsync_WhenReceivePumpIsBackpressured_CleansUpBlockedWriterAndAllowsReconnect`
- Verified the slice with:
  - `dotnet restore tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj`
  - `dotnet restore tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~ReceivePump_WithBoundedInboundQueue_BackpressuresTransportUntilConsumerDrains|FullyQualifiedName~DisconnectAsync_WhenReceivePumpIsBackpressured_CleansUpBlockedWriterAndAllowsReconnect"`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
