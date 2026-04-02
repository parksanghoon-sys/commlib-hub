# CommLib WinUI Device Lab

`CommLib.Examples.WinUI` is a WinUI 3 desktop example that lets you connect to a real TCP, UDP, multicast, or serial endpoint, send a `MessageModel`, and watch inbound traffic in a live log.

## Architecture

- Strict MVVM: `MainWindow` is just the shell, `DeviceLabView` owns the visual tree, and `MainViewModel` plus the transport-specific view models hold state and commands.
- DI first: `App` composes CommLib services, the session service, the dispatcher abstraction, the view models, and the view through `Microsoft.Extensions.DependencyInjection`.
- UI polish: the example uses a code-built theme dictionary, styled inputs and buttons, and entrance/reposition transitions for cards, transport panels, and live log entries.

## What It Exercises

- `ConnectionManager` session lifecycle
- Real `TcpTransport`, `UdpTransport`, `MulticastTransport`, and `SerialTransport`
- Length-prefixed framing with the `AutoBinary` serializer

## Run

```powershell
dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj
dotnet run --project examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj
```

## Notes

- The peer device or test server must speak the same `LengthPrefixed + AutoBinary` format used by the library examples.
- TCP and UDP expect a reachable remote endpoint.
- Multicast receive/send requires the same group and port on both sides.
- Serial requires a real COM port, a paired virtual port, or hardware loopback wiring.
- The project is built as `win-x64` with `WindowsAppSDKSelfContained=true` so the output is easier to move between developer machines.
