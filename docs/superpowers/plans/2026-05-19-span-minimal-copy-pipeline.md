# Span Minimal-Copy Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make CommLib's frame receive/send pipeline prefer `ReadOnlyMemory<byte>` and `Span<byte>` so protocol decoding, receiver buffering, and common serializer/protocol encoding avoid avoidable intermediate `byte[]` copies while preserving the current `IProtocol` and `ISerializer` compatibility contracts.

**Architecture:** Add additive opt-in contracts beside the existing interfaces instead of replacing public APIs. Inbound decoding gets a zero-copy `ReadOnlyMemory<byte>` protocol path first, then `MessageFrameDecoder` and `TransportMessageReceiver` use that path when available. Outbound encoding gets a span-writer fast path that lets serializers write payload bytes directly into the final protocol frame, while unsupported custom implementations continue through the existing array-returning methods.

**Tech Stack:** C#/.NET, `ReadOnlyMemory<byte>`, `ReadOnlySpan<byte>`, `Span<byte>`, `System.Buffers.Binary`, xUnit, existing CommLib Domain/Infrastructure project layout.

---

## Scope

This plan covers the next coherent span/minimal-copy slice:

- Zero-copy protocol decode for `LengthPrefixedProtocol` and `BinaryFrameProtocol`.
- Memory-based message decoding path.
- Windowed receive buffer management in `TransportMessageReceiver` to stop re-copying the entire pending buffer on every chunk or frame consumption.
- Span-based outbound serialization and frame wrapping for the built-in serializers/protocols.

This plan does not add pooling ownership APIs such as `IMemoryOwner<byte>`, `ArrayPool<byte>`, or `IBufferWriter<byte>` to the public surface. Those would require a separate lifetime/ownership design after this additive compatibility slice is proven.

## File Structure

Create:

- `src/CommLib.Domain/Protocol/ProtocolDecodeResult.cs` - immutable decode result carrying a payload memory slice and consumed frame length.
- `src/CommLib.Domain/Protocol/IZeroCopyProtocol.cs` - optional protocol decode contract for implementations that can return payload slices into the caller-owned frame memory.
- `src/CommLib.Domain/Protocol/ProtocolFrameLayout.cs` - immutable outbound frame layout describing final frame length and payload slot.
- `src/CommLib.Domain/Protocol/IFrameEncodingProtocol.cs` - optional protocol encode contract for writing protocol headers/suffixes around a payload that is placed directly in the final frame.
- `src/CommLib.Domain/Protocol/ISpanSerializer.cs` - optional serializer contract for writing serialized payload bytes into a caller-provided destination.

Modify:

- `src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs` - implement zero-copy decode and frame-writer encode while preserving existing `IProtocol` methods.
- `src/CommLib.Infrastructure/Protocol/BinaryFrameProtocol.cs` - implement zero-copy decode and frame-writer encode while preserving existing checksum behavior.
- `src/CommLib.Infrastructure/Protocol/MessageFrameDecoder.cs` - add a `ReadOnlyMemory<byte>` overload that uses `IZeroCopyProtocol` when available.
- `src/CommLib.Infrastructure/Transport/TransportMessageReceiver.cs` - replace append-and-trim arrays with a reusable pending-buffer window.
- `src/CommLib.Infrastructure/Protocol/MessageFrameEncoder.cs` - use the span serializer plus frame encoding protocol fast path when both sides support it.
- `src/CommLib.Infrastructure/Protocol/RawHexSerializer.cs` - implement `ISpanSerializer` so binary payload messages do not allocate a second payload array before framing.
- `src/CommLib.Infrastructure/Protocol/NoOpSerializer.cs` - implement `ISpanSerializer` for the default text serializer.

Test:

- `tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs`
- `tests/CommLib.Infrastructure.Tests/BinaryFrameProtocolTests.cs`
- `tests/CommLib.Infrastructure.Tests/MessageFrameDecoderTests.cs`
- `tests/CommLib.Infrastructure.Tests/TransportMessageReceiverTests.cs`
- `tests/CommLib.Infrastructure.Tests/MessageFrameEncoderTests.cs`
- `tests/CommLib.Infrastructure.Tests/RawHexSerializerTests.cs`
- `tests/CommLib.Infrastructure.Tests/NoOpSerializerTests.cs`

## Compatibility Rules

- Keep `IProtocol.Encode(ReadOnlySpan<byte>)` and `IProtocol.TryDecode(ReadOnlySpan<byte>, out byte[], out int)` working.
- Keep `ISerializer.Serialize(IMessage)` and `ISerializer.Deserialize(ReadOnlySpan<byte>)` working.
- Make new contracts optional. Existing custom protocol/serializer implementations must continue compiling and running without change.
- Do not move Modbus-like address/function/register semantics into `IProtocol`; frame parsing remains protocol-level, and bit/register payload interpretation remains in the existing payload schema layer.

---

### Task 1: Add Zero-Copy Protocol Decode Contract

**Files:**

- Create: `src/CommLib.Domain/Protocol/ProtocolDecodeResult.cs`
- Create: `src/CommLib.Domain/Protocol/IZeroCopyProtocol.cs`
- Modify: `src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs`
- Modify: `src/CommLib.Infrastructure/Protocol/BinaryFrameProtocol.cs`
- Test: `tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs`
- Test: `tests/CommLib.Infrastructure.Tests/BinaryFrameProtocolTests.cs`

- [ ] **Step 1: Write failing zero-copy tests for length-prefixed frames**

Add these `using` statements to `tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs`:

```csharp
using System.Runtime.InteropServices;
using CommLib.Domain.Protocol;
```

Add this test to `LengthPrefixedProtocolTests`:

