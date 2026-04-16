# Current Plan

Date: 2026-04-16

## Goal
Deliver issue `#21` as a clean `main`-based runtime slice that bounds unsolicited inbound buffering without mixing bootstrap-report, reconnect-contract, or hosting-surface changes into the same branch.

## Confirmed Facts
- Active branch is `feat/issue-21-bounded-inbound-buffering`, created from the current `commlib-hub/main` after the user cleaned up the earlier PR / issue lines.
- GitHub tracking for this slice is issue `#21`: `Port bounded unsolicited inbound buffering as a clean runtime slice`.
- Current `main` already includes the previously split runtime and hosting lines, so this branch does not need to restack on top of another open feature branch.
- This branch now carries only the bounded unsolicited inbound buffering slice:
  - `ConnectionManager` owns a default inbound queue capacity of `256`
  - the internal constructor accepts an explicit `inboundQueueCapacity`
  - unsolicited inbound messages now use a bounded channel created with `BoundedChannelFullMode.Wait`
  - disconnect / reconnect cleanup keeps working even when the receive pump is blocked on a full queue
- This branch intentionally does **not** yet add:
  - bootstrap validation / `StartWithReportAsync()`
  - public queue-pressure signals or hosting-level queue-capacity configuration
  - `ReconnectOptions` wording changes
  - `DeviceSession` reflection removal
- Validation succeeded on 2026-04-16:
  - `dotnet restore tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj`
  - `dotnet restore tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~ReceivePump_WithBoundedInboundQueue_BackpressuresTransportUntilConsumerDrains|FullyQualifiedName~DisconnectAsync_WhenReceivePumpIsBackpressured_CleansUpBlockedWriterAndAllowsReconnect"`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`

## Next Work Unit
1. Commit the bounded inbound buffering slice and the continuity-file updates as separate commits.
2. Push `feat/issue-21-bounded-inbound-buffering` to `commlib-hub`.
3. Open a draft PR against `main` that stays limited to the bounded queue / focused tests only.

## Next Slice Design
1. Keep this branch reviewable as one correctness / runtime-pressure slice.
2. Treat bootstrap reporting as the next likely runtime slice after this PR is published.
3. Keep queue-pressure signaling, DI surface expansion, and reconnect naming as separate follow-up lines unless review feedback makes them blocking.

## Stop / Reassess Conditions
- If review feedback rejects `BoundedChannelFullMode.Wait` as the first policy, stop and decide whether to switch policy here or defer alternative queue-pressure semantics into a separate slice.
- If any additional runtime changes are needed outside `ConnectionManager` and its focused tests, reassess whether they belong in this branch or in the next runtime slice.
