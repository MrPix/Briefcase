# GitHub CLI script — create all issues for SavedMessages
# Prerequisites:
#   - gh auth login completed
#   - .\create-labels-milestones.ps1 has already been run (labels + milestones must exist)
#
# Usage:  .\create-issues.ps1
#
# Idempotent: skips creation if an open issue with the same title already exists.

$repo = "MrPix/SavedMessages"

# ---------------------------------------------------------------------------
# Helper
# ---------------------------------------------------------------------------

# Fetch all existing open issue titles once to guard against duplicates
$existingIssues = gh api "repos/$repo/issues" --paginate `
    --field "state=open" `
    --field "per_page=100" |
ConvertFrom-Json |
Select-Object -ExpandProperty title

function New-Issue {
    param(
        [string]$Title,
        [string[]]$Labels,
        [string]$Milestone,
        [string]$Body
    )

    if ($existingIssues -contains $Title) {
        Write-Host "  [skip] already exists: $Title" -ForegroundColor Yellow
        return
    }

    Write-Host "  [create] $Title"

    $labelArgs = $Labels | ForEach-Object { "--label", $_ }

    gh issue create `
        --repo $repo `
        --title $Title `
        --body $Body `
        --milestone $Milestone `
        @labelArgs | Out-Null
}

# ---------------------------------------------------------------------------
# MVP
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Creating MVP issues..." -ForegroundColor Cyan

New-Issue `
    -Title "Set up solution structure" `
    -Labels @("type:chore") `
    -Milestone "MVP" `
    -Body "Create \``.slnx\``, all 9 projects, and project references per §2 of ARCHITECTURE.md."

New-Issue `
    -Title "Configure .NET Aspire AppHost" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "MVP" `
    -Body "Wire up API, SQL, Blob, SignalR, and Web resources per §3.1 of ARCHITECTURE.md."

New-Issue `
    -Title "Add Aspire ServiceDefaults" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "MVP" `
    -Body "OpenTelemetry, health checks, and resilience policies per §3.1 of ARCHITECTURE.md."

New-Issue `
    -Title "Scaffold ASP.NET Core API" `
    -Labels @("type:chore", "area:api") `
    -Milestone "MVP" `
    -Body "Add controller stubs, \`\`MessageHub\`\`, \`\`QrCodeService\`\`, \`\`TransferSessionService\`\`; configure JWT middleware per §3.2 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement domain entities" `
    -Labels @("type:chore", "area:infra") `
    -Milestone "MVP" `
    -Body "Implement \`\`User\`\`, \`\`Device\`\`, \`\`Message\`\`, \`\`FileAttachment\`\`, \`\`TransferSession\`\` with all fields per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Configure EF Core + AppDbContext" `
    -Labels @("type:chore", "area:infra") `
    -Milestone "MVP" `
    -Body "Schema, indexes, initial migration, Azure SQL / PostgreSQL provider per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Auth — register & login" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "\`\`POST /api/auth/register\`\`, \`\`/login\`\` → JWT; 15 min access + 7 day refresh per §5, §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement JWT refresh" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "\`\`POST /api/auth/refresh\`\`; HttpOnly cookie for web, secure storage for MAUI per §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement OAuth 2.0 + PKCE" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "Google / Facebook / Apple / Microsoft providers; PKCE + state param per §5, §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Messages API" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "\`\`GET/POST/DELETE/PATCH\`\` per §5; \`\`IsDeleted\`\` soft-delete, \`\`DeletedAt\`\`, pin toggle per §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Trash API" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "\`\`GET /api/trash\`\`, \`\`POST /api/trash/{id}/restore\`\`; filter on \`\`IsDeleted = true\`\` per §5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Files API" `
    -Labels @("type:feature", "area:api", "area:infra") `
    -Milestone "MVP" `
    -Body "Multipart upload, SAS download redirect, delete blob + metadata per §5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Azure Blob Storage service" `
    -Labels @("type:chore", "area:infra") `
    -Milestone "MVP" `
    -Body "\`\`AzureBlobStorageService\`\` against Azurite locally and Azure Blob in prod per §3 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Devices API" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "List, remove, QR pair-code (signed JWT, 5 min TTL), claim per §3.4, §5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Quick Transfer API" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "\`\`POST /api/transfer/session\`\` + \`\`/push\`\`; 10 min expiry, single-use per §3.4, §5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement SignalR MessageHub" `
    -Labels @("type:feature", "area:api") `
    -Milestone "MVP" `
    -Body "User-group push for \`\`MessageCreated\`\`, \`\`MessageTrashed\`\`, \`\`MessageRestored\`\`, \`\`TransferReceived\`\` per §3.3, §5 of ARCHITECTURE.md."

New-Issue `
    -Title "Build shared Razor component library" `
    -Labels @("type:feature", "area:frontend") `
    -Milestone "MVP" `
    -Body "\`\`ClipboardPage\`\`, \`\`TransferPage\`\`, \`\`DevicesPage\`\`, \`\`MessageCard\`\`, \`\`QrScanner\`\`; platform interfaces per §3.5 of ARCHITECTURE.md."

New-Issue `
    -Title "Set up .NET MAUI Blazor Hybrid" `
    -Labels @("type:chore", "area:maui") `
    -Milestone "MVP" `
    -Body "\`\`MauiProgram.cs\`\`, \`\`MainPage.xaml\`\` with \`\`BlazorWebView\`\`, platform folder stubs per §3.6 of ARCHITECTURE.md."

New-Issue `
    -Title "Set up CI/CD pipeline" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "MVP" `
    -Body "GitHub Actions: build, run unit tests, run integration tests, push container image per §7 of ARCHITECTURE.md."

New-Issue `
    -Title "Write unit tests — domain & services" `
    -Labels @("type:test") `
    -Milestone "MVP" `
    -Body "\`\`QrCodeService\`\`, \`\`TransferSessionService\`\`, JWT helpers, soft-delete invariants per §8.1 of ARCHITECTURE.md."

New-Issue `
    -Title "Write integration tests — API" `
    -Labels @("type:test") `
    -Milestone "MVP" `
    -Body "Auth, messages, files, devices, transfer; shared \`\`WebApplicationFactory\`\`; Respawn DB reset per §8.2 of ARCHITECTURE.md."

New-Issue `
    -Title "Deploy API to Azure Container Apps" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "MVP" `
    -Body "Container image, \`\`azd deploy\`\`, scale-to-zero, Key Vault secrets per §7 of ARCHITECTURE.md."

# ---------------------------------------------------------------------------
# Security
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Creating Security issues..." -ForegroundColor Cyan

New-Issue `
    -Title "Implement ShareLink entity + migration" `
    -Labels @("type:chore", "area:infra") `
    -Milestone "Security" `
    -Body "\`\`Slug\`\`, \`\`ExpiresAt\`\`, \`\`IsOneTime\`\`, \`\`ViewCount\`\`, \`\`RevokedAt\`\` fields per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement share link API" `
    -Labels @("type:feature", "area:api") `
    -Milestone "Security" `
    -Body "\`\`POST\`\` / \`\`DELETE /api/messages/{id}/share\`\`; CSPRNG 12-char slug (~72 bits entropy) per §5, §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement public share link view" `
    -Labels @("type:feature", "area:api") `
    -Milestone "Security" `
    -Body "\`\`GET /s/{slug}\`\`; one-time atomic revoke via optimistic concurrency update; file → fresh SAS URL per §5, §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement UserE2eeSettings entity" `
    -Labels @("type:chore", "area:infra", "area:e2ee") `
    -Milestone "Security" `
    -Body "\`\`KdfAlgorithm\`\`, \`\`KdfSalt\`\`, \`\`KdfParams\`\`, \`\`KeyVerifier\`\` fields; migration per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement E2EE API" `
    -Labels @("type:feature", "area:api", "area:e2ee") `
    -Milestone "Security" `
    -Body "\`\`GET/POST /api/e2ee/settings\`\`, \`\`/enable\`\`, \`\`/disable\`\`, \`\`PUT /change-passphrase\`\`; passphrase never sent to server per §5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement client-side Argon2id KDF" `
    -Labels @("type:feature", "area:frontend", "area:e2ee") `
    -Milestone "Security" `
    -Body "Key derivation, AES-256-GCM encrypt/decrypt, KeyVerifier generation and check per §6, §9 of ARCHITECTURE.md."

New-Issue `
    -Title "Store E2EE ciphertext + IV in Message" `
    -Labels @("type:feature", "area:infra", "area:e2ee") `
    -Milestone "Security" `
    -Body "\`\`IsEncrypted\`\`, \`\`EncryptionIV\`\` fields; client encrypts content before \`\`POST /api/messages\`\` per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Write E2EE unit tests" `
    -Labels @("type:test", "area:e2ee") `
    -Milestone "Security" `
    -Body "Argon2id param validation, KDF verifier round-trip, AES-GCM encrypt/decrypt per §8.1 of ARCHITECTURE.md."

New-Issue `
    -Title "Write share link integration tests" `
    -Labels @("type:test") `
    -Milestone "Security" `
    -Body "Public view, one-time atomic revoke race condition, cross-user isolation, trashed-message link rejection per §8.2 of ARCHITECTURE.md."

# ---------------------------------------------------------------------------
# iOS
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Creating iOS issues..." -ForegroundColor Cyan

New-Issue `
    -Title "Configure iOS platform target" `
    -Labels @("type:chore", "area:maui") `
    -Milestone "iOS" `
    -Body "\`\`Info.plist\`\`, entitlements, bundle ID, code signing profile per §3.6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement camera QR scanner (iOS)" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "iOS" `
    -Body "\`\`AVCaptureSession\`\` via MAUI native API; exposed via \`\`IQrScannerService\`\` per §3.5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement APNs push notifications" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "iOS" `
    -Body "Register APNs token on launch; store as \`\`Device.PushToken\`\` per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement iOS secure credential storage" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "iOS" `
    -Body "iOS Keychain for JWT refresh token per §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Publish to App Store" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "iOS" `
    -Body "Archive build, TestFlight internal testing, App Store submission per §7 of ARCHITECTURE.md."

# ---------------------------------------------------------------------------
# Android
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Creating Android issues..." -ForegroundColor Cyan

New-Issue `
    -Title "Configure Android platform target" `
    -Labels @("type:chore", "area:maui") `
    -Milestone "Android" `
    -Body "\`\`AndroidManifest.xml\`\`, camera + internet + notification permissions per §3.6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement camera QR scanner (Android)" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "Android" `
    -Body "CameraX / ZXing via MAUI native API; exposed via \`\`IQrScannerService\`\` per §3.5 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement FCM push notifications" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "Android" `
    -Body "Register FCM token on launch; store as \`\`Device.PushToken\`\` per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Android secure credential storage" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "Android" `
    -Body "Android Keystore / \`\`EncryptedSharedPreferences\`\` for JWT refresh token per §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Publish to Google Play" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "Android" `
    -Body "Signed AAB, internal test track, production release per §7 of ARCHITECTURE.md."

# ---------------------------------------------------------------------------
# Windows
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Creating Windows issues..." -ForegroundColor Cyan

New-Issue `
    -Title "Configure Windows platform target" `
    -Labels @("type:chore", "area:maui") `
    -Milestone "Windows" `
    -Body "\`\`Package.appxmanifest\`\`, identity, capabilities per §3.6 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Windows push notifications" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "Windows" `
    -Body "WNS token registration; store as \`\`Device.PushToken\`\` per §4 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement Windows secure credential storage" `
    -Labels @("type:feature", "area:maui") `
    -Milestone "Windows" `
    -Body "Windows Credential Manager for JWT refresh token per §6 of ARCHITECTURE.md."

New-Issue `
    -Title "Publish to Microsoft Store" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "Windows" `
    -Body "MSIX package, Partner Center submission per §7 of ARCHITECTURE.md."

# ---------------------------------------------------------------------------
# Web
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Creating Web issues..." -ForegroundColor Cyan

New-Issue `
    -Title "Set up Blazor WASM PWA project" `
    -Labels @("type:chore", "area:frontend") `
    -Milestone "Web" `
    -Body "\`\`Program.cs\`\`, service worker, \`\`manifest.json\`\`, PWA installability per §3.7 of ARCHITECTURE.md."

New-Issue `
    -Title "Configure offline PWA support" `
    -Labels @("type:feature", "area:frontend") `
    -Milestone "Web" `
    -Body "Service worker caching strategy; offline placeholder for no-connection state per §3.7, §9 of ARCHITECTURE.md."

New-Issue `
    -Title "Implement SignalR client (Web)" `
    -Labels @("type:feature", "area:frontend") `
    -Milestone "Web" `
    -Body "Real-time \`\`MessageCreated\`\` / \`\`TransferReceived\`\` in browser; auto-reconnect per §3.3 of ARCHITECTURE.md."

New-Issue `
    -Title "Write PWA manifest + icons" `
    -Labels @("type:chore", "area:frontend") `
    -Milestone "Web" `
    -Body "192 px and 512 px icons, \`\`theme_color\`\`, \`\`display: standalone\`\` per §3.7 of ARCHITECTURE.md."

New-Issue `
    -Title "Deploy WASM to Azure Static Web Apps" `
    -Labels @("type:chore", "area:ops") `
    -Milestone "Web" `
    -Body "GitHub Actions publish WASM \`\`wwwroot\`\`; free-tier CDN per §7 of ARCHITECTURE.md."

# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Done! All issues created." -ForegroundColor Green
