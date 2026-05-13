# Architecture

## 1. System Overview

SavedMessages is a multi-tier, real-time application. A single ASP.NET Core 10 backend serves all clients. Native apps (Windows, Android, iOS, macOS) are built with .NET MAUI + Blazor Hybrid. The web client is a Blazor WebAssembly PWA. Both frontends share a common Razor component library, so UI code is written once.

```
┌─────────────────────────────────────────────────────────────┐
│                         Clients                             │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────┐  │
│  │  Windows │  │ Android  │  │   iOS    │  │  macOS    │  │
│  │  (MAUI)  │  │  (MAUI)  │  │  (MAUI)  │  │  (MAUI)   │  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └─────┬─────┘  │
│       │             │              │               │        │
│  ┌────┴─────────────┴──────────────┴───────────────┴─────┐  │
│  │          Shared Razor Component Library               │  │
│  └────────────────────────┬──────────────────────────────┘  │
│                           │                                 │
│  ┌────────────────────────┴──────────────────────────────┐  │
│  │            Blazor WebAssembly PWA (Web)               │  │
│  └───────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS / WebSocket (SignalR)
┌────────────────────────▼────────────────────────────────────┐
│                   ASP.NET Core 10 API                       │
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │  Auth       │  │  Messages   │  │  Files              │ │
│  │  Controller │  │  Controller │  │  Controller         │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              E2EE Controller (key params only)          │ │
│  └─────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              SignalR Hub  (MessageHub)                  │ │
│  └─────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              QR Code Service                           │ │
│  └─────────────────────────────────────────────────────────┘ │
└──────────┬──────────────┬───────────────────────────────────┘
           │              │
  ┌────────▼──────┐  ┌────▼───────────────┐
  │  Azure SQL    │  │  Azure Blob Storage│
  │  Database     │  │  (files)           │
  └───────────────┘  └────────────────────┘
```

---

## 2. Solution Structure

```
SavedMessages/
├── src/
│   ├── SavedMessages.AppHost/          # .NET Aspire orchestration entry point
│   ├── SavedMessages.ServiceDefaults/  # Shared Aspire defaults: OpenTelemetry, health checks, resilience
│   ├── SavedMessages.Api/              # ASP.NET Core 10 Web API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── MessagesController.cs
│   │   │   ├── FilesController.cs
│   │   │   └── DevicesController.cs
│   │   ├── Hubs/
│   │   │   └── MessageHub.cs          # SignalR hub
│   │   ├── Services/
│   │   │   ├── QrCodeService.cs
│   │   │   └── TransferSessionService.cs
│   │   └── Program.cs
│   ├── SavedMessages.Domain/           # Pure domain — no framework deps
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Device.cs
│   │   │   ├── Message.cs
│   │   │   └── FileAttachment.cs
│   │   └── Interfaces/
│   │       ├── IMessageRepository.cs
│   │       └── IFileStorageService.cs
│   ├── SavedMessages.Infrastructure/   # EF Core, Azure SDK integrations
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   └── Storage/
│   │       └── AzureBlobStorageService.cs
│   ├── SavedMessages.Components/       # Shared Razor component library
│   │   ├── Pages/
│   │   │   ├── ClipboardPage.razor
│   │   │   ├── TransferPage.razor
│   │   │   └── DevicesPage.razor
│   │   └── Components/
│   │       ├── MessageCard.razor
│   │       └── QrScanner.razor
│   ├── SavedMessages.Web/              # Blazor WebAssembly PWA
│   │   ├── Program.cs
│   │   └── wwwroot/
│   │       └── manifest.json          # PWA manifest
│   └── SavedMessages.Maui/            # .NET MAUI Blazor Hybrid
│       ├── MauiProgram.cs
│       ├── Platforms/
│       │   ├── Android/
│       │   ├── iOS/
│       │   ├── MacCatalyst/
│       │   └── Windows/
│       └── MainPage.xaml              # Hosts BlazorWebView
├── tests/
│   ├── SavedMessages.UnitTests/        # mstests — domain logic, services, E2EE
│   └── SavedMessages.IntegrationTests/ # mstests + Aspire test host — full HTTP + DB + SignalR
└── docs/
    └── ARCHITECTURE.md
```

---

## 3. Components

### 3.1 .NET Aspire (`AppHost`)

Aspire is the local development orchestrator. It wires up:
- The API project
- Azure SQL (via the Aspire SQL Server container or emulator)
- Azure Blob Storage emulator (Azurite)
- Azure SignalR Service emulator (or local in-process fallback)
- The Blazor WASM web frontend

