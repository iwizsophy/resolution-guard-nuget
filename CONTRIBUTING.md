# Contributing

Thanks for your interest in contributing to ResolutionGuard.NuGet.

## Before you start

- Open an issue for bug reports, feature proposals, user-visible behavior changes, configuration/schema changes, publish-workflow changes, or dependency additions and major dependency updates.
- Keep changes focused and small when possible.
- The default branch is `main`.
- The integration branch is `develop`.
- The standard branch flow is `feature/* -> develop`, `bugfix/* -> develop`, `chore/* -> develop`, and `develop -> main` for releases.
- Prefer pull requests for changes that reach `develop` or `main`.
- Update English and Japanese documentation together when practical, or create a follow-up issue if one language must lag.
- Changes to the config surface should update `README.md`, `README.ja.md`, and `nuget-resolution-guard.schema.json` together.
- Dependency changes must update `THIRD-PARTY-NOTICES.md` in the same pull request.
- Release-visible changes should update `CHANGELOG.md`.
- While discussion and contributions are welcome, final decisions are made by the maintainers.

## Development workflow

1. Fork and create a topic branch.
2. Open or update an issue when the work changes behavior, release flow, documentation policy, or dependency policy.
3. Implement your change with tests, validation updates, or documentation updates.
4. Run local checks:
   - `dotnet restore ResolutionGuard.NuGet.slnx`
   - `dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore`
   - `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --no-build`
   - `dotnet pack src/ResolutionGuard.NuGet.Package/ResolutionGuard.NuGet.Package.csproj -c Release --no-build -o artifacts`
5. Submit a pull request with:
   - What changed
   - Why it changed
   - Validation results
   - Target branch (`develop` for normal work, `main` only for release PRs from `develop`)

## Release and publishing notes

- Release tags should use the format `v<major>.<minor>.<patch>`.
- Tagged commits reachable from `main` publish to `https://api.nuget.org/v3/index.json`.
- Tagged commits reachable from `develop` and not from `main` publish to `https://apiint.nugettest.org/v3/index.json`.
- Publishing uses GitHub Actions Trusted Publishing with `NuGet/login@v1` and the repository secret `NUGET_USER`.
- The publish-time pack step disables `RelaxVersioner` working-directory dirty checks so generated outputs do not silently bump the package version.
- The publish workflow verifies that the generated `.nupkg` filename matches the release tag version before upload.

## Additional docs

- Development details: `docs/development.md`
- NuGet.org Trusted Publishing: `docs/trusted-publishing.md`
- Code of Conduct: `CODE_OF_CONDUCT.md`
- Support policy: `.github/SUPPORT.md`