```csharp
[Fact]
public void TryDecodeMemory_CompleteFrame_ReturnsPayloadSliceWithoutCopy()
{
    var protocol = Assert.IsAssignableFrom<IZeroCopyProtocol>(new LengthPrefixedProtocol());
    var frame = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 };

    var decoded = protocol.TryDecode(frame.AsMemory(), out var result);

    Assert.True(decoded);
    Assert.Equal(7, result.BytesConsumed);
    Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, result.Payload.ToArray());
    Assert.True(MemoryMarshal.TryGetArray(result.Payload, out var segment));
    Assert.Same(frame, segment.Array);
    Assert.Equal(4, segment.Offset);
    Assert.Equal(3, segment.Count);
}
```

- [ ] **Step 2: Write failing zero-copy tests for binary frames**

Add these `using` statements to `tests/CommLib.Infrastructure.Tests/BinaryFrameProtocolTests.cs`:

```csharp
using System.Runtime.InteropServices;
using CommLib.Domain.Protocol;
```

Add this test to `BinaryFrameProtocolTests`:

```csharp
[Fact]
public void TryDecodeMemory_WithStartLengthAndCrc16Modbus_ReturnsPayloadSliceWithoutCopy()
{
    var protocol = Assert.IsAssignableFrom<IZeroCopyProtocol>(CreateProtocol());
    var frame = new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x10, 0x20, 0x30, 0x05, 0x5A, 0x99 };

    var decoded = protocol.TryDecode(frame.AsMemory(), out var result);

    Assert.True(decoded);
    Assert.Equal(9, result.BytesConsumed);
    Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, result.Payload.ToArray());
    Assert.True(MemoryMarshal.TryGetArray(result.Payload, out var segment));
    Assert.Same(frame, segment.Array);
    Assert.Equal(4, segment.Offset);
    Assert.Equal(3, segment.Count);
}
```

- [ ] **Step 3: Run protocol tests and confirm the expected compile failure**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests"
```

Expected: build fails because `IZeroCopyProtocol` does not exist.

- [ ] **Step 4: Add the zero-copy domain types**

Create `src/CommLib.Domain/Protocol/ProtocolDecodeResult.cs`:

```csharp
namespace CommLib.Domain.Protocol;

/// <summary>
/// Represents a decoded frame payload slice and the full encoded frame length consumed from the source buffer.
/// </summary>
public readonly record struct ProtocolDecodeResult(ReadOnlyMemory<byte> Payload, int BytesConsumed);
```

Create `src/CommLib.Domain/Protocol/IZeroCopyProtocol.cs`:

```csharp
namespace CommLib.Domain.Protocol;

/// <summary>
/// Optional protocol contract for implementations that can expose decoded payload bytes as a slice of the caller-provided frame memory.
/// </summary>
public interface IZeroCopyProtocol : IProtocol
{
    /// <summary>
    /// Attempts to decode one complete frame from <paramref name="buffer"/> without copying the decoded payload.
    /// </summary>
    bool TryDecode(ReadOnlyMemory<byte> buffer, out ProtocolDecodeResult result);
}
```

- [ ] **Step 5: Implement zero-copy decode in `LengthPrefixedProtocol`**

Change the class declaration:

```csharp
public sealed class LengthPrefixedProtocol : IProtocol, IZeroCopyProtocol
```

Replace the current decode method with a shared parser plus both decode paths:

```csharp
public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
{
    payload = Array.Empty<byte>();
    bytesConsumed = 0;

    if (!TryReadFrame(buffer, out var payloadOffset, out var payloadLength, out var frameLength))
    {
        return false;
    }

    payload = buffer.Slice(payloadOffset, payloadLength).ToArray();
    bytesConsumed = frameLength;
    return true;
}

public bool TryDecode(ReadOnlyMemory<byte> buffer, out ProtocolDecodeResult result)
{
    result = default;

    if (!TryReadFrame(buffer.Span, out var payloadOffset, out var payloadLength, out var frameLength))
    {
        return false;
    }

    result = new ProtocolDecodeResult(buffer.Slice(payloadOffset, payloadLength), frameLength);
    return true;
}

private bool TryReadFrame(ReadOnlySpan<byte> buffer, out int payloadOffset, out int payloadLength, out int frameLength)
{
    payloadOffset = 0;
    payloadLength = 0;
    frameLength = 0;

    if (buffer.Length < HeaderSize)
    {
        return false;
    }

    payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer[..HeaderSize]);
    if (payloadLength < 0)
    {
        throw new InvalidOperationException("Frame length cannot be negative.");
    }

    frameLength = checked(HeaderSize + payloadLength);
    if (frameLength > _maxFrameLength)
    {
        throw new InvalidOperationException(
            $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
    }

    if (buffer.Length < frameLength)
    {
        return false;
    }

    payloadOffset = HeaderSize;
    return true;
}
```

- [ ] **Step 6: Implement zero-copy decode in `BinaryFrameProtocol`**

Change the class declaration:

```csharp
public sealed class BinaryFrameProtocol : IProtocol, IZeroCopyProtocol
```

Refactor the current decode method into a shared parser. The legacy method still returns a copied payload array; the memory method returns a slice of the source frame:

```csharp
public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
{
    payload = Array.Empty<byte>();
    bytesConsumed = 0;

    if (!TryReadFrame(buffer, out var payloadOffset, out var payloadLength, out var frameLength))
    {
        return false;
    }

    payload = buffer.Slice(payloadOffset, payloadLength).ToArray();
    bytesConsumed = frameLength;
    return true;
}

public bool TryDecode(ReadOnlyMemory<byte> buffer, out ProtocolDecodeResult result)
{
    result = default;

    if (!TryReadFrame(buffer.Span, out var payloadOffset, out var payloadLength, out var frameLength))
    {
        return false;
    }

    result = new ProtocolDecodeResult(buffer.Slice(payloadOffset, payloadLength), frameLength);
    return true;
}