In production, resources are replaced by real Azure services referenced via connection strings stored in Azure Key Vault.

### 3.2 ASP.NET Core API

Responsibilities:
- JWT + OAuth 2.0 token issuance and validation
- CRUD for messages and file metadata
- Streaming file upload/download to/from Azure Blob Storage
- SignalR hub for real-time push to connected devices
- QR code generation and transfer-session management

### 3.3 SignalR Hub (`MessageHub`)

Every authenticated client connects to the hub. When a message is created or a quick-transfer session is resolved, the server pushes events to the relevant user's group (all devices belonging to that user). Azure SignalR Service is used in production for horizontal scale-out.

### 3.4 QR Code Flows

**Quick Device Add**
```
1. Authenticated device calls  POST /api/devices/pair-code
   → Server returns a short-lived signed token (JWT, 5 min TTL)
   → Client renders it as a QR code
2. New device scans the QR code
   → Calls  POST /api/devices/claim  { token }
   → Server validates token, registers the new device under the same user account
   → Returns a full session JWT for the new device
```

**Quick Transfer**
```
1. Target device (browser) opens  /transfer
   → Calls  POST /api/transfer/session
   → Server creates a transfer session ID, returns it
   → Page renders session ID as a QR code and opens a SignalR connection
2. Source device (app or browser) scans the QR code
   → Calls  POST /api/transfer/push  { sessionId, content }
   → Server pushes content to the transfer session's SignalR group
3. Target browser receives the SignalR event, displays the content
   → No account required on the target device for this flow (session is the auth)
```

### 3.5 Shared Razor Component Library

Contains all pages and UI components as Razor components. Both `SavedMessages.Web` (WASM) and `SavedMessages.Maui` (Blazor Hybrid) reference this library. Platform-specific concerns (camera for QR scanning, file picker) are abstracted behind interfaces injected at the app level.

### 3.6 .NET MAUI Blazor Hybrid

`MainPage.xaml` hosts a `BlazorWebView` that renders the shared Razor components. MAUI provides native platform APIs (camera, share sheet, background notifications, local secure storage for tokens). One project builds for Windows, Android, iOS, and macOS.

### 3.7 Blazor WebAssembly PWA

A standard Blazor WASM project that references the shared component library. Configured as a PWA so it can be installed from the browser on any platform. Used on devices where installing a native app is impractical (work laptops, car head units, tablets).

---

## 4. Data Model

```
User
  Id              Guid        PK
  Email           string      unique
  DisplayName     string
  AvatarUrl       string?
  CreatedAt       datetime

ExternalLogin
  Id              Guid        PK
  UserId          Guid        FK → User
  Provider        string      (Google | Facebook | Apple | Microsoft)
  ProviderKey     string
  unique(Provider, ProviderKey)

Device
  Id              Guid        PK
  UserId          Guid        FK → User
  Name            string      (e.g. "Work Laptop Chrome", "iPhone 15")
  Platform        enum        (Windows | Android | iOS | macOS | Web)
  PushToken       string?     (FCM / APNs token for push notifications)
  LastSeenAt      datetime
  CreatedAt       datetime

Message
  Id              Guid        PK
  UserId          Guid        FK → User
  Kind            enum        (Text | Url | File)
  Content         string?     (plaintext body/URL, or base64 AES-256-GCM ciphertext when encrypted)
  FileId          Guid?       FK → FileAttachment
  IsPinned        bool
  IsDeleted       bool        soft-delete flag; default false
  DeletedAt       datetime?   set when moved to Trash, null when active or restored
  IsEncrypted     bool        true when Content is E2EE ciphertext; default false
  EncryptionIV    string?     base64-encoded 96-bit AES-GCM nonce, unique per message
  CreatedAt       datetime
  UpdatedAt       datetime

UserE2eeSettings
  UserId          Guid        PK, FK → User
  IsEnabled       bool        user has opted into E2EE
  KdfAlgorithm    string      key derivation algorithm (Argon2id recommended; PBKDF2-SHA256 fallback)
  KdfSalt         string      base64 random salt (generated client-side, stored server-side)
  KdfParams       string      JSON — algorithm-specific params (iterations, memory, parallelism)
  KeyVerifier     string      base64 — a fixed known plaintext encrypted with the derived key;
                              lets the client confirm the passphrase is correct without
                              transmitting the key or passphrase to the server
  UpdatedAt       datetime

FileAttachment
  Id              Guid        PK
  UserId          Guid        FK → User
  OriginalName    string
  ContentType     string
  SizeBytes       long
  BlobPath        string      (Azure Blob Storage path)
  CreatedAt       datetime

TransferSession
  Id              Guid        PK
  CreatedAt       datetime
  ExpiresAt       datetime    (TTL: 10 minutes)
  ClaimedAt       datetime?
  Content         string?     (payload pushed by source device)

ShareLink
  Id              Guid        PK
  MessageId       Guid        FK → Message
  UserId          Guid        FK → User
  Slug            string      unique, URL-safe random token (e.g. 12 chars)
  ExpiresAt       datetime?   null = never expires
  IsOneTime       bool        if true, link is revoked after the first successful view
  ViewCount       int         how many times the link was opened
  RevokedAt       datetime?   null = still active
  CreatedAt       datetime
```

