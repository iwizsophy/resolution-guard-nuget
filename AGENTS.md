# Agent Contract

All AI coding agents working in this repository MUST follow these rules:

1. Do NOT implement behavior based on assumptions.
2. Keep changes focused on the requested scope.
3. Code changes MUST include tests or a clear justification when tests are not practical.
4. Public or user-visible contract changes MUST update the related documentation in the same change.
5. Configuration surface changes MUST update `README.md`, `README.ja.md`, and `nuget-resolution-guard.schema.json` together.
6. Dependency changes MUST update `THIRD-PARTY-NOTICES.md` in the same change.
7. Release-visible changes MUST update `CHANGELOG.md`.
8. Before modifying 4 or more files, the agent MUST explain the change plan and impacted areas.
9. The agent MUST NOT perform opportunistic refactoring outside the requested scope unless it is required for correctness, testability, or consistency.
10. If a change affects behavior, publishing flow, dependency policy, or other tracked repository policy, prefer opening or linking a GitHub Issue first.

If repository documents conflict, follow this order:

`README.md` / `README.ja.md` -> `CONTRIBUTING.md` / `CONTRIBUTING.ja.md` -> `docs/*` -> `AGENTS.md`

`AGENTS.md` defines workflow and quality rules for agents. It does not override the repository's documented behavior or maintainer decisions.

---

# Principles

## Quality First

Prefer a correct and maintainable solution over a quick local fix.

Avoid temporary workarounds when a proper design change is practical.

## Scope Discipline

Do one thing at a time.

Do not mix unrelated cleanup into feature, bug-fix, or packaging work.

## Documentation Discipline

When code, configuration, packaging, or release behavior changes, verify the related docs in the same change.

English docs are the source of truth. Japanese docs should be updated in the same change when practical, or tracked with a follow-up issue.

---

# Workflow

Use this default sequence:

1. Understand the requested change and the affected contract.
2. Identify the impacted code, docs, tests, and packaging behavior.
3. Open or reference a GitHub Issue when the change affects user-visible behavior, config/schema, publish flow, or dependency policy.
4. Explain the change plan before implementation when 4 or more files are expected to change.
5. Add or update tests.
6. Implement the change.
7. Run relevant local validation.
8. Update documentation and changelog files that match the change scope.

If a requested change is ambiguous, clarify the desired behavior before implementation instead of guessing.

---

# Testing And Validation

Tests are mandatory for normal code changes unless adding or updating a meaningful test is genuinely not practical.

Run the relevant local checks from `CONTRIBUTING.md` when your change affects build, behavior, packaging, or release flow:

- `dotnet restore ResolutionGuard.NuGet.slnx`
- `dotnet build ResolutionGuard.NuGet.slnx -c Release --no-restore`
- `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net8.0 --no-build`
- `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net9.0 --no-build`
- `dotnet run --project tests/ResolutionGuard.NuGet.Tests -c Release --framework net10.0 --no-build`
- `dotnet pack src/ResolutionGuard.NuGet.Package/ResolutionGuard.NuGet.Package.csproj -c Release --no-build -o artifacts`

When you do not run a listed check, state that clearly in the final report.

---

# Documentation Rules

Update the following when they are affected:

- `README.md` and `README.ja.md` for user-visible behavior or usage changes
- `nuget-resolution-guard.schema.json` for configuration surface changes
- `CHANGELOG.md` for release-visible changes
- `THIRD-PARTY-NOTICES.md` for dependency changes
- `docs/development.md` and `docs/trusted-publishing.md` when development or publishing guidance changes

Do not leave examples, defaults, or workflow descriptions inconsistent with the implementation.

---

# Forbidden

The following are prohibited:

- implementing behavior based on unstated assumptions
- merging code changes without considering test coverage
- changing user-visible configuration without updating schema and docs
- making broad refactors outside the requested scope without justification
- treating `AGENTS.md` as stronger than repository documentation or maintainer direction
