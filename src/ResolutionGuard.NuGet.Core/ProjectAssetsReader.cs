using System.Text.Json;

namespace ResolutionGuard.NuGet.Core;

internal sealed class ProjectAssetsDocument
{
    public string AssetsPath { get; set; } = string.Empty;

    public string ProjectPath { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public IReadOnlyList<ResolvedPackage> Packages { get; set; } = [];
}

internal sealed class ResolvedPackage
{
    public string PackageId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool IsDirect { get; set; }

    public bool HasRuntimeAssets { get; set; }
}

internal static class ProjectAssetsReader
{
    private static readonly string[] SupportedProjectExtensions = [".csproj", ".fsproj", ".vbproj"];

    public static bool TryRead(string assetsPath, out ProjectAssetsDocument? document, out string? diagnostic)
    {
        document = null;
        diagnostic = null;

        try
        {
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(assetsPath));
            JsonElement root = json.RootElement;

            if (!TryResolveProjectPath(root, assetsPath, out string projectPath, out string? projectPathDiagnostic))
            {
                diagnostic = projectPathDiagnostic;
                return false;
            }

            string projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath);

            Dictionary<string, ResolvedPackage> packages = ReadPackages(root);
            HashSet<string> directPackageIds = ReadDirectPackageIds(root);
            (bool hasTargets, HashSet<string> runtimeAssetPackageKeys) = ReadRuntimeAssetPackageKeys(root);

            foreach (ResolvedPackage package in packages.Values)
            {
                package.IsDirect = directPackageIds.Contains(package.PackageId);

                string key = CreatePackageKey(package.PackageId, package.Version);
                package.HasRuntimeAssets = !hasTargets || runtimeAssetPackageKeys.Contains(key);
            }

            document = new ProjectAssetsDocument
            {
                AssetsPath = NormalizePath(assetsPath),
                ProjectPath = NormalizePath(projectPath),
                ProjectName = projectName,
                Packages = [.. packages.Values
                    .OrderBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(p => p.Version, StringComparer.OrdinalIgnoreCase)],
            };

