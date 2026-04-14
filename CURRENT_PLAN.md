# Current Plan

Date: 2026-04-07

## Goal
Continue the raw hex / future bitfield support design by turning the new schema-backed payload layer into one real consumer, while keeping transport/protocol behavior unchanged and still stopping short of a WinUI schema editor.

## Confirmed Facts
- The repository continuity rules currently point at `AGENT.md`; a root `AGENT_RULES.md` file is not present.
- Active branch is `feat/rawhex-compose-flow`.
- GitHub PR `#3` (`[codex] localize and streamline the WinUI device lab`) was already merged into `main`; the ongoing raw-hex and bitfield work remains isolated on the dedicated feature branch.
- The earlier WinUI follow-up work remains in place:
  - persisted English/Korean `AppLanguageMode`
  - localized shell / Device Lab / Settings / status copy
  - pointer-wheel forwarding for page text inputs
  - conservative Device Lab <-> Settings transition
  - scrollable auto-follow live log
  - active `DeviceLabTheme` hookup
  - collapsed transport-specific panels
  - in-app TCP / UDP / Multicast mock endpoint support
  - repo-level package/build centralization and coverage collector support
- The raw hex / bitfield direction still belongs in the serializer/composer path, not in transport creation or frame-boundary protocol logic.
- The raw-hex foundation from 2026-04-06 remains in place:
  - `IBinaryMessagePayload`
  - `BinaryMessageModel`, `BinaryRequestMessageModel`, and `BinaryResponseMessageModel`
  - `MessagePayloadFormatter`
  - `RawHexSerializer`
  - `SerializerFactory` support for `RawHex`
  - WinUI serializer selection plus localized raw-hex validation
  - transport-level and live WinUI raw-hex TCP roundtrip proof
- The first low-level bitfield foundation from 2026-04-06 remains in place:
  - `BitFieldDefinition`
  - `BitFieldCodec`
  - the `payload[0]` LSB = bit `0` convention
- The first schema-backed bitfield slice completed on 2026-04-07 without widening into protocol changes or a WinUI schema editor:
  - `BitFieldPayloadSchema`, `BitFieldPayloadField`, and `BitFieldScalarKind`
  - `BitFieldFieldAssignment` and `BitFieldFieldValue`
  - `BitFieldPayloadSchemaValidator`
  - `BitFieldPayloadSchemaCodec` for named-field compose/inspect above `BitFieldCodec`
  - optional `SerializerOptions.BitFieldSchema`
  - `DeviceProfileValidator` enforcement that schema usage is currently valid only with `RawHex`
  - `OutboundMessageComposer` overload for schema-driven binary payload composition
- The first schema slice also chose a single numeric value representation for the API surface:
  - compose assignments and inspect results use `decimal`
  - this keeps the first schema API simple while still covering the full signed and unsigned 64-bit scalar range that the low-level codec already supports
- Verification completed on 2026-04-07:
  - `dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --no-restore`
  - `dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --no-restore`
  - `dotnet build commlib-codex-full.sln --no-restore`
- The earlier `PROGRESS.md` encoding issue still remains deferred; this cycle did not attempt another in-place rewrite.

## Next Work Unit
1. Choose the smallest real consumer of `SerializerOptions.BitFieldSchema`, preferring config-backed inbound inspection/log enrichment over a WinUI schema editor or any transport change.
2. If that consumer stays small and reviewable, thread the schema through one example/config surface while keeping `RawHex` as the active transport/session payload carrier.
3. Revisit richer typed serializer/protocol options only if the first consumer exposes real pressure beyond the current optional schema seam.

## Stop / Reassess Conditions
- If the first consumer starts pulling both outbound schema editing and inbound inspection UI into the same slice, split the work and keep only one direction in scope.
- If schema usage starts requiring variable-length payloads or non-integer field types, stop and record that as follow-up instead of stretching the current fixed-length signed/unsigned model.
- If `SerializerOptions.BitFieldSchema` begins to force a broad options-hierarchy refactor, document the pressure and defer that refactor until after a concrete consumer proves it necessary.
