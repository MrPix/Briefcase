# GitHub Issues Plan — Windows Milestone

## Labels Reference

| Label | Description |
|-------|-------------|
| `area:maui` | .NET MAUI Blazor Hybrid |
| `area:api` | ASP.NET Core backend |
| `area:ops` | CI/CD, Docker, Aspire |
| `type:feature` | New functionality |
| `type:chore` | Setup, scaffolding, config |
| `type:test` | Unit or integration test work |

---

## Milestone: Windows

Issues needed to ship the Windows desktop application (MAUI Blazor Hybrid targeting `net10.0-windows10.0.19041.0`). Prerequisite: MVP API milestone complete.

---

### Setup & Platform Configuration

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Complete Windows platform target configuration | `type:chore` `area:maui` | Finalize `Package.appxmanifest`: set real `Identity`, `PublisherDisplayName`, add `webcam` capability for QR scanning, add `backgroundTasks` capability for WNS; replace all `$placeholder$.png` visual elements with actual app icons per §3.6 |
| 2 | Configure Windows TFM and project settings | `type:chore` `area:maui` | In `SavedMessages.Maui.csproj` set `<WindowsPackageType>MSIX</WindowsPackageType>`, `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`, `<RuntimeIdentifiers>win10-x64;win10-arm64</RuntimeIdentifiers>`, and target OS version `10.0.19041.0` minimum per §3.6 |
| 3 | Register Windows-specific services in MauiProgram | `type:chore` `area:maui` | Add `#if WINDOWS` block in `MauiProgram.cs` to register `WindowsTokenStorageService`, `WindowsQrScannerService`, `WindowsClipboardService`, and `WindowsNotificationService` against their shared `IXxx` interfaces from `SavedMessages.Components` per §3.5, §3.6 |
| 4 | Configure initial window size and title | `type:chore` `area:maui` | Override `CreateWindow` in `App.xaml.cs` (Windows only) to set minimum size 900×600 and default size 1200×800; set window title to `"SavedMessages"` via `Microsoft.UI.Windowing.AppWindow` per §3.6 |

---

### Secure Credential Storage

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 5 | Implement Windows Credential Manager token storage | `type:feature` `area:maui` | Create `Platforms/Windows/WindowsTokenStorageService.cs` implementing `ITokenStorageService`; use `Windows.Security.Credentials.PasswordVault` to store and retrieve access/refresh tokens keyed by `"SavedMessages/access_token"` and `"SavedMessages/refresh_token"`; replaces the generic `SecureStorage.Default` path on Windows per §6 |

---

### Camera & QR Scanning

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 6 | Implement QR scanner service (Windows) | `type:feature` `area:maui` | Create `Platforms/Windows/WindowsQrScannerService.cs` implementing `IQrScannerService`; use `Windows.Media.Capture.MediaCapture` to open the default webcam, capture preview frames, and decode QR codes via `ZXing.Net.MAUI` or `ZXing.Net` directly; surface result as `Task<string?>` per §3.5 |
| 7 | Add webcam capability to Package.appxmanifest | `type:chore` `area:maui` | Add `<DeviceCapability Name="webcam" />` inside `<Capabilities>` in `Platforms/Windows/Package.appxmanifest`; required before `MediaCapture` can open the camera; depends on issue #6 per §3.6 |

---

### Push Notifications

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 8 | Implement WNS push notification registration | `type:feature` `area:maui` | Create `Platforms/Windows/WindowsNotificationService.cs`; on app start call `PushNotificationManager.Default.CreateChannelAsync` to obtain a WNS channel URI; call `POST /api/devices` (or update endpoint) to persist the WNS URI as `Device.PushToken`; handle channel renewal per §4 |
| 9 | Implement Windows toast notification handler | `type:feature` `area:maui` | In `WindowsNotificationService` show `Microsoft.Windows.AppNotifications.AppNotification` toasts for `MessageCreated` and `TransferReceived` SignalR events; include action buttons ("Open", "Dismiss"); deep-link toast activation to the relevant app page via URI protocol per §3.3 |
| 10 | Handle WNS background notification activation | `type:feature` `area:maui` | Register a background task in `Package.appxmanifest` (`<uap:Extension Category="windows.backgroundTasks">`); process incoming WNS raw notifications when the app is suspended so toasts still appear; persist the payload until the app foregrounds per §3.3 |

