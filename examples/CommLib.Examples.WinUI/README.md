# CommLib WinUI Device Lab

`CommLib.Examples.WinUI` is a WinUI 3 desktop example that lets you connect to a real TCP, UDP, multicast, or serial endpoint, send outbound traffic with either the text-oriented `AutoBinary` serializer or the new `RawHex` mode, and watch inbound traffic in a live log.

## Architecture

- Strict MVVM: `MainWindow` is just the shell, `AppShellView` handles page composition, `DeviceLabView` and `SettingsView` are separate pages, and state lives in shared view models.
- DI first: `App` composes CommLib services, the session service, the dispatcher abstraction, the settings store, the view models, and the views through `Microsoft.Extensions.DependencyInjection`.
- Persistent settings: `DeviceLabSettingsViewModel` is loaded from `appsettings.json` at startup, edited on the `Settings` page, and saved back to disk explicitly or when the window closes.
- Language mode: the example persists an English/Korean UI mode in `appsettings.json`, and the shell plus primary pages refresh their copy when the mode changes.
- UI polish: the example keeps a card-based control room layout, but favors a conservative WinUI control set so the sample stays runnable on developer machines with fragile XAML runtime environments.
- Local loopback help: `Device Lab` can now start in-app TCP/UDP/Multicast mock peers so transport checks do not always require a second terminal or external process.

## What It Exercises

- `ConnectionManager` session lifecycle
- Real `TcpTransport`, `UdpTransport`, `MulticastTransport`, and `SerialTransport`
- Length-prefixed framing with selectable `AutoBinary` or `RawHex` serialization
- Shared settings state across the `Device Lab` and `Settings` pages
- JSON-backed app configuration in `examples/CommLib.Examples.WinUI/appsettings.json`

## Run

```powershell
dotnet build examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj
dotnet run --project examples/CommLib.Examples.WinUI/CommLib.Examples.WinUI.csproj
```

## Notes

- The peer device or test server must speak the same `LengthPrefixed + serializer` combination currently selected in the app.
- `AutoBinary` keeps the existing text-body example format, while `RawHex` sends whitespace-tolerant hexadecimal byte pairs as the raw payload.
- `RawHex` can now also load an optional `messageComposer.bitFieldSchema` from `appsettings.json`; when present, the live log appends decoded field summaries for inbound/outbound binary payloads. There is still no in-app schema editor, so this schema is currently JSON-managed only.
- TCP and UDP expect a reachable remote endpoint.
- Multicast receive/send requires the same group and port on both sides.
- Serial requires a real COM port, a paired virtual port, or hardware loopback wiring.
- The `Device Lab` and `Settings` pages now show only the currently selected transport panel instead of every transport preset at once.
- For local manual verification without external hardware, reuse `examples/CommLib.Examples.Console` for `tcp-demo`, `udp-demo`, or multicast send/receive peers in separate terminals.
- The in-app mock peer path covers TCP, UDP, and Multicast on loopback; Serial still stays external because it needs a paired COM environment.
- The sample now defaults to `win-x64` again because the current branch state no longer reproduces the earlier local `Microsoft.UI.Xaml.dll` startup fault; `-r win-x86` remains available if you need to compare runtimes.
- The default `appsettings.json` is copied to the output folder on build and then reused as the runtime settings file.

## RawHex Schema Example

Use a `RawHex` serializer plus `messageComposer.bitFieldSchema` when you want the live log to append decoded field summaries for binary payloads:

```json
{
  "messageComposer": {
    "serializerType": "RawHex",
    "bitFieldSchema": {
      "payloadLengthBytes": 4,
      "fields": [
        {
          "name": "prefix",
          "bitOffset": 0,
          "bitLength": 8
        },
        {
          "name": "register",
          "bitOffset": 8,
          "bitLength": 16,
          "endianness": "BigEndian"
        },
        {
          "name": "tail",
          "bitOffset": 24,
          "bitLength": 8
        }
      ]
    },
    "outboundMessageId": "100",
    "outboundBody": "AA 12 34 7F"
  }
}
```

- The schema is used for log enrichment only in this slice; it does not add an in-app schema editor or change the transport/protocol boundary.
- `BigEndian` is currently supported only for byte-aligned multi-byte fields whose lengths are whole-byte multiples.
