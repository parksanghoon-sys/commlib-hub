# CommLib

CommLib is a config-driven .NET communication library for device-facing applications.
It focuses on a small, explicit runtime contract for TCP, UDP, Serial, and Multicast
connections, plus a lightweight application/bootstrap layer and runnable examples.

## What It Supports Today

- Transports: `TcpClient`, `Udp`, `Serial`, `Multicast`
- Framing: `LengthPrefixed`
- Serializers: `AutoBinary`, `RawHex`
- Request/response session flow with pending-request limits and timeouts
- Config-driven device profiles via `DeviceProfileRaw -> DeviceProfile` mapping
- Dependency-injection-friendly runtime composition through `AddCommLibCore()`
- Example applications:
  - [Console sample](examples/CommLib.Examples.Console/README.md)
  - [WinUI Device Lab](examples/CommLib.Examples.WinUI/README.md)

## Honest Contract Notes

This repository intentionally keeps the public contract narrower than the long-term roadmap.
Before using it in another project, these current boundaries are important:

- `ProtocolOptions` currently represents `LengthPrefixed` framing only.
- `ReconnectOptions` currently controls initial `ConnectAsync()` retry behavior only.
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

## Build And Test

```powershell
dotnet restore commlib-codex-full.sln
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj
dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj
```

## Examples

- [examples/CommLib.Examples.Console](examples/CommLib.Examples.Console/README.md): loopback-oriented command-line demos for TCP, UDP, Multicast, and Serial.
- [examples/CommLib.Examples.WinUI](examples/CommLib.Examples.WinUI/README.md): a Windows desktop device-lab app for manual transport/session validation.

## Repository Notes

- This repository still contains internal planning and continuity files used during active development.
  Treat the runtime behavior in `src/` and the example READMEs as the real product-facing contract.
- CI currently validates the core libraries, console example, and both test projects on Windows.
- A repository license file is still required before calling the repo fully open-source ready.
