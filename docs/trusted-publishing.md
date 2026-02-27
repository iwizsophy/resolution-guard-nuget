# Trusted Publishing setup

1. Create package `ResolutionGuard.NuGet` on nuget.org (or reserve the package id).
2. In nuget.org package settings, add a Trusted Publisher for this GitHub repository and workflow (`.github/workflows/publish.yml`).
3. In nugettest.org package settings, add a Trusted Publisher for the same workflow.
4. Ensure `publish.yml` keeps `permissions.id-token: write`.
5. Create and push a version tag (for example: `v1.2.3`).
6. The publish destination is selected from the tagged commit ancestry:
   - commit in `main`: publishes to `https://api.nuget.org/v3/index.json`
   - commit in `develop` (and not in `main`): publishes to `https://apiint.nugettest.org/v3/index.json`
7. Package and assembly versions are resolved from git tags by `RelaxVersioner`.

The workflow authenticates via `NuGet/login@v1` and pushes with a short-lived API key output.
- `main` uses default trusted publishing settings.
- `develop` uses `audience=https://apiint.nugettest.org` and `token-service-url=https://apiint.nugettest.org/v2.0/package/create-verification-key`.
