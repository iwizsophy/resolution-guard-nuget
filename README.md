# ResolutionGuard.NuGet

![ResolutionGuard.NuGet Icon](https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/assets/icon.png)

Japanese README: [README.ja.md](https://github.com/iwizsophy/resolution-guard-nuget/blob/main/README.ja.md)

`ResolutionGuard.NuGet` is an MSBuild Task package that inspects restored `obj/project.assets.json` files and reports NuGet package-version mismatches across projects.

## Disclaimer

- Unofficial tool for managing NuGet dependency resolution.
- Not affiliated with Microsoft or NuGet.

## Project Philosophy

This project focuses on practical collaboration and respectful technical discussion.
Contributions are welcome, while final decisions remain with the maintainers.

## Why

NuGet dependency resolution is performed independently for each entry project. Even within the same solution, different startup points can therefore resolve different versions of the same package.

This package came out of work on a large multi-project solution where multiple applications could be built from the same codebase.

After updating references in parent projects, most applications picked up the expected transitive package versions. Some applications, however, were left behind and continued resolving problematic package versions that were still pinned through child projects.

As a result, errors that should already have been fixed started showing up again.

These issues are hard to notice as long as the build still succeeds. They tend to remain latent as "only some applications still use the old dependency graph." Existing NuGet warnings such as `NU1605` and `NU1107` also do not necessarily detect these cross-project resolution mismatches.

In normal Visual Studio workflows, package-version inconsistencies also do not always surface as explicit errors or warnings, so partial update misses often depend on manual verification.

That verification does not scale well in solutions with many projects and multiple entry points.

This tool was created from the need to detect, during build, which versions were actually resolved across projects.

`ResolutionGuard.NuGet` analyzes restored `project.assets.json` files across the repository and reports mismatches in package resolution results between projects.

It does not change NuGet dependency resolution itself. Its purpose is to detect differences based on the fact of what was actually resolved.

## Security and behavior

- This package **modifies build behavior** via `.props/.targets`.
- The task runs `AfterTargets="Build"` when enabled.
- The task only reads local files (`**/obj/project.assets.json`, optional `nuget-resolution-guard.json`, optional `.sln`/`.slnx`).
- The task does **not** execute external commands and does **not** perform network calls.
- Default behavior is opt-in: `ResolutionGuardNuGetEnabled=false` unless explicitly enabled.

## Supported environments

`ResolutionGuard.NuGet` is distributed as an MSBuild task package targeting `netstandard2.0`.

It is intended for SDK-style .NET build environments that:
- support MSBuild task execution
- can consume packages whose task assemblies target `netstandard2.0`

Current CI validates build-time task behavior with .NET SDK / MSBuild hosts from .NET 8, 9, and 10.

That validation scope describes the repository's current smoke-test coverage. It is not an exhaustive IDE or SDK support matrix.

Common SDK-style project types such as `.csproj`, `.fsproj`, and `.vbproj` are within the current support policy.

## Install

```xml
<ItemGroup>
  <PackageReference Include="ResolutionGuard.NuGet" Version="1.5.0" PrivateAssets="all" />
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

Recommended editor integration:

- Add `$schema` to the config file and point it at the published schema URL: `https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/nuget-resolution-guard.schema.json`
- The NuGet package also carries `schema/nuget-resolution-guard.schema.json` and `sbom/ResolutionGuard.NuGet.spdx.json` for package completeness and offline inspection, but the published schema URL remains the canonical editor-integration entrypoint.

Example:

```json
{
  "$schema": "https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/nuget-resolution-guard.schema.json",
  "mode": "warning",
  "scope": "repository",
  "directOnly": false,
  "runtimeOnly": false,
  "includeEntrypoints": [
    "src/AppA/AppA.csproj",
    "src/AppB/AppB.csproj"
  ],
  "excludeEntrypoints": [
    "src/ExperimentalApp/ExperimentalApp.csproj"
  ],
  "includePackageIds": [
    "Newtonsoft.Json",
    "Plugin.SDK"
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
- `includeEntrypoints` is an optional entry-project allowlist. When `scope=solution`, it narrows the solution project set.
- `excludeEntrypoints` is an entry-project exclude-list (blacklist) and wins if the same path is present in both include/exclude lists.
- Entrypoint paths are SDK-style project paths. Common extensions such as `.csproj`, `.fsproj`, and `.vbproj` are supported.
- When the effective entrypoint set is known (for example via `scope=solution` or `includeEntrypoints`), the analyzer derives those projects' `obj/project.assets.json` paths directly instead of recursively scanning the entire repository root.
- `includePackageIds` is an optional package allowlist.
- `excludePackageIds` is a package exclude-list (blacklist) and wins if the same package ID is present in both include/exclude lists.
- `rules[].mode` overrides the global `mode` for that package ID.
- `rules[].versions` is optional. Listed versions are treated as allowed, and any other resolved version follows `rules[].mode`.
- When `scope=solution`, the task uses `$(SolutionPath)` by default. If no solution file is available, it logs a warning and falls back to `repository`.
- When `scope=solution`, projects without a corresponding `project.assets.json` are reported as a warning and omitted from analysis until they are restored.
- When `project.assets.json` omits `project.restore.projectPath`, fallback resolution accepts only a single unambiguous SDK-style project file in that directory. If fallback is ambiguous, the task warns and skips that assets file instead of guessing.

## MSBuild properties

- `ResolutionGuardNuGetEnabled` (`true|false`)
- `ResolutionGuardNuGetEmitSuccessMessage` (`true|false`, default `false`; when `true`, logs a success message if no mismatch is found)
- `ResolutionGuardNuGetConfigFile`
- `ResolutionGuardNuGetMode` (`off|info|warning|error`)
- `ResolutionGuardNuGetScope` (`repository|solution`)
- `ResolutionGuardNuGetDirectOnly` (`true|false`)
- `ResolutionGuardNuGetRuntimeOnly` (`true|false`)
- `ResolutionGuardNuGetSolutionFile` (`.sln` / `.slnx` path override)
- `ResolutionGuardNuGetIncludedEntrypoints` (`;` separated SDK-style project paths to include)
- `ResolutionGuardNuGetExcludedEntrypoints` (`;` separated SDK-style project paths to exclude)
- `ResolutionGuardNuGetIncludedPackageIds` (`;` separated package ids to include)
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
