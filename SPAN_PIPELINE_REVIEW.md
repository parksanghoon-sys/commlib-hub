# Span 기반 최소 복사 파이프라인 검토

> 검토일: 2026-05-20
> 대상: 최근 구현된 span/zero-copy 관련 신규·수정 파일
> 연관 문서: [DESIGN_REVIEW.md](DESIGN_REVIEW.md), [VERIFICATION_REPORT.md](VERIFICATION_REPORT.md)

---

## 1. 구현 범위 요약

| 파일 | 상태 | 역할 |
|------|------|------|
| `Domain/Protocol/IZeroCopyProtocol.cs` | 신규 | payload를 복사 없이 `ReadOnlyMemory<byte>` slice로 반환하는 선택적 계약 |
| `Domain/Protocol/IFrameEncodingProtocol.cs` | 신규 | serializer가 최종 frame buffer에 직접 쓸 수 있게 하는 선택적 계약 |
| `Domain/Protocol/ISpanSerializer.cs` | 신규 | caller-provided `Span<byte>`에 직렬화하는 선택적 serializer 계약 |
| `Domain/Protocol/ProtocolDecodeResult.cs` | 신규 | `ReadOnlyMemory<byte> Payload + int BytesConsumed` 값 타입 |
| `Domain/Protocol/ProtocolFrameLayout.cs` | 신규 | `FrameLength + PayloadOffset + PayloadLength` 값 타입 |
| `Infrastructure/Protocol/BinaryFrameProtocol.cs` | 신규 | start bytes + length prefix + CRC16 조합 범용 binary frame protocol |
| `Infrastructure/Protocol/LengthPrefixedProtocol.cs` | 수정 | `IZeroCopyProtocol + IFrameEncodingProtocol` 추가 구현 |
| `Infrastructure/Protocol/MessageFrameEncoder.cs` | 수정 | `ISpanSerializer + IFrameEncodingProtocol` fast path 추가 |
| `Infrastructure/Protocol/MessageFrameDecoder.cs` | 수정 | `IZeroCopyProtocol` fast path 추가 |
| `Infrastructure/Protocol/NoOpSerializer.cs` | 수정 | `ISpanSerializer` 추가 구현 |
| `Infrastructure/Protocol/RawHexSerializer.cs` | 수정 | `ISpanSerializer` 추가 구현 |
| `Domain/Messaging/BitFieldDefinition.cs` | 수정 | `FromByteBits(name, byteIndex, startBit, endBit)` factory 추가 |
| `Domain/Messaging/BitFieldCodec.cs` | 수정 | `ReadUnsigned<T>` / `ReadSigned<T>` generic 오버로드 추가 |
| `Domain/Configuration/BinaryFrameOptions.cs` | 신규 | BinaryFrame 설정 모델 |
| `Domain/Configuration/ProtocolTypes.cs` | 신규 | 프로토콜 타입 문자열 상수 |
| `Infrastructure/Factories/ProtocolFactory.cs` | 수정 | `BinaryFrame` 분기 추가 |

---

## 2. 설계 판단 — 잘된 부분

### 2-1. 기존 계약을 깨지 않는 additive 설계

`IProtocol`과 `ISerializer`는 전혀 변경되지 않았습니다. 세 인터페이스(`IZeroCopyProtocol`, `IFrameEncodingProtocol`, `ISpanSerializer`)는 모두 선택적 확장이며, `MessageFrameEncoder`와 `MessageFrameDecoder`는 런타임 `is` 검사로 fast path를 골라씁니다.

```csharp
// MessageFrameEncoder.Encode — fast path
if (_serializer is ISpanSerializer spanSerializer &&
    _protocol is IFrameEncodingProtocol frameProtocol)
{
    var layout = frameProtocol.CreateFrameLayout(payloadLength);
    var frame = new byte[layout.FrameLength];
    frameProtocol.WriteFramePrefix(frame, layout);
    spanSerializer.Serialize(message, frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
    frameProtocol.WriteFrameSuffix(frame, layout);
    return frame;
}
```

