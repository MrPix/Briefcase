# SavedMessages — Implementation Status

### MVP

| # | Title | Status |
|---|-------|--------|
| 1 | Set up solution structure | ✅ Done |
| 2 | Configure .NET Aspire AppHost | ✅ Done — PostgreSQL, MinIO, Redis, Seq, API, Web all wired |
| 3 | Add Aspire ServiceDefaults | ✅ Done — OpenTelemetry, health checks, resilience |
| 4 | Scaffold ASP.NET Core API | ✅ Done — all controllers, `MessageHub`, services |
| 5 | Implement domain entities | ✅ Done — `User`, `Device`, `Message`, `FileAttachment`, `RefreshToken`, `TransferSession`, `ExternalLogin` |
| 6 | Configure EF Core + AppDbContext | ✅ Done — 2 migrations applied |
| 7 | Auth — register & login (JWT) | ✅ Done |
| 8 | JWT refresh token rotation | ✅ Done |
| 9 | OAuth 2.0 + PKCE | ✅ Done — `OAuthService`, redirect + callback endpoints |
| 10 | Messages API (GET/POST/DELETE/PATCH + pin toggle) | ✅ Done |
| 11 | Trash API (list + restore) | ✅ Done |
| 12 | Files API (upload + download) | ✅ Done |
| 13 | MinIO storage service | ✅ Done |
| 14 | Devices API (list, remove, pair-code, claim) | ✅ Done |
| 15 | Quick Transfer API (session + push) | ✅ Done |
| 16 | SignalR `MessageHub` | ✅ Done |
| 17 | Shared Razor component library | ✅ Done — `ClipboardPage`, `TransferPage`, `DevicesPage`, `MessageCard`, `QrScanner`, `LoginPage`, `SignupPage` |
| 18 | .NET MAUI Blazor Hybrid setup | ✅ Done |
| 19 | CI/CD pipeline (GitHub Actions) | ❌ Not started — no `.github/workflows/` directory |
| 20 | Unit tests — domain & services | ❌ Not started — empty stubs only |
| 21 | Integration tests — API | ❌ Not started — empty stubs only |
| 22 | Deploy to Azure Container Apps | ❌ Not started |

---

### Security

| # | Title | Status |
|---|-------|--------|
| 1 | `ShareLink` entity + migration | ✅ Done — entity and migration exist |
| 2 | Share link API (`POST`/`DELETE /api/messages/{id}/share`) | ❌ Not implemented — 501 stubs |
| 3 | Public share link view (`GET /s/{slug}`) | ❌ Not implemented |
| 4 | `UserE2eeSettings` entity + migration | ✅ Done |
| 5 | E2EE API (`/api/e2ee/*`) | ❌ Not implemented — 501 stubs |
| 6 | Client-side Argon2id KDF + AES-256-GCM | ❌ Not started |
| 7 | E2EE fields in `Message` (`IsEncrypted`, `EncryptionIV`) | ✅ Done |
| 8 | E2EE unit tests | ❌ Not started |
| 9 | Share link integration tests | ❌ Not started |

---

### Windows

| # | Title | Status |
|---|-------|--------|
| 1 | Configure Windows platform target | ✅ Done — `Package.appxmanifest`, `App.xaml` |
| 2 | Windows push notifications (WNS) | ❌ Not implemented |
| 3 | Windows secure credential storage | ✅ Done — `MauiTokenStorageService` uses `SecureStorage` → Windows Credential Manager |
| 4 | Publish to Microsoft Store | ❌ Not started |

Windows extras (beyond the plan): `WindowsTrayService`, `WindowsKeyboardShortcutService` (`Ctrl+N`, `Ctrl+Shift+V`, `Del`, `Ctrl+P`, `Ctrl+F`), `WindowsJumpListService`, `WindowsFileDropService` — all implemented.

---

### iOS

| # | Title | Status |
|---|-------|--------|
| 1 | Configure iOS platform target | ✅ Done — `Info.plist`, `AppDelegate.cs` |
| 2 | Camera QR scanner (`AVCaptureSession`) | ❌ Not implemented — no platform `IQrScannerService` |
| 3 | APNs push notifications | ❌ Not implemented |
| 4 | iOS secure credential storage | ✅ Done — `MauiTokenStorageService` → Keychain |
| 5 | Publish to App Store | ❌ Not started |

---

### Android

| # | Title | Status |
|---|-------|--------|
| 1 | Configure Android platform target | ✅ Done — `AndroidManifest.xml` |
| 2 | Camera QR scanner (CameraX / ZXing) | ❌ Not implemented |
| 3 | FCM push notifications | ❌ Not implemented |
| 4 | Android secure credential storage | ✅ Done — `MauiTokenStorageService` → `EncryptedSharedPreferences` |
| 5 | Publish to Google Play | ❌ Not started |

---

### Web (Blazor WASM PWA)

| # | Title | Status |
|---|-------|--------|
| 1 | Set up Blazor WASM PWA project | ✅ Done |
| 2 | Offline PWA support (service worker caching) | ✅ Done — `service-worker.published.js` has full asset caching strategy |
| 3 | SignalR client (real-time in browser) | ❌ Not implemented — no `HubConnection` in Web or Components projects |
| 4 | PWA manifest + icons | ⚠️ Partial — `manifest.json` + 512 px icon present; 192 px icon missing |
| 5 | Deploy to Azure Static Web Apps | ❌ Not started |

---

**Summary:** ~28 of 47 planned items are implemented. The main gaps are CI/CD, tests, push notifications on all platforms, the E2EE API logic, share link API/view, SignalR client on the web frontend, and all store/deployment steps.
