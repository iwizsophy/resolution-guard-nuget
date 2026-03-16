# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

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
