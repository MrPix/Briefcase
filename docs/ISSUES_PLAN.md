# GitHub Issues Plan — SavedMessages

## Labels

### Area
| Label | Color | Description |
|-------|-------|-------------|
| `area:api` | `#0075ca` | ASP.NET Core backend |
| `area:frontend` | `#d93f0b` | Shared Razor components |
| `area:infra` | `#e4e669` | EF Core, S3/MinIO storage |
| `area:e2ee` | `#b60205` | End-to-end encryption |
| `area:ops` | `#c5def5` | CI/CD, Docker, Aspire |
| `area:maui` | `#bfd4f2` | .NET MAUI Blazor Hybrid |

### Type
| Label | Color | Description |
|-------|-------|-------------|
| `type:feature` | `#0052cc` | New functionality |
| `type:chore` | `#cccccc` | Setup, scaffolding, config |
| `type:bug` | `#d73a4a` | Something is broken |
| `type:test` | `#0e8a16` | Unit or integration test work |

---

## Milestones

| Title | Goal |
|-------|------|
| MVP | Must ship in the first public release |
| Security | Security hardening — E2EE, share links |
| iOS | iOS / iPhone specific work |
| Android | Android specific work |
| Windows | Windows desktop specific work |
| Web | Blazor WASM / PWA specific work |

---

## Issues

### MVP

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Set up solution structure | `type:chore` | Create `.slnx`, all 9 projects, and project references per §2 |
| 2 | Configure .NET Aspire AppHost | `type:chore` `area:ops` | Wire up API, SQL, Blob, SignalR, and Web resources per §3.1 |
| 3 | Add Aspire ServiceDefaults | `type:chore` `area:ops` | OpenTelemetry, health checks, and resilience policies per §3.1 |
| 4 | Scaffold ASP.NET Core API | `type:chore` `area:api` | Add controller stubs, `MessageHub`, `QrCodeService`, `TransferSessionService`; configure JWT middleware per §3.2; project is `SavedMessages.ApiService` |
| 5 | Implement domain entities | `type:chore` `area:infra` | `User`, `Device`, `Message`, `FileAttachment`, `RefreshToken`, `TransferSession` with all fields per §4 |
| 6 | Configure EF Core + AppDbContext | `type:chore` `area:infra` | Schema, indexes, initial migration, Azure SQL / PostgreSQL provider per §4 |
| 7 | Implement Auth — register & login | `type:feature` `area:api` | `POST /api/auth/register`, `/login` → JWT; 15 min access + 7 day refresh per §5, §6 |
| 8 | Implement JWT refresh | `type:feature` `area:api` | `POST /api/auth/refresh`; HttpOnly cookie for web, secure storage for MAUI per §6 |
| 9 | Implement OAuth 2.0 + PKCE | `type:feature` `area:api` | Google / Facebook / Apple / Microsoft providers; PKCE + state param per §5, §6 |
| 10 | Implement Messages API | `type:feature` `area:api` | `GET/POST/DELETE/PATCH` per §5; `IsDeleted` soft-delete, `DeletedAt`, pin toggle per §6 |
| 11 | Implement Trash API | `type:feature` `area:api` | `GET /api/trash`, `POST /api/trash/{id}/restore`; filter on `IsDeleted = true` per §5 |
| 12 | Implement Files API | `type:feature` `area:api` `area:infra` | Multipart upload, stream download through API, delete blob + metadata per §5 |
| 13 | Implement MinIO storage service | `type:chore` `area:infra` | `MinioStorageService` (S3-compatible via `AWSSDK.S3`) against local MinIO container; configure presigned-URL generation; wire S3 credentials via `IConfiguration` per §3 |
| 14 | Implement Devices API | `type:feature` `area:api` | List, remove, QR pair-code (signed JWT, 5 min TTL), claim per §3.4, §5 |
| 15 | Implement Quick Transfer API | `type:feature` `area:api` | `POST /api/transfer/session` + `/push`; 10 min expiry, single-use per §3.4, §5 |
| 16 | Implement SignalR MessageHub | `type:feature` `area:api` | User-group push for `MessageCreated`, `MessageTrashed`, `MessageRestored`, `TransferReceived` per §3.3, §5 |
| 17 | Build shared Razor component library | `type:feature` `area:frontend` | `ClipboardPage`, `TransferPage`, `DevicesPage`, `MessageCard`, `QrScanner`; platform interfaces per §3.5 |
| 18 | Set up .NET MAUI Blazor Hybrid | `type:chore` `area:maui` | `MauiProgram.cs`, `MainPage.xaml` with `BlazorWebView`, platform folder stubs per §3.6 |
| 19 | Set up CI/CD pipeline | `type:chore` `area:ops` | GitHub Actions: build, run unit tests, run integration tests, push container image per §7 |
| 20 | Write unit tests — domain & services | `type:test` | `QrCodeService`, `TransferSessionService`, JWT helpers, soft-delete invariants per §8.1 |
| 21 | Write integration tests — API | `type:test` | Auth, messages, files, devices, transfer; shared `WebApplicationFactory`; Respawn DB reset per §8.2 |
| 22 | Deploy API to Azure Container Apps | `type:chore` `area:ops` | Container image, `azd deploy`, scale-to-zero, Key Vault secrets per §7 |

