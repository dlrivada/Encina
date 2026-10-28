## Scope

Closing commit f60f37786 (timeline `closed` event commit_id, 2025-12-27 00:56 +0100; no PR). Today:
- `src/Encina.AzureFunctions/`: EncinaAzureFunctionsOptions.cs, EncinaFunctionMiddleware.cs, FunctionContextExtensions.cs, FunctionsWorkerApplicationBuilderExtensions.cs, HttpResponseDataExtensions.cs, Log.cs, ServiceCollectionExtensions.cs, Health/AzureFunctionsHealthCheck.cs, README.md, PublicAPI files, plus Durable/* (added later by #61, out of #59's scope). HttpResponseDataExtensions.cs was last changed by #1919 (authorization codes -> 401/403).
- Tests: the commit's five per-type projects (`tests/Encina.AzureFunctions.{Tests,GuardTests,ContractTests,PropertyTests}`) were deleted by consolidation commit 658263027. Today: `tests/Encina.UnitTests/AzureFunctions/` (health check, options, middleware, FunctionContext, builder, HttpResponseData, ServiceCollection tests), `tests/Encina.GuardTests/Cloud/AzureFunctions/` (FunctionsGuardTests.cs, GuardClauseTests.cs, DurableGuardTests.cs). Contract and property test files for the package no longer exist under their old projects; I did not locate replacements.
- Benchmark: `tests/Encina.BenchmarkTests/Encina.AzureFunctions.Benchmarks/ErrorCodeMappingBenchmarks.cs`.
- Manifest: `.github/coverage-manifest/Encina.AzureFunctions.json` (unit 55, guard 15).
- For the code stage: no `ToActionResult` symbol, no Queue/Timer code, no integration tests, no cold-start code in src/ (searched `ToActionResult`, `QueueTrigger`, `TimerTrigger` in src/Encina.AzureFunctions/*.cs; only README hits).

## Destinations

- Package docs: `src/Encina.AzureFunctions/README.md` present (Queue/Timer samples at lines 87-136). The pre-draft's `docs/history/2025-12.md` does not exist in this tree.
- Decision "HttpResponseData instead of ActionResult": pinned by `tests/Encina.UnitTests/AzureFunctions/HttpResponseDataExtensionsTests.cs`, present.
- Benchmark: present (error-code mapping only).
- ROADMAP.md lists Azure Functions #59 as done; CHANGELOG.md mentions the package only in test-phase lines.
- Missing: Queue/Timer implementation, cold-start optimisation, host-level integration tests; no open issue was searched or found to track them (not verified beyond the closed #2784 guard-coverage issue).

## Successor and duplicate issues

No successor or duplicate. Related: #61 Durable Functions (CLOSED 2025-12-27, same package); #2784 [TEST] guard flag below target (CLOSED).

## Lessons for the pipeline

- The pre-draft listed `closed_at: 12/26/2025`, `linked_prs: []` and a "specific files unknown" scope, and copied "Rules and lessons" from the issue's acceptance criteria; resolved with `git show --stat` of the timeline close commit and dropped the invented rules.
- `gh issue view --comments` printed nothing in this session; `--json title,body,comments` worked.
- The commit message claimed "Support for HTTP, Queue, and Timer triggers" while src had only HTTP/middleware code; compare each trigger bullet with src before choosing `delivered`.
