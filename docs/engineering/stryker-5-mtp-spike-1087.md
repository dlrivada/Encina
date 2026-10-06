---
nav_exclude: true
---

# Stryker.NET 5.0.0 MTP spike (#1087)

This page is for a contributor who needs to know what was measured before touching the mutation-testing workflow: it records the environment, the eight runs, the seven findings and the decision against #1087's five criteria, so the next change starts from evidence instead of re-running the experiment. It explains why the current configuration produces no mutation signal and what would have to change before it does; for the day-to-day rules of the mutation workflow (shards, filters, score formula), see [mutation-measurement-methodology.md](../testing/mutation-measurement-methodology.md).

**Status:** measured spike, closed 2026-09-27. **Issue:** #1087. **Knowledge record:** `docs/knowledge/issues/1087.md`.

## 1. Environment

- Machine: Windows 11 Pro 10.0.26200, 32 logical cores, .NET SDK 10.0.401.
- Worktree base: `origin/main` at `57cb73d0` (branch `spike/stryker5-mtp-1087`).
- Test stack: `xunit.v3` 3.2.2, `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 18.4.0 (`Directory.Packages.props`).
- Pilot shard: `**/Dispatchers/Strategies/*.cs`, filter `FullyQualifiedName~Dispatchers.Strategies`, concurrency 2, and the same six `--mutate:!` exclusions as `.github/workflows/mutation-tests.yml` (`Log.cs`, `LogMessages.cs`, `Diagnostics/*ActivitySource.cs`, `Diagnostics/*Metrics.cs`, `*Errors.cs`, `*Constants.cs`). In scope: 3 files (`ParallelDispatchStrategy.cs`, `ParallelWhenAllDispatchStrategy.cs`, `SequentialDispatchStrategy.cs`), 64 mutants.
- Each run used its own copy of `.github/stryker-config.json` under `artifacts/mutation/configs/`, differing only in `test-case-filter` (patched the same way CI patches it), `coverage-analysis`, and, for the project-mode runs, removing the `solution`/`test-projects` keys and setting `project: Encina.csproj`. The tracked `.github/stryker-config.json` was not changed.
- Solution pre-built once in Release (103 s) before run 1; Stryker itself builds Debug in every run.
- Stryker 5.0.0 was installed by bumping `.config/dotnet-tools.json` from 4.14.0 to 5.0.0 on this spike branch only, for runs 2-8; the bump was reverted to 4.14.0 before merging, because merging it alone would switch CI to Stryker 5.0.0 with the VsTest runner, which still kills no mutant, and the bump belongs with [the MTP runner migration follow-up](https://github.com/dlrivada/Encina/issues/1441).
- The second shard (`Pipeline/Behaviors`) was **not run**: the 3-hour time box was spent on the pilot shard's option A variants (runs 3-5, 8).

## 2. Runs

Common suffix of every command: `--verbosity info --log-to-file --mutate:<scope> --mutate:!**/Log.cs --mutate:!**/LogMessages.cs --mutate:!**/Diagnostics/*ActivitySource.cs --mutate:!**/Diagnostics/*Metrics.cs --mutate:!**/*Errors.cs --mutate:!**/*Constants.cs` (runs 6 and 8 mutated only `SequentialDispatchStrategy.cs`).

| Run | Stryker | Runner | coverage-analysis | Mode (cwd) | Scope | Tests found | Killed | Survived | Timeout | NoCoverage | Errors | Wall (Time Elapsed) | Mutant-testing phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 baseline | 4.14.0 | VsTest | off | solution (repo root) | shard | 34 | 0 | 64 | 0 | 0 | - | 12:20 (740.6 s) | 483 s (12:24:25-12:32:28), 7.5 s/mutant |
| 2 option B | 5.0.0 | VsTest | off | solution (repo root) | shard | 34 | 0 | 64 | 0 | 0 | 0 | 12:08 (729.1 s) | 478 s (12:41:44-12:49:42), 7.5 s/mutant |
| 3 option A | 5.0.0 | MTP | perTest | solution (repo root) | shard | 34978 | - | - | - | - | - | 7:25, aborted in initial test run | none |
| 4 option A | 5.0.0 | MTP | perTest | repo root, `test-projects` = UnitTests only, no `solution` key | shard | 34978 | - | - | - | - | - | 9:35, aborted in initial test run | none |
| 5 option A | 5.0.0 | MTP | perTest | project mode (cwd `tests/Encina.UnitTests`) | shard | 21091 | - | - | - | - | - | stopped after 25.5 min (time box) during per-test coverage capture | none |
| 6 probe | 5.0.0 | MTP | off | project mode | `SequentialDispatchStrategy.cs` | 21091 | 4 | 0 | 0 | 0 | 0 | 9:36 (577.2 s) | 160 s for 4 mutants |
| 7 option A-off | 5.0.0 | MTP | off | project mode | shard | 21091 | 63 | 1 | 0 | 0 | 0 | 46:18 (2778.3 s) | 2281 s (13:52:49-14:30:50), 35.6 s/mutant |
| 8 probe | 5.0.0 | MTP | off, concurrency 1 | project mode | `SequentialDispatchStrategy.cs` | 21091 | 4 | 0 | 0 | 0 | 0 | 11:16 (677.2 s) | - |

Total mutation-run wall time across runs 1-8: about 2 h 14 min, inside the 3-hour time box.

Commands (all paths relative to the worktree root `<wt>`, `<suffix>` is the common suffix above):

1. `dotnet tool run dotnet-stryker --config-file artifacts/mutation/configs/strategies-vstest-off.json --output <wt>/artifacts/mutation/run1-baseline-4.14-vstest-off <suffix>` (manifest at 4.14.0, cwd = worktree root)
2. same as 1 with the manifest at 5.0.0 and `--output <wt>/artifacts/mutation/run2-optB-5.0-vstest-off`
3. `dotnet tool run dotnet-stryker --config-file artifacts/mutation/configs/strategies-mtp-pertest.json --test-runner mtp --output <wt>/artifacts/mutation/run3-optA-5.0-mtp-pertest <suffix>` (cwd = worktree root)
4. `... --config-file artifacts/mutation/configs/strategies-mtp-pertest-unitonly.json --test-runner mtp --output <wt>/artifacts/mutation/run4-optA-5.0-mtp-pertest-unitonly <suffix>` (cwd = worktree root)
5. `... --config-file <wt>/artifacts/mutation/configs/strategies-mtp-pertest-projectmode.json --test-runner mtp --output <wt>/artifacts/mutation/run5-optA-5.0-mtp-pertest-projectmode <suffix>` (cwd = `<wt>/tests/Encina.UnitTests`)
6. `... --config-file <wt>/artifacts/mutation/configs/strategies-mtp-off-projectmode.json --test-runner mtp --output <wt>/artifacts/mutation/run6-5.0-mtp-off-sequential --verbosity info --log-to-file --mutate:**/Dispatchers/Strategies/SequentialDispatchStrategy.cs` (cwd = `<wt>/tests/Encina.UnitTests`)
7. `... --config-file <wt>/artifacts/mutation/configs/strategies-mtp-off-projectmode.json --test-runner mtp --output <wt>/artifacts/mutation/run7-5.0-mtp-off-shard <suffix>` (cwd = `<wt>/tests/Encina.UnitTests`)
8. run 6 plus `--concurrency 1`, output `run8-5.0-mtp-off-sequential-c1`.

## 3. Findings

### Finding 1 - the VsTest runner kills 0 mutants under xUnit v3 (4.14.0 and 5.0.0 alike)

- Runs 1 and 2: 0 killed of 64. The two JSON reports have the same 64 in-scope mutants, all `Survived`, `coveredBy` empty.
- CI run 36304862790 (2026-09-27, Stryker 4.14.0, ubuntu) - the first complete run of the 17-shard matrix - reports Killed 0 on all 17 shards (killed/survived): Core 0/226; Pipeline/Behaviors 0/144; Pipeline 0/140; Dispatchers/Strategies 0/64; Validation 0/29; Sharding/Migrations/Strategies 0/59; Sharding/ReplicaSelection 0/153; Sharding/Routing 0/247; Sharding/Execution 0/62; Sharding/Shadow 0/116; Sharding/Colocation 0/58; Sharding/TimeBased 0/287; Sharding/Resharding/Phases 0/209; Sharding/Diagnostics 0/66; Sharding/Health 0/53; Modules/Isolation 0/552; Results 0/16. Its Dispatchers/Strategies job: Time Elapsed 00:20:42, job 08:05:05-08:31:50. The weekly runs from 2026-07-10 to 2026-09-18 failed before producing reports. `publish-mutations.yml` runs of 2026-09-11/18/25 were skipped; the published dashboard `latest.json` still carries run 24584286635 (overall score 0.89).
- The same 4 `SequentialDispatchStrategy` mutants that survive under VsTest are all killed under MTP (runs 6 and 8), for example `ConfigureAwait(false)` → `ConfigureAwait(true)` killed by `EncinaTests.Publish_DoesNotCaptureSynchronizationContext`, and `h is not null` → `h is null` killed by `SequentialDispatchStrategyTests.DispatchAsync_WithNullHandler_SkipsNull` and 7 other strategy tests.
- Mechanism seen in the VsTest host log (run 2, `Runner 0-log.host.*`): Stryker passes the active mutant through an in-process data collector (`InProcDataCollector` `Stryker.DataCollector.CoverageCollector`, `<Mutant id="671" .../>`, `MutantControl`) inside `testhost.dll`. xUnit v3 test projects are executables that `xunit.runner.visualstudio` 3.x runs out of process, so the static set by the collector is not the one the mutated code reads. This is a hypothesis consistent with every observation, not proven by a debugger.
- 4.14.0 and 5.0.0 both log "is using Microsoft.Testing.Platform which is not yet supported by Stryker" ([stryker-net#3094](https://github.com/stryker-mutator/stryker-net/issues/3094)) for `ContractTests`, `PropertyTests`, `GuardTests`, `IntegrationTests`, `Testing.Examples` and `Encina.AspNetCore.Benchmarks`, so only `Encina.UnitTests` runs under VsTest (34 tests found for the pilot filter).

### Finding 2 - option A (MTP + perTest) cannot run a shard as configured today

- Solution mode (runs 3 and 4): the MTP runner starts a test server for every test project in the solution, ignoring `test-projects` (run 4 logged `TestProjects: [...Encina.UnitTests.csproj]` and still started servers for `ContractTests`, `GuardTests`, `IntegrationTests`, `PropertyTests`, `Testing.Examples` and `Encina.AspNetCore.Benchmarks`). It found 34978 tests. The initial test run failed ("Failed to start test server for ...Encina.AspNetCore.Benchmarks.dll", and Docker-backed `IntegrationTests` in `State: error`), and `break-on-initial-test-failure: true` stopped the run after 7:25 / 9:35.
- Project mode (cwd `tests/Encina.UnitTests`, run 5): 21091 tests found (the whole `UnitTests` project), initial test run 13:10:31-13:14:27 (3 min 56 s), completed. Per-test coverage capture started 13:14:28 for 21091 tests. Measured throughput: 1524 tests read by 13:23:03, 2993 by 13:33:04, 3120 by 13:34:01 (about 150 tests/min). Runner `MtpRunner-1` logged "No coverage file found" then repeated 10-second "Timed out waiting for coverage relay ack ... marking as Dubious" (97 Dubious by 13:34:01) and one "Test run timed out while capturing per-test coverage". Projected capture time for 21091 tests at 150 tests/min: about 2 h 20 min, before any mutant is tested. Stopped at 25.5 min (time box). No mutant result was produced with perTest.

### Finding 3 - test-case-filter is not honoured by the MTP runner ([stryker-net#3757](https://github.com/stryker-mutator/stryker-net/issues/3757))

- VsTest runs: 34 tests found with `FullyQualifiedName~Dispatchers.Strategies`.
- MTP runs with the same config value: 34978 tests (solution mode) and 21091 tests (project mode, the whole `UnitTests` project).
- `Stryker.TestRunner.MicrosoftTestPlatform.dll` 5.0.0 contains a test-UID filter (`BuildTestUidFilter`) used for per-test runs and no test-case-filter handling (confirmed by a string search of the binary).
- Consequence: under MTP every shard runs the full `UnitTests` project per mutant; the `FILTERS` array of `.github/workflows/mutation-tests.yml` loses its effect.

### Finding 4 - MTP discovery and the 3-minute timeout ([stryker-net#3692](https://github.com/stryker-mutator/stryker-net/issues/3692)) did not reproduce

- Discovery of 21091 tests took 3-5 s (13:10:28 → 13:10:31 in run 5; 13:37:19 → 13:37:24 in run 6).
- The initial test runs lasted 3 min 56 s (run 5), 3 min 56 s (run 6: 13:37:24 → 13:41:20) and 5 min 30 s (run 7: 13:47:19 → 13:52:49), all longer than 3 minutes, and none was killed by a timeout.
- So #3692 did not reproduce in project mode on `Encina.UnitTests`. The timeouts that did appear were in per-test coverage capture (Finding 2), which is a different mechanism.

### Finding 5 - MTP with coverage-analysis off kills mutants, but Verify snapshot tests produce kills unrelated to the mutant

- Run 7: 63 killed, 1 survived (`ParallelWhenAllDispatchStrategy.cs:135`, object initializer `new Dictionary<string, object?> {}`), reported score 98.44 %.
- 38 of the 63 killed mutants have at least one killer in `Dispatchers.Strategies`; 42 have at least one killer in strategy, `Publish` or notification tests.
- 21 of the 63 were killed only by the same 36 tests: `PostgreSqlPermissionScriptGeneratorTests` (13), `SqlServerPermissionScriptGeneratorTests` (13), `EncinaVerifyTests` (7), `AggregateVerifyTests` (2), `EncinaVerifySettingsTests` (1) - all Verify snapshot tests unrelated to the dispatch strategies (for example a `$""` string mutation in a log message of `ParallelWhenAllDispatchStrategy.cs:110`).
- These 36 tests pass in the initial test run and fail in many mutant runs. Run 8 (concurrency 1) shows the same 36 failing for 3 of 4 mutants, so the failure does not come from two test servers running at once. The most likely cause, not proven, is state that Verify keeps per process while the MTP test server is reused between runs.
- So the run-7 score is inflated: the 21 mutants killed only by Verify tests have no evidence that a related test kills them. The credible lower bound is 42/64 (65.6 %), the reported value 63/64 (98.44 %).
- Equality and boolean mutants ([stryker-net#3563](https://github.com/stryker-mutator/stryker-net/issues/3563)) did not reproduce either: 10 equality mutants were in scope, all 10 killed, 9 of them by at least one `Dispatchers.Strategies` test (the tenth, `ParallelDispatchStrategy.cs:78` `errorHolder.Error is null`, only by Verify tests). No immortal equality mutant was observed. Spot check 1: `SequentialDispatchStrategy.cs:30` `h is not null` → `h is null` killed by `DispatchAsync_WithNullHandler_SkipsNull`, which asserts that handlers `[1, null, 3]` invoke `[1, 3]` - a correct kill. Spot check 2: `SequentialDispatchStrategy.cs:32` `ConfigureAwait(false)` → `true` killed by `Publish_DoesNotCaptureSynchronizationContext`, which asserts `context.PostCallCount == 0` under a recording `SynchronizationContext` - a correct kill.

### Finding 6 - `mutation-history.cs --merge-from` accepts 5.0.0 reports unchanged

- Command per report (copies under `artifacts/spike-1087/history-check/<run>/`): `dotnet run --file .github/scripts/mutation-history.cs -- --report <copy>/mutation-report.json --latest <copy>/latest.json --history <copy>/history.json --docref-index <copy>/docref-index.json --run-id 1087 --scope **/Dispatchers/Strategies/*.cs --merge-from <copy>/merge-source.json` where `merge-source.json` and `history.json` are copies of `docs/mutations/data/latest.json` and `history.json`.
- Exit 0 for run 1 (4.14.0), run 2 (5.0.0 VsTest) and run 7 (5.0.0 MTP). Runs 1 and 2 produce byte-identical `latest.json` (0 differing lines). Run 7 produces per-file Killed/Survived for the three files (27/0, 32/1, 4/0).
- Report shape: `schemaVersion` 2; top-level keys `files`, `projectRoot`, `schemaVersion`, `testFiles`, `thresholds`; mutant keys `coveredBy`, `id`, `killedBy`, `location`, `mutatorName`, `replacement`, `static`, `status`, `statusReason` - identical in 4.14.0 and 5.0.0. 5.0.0 prints a new "Errors:" line in the console summary; no new status value appeared in these reports.

### Finding 7 - wall time

- Pilot shard, local: 4.14.0 VsTest 12:20; 5.0.0 VsTest 12:08; 5.0.0 MTP off 46:18 (3.8x); 5.0.0 MTP perTest not finished (capture alone projected about 2 h 20 min).
- Per mutant: VsTest 7.5 s (with 34 filtered tests, but mutants never active); MTP off 35.6 s (full 21091-test project per mutant, with `bail`).
- Other 5.0.0 differences seen: 6147 mutants created in `src/Encina` versus 6236 in 4.14.0 (652 versus 700 compile errors); the in-scope set is the same 64.

## 4. Decision criteria of #1087

| Criterion | Measured answer | Evidence |
| --- | --- | --- |
| Killed/survived counts plausible (no mass immortal mutants, no 0-killed runs) | A (perTest): **not measured** - no mutant was tested before the time box. B: **No** (0/64). Baseline 4.14.0: **No** (0/64 locally, 0 killed on all 17 CI shards). MTP off variant (not one of A/B/C): kills are real (42/64 with a related killer) but inflated by Verify snapshot tests (21 kills with only unrelated killers). | Runs 1, 2, 5, 7, 8; CI run 36304862790 |
| Discovery and execution complete within the MTP timeout for every shard, or a workaround exists | Discovery: **yes** (3-5 s for 21091 tests; initial runs of 3:56-5:30 not killed). perTest coverage capture: **no** (10-second relay-ack timeouts, 97 Dubious in 20 min). Solution mode: **no** (runs every test project, fails on `IntegrationTests` and the `AspNetCore.Benchmarks` exe); workaround: project mode from `tests/Encina.UnitTests`. | Runs 3, 4, 5, 6, 7 |
| Per-mutant wall time improves versus AllTests mode | **No.** MTP off 35.6 s/mutant versus 7.5 s/mutant (VsTest); MTP perTest capture alone projected about 2 h 20 min per shard. | Runs 1, 2, 5, 7 |
| `mutation-history.cs --merge-from` accepts the new report shape unchanged | **Yes**, for 5.0.0 VsTest and 5.0.0 MTP reports; same schema and keys. | Finding 6 |
| test-case-filter honoured by the MTP runner ([stryker-net#3757](https://github.com/stryker-mutator/stryker-net/issues/3757)) | **No.** 34 tests under VsTest versus 21091/34978 under MTP with the same filter. | Runs 1-7, Finding 3 |

## 5. Recommendation

- **Option A (5.0.0 + MTP + perTest) is rejected.** It fails "per-mutant wall time improves" and "test-case-filter honoured", and "plausible counts" could not be measured because no mutant was tested before the time box.
- **Options B and C.** 5.0.0 with VsTest regresses nothing against 4.14.0 (same 64 mutants, same statuses, identical `mutation-history` output, 12:08 versus 12:20), so by the issue's rule B would be chosen over C; but both kill 0 mutants under xUnit v3, so neither produces a valid mutation measurement and adopting B alone changes nothing that matters.
- **The only configuration that activated mutants** was 5.0.0 + MTP + `coverage-analysis: off` in project mode (run from `tests/Encina.UnitTests`). It is not ready to adopt: 3.8x slower on the pilot shard (46:18 versus 12:08), `test-case-filter` ignored so every shard runs all 21,091 unit tests per mutant, and 21 of 63 kills came only from Verify snapshot tests that fail in the reused test server.
- **Consequences:** #1026 stays open; every mutation score produced by the current VsTest workflow is 0 % by construction. Follow-up work: (a) switch the mutation workflow to the MTP runner with coverage off in project mode and re-design sharding ([#1441](https://github.com/dlrivada/Encina/issues/1441), workflow change, owned by the maintainer); (b) [make the Verify snapshot tests pass when the same test process runs them repeatedly](https://github.com/dlrivada/Encina/issues/1442); (c) the Pipeline/Behaviors second shard was not measured.
- The second shard (`Pipeline/Behaviors`) was not run for lack of time box.

## 6. Phase 2e: native memory

This section records what holds memory across mutant runs in the reused MTP test server, a question left open by the preceding phase 2d investigation of the reused server (phases 2a-2d are tracked in the [#1441 comment thread](https://github.com/dlrivada/Encina/issues/1441), not on this page), which saw about 5 GB retained per full-suite run and an out-of-memory kill at 12 GiB. All values were measured on 2026-10-05 on the Debug build of `Encina.UnitTests`, MTP server mode (`--server --client-port`), repeating the full suite in the same process through a JSON-RPC harness. Linux runs used the `mcr.microsoft.com/dotnet/sdk:10.0` container with `--memory 12g --cpus 4` and `DOTNET_GCHeapHardLimit=0x100000000`, `DOTNET_gcServer=0`, `DOTNET_gcConcurrent=0`, like the CI job. Verify snapshot tests error in the container because the binaries were built on Windows; this is unrelated to memory.

### 6.1 Bisection by namespace (Windows, one process per namespace)

Only two namespaces exceed 1 GB peak working set (WS) in a single run:

| Namespace | Peak WS | GC heap | Nature |
| --- | --- | --- | --- |
| `Testing` | 4.5 GB | `Testing.Architecture`: 2.8 GB WS with 2.4 GB heap; `Testing.Base` and `Testing.Modules`: about 1.1 GB, mostly heap | Managed, released after the run |
| `Security` | 3.3-3.8 GB | `Security.ABAC`: 2.35 GB WS with about 140 MB heap; `Security.ABAC.EEL`: 3.3 GB WS with about 180 MB heap | Native, outside the GC heap |

Inside `Security.ABAC.EEL`, `EELCompilerTests` peaks at 1.8 GB and `EELConformanceTests` at 2.2 GB working set, each with about 1.3 GB outside the GC heap.

### 6.2 Repeated runs in one server (Linux)

Idle private memory in MB after runs 1-4:

| Configuration | Run 1 peak WS (GB) | Idle private MB after run 1 / 2 / 3 / 4 | Outcome |
| --- | --- | --- | --- |
| Full suite | 7.6 GB | 7,286 / 10,087 / 11,454 / killed | Out of memory (12 GiB) in run 4 |
| Full suite, `MALLOC_ARENA_MAX=2` | 7.6 GB | 7,628 / 9,779 / 9,723 / 9,378 | Reaches the 12 GiB cgroup peak in run 2; no improvement |
| Full suite, `MALLOC_MMAP_THRESHOLD_=131072` and `MALLOC_TRIM_THRESHOLD_=131072` | 4.9 GB | 3,956 / 4,205 / 4,420 / 4,670 | About +230 MB per run; cgroup peak 8.9 GB after 4 runs |
| Without `Encina.UnitTests.Security.ABAC*` | 4.4 GB | 3,877 / 3,848 / 3,913 / 3,961 | Flat |
| Without ABAC and with the two malloc thresholds | 4.7 GB | 3,616 / 3,180 / 3,247 / 3,985 | Flat |

On Windows, the same harness over 6 runs shows idle private memory oscillating between 3.0 and 6.1 GB with no trend: the Windows heap returns freed native memory to the OS, glibc does not.

### 6.3 Parallelism (single Linux run, xUnit `--max-threads`)

| Threads | Peak WS | Duration |
| --- | --- | --- |
| 1 | 7.4 GB | 190 s |
| 2 | 6.7 GB | 102 s |
| 4 (equals the default on 4 CPUs) | 7.4 GB | 124 s |
| Default | 7.6 GB | 111-122 s |

Limiting threads does not lower the peak meaningfully and slows the run.

### 6.4 Root cause

- `src/Encina.Security.ABAC/EEL/EELCompiler.cs` compiles each expression with Roslyn scripting (`CSharpScript.Create` at line 105, `CreateDelegate` at line 127) and caches the result per compiler instance.
- `tests/Encina.UnitTests/Security/ABAC/EEL/EELCompilerTests.cs:15` and `EELConformanceTests.cs:16` hold the compiler in an instance field. xUnit creates one class instance per test, so every run recompiles the expressions of each test (the phase 2e harness counted about 76 compilations per run, including the cases of the parameterised conformance tests; the count was not re-derived from the source).
- Each compilation leaves two kinds of memory:
  - (a) Transient native memory that only finalizers release. glibc's dynamic mmap threshold keeps it in its arenas instead of returning it to the OS. This is the roughly 2.7 GB per-run amplification that the fixed thresholds remove.
  - (b) A script assembly that Roslyn scripting never unloads. This is the residual growth of about 230 MB per run.
- Classes that share a static compiler (`ABACPipelineBehaviorTests.cs:32`, `ABACRequirementEnforcementTests.cs:34`) compile once per process and do not regrow.
- Confidence: high for the location (excluding `Encina.UnitTests.Security.ABAC*`, which includes `Security.ABAC.EEL`, removes the growth), medium for the exact split between (a) and (b).

### 6.5 Stryker 5.0.0 cannot recycle the server

Stryker.NET 5.0.0 has no option to recycle the test server: the pool resets only after the initial test run and after coverage capture, and servers are replaced only on timeout, crash or exit. Research is in the [#1441 comment thread](https://github.com/dlrivada/Encina/issues/1441); the upstream issue is [stryker-net#3742](https://github.com/stryker-mutator/stryker-net/issues/3742).

### 6.6 Phase 2f plan

| Step | Action | Why | Status |
| --- | --- | --- | --- |
| 1 | Set `MALLOC_MMAP_THRESHOLD_=131072` and `MALLOC_TRIM_THRESHOLD_=131072` in the mutation job environment; they reach the test host | Removes cause (a) | Done (commit 38f741ff: the mutation workflow sets both to 131072) |
| 2 | Fix the EEL tests to share one compiler | Removes the cost at its source | In progress as [#1858](https://github.com/dlrivada/Encina/issues/1858) |
| 3 | Keep option A of the #1441 research as a safety net: an MTP `ITestSessionLifetimeHandler` in `Encina.UnitTests`, active only when `STRYKER_MUTANT_FILE` is set, that exits at session start above a private-memory threshold so Stryker reruns the mutant on a fresh server | Bounds cause (b) | Implemented in phase 2f (see 6.7) |
| 4 | Comment on stryker-net#3742 with these figures | Gives upstream the evidence | Open |

A fresh server's first run is 2-3x slower than a warm one (70-122 s against 31-44 s locally), so recycling should be rare, not per mutant. The threshold chosen after the CI verification (6.7, "First CI verification") trades against this: at 4096 MB recycles are expected to be frequent.

### 6.7 Phase 2f: test server recycler

The recycler is the safety net of step 3. It ends the test server when its private memory is too high, so that Stryker reruns the mutant on a fresh server.

#### Implementation

| Item | Detail |
| --- | --- |
| Files | `tests/Encina.UnitTests/TestHost/StrykerServerRecycleBuilderHook.cs` and `tests/Encina.UnitTests/TestHost/StrykerServerRecycler.cs` |
| Registration | A `TestingPlatformBuilderHook` item in `tests/Encina.UnitTests/Encina.UnitTests.csproj`, so the generated `SelfRegisteredExtensions` calls it. The xunit.v3 generated entry point uses Microsoft.Testing.Platform (1.9.1) only for `--server`, which is how Stryker starts the test server; other runs use xUnit's console runner and never reach the hook. |
| Activation | Registers an `ITestSessionLifetimeHandler` only when `STRYKER_MUTANT_FILE` is set (Stryker sets it on every test server it starts) |
| Trigger | At the start of each test session after the first one in the process, if `Process.PrivateMemorySize64` is above `ENCINA_MTP_RECYCLE_MB` (default 4096 MB, `StrykerServerRecycler.DefaultThresholdMb`; it was 6144 MB until the CI verification) |
| Action | Writes one line starting with `[encina-mtp-recycle]` and kills its own process with `Process.Kill`, not `Environment.Exit`, so no `ProcessExit` handler can delay the exit |
| First session | Never recycles, so the fresh server of the retry cannot be ended by the hook |
| Metric | `PrivateMemorySize64` is private bytes on Windows and `VmData` on Linux (dotnet/runtime `ProcessManager.Linux.cs`: `PrivateBytes = (long)procFsStatus.VmData`), the same metric as the 6.2 measurements |
| Log | Stryker 5.0.0 sends the test server's stdout and stderr to `Stream.Null` unless `--log-to-file` is set, so the line is also appended to the file named by `ENCINA_MTP_RECYCLE_LOG` when that variable is set. Stryker itself logs the recycle only at debug level, as "Test run for Encina.UnitTests.dll failed on attempt 1/2; discarding crashed server". |

#### Why the mutant's verdict is unaffected

All links pin Stryker.NET 5.0.0 at commit `6e77a3451bac4793e9c839c3ff4055c1b30ca3af`.

- [`RunAssemblyTestsInternalAsync`](https://github.com/stryker-mutator/stryker-net/blob/6e77a3451bac4793e9c839c3ff4055c1b30ca3af/src/Stryker.TestRunner.MicrosoftTestPlatform/MicrosoftTestingPlatformRunner.cs#L1184-L1244) runs up to two attempts. Any exception from the run discards the server and retries on a fresh one; only when both attempts fail does the mutant become RuntimeError.
- [A host that exits during the run](https://github.com/stryker-mutator/stryker-net/blob/6e77a3451bac4793e9c839c3ff4055c1b30ca3af/src/Stryker.TestRunner.MicrosoftTestPlatform/AssemblyTestServer.cs#L134-L197) surfaces as an exception (the JSON-RPC connection is lost, or `ThrowIfHostCrashed` throws), never as a timeout or a result.
- [The retry starts a new server without rediscovering tests](https://github.com/stryker-mutator/stryker-net/blob/6e77a3451bac4793e9c839c3ff4055c1b30ca3af/src/Stryker.TestRunner.MicrosoftTestPlatform/MicrosoftTestingPlatformRunner.cs#L773-L814), so its run is that process's first session.
- [The active mutant id stays in the memory-mapped file](https://github.com/stryker-mutator/stryker-net/blob/6e77a3451bac4793e9c839c3ff4055c1b30ca3af/src/Stryker.TestRunner.MicrosoftTestPlatform/MicrosoftTestingPlatformRunner.cs#L131-L157), so the fresh server activates the same mutant.
- The hook ends the process at session start, before any test runs, so the first attempt reports no test result.

#### Measured

Measured on 2026-10-05 on Windows, Debug build, Stryker 5.0.0 at concurrency 1 with coverage analysis off, scope `**/Sharding/ReplicaSelection/RoundRobinShardReplicaSelector.cs{874..1005}`. The scope holds two mutants: 4377 (statement removal) and 4378 (string mutation).

| Run | `ENCINA_MTP_RECYCLE_MB` | Recycles | 4377 | 4378 | Wall time |
| --- | --- | --- | --- | --- | --- |
| Hook never recycles | 1000000 | 0 | Killed | Survived | 676 s |
| Hook on | 3000 | 0 (private memory was below 3000 MB at session 2) | Killed | Survived | 568 s |
| Hook on | 1000 | 1 | Killed | Survived | 552 s |
| Hook on | 3000 | 1 | Killed | Killed by an unrelated test (see below) | 696 s |

- No mutant was RuntimeError or Timeout in any run; 4377 was killed by the same test in every run.
- The odd kill in the last row came from `EncinaFakerTests.RecentUtc_ShouldReturnUtcDate`, which fails at random outside UTC because `RecentUtc` labels a local time as UTC (`src/Encina.Testing.Bogus/EncinaFaker.cs` line 224, `DateTime.SpecifyKind(date.Recent(days), DateTimeKind.Utc)`). It is a test-helper bug unrelated to the hook (follow-up issue to be opened). CI runs in UTC, where it does not fail.
- Mutant 4378 took 43 s on the warm server and 62 s when retried on a fresh one; the test-run timeout was about 116-129 s.
- A server-mode check of the whole unit suite (JSON-RPC harness, `STRYKER_MUTANT_FILE` set, threshold 3000 MB): run 1 completed with 22,382 tests passed and 1 skipped at 5,016 MB private memory; run 2's request ended 0.8 s later with the server gone, zero test updates and the `[encina-mtp-recycle]` line. Without `STRYKER_MUTANT_FILE` (threshold 100 MB), three runs completed with no line.

#### First CI verification

Measured on 2026-10-06 (the figures below are read from the run's Stryker report and the local report, not typed from a dashboard). CI run 37371127806 (attempt 2) ran the custom scope `**/Dispatchers/Strategies/*.cs` with `ENCINA_MTP_RECYCLE_MB` 6144. Its report: Killed 40, Survived 22, Timeout 2, CompileError 16, Ignored 29 (109 mutants), score 65.62 %.

| Criterion | Result | Evidence |
| --- | --- | --- |
| No RuntimeError | Met | None in the report |
| Every "failed on attempt 1/2" is followed by a verdict for that mutant | Met | 7 crashes (mutants 712, 731, 739, 746, 773, 788, 816), each the recycle hook exiting 1-2 s after the mutant id was written; the retry on a fresh server logged the verdict about 65 s later. 712, 773, 788 and 816 Killed; 731, 739 and 746 Survived |
| cgroup peak below 12 GiB | Not met | `cg_peak` reached 12288 MB, equal to `memory.max`, right after the 773 recycle; `oom_kill` stayed 0 |
| Same kill set as a reference | Met | See the local reference below |

The hook recycled at 6.3-7.6 GB of private memory while the replacement server, Stryker and the page cache share the cgroup. That attribution is a hypothesis; the `cg_ev_max` counter added to the telemetry (see the [mutation methodology](../testing/mutation-measurement-methodology.md)) confirms or refutes it.

Local reference: Windows, concurrency 1, hook disabled with `ENCINA_MTP_RECYCLE_MB=999999999`, the same mutate globs and exclusions as CI, Stryker 5.0.0 MTP, 43 min, score 65.62 %. Compared by mutant id, with file, mutator, location and replacement, both sides have 109 mutants and 107 have the same status (Killed 39, Survived 21, Timeout 2, CompileError 16, Ignored 29 among those 107; the local totals, with the two differences below, are Killed 40 and Survived 22). The recycled mutants match: 712, 773, 788 and 816 Killed, 731, 739 and 746 Survived on both sides. The two differences are not recycled mutants:

| Mutant | Location | CI | Local | Killing test |
| --- | --- | --- | --- | --- |
| 722 | `ParallelDispatchStrategy.cs`, Conditional (false) mutation, 46:20-48:96 | Survived | Killed | `Marten.GDPR.CryptoShreddedPropertyCacheTests.ClearCache_RemovesAllEntries` |
| 802 | `ParallelWhenAllDispatchStrategy.cs`, Object initializer mutation, 135:47-140:10 | Killed | Survived | `Security.Secrets.SecretsMetricsTests.RecordGetSecret_RecordsDuration` |

Both are kills by one test unrelated to the mutated code (shared static state or timing under parallel dispatch), so they depend on the runner, not on the recycle. The recycle does not change the verdicts: criterion 4 is met.

New threshold: `ENCINA_MTP_RECYCLE_MB` and `StrykerServerRecycler.DefaultThresholdMb` go from 6144 to 4096 MB, so that the cgroup peak is expected to stay below `memory.max`; this has not run in CI yet. The cost is stated under Limits: the idle private memory after runs 1-4 on Linux with the `MALLOC_*` thresholds is 3,956 / 4,205 / 4,420 / 4,670 MB (6.2), so 4096 MB is expected to recycle roughly every other mutant. A fresh server's first session never recycles, so a lower threshold cannot cause RuntimeError.

#### Limits

- The Linux behaviour (SIGKILL on itself, the `VmData` reading) is confirmed by run 37371127806, but only for the scope above and at the 6144 MB threshold; the 4096 MB threshold has not run in CI yet.
- The retry is a cold run with the timeout computed from the initial run. The initial run is itself cold and mutant 1 after the pool reset always runs cold, so the margin is the same as for every first mutant, but a slower runner narrows it.
- Without the two `MALLOC_*` thresholds, private memory after one run is about 7.3 GB on Linux (6.2), above the 4,096 MB default, so every mutant after the first on a server would recycle: correct but slower.
- Even with the thresholds, the 4,096 MB default sits below the idle private memory after runs 2-4, so recycles are frequent, not rare, and cold runs are 2-3x slower than warm ones. The mutation workflow's timeouts are still provisional; the next CI run must report seconds per mutant before they are set.
- Remove the hook once Stryker can recycle the server ([stryker-net#3742](https://github.com/stryker-mutator/stryker-net/issues/3742)) or #1858 removes the growth and a custom-shard run confirms it.

## See also

- [Stryker.NET and xUnit v3: status for Encina 1.0](Stryker-xUnit-v3.md) - the status note this spike updates.
- [Mutation Measurement Methodology](../testing/mutation-measurement-methodology.md) - the day-to-day rules of the mutation workflow, with the runner caveat this spike produced.
- `docs/knowledge/issues/1087.md` - the knowledge record for issue #1087.
