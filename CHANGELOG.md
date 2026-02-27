# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog, and this project follows Semantic Versioning.

## [1.0.0] - Planned

### Added

- Initial implementation of `ResolutionGuard.NuGet` as an MSBuild task package that inspects restored `project.assets.json` files and reports NuGet package-version mismatches across entry projects.
- Configuration support via `nuget-resolution-guard.json` and MSBuild properties, including severity control, `directOnly` / `runtimeOnly` filters, exclude lists, and per-package rules.
- NuGet package assets for `build` and `buildTransitive`, plus bundled schema, README, and icon files so the guard can be consumed as a reusable package.
- Basic automated test coverage for the analyzer and task behavior to support the first public release.

### Changed

- Repository foundations were prepared for the first release, including CI, trusted publishing workflow, and development / release documentation.
- Package metadata and project configuration were aligned for the planned `1.0.0` release.

### Fixed

- _None_

### Removed

- _None_

[1.0.0]: https://github.com/iwizsophy/resolution-guard-nuget/releases/tag/v1.0.0