---

## 5. API Surface

### Auth
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/register` | Email + password registration |
| POST | `/api/auth/login` | Email + password login → JWT |
| GET | `/api/auth/oauth/{provider}` | Redirect to OAuth provider |
| GET | `/api/auth/oauth/{provider}/callback` | OAuth callback → JWT |
| POST | `/api/auth/refresh` | Refresh JWT |

### Messages
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/messages` | List active messages (paged, newest first; `IsDeleted = false`) |
| POST | `/api/messages` | Create text or URL message |
| DELETE | `/api/messages/{id}` | Move message to Trash (`IsDeleted = true`, sets `DeletedAt`) |
| PATCH | `/api/messages/{id}/pin` | Toggle pin |
| POST | `/api/messages/{id}/share` | Generate a share link → `{ url, slug, expiresAt }` |
| DELETE | `/api/messages/{id}/share` | Revoke the share link |

### Trash
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/trash` | List trashed messages (paged, `IsDeleted = true`) |
| POST | `/api/trash/{id}/restore` | Restore message (`IsDeleted = false`, clears `DeletedAt`) |

### Share Links (public, no auth)
| Method | Path | Description |
|--------|------|-------------|
| GET | `/s/{slug}` | View shared note in browser (HTML page, no login required). If `IsOneTime = true`, the link is atomically revoked on first read |

### Files
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/files` | Upload file (multipart) |
| GET | `/api/files/{id}` | Download file (redirect to SAS URL) |
| DELETE | `/api/files/{id}` | Delete file + blob |

### Devices
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/devices` | List registered devices |
| DELETE | `/api/devices/{id}` | Remove device |
| POST | `/api/devices/pair-code` | Generate device-pairing QR token |
| POST | `/api/devices/claim` | Claim device via QR token |

### Transfer
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/transfer/session` | Create anonymous transfer session → session ID |
| POST | `/api/transfer/push` | Push content into a transfer session |

