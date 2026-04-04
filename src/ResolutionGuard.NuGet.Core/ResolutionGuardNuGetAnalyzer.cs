namespace ResolutionGuard.NuGet.Core;

public static class ResolutionGuardNuGetAnalyzer
{
    public static GuardAnalysisResult Analyze(GuardSettings settings)
    {
        List<string> diagnostics = [];

        if (!settings.Enabled)
        {
            return new GuardAnalysisResult
            {
                AssetsFileCount = 0,
                Diagnostics = diagnostics,
                Mismatches = [],
            };
        }

        if (!Directory.Exists(settings.RepositoryRoot))
        {
            diagnostics.Add($"ResolutionGuard.NuGet: Repository root '{settings.RepositoryRoot}' does not exist.");
            return new GuardAnalysisResult
            {
                AssetsFileCount = 0,
                Diagnostics = diagnostics,
                Mismatches = [],
            };
        }

        HashSet<string> expectedEntrypoints = new(
            settings.IncludedEntrypoints.Where(path => !settings.ExcludedEntrypoints.Contains(path)),
            GuardPathComparer.StringComparer);
        List<string> assetsFiles = ResolveAssetsFiles(settings, expectedEntrypoints);

        HashSet<string>? expectedSolutionEntrypoints = settings.Scope == GuardScope.Solution
            ? expectedEntrypoints
            : null;
        HashSet<string>? observedSolutionEntrypoints = expectedSolutionEntrypoints is null
            ? null
            : new HashSet<string>(GuardPathComparer.StringComparer);

        Dictionary<string, Dictionary<string, HashSet<ProjectDescriptor>>> packageVersionMap =
            new(GuardPackageIdComparer.StringComparer);

        foreach (string assetsFile in assetsFiles)
        {
            if (!ProjectAssetsReader.TryRead(assetsFile, out ProjectAssetsDocument? document, out string? parseDiagnostic)
                || document is null)
            {
                if (!string.IsNullOrWhiteSpace(parseDiagnostic))
                {
                    string diagnostic = parseDiagnostic ?? string.Empty;
                    diagnostics.Add(diagnostic);
                }

                if (ProjectAssetsReader.TryInferProjectPathFromAssetsPath(assetsFile, out string inferredProjectPath))
                {
                    TrackObservedSolutionEntrypoint(expectedSolutionEntrypoints, observedSolutionEntrypoints, inferredProjectPath);
                }

                continue;
            }

            TrackObservedSolutionEntrypoint(expectedSolutionEntrypoints, observedSolutionEntrypoints, document.ProjectPath);

            if (settings.IncludedEntrypoints.Count > 0
                && !settings.IncludedEntrypoints.Contains(document.ProjectPath))
            {
                continue;
            }

            if (settings.ExcludedEntrypoints.Contains(document.ProjectPath))
            {
                continue;
            }

            ProjectDescriptor descriptor = new()
            {
                Name = document.ProjectName,
                Path = document.ProjectPath,
            };

            foreach (ResolvedPackage package in document.Packages)
            {
                string packageId = package.PackageId;
                string version = package.Version;

                if (settings.IncludedPackageIds.Count > 0
                    && !settings.IncludedPackageIds.Contains(packageId))
                {
                    continue;
                }

                if (settings.ExcludedPackageIds.Contains(packageId))
                {
                    continue;
                }

                if (settings.DirectOnly && !package.IsDirect)
                {
                    continue;
                }

                if (settings.RuntimeOnly && !package.HasRuntimeAssets)
                {
                    continue;
                }

                if (!packageVersionMap.TryGetValue(packageId, out Dictionary<string, HashSet<ProjectDescriptor>>? versions))
                {
                    versions = new Dictionary<string, HashSet<ProjectDescriptor>>(StringComparer.OrdinalIgnoreCase);
                    packageVersionMap.Add(packageId, versions);
                }

                if (!versions.TryGetValue(version, out HashSet<ProjectDescriptor>? projects))
                {
                    projects = new HashSet<ProjectDescriptor>(ProjectDescriptorComparer.Instance);
                    versions.Add(version, projects);
                }

                projects.Add(descriptor);
            }
        }

        List<PackageMismatch> mismatches = [];

        foreach (KeyValuePair<string, Dictionary<string, HashSet<ProjectDescriptor>>> packageEntry in packageVersionMap)
        {
            string packageId = packageEntry.Key;
            Dictionary<string, HashSet<ProjectDescriptor>> versions = packageEntry.Value;

            if (versions.Count <= 1)
            {
                continue;
            }

            GuardMode mode = settings.Rules.TryGetValue(packageId, out GuardRule? rule)
                ? rule.Mode
                : settings.Mode;

            if (mode == GuardMode.Off)
            {
                continue;
            }

            if (rule is not null
                && rule.Versions.Count > 0
                && versions.Keys.All(rule.Versions.Contains))
            {
                continue;
            }

            Dictionary<string, IReadOnlyList<ProjectDescriptor>> normalizedVersionMap = versions
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<ProjectDescriptor>)[.. x.Value.OrderBy(p => p.Path, GuardPathComparer.StringComparer)],
                    StringComparer.OrdinalIgnoreCase);

