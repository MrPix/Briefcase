# How to Release

Releases are managed via GitHub's release interface. There are no special release branches — all releases are cut from `main`.

## Prerequisites

- Your changes are merged into `main` and CI is green.
- You have write access to the repository.

## Steps

1. **Go to the Releases page**
   Navigate to [Releases](../../releases) → click **Draft a new release**.

2. **Create a tag**
   In the *Choose a tag* field, type the new version following [Semantic Versioning](https://semver.org/), prefixed with `v`:
   ```
   v1.2.3
   ```
   Select **+ Create new tag: v1.2.3 on publish** and target the `main` branch.

3. **Set the release title**
   Use the version number as the title, e.g. `v1.2.3`.

4. **Write release notes**
   Describe what changed — new features, bug fixes, and breaking changes.
   GitHub's **Generate release notes** button can auto-populate this from merged PRs.

5. **Publish the release**
   Click **Publish release** (or **Save draft** to review later).

## What happens automatically

Publishing the release triggers the [Release workflow](../.github/workflows/release.yml), which:

- Builds and tests the solution (`dotnet build` / `dotnet test`)
- Extracts the version from the tag (e.g. `v1.2.3` → `1.2.3`)
- Builds and pushes Docker images to GHCR tagged with:
  - `1.2.3` — exact version
  - `1.2.3-<run_number>` — version + build number
  - `latest` — always points to the most recently published release

## Docker image tags summary

| Tag | Updated on |
|---|---|
| `latest` | Every published release |
| `1.2.3` | Published release `v1.2.3` |
| `1.2.3-42` | Published release `v1.2.3`, build run 42 |
| `edge` | Every push to `main` (development builds) |
| `sha-<commit>` | Every push to `main` |

## Version format

Versions follow [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`

| Increment | When |
|---|---|
| `MAJOR` | Breaking changes |
| `MINOR` | New backward-compatible features |
| `PATCH` | Backward-compatible bug fixes |
