# CommLib Quick Start

This guide is the fastest path to:

- restore and build the repository
- run the test projects
- exercise the shipped examples
- wire CommLib into a host or use it manually through `IConnectionManager`

## Before You Start

- .NET SDK 9.0
- Windows if you want to run the WinUI example
- A reachable TCP/UDP/Serial/Multicast endpoint, or the local loopback examples in this repo

## 1. Restore And Build

From the repository root:

```powershell
dotnet restore commlib-codex-full.sln
dotnet build commlib-codex-full.sln --configuration Release --no-restore
```

If you only want the non-WinUI baseline that CI validates, use:

```powershell
dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore
```

## 2. Run Tests

These are the workflow-aligned commands used for the core validation path:

```powershell
dotnet restore commlib-codex-full.sln
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --configuration Release --no-restore
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --configuration Release --no-restore
dotnet build examples/CommLib.Examples.Console/CommLib.Examples.Console.csproj --configuration Release --no-restore
```

Useful focused commands:

```powershell
dotnet test tests/CommLib.Unit.Tests/CommLib.Unit.Tests.csproj --filter DeviceSessionTests
dotnet test tests/CommLib.Infrastructure.Tests/CommLib.Infrastructure.Tests.csproj --filter ConnectionManagerTests
dotnet pack src/CommLib.Domain/CommLib.Domain.csproj --configuration Release --no-restore -o artifacts/pack
```

## 3. Run The Examples

### Console Sample

```powershell
dotnet run --project examples/CommLib.Examples.Console -- tcp-demo
dotnet run --project examples/CommLib.Examples.Console -- udp-demo
dotnet run --project examples/CommLib.Examples.Console -- multicast-receive --port 7004
dotnet run --project examples/CommLib.Examples.Console -- multicast-send --port 7004
dotnet run --project examples/CommLib.Examples.Console -- serial-demo --port COM3 --peer-port COM4
```

See the full command notes in [examples/CommLib.Examples.Console/README.md](../examples/CommLib.Examples.Console/README.md).

### WinUI Device Lab

```powershell
dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj
dotnet run --project examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj
```

See the detailed operator notes in [examples/CommLib.Examples.WinUI/README.md](../examples/CommLib.Examples.WinUI/README.md).

## 4. Understand The Current Contract

CommLib is intentionally narrow right now:

- transport types: `TcpClient`, `Udp`, `Serial`, `Multicast`
- framing: `LengthPrefixed`
- serializers: `AutoBinary`, `RawHex`
- reconnect behavior: initial `ConnectAsync()` retry only
- failed receive loop behavior: the session becomes terminal until the caller reconnects explicitly

If you are integrating the library into another app, keep those boundaries in mind before assuming richer framing or runtime reconnect behavior.

## 5. Use It With Generic Host Configuration

If your app already uses `Microsoft.Extensions.Hosting`, the simplest path is to bind the `CommLib` section from configuration and let the hosted service connect enabled devices on startup.

```csharp
using CommLib.Hosting;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCommLibCore(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
```

This path:

- binds `CommLibOptions`
- maps each enabled `DeviceProfileRaw` entry to `DeviceProfile`
- validates and connects them during host startup through `DeviceBootstrapper`
- disposes active connections when the host stops

The repository root already includes a sample [appsettings.json](../appsettings.json) with the expected shape.

## 6. Use It Manually Through `IConnectionManager`

If you want full control over connect/send/receive timing, register the core services and drive the connection manager directly.

```csharp
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Hosting;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCommLibCore(options =>
{
    options.InboundQueueCapacity = 256;
});

await using var provider = services.BuildServiceProvider();
var manager = provider.GetRequiredService<IConnectionManager>();

var profile = new DeviceProfile
{
    DeviceId = "tcp-demo",
    DisplayName = "Local TCP Demo",
    Enabled = true,
    Transport = new TcpClientTransportOptions
    {
        Type = "TcpClient",
        Host = "127.0.0.1",
        Port = 7001,
        ConnectTimeoutMs = 1000,
        BufferSize = 1024,
        NoDelay = true
    },
    Protocol = new ProtocolOptions
    {
        Type = "LengthPrefixed",
        MaxFrameLength = 4096
    },
    Serializer = new SerializerOptions
    {
        Type = SerializerTypes.AutoBinary
    },
    RequestResponse = new RequestResponseOptions
    {
        DefaultTimeoutMs = 2000,
        MaxPendingRequests = 32
    }
};

await manager.ConnectAsync(profile);
await manager.SendAsync(profile.DeviceId, new MessageModel(100, "ping"));

var inbound = await manager.ReceiveAsync(profile.DeviceId);
Console.WriteLine($"Received message {inbound.MessageId}");

await manager.DisconnectAsync(profile.DeviceId);
```

Message helpers already shipped in `CommLib.Domain.Messaging`:

- `MessageModel` for text-oriented `AutoBinary`
- `BinaryMessageModel` for raw payloads
- `RequestMessageModel` / `ResponseMessageModel` for request-response flows
- binary request/response variants for raw payload request-response cases

## 7. Common Next Steps

- Start with `tcp-demo` if you want the quickest loopback sanity check.
- Use the root `appsettings.json` as the template for real device profiles.
- Use `AutoBinary` first unless you specifically need raw payload control.
- Move to the WinUI sample when you want manual endpoint validation with live logs.