private bool TryReadFrame(ReadOnlySpan<byte> buffer, out int payloadOffset, out int payloadLength, out int frameLength)
{
    payloadOffset = 0;
    payloadLength = 0;
    frameLength = 0;

    if (buffer.Length < _startBytes.Length)
    {
        return false;
    }

    if (_startBytes.Length > 0 && !buffer[.._startBytes.Length].SequenceEqual(_startBytes))
    {
        throw new InvalidOperationException("Frame start bytes do not match the configured BinaryFrame start bytes.");
    }

    var lengthPrefixOffset = _startBytes.Length;
    if (buffer.Length < lengthPrefixOffset + _lengthPrefixSizeBytes)
    {
        return false;
    }

    payloadLength = ReadLengthPrefix(buffer.Slice(lengthPrefixOffset, _lengthPrefixSizeBytes));
    frameLength = checked(_startBytes.Length + _lengthPrefixSizeBytes + payloadLength + _checksumSizeBytes);
    if (frameLength > _maxFrameLength)
    {
        throw new InvalidOperationException(
            $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
    }

    if (buffer.Length < frameLength)
    {
        return false;
    }

    payloadOffset = lengthPrefixOffset + _lengthPrefixSizeBytes;
    var payloadSpan = buffer.Slice(payloadOffset, payloadLength);

    if (_checksumSizeBytes > 0)
    {
        var checksumOffset = payloadOffset + payloadLength;
        var expected = ReadChecksum(buffer.Slice(checksumOffset, _checksumSizeBytes));
        var actual = ComputeChecksum(GetChecksumCoverage(buffer[..checksumOffset], payloadSpan));
        if (expected != actual)
        {
            throw new InvalidOperationException("Frame checksum is invalid.");
        }
    }

    return true;
}
```

- [ ] **Step 7: Run focused protocol tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests"
```

Expected: all selected protocol tests pass.

- [ ] **Step 8: Commit this task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add src/CommLib.Domain/Protocol/ProtocolDecodeResult.cs src/CommLib.Domain/Protocol/IZeroCopyProtocol.cs src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs src/CommLib.Infrastructure/Protocol/BinaryFrameProtocol.cs tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs tests/CommLib.Infrastructure.Tests/BinaryFrameProtocolTests.cs
git commit -m "perf(protocol): add zero-copy decode path"
```

---

### Task 2: Add Memory-Based Message Decode Path

**Files:**

- Modify: `src/CommLib.Infrastructure/Protocol/MessageFrameDecoder.cs`
- Test: `tests/CommLib.Infrastructure.Tests/MessageFrameDecoderTests.cs`

- [ ] **Step 1: Write the failing decoder fast-path test**

Add this test to `MessageFrameDecoderTests`:

```csharp
[Fact]
public void TryDecodeMemory_WithZeroCopyProtocol_UsesMemoryDecodePath()
{
    var protocol = new FakeZeroCopyProtocol(payloadOffset: 2, payloadLength: 3, bytesConsumed: 6);
    var serializer = new CapturingSerializer(new FakeMessage(7));
    var decoder = new MessageFrameDecoder(protocol, serializer);
    var frame = new byte[] { 0xAA, 0xBB, 0x10, 0x20, 0x30, 0xCC };

    var decoded = decoder.TryDecode(frame.AsMemory(), out var message, out var bytesConsumed);

    Assert.True(decoded);
    Assert.NotNull(message);
    Assert.Equal((ushort)7, message.MessageId);
    Assert.Equal(6, bytesConsumed);
    Assert.True(protocol.MemoryDecodeCalled);
    Assert.False(protocol.LegacyDecodeCalled);
    Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, serializer.LastPayload);
}
```

Add these helper types inside `MessageFrameDecoderTests`:

```csharp
private sealed class FakeZeroCopyProtocol : IZeroCopyProtocol
{
    private readonly int _payloadOffset;
    private readonly int _payloadLength;
    private readonly int _bytesConsumed;

    public FakeZeroCopyProtocol(int payloadOffset, int payloadLength, int bytesConsumed)
    {
        _payloadOffset = payloadOffset;
        _payloadLength = payloadLength;
        _bytesConsumed = bytesConsumed;
    }

    public string Name => "ZeroCopyFake";
    public bool MemoryDecodeCalled { get; private set; }
    public bool LegacyDecodeCalled { get; private set; }

    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        throw new NotSupportedException();
    }

    public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
    {
        LegacyDecodeCalled = true;
        payload = Array.Empty<byte>();
        bytesConsumed = 0;
        return false;
    }

    public bool TryDecode(ReadOnlyMemory<byte> buffer, out ProtocolDecodeResult result)
    {
        MemoryDecodeCalled = true;
        result = new ProtocolDecodeResult(buffer.Slice(_payloadOffset, _payloadLength), _bytesConsumed);
        return true;
    }
}

private sealed class CapturingSerializer : ISerializer
{
    private readonly IMessage _message;

    public CapturingSerializer(IMessage message)
    {
        _message = message;
    }

    public byte[] LastPayload { get; private set; } = Array.Empty<byte>();

    public byte[] Serialize(IMessage message)
    {
        throw new NotSupportedException();
    }

    public IMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        LastPayload = payload.ToArray();
        return _message;
    }
}
```

- [ ] **Step 2: Run decoder tests and confirm the expected compile failure**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter MessageFrameDecoderTests
```

Expected: build fails because `MessageFrameDecoder` has no `ReadOnlyMemory<byte>` overload.

- [ ] **Step 3: Add the memory overload**

Add this method to `src/CommLib.Infrastructure/Protocol/MessageFrameDecoder.cs`:

```csharp
public bool TryDecode(ReadOnlyMemory<byte> buffer, out IMessage? message, out int bytesConsumed)
{
    message = null;
    bytesConsumed = 0;

    if (_protocol is IZeroCopyProtocol zeroCopyProtocol)
    {
        if (!zeroCopyProtocol.TryDecode(buffer, out var result))
        {
            return false;
        }

        message = _serializer.Deserialize(result.Payload.Span);
        bytesConsumed = result.BytesConsumed;
        return true;
    }

    return TryDecode(buffer.Span, out message, out bytesConsumed);
}
```

Keep the existing `ReadOnlySpan<byte>` overload unchanged so legacy direct callers preserve current behavior.

