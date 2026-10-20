## Coverage measured
Issue #23 was closed "created in error" with no diff of its own: its only timeline commit 2b50a1ec touches `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` (`git show --stat`), nothing under `src/` or `tests/`. The successor #44's commit 929046c5 is an ancestor of the audit branch HEAD (`2af75334`; `git merge-base --is-ancestor` exit 0). So this stage ran a narrow, filtered measurement of the three files the archivist listed, in the worktree `wia-23`, build configuration `-c Release` (as CI). Figures are informational for #44's audit, not a verdict on #23.

Manifest `.github/coverage-manifest/Encina.Testing.json`: package targets `unit: 60`, `guard: 15` (package-wide aggregates, not per-file thresholds); each of the three files has `defaultTests: ["unit","guard"]` (`defaultRule: *.cs`).

Commands (filtered, so the package aggregate is not validated by them):
- unit: `dotnet test tests\Encina.UnitTests\Encina.UnitTests.csproj -c Release --filter "FullyQualifiedName~EitherAssertionsTests|FullyQualifiedName~EncinaFixtureTests|FullyQualifiedName~AggregateTestBase" --collect "XPlat Code Coverage" --results-directory <wt>\artifacts\audit\coverage\unit` -> 244 passed, 0 failed.
- guard: `dotnet test tests\Encina.GuardTests\Encina.GuardTests.csproj -c Release --filter "FullyQualifiedName~Encina.GuardTests.Testing.Assertions|FullyQualifiedName~Encina.GuardTests.Testing.EventSourcing|FullyQualifiedName~EncinaFixture" --collect "XPlat Code Coverage" --results-directory <wt>\artifacts\audit\coverage\guard` -> 35 passed, 0 failed.

Measured line coverage (coverable lines per the cobertura report):

| File | unit | guard |
| --- | --- | --- |
| `src/Encina.Testing/EncinaFixture.cs` | 22/37 = 59.5% | 0/37 = 0.0% (no guard test class exists; the file has no argument guards) |
| `src/Encina.Testing/Assertions/EitherAssertions.cs` | 80/99 = 80.8% | 24/99 = 24.2% |
| `src/Encina.Testing/EventSourcing/AggregateTestBase.cs` | 118/121 = 97.5% | 34/121 = 28.1% |

Flags contract, property, integration: not measured. They are not in the manifest's `targets` for `Encina.Testing` (only `unit` and `guard`), and none of the three files lists them in `defaultTests`. (`AggregateTestBase` has contract and property test classes under `tests\Encina.UnitTests\Testing\EventSourcing\`, but they run in the unit project and are counted by the unit run above.)

Test types per file: unit and guard test files exist for `EitherAssertions` (`tests\Encina.UnitTests\Testing\EitherAssertionsTests.cs`, `tests\Encina.GuardTests\Testing\Assertions\AssertionsGuardTests.cs`) and `AggregateTestBase` (`...\Testing\EventSourcing\AggregateTestBaseTests.cs`, `tests\Encina.GuardTests\Testing\EventSourcing\AggregateTestBaseGuardTests.cs`). `EncinaFixture` has unit tests (`tests\Encina.UnitTests\Testing\EncinaFixtureTests.cs`) and no guard test file. Load, benchmark: not applicable to a test-support package.

## Findings
- none

## Informational (not findings)
These concern today's code delivered under #44 and later issues, not #23, which changed nothing. They are for #44's audit, not for remediation drafted from this issue.
- `EncinaFixture.cs` unit-uncovered lines: 109-112 (`CreateEncina(params Assembly[])`), 168-169 (`GetService<T>` before `CreateEncina` throws `InvalidOperationException`), 198-218 (`Dispose` and `Dispose(bool)`). `EncinaFixtureTests.cs` never disposes the fixture and never calls the `params Assembly[]` overload.
- `EitherAssertions.cs` unit-uncovered lines: 122, 284-285, 295-296, 307-308, 419-421, 441-443, 453-455, 465-467.
- `AggregateTestBase.cs` unit-uncovered lines: 424-426.
- Folder doubling: `tests\Encina.UnitTests\Testing\Base\` (27 files) next to `tests\Encina.UnitTests\Testing\` hold the same test classes. Of the 27, 18 are identical to their counterpart once the namespace `Encina.UnitTests.Testing.Base` is mapped to `Encina.UnitTests.Testing` (including `EitherAssertionsTests`, `EncinaFixtureTests` and the four `AggregateTestBase*Tests` files), 6 under `Base\Messaging\` have no counterpart, and 3 differ (`Handlers\HandlerSpecificationTests.cs`, `Handlers\ScenarioTests.cs`, `Modules\ModuleArchitectureAnalyzerTests.cs`). Both copies compile (different namespaces), so every scoped test runs twice; the unit run above passed 244 tests, which includes both copies. This belongs to the Test Consolidation plan, not to #23.
- `AggregateTestBaseTests.cs:372`, `:629`, `:639`, `:649` pass `DateTime.UtcNow` as event payload data (no assertion depends on it). Not a defect; noted because production code must not read it.
- Test quality scan of `EitherAssertionsTests.cs` and `EncinaFixtureTests.cs`: no `Thread.Sleep`, `Task.Delay`, `Random` or reflection-only tests.
- Regression tests: `stages/code.md` reports no bug, so none is required.
- Real infrastructure: not applicable (no database or Marten feature).

## CRAP
Pending #1346.

## Lessons for the pipeline
- For a closed-in-error issue the successor's files carry unit/guard gaps that look like findings; the "Findings" section stays at "- none" and they go to "Informational (not findings)" for the successor's audit.
- A filter `FullyQualifiedName~EncinaFixtureTests` does not match `EncinaTestFixtureTests`; a class-name filter must be checked against the class list, and the Base\ duplicate folder makes every such filter run both copies.
