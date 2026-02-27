using Microsoft.Build.Framework;
using ResolutionGuard.NuGet.Core;
using System.Text;

namespace ResolutionGuard.NuGet.Tasks;

public sealed class ResolutionGuardNuGetTask : Microsoft.Build.Utilities.Task
{
    public string? ConfigFile { get; set; }

    public string? ModeOverride { get; set; }

    public string? DirectOnlyOverride { get; set; }

    public string? RuntimeOnlyOverride { get; set; }

    public string? ScopeOverride { get; set; }

    public string? Enabled { get; set; }

    public string? SolutionFile { get; set; }

    public string? ExcludedEntrypoints { get; set; }

    public string? ExcludedPackageIds { get; set; }

    public string? RepositoryRoot { get; set; }

    [Required]
    public string ProjectDirectory { get; set; } = string.Empty;

    public override bool Execute()
    {
        GuardSettingsResolution resolution = GuardSettingsResolver.Resolve(
            projectDirectory: ProjectDirectory,
            repositoryRootOverride: RepositoryRoot,
            configFileOverride: ConfigFile,
            modeOverride: ModeOverride,
            directOnlyOverride: DirectOnlyOverride,
            runtimeOnlyOverride: RuntimeOnlyOverride,
            scopeOverride: ScopeOverride,
            enabledOverride: Enabled,
            solutionFileOverride: SolutionFile,
            excludedEntrypointsOverride: ExcludedEntrypoints,
            excludedPackageIdsOverride: ExcludedPackageIds);

        foreach (string diagnostic in resolution.Diagnostics)
        {
            Log.LogWarning(diagnostic);
        }

        GuardSettings settings = resolution.Settings;
        if (!settings.Enabled)
        {
            Log.LogMessage(MessageImportance.Low, "ResolutionGuard.NuGet: disabled (set ResolutionGuardNuGetEnabled=true to enable).");
            return true;
        }

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        foreach (string diagnostic in result.Diagnostics)
        {
            Log.LogWarning(diagnostic);
        }

        foreach (PackageMismatch mismatch in result.Mismatches)
        {
            string message = FormatMismatchMessage(mismatch);
            switch (mismatch.Mode)
            {
                case GuardMode.Error:
                    Log.LogError(message);
                    break;
                case GuardMode.Warning:
                    Log.LogWarning(message);
                    break;
                case GuardMode.Info:
                    Log.LogMessage(MessageImportance.High, message);
                    break;
            }
        }

        if (result.Mismatches.Count == 0)
        {
            Log.LogMessage(MessageImportance.Low, $"ResolutionGuard.NuGet: analyzed {result.AssetsFileCount} assets file(s), no mismatch found.");
        }

        return !Log.HasLoggedErrors;
    }

    private static string FormatMismatchMessage(PackageMismatch mismatch)
    {
        StringBuilder builder = new();
        string versionList = string.Join(", ", mismatch.VersionMap.Keys.OrderBy(v => v, StringComparer.OrdinalIgnoreCase));

        builder.Append("ResolutionGuard.NuGet mismatch: package '")
            .Append(mismatch.PackageId)
            .Append("' resolved to multiple versions: ")
            .Append(versionList)
            .Append('.');

        foreach (KeyValuePair<string, IReadOnlyList<ProjectDescriptor>> versionEntry in mismatch.VersionMap)
        {
            string projects = string.Join(
                ", ",
                versionEntry.Value.Select(p => $"{p.Name} ({p.Path})"));

            builder.Append(' ')
                .Append("[")
                .Append(versionEntry.Key)
                .Append("] => ")
                .Append(projects)
                .Append('.');
        }

        return builder.ToString();
    }
}
