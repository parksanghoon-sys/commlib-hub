# Current Plan

## Date
- 2026-04-07

## Current Scope
- Continue the raw hex / future bitfield support design by turning the new schema-backed payload layer into one real consumer while keeping transport/protocol behavior unchanged
- Keep transport/protocol behavior unchanged and still stop short of a WinUI schema editor or transport/protocol rewrites for this cycle

## Confirmed State
- `AGENT.md` is the active repository rules file; `AGENT_RULES.md` is not present at the repo root.
- Branch remains `feat/rawhex-compose-flow`.
- GitHub PR `#3` for the earlier WinUI line of work is already merged into `main`, and the ongoing raw-hex / bitfield work stays isolated on the dedicated feature branch.
- The WinUI example follow-up from the prior cycle remains intact:
  - localized shell / Device Lab / Settings / status copy
  - page text-input wheel forwarding
  - conservative page transitions
  - scrollable auto-follow live log
  - active `DeviceLabTheme` usage
  - transport-panel collapsing
  - in-app TCP / UDP / Multicast mock endpoint support
  - repo-level package/build centralization
- The raw-hex library/application surface from 2026-04-06 remains in place:
  - `IBinaryMessagePayload`
  - `BinaryMessageModel`, `BinaryRequestMessageModel`, and `BinaryResponseMessageModel`
  - `MessagePayloadFormatter`
  - `RawHexSerializer`
  - `SerializerFactory` support for `RawHex`
  - WinUI serializer selection and localized raw-hex validation
  - automated transport/session raw-hex roundtrip coverage
  - a live WinUI raw-hex TCP roundtrip proof
- The low-level bitfield foundation from 2026-04-06 remains in place:
  - `BitFieldDefinition`
  - `BitFieldCodec`
  - the `payload[0]` LSB = bit `0` convention
- The first schema-backed bitfield slice completed on 2026-04-07:
  - `BitFieldPayloadSchema`, `BitFieldPayloadField`, and `BitFieldScalarKind`
  - `BitFieldFieldAssignment` and `BitFieldFieldValue`
  - `BitFieldPayloadSchemaValidator`
  - `BitFieldPayloadSchemaCodec` for named-field compose/inspect above `BitFieldCodec`
  - optional `SerializerOptions.BitFieldSchema`
  - `DeviceProfileValidator` enforcement that schema usage is currently valid only with `RawHex`
  - `OutboundMessageComposer` support for schema-driven binary payload composition
- The first schema API uses `decimal` for compose assignments and inspect results so the layer stays simple while still covering signed and unsigned 64-bit scalar values.
- Verification completed with:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- `PROGRESS.md` still needs separate encoding normalization before normal in-place automation is safe.

## Next Work Unit
1. Choose the smallest real consumer of `SerializerOptions.BitFieldSchema`, preferring config-backed inbound inspection/log enrichment over a WinUI schema editor or any transport change.
2. If that consumer stays small and reviewable, thread the schema through one example/config surface while keeping `RawHex` as the current payload carrier.
3. Revisit richer typed serializer/protocol options only if the first consumer exposes real pressure beyond the current optional schema seam.

## Deferred / Not For This Step
- Resume the older UDP / Multicast / pointer-session manual validation pass after the current serializer/composer work makes the next manual check more valuable.
- `PROGRESS.md` full encoding normalization stays separate repo hygiene work.
- Full templated-control theming stays deferred until the WinUI default-style key coverage is mapped safely for this app shape.
- A WinUI schema editor stays deferred until the lower schema-backed layer has a real consumer and review feedback.
