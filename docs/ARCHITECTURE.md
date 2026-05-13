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
  Content         string?     (text body or URL)
  FileId          Guid?       FK → FileAttachment
  IsPinned        bool
  CreatedAt       datetime
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
| GET | `/api/messages` | List messages (paged, newest first) |
| POST | `/api/messages` | Create text or URL message |
| DELETE | `/api/messages/{id}` | Delete message |
| PATCH | `/api/messages/{id}/pin` | Toggle pin |

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

### SignalR Hub: `/hubs/messages`
| Event (server → client) | Payload |
|--------------------------|---------|
| `MessageCreated` | `Message` |
| `MessageDeleted` | `{ id }` |
| `TransferReceived` | `{ sessionId, content }` |

---

## 6. Authentication & Security

- JWTs are short-lived (15 min access + 7 day refresh, stored in secure storage / HttpOnly cookie for web).
- OAuth flows use PKCE. State parameter prevents CSRF.
- File downloads use time-limited Azure Blob SAS URLs (never expose the raw storage connection string to clients).
- Transfer sessions expire after 10 minutes and are single-use.
- Pairing QR tokens expire after 5 minutes and are signed JWTs verified server-side.
- All user data is scoped by `UserId` — no cross-user data access is possible at the repository layer.
- Azure Key Vault holds all secrets; Aspire wires them via `IConfiguration` in production.

---

## 7. Azure Deployment Topology

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

## 8. Technology Decision Notes

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