커스텀 `IProtocol` 구현체는 새 인터페이스를 구현하지 않아도 기존 경로로 계속 동작합니다.

### 2-2. `ProtocolDecodeResult`, `ProtocolFrameLayout`이 `readonly record struct`

힙 할당 없는 값 타입입니다. hot decode/encode 경로에서 불필요한 GC 압력이 없습니다.

### 2-3. 두 프로토콜 모두 3개 인터페이스를 완전 구현

`LengthPrefixedProtocol`과 `BinaryFrameProtocol` 모두 `IProtocol + IZeroCopyProtocol + IFrameEncodingProtocol`을 구현합니다. 기존 프로토콜도 fast path 혜택을 받습니다.

### 2-4. zero-copy 테스트 품질이 높음

```csharp
// BinaryFrameProtocolTests.cs:68-71
Assert.True(MemoryMarshal.TryGetArray(result.Payload, out var segment));
Assert.Same(frame, segment.Array);   // 원본 배열 동일성 검증
Assert.Equal(4, segment.Offset);     // slice 위치 검증
Assert.Equal(3, segment.Count);      // slice 길이 검증
```

payload가 새 배열 복사본이 아닌 원본 버퍼의 slice임을 `MemoryMarshal`로 직접 검증합니다.

### 2-5. CRC 순서 보호 주석

```csharp
// checksum이 payload를 포함할 수 있으므로 prefix -> payload -> suffix 순서를 반드시 유지합니다.
frameProtocol.WriteFramePrefix(frame, layout);
spanSerializer.Serialize(message, frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
frameProtocol.WriteFrameSuffix(frame, layout);
```

`FrameWithoutChecksum` coverage 모드에서 prefix 이후 payload가 이미 frame에 기록된 상태에서 CRC를 계산해야 한다는 제약을 주석으로 명시했습니다.

### 2-6. `IBinaryInteger<T>` constraint를 활용한 generic 오버로드

```csharp
public static T ReadUnsigned<T>(ReadOnlySpan<byte> payload, int byteIndex, int startBit, int endBit)
    where T : unmanaged, IBinaryInteger<T>
```

`T.CreateChecked(value)`로 오버플로를 명시적 예외로 전환합니다. .NET 7+ 숫자 인터페이스를 올바르게 활용했습니다.

---

## 3. 설계 문제점

### 문제 A — `ProtocolFactory`의 dead null-coalescing

**위치**: `src/CommLib.Infrastructure/Factories/ProtocolFactory.cs:26`

```csharp
return new BinaryFrameProtocol(options.BinaryFrame ?? new BinaryFrameOptions(), options.MaxFrameLength);
```

`ProtocolOptions.BinaryFrame`은 이미 `= new()` 기본값을 가지므로 `??` 조건은 `IConfiguration.Bind` 경로에서 절대 활성화되지 않습니다. `BinaryFrameProtocol` 생성자 내부의 `LengthPrefix ?? ...` / `Checksum ?? ...` 도 동일하게 중복입니다.

단, JSON 역직렬화 경로(예: `Newtonsoft.Json`)에서는 실제로 null이 될 수 있으므로 생성자 내부의 방어 코드는 수용 가능합니다. `ProtocolFactory`의 `??`만 제거해도 충분합니다.

---

### 문제 B — `WriteFramePrefix` / `WriteFrameSuffix`가 public이지만 순서 제약 보호가 없음

**위치**: `BinaryFrameProtocol.cs:103, 114`

`IFrameEncodingProtocol` 계약이 "prefix → payload → suffix" 순서를 요구하지만, 인터페이스나 구현에 순서 검증이 없습니다. 잘못된 순서로 호출하면 CRC가 silently 틀립니다.

```csharp
// 실수 가능한 호출 순서
protocol.WriteFrameSuffix(frame, layout);   // payload 없이 호출 → CRC 오염
spanSerializer.Serialize(message, frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
protocol.WriteFramePrefix(frame, layout);
```

인터페이스 XML 문서에 순서 제약이 명시되어 있지 않습니다.

**개선 방향**: `IFrameEncodingProtocol` XML 주석에 호출 순서를 명시.

---

