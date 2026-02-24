using System.Text.Json;

namespace ResolutionGuard.NuGet.Core;

public static class GuardSettingsResolver
{
    private const string DefaultConfigFileName = "nuget-resolution-guard.json";

    public static GuardSettingsResolution Resolve(
        string? projectDirectory,
        string? repositoryRootOverride,
        string? configFileOverride,
        string? modeOverride,
        string? directOnlyOverride,
        string? runtimeOnlyOverride,
        string? enabledOverride,
        string? excludedEntrypointsOverride,
        string? excludedPackageIdsOverride)
    {
        List<string> diagnostics = [];

        string normalizedProjectDirectory = NormalizePath(projectDirectory);
        string repositoryRoot = ResolveRepositoryRoot(normalizedProjectDirectory, repositoryRootOverride);

        string? configFilePath = ResolveConfigFilePath(normalizedProjectDirectory, configFileOverride);
        GuardConfigFile? configFile = TryLoadConfig(configFilePath, diagnostics);

        bool enabled = false;
        GuardMode mode = GuardMode.Warning;
        bool directOnly = false;
        bool runtimeOnly = false;

        HashSet<string> excludedPackageIds = new(GuardPathComparer.StringComparer);
        Dictionary<string, GuardRule> rules = new(GuardPathComparer.StringComparer);
        HashSet<string> excludedEntrypoints = new(GuardPathComparer.StringComparer);

        if (configFile is not null)
        {
            if (TryParseMode(configFile.Mode, out GuardMode configMode))
            {
                mode = configMode;
            }
            else if (!string.IsNullOrWhiteSpace(configFile.Mode))
            {
                diagnostics.Add($"ResolutionGuard.NuGet: Unknown mode '{configFile.Mode}' in {configFilePath}. Falling back to '{mode}'.");
            }

            if (configFile.DirectOnly.HasValue)
            {
                directOnly = configFile.DirectOnly.Value;
            }

            if (configFile.RuntimeOnly.HasValue)
            {
                runtimeOnly = configFile.RuntimeOnly.Value;
            }

            AddAll(excludedPackageIds, configFile.ExcludePackageIds);

            if (configFile.Rules is not null)
            {
                foreach (GuardRuleConfig? rule in configFile.Rules)
                {
                    if (rule is null)
                    {
                        continue;
                    }

                    string packageId = rule.PackageId ?? string.Empty;
                    string modeText = rule.Mode ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(packageId) || string.IsNullOrWhiteSpace(modeText))
                    {
                        continue;
                    }

                    if (!TryParseMode(modeText, out GuardMode parsedRuleMode))
                    {
                        diagnostics.Add($"ResolutionGuard.NuGet: Unknown rule mode '{modeText}' for '{packageId}'. Rule ignored.");
                        continue;
                    }

                    HashSet<string> ruleVersions = new(StringComparer.OrdinalIgnoreCase);
                    AddAll(ruleVersions, rule.Versions);

                    rules[packageId.Trim()] = new GuardRule
                    {
                        Mode = parsedRuleMode,
                        Versions = ruleVersions,
                    };
                }
            }

            if (configFile.ExcludeEntrypoints is not null)
            {
                foreach (string entrypoint in configFile.ExcludeEntrypoints)
                {
                    string? normalizedEntryPoint = TryResolvePath(entrypoint, repositoryRoot);
                    if (normalizedEntryPoint is not null)
                    {
                        excludedEntrypoints.Add(normalizedEntryPoint);
                    }
                }
            }
        }

        if (TryParseBoolean(enabledOverride, out bool enabledOverrideValue))
        {
            enabled = enabledOverrideValue;
        }

        if (TryParseMode(modeOverride, out GuardMode modeOverrideValue))
        {
            mode = modeOverrideValue;
        }
        else if (!string.IsNullOrWhiteSpace(modeOverride))
        {
            diagnostics.Add($"ResolutionGuard.NuGet: Unknown mode override '{modeOverride}'. Using '{mode}'.");
        }

        if (TryParseBoolean(directOnlyOverride, out bool directOnlyOverrideValue))
        {
            directOnly = directOnlyOverrideValue;
        }
        else if (!string.IsNullOrWhiteSpace(directOnlyOverride))
        {
            diagnostics.Add($"ResolutionGuard.NuGet: Unknown directOnly override '{directOnlyOverride}'. Using '{directOnly}'.");
        }

        if (TryParseBoolean(runtimeOnlyOverride, out bool runtimeOnlyOverrideValue))
        {
            runtimeOnly = runtimeOnlyOverrideValue;
        }
        else if (!string.IsNullOrWhiteSpace(runtimeOnlyOverride))
        {
            diagnostics.Add($"ResolutionGuard.NuGet: Unknown runtimeOnly override '{runtimeOnlyOverride}'. Using '{runtimeOnly}'.");
        }

