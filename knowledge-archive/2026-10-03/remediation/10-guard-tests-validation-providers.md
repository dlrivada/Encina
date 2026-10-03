<!-- issue
title: [TEST] Guard tests missing for all three validation providers (DataAnnotations, FluentValidation, MiniValidator)
labels: area-testing, area-validation, ai:local-candidate
milestone: v0.19.0 — Providers & Testing
-->

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [x] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Found by the SPEC-003 audit of #10.

The coverage manifests `.github/coverage-manifest/Encina.DataAnnotations.json`, `Encina.FluentValidation.json` and `Encina.MiniValidator.json` each declare a `guard: 25` target, but **no guard-test source file exists for any of the three packages** under `tests/Encina.GuardTests/`: a glob for `*DataAnnotation*`, `*FluentValidation*`, `*MiniValidator*` under that folder matches only compiled `bin/` binaries, never a `.cs` file.

`.github/scripts/coverage-report.cs` attributes the "guard" flag to files physically located under a directory named `GuardTests`. With zero such files for these three packages, the manifest's 25% guard target has no coverable-lines source and can never be measured or satisfied by CI — it is a silent, permanently-unmet obligation (SPEC-003 AUD-03/AUD-05).

The null-argument checks for the three `AddXValidation()` extension methods (`AddDataAnnotationsValidation`, `AddEncinaFluentValidation`, `AddMiniValidation`) do exist, but live in `Encina.UnitTests`, so the coverage script attributes them to the `unit` flag, not `guard`. No `.md` justification file exists for the missing guard tests either, and CLAUDE.md's "Test Justification Documents" rule never accepts a justification for guard tests.

Verified independently by an `adversarial-reviewer` pass on 2026-09-25 (same worktree), which confirmed both the missing guard-test files and that the manifest's guard target is unmeasurable as declared.

## Packages / Providers Affected

- **Package(s)**: Encina.DataAnnotations, Encina.FluentValidation, Encina.MiniValidator
- **Provider(s)**: DataAnnotationsValidationProvider, FluentValidationProvider, MiniValidationProvider (all three `IValidationProvider` implementations)

## Current Coverage

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.DataAnnotations (guard flag) | unmeasurable (0 source files) | 25% | 25 pts |
| Encina.FluentValidation (guard flag) | unmeasurable (0 source files) | 25% | 25 pts |
| Encina.MiniValidator (guard flag) | unmeasurable (0 source files) | 25% | 25 pts |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Create `tests/Encina.GuardTests/Validation/DataAnnotations/DataAnnotationsGuardTests.cs` using GuardClauses.xUnit, covering `DataAnnotationsValidationProvider`'s public constructor and `ValidateAsync`, and `ServiceCollectionExtensions.AddDataAnnotationsValidation`'s null-argument check.
- [ ] Create the equivalent `tests/Encina.GuardTests/Validation/FluentValidation/FluentValidationGuardTests.cs` for `FluentValidationProvider` and `AddEncinaFluentValidation`.
- [ ] Create the equivalent `tests/Encina.GuardTests/Validation/MiniValidator/MiniValidatorGuardTests.cs` for `MiniValidationProvider` and `AddMiniValidation`.
- [ ] Keep the existing null-argument tests in `Encina.UnitTests` as-is (unit flag); the new guard tests are additive, not a move, so the unit flag does not regress.

### Success Criteria

- [x] All new tests pass
- [x] Coverage meets manifest target per flag
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

- **Collection**: N/A
- **Fixture**: N/A

## Related Issues

None found: searched `gh issue list --repo dlrivada/Encina --state open --search "DataAnnotations guard"`, `"validation guard coverage"` and `"FluentValidation MiniValidator guard"` on 2026-09-25; no open issue tracks this gap.
