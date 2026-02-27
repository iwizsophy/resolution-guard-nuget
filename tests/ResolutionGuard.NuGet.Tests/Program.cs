using ResolutionGuard.NuGet.Core;
using System.Text.Json;

var CachedJsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

var failures = 0;

RunTest("detect mismatch", TestDetectMismatch);
RunTest("excluded package ids blacklist", TestExcludedPackageIds);
RunTest("direct only filters transitive", TestDirectOnlyFiltersTransitive);
RunTest("runtime only filters non-runtime packages", TestRuntimeOnlyFiltersNonRuntimePackages);
RunTest("excluded entrypoints blacklist", TestExcludedEntrypoints);
RunTest("rule mode override", TestRuleModeOverride);
RunTest("rule versions allow listed", TestRuleVersionsAllowListed);
RunTest("rule versions detect out-of-rule versions", TestRuleVersionsOutOfRule);
RunTest("resolver exclude package ids", TestResolverExcludePackageIds);
RunTest("resolver rule versions", TestResolverRuleVersions);
RunTest("resolver direct/runtime flags", TestResolverDirectRuntimeFlags);
RunTest("resolver direct/runtime empty overrides keep config", TestResolverDirectRuntimeEmptyOverridesKeepConfig);
RunTest("resolver mode empty override keeps config", TestResolverModeEmptyOverrideKeepsConfig);
RunTest("resolver excludes empty overrides keep config", TestResolverExcludesEmptyOverridesKeepConfig);

if (failures > 0)
{
    Console.Error.WriteLine($"ResolutionGuard.NuGet.Tests failed: {failures} test(s).");
    return 1;
}

Console.WriteLine("ResolutionGuard.NuGet.Tests passed.");
return 0;

void RunTest(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"[FAIL] {name}: {ex.Message}");
    }
}

