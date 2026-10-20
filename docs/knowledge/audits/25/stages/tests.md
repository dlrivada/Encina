## Coverage measured

Issue #25 delivered no code (closed "created in error", no linked PR). Its only timeline commit `2b50a1ec` changed 3 files (`.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`; `git show --stat`), so there is nothing of #25 to measure. The code in the archivist's scope list (`src/Encina.Marten/Snapshots/`) was delivered by the duplicate #52 (commit `c1e4a50c`, verified an ancestor of `audit/25` with `git merge-base --is-ancestor`, HEAD `69a1d4a0`). The figures below are informational, from a narrow filtered run of the successor's tests; per-file gaps belong to the audit of #52.

Build configuration: Release (`-c Release`, as CI), `--collect "XPlat Code Coverage"`, results under `artifacts\audit\coverage\<flag>`. Line coverage = covered/coverable lines of the cobertura report (unique line numbers, max hits). All runs on the audit/25 worktree.

Test projects and filters (all passed):

| Flag | Filter | Result |
| --- | --- | --- |
| unit | `FullyQualifiedName~Encina.UnitTests.Marten.Snapshots` | 67 passed, 0 failed |
| guard | `FullyQualifiedName~Snapshot&FullyQualifiedName~Marten` (covers `Encina.GuardTests.Marten.Snapshots` and `Encina.GuardTests.Infrastructure.Marten`) | 33 passed, 0 failed |
| contract | `FullyQualifiedName~Encina.ContractTests.Marten` | 97 passed, 0 failed |
| integration | `FullyQualifiedName~MartenSnapshotStore` (Docker 29.8.0 available; `[Collection(MartenCollection.Name)]`, PostgreSQL) | 11 passed, 0 failed |
| property | not measured: the manifest lists no property flag for any `Snapshots/` file | n/a |

Per scoped file (manifest `defaultTests` quoted; the manifest `targets` block for `Encina.Marten` is a package-wide aggregate, unit 38 / guard 7 / contract 8, and a filtered run cannot be compared against it, so no pass/fail is stated):

| File (`src/Encina.Marten/Snapshots/`) | Manifest `defaultTests` | unit | guard | contract | integration |
| --- | --- | --- | --- | --- | --- |
| `ISnapshot.cs` | `[]` (interfaces; the file holds the `Snapshot<T>` record) | 11/11 | 2/11 | 0/11 | 11/11 |
| `ISnapshotable.cs` | unit, guard | no coverable lines | no coverable lines | no coverable lines | no coverable lines |
| `ISnapshotStore.cs` | `[]` | no coverable lines | no coverable lines | no coverable lines | no coverable lines |
| `MartenSnapshotStore.cs` | unit, guard | 75/133 | 10/133 | 0/133 | 72/133 |
| `SnapshotAwareAggregateRepository.cs` | unit, guard, contract | 228/259 | 42/259 | 42/259 | 0/259 |
| `SnapshotEnvelope.cs` | unit, guard | 21/21 | 1/21 | 0/21 | 19/21 |
| `SnapshotErrorCodes.cs` | unit | not in the cobertura report (constants only) | n/a | n/a | n/a |
| `SnapshotLog.cs` | unit, guard | not in the cobertura report (`[LoggerMessage]` generated code) | n/a | n/a | n/a |
| `SnapshotOptions.cs` | unit | 20/20 | 7/20 | 7/20 | 0/20 |

Union across the four flags, lines no flag executed (informational, for the audit of #52):

- `MartenSnapshotStore.cs`: 28 of 133 coverable lines: 67-75 (the `catch` of `GetLatestAsync`), 87-116 (the whole `GetAtVersionAsync` body, declared at `:80`, never executed by any flag) plus 175 and 218.
- `SnapshotAwareAggregateRepository.cs`: 31 of 259 coverable lines: 128-131, 340-345 (the "stream belongs to another aggregate type" branch) and 427-448 (the `AsyncSnapshotCreation` fire-and-forget branch).

Test types per `AGENTS.md` §9 for the scope: this is a Marten (event-sourcing) feature, not one of the 10 database providers. Unit, guard and contract exist as `.cs`; integration exists against Marten on PostgreSQL through Testcontainers (`MartenFixture`, `MartenCollection`), no in-memory substitute. Load and benchmark have no `.cs` or `.md` for snapshotting (benchmarks are tracked by the open #940); that is a question for the audit of #52, not #25.

Side observations, all within #52's scope: `GetAtVersionAsync` has no behavioural test anywhere (the only references are `tests\Encina.ContractTests\Marten\Core\SnapshotAwareAggregateRepositoryContractTests.cs:86` `ISnapshotStore_HasGetAtVersionAsync`, a reflection-only `GetMethods()` assertion that executes zero lines, and its sibling at `:93`); duplicate guard classes `tests\Encina.GuardTests\Infrastructure\Marten\SnapshotAwareAggregateRepositoryGuardTests.cs` and `tests\Encina.GuardTests\Marten\Snapshots\SnapshotAwareAggregateRepositoryGuardTests.cs` (the folder-doubling pattern); integration tests use `DateTime.UtcNow` only as test data, no `Thread.Sleep`.

## Findings
- none

Nothing survives for #25: the issue changed no code, so it has no regression test, missing test type or test-quality obligation of its own. The gaps above are recorded for the audit of #52 and are not drafted as remediation of #25.

## CRAP
pending #1346

## Lessons for the pipeline
- For a closed-in-error issue whose duplicate delivered the code, the one-call `Select-String -List` over `tests\` for the duplicate's class names (lesson of #24) located 13 test files at once; a narrow filtered run of all four flags under Release was quick enough (the integration run itself reported 916 ms with Docker available), so run integration whenever Docker is up.
- The filtered contract run needs the bare namespace (`Encina.ContractTests.Marten`) while the guard tests of the same feature live in two namespaces (`...Marten.Snapshots` and `...Infrastructure.Marten`); a name-based filter (`Snapshot&Marten`) caught both, a namespace-suffix filter would have dropped one.
