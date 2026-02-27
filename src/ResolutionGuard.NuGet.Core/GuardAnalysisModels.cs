namespace ResolutionGuard.NuGet.Core;

public sealed class GuardAnalysisResult
{
    public IReadOnlyList<PackageMismatch> Mismatches { get; set; } = [];

    public IReadOnlyList<string> Diagnostics { get; set; } = [];

    public int AssetsFileCount { get; set; }
}

public sealed class PackageMismatch
{
    public string PackageId { get; set; } = string.Empty;

    public GuardMode Mode { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyList<ProjectDescriptor>> VersionMap { get; set; } =
        new Dictionary<string, IReadOnlyList<ProjectDescriptor>>(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProjectDescriptor
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}