- [ ] **Step 4: Run decoder tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter MessageFrameDecoderTests
```

Expected: all selected decoder tests pass.

- [ ] **Step 5: Commit this task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add src/CommLib.Infrastructure/Protocol/MessageFrameDecoder.cs tests/CommLib.Infrastructure.Tests/MessageFrameDecoderTests.cs
git commit -m "perf(protocol): decode messages from frame memory"
```

---

### Task 3: Replace Receiver Pending Array Churn With a Windowed Buffer

**Files:**

- Modify: `src/CommLib.Infrastructure/Transport/TransportMessageReceiver.cs`
- Test: `tests/CommLib.Infrastructure.Tests/TransportMessageReceiverTests.cs`

- [ ] **Step 1: Write the fragmented-plus-buffered-remainder test**

Add this test to `TransportMessageReceiverTests`:

```csharp
[Fact]
public async Task ReceiveAsync_FragmentedFirstFrameWithSecondFrameRemainder_ReturnsBothMessages()
{
    var encoder = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol());
    var firstFrame = encoder.Encode(new MessageModel(42));
    var secondFrame = encoder.Encode(new MessageModel(43));
    var secondChunk = new byte[(firstFrame.Length - 3) + secondFrame.Length];
    firstFrame.AsSpan(3).CopyTo(secondChunk);
    secondFrame.CopyTo(secondChunk.AsSpan(firstFrame.Length - 3));

    var transport = new FakeTransport(firstFrame[..3], secondChunk);
    var receiver = new TransportMessageReceiver(
        new MessageFrameDecoder(new LengthPrefixedProtocol(), new NoOpSerializer()),
        transport);

    var firstMessage = await receiver.ReceiveAsync();
    var secondMessage = await receiver.ReceiveAsync();

    Assert.Equal((ushort)42, firstMessage.MessageId);
    Assert.Equal((ushort)43, secondMessage.MessageId);
    Assert.Equal(2, transport.ReceiveCount);
}
```

- [ ] **Step 2: Run receiver tests before the refactor**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter TransportMessageReceiverTests
```

Expected: tests pass before the implementation refactor. This locks behavior so the buffer rewrite does not change observable receive semantics.

- [ ] **Step 3: Replace the pending buffer fields**

In `src/CommLib.Infrastructure/Transport/TransportMessageReceiver.cs`, replace:

```csharp
private byte[] _pendingBuffer = Array.Empty<byte>();
```

with:

```csharp
private const int InitialPendingBufferSize = 256;
private byte[] _pendingBuffer = Array.Empty<byte>();
private int _pendingOffset;
private int _pendingLength;
```

- [ ] **Step 4: Make pending decode use the memory overload**

Replace `TryDecodePending` with:

```csharp
private bool TryDecodePending(out IMessage message)
{
    if (_pendingLength == 0 ||
        !_frameDecoder.TryDecode(_pendingBuffer.AsMemory(_pendingOffset, _pendingLength), out var decodedMessage, out var bytesConsumed) ||
        decodedMessage is null)
    {
        message = null!;
        return false;
    }

    AdvancePending(bytesConsumed);
    message = decodedMessage;
    return true;
}
```

Add this helper:

```csharp
private void AdvancePending(int bytesConsumed)
{
    _pendingOffset += bytesConsumed;
    _pendingLength -= bytesConsumed;

    if (_pendingLength == 0)
    {
        _pendingOffset = 0;
        return;
    }

    if (_pendingOffset > _pendingBuffer.Length / 2)
    {
        CompactPendingBuffer();
    }
}
```

- [ ] **Step 5: Replace append-and-merge with reusable capacity management**

Replace `AppendChunk` with:

```csharp
private void AppendChunk(ReadOnlySpan<byte> chunk)
{
    if (chunk.IsEmpty)
    {
        return;
    }

    EnsureWritableCapacity(chunk.Length);
    chunk.CopyTo(_pendingBuffer.AsSpan(_pendingOffset + _pendingLength, chunk.Length));
    _pendingLength += chunk.Length;
}
```

Add these helpers:

```csharp
private void EnsureWritableCapacity(int additionalLength)
{
    if (_pendingBuffer.Length == 0)
    {
        _pendingBuffer = new byte[Math.Max(InitialPendingBufferSize, additionalLength)];
        _pendingOffset = 0;
        return;
    }

    var requiredWithCurrentOffset = _pendingOffset + _pendingLength + additionalLength;
    if (requiredWithCurrentOffset <= _pendingBuffer.Length)
    {
        return;
    }

    var requiredCompactedLength = _pendingLength + additionalLength;
    if (requiredCompactedLength <= _pendingBuffer.Length)
    {
        CompactPendingBuffer();
        return;
    }

    var nextLength = Math.Max(requiredCompactedLength, _pendingBuffer.Length * 2);
    var next = new byte[nextLength];
    _pendingBuffer.AsSpan(_pendingOffset, _pendingLength).CopyTo(next);
    _pendingBuffer = next;
    _pendingOffset = 0;
}

private void CompactPendingBuffer()
{
    if (_pendingLength == 0)
    {
        _pendingOffset = 0;
        return;
    }

    _pendingBuffer.AsSpan(_pendingOffset, _pendingLength).CopyTo(_pendingBuffer);
    _pendingOffset = 0;
}
```

- [ ] **Step 6: Keep direct `TryDecode(ReadOnlySpan<byte>)` compatibility**

Leave this public method as the compatibility path:

```csharp
public bool TryDecode(ReadOnlySpan<byte> buffer, out IMessage message, out int bytesConsumed)
{
    if (_frameDecoder.TryDecode(buffer, out var decodedMessage, out bytesConsumed) && decodedMessage is not null)
    {
        message = decodedMessage;
        return true;
    }

    message = null!;
    return false;
}
```

This method still uses the legacy span path because a span-only caller did not provide memory that can be returned as a stable slice.

- [ ] **Step 7: Run receiver tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter TransportMessageReceiverTests
```

Expected: all selected receiver tests pass.

