namespace ResolutionGuard.NuGet.Core;

public sealed class GuardConfigFile
{
    public string? Mode { get; set; }

    public bool? DirectOnly { get; set; }

    public bool? RuntimeOnly { get; set; }

    public List<string>? ExcludeEntrypoints { get; set; }

    public List<string>? ExcludePackageIds { get; set; }

    public List<GuardRuleConfig>? Rules { get; set; }
}

public sealed class GuardRuleConfig
{
    public string? PackageId { get; set; }

    public string? Mode { get; set; }

    public List<string>? Versions { get; set; }
}
