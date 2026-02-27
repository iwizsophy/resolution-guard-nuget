# Development Guide

This document is for maintainers and contributors.

## Prerequisites

- .NET SDK installed
- Validated SDK versions in this repository:
  - 8.0.x
  - 10.0.x

## Build and test

```powershell
dotnet restore ResolutionGuard.NuGet.slnx
dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release
```

## Pack locally

```powershell
dotnet pack src/ResolutionGuard.NuGet.Package/ResolutionGuard.NuGet.Package.csproj -c Release --no-restore -o artifacts
```

## Project layout

- `src/ResolutionGuard.NuGet.Core`: analysis and config resolution logic
- `src/ResolutionGuard.NuGet.Tasks`: MSBuild Task implementation
- `src/ResolutionGuard.NuGet.Package`: NuGet packaging (`build`, `buildTransitive`, task binaries)
- `tests/ResolutionGuard.NuGet.Tests`: smoke tests
- `docs/`: contributor and publishing documentation

## Release

For NuGet.org publishing, follow `docs/trusted-publishing.md`.