- [ ] **Step 8: Commit this task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add src/CommLib.Infrastructure/Transport/TransportMessageReceiver.cs tests/CommLib.Infrastructure.Tests/TransportMessageReceiverTests.cs
git commit -m "perf(transport): reuse pending receive buffer"
```

---

### Task 4: Add Span-Based Outbound Writer Contracts

**Files:**

- Create: `src/CommLib.Domain/Protocol/ProtocolFrameLayout.cs`
- Create: `src/CommLib.Domain/Protocol/IFrameEncodingProtocol.cs`
- Create: `src/CommLib.Domain/Protocol/ISpanSerializer.cs`
- Modify: `src/CommLib.Infrastructure/Protocol/MessageFrameEncoder.cs`
- Test: `tests/CommLib.Infrastructure.Tests/MessageFrameEncoderTests.cs`

- [ ] **Step 1: Write the failing encoder fast-path test**

Add this test to `MessageFrameEncoderTests`:

```csharp
[Fact]
public void Encode_WhenSerializerAndProtocolSupportSpanWriters_WritesPayloadDirectlyIntoFinalFrame()
{
    var serializer = new SpanFakeSerializer(new byte[] { 0x10, 0x20, 0x30 });
    var protocol = new SpanFakeProtocol();
    var encoder = new MessageFrameEncoder(serializer, protocol);

    var frame = encoder.Encode(new FakeMessage(7));

    Assert.Equal(new byte[] { 0xAA, 0x03, 0x10, 0x20, 0x30, 0x55 }, frame);
    Assert.True(serializer.SpanSerializeCalled);
    Assert.False(serializer.LegacySerializeCalled);
    Assert.True(protocol.FrameWriterCalled);
    Assert.False(protocol.LegacyEncodeCalled);
}
```

Add these helper types inside `MessageFrameEncoderTests`:

```csharp
private sealed class SpanFakeSerializer : ISpanSerializer
{
    private readonly byte[] _payload;

    public SpanFakeSerializer(byte[] payload)
    {
        _payload = payload;
    }

    public bool SpanSerializeCalled { get; private set; }
    public bool LegacySerializeCalled { get; private set; }

    public int GetSerializedLength(IMessage message)
    {
        return _payload.Length;
    }

    public void Serialize(IMessage message, Span<byte> destination)
    {
        SpanSerializeCalled = true;
        _payload.CopyTo(destination);
    }

    public byte[] Serialize(IMessage message)
    {
        LegacySerializeCalled = true;
        return _payload;
    }

    public IMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        throw new NotSupportedException();
    }
}

private sealed class SpanFakeProtocol : IFrameEncodingProtocol
{
    public string Name => "SpanFake";
    public bool FrameWriterCalled { get; private set; }
    public bool LegacyEncodeCalled { get; private set; }

    public ProtocolFrameLayout CreateFrameLayout(int payloadLength)
    {
        return new ProtocolFrameLayout(FrameLength: payloadLength + 3, PayloadOffset: 2, PayloadLength: payloadLength);
    }

    public void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout)
    {
        FrameWriterCalled = true;
        frame[0] = 0xAA;
        frame[1] = checked((byte)layout.PayloadLength);
    }

    public void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout)
    {
        frame[layout.PayloadOffset + layout.PayloadLength] = 0x55;
    }

    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        LegacyEncodeCalled = true;
        return payload.ToArray();
    }

    public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
    {
        payload = Array.Empty<byte>();
        bytesConsumed = 0;
        return false;
    }
}
```

- [ ] **Step 2: Run encoder tests and confirm the expected compile failure**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter MessageFrameEncoderTests
```

Expected: build fails because `ISpanSerializer`, `IFrameEncodingProtocol`, and `ProtocolFrameLayout` do not exist.

- [ ] **Step 3: Add outbound writer contracts**

Create `src/CommLib.Domain/Protocol/ProtocolFrameLayout.cs`:

```csharp
namespace CommLib.Domain.Protocol;

/// <summary>
/// Describes where a serialized payload belongs inside a final encoded protocol frame.
/// </summary>
public readonly record struct ProtocolFrameLayout(int FrameLength, int PayloadOffset, int PayloadLength);
```

Create `src/CommLib.Domain/Protocol/IFrameEncodingProtocol.cs`:

```csharp
namespace CommLib.Domain.Protocol;

/// <summary>
/// Optional protocol contract for writing a frame around a payload that is serialized directly into the final frame buffer.
/// </summary>
public interface IFrameEncodingProtocol : IProtocol
{
    ProtocolFrameLayout CreateFrameLayout(int payloadLength);

    void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout);

    void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout);
}
```

Create `src/CommLib.Domain/Protocol/ISpanSerializer.cs`:

```csharp
using CommLib.Domain.Messaging;

namespace CommLib.Domain.Protocol;

/// <summary>
/// Optional serializer contract for writing serialized payload bytes into caller-provided memory.
/// </summary>
public interface ISpanSerializer : ISerializer
{
    int GetSerializedLength(IMessage message);

    void Serialize(IMessage message, Span<byte> destination);
}
```

- [ ] **Step 4: Add the span writer fast path to `MessageFrameEncoder`**

Replace `Encode(IMessage message)` in `src/CommLib.Infrastructure/Protocol/MessageFrameEncoder.cs` with:

```csharp
public byte[] Encode(IMessage message)
{
    if (_serializer is ISpanSerializer spanSerializer &&
        _protocol is IFrameEncodingProtocol frameProtocol)
    {
        var payloadLength = spanSerializer.GetSerializedLength(message);
        var layout = frameProtocol.CreateFrameLayout(payloadLength);
        var frame = new byte[layout.FrameLength];
        frameProtocol.WriteFramePrefix(frame, layout);
        spanSerializer.Serialize(message, frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
        frameProtocol.WriteFrameSuffix(frame, layout);
        return frame;
    }

    var payload = _serializer.Serialize(message);
    return _protocol.Encode(payload);
}
```

