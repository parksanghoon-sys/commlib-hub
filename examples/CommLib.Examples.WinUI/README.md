# CommLib WinUI Device Lab

`CommLib.Examples.WinUI` is a WinUI 3 desktop example that lets you connect to a real TCP, UDP, multicast, or serial endpoint, send a `MessageModel`, and watch inbound traffic in a live log.

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
