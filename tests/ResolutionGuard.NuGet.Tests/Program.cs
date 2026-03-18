using Microsoft.Build.Framework;
using ResolutionGuard.NuGet.Core;
using ResolutionGuard.NuGet.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

var CachedJsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
var CurrentTestTargetFramework = GetCurrentTestTargetFramework();

var failures = 0;

RunTest("detect mismatch", TestDetectMismatch);
RunTest("task disabled logs message", TestTaskDisabledLogsMessage);
RunTest("task no mismatch stays quiet by default", TestTaskNoMismatchStaysQuietByDefault);
RunTest("task emit success message logs summary", TestTaskEmitSuccessMessageLogsSummary);
RunTest("task invalid emit success message warns and stays quiet", TestTaskInvalidEmitSuccessMessageWarnsAndStaysQuiet);
RunTest("task warning mode logs and succeeds", TestTaskWarningModeLogsAndSucceeds);
RunTest("task error mode logs and fails", TestTaskErrorModeLogsAndFails);
RunTest("task included package ids allowlist", TestTaskIncludedPackageIdsAllowlist);
RunTest("task solution scope warns when projects are unrestored", TestTaskSolutionScopeWarnsWhenProjectsAreUnrestored);
RunTest("msbuild integration uses solution path default", TestMsBuildIntegrationUsesSolutionPathDefault);
RunTest("msbuild integration included entrypoints override", TestMsBuildIntegrationIncludedEntrypointsOverride);
RunTest("msbuild integration emits success message when enabled", TestMsBuildIntegrationEmitsSuccessMessageWhenEnabled);
RunTest("analyzer disabled returns empty", TestAnalyzerDisabledReturnsEmpty);
RunTest("analyzer missing repository root reports diagnostic", TestAnalyzerMissingRepositoryRootReportsDiagnostic);
RunTest("analyzer parse failure reports diagnostic", TestAnalyzerParseFailureReportsDiagnostic);
RunTest("included entrypoints allowlist", TestIncludedEntrypointsAllowlist);
RunTest("included entrypoints exclude wins", TestIncludedEntrypointsExcludeWins);
RunTest("excluded package ids blacklist", TestExcludedPackageIds);
RunTest("excluded package ids ignore casing", TestExcludedPackageIdsIgnoreCasing);
RunTest("included package ids allowlist", TestIncludedPackageIdsAllowlist);
RunTest("included package ids exclude wins", TestIncludedPackageIdsExcludeWins);
RunTest("direct only filters transitive", TestDirectOnlyFiltersTransitive);
RunTest("runtime only filters non-runtime packages", TestRuntimeOnlyFiltersNonRuntimePackages);
RunTest("runtime only without targets treats packages as runtime", TestRuntimeOnlyWithoutTargetsTreatsPackagesAsRuntime);
RunTest("project path fallback infers supported SDK project types", TestProjectPathFallbackInfersSupportedSdkProjectTypes);
RunTest("project path fallback rejects ambiguous project directories", TestProjectPathFallbackRejectsAmbiguousProjectDirectory);
RunTest("solution scope filters non-solution projects", TestSolutionScopeFiltersNonSolutionProjects);
RunTest("solution scope warns when projects are unrestored", TestSolutionScopeWarnsWhenProjectsAreUnrestored);
RunTest("solution scope missing assets ignores excluded entrypoints", TestSolutionScopeMissingAssetsIgnoresExcludedEntrypoints);
RunTest("excluded entrypoints blacklist", TestExcludedEntrypoints);
RunTest("rule off suppresses mismatch", TestRuleOffSuppressesMismatch);
RunTest("rule mode override", TestRuleModeOverride);
RunTest("rule matching ignores package id casing", TestRuleMatchingIgnoresPackageIdCasing);
RunTest("rule versions allow listed", TestRuleVersionsAllowListed);
RunTest("rule versions detect out-of-rule versions", TestRuleVersionsOutOfRule);
RunTest("package aggregation ignores package id casing", TestPackageAggregationIgnoresPackageIdCasing);
RunTest("resolver solution scope loads slnx projects", TestResolverSolutionScopeLoadsSlnxProjects);
RunTest("resolver solution scope loads sln projects", TestResolverSolutionScopeLoadsSlnProjects);
RunTest("resolver solution scope no file falls back", TestResolverSolutionScopeNoFileFallsBack);
RunTest("resolver solution scope missing file falls back", TestResolverSolutionScopeMissingFileFallsBack);
RunTest("resolver solution scope unsupported file falls back", TestResolverSolutionScopeUnsupportedFileFallsBack);
RunTest("resolver solution scope malformed file falls back", TestResolverSolutionScopeMalformedFileFallsBack);
RunTest("resolver discovers config in parent", TestResolverDiscoversConfigInParent);
RunTest("resolver discovers git repository root", TestResolverDiscoversGitRepositoryRoot);
RunTest("resolver include entrypoints", TestResolverIncludeEntrypoints);
RunTest("resolver exclude package ids", TestResolverExcludePackageIds);
RunTest("resolver include package ids", TestResolverIncludePackageIds);
RunTest("resolver package id collections ignore casing", TestResolverPackageIdCollectionsIgnoreCasing);
RunTest("resolver config scope applies", TestResolverConfigScopeApplies);
RunTest("resolver invalid mode reports diagnostics", TestResolverInvalidModeReportsDiagnostics);
RunTest("resolver invalid boolean overrides report diagnostics", TestResolverInvalidBooleanOverridesReportDiagnostics);
RunTest("resolver invalid rule mode reports diagnostic", TestResolverInvalidRuleModeReportsDiagnostic);
RunTest("resolver invalid config file reports diagnostic", TestResolverInvalidConfigFileReportsDiagnostic);
RunTest("resolver config accepts schema property", TestResolverConfigAcceptsSchemaProperty);
RunTest("resolver rule versions", TestResolverRuleVersions);
RunTest("resolver direct/runtime flags", TestResolverDirectRuntimeFlags);
RunTest("resolver scope override", TestResolverScopeOverride);
RunTest("resolver invalid scope reports diagnostics", TestResolverInvalidScopeReportsDiagnostics);
RunTest("resolver direct/runtime empty overrides keep config", TestResolverDirectRuntimeEmptyOverridesKeepConfig);
RunTest("resolver mode empty override keeps config", TestResolverModeEmptyOverrideKeepsConfig);
RunTest("resolver includes empty overrides keep config", TestResolverIncludesEmptyOverridesKeepConfig);
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

