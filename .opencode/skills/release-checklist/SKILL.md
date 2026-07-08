---
name: release-checklist
description: Validates that the codebase is ready for release — builds, passes all tests, coverage targets, PublicAPI tracking, ADRs, CHANGELOG, no warnings. Triggers when preparing a tagged release or version bump.
---

# Release Checklist Skill

Validates that Encina is ready for a tagged release. Covers build, tests, coverage, API, docs, and quality gates.

## Pre-Release Checklist

### Build

- [ ] Clean build: `dotnet build Encina.slnx --configuration Release`
- [ ] Zero MSBuild warnings
- [ ] .NET 10 compatible (no .NET 9 or older references)
- [ ] SLNX solution format (NOT SLC)
- [ ] Package versions consistent (Directory.Packages.props)

### Tests

- [ ] All tests pass: `dotnet test Encina.slnx --configuration Release`
- [ ] Integration tests pass (`dotnet run --file .github/scripts/run-integration-tests.cs`)
- [ ] No flaky tests
- [ ] All 10 providers covered (check manifest)
- [ ] Per-flag coverage targets met

### Coverage Targets

| Metric | Target | Where |
|--------|--------|-------|
| Line Coverage | ≥85% | Overall codebase |
| Branch Coverage | ≥80% | Overall codebase |
| Method Coverage | ≥90% | Overall codebase |
| Mutation Score | Per-file | [mutations dashboard](https://dlrivada.github.io/Encina/mutations/) |

### Public API

- [ ] `PublicAPI.Unshipped.txt` updated (RS0016/RS0017)
- [ ] Format: `Namespace.Type.Member(params) -> ReturnType`
- [ ] Nullable annotations correct (`string!` / `string?`)
- [ ] No symbols leaked without registration

### Documentation

- [ ] CHANGELOG.md updated (Unreleased section)
- [ ] ADRs created for architectural decisions
- [ ] XML comments complete (no DocFX metadata warnings)
- [ ] Version-specific docs in `docs/releases/vX.Y.Z/`

### Code Quality

- [ ] All CA warnings resolved (or suppressed with justification)
- [ ] No `[Obsolete]` for backward compatibility
- [ ] Nullable reference types enabled
- [ ] EventIds registered (EventIdRanges.cs)
- [ ] No sparses (EVIds packed sequentially)

### Migration

- [ ] No force push to main/master
- [ ] Commit messages follow conventional format
- [ ] No AI attribution in commits

## CI Gates

- [ ] All tests pass
- [ ] 0 build warnings
- [ ] Code formatting
- [ ] Public API compatibility

## Release Flow

1. Merge feature branches to main
2. Update CHANGELOG.md Unreleased section
3. Run mutation tests
4. Validate coverage dashboard (all green)
5. Create version tag
6. Update `docs/releases/vX.Y.Z/README.md`
7. Create PR referencing CI status
