# SavedMessages

A cross-platform personal clipboard and message relay service — send text, links, and files between any of your devices instantly, without relying on a third-party messenger.

## The Problem

Many people use Telegram's "Saved Messages" as a personal relay — paste a URL on a PC, read it on a phone. But that requires Telegram to be installed and signed in on every device, which is often inconvenient (work laptop policies, shared tablets, car head units, etc.). Existing alternatives either cover only some platforms, require complex setup, or come with unnecessary social features.

## The Solution

SavedMessages is a focused, self-hosted-friendly service that does one thing well: move your own content between your own devices. It is intentionally minimal — no social feed, no contacts, no distractions.

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
SavedMessages/
├── src/
│   ├── SavedMessages.AppHost/          # .NET Aspire host
│   ├── SavedMessages.ServiceDefaults/  # Aspire shared defaults (telemetry, health checks)
│   ├── SavedMessages.Api/              # ASP.NET Core Web API
│   ├── SavedMessages.Domain/           # Domain models, interfaces
│   ├── SavedMessages.Infrastructure/   # EF Core, Azure integrations
│   ├── SavedMessages.Components/       # Shared Razor component library
│   ├── SavedMessages.Web/              # Blazor WebAssembly PWA
│   └── SavedMessages.Maui/             # .NET MAUI + Blazor Hybrid (Win/Android/iOS/macOS)
└── docs/
    └── ARCHITECTURE.md
```

## Getting Started

> Prerequisites: .NET 10 SDK, Azure subscription (or local emulators via Aspire), Node.js (for Tailwind/CSS tooling if used).

```bash
git clone https://github.com/you/SavedMessages.git
cd SavedMessages
dotnet run --project src/SavedMessages.AppHost
```

Aspire will launch the API, database migrations, and the web frontend. Navigate to the Aspire dashboard URL printed in the console to see all running services.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full system design, data models, API surface, and deployment topology.

## License

[MIT](LICENSE)
