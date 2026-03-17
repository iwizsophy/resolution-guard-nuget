using System.Runtime.InteropServices;

namespace ResolutionGuard.NuGet.Core;

public sealed class GuardSettings
{
    public bool Enabled { get; set; }

    public GuardMode Mode { get; set; }

    public GuardScope Scope { get; set; } = GuardScope.Repository;

    public bool DirectOnly { get; set; }

    public bool RuntimeOnly { get; set; }

    public string RepositoryRoot { get; set; } = string.Empty;

    public string ProjectDirectory { get; set; } = string.Empty;

    public string? ConfigFilePath { get; set; }

    public string? SolutionFilePath { get; set; }

    public ISet<string> IncludedEntrypoints { get; set; } = new HashSet<string>(GuardPathComparer.StringComparer);

    public ISet<string> ExcludedEntrypoints { get; set; } = new HashSet<string>(GuardPathComparer.StringComparer);

    public ISet<string> IncludedPackageIds { get; set; } = new HashSet<string>(GuardPackageIdComparer.StringComparer);

    public ISet<string> ExcludedPackageIds { get; set; } = new HashSet<string>(GuardPackageIdComparer.StringComparer);

    public IDictionary<string, GuardRule> Rules { get; set; } = new Dictionary<string, GuardRule>(GuardPackageIdComparer.StringComparer);
}

public sealed class GuardRule
{
    public GuardMode Mode { get; set; }

    public ISet<string> Versions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class GuardSettingsResolution
{
    public GuardSettings Settings { get; set; } = new GuardSettings();

    public IReadOnlyList<string> Diagnostics { get; set; } = [];
}

public static class GuardPathComparer
{
    public static StringComparer StringComparer { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}

public static class GuardPackageIdComparer
{
    public static StringComparer StringComparer { get; } = StringComparer.OrdinalIgnoreCase;
}
