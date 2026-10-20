## Coverage measured
- Unit: not measured: no src/ scope. The archivist's scope list is empty (issue #27 was closed in error 20 minutes after creation, duplicate of open #50; its only timeline commit `2b50a1ec` touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`, no `src/` or `tests/` file).
- Guard: not measured: no src/ scope.
- Contract: not measured: no src/ scope.
- Property: not measured: no src/ scope.
- Integration: not measured: no src/ scope.
- No `.github/coverage-manifest/*.json` file names or mentions `SourceGenerator` or `EncinaHandler` (checked by file name and by content), so there is no manifest entry or target to compare against.
- No build configuration applies: no test run was made, so there is no coverage figure to qualify with `-c Release`.

## Findings
- none

## Informational (not findings)
- Branch state: `audit/27` is based on `main` at `29126daa` (`git merge-base HEAD main`), so the searches below ran on the same commit as `main` today. `2b50a1ec` is an ancestor of HEAD (`git merge-base --is-ancestor`, exit 0).
- No test claims to cover a source generator. A recursive `Select-String` over every `.cs`, `.md` and `.csproj` under `tests\` for `Roslyn`, `Microsoft.CodeAnalysis`, `EncinaHandler`, `zero-reflection`, `SwitchDispatch`, `switch-based dispatch`, `NativeAOT`, `PublishAot`, `IIncrementalGenerator`, `ISourceGenerator`, `GeneratorDriver`, `CSharpGeneratorDriver` and `Encina.SourceGenerators` returned zero files.
- Test files and folders whose name contains "Generator" exist, all unrelated to Roslyn source generation, so none is a partial test of #27: ID generation (`tests\Encina.UnitTests\IdGeneration\Generators`, `Encina.ContractTests\IdGeneration\IdGeneratorContractTests.cs`, `Encina.GuardTests\IdGeneration\SnowflakeIdGeneratorGuardTests.cs`, `Encina.PropertyTests\IdGeneration\SnowflakeIdGeneratorPropertyTests.cs`), cache keys (`DefaultCacheKeyGeneratorTests.cs`, `ShardCacheKeyGeneratorTests.cs`, `CacheKeyGeneratorBenchmarks.cs`, `CacheKeyGeneratorPropertyTests.cs`), the CLI scaffolding `CodeGenerator` (`Encina.GuardTests\CLI\CodeGeneratorGuardTests.cs`, `Encina.UnitTests\Cli\Services\CodeGeneratorTests.cs`) and PostgreSQL permission scripts (`PostgreSqlPermissionScriptGeneratorTests.cs` with its verified snapshots). `tests\TestInfrastructure\PropertyTests\MessageDataGenerators.cs` holds FsCheck data generators.
- Test project folders present: Encina.BenchmarkTests, ContractTests, GuardTests, IntegrationTests, LoadTests, NBomber, PropertyTests, TestInfrastructure, Testing.Examples, UnitTests. None has a source-generator folder, justification `.md` or `.csproj` reference to Roslyn analyzers.
- Missing test types for the pending work (unit, guard, contract, property, benchmark for a generator, plus generator-driver tests) are the obligation of #50 and #51 when they are delivered. Nothing exists to test today, so the absence of tests is a consequence of the feature not existing, not negligence; any gap goes to the audits of #50 and #51, not to #27.
- Regression-test check: the code stage reported no bug, so no regression test is owed.
- Test-quality and real-infrastructure checks: not applicable, no test or database code is in scope.

## CRAP
- Pending #1346: not computed. No `src/` method is in scope.

## Lessons for the pipeline
- For a closed-in-error issue with an empty diff and an open successor (#27 to #50), the whole test stage is one `Select-String` over `tests\` for the proposed symbols plus a manifest search; no test run is needed because nothing in the scope can be executed. Run a filtered successor-test run only when the successor already delivered code (as in #24 and #25).