        if (!string.IsNullOrWhiteSpace(excludedEntrypointsOverride))
        {
            excludedEntrypoints.Clear();
            foreach (string entrypoint in SplitPropertyValues(excludedEntrypointsOverride))
            {
                string? normalizedEntryPoint = TryResolvePath(entrypoint, repositoryRoot);
                if (normalizedEntryPoint is not null)
                {
                    excludedEntrypoints.Add(normalizedEntryPoint);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(excludedPackageIdsOverride))
        {
            excludedPackageIds.Clear();
            foreach (string packageId in SplitPropertyValues(excludedPackageIdsOverride))
            {
                excludedPackageIds.Add(packageId);
            }
        }

        GuardSettings settings = new()
        {
            Enabled = enabled,
            Mode = mode,
            DirectOnly = directOnly,
            RuntimeOnly = runtimeOnly,
            RepositoryRoot = repositoryRoot,
            ProjectDirectory = normalizedProjectDirectory,
            ConfigFilePath = configFilePath,
            ExcludedEntrypoints = excludedEntrypoints,
            ExcludedPackageIds = excludedPackageIds,
            Rules = rules,
        };

        return new GuardSettingsResolution
        {
            Settings = settings,
            Diagnostics = diagnostics,
        };
    }

    private static GuardConfigFile? TryLoadConfig(string? configFilePath, List<string> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(configFilePath) || !File.Exists(configFilePath))
        {
            return null;
        }

        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            string json = File.ReadAllText(configFilePath);
            GuardConfigFile? config = JsonSerializer.Deserialize<GuardConfigFile>(json, options);
            return config;
        }
        catch (Exception ex)
        {
            diagnostics.Add($"ResolutionGuard.NuGet: Failed to read config '{configFilePath}'. {ex.Message}");
            return null;
        }
    }

    private static void AddAll(ISet<string> target, IEnumerable<string>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value.Trim());
            }
        }
    }

    private static IEnumerable<string> SplitPropertyValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        string[] tokens = (value ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            string trimmed = token.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }

    private static bool TryParseBoolean(string? value, out bool parsed)
    {
        parsed = false;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (bool.TryParse(value, out bool boolValue))
        {
            parsed = boolValue;
            return true;
        }

        return false;
    }

    private static bool TryParseMode(string? value, out GuardMode mode)
    {
        mode = GuardMode.Warning;
        string normalizedInput = value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        string normalized = normalizedInput.Trim().ToLowerInvariant();
        return normalized switch
        {
            "off" => Assign(GuardMode.Off, out mode),
            "info" => Assign(GuardMode.Info, out mode),
            "warning" => Assign(GuardMode.Warning, out mode),
            "error" => Assign(GuardMode.Error, out mode),
            _ => false,
        };
    }

    private static string ResolveRepositoryRoot(string projectDirectory, string? repositoryRootOverride)
    {
        string? explicitRoot = TryResolvePath(repositoryRootOverride, projectDirectory);
        if (!string.IsNullOrEmpty(explicitRoot))
        {
            return explicitRoot!;
        }

        DirectoryInfo? current = new(projectDirectory);
        while (current is not null)
        {
            string candidateGit = System.IO.Path.Combine(current.FullName, ".git");
            if (Directory.Exists(candidateGit) || File.Exists(candidateGit))
            {
                return NormalizePath(current.FullName);
            }

            current = current.Parent;
        }

        return projectDirectory;
    }

    private static string? ResolveConfigFilePath(string projectDirectory, string? configFileOverride)
    {
        string? explicitConfig = TryResolvePath(configFileOverride, projectDirectory);
        if (!string.IsNullOrWhiteSpace(explicitConfig))
        {
            return explicitConfig;
        }

        DirectoryInfo? current = new(projectDirectory);
        while (current is not null)
        {
            string candidate = System.IO.Path.Combine(current.FullName, DefaultConfigFileName);
            if (File.Exists(candidate))
            {
                return NormalizePath(candidate);
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? TryResolvePath(string? value, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value == null ? string.Empty : value.Trim();
        string resolvedPath = System.IO.Path.IsPathRooted(trimmed)
            ? trimmed
            : System.IO.Path.Combine(baseDirectory, trimmed);

        return NormalizePath(resolvedPath);
    }

    private static string NormalizePath(string? path)
    {
        string value = string.IsNullOrWhiteSpace(path)
            ? Environment.CurrentDirectory
            : path == null ? Environment.CurrentDirectory : path.Trim();

        return System.IO.Path.GetFullPath(value)
            .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
    }

    private static bool Assign<T>(T value, out T output)
    {
        output = value;
        return true;
    }
}