### E2EE
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/e2ee/settings` | Get current user's KDF salt, params, key verifier, and `isEnabled` flag |
| POST | `/api/e2ee/enable` | Store KDF salt + params + key verifier; mark `IsEnabled = true`. Passphrase **never sent to server** — only derived artefacts |
| POST | `/api/e2ee/disable` | Clear E2EE settings; client must re-upload all messages as plaintext first |
| PUT | `/api/e2ee/change-passphrase` | Replace KDF salt + params + key verifier after client re-encrypts all messages with new key |

### SignalR Hub: `/hubs/messages`
| Event (server → client) | Payload |
|--------------------------|---------|
| `MessageCreated` | `Message` |
| `MessageDeleted` | `{ id }` |
| `TransferReceived` | `{ sessionId, content }` || `ShareLinkCreated` | `{ messageId, url }` |
| `ShareLinkRevoked` | `{ messageId }` |
| `MessageTrashed` | `{ id }` |
| `MessageRestored` | `Message` |
| `E2eeSettingsChanged` | `{ isEnabled }` |
---

## 6. Authentication & Security

- Share link slugs are cryptographically random (CSPRNG, 12 chars, ~72 bits of entropy) — not guessable by enumeration.
- Share links expose only the message content, never the owner's identity or other messages.
- One-time links are revoked atomically on first read using an optimistic concurrency update (`UPDATE ... WHERE RevokedAt IS NULL`) — a race condition between two simultaneous requests cannot result in both succeeding.
- Messages are **never hard-deleted**. `DELETE /api/messages/{id}` sets `IsDeleted = true` and `DeletedAt = now`. No row or blob is removed. This protects against accidental data loss and simplifies audit trails.
- Share links whose parent message is in Trash are treated as revoked until the message is restored.
- All message list queries filter on `IsDeleted = false` by default; the Trash endpoint explicitly filters on `IsDeleted = true`.
- File messages shared via link redirect to a fresh short-lived SAS URL on each view — the slug itself does not embed storage credentials.
- JWTs are short-lived (15 min access + 7 day refresh, stored in secure storage / HttpOnly cookie for web).
- OAuth flows use PKCE. State parameter prevents CSRF.
- File downloads use time-limited Azure Blob SAS URLs (never expose the raw storage connection string to clients).
- Transfer sessions expire after 10 minutes and are single-use.
- Pairing QR tokens expire after 5 minutes and are signed JWTs verified server-side.
- All user data is scoped by `UserId` — no cross-user data access is possible at the repository layer.
- Azure Key Vault holds all secrets; Aspire wires them via `IConfiguration` in production.

---

## 7. Azure Deployment Topology

> This is the recommended production path. For early development or self-hosted deployments see **Section 8** instead.

```
┌─────────────────────────────────────────────────────┐
│  Azure Resource Group: rg-savedmessages-prod        │
│                                                     │
│  ┌───────────────────────────────────────────────┐  │
│  │  Azure Container Apps Environment             │  │
│  │  ┌─────────────────┐  ┌────────────────────┐  │  │
│  │  │  API Container  │  │  Web (WASM) static │  │  │
│  │  │  (ASP.NET Core) │  │  Azure Static Web  │  │  │
│  │  │                 │  │  Apps              │  │  │
│  │  └────────┬────────┘  └────────────────────┘  │  │
│  └───────────┼───────────────────────────────────┘  │
│              │                                      │
│  ┌───────────▼───────────────────────────────────┐  │
│  │  Azure SQL Database                           │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │  Azure Blob Storage (files)                   │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │  Azure SignalR Service                        │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │  Azure Key Vault                              │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │  Azure Monitor + Application Insights         │  │
│  │  (fed by Aspire OpenTelemetry defaults)       │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

- **API**: Azure Container Apps (auto-scales to zero when idle, cost-efficient).
- **Web frontend**: Azure Static Web Apps (free tier eligible, global CDN).
- **MAUI apps**: distributed via Microsoft Store (Windows), Google Play (Android), App Store (iOS/macOS).
- **CI/CD**: GitHub Actions — build, test, push container image, deploy to Container Apps.

---

## 9. Self-Hosted Linux Deployment

All Azure managed services have drop-in self-hosted equivalents. No application code changes are required — only connection strings and Aspire resource registrations differ.

| Azure Service | Self-hosted equivalent | Notes |
|---|---|---|
| Azure SQL Database | **PostgreSQL** | Switch EF Core provider to `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Azure Blob Storage | **MinIO** | S3-compatible API; use `AWSSDK.S3` or MinIO .NET SDK; pre-signed URLs replace SAS URLs |
| Azure SignalR Service | **ASP.NET Core SignalR in-process** | No external service needed at single-instance scale; add a **Redis backplane** when scaling to multiple containers |
| Azure Key Vault | **Environment variables / `.env` file** | Use HashiCorp Vault or Doppler for a managed secrets experience |
| Azure Static Web Apps | **Nginx** | Serves the Blazor WASM `wwwroot` publish output |
| Azure Monitor / App Insights | **Seq** (simple) or **Grafana + Loki + Tempo** | Aspire's OpenTelemetry output targets either with no code changes |

### Docker Compose layout

```yaml
services:
  api:        # ASP.NET Core 10 Web API
  web:        # Nginx serving Blazor WASM wwwroot
  postgres:   # PostgreSQL 16
  minio:      # MinIO object storage
  redis:      # Redis (SignalR backplane — add when running multiple API replicas)
  seq:        # Structured log viewer (optional)
```

### Topology

```
┌─────────────────────────────────────────────────┐
│  Linux Server (VPS / bare metal)                │
│                                                 │
│  ┌──────────┐   ┌──────────────────────────┐   │
│  │  Nginx   │   │  Nginx (reverse proxy /  │   │
│  │  WASM    │   │  SSL termination)        │   │
│  └──────────┘   └────────────┬─────────────┘   │
│                               │                │
│               ┌───────────────▼─────────────┐  │
│               │  API Container              │  │
│               │  (ASP.NET Core + SignalR)   │  │
│               └──┬────────────┬─────────────┘  │
│                  │            │                │
│  ┌───────────────▼──┐  ┌──────▼────────────┐  │
│  │  PostgreSQL       │  │  MinIO            │  │
│  │  (messages, users)│  │  (file blobs)     │  │
│  └──────────────────┘  └───────────────────┘  │
│  ┌─────────────────────────────────────────┐  │
│  │  Seq  (logs, traces)                    │  │
│  └─────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