void TestDetectMismatch()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Core", "1.0.0"));
        string appB = WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Core", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);

        Expect(result.Mismatches.Count == 1, "Expected one mismatch.");
        PackageMismatch mismatch = result.Mismatches[0];
        Expect(mismatch.PackageId == "Example.Core", "PackageId mismatch.");
        Expect(mismatch.Mode == GuardMode.Warning, "Expected warning mode.");
        Expect(mismatch.VersionMap.Count == 2, "Expected two versions.");
        Expect(mismatch.VersionMap["1.0.0"].Any(x => x.Path == appA), "AppA not linked to 1.0.0.");
        Expect(mismatch.VersionMap["2.0.0"].Any(x => x.Path == appB), "AppB not linked to 2.0.0.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestExcludedPackageIds()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Legacy.SDK", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Legacy.SDK", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            excludedPackageIds: ["Legacy.SDK"]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "excludePackageIds should suppress mismatch.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestDirectOnlyFiltersTransitive()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssetsDetailed(
            root,
            "src/AppA/AppA.csproj",
            ("Transitive.Only", "1.0.0", false, true));
        WriteProjectAssetsDetailed(
            root,
            "src/AppB/AppB.csproj",
            ("Transitive.Only", "2.0.0", false, true));

        GuardSettings directOnlySettings = CreateSettings(
            root,
            mode: GuardMode.Error,
            directOnly: true);

        GuardAnalysisResult directOnlyResult = ResolutionGuardNuGetAnalyzer.Analyze(directOnlySettings);
        Expect(directOnlyResult.Mismatches.Count == 0, "directOnly should ignore transitive-only mismatches.");

        GuardSettings allSettings = CreateSettings(
            root,
            mode: GuardMode.Error,
            directOnly: false);

        GuardAnalysisResult allResult = ResolutionGuardNuGetAnalyzer.Analyze(allSettings);
        Expect(allResult.Mismatches.Count == 1, "Without directOnly, transitive mismatch should be detected.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestRuntimeOnlyFiltersNonRuntimePackages()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssetsDetailed(
            root,
            "src/AppA/AppA.csproj",
            ("Analyzer.Only", "1.0.0", true, false));
        WriteProjectAssetsDetailed(
            root,
            "src/AppB/AppB.csproj",
            ("Analyzer.Only", "2.0.0", true, false));

        GuardSettings runtimeOnlySettings = CreateSettings(
            root,
            mode: GuardMode.Error,
            runtimeOnly: true);

        GuardAnalysisResult runtimeOnlyResult = ResolutionGuardNuGetAnalyzer.Analyze(runtimeOnlySettings);
        Expect(runtimeOnlyResult.Mismatches.Count == 0, "runtimeOnly should ignore packages without runtime assets.");

        GuardSettings allSettings = CreateSettings(
            root,
            mode: GuardMode.Error,
            runtimeOnly: false);

        GuardAnalysisResult allResult = ResolutionGuardNuGetAnalyzer.Analyze(allSettings);
        Expect(allResult.Mismatches.Count == 1, "Without runtimeOnly, non-runtime package mismatch should be detected.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestRuleModeOverride()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Newtonsoft.Json", "12.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Newtonsoft.Json", "13.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            rules: new Dictionary<string, GuardRule>(GuardPathComparer.StringComparer)
            {
                ["Newtonsoft.Json"] = CreateRule(GuardMode.Error),
            });

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 1, "Expected one mismatch.");
        Expect(result.Mismatches[0].Mode == GuardMode.Error, "Rule mode should be error.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestRuleVersionsAllowListed()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Plugin.SDK", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Plugin.SDK", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            rules: new Dictionary<string, GuardRule>(GuardPathComparer.StringComparer)
            {
                ["Plugin.SDK"] = CreateRule(GuardMode.Error, "1.0.0", "2.0.0"),
            });

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "Rule versions should allow listed versions.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestRuleVersionsOutOfRule()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Plugin.SDK", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Plugin.SDK", "3.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            rules: new Dictionary<string, GuardRule>(GuardPathComparer.StringComparer)
            {
                ["Plugin.SDK"] = CreateRule(GuardMode.Error, "1.0.0", "2.0.0"),
            });

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 1, "Unlisted versions should follow the rule mode.");
        Expect(result.Mismatches[0].Mode == GuardMode.Error, "Rule mode should apply to unlisted versions.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestExcludedEntrypoints()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Blacklist", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Blacklist", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            excludedEntrypoints: [appA]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "Excluded entrypoint should be ignored from analysis.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverExcludePackageIds()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "excludePackageIds": [ "Package.FromConfig" ]
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: "Package.FromProperty");

        Expect(
            resolved.Settings.ExcludedPackageIds.Contains("Package.FromProperty"),
            "exclude package ids override should apply as blacklist.");
        Expect(
            !resolved.Settings.ExcludedPackageIds.Contains("Package.FromConfig"),
            "exclude package ids override should replace config list.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverRuleVersions()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "rules": [
                {
                  "packageId": "Plugin.SDK",
                  "mode": "error",
                  "versions": [ "1.0.0", "2.0.0" ]
                }
              ]
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Rules.TryGetValue("Plugin.SDK", out GuardRule? rule), "Rule should be loaded.");
        Expect(rule is not null && rule.Mode == GuardMode.Error, "Rule mode should be loaded.");
        Expect(rule is not null && rule.Versions.Contains("1.0.0"), "Rule versions should include 1.0.0.");
        Expect(rule is not null && rule.Versions.Contains("2.0.0"), "Rule versions should include 2.0.0.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverDirectRuntimeFlags()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "directOnly": true,
              "runtimeOnly": false
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: "false",
            runtimeOnlyOverride: "true",
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(!resolved.Settings.DirectOnly, "directOnly override should apply.");
        Expect(resolved.Settings.RuntimeOnly, "runtimeOnly override should apply.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverDirectRuntimeEmptyOverridesKeepConfig()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "directOnly": true,
              "runtimeOnly": true
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: "",
            runtimeOnlyOverride: "",
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.DirectOnly, "Empty directOnly override should not replace config.");
        Expect(resolved.Settings.RuntimeOnly, "Empty runtimeOnly override should not replace config.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverModeEmptyOverrideKeepsConfig()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "mode": "error"
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: "",
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Mode == GuardMode.Error, "Empty mode override should not replace config.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverExcludesEmptyOverridesKeepConfig()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "excludeEntrypoints": [ "src/AppA/AppA.csproj" ],
              "excludePackageIds": [ "Package.FromConfig" ]
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: "",
            excludedPackageIdsOverride: "");

        string expectedEntrypoint = Normalize(Path.Combine(root, "src/AppA/AppA.csproj"));
        Expect(
            resolved.Settings.ExcludedEntrypoints.Contains(expectedEntrypoint),
            "Empty excludeEntrypoints override should not replace config.");
        Expect(
            resolved.Settings.ExcludedPackageIds.Contains("Package.FromConfig"),
            "Empty excludePackageIds override should not replace config.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

GuardSettings CreateSettings(
    string root,
    GuardMode mode,
    bool directOnly = false,
    bool runtimeOnly = false,
    IEnumerable<string>? excludedEntrypoints = null,
    IEnumerable<string>? excludedPackageIds = null,
    IDictionary<string, GuardRule>? rules = null)
{
    return new GuardSettings
    {
        Enabled = true,
        Mode = mode,
        DirectOnly = directOnly,
        RuntimeOnly = runtimeOnly,
        RepositoryRoot = Normalize(root),
        ProjectDirectory = Normalize(root),
        ConfigFilePath = null,
        ExcludedEntrypoints = new HashSet<string>((excludedEntrypoints ?? []).Select(Normalize), GuardPathComparer.StringComparer),
        ExcludedPackageIds = new HashSet<string>(excludedPackageIds ?? [], GuardPathComparer.StringComparer),
        Rules = CloneRules(rules),
    };
}

IDictionary<string, GuardRule> CloneRules(IDictionary<string, GuardRule>? rules)
{
    Dictionary<string, GuardRule> result = new(GuardPathComparer.StringComparer);
    if (rules is null)
    {
        return result;
    }

    foreach (KeyValuePair<string, GuardRule> entry in rules)
    {
        result[entry.Key] = CreateRule(entry.Value.Mode, [.. entry.Value.Versions]);
    }

    return result;
}

GuardRule CreateRule(GuardMode mode, params string[] versions)
{
    return new GuardRule
    {
        Mode = mode,
        Versions = new HashSet<string>(versions ?? [], StringComparer.OrdinalIgnoreCase),
    };
}

string WriteProjectAssets(string root, string projectRelativePath, params (string PackageId, string Version)[] packages)
{
    (string PackageId, string Version, bool IsDirect, bool HasRuntimeAssets)[] fixtures = [.. packages.Select(p => (p.PackageId, p.Version, true, true))];
    return WriteProjectAssetsDetailed(root, projectRelativePath, fixtures);
}

string WriteProjectAssetsDetailed(
    string root,
    string projectRelativePath,
    params (string PackageId, string Version, bool IsDirect, bool HasRuntimeAssets)[] packages)
{
    string projectPath = Normalize(Path.Combine(root, projectRelativePath));
    string projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Project directory missing.");
    Directory.CreateDirectory(projectDirectory);

    if (!File.Exists(projectPath))
    {
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    var libraries = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    var targetLibraries = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    var directDependencies = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    foreach ((string PackageId, string Version, bool IsDirect, bool HasRuntimeAssets) package in packages)
    {
        libraries[$"{package.PackageId}/{package.Version}"] = new Dictionary<string, string>
        {
            ["type"] = "package",
        };

        var targetLibrary = new Dictionary<string, object?>
        {
            ["type"] = "package",
        };

        if (package.HasRuntimeAssets)
        {
            targetLibrary["runtime"] = new Dictionary<string, object?>
            {
                [$"lib/net8.0/{package.PackageId}.dll"] = new Dictionary<string, object?>(),
            };
        }
        else
        {
            targetLibrary["compile"] = new Dictionary<string, object?>
            {
                [$"ref/net8.0/{package.PackageId}.dll"] = new Dictionary<string, object?>(),
            };
        }

        targetLibraries[$"{package.PackageId}/{package.Version}"] = targetLibrary;

        if (package.IsDirect)
        {
            directDependencies[package.PackageId] = package.Version;
        }
    }

    var jsonModel = new Dictionary<string, object?>
    {
        ["version"] = 3,
        ["libraries"] = libraries,
        ["targets"] = new Dictionary<string, object?>
        {
            ["net8.0"] = targetLibraries,
        },
        ["project"] = new Dictionary<string, object?>
        {
            ["restore"] = new Dictionary<string, object?>
            {
                ["projectPath"] = projectPath,
            },
            ["frameworks"] = new Dictionary<string, object?>
            {
                ["net8.0"] = new Dictionary<string, object?>
                {
                    ["dependencies"] = directDependencies,
                },
            },
        },
    };

    string objDirectory = Path.Combine(projectDirectory, "obj");
    Directory.CreateDirectory(objDirectory);

    string assetsPath = Path.Combine(objDirectory, "project.assets.json");
    string json = JsonSerializer.Serialize(jsonModel, CachedJsonSerializerOptions);
    File.WriteAllText(assetsPath, json);

    return projectPath;
}

string CreateTempRoot()
{
    string root = Path.Combine(Path.GetTempPath(), "ResolutionGuard.NuGet.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    return Normalize(root);
}

string Normalize(string path)
{
    return Path.GetFullPath(path)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

void Expect(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