            mismatches.Add(new PackageMismatch
            {
                PackageId = packageId,
                Mode = mode,
                VersionMap = normalizedVersionMap,
            });
        }

        if (expectedSolutionEntrypoints is not null && observedSolutionEntrypoints is not null)
        {
            List<string> missingEntrypoints = [.. expectedSolutionEntrypoints
                .Where(path => !observedSolutionEntrypoints.Contains(path))
                .OrderBy(path => path, GuardPathComparer.StringComparer)];

            if (missingEntrypoints.Count > 0)
            {
                diagnostics.Add(FormatMissingSolutionAssetsDiagnostic(missingEntrypoints));
            }
        }

        mismatches = [.. mismatches.OrderBy(m => m.PackageId, GuardPackageIdComparer.StringComparer)];

        return new GuardAnalysisResult
        {
            AssetsFileCount = assetsFiles.Count,
            Diagnostics = diagnostics,
            Mismatches = mismatches,
        };
    }

    private static bool IsObjAssetsPath(string path)
    {
        string marker = $"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}";
        string normalized = path.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
        return normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static List<string> ResolveAssetsFiles(GuardSettings settings, ISet<string> expectedEntrypoints)
    {
        bool canNarrowByEntrypoints = settings.Scope == GuardScope.Solution || settings.IncludedEntrypoints.Count > 0;
        if (!canNarrowByEntrypoints)
        {
            return EnumerateRepositoryAssetsFiles(settings.RepositoryRoot);
        }

        if (expectedEntrypoints.Count == 0)
        {
            return [];
        }

        List<string> localAssetsFiles = [];
        bool requiresRepositoryRootObjFallback = false;

        foreach (string entrypoint in expectedEntrypoints)
        {
            List<string> entrypointAssetsFiles = EnumerateEntrypointAssetsFiles(entrypoint);
            if (entrypointAssetsFiles.Count == 0)
            {
                requiresRepositoryRootObjFallback = true;
                continue;
            }

            localAssetsFiles.AddRange(entrypointAssetsFiles);
        }

        if (requiresRepositoryRootObjFallback)
        {
            localAssetsFiles.AddRange(EnumerateRepositoryRootObjAssetsFiles(settings.RepositoryRoot));
        }

        return [.. localAssetsFiles
            .Distinct(GuardPathComparer.StringComparer)
            .OrderBy(path => path, GuardPathComparer.StringComparer)];
    }

    private static List<string> EnumerateRepositoryAssetsFiles(string repositoryRoot)
    {
        return [.. Directory
            .EnumerateFiles(repositoryRoot, "project.assets.json", SearchOption.AllDirectories)
            .Where(IsObjAssetsPath)];
    }

    private static List<string> EnumerateRepositoryRootObjAssetsFiles(string repositoryRoot)
    {
        string repositoryObjDirectory = System.IO.Path.Combine(repositoryRoot, "obj");
        if (!Directory.Exists(repositoryObjDirectory))
        {
            return [];
        }

        return [.. Directory
            .EnumerateFiles(repositoryObjDirectory, "project.assets.json", SearchOption.AllDirectories)
            .Where(IsObjAssetsPath)
            .OrderBy(path => path, GuardPathComparer.StringComparer)];
    }

    private static List<string> EnumerateEntrypointAssetsFiles(string projectPath)
    {
        string projectDirectory = System.IO.Path.GetDirectoryName(projectPath) ?? projectPath;
        if (!Directory.Exists(projectDirectory))
        {
            return [];
        }

        return [.. Directory
            .EnumerateFiles(projectDirectory, "project.assets.json", SearchOption.AllDirectories)
            .Where(IsObjAssetsPath)
            .OrderBy(path => path, GuardPathComparer.StringComparer)];
    }

    private static void TrackObservedSolutionEntrypoint(
        ISet<string>? expectedSolutionEntrypoints,
        ISet<string>? observedSolutionEntrypoints,
        string projectPath)
    {
        if (expectedSolutionEntrypoints is null
            || observedSolutionEntrypoints is null
            || !expectedSolutionEntrypoints.Contains(projectPath))
        {
            return;
        }

        observedSolutionEntrypoints.Add(projectPath);
    }

    private static string FormatMissingSolutionAssetsDiagnostic(IReadOnlyList<string> missingEntrypoints)
    {
        string projectList = string.Join(", ", missingEntrypoints);
        return $"ResolutionGuard.NuGet: solution scope analyzed only the restored subset. No corresponding project.assets.json was found for {missingEntrypoints.Count} project(s) listed in the solution: {projectList}.";
    }

    private sealed class ProjectDescriptorComparer : IEqualityComparer<ProjectDescriptor>
    {
        public static ProjectDescriptorComparer Instance { get; } = new();

        public bool Equals(ProjectDescriptor? x, ProjectDescriptor? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return GuardPathComparer.StringComparer.Equals(x.Path, y.Path);
        }

        public int GetHashCode(ProjectDescriptor obj)
        {
            return GuardPathComparer.StringComparer.GetHashCode(obj.Path);
        }
    }
}
