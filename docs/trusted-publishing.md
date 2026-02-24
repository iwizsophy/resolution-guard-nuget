# Trusted Publishing setup

1. Create package `ResolutionGuard.NuGet` on nuget.org (or reserve the package id).
2. In nuget.org package settings, add a Trusted Publisher for this GitHub repository and workflow (`.github/workflows/publish.yml`).
3. Ensure `publish.yml` keeps `permissions.id-token: write`.
4. Release by creating a git tag, e.g. `v1.0.0`.

The workflow authenticates via `NuGet/login@v1` and pushes with a short-lived API key output.
