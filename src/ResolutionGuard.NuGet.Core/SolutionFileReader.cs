using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ResolutionGuard.NuGet.Core;

internal static class SolutionFileReader
{
    private static readonly Regex SlnProjectLineRegex = new(
        "^Project\\(\"[^\"]+\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"(?<path>[^\"]+)\",\\s*\"[^\"]+\"\\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryRead(string solutionFilePath, out ISet<string>? projectPaths, out string? diagnostic)
    {
        projectPaths = null;
        diagnostic = null;

        try
        {
            string normalizedSolutionFilePath = NormalizePath(solutionFilePath);
            string extension = Path.GetExtension(normalizedSolutionFilePath);
            HashSet<string> parsedPaths = new(GuardPathComparer.StringComparer);

            switch (extension.ToLowerInvariant())
            {
                case ".sln":
                    ReadSln(normalizedSolutionFilePath, parsedPaths);
                    break;
                case ".slnx":
                    ReadSlnx(normalizedSolutionFilePath, parsedPaths);
                    break;
                default:
                    diagnostic = $"ResolutionGuard.NuGet: Unsupported solution file '{normalizedSolutionFilePath}'. Only .sln and .slnx are supported.";
                    return false;
            }

            projectPaths = parsedPaths;
            return true;
        }
        catch (Exception ex)
        {
            diagnostic = $"ResolutionGuard.NuGet: Failed to read solution '{solutionFilePath}'. {ex.Message}";
            return false;
        }
    }

    private static void ReadSln(string solutionFilePath, ISet<string> projectPaths)
    {
        string solutionDirectory = Path.GetDirectoryName(solutionFilePath) ?? Environment.CurrentDirectory;

        foreach (string line in File.ReadLines(solutionFilePath))
        {
            Match match = SlnProjectLineRegex.Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            string? resolvedPath = TryResolveProjectPath(match.Groups["path"].Value, solutionDirectory);
            if (resolvedPath is not null)
            {
                projectPaths.Add(resolvedPath);
            }
        }
    }

    private static void ReadSlnx(string solutionFilePath, ISet<string> projectPaths)
    {
        string solutionDirectory = Path.GetDirectoryName(solutionFilePath) ?? Environment.CurrentDirectory;
        XDocument document = XDocument.Load(solutionFilePath, LoadOptions.None);

        foreach (XElement project in document
            .Descendants()
            .Where(x => string.Equals(x.Name.LocalName, "Project", StringComparison.OrdinalIgnoreCase)))
        {
            string? resolvedPath = TryResolveProjectPath(project.Attribute("Path")?.Value, solutionDirectory);
            if (resolvedPath is not null)
            {
                projectPaths.Add(resolvedPath);
            }
        }
    }

    private static string? TryResolveProjectPath(string? value, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = (value ?? string.Empty)
            .Trim()
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (!Path.HasExtension(normalizedValue))
        {
            return null;
        }

        string resolvedPath = Path.IsPathRooted(normalizedValue)
            ? normalizedValue
            : Path.Combine(baseDirectory, normalizedValue);

        return NormalizePath(resolvedPath);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