---

### Clipboard & File Operations

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 11 | Implement Windows clipboard service | `type:feature` `area:maui` | Create `Platforms/Windows/WindowsClipboardService.cs` implementing `IClipboardService`; use `Windows.ApplicationModel.DataTransfer.Clipboard` to read/write text and optionally rich content; wire into shared `ClipboardPage.razor` via DI per §3.5 |
| 12 | Implement Windows file picker for uploads | `type:feature` `area:maui` | Add Windows-specific override for the file-selection flow on `FilesPage`/`MessageCard`; use `Windows.Storage.Pickers.FileOpenPicker` (set `hwnd` via `WinRT.Interop.WindowNative.GetWindowHandle`); call `POST /api/files` multipart with the selected stream per §5 |
| 13 | Implement file download to Downloads folder | `type:feature` `area:maui` | When the user saves a file message, stream `GET /api/files/{id}` response to `%USERPROFILE%\Downloads\<OriginalName>` via `Windows.Storage.KnownFolders` or `Environment.GetFolderPath`; show a toast notification with "Open file" action on completion per §5 |

---

### URI Activation (Deep Linking)

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 14 | Register URI scheme activation in Package.appxmanifest | `type:chore` `area:maui` | Add `<uap:Extension Category="windows.protocol"><uap:Protocol Name="savedmessages" /></uap:Extension>` to `Package.appxmanifest`; enables `savedmessages://pair?token=...` deep links for device QR pairing and `savedmessages://transfer?session=...` for quick transfer per §3.4 |
| 15 | Handle URI activation in MAUI Windows app | `type:feature` `area:maui` | Override `OnActivated` in `Platforms/Windows/App.xaml.cs` to intercept `ProtocolActivatedEventArgs`; parse scheme and path, then navigate the Blazor shell to `DevicesPage` (pair flow) or `TransferPage` (transfer flow) accordingly per §3.4, §3.6 |

---

### SignalR & Background Connectivity

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 16 | Implement SignalR client for Windows MAUI | `type:feature` `area:maui` | Wire `Microsoft.AspNetCore.SignalR.Client.HubConnection` in a scoped `SignalRService` registered in `MauiProgram.cs`; connect on app foreground, disconnect on suspend; subscribe to `MessageCreated`, `MessageTrashed`, `MessageRestored`, `TransferReceived` events and dispatch to the component layer per §3.3 |
| 17 | Implement auto-reconnect and token refresh for SignalR | `type:feature` `area:maui` | Configure `.WithAutomaticReconnect()` on `HubConnectionBuilder`; on `Closed` event, re-acquire a valid JWT via `ITokenStorageService` + `POST /api/auth/refresh` before reconnecting; expose connection state to the UI per §3.3, §6 |

---

### Packaging & Distribution

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 18 | Configure MSIX package signing certificate | `type:chore` `area:ops` | Generate a self-signed certificate for local dev (`New-SelfSignedCertificate`); store the production code-signing certificate as a GitHub Actions secret; configure `<PackageCertificateThumbprint>` in `SavedMessages.Maui.csproj` per §7 |
| 19 | Add GitHub Actions Windows build and package job | `type:chore` `area:ops` | Add `build-windows` job to CI pipeline running on `windows-latest`; steps: `dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64`; sign MSIX via `SignTool`; upload `.msix` as a build artifact per §7 |
| 20 | Publish to Microsoft Store via Partner Center | `type:chore` `area:ops` | Create app listing in Partner Center; submit signed MSIX; configure Store association in `Package.appxmanifest` (`<Identity Name="..." Publisher="CN=..." />`); set up `StoreBroker` or manual submission in CI for release builds per §7 |

---

