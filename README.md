# CommLib
 
CommLib is a config-driven .NET communication library for device-facing applications.
It focuses on a small, explicit runtime contract for TCP, UDP, Serial, and Multicast
connections, plus a lightweight application/bootstrap layer and runnable examples.

## What It Supports Today

- Transports: `TcpClient`, `Udp`, `Serial`, `Multicast`
- Framing: `LengthPrefixed`, plus configurable `BinaryFrame` envelopes for start bytes,
  payload length prefixes, and optional CRC16/Modbus checksums
- Serializers: `AutoBinary`, `RawHex`
- Request/response session flow with pending-request limits and timeouts
- Config-driven device profiles via `DeviceProfileRaw -> DeviceProfile` mapping
- Dependency-injection-friendly runtime composition through `AddCommLibCore()`
- Example applications:
  - [Console sample](examples/CommLib.Examples.Console/README.md)
  - [WinUI Device Lab](examples/CommLib.Examples.WinUI/README.md)

Built-in frame protocols and serializers use additive `ReadOnlyMemory<byte>` / `Span<byte>` fast paths internally where possible. Existing custom `IProtocol` and `ISerializer` implementations remain supported through the original array-returning compatibility methods.

## Honest Contract Notes

This repository intentionally keeps the public contract narrower than the long-term roadmap.
Before using it in another project, these current boundaries are important:

- `ProtocolOptions` currently supports `LengthPrefixed` framing and the first
  configurable `BinaryFrame` envelope. It does not yet model a full Modbus register/function
  workflow by itself.
- `ReconnectOptions` currently controls transport-open retries inside the initial `ConnectAsync()` call only.
- A session whose background receive loop fails is treated as terminal until the caller reconnects it explicitly.
- The WinUI example is Windows-only and is provided as an operator/developer sample, not as a reusable UI package.

## Project Layout

```text
src/
  CommLib.Domain          Contracts and configuration models
  CommLib.Application     Validation, mapping, bootstrap, session use cases
  CommLib.Infrastructure  Transport, protocol, serializer, and connection runtime
  CommLib.Hosting         DI registration helpers

examples/
  CommLib.Examples.Console
  CommLib.Examples.WinUI

tests/
  CommLib.Unit.Tests
  CommLib.Infrastructure.Tests
```

## Quick Start

- Start with [docs/quick-start.md](docs/quick-start.md) for the fastest path through restore, test, example runs, and basic host/manual integration.
- Use [examples/CommLib.Examples.Console/README.md](examples/CommLib.Examples.Console/README.md) for loopback-friendly CLI demos.
- Use [examples/CommLib.Examples.WinUI/README.md](examples/CommLib.Examples.WinUI/README.md) for the Windows Device Lab workflow.

## Sample Configuration

The repository includes a root `appsettings.json` with example device definitions.
The overall shape looks like this:

```json
{
  "CommLib": {
    "Devices": [
      {
        "DeviceId": "radar-01",
        "DisplayName": "Radar Main TCP",
        "Enabled": true,
        "Transport": {
          "Type": "TcpClient",
          "Host": "192.168.10.50",
          "Port": 9001
        },
        "Protocol": {
          "Type": "LengthPrefixed",
          "MaxFrameLength": 65536
        },
        "Serializer": {
          "Type": "AutoBinary"
        },
        "Reconnect": {
          "Type": "ExponentialBackoff",
          "MaxAttempts": 10
        },
        "RequestResponse": {
          "DefaultTimeoutMs": 2000,
          "MaxPendingRequests": 100
        }
      }
    ]
  }
}
```

The `Reconnect` section in that sample applies only while `ConnectAsync()` is still trying to open
the transport. It does not mean the library will auto-recover a session later after a successful
connection has already transitioned into a receive failure.

For custom binary devices, `BinaryFrame` can describe a simple reusable wire envelope while
`RawHex` and `BitFieldSchema` continue to describe the payload bytes:

```json
{
  "Protocol": {
    "Type": "BinaryFrame",
    "MaxFrameLength": 512,
    "BinaryFrame": {
      "StartHex": "AA 55",
      "LengthPrefix": {
        "SizeBytes": 2,
        "Endianness": "BigEndian"
      },
      "Checksum": {
        "Type": "Crc16Modbus",
        "Endianness": "LittleEndian",
        "Coverage": "FrameWithoutChecksum"
      }
    }
  }
}
```

Protocols that need behavior beyond this envelope can still use a custom `IProtocolFactory`
registered through DI.

For payload-level bit values, keep the frame protocol out of it and read from the decoded
payload directly:

```csharp
var mode = BitFieldCodec.ReadUnsigned<byte>(payload, byteIndex: 1, startBit: 2, endBit: 5);
var signedNibble = BitFieldCodec.ReadSigned<sbyte>(payload, byteIndex: 2, startBit: 4, endBit: 7);
```

## Build And Test

```powershell
dotnet restore commlib-codex-full.sln
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj
dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj
```

For the workflow-aligned `Release` commands and focused test filters, see [docs/quick-start.md](docs/quick-start.md).

## Examples

- [examples/CommLib.Examples.Console](examples/CommLib.Examples.Console/README.md): loopback-oriented command-line demos for TCP, UDP, Multicast, and Serial.
- [examples/CommLib.Examples.WinUI](examples/CommLib.Examples.WinUI/README.md): a Windows desktop device-lab app for manual transport/session validation.

## Repository Notes

- This repository keeps several development-workflow artifacts at the root during active maintenance:
  `AGENT.md`, `CURRENT_PLAN.md`, `TODOS.md`, `CHANGELOG_AGENT.md`, `DECISIONS.md`, and `PROGRESS.md`.
  They are internal planning/continuity files, not part of the public CommLib runtime or package contract.
- Treat the runtime behavior in `src/`, package metadata, and the example READMEs as the real product-facing contract.
- The repository is now licensed under MIT. See [LICENSE](LICENSE).
- CI currently validates the core libraries, console example, and both test projects on Windows.