### 문제 C — CRC16 Modbus 구현이 bit-by-bit 루프 (성능)

**위치**: `BinaryFrameProtocol.cs:371-386`

```csharp
private static ushort ComputeChecksum(ReadOnlySpan<byte> bytes)
{
    ushort crc = 0xFFFF;
    foreach (var current in bytes)
    {
        crc ^= current;
        for (var bit = 0; bit < 8; bit++)  // byte당 8 iteration
        {
            crc = (crc & 0x0001) != 0
                ? (ushort)((crc >> 1) ^ 0xA001)
                : (ushort)(crc >> 1);
        }
    }
    return crc;
}
```

표준 구현은 256-entry 룩업 테이블로 byte당 1 iteration에 처리합니다. 현재 구현은 8배 느립니다. 장치 통신 특성상 payload가 수 KB 이하로 작아 실용적 문제는 없지만, 처리량 요구가 증가하면 교체 필요.

---

### 문제 D — `MessageFrameEncoder` fast path 진입 여부를 관측할 수 없음

**위치**: `MessageFrameEncoder.cs:32-44`

```csharp
if (_serializer is ISpanSerializer spanSerializer &&
    _protocol is IFrameEncodingProtocol frameProtocol)
{
    // fast path
}
// fallback: 중간 payload 배열 할당
```

어느 경로가 실행되는지 외부에서 알 수 없습니다. 성능 디버깅 시 `IConnectionEventSink`가 없으므로, 의도치 않게 slow path를 타고 있어도 발견하기 어렵습니다.

---

### 문제 E — 테스트 커버리지 갭: `MessageFrameEncoder` partial fast path

`ISpanSerializer`만 있고 `IFrameEncodingProtocol`이 없는 경우, 또는 반대의 경우 → fallback. 이 조합은 테스트되지 않았습니다. 현재는 두 인터페이스 모두 구현한 경우(fast path)와 둘 다 없는 경우(legacy path)만 검증됩니다.

---

### 문제 F — `FromByteBits`는 byte-local (단일 바이트 내) 범위만 지원

**위치**: `BitFieldDefinition.cs:17-47`

```csharp
if (startBit is < 0 or > 7) throw ...
if (endBit is < 0 or > 7) throw ...
```

byte 경계를 넘는 필드(예: 바이트 1의 비트 4부터 바이트 2의 비트 3까지)는 `FromByteBits`로 표현 불가합니다. 이 경우 `new BitFieldDefinition(name, bitOffset, bitLength)` 직접 생성자를 써야 하지만, 이 제약이 XML 문서나 예외 메시지에 명시되지 않았습니다.

---

## 4. 종합 평가

| 항목 | 결과 |
|------|------|
| 기존 계약 하위 호환성 | ✅ 완전 유지 |
| zero-copy decode 구현 | ✅ `ReadOnlyMemory<byte>` slice 반환 검증됨 |
| zero-copy encode 구현 | ✅ 최종 frame에 직접 쓰기 검증됨 |
| 두 프로토콜 모두 fast path 지원 | ✅ |
| `NoOpSerializer`, `RawHexSerializer` fast path 지원 | ✅ |
| `BinaryFrameProtocol` CRC16/엔디안/start bytes 동작 | ✅ 테스트 검증됨 |
| `BitFieldCodec` generic 오버로드 | ✅ 테스트 검증됨 |
| CRC 성능 | ⚠️ bit-by-bit 루프 (문제 C) |
| fast path 관측 가능성 | ⚠️ 없음 (문제 D) |
| `WriteFramePrefix/Suffix` 순서 제약 명시 | ⚠️ 문서 누락 (문제 B) |
| `ProtocolFactory` dead null-coalescing | ⚠️ 사소한 코드 노이즈 (문제 A) |
| partial fast path 테스트 | ⚠️ 갭 존재 (문제 E) |
| `FromByteBits` 제약 명시 | ⚠️ 문서 누락 (문제 F) |

