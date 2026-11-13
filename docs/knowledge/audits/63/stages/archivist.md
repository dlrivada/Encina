## Scope

Closing work: commit bb01f3acb (2025-12-27 14:06 +0100, "Fixes #63"), no PR. The timeline `closed` event (13:01:39Z) has no commit_id; the commit is a later `referenced` event (13:06:28Z). Issue created 2025-12-24T13:32:21Z, closed 2025-12-27T13:01:38Z (2 d 23 h 29 min).

Code today (all still at the original paths, `git log --diff-filter=A` first-add is bb01f3acb):
- `src/Encina.Messaging/ScatterGather/`: BuiltScatterGatherDefinition, GatherStrategy, IScatterGatherRunner, ScatterDefinition, ScatterExecutionResult, ScatterGatherBuilder, ScatterGatherErrorCodes, ScatterGatherLog (EventIds 2915-2930), ScatterGatherOptions, ScatterGatherResult, ScatterGatherRunner.
- `src/Encina.Messaging/MessagingConfiguration.cs` (`UseScatterGather`, line 212) and `MessagingServiceCollectionExtensions.cs` (line 161-164, `AddScoped<IScatterGatherRunner, ScatterGatherRunner>`).
- `src/Encina/Diagnostics/EventIdRanges.cs`: `Messaging = (2800, 2999)`.
- Other implementers/registrars: `src/Encina.EntityFrameworkCore` (fixed by #2711; `tests/Encina.UnitTests/EntityFrameworkCore/ServiceCollectionExtensionsRecoverabilityRoutersTests.cs`); MongoDB does not register it (open #2826).
- Name collision to ignore: the sharding `ScatterGather*` files (`src/Encina/Sharding/Configuration/ScatterGatherOptions.cs`, `Encina.DomainModeling`, `Encina.Caching`) belong to the #289 sharding work, not to this issue.
- Tests today: `tests/Encina.UnitTests/Messaging/ScatterGather/` (Builder, Runner, Options, Result; 116 [Fact]/[Theory] attributes), `tests/Encina.GuardTests/Messaging/ScatterGather/` (45), `tests/Encina.UnitTests/Messaging/MessagingConfigurationPatternFlagsTests.cs` and `MessagingServiceCollectionExtensionsCoreEquivalenceTests.cs` (UseScatterGather). Benchmark: `tests/Encina.BenchmarkTests/Encina.Benchmarks/ScatterGatherBenchmarks.cs` (perf-manifest entry exists). Manifest: `.github/coverage-manifest/Encina.Messaging.json` lists all 11 source files.
- Tests deleted by consolidation 658263027: `tests/Encina.Tests/Contracts/ScatterGatherContractTests.cs`, `PropertyTests/ScatterGatherPropertyTests.cs`, `LoadTests/ScatterGatherLoadTests.cs`, `Guards/ScatterGatherGuardsTests.cs`. No contract, property, load or integration test of the Messaging pattern exists today (the `SpecificationScatterGather.md` load note and sharding tests are another feature).
- Docs touched by the commit: CHANGELOG.md, ROADMAP.md (lines 327 still lists it), docs/history/2025-12.md. Today it also appears in docs/examples.md, docs/INVENTORY.md, docs/releases/v0.11.0/CHANGELOG-DETAILS.md. No `docs/` feature page.

## Destinations

- Registration under opt-in flag: present (`MessagingServiceCollectionExtensions.cs`, `UseScatterGather`), pinned by the pattern-flags and core-equivalence unit tests.
- Four gather strategies, aggregation, timeout, max parallelism: present in code; unit tests in `ScatterGatherBuilderTests.cs` / `ScatterGatherRunnerTests.cs`.
- EventIds: the close comment's "600-615" is WRONG; actual ids are 2915-2930 inside the registered Messaging range 2800-2999. Present in `EventIdRanges.cs`.
- Contract/property/load tests promised ("131 tests"): missing today (deleted by 658263027, not replaced). Guard tests and benchmark present.
- Documentation (task "Add documentation"): missing as a feature page; only changelog, history, examples, inventory. Not tracked by a specific open issue (#3313 is about the routing slip tutorial; not this).
- MongoDB registration: missing, tracked by open #2826 (verified OPEN today).
- Pre-draft items dropped: "Rules and lessons" (source-generated logging, property tests, parallel+sequential) restate the delivered feature, not rules; `spec-invariant` and `regression-test` candidate destinations had no source; `linked_prs: [63]` is the issue number, not a PR.
- Delivery check: issue code sample used `ScatterGatherBuilder<PriceQuote>().ScatterTo<T>()` and `Execute(scatterGather, ...)`; shipped API is `ScatterGatherBuilder.Create<TRequest,TResponse>(name)` with lambda handlers and `IScatterGatherRunner.ExecuteAsync` (as the close comment says). WaitForFirst/Quorum/AllowPartial, MaxDegreeOfParallelism and timeouts all exist, so outcome `delivered`.

## Successor and duplicate issues

None. Related open issues checked: #2826 (OPEN, MongoDB registration omits scatter-gather), #2843 (OPEN, its sharding `SpecificationScatterGather.md` load note is another feature), #883 (OPEN EPIC, EIP part 2).

## Lessons for the pipeline

- (issue-archivist) The pre-draft had `closed_at: 12/27/2025 13:01:38`, `linked_prs: [63]` (the issue number itself), `packages` straight from the issue and invented "Rules and lessons" and destinations; all normalised or dropped. A close comment's EventId range (600-615) was again wrong (actual 2915-2930); read `EventId =` values in the code.
- (issue-archivist) Same-named files can belong to a different feature (sharding `ScatterGather*` vs Messaging); scope by namespace/folder, and name the collision so the code stage ignores it.