### UI & Desktop Experience

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 23 | Implement desktop app shell with sidebar navigation | `type:feature` `area:maui` | Create `Platforms/Windows/Components/DesktopShell.razor` with a fixed left sidebar (`<nav>`) containing links to Clipboard, Transfer, Devices, Trash, and Settings; replace the default top-navbar layout used on mobile; shell is injected as the root layout in `Routes.razor` via `#if WINDOWS` per §3.6 |
| 24 | Implement two-column desktop layout for ClipboardPage | `type:feature` `area:frontend` | On `min-width: 900px` render a master–detail layout: left column shows the pinned + recent message list, right column shows the compose area and selected message detail; add a CSS class `layout-desktop` toggled via a `IWindowSizeService` or CSS media query in the shared component library per §3.5 |
| 25 | Add Windows app visual assets (icons & splash) | `type:chore` `area:maui` | Replace all `$placeholder$.png` references in `Package.appxmanifest` with properly sized assets: Square44×44, Square71×71, Square150×150, Square310×310, Wide310×150, and SplashScreen; provide both light and dark variants in `Assets/` per §3.6 |
| 26 | Implement system dark/light theme following | `type:feature` `area:maui` | Subscribe to `Microsoft.UI.Xaml.Application.Current.RequestedTheme` change events on Windows; propagate the resolved theme (`dark`/`light`) as a CSS class on the Blazor `<body>` element so Bootstrap color-scheme variables respond to Windows system theme changes per §3.6 |
| 27 | Implement drag-and-drop file upload | `type:feature` `area:maui` | Handle `DragDrop` events on the `BlazorWebView` host window using `Microsoft.UI.Xaml.UIElement.Drop`; extract `IStorageItem` paths from the `DataPackage`, then forward the file stream to `POST /api/files`; show an inline drop-zone overlay in `ClipboardPage.razor` when `dragenter` is detected per §5 |
| 28 | Implement keyboard shortcuts | `type:feature` `area:maui` | Register global keyboard accelerators in `Platforms/Windows/App.xaml.cs` using `Microsoft.UI.Xaml.Input.KeyboardAccelerator`: Ctrl+N (new message), Ctrl+Shift+V (paste from clipboard), Del (move to trash), Ctrl+P (toggle pin), Ctrl+F (focus search); dispatch actions to the current Blazor page via a shared `IKeyboardShortcutService` per §3.5 |
| 29 | Add right-click context menu to MessageCard | `type:feature` `area:frontend` | Extend `MessageCard.razor` with a Bootstrap dropdown triggered on `@oncontextmenu`; items: Copy, Pin/Unpin, Share, Move to Trash; suppress browser default context menu via `e.preventDefault()`; gated behind a `[Parameter] bool ShowContextMenu` flag so mobile layouts are unaffected per §3.5 |
| 30 | Implement Windows taskbar Jump List | `type:feature` `area:maui` | On app launch populate a `JumpList` (`Windows.UI.StartScreen.JumpList`) with tasks: "New Message", "Quick Transfer", "Open Clipboard"; update "Recent" category with the last 5 message snippets when messages are loaded; clear stale entries on sign-out per §3.6 |
| 31 | Implement system tray icon | `type:feature` `area:maui` | Add an `H.NotifyIcon` (or `WPF NotifyIcon` via WinRT interop) to the MAUI Windows app; tray menu items: Show, New Message, Quick Transfer, Sign Out, Quit; clicking a notification toast while the window is minimized should restore and focus the window per §3.6 |

---

### Tests

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 21 | Write unit tests — Windows platform services | `type:test` `area:maui` | Unit-test `WindowsTokenStorageService` (mock `PasswordVault`), `WindowsNotificationService` toast dispatch logic, and URI-activation parsing; add to `SavedMessages.UnitTests` per §8.1 |
| 22 | Write integration tests — Windows MAUI end-to-end | `type:test` `area:maui` | Validate auth flow (login → token stored in Credential Manager), message list load, file upload, and QR pair initiation against the `WebApplicationFactory` test host; run on `windows-latest` CI runner per §8.2 |
