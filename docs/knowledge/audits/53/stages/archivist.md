## Scope

Delivering commit 84ba922b5 (message "Fixes #53"; no PR). It did not close the issue: the timeline `closed` event (2025-12-26T19:01:33Z) has no commit_id, and the commit (19:09:50Z) appears as a later `referenced` event. All paths below exist today in wia-53.

Source (Encina.Polly), each touched later only by ace6533f9 (EventId renumbering, #1126) and 7a87cabaf (CI/Sonar fixes):
- `src/Encina.Polly/Attributes/BulkheadAttribute.cs`
- `src/Encina.Polly/Bulkhead/IBulkheadManager.cs` (also BulkheadMetrics, BulkheadAcquireResult)
- `src/Encina.Polly/Bulkhead/BulkheadManager.cs` (`timeProvider reserved for future use` comment present)
- `src/Encina.Polly/Behaviors/BulkheadPipelineBehavior.cs` (key = `typeof(TRequest).Name`)
- `src/Encina.Polly/ServiceCollectionExtensions.cs` (registers manager singleton and behavior), `src/Encina.Polly/PublicAPI.Unshipped.txt`
- `src/Encina/Core/Encina.cs` (the #120 MatchUnsafe fix in the same commit)

Tests today (consolidated by 658263027): `tests/Encina.UnitTests/Polly/Bulkhead{Attribute,Manager,PipelineBehavior}Tests.cs`, `PolicyCombinationTests.cs`, `ServiceCollectionExtensionsTests.cs`; `tests/Encina.GuardTests/Infrastructure/Polly/Bulkhead{Manager,PipelineBehavior}GuardsTests.cs`; `tests/Encina.IntegrationTests/Observability/Polly/EndToEndIntegrationTests.cs`; `tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks/BulkheadBenchmarks.cs`. Not found by name: the commit's property tests, contract tests (`BehaviorContractTests`) and load tests (`tests/Encina.Polly.LoadTests/ResilienceLoadTests.cs` was deleted by 658263027); the test auditor must check whether the move kept them elsewhere. Coverage manifest: `.github/coverage-manifest/Encina.Polly.json` lists the four Bulkhead files.

Docs: `src/Encina.Polly/README.md` (one intro sentence only, no usage section), `docs/engineering/PROJECT-HISTORY.md`, `CHANGELOG.md` ("Bulkhead Isolation Pattern (Issue #53)"), `docs/plans/pipeline-execution-order-implementation-plan-2184.md` (Admission stage).

PR #2591 (#1223, state OPEN today, mergedAt null) changes `src/Encina.Polly/ServiceCollectionExtensions.cs`, `src/Encina.Polly/README.md`, `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Polly.json`, but not the Bulkhead files; if it merges during the audit, re-read those four shared files before judging registration or README.

## Destinations

- Decision "SemaphoreSlim manager + pipeline behavior + [Bulkhead] attribute": present in code and mentioned in `src/Encina.Polly/README.md` (intro line) and PROJECT-HISTORY.md. Docs destination is thin (no usage/property reference): present but weak.
- Decision "global + per-handler `AddEncinaBulkhead`/`ForHandler<T>`": missing. `Select-String` for `AddEncinaBulkhead|ForHandler` under `src/Encina.Polly` finds nothing. No open issue tracks it (open `bulkhead` search: #728 OpenTelemetry, #2278 rate limit/circuit breaker config API, #139 adaptive concurrency; none asks for bulkhead configuration). Recorded `current: unknown`, destination `none`; remediation should open one.
- "Metrics available for monitoring": only `GetMetrics()` snapshots and logging; no Meter/ActivitySource (no `Diagnostics` folder in Encina.Polly). #728 (OPEN) lists bulkhead rejections in its proposal; destination planned `#728`. Related #2277 (OPEN): `EnableTelemetry` option is never read.
- Gotcha simple-type-name key: open #2275 covers rate limiting only; the bulkhead has the same keying and is not named there. Recorded as planned `#2275`; remediation may need its own issue.
- Pre-draft destinations `regression-test` (load tests) and `spec-invariant` were not kept as decisions: no source states them as decisions (load test was a template row, not a statement).

## Successor and duplicate issues

None. Related, all verified OPEN today: #728, #2275, #2277, #2278, #139. #120 (fixed in the same commit) is CLOSED (2025-12-26T19:07:09Z).

## Lessons for the pipeline

- The pre-draft took "Rules and lessons" and `spec-invariant`/`regression-test` destinations from template rows and acceptance criteria and listed `closed_at: 12/26/2025`, `linked_prs: []`; normalised and dropped (repeat).
- I called 84ba922b5 the closing commit, but the timeline `closed` event has no commit_id and the commit is a later `referenced` event (verifier correction 7); check the `closed` event's commit_id before writing "closed by", and never derive the close from a commit that references the issue.
- A close comment listing "Implementation Summary" did not mention that the issue's headline API (`AddEncinaBulkhead`, `ForHandler<T>`) was never built; compare the body's code sample with `Select-String` over `src/` for each named method.
- The close commit named tests (property, contract, load) that are not found by name today because of a later consolidation; list them as "check in test stage" rather than assuming they were lost.