### Migration path to Azure

When ready to move to Azure, only the following need to change:
1. Swap the Aspire resource registrations in `AppHost` (`AddPostgres` → `AddAzureSqlDatabase`, `AddMinio` → `AddAzureBlobStorage`, etc.).
2. Point GitHub Actions to `azd deploy` instead of `docker compose up`.
3. Application code, domain logic, and API contracts remain unchanged.

---

## 8. Testing Strategy

### 8.1 Unit Tests (`SavedMessages.UnitTests`)

Framework: **mstests** + **NSubstitute** (mocking) + **FluentAssertions**.

Covers pure logic with no I/O — all dependencies are substituted.

| Area | What is tested |
|------|---------------|
| Domain entities | Field defaults, soft-delete lifecycle, `IsDeleted` / `DeletedAt` invariants |
| `QrCodeService` | Token generation, TTL calculation |
| `TransferSessionService` | Expiry logic, single-use enforcement |
| E2EE key derivation helpers | Argon2id param validation, key verifier generation/check |
| Share link logic | CSPRNG slug length/entropy, one-time revoke state machine |
| JWT helpers | Token issuance claims, expiry, refresh logic |

### 8.2 Integration Tests (`SavedMessages.IntegrationTests`)

Framework: **mstests** + **Aspire test host** (`Aspire.Hosting.Testing`) + **Microsoft.AspNetCore.Mvc.Testing**.

Spins up the real API, an in-process SQL Server (or Testcontainers PostgreSQL), Azurite for blob storage, and in-process SignalR — no mocks at the HTTP boundary.

| Area | What is tested |
|------|---------------|
| Auth endpoints | Register, login, token refresh, OAuth PKCE flow (mocked provider) |
| Messages API | Create, list (pagination, newest-first), soft-delete, pin, Trash/restore |
| Files API | Multipart upload, download SAS redirect, delete |
| Devices API | List, remove, QR pair-code + claim round-trip |
| Transfer API | Session create → push → SignalR event received on target connection |
| E2EE API | Enable/disable/change-passphrase; passphrase never appears in request log |
| Share links | Generate slug, public `GET /s/{slug}` view, one-time atomic revoke race condition |
| SignalR hub | `MessageCreated` / `MessageTrashed` / `MessageRestored` pushed to correct user group |
| Security | Cross-user data isolation (userId scoping), expired transfer session rejection |

### 8.3 Conventions

- Test projects live under `tests/`, never inside `src/`.
- Each test class maps 1-to-1 to the class under test (e.g. `MessagesControllerTests`).
- Integration tests use a shared `WebApplicationFactory<Program>` fixture to avoid cold-start overhead per test.
- The CI pipeline runs unit tests first (fast gate), then integration tests (slower, needs containers).
- Test database is reset between test classes using `Respawn`.

---

## 9. Technology Decision Notes

### Why .NET MAUI + Blazor Hybrid for native apps?
- One codebase builds for Windows, Android, iOS, and macOS.
- Blazor Hybrid shares UI components directly with the Blazor WASM web app — no duplication.
- The whole stack stays in .NET, reducing context switching.
- MAUI gives access to native APIs (camera for QR, push notifications, secure credential storage, share sheet).

### Why Blazor WebAssembly (not server-side Blazor) for the web?
- Works offline as a PWA once loaded — important for the "open a page to receive a transfer" use case where connectivity may be intermittent.
- No persistent server-side circuit needed for the web client; SignalR is used only for the real-time push channel.

### Why Azure Container Apps (not App Service)?
- Natively integrates with .NET Aspire's deployment model (`azd` / Aspire manifest).
- Scale-to-zero reduces cost for a personal-scale service.

### Why Azure SignalR Service?
- Offloads WebSocket connection management; Container Apps instances remain stateless.
- Aspire has a first-class integration component for it.

### Why client-side key derivation for E2EE (not server-managed keys)?
- The server operator (or an attacker with database access) cannot decrypt messages even with full DB access.
- Argon2id is memory-hard, making brute-force attacks against the passphrase expensive.
- The `KeyVerifier` pattern lets clients validate a passphrase locally without a server round-trip that would expose the key.
