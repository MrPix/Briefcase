# Briefcase

A cross-platform personal clipboard and message relay service — send text, links, and files between any of your devices instantly, without relying on a third-party messenger.

## The Problem

Most major messengers — Telegram, WhatsApp, Signal, Viber, and others — now offer a "Saved Messages" or "Note to Self" feature as a personal relay: paste a URL on a PC, read it on a phone. But all of them require the same app to be installed and signed in on every device, which is often inconvenient (work laptop policies, shared tablets, car head units, etc.). Switching messengers just to share a link is friction you shouldn't have, and existing clipboard tools either cover only some platforms, require complex setup, or come with unnecessary social features.

## The Solution

Briefcase is a focused, self-hosted-friendly service that does one thing well: move your own content between your own devices. It is intentionally minimal — no social feed, no contacts, no distractions.

## Key Features

| Feature | Description |
|---|---|
| **Clip & Send** | Save text snippets, URLs, and files from any device |
| **Real-time sync** | Changes appear on all signed-in devices instantly via SignalR |
| **Quick Transfer QR** | Open a web page on the target device → scan a QR code from the source → content arrives immediately, no app install needed |
| **Quick Device Add QR** | Pair a new device to your account by scanning a single QR code |
| **OAuth Sign-in** | Google, Facebook, Apple, Microsoft — no new password required |
| **Email / password** | Traditional registration also supported |
| **File support** | Upload and download files up to a configurable size limit |
| **Share Link** | Generate a public link for any note — anyone with the link can view it in a browser, no account required. Links can be set to expire, revoked at any time, or marked as **one-time** (automatically invalidated after the first open) |
| **Trash & Restore** | Deleted messages go to Trash and can be restored at any time. Content is never permanently removed from the database or storage — `IsDeleted` is a soft flag |
| **End-to-End Encryption** | Optional per-user E2EE using AES-256-GCM. The encryption key is derived client-side from a passphrase and never leaves the device. Can be disabled globally by the server operator or toggled on/off by each user independently |

## Supported Platforms

- **Windows** — native WinUI application (.NET MAUI)
- **Android** — phones and tablets (.NET MAUI)
- **iOS / iPhone** — native app (.NET MAUI)
- **macOS / MacBook** — native app (.NET MAUI)
- **Web** — Blazor WebAssembly PWA, works in any browser (no install required — ideal for work laptops, car head units, smart TVs)

## Technology Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core 10 Web API |
| Orchestration | .NET Aspire |
| Real-time | ASP.NET Core SignalR → Azure SignalR Service (prod) |
| Auth | ASP.NET Core Identity + OAuth 2.0 / OIDC (Google, Facebook, Apple, Microsoft) |
| Database | Entity Framework Core + Azure SQL Database |
| File storage | Azure Blob Storage |
| Native apps | .NET MAUI + Blazor Hybrid |
| Web app | Blazor WebAssembly (PWA) |
| Shared UI | Razor Component Library (shared between MAUI and WASM) |
| Cloud | Microsoft Azure |
| Secrets | Azure Key Vault |

## Project Structure

```
Briefcase/
├── src/
│   ├── Briefcase.AppHost/          # .NET Aspire host
│   ├── Briefcase.ServiceDefaults/  # Aspire shared defaults (telemetry, health checks)
│   ├── Briefcase.ApiService/       # ASP.NET Core Web API
│   ├── Briefcase.Domain/           # Domain models, interfaces
│   ├── Briefcase.Infrastructure/   # EF Core, Azure integrations
│   ├── Briefcase.Components/       # Shared Razor component library
│   ├── Briefcase.Web/              # Blazor WebAssembly PWA
│   └── Briefcase.Maui/             # .NET MAUI + Blazor Hybrid (Win/Android/iOS/macOS)
└── docs/
    └── ARCHITECTURE.md
```

## Getting Started

> Prerequisites: .NET 10 SDK, Azure subscription (or local emulators via Aspire), Node.js (for Tailwind/CSS tooling if used).

```bash
git clone https://github.com/you/Briefcase.git
cd Briefcase
dotnet run --project src/Briefcase.AppHost
```

Aspire will launch the API, database migrations, and the web frontend. Navigate to the Aspire dashboard URL printed in the console to see all running services.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full system design, data models, API surface, and deployment topology.

Two deployment targets are documented:
- **Azure** — Container Apps, Azure SQL, Blob Storage, SignalR Service (recommended for production scale-out)
- **Self-hosted Linux** — Docker Compose, PostgreSQL, MinIO, in-process SignalR (recommended starting point)

## License

[MIT](LICENSE)
