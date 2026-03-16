# Trusted Publishing Guide

This document describes the current release model for publishing `ResolutionGuard.NuGet`.

## Recommended approach

Use NuGet Trusted Publishing with GitHub Actions and OpenID Connect instead of long-lived API keys.

## Setup checklist

1. Reserve or create the `ResolutionGuard.NuGet` package on nuget.org.
2. Reserve or create the `ResolutionGuard.NuGet` package on `int.nugettest.org` if `develop` tag publishes should push there.
3. Add this GitHub repository as a Trusted Publisher for the nuget.org package.
4. Add this GitHub repository as a Trusted Publisher for the `int.nugettest.org` package if `develop` publishes are enabled.
5. Register the workflow file name `publish.yml` in the Trusted Publishing policy. Do not include the `.github/workflows/` prefix.
6. Configure the repository secret `NUGET_USER` with the account name that can publish the package.
7. Ensure `.github/workflows/publish.yml` keeps `permissions.id-token: write`.
8. Create and push a release tag using the format `v<major>.<minor>.<patch>`, for example `v1.2.3`.
9. Package and assembly versions are resolved from git tags by `RelaxVersioner`.

## Workflow behavior

- `.github/workflows/publish.yml` handles tag-based publishing.
- The publish destination is selected from tagged-commit ancestry:
  - commit reachable from `main`: publishes to `https://api.nuget.org/v3/index.json`
  - commit reachable from `develop` and not from `main`: publishes to `https://apiint.nugettest.org/v3/index.json`
- The workflow authenticates via `NuGet/login@v1`.
- nuget.org uses `audience=https://www.nuget.org` and `token-service-url=https://www.nuget.org/api/v2/token`.
- `int.nugettest.org` uses `audience=https://int.nugettest.org` and `token-service-url=https://int.nugettest.org/api/v2/token`.
- The feed host (`apiint.nugettest.org`) and OIDC token host (`int.nugettest.org`) differ for the test feed.
- Build runs before publish-time pack.
- The publish-time pack step disables `RelaxVersioner` working-directory dirty checks so generated build outputs do not silently bump the package version.
- The workflow verifies that the generated `.nupkg` filename matches the release tag version before upload.

## Stable release flow

Use this sequence for a nuget.org release such as `v1.3.0`.

1. Ensure the intended release content is merged into `develop`.
2. Move release-visible notes from `Unreleased` to the concrete version section in `CHANGELOG.md`.
3. Update the README install snippet to the release version.
4. Open and merge the release pull request from `develop` to `main`.
5. Create the release tag from the merge commit on `main`, for example `git tag v1.3.0` followed by `git push origin v1.3.0`.
6. Confirm that the publish workflow resolves the package version from the tag and targets nuget.org.

## Release checklist

- `CHANGELOG.md` updated
- `README.md` and `README.ja.md` install snippets updated to the release version
- English and Japanese docs reviewed for sync
- `THIRD-PARTY-NOTICES.md` updated when dependency changes are included
- release tag created from the intended publish branch
- publish target confirmed (`main` for nuget.org or `develop` for `apiint.nugettest.org`)
