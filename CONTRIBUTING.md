# Contributing

Thanks for your interest in contributing to ResolutionGuard.NuGet.

## Before you start

- Open an issue for bug reports or feature proposals.
- Keep changes focused and small when possible.

## Development workflow

1. Fork and create a topic branch.
2. Implement your change with tests or validation updates.
3. Run local checks:
   - `dotnet restore ResolutionGuard.NuGet.slnx`
   - `dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore`
   - `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release`
4. Submit a pull request with:
   - What changed
   - Why it changed
   - Validation results

## Additional docs

- Development details: `docs/development.md`
- NuGet.org Trusted Publishing: `docs/trusted-publishing.md`