**판정: 기능적으로 올바르고 안전함. 즉각적 버그 없음.**
성능(문제 C)과 관측 가능성(문제 D)은 처리량 요구가 높아질 때 재검토 필요.
가장 낮은 비용으로 고칠 수 있는 것은 문제 A (dead null-coalescing 제거)와 문제 B (XML 문서 보완)입니다.

---

## 5. Codex 처리 결과

> 처리일: 2026-05-20
> 대상 커밋: `155545f`, `6ec74b3`, `8b46452`

### 5-1. 처리 완료

| 원문 항목 | 처리 결과 | 근거 |
|------|------|------|
| 문제 A - `ProtocolFactory` dead null-coalescing | 완료 | `ProtocolOptions.BinaryFrame`의 정상 설정 경로가 non-null 기본값을 가지므로 `ProtocolFactory`의 `options.BinaryFrame ?? new BinaryFrameOptions()`를 `options.BinaryFrame`으로 단순화했습니다. `BinaryFrameProtocol` 생성자 내부의 방어 코드는 외부 역직렬화나 수동 생성 경로를 고려해 유지했습니다. |
| 문제 B - `WriteFramePrefix` / `WriteFrameSuffix` 순서 제약 | 완료 | `IFrameEncodingProtocol` XML 주석에 `CreateFrameLayout -> WriteFramePrefix -> payload 기록 -> WriteFrameSuffix` 호출 순서를 명시했습니다. `BinaryFrameProtocol`에도 payload 기록 전후 호출 제약과 CRC가 payload를 읽는 이유를 보강했습니다. |
| 문제 E - partial fast path 테스트 갭 | 완료 | `MessageFrameEncoderTests`에 serializer만 `ISpanSerializer`를 구현한 경우와 protocol만 `IFrameEncodingProtocol`을 구현한 경우를 각각 추가했습니다. 두 경우 모두 legacy fallback 경로로 내려가는지 검증합니다. |
| 문제 F - `FromByteBits` byte-local 제약 명시 | 완료 | `BitFieldDefinition.FromByteBits(...)` XML 주석에 단일 byte 내부 범위만 표현한다는 점과 byte 경계를 넘는 필드는 `BitFieldDefinition(name, bitOffset, bitLength, endianness)` 생성자를 사용해야 한다는 점을 추가했습니다. |

### 5-2. 의도적으로 보류한 항목

| 원문 항목 | 현재 판정 | 이유 |
|------|------|------|
| 문제 C - CRC16 Modbus bit-by-bit 루프 | 보류 | 현재 구현은 정확성 테스트가 있고, payload 크기나 처리량 병목 증거 없이 테이블 기반 CRC로 바꾸는 것은 최적화 범위 확장입니다. 실제 처리량 요구나 profiling 결과가 생기면 별도 성능 slice로 다루는 것이 적절합니다. |
| 문제 D - fast path 관측 가능성 | 보류 | fast path 여부는 단위 테스트로 검증하고 있습니다. 운영 관측은 `IConnectionEventSink` 기반 production diagnostics slice와 함께 설계하는 편이 낫습니다. 지금 encoder에 관측용 public surface를 추가하면 API가 불필요하게 넓어질 수 있습니다. |

### 5-3. 검증 결과

다음 명령으로 후속 수정 범위를 검증했습니다.

```powershell
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "MessageFrameEncoderTests|ProtocolFactoryTests"
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore --filter BitFieldCodecTests
dotnet build commlib-codex-full.sln --configuration Release --no-restore
git diff --check
```

검증 결과:

- `MessageFrameEncoderTests|ProtocolFactoryTests`: 실패 0, 통과 9
- `BitFieldCodecTests`: 실패 0, 통과 16
- solution Release build: 경고 0, 오류 0
- `git diff --check`: whitespace 오류 없음. Windows line-ending 경고만 출력됨

### 5-4. 최종 판정

`SPAN_PIPELINE_REVIEW.md`의 즉시 처리 가치가 높은 항목은 반영 완료했습니다. 현재 남은 문제 C와 D는 결함이라기보다 성능 및 운영 관측성 개선 후보이며, 다음 production diagnostics 또는 성능 측정 요구가 생길 때 별도 작업으로 다루는 것이 맞습니다.
