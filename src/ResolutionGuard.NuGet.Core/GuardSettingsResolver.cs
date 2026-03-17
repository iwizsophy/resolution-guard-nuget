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
        string? excludedPackageIdsOverride,
        string? scopeOverride = null,
        string? solutionFileOverride = null,
        string? includedEntrypointsOverride = null,
        string? includedPackageIdsOverride = null)
    {
        List<string> diagnostics = [];

        string normalizedProjectDirectory = NormalizePath(projectDirectory);
        string repositoryRoot = ResolveRepositoryRoot(normalizedProjectDirectory, repositoryRootOverride);

        string? configFilePath = ResolveConfigFilePath(normalizedProjectDirectory, configFileOverride);
        GuardConfigFile? configFile = TryLoadConfig(configFilePath, diagnostics);

        bool enabled = false;
        GuardMode mode = GuardMode.Warning;
        GuardScope scope = GuardScope.Repository;
        bool directOnly = false;
        bool runtimeOnly = false;

        string? solutionFilePath = null;
        HashSet<string> configuredIncludedEntrypoints = new(GuardPathComparer.StringComparer);
        HashSet<string>? solutionEntrypoints = null;
        HashSet<string> excludedPackageIds = new(GuardPackageIdComparer.StringComparer);
        HashSet<string> includedPackageIds = new(GuardPackageIdComparer.StringComparer);
        Dictionary<string, GuardRule> rules = new(GuardPackageIdComparer.StringComparer);
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

            if (TryParseScope(configFile.Scope, out GuardScope configScope))
            {
                scope = configScope;
            }
            else if (!string.IsNullOrWhiteSpace(configFile.Scope))
            {
                diagnostics.Add($"ResolutionGuard.NuGet: Unknown scope '{configFile.Scope}' in {configFilePath}. Falling back to '{scope.ToString().ToLowerInvariant()}'.");
            }

            if (configFile.DirectOnly.HasValue)
            {
                directOnly = configFile.DirectOnly.Value;
            }

            if (configFile.RuntimeOnly.HasValue)
            {
                runtimeOnly = configFile.RuntimeOnly.Value;
            }

            AddResolvedPaths(configuredIncludedEntrypoints, configFile.IncludeEntrypoints, repositoryRoot);
            AddAll(includedPackageIds, configFile.IncludePackageIds);
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

            AddResolvedPaths(excludedEntrypoints, configFile.ExcludeEntrypoints, repositoryRoot);
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

        if (TryParseScope(scopeOverride, out GuardScope scopeOverrideValue))
        {
            scope = scopeOverrideValue;
        }
        else if (!string.IsNullOrWhiteSpace(scopeOverride))
        {
            diagnostics.Add($"ResolutionGuard.NuGet: Unknown scope override '{scopeOverride}'. Using '{scope.ToString().ToLowerInvariant()}'.");
        }

        solutionFilePath = TryResolvePath(solutionFileOverride, normalizedProjectDirectory);

        if (scope == GuardScope.Solution)
        {
            if (string.IsNullOrWhiteSpace(solutionFilePath))
            {
                diagnostics.Add("ResolutionGuard.NuGet: scope 'solution' requested but no solution file was provided. Falling back to repository scope.");
                scope = GuardScope.Repository;
            }
            else if (!File.Exists(solutionFilePath))
            {
                diagnostics.Add($"ResolutionGuard.NuGet: solution file '{solutionFilePath}' does not exist. Falling back to repository scope.");
                scope = GuardScope.Repository;
                solutionFilePath = null;
            }
            else
            {
                string resolvedSolutionFilePath = solutionFilePath!;
                if (!SolutionFileReader.TryRead(resolvedSolutionFilePath, out ISet<string>? includedProjects, out string? solutionDiagnostic)
                    || includedProjects is null)
                {
                    if (!string.IsNullOrWhiteSpace(solutionDiagnostic))
                    {
                        diagnostics.Add(solutionDiagnostic!);
                    }

                    diagnostics.Add("ResolutionGuard.NuGet: solution scope could not be resolved. Falling back to repository scope.");
                    scope = GuardScope.Repository;
                    solutionFilePath = null;
                }
                else
                {
                    solutionEntrypoints = new HashSet<string>(includedProjects, GuardPathComparer.StringComparer);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(includedEntrypointsOverride))
        {
            configuredIncludedEntrypoints.Clear();
            foreach (string entrypoint in SplitPropertyValues(includedEntrypointsOverride))
            {
                string? normalizedEntryPoint = TryResolvePath(entrypoint, repositoryRoot);
                if (normalizedEntryPoint is not null)
                {
                    configuredIncludedEntrypoints.Add(normalizedEntryPoint);
                }
            }
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

        if (!string.IsNullOrWhiteSpace(includedPackageIdsOverride))
        {
            includedPackageIds.Clear();
            foreach (string packageId in SplitPropertyValues(includedPackageIdsOverride))
            {
                includedPackageIds.Add(packageId);
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

        HashSet<string> includedEntrypoints = BuildFinalIncludedEntrypoints(
            scope,
            configuredIncludedEntrypoints,
            solutionEntrypoints);

        GuardSettings settings = new()
        {
            Enabled = enabled,
            Mode = mode,
            Scope = scope,
            DirectOnly = directOnly,
            RuntimeOnly = runtimeOnly,
            RepositoryRoot = repositoryRoot,
            ProjectDirectory = normalizedProjectDirectory,
            ConfigFilePath = configFilePath,
            SolutionFilePath = scope == GuardScope.Solution ? solutionFilePath : null,
            IncludedEntrypoints = includedEntrypoints,
            ExcludedEntrypoints = excludedEntrypoints,
            IncludedPackageIds = includedPackageIds,
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

    private static void AddResolvedPaths(ISet<string> target, IEnumerable<string>? values, string baseDirectory)
    {
        if (values is null)
        {
            return;
        }

        foreach (string value in values)
        {
            string? normalizedPath = TryResolvePath(value, baseDirectory);
            if (normalizedPath is not null)
            {
                target.Add(normalizedPath);
            }
        }
    }

    private static HashSet<string> BuildFinalIncludedEntrypoints(
        GuardScope scope,
        ISet<string> configuredIncludedEntrypoints,
        ISet<string>? solutionEntrypoints)
    {
        if (scope != GuardScope.Solution || solutionEntrypoints is null)
        {
            return new HashSet<string>(configuredIncludedEntrypoints, GuardPathComparer.StringComparer);
        }

        if (configuredIncludedEntrypoints.Count == 0)
        {
            return new HashSet<string>(solutionEntrypoints, GuardPathComparer.StringComparer);
        }

        HashSet<string> finalIncludedEntrypoints = new(configuredIncludedEntrypoints, GuardPathComparer.StringComparer);
        finalIncludedEntrypoints.IntersectWith(solutionEntrypoints);
        return finalIncludedEntrypoints;
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

    private static bool TryParseScope(string? value, out GuardScope scope)
    {
        scope = GuardScope.Repository;
        string normalizedInput = value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        string normalized = normalizedInput.Trim().ToLowerInvariant();
        return normalized switch
        {
            "repository" => Assign(GuardScope.Repository, out scope),
            "solution" => Assign(GuardScope.Solution, out scope),
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
