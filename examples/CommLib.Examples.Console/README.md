# CommLib Example Console

`CommLib.Examples.Console` is a small command-line sample that exercises the current library stack with concrete transport settings.

## Commands

```powershell
dotnet run --project examples/CommLib.Examples.Console -- tcp-demo
dotnet run --project examples/CommLib.Examples.Console -- tcp-echo-server --port 7001
dotnet run --project examples/CommLib.Examples.Console -- udp-demo
dotnet run --project examples/CommLib.Examples.Console -- udp-echo-server --port 7002
dotnet run --project examples/CommLib.Examples.Console -- multicast-receive --port 7004
dotnet run --project examples/CommLib.Examples.Console -- multicast-send --port 7004
dotnet run --project examples/CommLib.Examples.Console -- serial-demo --port COM3 --peer-port COM4
```

## What Each Demo Covers

- `tcp-demo`: starts a loopback TCP echo server, connects with `ConnectionManager`, sends one `MessageModel`, and receives the echoed frame back.
- `tcp-echo-server`: keeps a loopback TCP echo peer alive so the WinUI example or another local process can connect and exchange multiple frames.
- `udp-demo`: starts a loopback UDP echo server, sends one datagram through `UdpTransport`, and receives the echoed frame back.
- `udp-echo-server`: keeps a loopback UDP echo peer alive so the WinUI example or another local process can exchange multiple datagrams.
- `multicast-receive`: joins the multicast group and waits for one inbound frame.
- `multicast-send`: opens `MulticastTransport` and publishes one frame to the multicast group.
- `serial-demo`: requires a paired COM port setup such as `com0com` or a hardware loopback pair. The sample opens the peer port directly and the client port through `SerialTransport`.

## Notes

- All demos use `LengthPrefixedProtocol` plus `NoOpSerializer` via the library factories.
- The sample prints connection attempts through `IConnectionEventSink`, so connect-time retry behavior is visible during runs.
- `Reconnect` in the sample profile means transport-open retry during `ConnectAsync()` only. A later live-session receive failure still remains terminal until a higher layer reconnects.
- `tcp-echo-server` and `udp-echo-server` stop on `Ctrl+C` or an optional `--timeout-ms` so they are easy to reuse during WinUI manual validation.
- `multicast-receive` exits with a timeout error when no traffic arrives before `--timeout-ms`, which makes empty validation runs obvious in scripts.
- `serial-demo` is intentionally the only example that needs external setup because a single process cannot open both ends of the same serial port.
- Run `multicast-receive` in one terminal first, then trigger `multicast-send` from another terminal.
