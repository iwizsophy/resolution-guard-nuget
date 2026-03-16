---
name: Bug report
about: Report incorrect package behavior, workflow regressions, or documentation problems.
title: "[Bug] "
labels: ["bug"]
assignees: []
---

## Summary

Describe the problem in one or two sentences.

## Reproduction

Provide the smallest practical reproduction. Include project layout, package references, or workflow steps as needed.

```xml
<!-- Optional project or Directory.Build.props snippet -->
```

## Expected behavior

Describe what you expected `ResolutionGuard.NuGet` or the repository workflow to do.

## Actual behavior

Describe what happened instead. Include diagnostics, logs, or produced package versions when relevant.

## Environment

- Package version:
- .NET SDK:
- OS:
- Build command:

## Relevant configuration

Include any `nuget-resolution-guard.json`, MSBuild properties, or solution-layout details that affect the result.

```json
{
  "mode": "warning"
}
```

## Additional context

Add screenshots, logs, related issues, or release tags if they help explain the problem.
