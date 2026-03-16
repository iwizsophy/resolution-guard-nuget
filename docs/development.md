# Development Guide

This document is for maintainers and contributors.

## Environment assumptions

`ResolutionGuard.NuGet` is distributed as an MSBuild task package targeting `netstandard2.0`.

Development and validation in this repository assume an SDK-style .NET build environment that:
- can run `dotnet restore`, `dotnet build`, and `dotnet pack`
- can consume packages whose task assemblies target `netstandard2.0`

Current CI validates build-time task behavior with .NET SDK / MSBuild hosts from .NET 8, 9, and 10.

That validation scope is narrower than a full IDE support matrix and should be described as current smoke-test coverage, not an exhaustive compatibility promise.

## Build and test

```powershell
dotnet restore ResolutionGuard.NuGet.slnx
dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net8.0 --no-build
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net9.0 --no-build
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net10.0 --no-build
```

To reproduce the CI host-validation job for a single SDK / MSBuild host on a clean tree, run:

```powershell
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net8.0 -p:ResolutionGuardTestTargetFrameworks=net8.0
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net9.0 -p:ResolutionGuardTestTargetFrameworks=net9.0
dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net10.0 -p:ResolutionGuardTestTargetFrameworks=net10.0
```

`ResolutionGuardTestTargetFrameworks` is a test-only property that narrows the smoke-test project to one target framework and isolates its restore assets for host-specific validation.

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
