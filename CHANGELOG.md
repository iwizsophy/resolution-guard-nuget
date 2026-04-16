# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [Unreleased]

### Added

- _None_

### Changed

- Develop-tag publishes now skip `CHANGELOG.md` release-note extraction and GitHub Release creation; those checks run only for `main` publishes.

### Fixed

- Packed NuGet artifacts no longer expose `nuget-resolution-guard.schema.json` to consuming projects as a restore-generated `contentFiles` link; the schema remains bundled in the `.nupkg` for offline inspection.

### Removed

- _None_

## [1.4.0] - Released

### Added

- Packed NuGet artifacts now include a Syft-generated SPDX SBOM at `sbom/ResolutionGuard.NuGet.spdx.json`.

### Changed

- Packed NuGet artifacts now include `THIRD-PARTY-NOTICES.md` so the
  distributed package carries the repository's third-party notice inventory.

### Fixed

- _None_

### Removed

- _None_

## [1.3.0] - Released

### Added

- CI smoke-test coverage now validates build-time task behavior against .NET SDK / MSBuild hosts for .NET 8, 9, and 10.

### Changed

- The smoke-test project now multi-targets `net8.0`, `net9.0`, and `net10.0`, and its integration fixtures generate projects and `project.assets.json` content for the current test target framework instead of assuming `net8.0`.
- README and development guides now distinguish the shipped package target (`netstandard2.0`) from the repository's current validated SDK / host coverage (`8 / 9 / 10`).
- Dependabot version-update PR creation is now disabled so the repository only relies on Dependabot security updates.

### Fixed

- `netstandard2.0` analyzer code now avoids newer string APIs that prevented builds under .NET SDK 8 / 9 hosts.

### Removed

- _None_

## [1.2.0] - Released

### Added

- Repository governance files for issue templates, pull request guidance, CODEOWNERS, and weekly Dependabot update proposals targeting `develop`.

### Changed

- README and development guides now describe supported environments in terms of `netstandard2.0` MSBuild task compatibility instead of listing a narrow validated SDK matrix.
- README links were aligned for NuGet rendering so image and cross-language references use absolute URLs.
- Contributing and trusted-publishing documentation now documents branch flow, documentation/schema sync expectations, dependency notice updates, and the current Trusted Publishing setup more explicitly.

### Fixed

- Publish-time packing now disables RelaxVersioner's working-directory dirty check so generated build outputs do not silently bump the package version away from the release tag.

### Removed

- _None_

## [1.1.0] - Released

### Added

- Opt-in `solution` scope to limit analysis to projects listed in a `.sln` or `.slnx`, with automatic fallback to repository scope when no solution file is available.
- Opt-in `ResolutionGuardNuGetEmitSuccessMessage` MSBuild property to emit a visible success message when analysis completes with no mismatch.

### Changed

- _None_

### Fixed

- _None_

### Removed

- _None_

## [1.0.0] - Released

### Added

- Initial implementation of `ResolutionGuard.NuGet` as an MSBuild task package that inspects restored `project.assets.json` files and reports NuGet package-version mismatches across entry projects.
- Configuration support via `nuget-resolution-guard.json` and MSBuild properties, including severity control, `directOnly` / `runtimeOnly` filters, exclude lists, and per-package rules.
- NuGet package assets for `build` and `buildTransitive`, plus bundled schema, README, and icon files so the guard can be consumed as a reusable package.
- Basic automated test coverage for the analyzer and task behavior to support the first public release.

### Changed

- Repository foundations were prepared for the first release, including CI, trusted publishing workflow, and development / release documentation.
- Package metadata and project configuration were aligned for the `1.0.0` release.

### Fixed

- _None_

### Removed

- _None_

[1.1.0]: https://github.com/iwizsophy/resolution-guard-nuget/releases/tag/v1.1.0
[1.0.0]: https://github.com/iwizsophy/resolution-guard-nuget/releases/tag/v1.0.0
[1.2.0]: https://github.com/iwizsophy/resolution-guard-nuget/releases/tag/v1.2.0
[1.3.0]: https://github.com/iwizsophy/resolution-guard-nuget/releases/tag/v1.3.0
[1.4.0]: https://github.com/iwizsophy/resolution-guard-nuget/releases/tag/v1.4.0
