# CommLib WinUI Device Lab

`CommLib.Examples.WinUI` is a WinUI 3 desktop example that lets you connect to a real TCP, UDP, multicast, or serial endpoint, send a `MessageModel`, and watch inbound traffic in a live log.

## Architecture

- Strict MVVM: `MainWindow` is just the shell, `AppShellView` handles page composition, `DeviceLabView` and `SettingsView` are separate pages, and state lives in shared view models.
- DI first: `App` composes CommLib services, the session service, the dispatcher abstraction, the settings store, the view models, and the views through `Microsoft.Extensions.DependencyInjection`.
- Persistent settings: `DeviceLabSettingsViewModel` is loaded from `appsettings.json` at startup, edited on the `Settings` page, and saved back to disk explicitly or when the window closes.
- UI polish: the example keeps a card-based control room layout, but favors a conservative WinUI control set so the sample stays runnable on developer machines with fragile XAML runtime environments.

## What It Exercises

- `ConnectionManager` session lifecycle
- Real `TcpTransport`, `UdpTransport`, `MulticastTransport`, and `SerialTransport`
- Length-prefixed framing with the `AutoBinary` serializer
- Shared settings state across the `Device Lab` and `Settings` pages
- JSON-backed app configuration in `examples/CommLib.Examples.WinUI/appsettings.json`

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
- The sample currently defaults to `win-x86` because the local `win-x64` runtime consistently faulted inside `Microsoft.UI.Xaml.dll` on this machine.
- The default `appsettings.json` is copied to the output folder on build and then reused as the runtime settings file.
