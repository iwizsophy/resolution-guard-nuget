using System.Runtime.InteropServices;

namespace ResolutionGuard.NuGet.Core;

public sealed class GuardSettings
{
    public bool Enabled { get; set; }

    public GuardMode Mode { get; set; }

    public bool DirectOnly { get; set; }

    public bool RuntimeOnly { get; set; }

    public string RepositoryRoot { get; set; } = string.Empty;

    public string ProjectDirectory { get; set; } = string.Empty;

    public string? ConfigFilePath { get; set; }

    public ISet<string> ExcludedEntrypoints { get; set; } = new HashSet<string>(GuardPathComparer.StringComparer);

    public ISet<string> ExcludedPackageIds { get; set; } = new HashSet<string>(GuardPathComparer.StringComparer);

    public IDictionary<string, GuardRule> Rules { get; set; } = new Dictionary<string, GuardRule>(GuardPathComparer.StringComparer);
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
