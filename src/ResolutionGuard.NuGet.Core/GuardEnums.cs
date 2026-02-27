namespace ResolutionGuard.NuGet.Core;

public enum GuardMode
{
    Off,
    Info,
    Warning,
    Error,
}

public enum GuardScope
{
    Repository,
    Solution,
}