string GetCurrentTestTargetFramework()
{
#if NET10_0
    return "net10.0";
#elif NET9_0
    return "net9.0";
#elif NET8_0
    return "net8.0";
#else
    throw new InvalidOperationException("Unsupported test target framework.");
#endif
}

void TestTaskDisabledLogsMessage()
{
    string root = CreateTempRoot();
    try
    {
        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "false",
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Disabled task should succeed.");
        Expect(buildEngine.Errors.Count == 0, "Disabled task should not log errors.");
        Expect(
            buildEngine.Messages.Any(x => x.Contains("disabled", StringComparison.OrdinalIgnoreCase)),
            "Disabled task should log a disabled message.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskNoMismatchStaysQuietByDefault()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Task.Same", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Task.Same", "1.0.0"));

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Task should succeed when no mismatches exist.");
        Expect(buildEngine.Errors.Count == 0, "Task should not log errors when no mismatches exist.");
        Expect(buildEngine.Warnings.Count == 0, "Task should not log warnings when no mismatches exist.");
        Expect(
            !buildEngine.Messages.Any(x => x.Contains("no mismatch found", StringComparison.OrdinalIgnoreCase)),
            "Task should stay quiet by default when no mismatches are found.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskEmitSuccessMessageLogsSummary()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Task.Same", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Task.Same", "1.0.0"));

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            EmitSuccessMessage = "true",
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Task should succeed when no mismatches exist.");
        Expect(buildEngine.Errors.Count == 0, "Task should not log errors when no mismatches exist.");
        Expect(
            buildEngine.Messages.Any(x => x.Contains("no mismatch found", StringComparison.OrdinalIgnoreCase)),
            "Task should log a summary when success messages are enabled.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskInvalidEmitSuccessMessageWarnsAndStaysQuiet()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Task.Same", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Task.Same", "1.0.0"));

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            EmitSuccessMessage = "not-a-bool",
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Task should still succeed when emitSuccessMessage is invalid.");
        Expect(
            buildEngine.Warnings.Any(x => x.Contains("Unknown emitSuccessMessage value", StringComparison.OrdinalIgnoreCase)),
            "Invalid emitSuccessMessage should report a warning.");
        Expect(
            !buildEngine.Messages.Any(x => x.Contains("no mismatch found", StringComparison.OrdinalIgnoreCase)),
            "Invalid emitSuccessMessage should not emit a success message.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskWarningModeLogsAndSucceeds()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Task.Warning", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Task.Warning", "2.0.0"));

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            EmitSuccessMessage = "true",
            ModeOverride = "warning",
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Warning mode should not fail the task.");
        Expect(buildEngine.Errors.Count == 0, "Warning mode should not log errors.");
        Expect(
            buildEngine.Warnings.Any(x => x.Contains("ResolutionGuard.NuGet mismatch", StringComparison.OrdinalIgnoreCase)),
            "Warning mode should log a warning mismatch.");
        Expect(
            !buildEngine.Messages.Any(x => x.Contains("no mismatch found", StringComparison.OrdinalIgnoreCase)),
            "Task should not log a success message when mismatches exist.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskErrorModeLogsAndFails()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Task.Error", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Task.Error", "2.0.0"));

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            ModeOverride = "error",
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(!succeeded, "Error mode should fail the task.");
        Expect(
            buildEngine.Errors.Any(x => x.Contains("ResolutionGuard.NuGet mismatch", StringComparison.OrdinalIgnoreCase)),
            "Error mode should log an error mismatch.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskIncludedPackageIdsAllowlist()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(
            root,
            "src/AppA/AppA.csproj",
            ("Tracked.Package", "1.0.0"),
            ("Ignored.Package", "1.0.0"));
        WriteProjectAssets(
            root,
            "src/AppB/AppB.csproj",
            ("Tracked.Package", "1.0.0"),
            ("Ignored.Package", "2.0.0"));

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            IncludedPackageIds = "Tracked.Package",
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Task should succeed when mismatches exist only outside includePackageIds.");
        Expect(buildEngine.Errors.Count == 0, "Task should not log errors for excluded package mismatches.");
        Expect(buildEngine.Warnings.Count == 0, "Task should stay quiet when included packages have no mismatches.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestTaskSolutionScopeWarnsWhenProjectsAreUnrestored()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Scope", "1.0.0"));
        EnsureProjectFile(root, "src/AppB/AppB.csproj");

        string solutionPath = Path.Combine(root, "Repo.slnx");
        File.WriteAllText(
            solutionPath,
            """
            <Solution>
              <Project Path="src/AppA/AppA.csproj" />
              <Project Path="src/AppB/AppB.csproj" />
            </Solution>
            """);

        RecordingBuildEngine buildEngine = new();
        ResolutionGuardNuGetTask task = new()
        {
            BuildEngine = buildEngine,
            Enabled = "true",
            ScopeOverride = "solution",
            SolutionFile = solutionPath,
            RepositoryRoot = root,
            ProjectDirectory = root,
        };

        bool succeeded = task.Execute();

        Expect(succeeded, "Task should succeed when only a missing-assets warning is reported.");
        Expect(
            buildEngine.Warnings.Any(x =>
                x.Contains("restored subset", StringComparison.OrdinalIgnoreCase)
                && x.Contains("AppB.csproj", StringComparison.OrdinalIgnoreCase)),
            "Task should warn when a solution project has no project.assets.json.");
        Expect(buildEngine.Errors.Count == 0, "Missing assets warning should not fail the task.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestMsBuildIntegrationUsesSolutionPathDefault()
{
    string root = CreateTempRoot();
    try
    {
        MsBuildIntegrationFixture fixture = CreateMsBuildIntegrationFixture(root, initialScope: "repository");

        CommandResult repositoryResult = RunDotNet(
            $"build \"{fixture.SolutionFilePath}\" --nologo -v:minimal",
            root);

        Expect(
            !repositoryResult.Succeeded,
            $"Repository scope should fail due to an out-of-solution mismatch.{Environment.NewLine}{repositoryResult.Output}");
        Expect(
            repositoryResult.Output.Contains("ResolutionGuard.NuGet mismatch", StringComparison.OrdinalIgnoreCase),
            $"Repository scope build should report a mismatch.{Environment.NewLine}{repositoryResult.Output}");

        SetMsBuildIntegrationScope(fixture.AppAProjectPath, "solution");

        CommandResult solutionResult = RunDotNet(
            $"build \"{fixture.SolutionFilePath}\" --no-restore --nologo -v:minimal",
            root);

        Expect(
            solutionResult.Succeeded,
            $"Solution scope should succeed when only the solution project is considered.{Environment.NewLine}{solutionResult.Output}");
        Expect(
            File.Exists(fixture.AppBAssetsPath),
            "Out-of-solution project assets should exist so the success case proves solution filtering.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestMsBuildIntegrationIncludedEntrypointsOverride()
{
    string root = CreateTempRoot();
    try
    {
        MsBuildIntegrationFixture fixture = CreateMsBuildIntegrationFixture(root, initialScope: "repository");
        SetMsBuildIntegrationIncludedEntrypoints(fixture.AppAProjectPath, "src/AppA/AppA.csproj");

        CommandResult result = RunDotNet(
            $"build \"{fixture.SolutionFilePath}\" --nologo -v:minimal",
            root);

        Expect(
            result.Succeeded,
            $"Repository-scoped build should succeed when included entrypoints narrow analysis to AppA.{Environment.NewLine}{result.Output}");
        Expect(
            !result.Output.Contains("ResolutionGuard.NuGet mismatch", StringComparison.OrdinalIgnoreCase),
            $"Included entrypoints override should suppress the out-of-scope mismatch.{Environment.NewLine}{result.Output}");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestMsBuildIntegrationEmitsSuccessMessageWhenEnabled()
{
    string root = CreateTempRoot();
    try
    {
        MsBuildIntegrationFixture fixture = CreateMsBuildIntegrationFixture(root, initialScope: "solution");
        SetMsBuildIntegrationEmitSuccessMessage(fixture.AppAProjectPath, "true");

        CommandResult result = RunDotNet(
            $"build \"{fixture.SolutionFilePath}\" --nologo -v:minimal",
            root);

        Expect(
            result.Succeeded,
            $"Solution-scoped build with success messages enabled should succeed.{Environment.NewLine}{result.Output}");
        Expect(
            result.Output.Contains("no mismatch found", StringComparison.OrdinalIgnoreCase),
            $"Build output should include the success message when ResolutionGuardNuGetEmitSuccessMessage=true.{Environment.NewLine}{result.Output}");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
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

void TestAnalyzerDisabledReturnsEmpty()
{
    string root = CreateTempRoot();
    try
    {
        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning);
        settings.Enabled = false;

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);

        Expect(result.Mismatches.Count == 0, "Disabled analyzer should not produce mismatches.");
        Expect(result.Diagnostics.Count == 0, "Disabled analyzer should not produce diagnostics.");
        Expect(result.AssetsFileCount == 0, "Disabled analyzer should not scan assets.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestAnalyzerMissingRepositoryRootReportsDiagnostic()
{
    string root = CreateTempRoot();
    try
    {
        GuardSettings settings = CreateSettings(
            Path.Combine(root, "missing"),
            mode: GuardMode.Warning);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);

        Expect(result.Mismatches.Count == 0, "Missing repository root should not produce mismatches.");
        Expect(
            result.Diagnostics.Any(x => x.Contains("does not exist", StringComparison.OrdinalIgnoreCase)),
            "Missing repository root should produce a diagnostic.");
        Expect(result.AssetsFileCount == 0, "Missing repository root should not scan assets.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestAnalyzerParseFailureReportsDiagnostic()
{
    string root = CreateTempRoot();
    try
    {
        WriteRawProjectAssets(root, "src/AppA/AppA.csproj", "{ invalid json");

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);

        Expect(result.Mismatches.Count == 0, "Parse failure should not produce mismatches.");
        Expect(result.AssetsFileCount == 1, "Invalid assets file should still be counted.");
        Expect(
            result.Diagnostics.Any(x => x.Contains("Failed to parse", StringComparison.OrdinalIgnoreCase)),
            "Parse failure should produce a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestIncludedEntrypointsAllowlist()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Allow", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Allow", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            includedEntrypoints: [appA]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "includeEntrypoints should narrow analysis to the selected project set.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestIncludedEntrypointsExcludeWins()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Allow", "1.0.0"));
        string appB = WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Allow", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            includedEntrypoints: [appA, appB],
            excludedEntrypoints: [appB]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "excludeEntrypoints should win over includeEntrypoints.");
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

void TestIncludedPackageIdsAllowlist()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(
            root,
            "src/AppA/AppA.csproj",
            ("Tracked.Package", "1.0.0"),
            ("Ignored.Package", "1.0.0"));
        WriteProjectAssets(
            root,
            "src/AppB/AppB.csproj",
            ("Tracked.Package", "1.0.0"),
            ("Ignored.Package", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            includedPackageIds: ["Tracked.Package"]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "includePackageIds should ignore mismatches outside the allowlist.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestIncludedPackageIdsExcludeWins()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Ignored.Package", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Ignored.Package", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            includedPackageIds: ["Ignored.Package"],
            excludedPackageIds: ["Ignored.Package"]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "excludePackageIds should win over includePackageIds.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestExcludedPackageIdsIgnoreCasing()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Legacy.SDK", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("LEGACY.sdk", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            excludedPackageIds: ["legacy.sdk"]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "excludePackageIds should ignore package ID casing.");
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

void TestRuntimeOnlyWithoutTargetsTreatsPackagesAsRuntime()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssetsDetailedWithOptions(
            root,
            "src/AppA/AppA.csproj",
            includeTargets: false,
            includeRestoreProjectPath: true,
            ("NoTargets.Package", "1.0.0", true, true));
        WriteProjectAssetsDetailedWithOptions(
            root,
            "src/AppB/AppB.csproj",
            includeTargets: false,
            includeRestoreProjectPath: true,
            ("NoTargets.Package", "2.0.0", true, true));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            runtimeOnly: true);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 1, "Missing targets should be treated as runtime-inclusive.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestProjectPathFallbackInfersSupportedSdkProjectTypes()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssetsDetailedWithOptions(
            root,
            "src/AppA/AppA.fsproj",
            includeTargets: true,
            includeRestoreProjectPath: false,
            ("Fallback.Package", "1.0.0", true, true));
        string appB = WriteProjectAssetsDetailedWithOptions(
            root,
            "src/AppB/AppB.vbproj",
            includeTargets: true,
            includeRestoreProjectPath: false,
            ("Fallback.Package", "2.0.0", true, true));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);

        Expect(result.Mismatches.Count == 1, "Fallback project-path resolution should still produce a mismatch.");
        PackageMismatch mismatch = result.Mismatches[0];
        Expect(mismatch.VersionMap["1.0.0"].Any(x => x.Path == appA), "Fallback should infer AppA.fsproj.");
        Expect(mismatch.VersionMap["2.0.0"].Any(x => x.Path == appB), "Fallback should infer AppB.vbproj.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestProjectPathFallbackRejectsAmbiguousProjectDirectory()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssetsDetailedWithOptions(
            root,
            "src/Mixed/App.fsproj",
            includeTargets: true,
            includeRestoreProjectPath: false,
            ("Fallback.Package", "1.0.0", true, true));
        string competingProject = EnsureProjectFile(root, "src/Mixed/App.csproj");

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);

        Expect(result.Mismatches.Count == 0, "Ambiguous fallback should skip the assets file instead of guessing a project path.");
        Expect(
            result.Diagnostics.Any(x =>
                x.Contains("Failed to resolve a project path", StringComparison.OrdinalIgnoreCase)
                && x.Contains(competingProject, StringComparison.OrdinalIgnoreCase)
                && x.Contains("App.fsproj", StringComparison.OrdinalIgnoreCase)),
            "Ambiguous fallback should report the competing SDK-style project paths.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestSolutionScopeFiltersNonSolutionProjects()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Scope", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Example.Scope", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Error,
            scope: GuardScope.Solution,
            includedEntrypoints: [appA]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "solution scope should ignore projects outside the solution.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestSolutionScopeWarnsWhenProjectsAreUnrestored()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Scope", "1.0.0"));
        string appB = EnsureProjectFile(root, "src/AppB/AppB.csproj");

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            scope: GuardScope.Solution,
            includedEntrypoints: [appA, appB]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "A single restored solution project should not produce a mismatch.");
        Expect(
            result.Diagnostics.Any(x =>
                x.Contains("restored subset", StringComparison.OrdinalIgnoreCase)
                && x.Contains(appB, StringComparison.OrdinalIgnoreCase)),
            "solution scope should warn when a listed project has no project.assets.json.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestSolutionScopeMissingAssetsIgnoresExcludedEntrypoints()
{
    string root = CreateTempRoot();
    try
    {
        string appA = WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Example.Scope", "1.0.0"));
        string appB = EnsureProjectFile(root, "src/AppB/AppB.csproj");

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            scope: GuardScope.Solution,
            includedEntrypoints: [appA, appB],
            excludedEntrypoints: [appB]);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(
            !result.Diagnostics.Any(x => x.Contains("restored subset", StringComparison.OrdinalIgnoreCase)),
            "Excluded solution entrypoints should not trigger missing-assets warnings.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestRuleOffSuppressesMismatch()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Suppressed.Package", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("Suppressed.Package", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            rules: new Dictionary<string, GuardRule>(GuardPackageIdComparer.StringComparer)
            {
                ["Suppressed.Package"] = CreateRule(GuardMode.Off),
            });

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 0, "Rule mode off should suppress mismatches.");
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
            rules: new Dictionary<string, GuardRule>(GuardPackageIdComparer.StringComparer)
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

void TestRuleMatchingIgnoresPackageIdCasing()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Newtonsoft.Json", "12.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("newtonsoft.json", "13.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning,
            rules: new Dictionary<string, GuardRule>(GuardPackageIdComparer.StringComparer)
            {
                ["NEWTONSOFT.JSON"] = CreateRule(GuardMode.Error),
            });

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 1, "Expected one mismatch.");
        Expect(result.Mismatches[0].Mode == GuardMode.Error, "Rule lookup should ignore package ID casing.");
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
            rules: new Dictionary<string, GuardRule>(GuardPackageIdComparer.StringComparer)
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
            rules: new Dictionary<string, GuardRule>(GuardPackageIdComparer.StringComparer)
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

void TestPackageAggregationIgnoresPackageIdCasing()
{
    string root = CreateTempRoot();
    try
    {
        WriteProjectAssets(root, "src/AppA/AppA.csproj", ("Plugin.SDK", "1.0.0"));
        WriteProjectAssets(root, "src/AppB/AppB.csproj", ("plugin.sdk", "2.0.0"));

        GuardSettings settings = CreateSettings(
            root,
            mode: GuardMode.Warning);

        GuardAnalysisResult result = ResolutionGuardNuGetAnalyzer.Analyze(settings);
        Expect(result.Mismatches.Count == 1, "Package aggregation should treat package IDs case-insensitively.");
        Expect(result.Mismatches[0].VersionMap.Count == 2, "Both versions should be aggregated into the same mismatch.");
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

void TestResolverSolutionScopeLoadsSlnxProjects()
{
    string root = CreateTempRoot();
    try
    {
        string appA = Normalize(Path.Combine(root, "src/AppA/AppA.fsproj"));
        string appB = Normalize(Path.Combine(root, "src/AppB/AppB.vbproj"));
        string solutionPath = Path.Combine(root, "Repo.slnx");

        Directory.CreateDirectory(Path.GetDirectoryName(appA) ?? root);
        Directory.CreateDirectory(Path.GetDirectoryName(appB) ?? root);
        File.WriteAllText(
            solutionPath,
            """
            <Solution>
              <Project Path="src/AppA/AppA.fsproj" />
              <Folder Name="/nested/">
                <Project Path="src/AppB/AppB.vbproj" />
              </Folder>
            </Solution>
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: solutionPath,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Solution, "Scope should remain solution.");
        Expect(resolved.Settings.IncludedEntrypoints.Contains(appA), "slnx should include AppA.");
        Expect(resolved.Settings.IncludedEntrypoints.Contains(appB), "slnx should include AppB.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverSolutionScopeLoadsSlnProjects()
{
    string root = CreateTempRoot();
    try
    {
        string appA = Normalize(Path.Combine(root, "src/AppA/AppA.fsproj"));
        string appB = Normalize(Path.Combine(root, "src/AppB/AppB.vbproj"));
        string solutionPath = Path.Combine(root, "Repo.sln");

        Directory.CreateDirectory(Path.GetDirectoryName(appA) ?? root);
        Directory.CreateDirectory(Path.GetDirectoryName(appB) ?? root);
        File.WriteAllText(
            solutionPath,
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{F2A71F9B-5D33-465A-A702-920D77279786}") = "AppA", "src\AppA\AppA.fsproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "AppB", "src\AppB\AppB.vbproj", "{33333333-3333-3333-3333-333333333333}"
            EndProject
            Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "src", "src", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Global
            EndGlobal
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: solutionPath,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Solution, "Scope should remain solution.");
        Expect(resolved.Settings.IncludedEntrypoints.Contains(appA), "sln should include AppA.");
        Expect(resolved.Settings.IncludedEntrypoints.Contains(appB), "sln should include AppB.");
        Expect(resolved.Settings.IncludedEntrypoints.Count == 2, "Solution folders should be ignored.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverSolutionScopeMissingFileFallsBack()
{
    string root = CreateTempRoot();
    try
    {
        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: Path.Combine(root, "missing.slnx"),
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Repository, "Missing solution file should fall back to repository scope.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("does not exist", StringComparison.OrdinalIgnoreCase)),
            "Missing solution file should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverSolutionScopeNoFileFallsBack()
{
    string root = CreateTempRoot();
    try
    {
        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: null,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Repository, "Missing solution path should fall back to repository scope.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("no solution file was provided", StringComparison.OrdinalIgnoreCase)),
            "Missing solution path should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverSolutionScopeUnsupportedFileFallsBack()
{
    string root = CreateTempRoot();
    try
    {
        string solutionPath = Path.Combine(root, "Repo.txt");
        File.WriteAllText(solutionPath, "not a solution");

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: solutionPath,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Repository, "Unsupported solution file should fall back to repository scope.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unsupported solution file", StringComparison.OrdinalIgnoreCase)),
            "Unsupported solution file should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverSolutionScopeMalformedFileFallsBack()
{
    string root = CreateTempRoot();
    try
    {
        string solutionPath = Path.Combine(root, "Repo.slnx");
        File.WriteAllText(solutionPath, "<Solution>");

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: solutionPath,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Repository, "Malformed solution file should fall back to repository scope.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Failed to read solution", StringComparison.OrdinalIgnoreCase)),
            "Malformed solution file should report a parse diagnostic.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("solution scope could not be resolved", StringComparison.OrdinalIgnoreCase)),
            "Malformed solution file should report fallback diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverDiscoversConfigInParent()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        string projectDirectory = Path.Combine(root, "src", "App");
        Directory.CreateDirectory(projectDirectory);

        File.WriteAllText(
            configPath,
            """
            {
              "mode": "error"
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: projectDirectory,
            repositoryRootOverride: root,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.ConfigFilePath == Normalize(configPath), "Config should be discovered from parent directories.");
        Expect(resolved.Settings.Mode == GuardMode.Error, "Discovered config should be applied.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverDiscoversGitRepositoryRoot()
{
    string root = CreateTempRoot();
    try
    {
        string projectDirectory = Path.Combine(root, "src", "App");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(Path.Combine(root, ".git"));

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: projectDirectory,
            repositoryRootOverride: null,
            configFileOverride: null,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.RepositoryRoot == Normalize(root), "Repository root should resolve to the nearest .git ancestor.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverIncludeEntrypoints()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        EnsureProjectFile(root, "src/AppA/AppA.csproj");
        string appB = EnsureProjectFile(root, "src/AppB/AppB.csproj");

        File.WriteAllText(
            configPath,
            """
            {
              "includeEntrypoints": [ "src/AppA/AppA.csproj" ]
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
            excludedPackageIdsOverride: null,
            includedEntrypointsOverride: "src/AppB/AppB.csproj");

        Expect(
            resolved.Settings.IncludedEntrypoints.Contains(appB),
            "includeEntrypoints override should replace the config list.");
        Expect(
            resolved.Settings.IncludedEntrypoints.Count == 1,
            "includeEntrypoints override should leave only the override paths.");
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

void TestResolverIncludePackageIds()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "includePackageIds": [ "Package.FromConfig" ]
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
            excludedPackageIdsOverride: null,
            includedPackageIdsOverride: "Package.FromProperty");

        Expect(
            resolved.Settings.IncludedPackageIds.Contains("Package.FromProperty"),
            "include package ids override should apply as an allowlist.");
        Expect(
            !resolved.Settings.IncludedPackageIds.Contains("Package.FromConfig"),
            "include package ids override should replace the config list.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverPackageIdCollectionsIgnoreCasing()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "includePackageIds": [ "Package.FromAllowlist" ],
              "excludePackageIds": [ "Package.FromConfig" ],
              "rules": [
                {
                  "packageId": "Plugin.SDK",
                  "mode": "error"
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

        Expect(
            resolved.Settings.IncludedPackageIds.Contains("package.fromallowlist"),
            "Resolved includePackageIds should ignore package ID casing.");
        Expect(
            resolved.Settings.ExcludedPackageIds.Contains("package.fromconfig"),
            "Resolved excludePackageIds should ignore package ID casing.");
        Expect(
            resolved.Settings.IncludedPackageIds.Contains("PACKAGE.FROMALLOWLIST"),
            "Resolved includePackageIds should preserve case-insensitive lookups.");
        Expect(
            resolved.Settings.Rules.TryGetValue("plugin.sdk", out GuardRule? rule),
            "Resolved rules should ignore package ID casing.");
        Expect(rule is not null && rule.Mode == GuardMode.Error, "Resolved rule mode should be preserved.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverInvalidModeReportsDiagnostics()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "mode": "invalid-from-config"
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: "invalid-from-override",
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Mode == GuardMode.Warning, "Invalid modes should keep the default warning mode.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown mode 'invalid-from-config'", StringComparison.OrdinalIgnoreCase)),
            "Invalid config mode should report a diagnostic.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown mode override 'invalid-from-override'", StringComparison.OrdinalIgnoreCase)),
            "Invalid mode override should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverInvalidBooleanOverridesReportDiagnostics()
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
            directOnlyOverride: "not-a-bool",
            runtimeOnlyOverride: "still-not-a-bool",
            enabledOverride: "true",
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.DirectOnly, "Invalid directOnly override should keep config value.");
        Expect(!resolved.Settings.RuntimeOnly, "Invalid runtimeOnly override should keep config value.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown directOnly override", StringComparison.OrdinalIgnoreCase)),
            "Invalid directOnly override should report a diagnostic.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown runtimeOnly override", StringComparison.OrdinalIgnoreCase)),
            "Invalid runtimeOnly override should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverInvalidRuleModeReportsDiagnostic()
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
                  "packageId": "Invalid.Rule.Package",
                  "mode": "unsupported"
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

        Expect(
            !resolved.Settings.Rules.ContainsKey("Invalid.Rule.Package"),
            "Rule with invalid mode should be ignored.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown rule mode", StringComparison.OrdinalIgnoreCase)),
            "Invalid rule mode should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverInvalidConfigFileReportsDiagnostic()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(configPath, "{ invalid json");

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

        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Failed to read config", StringComparison.OrdinalIgnoreCase)),
            "Malformed config should report a diagnostic.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverConfigAcceptsSchemaProperty()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "$schema": "https://raw.githubusercontent.com/iwizsophy/resolution-guard-nuget/main/nuget-resolution-guard.schema.json",
              "mode": "error",
              "excludePackageIds": [ "Microsoft.NETCore.App" ]
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

        Expect(resolved.Diagnostics.Count == 0, "$schema metadata should not make config loading fail.");
        Expect(resolved.Settings.Mode == GuardMode.Error, "Config values should still be applied when $schema is present.");
        Expect(
            resolved.Settings.ExcludedPackageIds.Contains("Microsoft.NETCore.App"),
            "$schema metadata should not interfere with package-id settings.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverConfigScopeApplies()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        string appA = Normalize(Path.Combine(root, "src/AppA/AppA.csproj"));
        string solutionPath = Path.Combine(root, "Repo.slnx");

        Directory.CreateDirectory(Path.GetDirectoryName(appA) ?? root);
        File.WriteAllText(
            configPath,
            """
            {
              "scope": "solution"
            }
            """);
        File.WriteAllText(
            solutionPath,
            """
            <Solution>
              <Project Path="src/AppA/AppA.csproj" />
            </Solution>
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: null,
            enabledOverride: "true",
            solutionFileOverride: solutionPath,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Solution, "Config scope should apply when override is absent.");
        Expect(resolved.Settings.IncludedEntrypoints.Contains(appA), "Config scope should load solution projects.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverScopeOverride()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        string solutionPath = Path.Combine(root, "Repo.slnx");
        File.WriteAllText(
            configPath,
            """
            {
              "scope": "repository"
            }
            """);
        File.WriteAllText(
            solutionPath,
            """
            <Solution />
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "solution",
            enabledOverride: "true",
            solutionFileOverride: solutionPath,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Solution, "scope override should apply.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

void TestResolverInvalidScopeReportsDiagnostics()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        File.WriteAllText(
            configPath,
            """
            {
              "scope": "invalid-from-config"
            }
            """);

        GuardSettingsResolution resolved = GuardSettingsResolver.Resolve(
            projectDirectory: root,
            repositoryRootOverride: root,
            configFileOverride: configPath,
            modeOverride: null,
            directOnlyOverride: null,
            runtimeOnlyOverride: null,
            scopeOverride: "invalid-from-override",
            enabledOverride: "true",
            solutionFileOverride: null,
            excludedEntrypointsOverride: null,
            excludedPackageIdsOverride: null);

        Expect(resolved.Settings.Scope == GuardScope.Repository, "Invalid scopes should keep repository scope.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown scope 'invalid-from-config'", StringComparison.OrdinalIgnoreCase)),
            "Invalid config scope should report a diagnostic.");
        Expect(
            resolved.Diagnostics.Any(x => x.Contains("Unknown scope override 'invalid-from-override'", StringComparison.OrdinalIgnoreCase)),
            "Invalid scope override should report a diagnostic.");
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

void TestResolverIncludesEmptyOverridesKeepConfig()
{
    string root = CreateTempRoot();
    try
    {
        string configPath = Path.Combine(root, "nuget-resolution-guard.json");
        string appA = EnsureProjectFile(root, "src/AppA/AppA.csproj");

        File.WriteAllText(
            configPath,
            """
            {
              "includeEntrypoints": [ "src/AppA/AppA.csproj" ],
              "includePackageIds": [ "Package.FromConfig" ]
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
            excludedPackageIdsOverride: null,
            includedEntrypointsOverride: "",
            includedPackageIdsOverride: "");

        Expect(
            resolved.Settings.IncludedEntrypoints.Contains(appA),
            "Empty includeEntrypoints override should not replace config.");
        Expect(
            resolved.Settings.IncludedPackageIds.Contains("Package.FromConfig"),
            "Empty includePackageIds override should not replace config.");
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
    GuardScope scope = GuardScope.Repository,
    bool directOnly = false,
    bool runtimeOnly = false,
    IEnumerable<string>? includedEntrypoints = null,
    IEnumerable<string>? excludedEntrypoints = null,
    IEnumerable<string>? includedPackageIds = null,
    IEnumerable<string>? excludedPackageIds = null,
    IDictionary<string, GuardRule>? rules = null)
{
    return new GuardSettings
    {
        Enabled = true,
        Mode = mode,
        Scope = scope,
        DirectOnly = directOnly,
        RuntimeOnly = runtimeOnly,
        RepositoryRoot = Normalize(root),
        ProjectDirectory = Normalize(root),
        ConfigFilePath = null,
        IncludedEntrypoints = new HashSet<string>((includedEntrypoints ?? []).Select(Normalize), GuardPathComparer.StringComparer),
        ExcludedEntrypoints = new HashSet<string>((excludedEntrypoints ?? []).Select(Normalize), GuardPathComparer.StringComparer),
        IncludedPackageIds = new HashSet<string>(includedPackageIds ?? [], GuardPackageIdComparer.StringComparer),
        ExcludedPackageIds = new HashSet<string>(excludedPackageIds ?? [], GuardPackageIdComparer.StringComparer),
        Rules = CloneRules(rules),
    };
}

IDictionary<string, GuardRule> CloneRules(IDictionary<string, GuardRule>? rules)
{
    Dictionary<string, GuardRule> result = new(GuardPackageIdComparer.StringComparer);
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

string EnsureProjectFile(string root, string projectRelativePath)
{
    string projectPath = Normalize(Path.Combine(root, projectRelativePath));
    string projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Project directory missing.");
    Directory.CreateDirectory(projectDirectory);

    if (!File.Exists(projectPath))
    {
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    return projectPath;
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
    return WriteProjectAssetsDetailedWithOptions(
        root,
        projectRelativePath,
        includeTargets: true,
        includeRestoreProjectPath: true,
        packages: packages);
}

string WriteProjectAssetsDetailedWithOptions(
    string root,
    string projectRelativePath,
    bool includeTargets,
    bool includeRestoreProjectPath,
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
                [$"lib/{CurrentTestTargetFramework}/{package.PackageId}.dll"] = new Dictionary<string, object?>(),
            };
        }
        else
        {
            targetLibrary["compile"] = new Dictionary<string, object?>
            {
                [$"ref/{CurrentTestTargetFramework}/{package.PackageId}.dll"] = new Dictionary<string, object?>(),
            };
        }

        if (includeTargets)
        {
            targetLibraries[$"{package.PackageId}/{package.Version}"] = targetLibrary;
        }

        if (package.IsDirect)
        {
            directDependencies[package.PackageId] = package.Version;
        }
    }

    var projectNode = new Dictionary<string, object?>();
    if (includeRestoreProjectPath)
    {
        projectNode["restore"] = new Dictionary<string, object?>
        {
            ["projectPath"] = projectPath,
        };
    }

    projectNode["frameworks"] = new Dictionary<string, object?>
    {
        [CurrentTestTargetFramework] = new Dictionary<string, object?>
        {
            ["dependencies"] = directDependencies,
        },
    };

    var jsonModel = new Dictionary<string, object?>
    {
        ["version"] = 3,
        ["libraries"] = libraries,
        ["project"] = projectNode,
    };

    if (includeTargets)
    {
        jsonModel["targets"] = new Dictionary<string, object?>
        {
            [CurrentTestTargetFramework] = targetLibraries,
        };
    }

    string objDirectory = Path.Combine(projectDirectory, "obj");
    Directory.CreateDirectory(objDirectory);

    string assetsPath = Path.Combine(objDirectory, "project.assets.json");
    string json = JsonSerializer.Serialize(jsonModel, CachedJsonSerializerOptions);
    File.WriteAllText(assetsPath, json);

    return projectPath;
}

string WriteRawProjectAssets(string root, string projectRelativePath, string rawJson)
{
    string projectPath = Normalize(Path.Combine(root, projectRelativePath));
    string projectDirectory = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Project directory missing.");
    Directory.CreateDirectory(projectDirectory);

    if (!File.Exists(projectPath))
    {
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    string objDirectory = Path.Combine(projectDirectory, "obj");
    Directory.CreateDirectory(objDirectory);
    string assetsPath = Path.Combine(objDirectory, "project.assets.json");
    File.WriteAllText(assetsPath, rawJson);
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

MsBuildIntegrationFixture CreateMsBuildIntegrationFixture(string root, string initialScope)
{
    string feedDirectory = Path.Combine(root, "feed");
    string packageSourceDirectory = Path.Combine(root, "pkgsrc");
    string packageProjectPath = Path.Combine(packageSourceDirectory, "Fixture.Dependency.csproj");
    string appAProjectPath = Path.Combine(root, "src", "AppA", "AppA.csproj");
    string appBProjectPath = Path.Combine(root, "src", "AppB", "AppB.csproj");
    string solutionFilePath = Path.Combine(root, "Repo.slnx");

    Directory.CreateDirectory(feedDirectory);
    Directory.CreateDirectory(packageSourceDirectory);

    File.WriteAllText(
        packageProjectPath,
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>{CurrentTestTargetFramework}</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """);

    File.WriteAllText(
        Path.Combine(packageSourceDirectory, "Placeholder.cs"),
        "namespace Fixture.Dependency; public sealed class Placeholder { }");

    CommandResult packV1 = RunDotNet(
        $"pack \"{packageProjectPath}\" -p:PackageId=Fixture.Dependency -p:Version=1.0.0 -o \"{feedDirectory}\" --nologo -v:minimal",
        root);
    Expect(packV1.Succeeded, $"Packing Fixture.Dependency 1.0.0 failed.{Environment.NewLine}{packV1.Output}");

    CommandResult packV2 = RunDotNet(
        $"pack \"{packageProjectPath}\" -p:PackageId=Fixture.Dependency -p:Version=2.0.0 -o \"{feedDirectory}\" --nologo -v:minimal",
        root);
    Expect(packV2.Succeeded, $"Packing Fixture.Dependency 2.0.0 failed.{Environment.NewLine}{packV2.Output}");

    File.WriteAllText(
        Path.Combine(root, "NuGet.Config"),
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="local" value="{EscapeXml(Normalize(feedDirectory))}" />
          </packageSources>
        </configuration>
        """);

    string guardPackageLayout = CreateGuardPackageLayout(root);

    Directory.CreateDirectory(Path.GetDirectoryName(appAProjectPath) ?? root);
    Directory.CreateDirectory(Path.GetDirectoryName(appBProjectPath) ?? root);

    WriteMsBuildIntegrationProjectFile(
        appAProjectPath,
        guardPackageLayout,
        packageVersion: "1.0.0",
        repositoryRoot: root,
        scope: initialScope,
        enableGuard: true);

    WriteMsBuildIntegrationProjectFile(
        appBProjectPath,
        guardPackageLayout,
        packageVersion: "2.0.0",
        repositoryRoot: root,
        scope: "repository",
        enableGuard: false);

    File.WriteAllText(
        solutionFilePath,
        """
        <Solution>
          <Project Path="src/AppA/AppA.csproj" />
        </Solution>
        """);

    CommandResult restoreAppB = RunDotNet(
        $"restore \"{appBProjectPath}\" --nologo -v:minimal",
        root);
    Expect(restoreAppB.Succeeded, $"Restoring AppB failed.{Environment.NewLine}{restoreAppB.Output}");

    return new MsBuildIntegrationFixture
    {
        AppAProjectPath = appAProjectPath,
        AppBAssetsPath = Path.Combine(Path.GetDirectoryName(appBProjectPath) ?? root, "obj", "project.assets.json"),
        SolutionFilePath = solutionFilePath,
    };
}

void SetMsBuildIntegrationScope(string appAProjectPath, string scope)
{
    SetMsBuildIntegrationProperty(appAProjectPath, "ResolutionGuardNuGetScope", scope);
}

void SetMsBuildIntegrationIncludedEntrypoints(string appAProjectPath, string value)
{
    SetMsBuildIntegrationProperty(appAProjectPath, "ResolutionGuardNuGetIncludedEntrypoints", value);
}

void SetMsBuildIntegrationEmitSuccessMessage(string appAProjectPath, string value)
{
    SetMsBuildIntegrationProperty(appAProjectPath, "ResolutionGuardNuGetEmitSuccessMessage", value);
}

void SetMsBuildIntegrationProperty(string projectPath, string propertyName, string value)
{
    XDocument document = XDocument.Parse(File.ReadAllText(projectPath), LoadOptions.PreserveWhitespace);
    XElement root = document.Root ?? throw new InvalidOperationException("MSBuild integration fixture project is missing a root element.");

    XNamespace ns = root.Name.Namespace;
    XName propertyGroupName = ns + "PropertyGroup";
    XName targetPropertyName = ns + propertyName;

    XElement propertyGroup = root.Elements(propertyGroupName).FirstOrDefault()
        ?? throw new InvalidOperationException("MSBuild integration fixture project is missing a PropertyGroup element.");

    XElement property = propertyGroup.Element(targetPropertyName) ?? new XElement(targetPropertyName);
    property.Value = value;

    if (property.Parent is null)
    {
        propertyGroup.Add(property);
    }

    File.WriteAllText(projectPath, document.ToString());
}

void WriteMsBuildIntegrationProjectFile(
    string projectPath,
    string guardPackageLayout,
    string packageVersion,
    string repositoryRoot,
    string scope,
    bool enableGuard)
{
    string buildTransitiveProps = Normalize(Path.Combine(guardPackageLayout, "buildTransitive", "ResolutionGuard.NuGet.props"));
    string buildTransitiveTargets = Normalize(Path.Combine(guardPackageLayout, "buildTransitive", "ResolutionGuard.NuGet.targets"));

    string projectXml =
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          {(enableGuard ? $"<Import Project=\"{EscapeXml(buildTransitiveProps)}\" />" : string.Empty)}
          <PropertyGroup>
            <TargetFramework>{CurrentTestTargetFramework}</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            {(enableGuard ? "<ResolutionGuardNuGetEnabled>true</ResolutionGuardNuGetEnabled>" : string.Empty)}
            {(enableGuard ? "<ResolutionGuardNuGetMode>error</ResolutionGuardNuGetMode>" : string.Empty)}
            {(enableGuard ? $"<ResolutionGuardNuGetScope>{EscapeXml(scope)}</ResolutionGuardNuGetScope>" : string.Empty)}
            {(enableGuard ? $"<ResolutionGuardNuGetRepositoryRoot>{EscapeXml(Normalize(repositoryRoot))}</ResolutionGuardNuGetRepositoryRoot>" : string.Empty)}
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Fixture.Dependency" Version="{EscapeXml(packageVersion)}" />
          </ItemGroup>
          {(enableGuard ? $"<Import Project=\"{EscapeXml(buildTransitiveTargets)}\" />" : string.Empty)}
        </Project>
        """;

    File.WriteAllText(projectPath, projectXml);
}

string CreateGuardPackageLayout(string root)
{
    string layoutRoot = Path.Combine(root, ".guardpkg");
    string repoRoot = FindRepositoryRootForTests();
    string sourcePackageRoot = Path.Combine(repoRoot, "src", "ResolutionGuard.NuGet.Package");
    string buildDirectory = Path.Combine(layoutRoot, "build");
    string buildTransitiveDirectory = Path.Combine(layoutRoot, "buildTransitive");
    string tasksDirectory = Path.Combine(layoutRoot, "tasks", "netstandard2.0");

    Directory.CreateDirectory(buildDirectory);
    Directory.CreateDirectory(buildTransitiveDirectory);
    Directory.CreateDirectory(tasksDirectory);

    foreach (string fileName in new[] { "ResolutionGuard.NuGet.props", "ResolutionGuard.NuGet.targets" })
    {
        File.Copy(
            Path.Combine(sourcePackageRoot, "build", fileName),
            Path.Combine(buildDirectory, fileName),
            overwrite: true);
        File.Copy(
            Path.Combine(sourcePackageRoot, "buildTransitive", fileName),
            Path.Combine(buildTransitiveDirectory, fileName),
            overwrite: true);
    }

    string taskAssemblyPath = typeof(ResolutionGuardNuGetTask).Assembly.Location;
    string taskAssemblyDirectory = Path.GetDirectoryName(taskAssemblyPath) ?? throw new InvalidOperationException("Task assembly directory missing.");

    foreach (string fileName in new[] { "ResolutionGuard.NuGet.Tasks.dll", "ResolutionGuard.NuGet.Core.dll" })
    {
        File.Copy(
            Path.Combine(taskAssemblyDirectory, fileName),
            Path.Combine(tasksDirectory, fileName),
            overwrite: true);
    }

    return layoutRoot;
}

string FindRepositoryRootForTests()
{
    DirectoryInfo? current = new(Environment.CurrentDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "ResolutionGuard.NuGet.slnx")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate the repository root for integration fixtures.");
}

string EscapeXml(string value)
{
    return value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}

CommandResult RunDotNet(string arguments, string workingDirectory)
{
    ProcessStartInfo startInfo = new("dotnet", arguments)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    using Process process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Failed to start dotnet {arguments}.");

    string standardOutput = process.StandardOutput.ReadToEnd();
    string standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();

    string output = string.Join(
        Environment.NewLine,
        new[] { standardOutput, standardError }.Where(x => !string.IsNullOrWhiteSpace(x)));

    return new CommandResult
    {
        ExitCode = process.ExitCode,
        Output = output,
    };
}

sealed class MsBuildIntegrationFixture
{
    public string AppAProjectPath { get; set; } = string.Empty;

    public string AppBAssetsPath { get; set; } = string.Empty;

    public string SolutionFilePath { get; set; } = string.Empty;
}

sealed class CommandResult
{
    public int ExitCode { get; set; }

    public string Output { get; set; } = string.Empty;

    public bool Succeeded => ExitCode == 0;
}

sealed class RecordingBuildEngine : IBuildEngine
{
    public List<string> Messages { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> Errors { get; } = [];

    public bool ContinueOnError => false;

    public int LineNumberOfTaskNode => 0;

    public int ColumnNumberOfTaskNode => 0;

    public string ProjectFileOfTaskNode => "ResolutionGuard.NuGet.Tests";

    public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
    {
        return false;
    }

    public void LogCustomEvent(CustomBuildEventArgs e)
    {
    }

    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        Errors.Add(e.Message);
    }

    public void LogMessageEvent(BuildMessageEventArgs e)
    {
        Messages.Add(e.Message);
    }

    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        Warnings.Add(e.Message);
    }
}
