# ResolutionGuard.NuGet

![ResolutionGuard.NuGet Icon](https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/develop/assets/icon.png)

Japanese README: [README.ja.md](README.ja.md)

`ResolutionGuard.NuGet` is an MSBuild Task package that inspects restored `obj/project.assets.json` files and reports NuGet package-version mismatches across projects.

## Disclaimer

- Unofficial tool for managing NuGet dependency resolution.
- Not affiliated with Microsoft or NuGet.

## Project Philosophy

This project focuses on practical collaboration and respectful technical discussion.
Contributions are welcome, while final decisions remain with the maintainers.

## Why

NuGet dependency resolution is performed per entry project. A solution can silently resolve different versions of the same package in different startup points, even when NU1605/NU1107 does not fail the build. This package detects that situation after restore.

## Security and behavior

- This package **modifies build behavior** via `.props/.targets`.
- The task runs `AfterTargets="Build"` when enabled.
- The task only reads local files (`**/obj/project.assets.json`, optional `nuget-resolution-guard.json`, optional `.sln`/`.slnx`).
- The task does **not** execute external commands and does **not** perform network calls.
- Default behavior is opt-in: `ResolutionGuardNuGetEnabled=false` unless explicitly enabled.

## Requirements

- SDK-style projects using `dotnet restore/build` (cross-platform)

## Validated Environments

- .NET SDK 8.0.x
- .NET SDK 10.0.x

## Install

```xml
<ItemGroup>
  <PackageReference Include="ResolutionGuard.NuGet" Version="0.1.0" PrivateAssets="all" />
</ItemGroup>
```

## Enable

Set in `Directory.Build.props` (or CI properties):

```xml
<Project>
  <PropertyGroup>
    <ResolutionGuardNuGetEnabled>true</ResolutionGuardNuGetEnabled>
  </PropertyGroup>
</Project>
```

## Configuration file

Default search file name: `nuget-resolution-guard.json` (searches from project directory upward).

You can override with MSBuild property:

```xml
<ResolutionGuardNuGetConfigFile>path/to/nuget-resolution-guard.json</ResolutionGuardNuGetConfigFile>
```

Example:

```json
{
  "mode": "warning",
  "scope": "repository",
  "directOnly": false,
  "runtimeOnly": false,
  "excludeEntrypoints": [
    "src/ExperimentalApp/ExperimentalApp.csproj"
  ],
  "excludePackageIds": [
    "Microsoft.NETCore.App",
    "Microsoft.AspNetCore.App"
  ],
  "rules": [
    { "packageId": "Newtonsoft.Json", "mode": "error" },
    { "packageId": "Plugin.SDK", "mode": "error", "versions": ["1.2.0", "1.3.0"] }
  ]
}
```

Notes:

- By default, all discovered projects and package IDs are analyzed.
- `scope` is optional (`repository` by default). Set `solution` to limit analysis to projects listed in the current solution file.
- `directOnly` is optional (`false` by default). When `true`, only directly referenced packages are analyzed.
- `runtimeOnly` is optional (`false` by default). When `true`, only packages with runtime assets are analyzed.
- `excludeEntrypoints` is an entry-project exclude-list (blacklist).
- `excludePackageIds` is package exclude-list (blacklist).
- `rules[].mode` overrides the global `mode` for that package ID.
- `rules[].versions` is optional. Listed versions are treated as allowed, and any other resolved version follows `rules[].mode`.
- When `scope=solution`, the task uses `$(SolutionPath)` by default. If no solution file is available, it logs a warning and falls back to `repository`.

## MSBuild properties

- `ResolutionGuardNuGetEnabled` (`true|false`)
- `ResolutionGuardNuGetEmitSuccessMessage` (`true|false`, default `false`; when `true`, logs a success message if no mismatch is found)
- `ResolutionGuardNuGetConfigFile`
- `ResolutionGuardNuGetMode` (`off|info|warning|error`)
- `ResolutionGuardNuGetScope` (`repository|solution`)
- `ResolutionGuardNuGetDirectOnly` (`true|false`)
- `ResolutionGuardNuGetRuntimeOnly` (`true|false`)
- `ResolutionGuardNuGetSolutionFile` (`.sln` / `.slnx` path override)
- `ResolutionGuardNuGetExcludedEntrypoints` (`;` separated csproj paths to exclude)
- `ResolutionGuardNuGetExcludedPackageIds` (`;` separated package ids to exclude)
- `ResolutionGuardNuGetRepositoryRoot` (search root override)

## Output behavior

- `error`: build fails (`Log.LogError`)
- `warning`: warning only (`Log.LogWarning`)
- `info`: message only (`Log.LogMessage`)
- `off`: no output for that package
- If `ResolutionGuardNuGetEmitSuccessMessage=true`, a success message is logged when analysis completes with no mismatch.

## Documentation

- Japanese user guide: `README.ja.md`
- Contributing guide: `CONTRIBUTING.md`
- Code of Conduct: `CODE_OF_CONDUCT.md`
- Development guide: `docs/development.md`
- Trusted Publishing guide: `docs/trusted-publishing.md`
- Security policy: `SECURITY.md`
- Support policy: `.github/SUPPORT.md`
- Changelog: `CHANGELOG.md`

## License

- This project is licensed under the MIT License. See `LICENSE` for the full license text.
- Japanese license translation is available in `LICENSE.ja.md`.
- Third-party license notices are listed in `THIRD-PARTY-NOTICES.md`.