- [ ] **Step 5: Run encoder tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter MessageFrameEncoderTests
```

Expected: all selected encoder tests pass.

- [ ] **Step 6: Commit this task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add src/CommLib.Domain/Protocol/ProtocolFrameLayout.cs src/CommLib.Domain/Protocol/IFrameEncodingProtocol.cs src/CommLib.Domain/Protocol/ISpanSerializer.cs src/CommLib.Infrastructure/Protocol/MessageFrameEncoder.cs tests/CommLib.Infrastructure.Tests/MessageFrameEncoderTests.cs
git commit -m "perf(protocol): add span-based frame encoding path"
```

---

### Task 5: Implement Frame Writers in Built-In Protocols

**Files:**

- Modify: `src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs`
- Modify: `src/CommLib.Infrastructure/Protocol/BinaryFrameProtocol.cs`
- Test: `tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs`
- Test: `tests/CommLib.Infrastructure.Tests/BinaryFrameProtocolTests.cs`

- [ ] **Step 1: Add frame-writer tests for `LengthPrefixedProtocol`**

Add these tests to `LengthPrefixedProtocolTests`:

```csharp
[Fact]
public void FrameWriter_WithPayloadSlot_ReturnsBigEndianLengthPrefixedFrame()
{
    var protocol = Assert.IsAssignableFrom<IFrameEncodingProtocol>(new LengthPrefixedProtocol());
    var layout = protocol.CreateFrameLayout(payloadLength: 3);
    var frame = new byte[layout.FrameLength];

    protocol.WriteFramePrefix(frame, layout);
    new byte[] { 0x10, 0x20, 0x30 }.CopyTo(frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
    protocol.WriteFrameSuffix(frame, layout);

    Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 }, frame);
}

[Fact]
public void CreateFrameLayout_FrameLongerThanConfiguredMaximum_Throws()
{
    var protocol = Assert.IsAssignableFrom<IFrameEncodingProtocol>(new LengthPrefixedProtocol(6));

    var exception = Assert.Throws<InvalidOperationException>(() => protocol.CreateFrameLayout(payloadLength: 3));

    Assert.Equal("Frame length 7 exceeds the configured maximum of 6.", exception.Message);
}
```

- [ ] **Step 2: Add frame-writer tests for `BinaryFrameProtocol`**

Add this test to `BinaryFrameProtocolTests`:

```csharp
[Fact]
public void FrameWriter_WithStartLengthAndCrc16Modbus_ReturnsConfiguredFrame()
{
    var protocol = Assert.IsAssignableFrom<IFrameEncodingProtocol>(CreateProtocol());
    var layout = protocol.CreateFrameLayout(payloadLength: 3);
    var frame = new byte[layout.FrameLength];

    protocol.WriteFramePrefix(frame, layout);
    new byte[] { 0x10, 0x20, 0x30 }.CopyTo(frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
    protocol.WriteFrameSuffix(frame, layout);

    Assert.Equal(
        new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x10, 0x20, 0x30, 0x05, 0x5A },
        frame);
}
```

- [ ] **Step 3: Run protocol tests and confirm the expected assertion failure**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests"
```

Expected: tests compile and fail because built-in protocols do not yet implement `IFrameEncodingProtocol`.

- [ ] **Step 4: Implement frame writer in `LengthPrefixedProtocol`**

Change the class declaration:

```csharp
public sealed class LengthPrefixedProtocol : IProtocol, IZeroCopyProtocol, IFrameEncodingProtocol
```

Replace `Encode(ReadOnlySpan<byte> payload)` with:

```csharp
public byte[] Encode(ReadOnlySpan<byte> payload)
{
    var layout = CreateFrameLayout(payload.Length);
    var frame = new byte[layout.FrameLength];
    WriteFramePrefix(frame, layout);
    payload.CopyTo(frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
    WriteFrameSuffix(frame, layout);
    return frame;
}
```

Add these methods:

```csharp
public ProtocolFrameLayout CreateFrameLayout(int payloadLength)
{
    if (payloadLength < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(payloadLength), payloadLength, "Payload length cannot be negative.");
    }

    var frameLength = checked(HeaderSize + payloadLength);
    if (frameLength > _maxFrameLength)
    {
        throw new InvalidOperationException(
            $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
    }

    return new ProtocolFrameLayout(frameLength, HeaderSize, payloadLength);
}

public void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout)
{
    BinaryPrimitives.WriteInt32BigEndian(frame[..HeaderSize], layout.PayloadLength);
}

public void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout)
{
}
```

- [ ] **Step 5: Implement frame writer in `BinaryFrameProtocol`**

Change the class declaration:

```csharp
public sealed class BinaryFrameProtocol : IProtocol, IZeroCopyProtocol, IFrameEncodingProtocol
```

Replace `Encode(ReadOnlySpan<byte> payload)` with:

```csharp
public byte[] Encode(ReadOnlySpan<byte> payload)
{
    var layout = CreateFrameLayout(payload.Length);
    var frame = new byte[layout.FrameLength];
    WriteFramePrefix(frame, layout);
    payload.CopyTo(frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
    WriteFrameSuffix(frame, layout);
    return frame;
}
```

Add these methods:

```csharp
public ProtocolFrameLayout CreateFrameLayout(int payloadLength)
{
    EnsurePayloadLengthFitsPrefix(payloadLength);

    var frameLength = checked(_startBytes.Length + _lengthPrefixSizeBytes + payloadLength + _checksumSizeBytes);
    if (frameLength > _maxFrameLength)
    {
        throw new InvalidOperationException(
            $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
    }

    return new ProtocolFrameLayout(frameLength, _startBytes.Length + _lengthPrefixSizeBytes, payloadLength);
}

public void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout)
{
    _startBytes.AsSpan().CopyTo(frame);
    WriteLengthPrefix(frame.Slice(_startBytes.Length, _lengthPrefixSizeBytes), layout.PayloadLength);
}

public void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout)
{
    if (_checksumSizeBytes == 0)
    {
        return;
    }

    var checksumOffset = layout.PayloadOffset + layout.PayloadLength;
    var frameWithoutChecksum = frame[..checksumOffset];
    var payload = frame.Slice(layout.PayloadOffset, layout.PayloadLength);
    var checksum = ComputeChecksum(GetChecksumCoverage(frameWithoutChecksum, payload));
    WriteChecksum(frame.Slice(checksumOffset, _checksumSizeBytes), checksum);
}
```

- [ ] **Step 6: Run protocol tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests"
```

