## Coverage measured

Delta `rules-2026-10`, rule (b) only. Scope: the validation files named by the audit of #14 (`docs/knowledge/audits/issue-14.md`). Measured with `-c Release` and `--collect "XPlat Code Coverage"` on branch `audit/14` (results in `artifacts\audit\coverage\<flag>`). Coverable lines are the unique line numbers per file in the Cobertura report (classes of one file merged, highest hit count per line). Filters (all passed, 0 failed, 0 skipped):

| Flag | Filter | Tests |
|------|--------|:-----:|
| unit | `Encina.UnitTests.FluentValidation`, `.DataAnnotations`, `.MiniValidator`, `ValidationResultTests`, `ValidationErrorTests`, `Encina.UnitTests.Core.EncinaTests`, `PipelineBehaviorRegistrationTests` | 794 |
| guard | `Encina.GuardTests.Core.Validation` | 18 |
| contract | `Encina.ContractTests.Core.Validation` | 7 |
| property | `Encina.PropertyTests.Validation` and `ValidationProviderProperties` | 92 |

Measured lines executed of coverable lines, with the uncovered line numbers. "Target" is what the manifest demands of the file today (package `targets` unless the file has its own `targets`).

| File | Flag | Measured | Uncovered lines | Target today |
|------|------|:--------:|-----------------|:------------:|
| `src/Encina/Validation/ValidationOrchestrator.cs` | unit | 13/18 = 72.22% | 59, 60, 76, 78, 79 | 70 (package), pass |
| | guard | 15/18 = 83.33% | 76, 78, 79 | 20 (package), pass |
| | contract | 13/18 = 72.22% | 59, 60, 76, 78, 79 | none for the file (package 15, entry lists no contract) |
| | property | 3/18 = 16.67% | 54, 55, 57, 59, 60, 65, 66, 68, 70, 71, 74, 76, 78, 79, 81 | not listed |
| `src/Encina/Validation/ValidationPipelineBehavior.cs` | unit | 12/12 = 100% | none | 70 (package), pass |
| | guard | 12/12 = 100% | none | 20 (package), pass |
| | contract | 12/12 = 100% | none | 15 (package), pass |
| | property | 0/12 = 0% | all | not listed |
| `src/Encina/Validation/ValidationResult.cs` | unit | 18/18 = 100% | none | 70 (package), pass |
| | guard | 16/18 = 88.89% | 69, 81 | 20 (package), pass |
| | contract | 17/18 = 94.44% | 81 | not listed |
| | property | 16/18 = 88.89% | 69, 81 | not listed |
| `src/Encina/Validation/IValidationProvider.cs` | all | no coverable line (interface) | - | empty `defaultTests` with reason |
| `src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs` | unit | 20/22 = 90.91% | 36, 41 | 90 (file), pass |
| | guard | 0/22 = 0% | all 22 | 0 (file, provisional, #1825) |
| | property | 22/22 = 100% | none | 90 (file), pass |
| `src/Encina.DataAnnotations/ServiceCollectionExtensions.cs` | unit | 5/5 = 100% | none | 100 (file), pass |
| | guard | 0/5 = 0% | all 5 | 0 (file, provisional, #1825) |
| | property | 5/5 = 100% | none | 90 (file), pass |
| `src/Encina.MiniValidator/MiniValidationProvider.cs` | unit | 9/9 = 100% | none | 80 (package), pass |
| | guard | 0/9 = 0% | 27, 28, 31, 33, 35, 38, 39, 40, 42 | 25 (package), FAIL |
| | property | 9/9 = 100% | none | none |
| `src/Encina.MiniValidator/ServiceCollectionExtensions.cs` | unit | 5/5 = 100% | none | 80 (package), pass |
| | guard | 0/5 = 0% | 62, 65, 66, 67, 69 | 25 (package), FAIL |
| | property | 5/5 = 100% | none | none |
| `src/Encina.FluentValidation/FluentValidationProvider.cs` | unit, guard, property | not measured: `Encina.FluentValidation` is absent from every Cobertura report (coverlet does not instrument it, open #898) | - | 80 / 25 (package) |
| `src/Encina.FluentValidation/ServiceCollectionExtensions.cs` | unit, guard, property | not measured: same reason (#898) | - | 80 / 25 (package) |

Flags not run: integration, load and benchmark are "not measured: delta rule (b)"; none applies to this scope (no database or external system). The guard rows for the two MiniValidator and DataAnnotations files are 0% because no `.cs` file under `tests\Encina.GuardTests` references any of the three provider packages (Glob and `Select-String` over `tests\`; the guard project has only `Core\Validation\ValidationPipelineBehaviorGuardTests.cs` and `EndpointValidatorGuardTests.cs` for this feature).

## Findings

1. **Major** — `src/Encina.MiniValidator/MiniValidationProvider.cs` and `src/Encina.MiniValidator/ServiceCollectionExtensions.cs` have no per-file `targets` or `justifications` in `.github/coverage-manifest/Encina.MiniValidator.json`, and the only demand on them, the package aggregate `guard: 25`, is met by no test and cannot be met by null-check guard tests. Today the entries carry `defaultTests: ["unit","guard"]` and the package `targets` hold `unit: 80`, `guard: 25`, with no `property` key although `tests\Encina.PropertyTests\Validation\MiniValidator\ValidationInvariantProperties.cs` executes both files completely. Measured with `-c Release`: unit 9/9 and 5/5, property 9/9 and 5/5, guard 0/9 and 0/5 (no guard test exists; #1338 tracks creating them). The guard-reachable lines are the argument checks only: `MiniValidationProvider.cs:27-28` (`ArgumentNullException.ThrowIfNull(request)`, `ThrowIfNull(context)`) and `ServiceCollectionExtensions.cs:62`, 3 of the package's 14 coverable lines (21.43%), so the package aggregate 25 can never be reached by null-check guard tests. Proposed per-file targets for `Encina.MiniValidator.json`, each with its justification for the `justifications` field. `MiniValidationProvider.cs` (9 coverable lines: 27, 28, 31, 33, 35, 38, 39, 40, 42): unit 100 ("The provider is one pure method over `MiniValidation.MiniValidator.TryValidate` and its valid, invalid and null-argument paths are all driven in memory by `MiniValidationProviderTests`."), property 100 ("The generated (`[EncinaProperty]`) and fixed-input requests of `ValidationInvariantProperties.cs` already execute all 9 lines, and the two fixed-input `[Fact]` tests at `:181-206` assert that an invalid request yields an error for the `Name` property (`ValidateAsync_FieldLevelErrors_IncludePropertyName`) and that every error of another invalid request has a non-empty message (`ValidateAsync_AllErrorsHaveMessages`)."; at 9 lines property 90 and 100 are the same gate), guard 22 ("Only the two `ThrowIfNull` checks at lines 27-28 are guard-reachable, 2 of 9 coverable lines, so a higher guard target could not be met by guard tests; ceil(0.22 x 9) = 2."). `ServiceCollectionExtensions.cs` (5 coverable lines: 62, 65, 66, 67, 69): unit 100 ("`AddMiniValidation` has no branch other than the null check and the DI unit tests execute all five lines."), property 100 ("The property tests build the provider through this registration (`AddMiniValidation_RegistersValidationOrchestrator`, `:370`); at 5 lines property 90 and 100 are the same gate."), guard 20 ("The single guard-reachable line is the null check at line 62, 1 of 5 coverable lines; ceil(0.20 x 5) = 1."). Also lower the package `guard` target from 25 to 21 (3 of 14 lines, ceil(0.21 x 14) = 3) and add a `property` package key, or drop the package aggregate in favour of the per-file values. The guard values are the ceiling for null-check guard tests; a guard test that calls the method with valid arguments would reach more lines, and the guard project must not be filled with behaviour tests to reach 25. Severity is Major because the file currently fails its only effective guard obligation (0 of 25) with no justification and no test type can satisfy it as written; the guard tests themselves are tracked by #1338, so this finding is the manifest change (and its justification) that makes #1338 closable.

2. **Minor** — `src/Encina/Validation/ValidationPipelineBehavior.cs` and `src/Encina/Validation/ValidationResult.cs` have no per-file `targets` or `justifications` in `.github/coverage-manifest/Encina.json`; their only demand is the package aggregate (unit 70, guard 20, contract 15), far below what the tests already reach and, for guard, unrelated to the file's argument checks. Measured with `-c Release`: `ValidationPipelineBehavior.cs` unit 12/12, guard 12/12, contract 12/12; `ValidationResult.cs` unit 18/18, guard 16/18, contract 17/18, property 16/18. The 100% guard figure of the pipeline behavior comes from two behaviour tests that live in the guard project (`ValidationPipelineBehaviorGuardTests.cs:84` `Handle_ValidationPasses_CallsNextStep` and `:112` `Handle_ValidationFails_ReturnsLeft`); the null-check guard tests are `:20`, `:33`, `:50` and `:67`. Proposed per-file targets, each with its justification. `ValidationPipelineBehavior.cs` (12 coverable lines): unit 100 ("The behavior is a short ROP pass-through over a mocked orchestrator and all 12 lines run in memory (12/12 today)."), contract 100 ("The contract tests send a valid and an invalid request through a real orchestrator and assert `Right` and the short-circuit `Left` (12/12 today)."), guard 33 ("The guard-reachable lines are the constructor check at line 47 and the `ThrowIfNull` checks at lines 57, 58 and 59, 4 of 12 coverable lines; ceil(0.33 x 12) = 4."). `ValidationResult.cs` (18 coverable lines, includes the `ValidationError` record): unit 100 ("`Success`, both `Failure` overloads, `ToErrorMessage` and the record are pure in-memory logic and all 18 lines run today (`ValidationResultTests`)."), guard 5 ("The only argument check is `ArgumentNullException.ThrowIfNull(errors)` at line 56, 1 of 18 coverable lines; ceil(0.05 x 18) = 1."). The guard values keep the behaviour tests of the guard project from counting as the obligation (lines 69 and 81 of `ValidationResult.cs` are not guard-reachable). The same manifest should replace the copied `reason` strings ("Mediator pipeline with mockeable next delegate", "Result type") with these justifications. Tracker: the neighbouring `ValidationOrchestrator.cs` is already in #1850; these two files are in no open issue (searched the open issue bodies for the two file names and `ValidationPipelineBehavior`).

## Informational (not findings)

Re-verification of what the original audit of #14 and the open issues say, measured today.

| Item | State today |
|------|-------------|
| `ValidationOrchestrator.cs` per-file targets and direct unit tests | Tracked by open #1850 (item "Set per-file unit, guard and contract targets for ValidationOrchestrator and add direct unit tests"). This stage's figures match its numbers exactly (unit 13/18, guard 15/18, contract 13/18; uncovered 59, 60, 76, 78, 79). Lines 76, 78, 79 (the `OperationCanceledException` catch) are executed by no flag. #1850 proposes guard 80 and contract 70; for a null-check guard obligation the reachable lines are 35, 54 and 55 (3 of 18 = 16.67%), so guard 80 depends on the behaviour tests in the guard project (`ValidationPipelineBehaviorGuardTests.cs:183`); the maintainer can keep either, no new finding. |
| `DataAnnotationsValidationProvider.cs` and `ServiceCollectionExtensions.cs` targets | Manifest now holds unit 90 / property 90 / guard 0 and unit 100 / property 90 / guard 0 (from #1826). #1850 proposes unit 100 / property 100 / guard 9 and unit 100 / property 100 / guard 20; measured today 90.91 / 100 / 0 and 100 / 100 / 0, the same as #1850. Lines 36 and 41 (`UserId`, `TenantId` items) are executed by property tests only, which assert `IsValid` alone (#1337). Package guard 25 unreachable: #1877. Not repeated as findings. |
| FluentValidation provider and registration | Not measured (#898 open). Provisional per-file proposals are in #1850; null-check guard tests reach provider lines 31, 40, 41 and registration lines 54, 56-58, 96, 98-100. |
| `IValidationProvider.cs` | No coverable line; empty `defaultTests` with reason "Interfaces have no implementation to test" is correct. No contract test runs the three providers against the interface contract (only `ValidationProviderProperties`, property flag); #1877 raises the contract target for DataAnnotations. |
| `GlobalSuppressions.cs` (three packages) | Empty `defaultTests` with reason "Only [SuppressMessage] attributes": correct. |
| `Validation/Endpoint*.cs` in the same folder | Out of scope for #14 (added by #852, commit `580774b9`); not judged. |
| Guard tests for the three providers | None exist; tracked by #1338 (all three) and #1825 (DataAnnotations). |
| `ValidateOnBuild`/`ValidateScopes` DI proofs | Not rule (b); tracked by #1337 (DataAnnotations) and #1339 (FluentValidation, MiniValidator), still open. |
| Test quality scan of the scoped test folders | No `Thread.Sleep` or `Task.Delay`; no reflection-only tests found in the 794 + 18 + 7 + 92 tests run; the provider tests instantiate real providers. |
| Other open trackers cited by the original audit | #1330 (README `Exception` claim, docs) and #1338/#1339/#1337 open; #1319 closed (the core no longer logs `EncinaError.Message`). |

## CRAP

Pending #1346; not computed in this delta (rule (b) only).

## Lessons for the pipeline

- When an open consolidated delta issue (#1850) already holds the per-file proposals for four of the ten scoped files, searching its body for each scoped file name before deriving anything shows which files are new (here MiniValidator and two core files); the delta brief could name the umbrella issues that overlap the scope.
- A guard obligation for a class whose guard-project tests include behaviour tests (`ValidationPipelineBehaviorGuardTests.cs:84`, `:112`) shows 100% guard coverage while only 4 of 12 lines are argument checks; derive the proposal from the `ThrowIfNull` lines and say which measured guard lines are behaviour tests.
