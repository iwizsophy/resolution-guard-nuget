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

        List<string> assetsFiles = [.. Directory
            .EnumerateFiles(settings.RepositoryRoot, "project.assets.json", SearchOption.AllDirectories)
            .Where(IsObjAssetsPath)];

        Dictionary<string, Dictionary<string, HashSet<ProjectDescriptor>>> packageVersionMap =
            new(GuardPathComparer.StringComparer);

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

                continue;
            }

            if (settings.Scope == GuardScope.Solution
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

        mismatches = [.. mismatches.OrderBy(m => m.PackageId, GuardPathComparer.StringComparer)];

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