            return true;
        }
        catch (Exception ex)
        {
            diagnostic = $"ResolutionGuard.NuGet: Failed to parse '{assetsPath}'. {ex.Message}";
            return false;
        }
    }

    private static Dictionary<string, ResolvedPackage> ReadPackages(JsonElement root)
    {
        Dictionary<string, ResolvedPackage> result = new(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("libraries", out JsonElement libraries) && libraries.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                if (library.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string? libraryType = null;
                if (library.Value.TryGetProperty("type", out JsonElement typeNode) && typeNode.ValueKind == JsonValueKind.String)
                {
                    libraryType = typeNode.GetString();
                }

                if (!string.Equals(libraryType, "package", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TrySplitPackageKey(library.Name, out string packageId, out string version))
                {
                    continue;
                }

                string key = CreatePackageKey(packageId, version);
                if (!result.ContainsKey(key))
                {
                    result[key] = new ResolvedPackage
                    {
                        PackageId = packageId,
                        Version = version,
                    };
                }
            }
        }

        return result;
    }

    private static HashSet<string> ReadDirectPackageIds(JsonElement root)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("project", out JsonElement projectNode)
            || projectNode.ValueKind != JsonValueKind.Object
            || !projectNode.TryGetProperty("frameworks", out JsonElement frameworksNode)
            || frameworksNode.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (JsonProperty framework in frameworksNode.EnumerateObject())
        {
            if (framework.Value.ValueKind != JsonValueKind.Object
                || !framework.Value.TryGetProperty("dependencies", out JsonElement dependenciesNode)
                || dependenciesNode.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (JsonProperty dependency in dependenciesNode.EnumerateObject())
            {
                if (!string.IsNullOrWhiteSpace(dependency.Name))
                {
                    result.Add(dependency.Name.Trim());
                }
            }
        }

        return result;
    }

    private static (bool HasTargets, HashSet<string> RuntimeAssetPackageKeys) ReadRuntimeAssetPackageKeys(JsonElement root)
    {
        HashSet<string> runtimeAssetPackages = new(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("targets", out JsonElement targetsNode)
            || targetsNode.ValueKind != JsonValueKind.Object)
        {
            return (false, runtimeAssetPackages);
        }

        foreach (JsonProperty target in targetsNode.EnumerateObject())
        {
            if (target.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (JsonProperty library in target.Value.EnumerateObject())
            {
                if (!TrySplitPackageKey(library.Name, out string packageId, out string version))
                {
                    continue;
                }

                if (library.Value.ValueKind != JsonValueKind.Object || !HasRuntimeAssets(library.Value))
                {
                    continue;
                }

                runtimeAssetPackages.Add(CreatePackageKey(packageId, version));
            }
        }

        return (true, runtimeAssetPackages);
    }

    private static bool HasRuntimeAssets(JsonElement libraryNode)
    {
        return HasNonEmptyObjectProperty(libraryNode, "runtime")
            || HasNonEmptyObjectProperty(libraryNode, "native")
            || HasNonEmptyObjectProperty(libraryNode, "runtimeTargets");
    }

    private static bool HasNonEmptyObjectProperty(JsonElement node, string propertyName)
    {
        return node.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Object
            && property.EnumerateObject().Any();
    }

    private static bool TrySplitPackageKey(string key, out string packageId, out string version)
    {
        packageId = string.Empty;
        version = string.Empty;

        int separatorIndex = key.LastIndexOf('/');
        if (separatorIndex <= 0 || separatorIndex >= key.Length - 1)
        {
            return false;
        }

        packageId = key.Substring(0, separatorIndex);
        version = key.Substring(separatorIndex + 1);
        return true;
    }

    private static string CreatePackageKey(string packageId, string version)
    {
        return $"{packageId}/{version}";
    }

    private static bool TryResolveProjectPath(
        JsonElement root,
        string assetsPath,
        out string projectPath,
        out string? diagnostic)
    {
        projectPath = string.Empty;
        diagnostic = null;

        if (root.TryGetProperty("project", out JsonElement projectNode)
            && projectNode.ValueKind == JsonValueKind.Object
            && projectNode.TryGetProperty("restore", out JsonElement restoreNode)
            && restoreNode.ValueKind == JsonValueKind.Object
            && restoreNode.TryGetProperty("projectPath", out JsonElement projectPathNode)
            && projectPathNode.ValueKind == JsonValueKind.String)
        {
            string? configuredPath = projectPathNode.GetString();
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                projectPath = (configuredPath ?? string.Empty).Trim();
                return true;
            }
        }

        return TryInferProjectPathFromAssetsPath(assetsPath, out projectPath, out diagnostic);
    }

    internal static bool TryInferProjectPathFromAssetsPath(string assetsPath, out string projectPath)
    {
        return TryInferProjectPathFromAssetsPath(assetsPath, out projectPath, out _);
    }

    private static bool TryInferProjectPathFromAssetsPath(
        string assetsPath,
        out string projectPath,
        out string? diagnostic)
    {
        string normalizedAssetsPath = NormalizePath(assetsPath);
        string objDirectory = System.IO.Path.GetDirectoryName(normalizedAssetsPath) ?? normalizedAssetsPath;
        string projectDirectory = Directory.GetParent(objDirectory)?.FullName ?? objDirectory;
        string normalizedProjectDirectory = NormalizePath(projectDirectory);

        string[] supportedProjectFiles = EnumerateSupportedProjectFiles(normalizedProjectDirectory);
        if (supportedProjectFiles.Length == 1)
        {
            projectPath = supportedProjectFiles[0];
            diagnostic = null;
            return true;
        }

        string directoryName = new DirectoryInfo(normalizedProjectDirectory).Name;
        string[] matchingDirectoryNameFiles = supportedProjectFiles
            .Where(path => string.Equals(
                System.IO.Path.GetFileNameWithoutExtension(path),
                directoryName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingDirectoryNameFiles.Length == 1)
        {
            projectPath = matchingDirectoryNameFiles[0];
            diagnostic = null;
            return true;
        }

        projectPath = string.Empty;
        diagnostic = FormatProjectPathResolutionDiagnostic(
            normalizedAssetsPath,
            normalizedProjectDirectory,
            supportedProjectFiles);
        return false;
    }

    private static string[] EnumerateSupportedProjectFiles(string projectDirectory)
    {
        if (!Directory.Exists(projectDirectory))
        {
            return [];
        }

        return [.. Directory
            .EnumerateFiles(projectDirectory, "*.*proj", SearchOption.TopDirectoryOnly)
            .Where(path => SupportedProjectExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(NormalizePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)];
    }

    private static string FormatProjectPathResolutionDiagnostic(
        string assetsPath,
        string projectDirectory,
        IReadOnlyList<string> supportedProjectFiles)
    {
        string supportedExtensions = string.Join(", ", SupportedProjectExtensions);

        if (supportedProjectFiles.Count == 0)
        {
            return $"ResolutionGuard.NuGet: Failed to resolve a project path for '{assetsPath}'. 'project.restore.projectPath' was missing, and fallback resolution found no supported SDK-style project file ({supportedExtensions}) under '{projectDirectory}'.";
        }

        string candidates = string.Join(", ", supportedProjectFiles);
        return $"ResolutionGuard.NuGet: Failed to resolve a project path for '{assetsPath}'. 'project.restore.projectPath' was missing, and fallback resolution was ambiguous among {supportedProjectFiles.Count} supported SDK-style project files under '{projectDirectory}': {candidates}.";
    }

    private static string NormalizePath(string path)
    {
        return System.IO.Path.GetFullPath(path)
            .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
    }
}