Expected: all selected protocol tests pass.

- [ ] **Step 7: Commit this task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add src/CommLib.Infrastructure/Protocol/LengthPrefixedProtocol.cs src/CommLib.Infrastructure/Protocol/BinaryFrameProtocol.cs tests/CommLib.Infrastructure.Tests/LengthPrefixedProtocolTests.cs tests/CommLib.Infrastructure.Tests/BinaryFrameProtocolTests.cs
git commit -m "perf(protocol): write built-in frames by span"
```

---

### Task 6: Implement Span Serializers for Built-In Serializers

**Files:**

- Modify: `src/CommLib.Infrastructure/Protocol/RawHexSerializer.cs`
- Modify: `src/CommLib.Infrastructure/Protocol/NoOpSerializer.cs`
- Test: `tests/CommLib.Infrastructure.Tests/RawHexSerializerTests.cs`
- Test: `tests/CommLib.Infrastructure.Tests/NoOpSerializerTests.cs`

- [ ] **Step 1: Add span serializer tests for `RawHexSerializer`**

Add this test to `RawHexSerializerTests`:

```csharp
[Fact]
public void SpanSerialize_BinaryMessage_WritesSamePayloadAsLegacySerialize()
{
    var serializer = Assert.IsAssignableFrom<ISpanSerializer>(new RawHexSerializer());
    var message = new BinaryMessageModel(12, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
    var destination = new byte[serializer.GetSerializedLength(message)];

    serializer.Serialize(message, destination);

    Assert.Equal(new RawHexSerializer().Serialize(message), destination);
}
```

Add this `using` statement:

```csharp
using CommLib.Domain.Protocol;
```

- [ ] **Step 2: Add span serializer tests for `NoOpSerializer`**

Add this test to `NoOpSerializerTests`:

```csharp
[Fact]
public void SpanSerialize_MessageWithBody_WritesSamePayloadAsLegacySerialize()
{
    var serializer = Assert.IsAssignableFrom<ISpanSerializer>(new NoOpSerializer());
    var message = new FakeBodyMessage(12, "hello|world");
    var destination = new byte[serializer.GetSerializedLength(message)];

    serializer.Serialize(message, destination);

    Assert.Equal(new NoOpSerializer().Serialize(message), destination);
}
```

Add this `using` statement:

```csharp
using CommLib.Domain.Protocol;
```

- [ ] **Step 3: Run serializer tests and confirm the expected assertion failure**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "RawHexSerializerTests|NoOpSerializerTests"
```

Expected: tests compile and fail because built-in serializers do not yet implement `ISpanSerializer`.

- [ ] **Step 4: Implement `ISpanSerializer` in `RawHexSerializer`**

Change the class declaration:

```csharp
public sealed class RawHexSerializer : ISerializer, ISpanSerializer
```

Replace `Serialize(IMessage message)` with:

```csharp
public byte[] Serialize(IMessage message)
{
    var serialized = new byte[GetSerializedLength(message)];
    Serialize(message, serialized);
    return serialized;
}
```

Add these methods:

```csharp
public int GetSerializedLength(IMessage message)
{
    return Encoding.ASCII.GetByteCount(CreateHeaderText(message)) + GetPayloadLength(message);
}

public void Serialize(IMessage message, Span<byte> destination)
{
    var headerText = CreateHeaderText(message);
    var headerLength = Encoding.ASCII.GetBytes(headerText, destination);
    WritePayload(message, destination[headerLength..]);
}
```

Replace `CreateHeader` with:

```csharp
private static string CreateHeaderText(IMessage message)
{
    return message switch
    {
        IResponseMessage response => string.Create(
            CultureInfo.InvariantCulture,
            $"response|{message.MessageId}|{response.CorrelationId:D}|{(response.IsSuccess ? "1" : "0")}|"),
        IRequestMessage request => string.Create(
            CultureInfo.InvariantCulture,
            $"request|{message.MessageId}|{request.CorrelationId:D}|"),
        _ => string.Create(
            CultureInfo.InvariantCulture,
            $"message|{message.MessageId}|")
    };
}
```

Add these helpers:

```csharp
private static int GetPayloadLength(IMessage message)
{
    if (message is IBinaryMessagePayload binaryPayload)
    {
        return binaryPayload.Payload.Length;
    }

    if (message is IMessageBody bodyMessage)
    {
        return ParseBodyPayload(bodyMessage).Length;
    }

    return 0;
}

private static void WritePayload(IMessage message, Span<byte> destination)
{
    if (message is IBinaryMessagePayload binaryPayload)
    {
        binaryPayload.Payload.Span.CopyTo(destination);
        return;
    }

    if (message is IMessageBody bodyMessage)
    {
        ParseBodyPayload(bodyMessage).AsSpan().CopyTo(destination);
        return;
    }
}

private static byte[] ParseBodyPayload(IMessageBody bodyMessage)
{
    try
    {
        return HexPayloadParser.Parse(bodyMessage.Body);
    }
    catch (FormatException exception)
    {
        throw new InvalidOperationException("Message body must be valid hexadecimal text.", exception);
    }
}
```

Remove the old `CreateHeader` and `ExtractPayload` helpers after the replacement compiles.

- [ ] **Step 5: Implement `ISpanSerializer` in `NoOpSerializer`**

Change the class declaration:

```csharp
public sealed class NoOpSerializer : ISerializer, ISpanSerializer
```

Replace `Serialize(IMessage message)` with:

```csharp
public byte[] Serialize(IMessage message)
{
    return Encoding.UTF8.GetBytes(CreatePayloadText(message));
}
```

Add these methods:

```csharp
public int GetSerializedLength(IMessage message)
{
    return Encoding.UTF8.GetByteCount(CreatePayloadText(message));
}

public void Serialize(IMessage message, Span<byte> destination)
{
    var written = Encoding.UTF8.GetBytes(CreatePayloadText(message), destination);
    if (written != destination.Length)
    {
        throw new InvalidOperationException("Serialized payload length did not match the destination length.");
    }
}
```

Extract the current string-building switch into:

```csharp
private static string CreatePayloadText(IMessage message)
{
    var encodedBody = TryEncodeBody(message);
    return message switch
    {
        IResponseMessage response => string.Join(
            Separator,
            "response",
            message.MessageId.ToString(CultureInfo.InvariantCulture),
            response.CorrelationId.ToString("D", CultureInfo.InvariantCulture),
            response.IsSuccess ? "1" : "0",
            encodedBody),
        IRequestMessage request => string.Join(
            Separator,
            "request",
            message.MessageId.ToString(CultureInfo.InvariantCulture),
            request.CorrelationId.ToString("D", CultureInfo.InvariantCulture),
            encodedBody),
        _ => string.Join(
            Separator,
            "message",
            message.MessageId.ToString(CultureInfo.InvariantCulture),
            encodedBody)
    };
}
```

- [ ] **Step 6: Run serializer tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "RawHexSerializerTests|NoOpSerializerTests"
```

Expected: all selected serializer tests pass.

- [ ] **Step 7: Commit this task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add src/CommLib.Infrastructure/Protocol/RawHexSerializer.cs src/CommLib.Infrastructure/Protocol/NoOpSerializer.cs tests/CommLib.Infrastructure.Tests/RawHexSerializerTests.cs tests/CommLib.Infrastructure.Tests/NoOpSerializerTests.cs
git commit -m "perf(protocol): serialize built-in payloads by span"
```

---

### Task 7: Integration Verification and Public Contract Review

**Files:**

- Modify: `README.md`
- Modify: `docs/quick-start.md`

- [ ] **Step 1: Run focused infrastructure tests for the full span pipeline**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "LengthPrefixedProtocolTests|BinaryFrameProtocolTests|MessageFrameDecoderTests|MessageFrameEncoderTests|TransportMessageReceiverTests|RawHexSerializerTests|NoOpSerializerTests"
```

Expected: all selected tests pass.

- [ ] **Step 2: Run full infrastructure tests**

Run:

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore
```

Expected: all infrastructure tests pass.

- [ ] **Step 3: Run full unit tests**

Run:

```powershell
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore
```

Expected: all unit tests pass.

- [ ] **Step 4: Build the full solution**

Run:

```powershell
dotnet build commlib-codex-full.sln --configuration Release --no-restore
```

Expected: build succeeds with 0 errors.

- [ ] **Step 5: Update README protocol notes**

In `README.md`, add one concise note near the protocol/serialization overview:

```markdown
Built-in frame protocols and serializers use additive `ReadOnlyMemory<byte>` / `Span<byte>` fast paths internally where possible. Existing custom `IProtocol` and `ISerializer` implementations remain supported through the original array-returning compatibility methods.
```

- [ ] **Step 6: Update quick-start protocol notes**

In `docs/quick-start.md`, add one concise note near the `LengthPrefixed` / `BinaryFrame` section:

```markdown
For high-throughput binary devices, prefer the built-in `LengthPrefixed` or `BinaryFrame` protocols plus `RawHex`; those combinations now use the span-based fast path for receive decoding and outbound frame construction while preserving the older extension interfaces.
```

- [ ] **Step 7: Review the changed public API surface**

Run:

```powershell
rg -n "public interface IZeroCopyProtocol|public interface IFrameEncodingProtocol|public interface ISpanSerializer|public readonly record struct ProtocolDecodeResult|public readonly record struct ProtocolFrameLayout" src/CommLib.Domain -S
```

Expected output includes only the five additive public domain types created in this plan.

- [ ] **Step 8: Commit the final verification/docs task in an isolated clean branch or worktree**

Run only when the branch/worktree contains this task's changes and no unrelated local edits:

```powershell
git add README.md docs/quick-start.md
git commit -m "docs(protocol): document span-based protocol fast paths"
```

---

## Failure Modes to Watch During Implementation

- **Trigger:** `TransportMessageReceiver` advances the pending offset incorrectly after decoding a frame.
  **Impact:** the next buffered frame starts at the wrong byte and fails decode or returns a corrupted message.
  **Detection:** `ReceiveAsync_FragmentedFirstFrameWithSecondFrameRemainder_ReturnsBothMessages` and `ReceiveAsync_MultipleFramesInSingleChunk_ReusesBufferedRemainder`.
  **Mitigation:** advance by `bytesConsumed`, compact only after preserving `_pendingLength`, and reset `_pendingOffset` only when `_pendingLength == 0`.

- **Trigger:** `BinaryFrameProtocol.WriteFrameSuffix` computes checksum before payload bytes are written.
  **Impact:** outbound `BinaryFrame` frames fail checksum validation on peer devices.
  **Detection:** `FrameWriter_WithStartLengthAndCrc16Modbus_ReturnsConfiguredFrame`.
  **Mitigation:** `MessageFrameEncoder` must call `WriteFramePrefix`, then serializer span write, then `WriteFrameSuffix` in that order.

- **Trigger:** a custom protocol implements only `IProtocol`.
  **Impact:** a breaking change would prevent existing custom protocol extensions from compiling.
  **Detection:** existing fake-protocol tests and the compatibility rules in this plan.
  **Mitigation:** keep new contracts optional and keep all existing interface members unchanged.

## Final Verification Checklist

- [ ] Focused span pipeline infrastructure test command passes.
- [ ] Full infrastructure test project passes.
- [ ] Full unit test project passes.
- [ ] Full solution Release build passes.
- [ ] README and quick-start describe fast paths without claiming full zero-copy ownership or a full Modbus stack.
- [ ] Existing custom `IProtocol` / `ISerializer` compatibility remains intact.