---

### Security

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Implement ShareLink entity + migration | `type:chore` `area:infra` | `Slug`, `ExpiresAt`, `IsOneTime`, `ViewCount`, `RevokedAt` fields per §4 |
| 2 | Implement share link API | `type:feature` `area:api` | `POST /DELETE /api/messages/{id}/share`; CSPRNG 12-char slug (~72 bits entropy) per §5, §6 |
| 3 | Implement public share link view | `type:feature` `area:api` | `GET /s/{slug}`; one-time atomic revoke via optimistic concurrency update; file → streamed through API per §5, §6 |
| 4 | Implement UserE2eeSettings entity | `type:chore` `area:infra` `area:e2ee` | `KdfAlgorithm`, `KdfSalt`, `KdfParams`, `KeyVerifier` fields; migration per §4 |
| 5 | Implement E2EE API | `type:feature` `area:api` `area:e2ee` | `GET/POST /api/e2ee/settings`, `/enable`, `/disable`, `PUT /change-passphrase`; passphrase never sent to server per §5 |
| 6 | Implement client-side Argon2id KDF | `type:feature` `area:frontend` `area:e2ee` | Key derivation, AES-256-GCM encrypt/decrypt, KeyVerifier generation and check per §6, §9 |
| 7 | Store E2EE ciphertext + IV in Message | `type:feature` `area:infra` `area:e2ee` | `IsEncrypted`, `EncryptionIV` fields; client encrypts content before `POST /api/messages` per §4 |
| 8 | Write E2EE unit tests | `type:test` `area:e2ee` | Argon2id param validation, KDF verifier round-trip, AES-GCM encrypt/decrypt per §8.1 |
| 9 | Write share link integration tests | `type:test` | Public view, one-time atomic revoke race condition, cross-user isolation, trashed-message link rejection per §8.2 |

---

### iOS

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Configure iOS platform target | `type:chore` `area:maui` | `Info.plist`, entitlements, bundle ID, code signing profile per §3.6 |
| 2 | Implement camera QR scanner (iOS) | `type:feature` `area:maui` | `AVCaptureSession` via MAUI native API; exposed via `IQrScannerService` per §3.5 |
| 3 | Implement APNs push notifications | `type:feature` `area:maui` | Register APNs token on launch; store as `Device.PushToken` per §4 |
| 4 | Implement iOS secure credential storage | `type:feature` `area:maui` | iOS Keychain for JWT refresh token per §6 |
| 5 | Publish to App Store | `type:chore` `area:ops` | Archive build, TestFlight internal testing, App Store submission per §7 |

---

### Android

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Configure Android platform target | `type:chore` `area:maui` | `AndroidManifest.xml`, camera + internet + notification permissions per §3.6 |
| 2 | Implement camera QR scanner (Android) | `type:feature` `area:maui` | CameraX / ZXing via MAUI native API; exposed via `IQrScannerService` per §3.5 |
| 3 | Implement FCM push notifications | `type:feature` `area:maui` | Register FCM token on launch; store as `Device.PushToken` per §4 |
| 4 | Implement Android secure credential storage | `type:feature` `area:maui` | Android Keystore / `EncryptedSharedPreferences` for JWT refresh token per §6 |
| 5 | Publish to Google Play | `type:chore` `area:ops` | Signed AAB, internal test track, production release per §7 |

---

### Windows

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Configure Windows platform target | `type:chore` `area:maui` | `Package.appxmanifest`, identity, capabilities per §3.6 |
| 2 | Implement Windows push notifications | `type:feature` `area:maui` | WNS token registration; store as `Device.PushToken` per §4 |
| 3 | Implement Windows secure credential storage | `type:feature` `area:maui` | Windows Credential Manager for JWT refresh token per §6 |
| 4 | Publish to Microsoft Store | `type:chore` `area:ops` | MSIX package, Partner Center submission per §7 |

---

### Web

| # | Title | Labels | Notes |
|---|-------|--------|-------|
| 1 | Set up Blazor WASM PWA project | `type:chore` `area:frontend` | `Program.cs`, service worker, `manifest.json`, PWA installability per §3.7 |
| 2 | Configure offline PWA support | `type:feature` `area:frontend` | Service worker caching strategy; offline placeholder for no-connection state per §3.7, §9 |
| 3 | Implement SignalR client (Web) | `type:feature` `area:frontend` | Real-time `MessageCreated` / `TransferReceived` in browser; auto-reconnect per §3.3 |
| 4 | Write PWA manifest + icons | `type:chore` `area:frontend` | 192 px and 512 px icons, `theme_color`, `display: standalone` per §3.7 |
| 5 | Deploy WASM to Azure Static Web Apps | `type:chore` `area:ops` | GitHub Actions publish WASM `wwwroot`; free-tier CDN per §7 |

---

