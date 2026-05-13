# GitHub CLI script — create all labels and milestones for SavedMessages
# Prerequisites:
#   - gh auth login completed
#
# Usage:  .\create-labels-milestones.ps1
#
# Idempotent: --force on labels updates color/description if the label already exists.
# Milestones: the script skips creation if a milestone with that title already exists.

$repo = "MrPix/SavedMessages"

# ---------------------------------------------------------------------------
# Labels
# ---------------------------------------------------------------------------

$labels = @(
    # Area
    @{ name = "area:api"; color = "0075ca"; description = "ASP.NET Core backend" }
    @{ name = "area:frontend"; color = "d93f0b"; description = "Shared Razor components" }
    @{ name = "area:infra"; color = "e4e669"; description = "EF Core, Azure SDK, storage" }
    @{ name = "area:e2ee"; color = "b60205"; description = "End-to-end encryption" }
    @{ name = "area:ops"; color = "c5def5"; description = "CI/CD, Docker, Aspire" }
    @{ name = "area:maui"; color = "bfd4f2"; description = ".NET MAUI Blazor Hybrid" }

    # Type
    @{ name = "type:feature"; color = "0052cc"; description = "New functionality" }
    @{ name = "type:chore"; color = "cccccc"; description = "Setup, scaffolding, config" }
    @{ name = "type:bug"; color = "d73a4a"; description = "Something is broken" }
    @{ name = "type:test"; color = "0e8a16"; description = "Unit or integration test work" }
)

# ---------------------------------------------------------------------------
# Delete obsolete release:* labels (idempotent — ignores 404s)
# ---------------------------------------------------------------------------
$obsoleteLabels = @("release:mvp", "release:security", "release:ios", "release:android", "release:windows", "release:web")

Write-Host "Deleting obsolete release labels..." -ForegroundColor Cyan

foreach ($label in $obsoleteLabels) {
    Write-Host "  deleting: $label"
    gh label delete $label --repo $repo --yes 2>$null
}

Write-Host ""
Write-Host "Creating labels..." -ForegroundColor Cyan

foreach ($label in $labels) {
    Write-Host "  label: $($label.name)"
    gh label create $label.name `
        --color $label.color `
        --description $label.description `
        --repo $repo `
        --force
}

# ---------------------------------------------------------------------------
# Milestones
# ---------------------------------------------------------------------------

$milestones = @(
    @{ title = "MVP"; description = "Must ship in the first public release" }
    @{ title = "Security"; description = "Security hardening — E2EE, auth, share links" }
    @{ title = "iOS"; description = "iOS / iPhone specific work" }
    @{ title = "Android"; description = "Android specific work" }
    @{ title = "Windows"; description = "Windows desktop specific work" }
    @{ title = "Web"; description = "Blazor WASM / PWA specific work" }
)

Write-Host ""
Write-Host "Creating milestones..." -ForegroundColor Cyan

# Fetch existing milestone titles once to avoid duplicates (gh api — gh has no milestone subcommand)
$existing = gh api "repos/$repo/milestones" --paginate | ConvertFrom-Json | Select-Object -ExpandProperty title

foreach ($ms in $milestones) {
    if ($existing -contains $ms.title) {
        Write-Host "  milestone already exists, skipping: $($ms.title)" -ForegroundColor Yellow
    }
    else {
        Write-Host "  milestone: $($ms.title)"
        gh api "repos/$repo/milestones" `
            --method POST `
            --field "title=$($ms.title)" `
            --field "description=$($ms.description)" | Out-Null
    }
}

Write-Host ""
Write-Host "Done! All labels and milestones are ready." -ForegroundColor Green
Write-Host "You can now run .\create-m1-issues.ps1"
