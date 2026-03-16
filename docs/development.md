# Development Guide

This document is for maintainers and contributors.

## Environment assumptions

`ResolutionGuard.NuGet` is distributed as an MSBuild task package targeting `netstandard2.0`.

Development and validation in this repository assume an SDK-style .NET build environment that:
- can run `dotnet restore`, `dotnet build`, and `dotnet pack`
- can consume packages whose task assemblies target `netstandard2.0`

This repository does not maintain a version-by-version IDE or SDK support matrix.

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
