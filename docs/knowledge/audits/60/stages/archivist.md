## Scope

Delivering commit: edec929e9 (2025-12-27 01:33 +01:00), the timeline `closed` event commit. No PR, no comments. Created 2025-12-24T13:31:17Z, closed 2025-12-27T00:34:56Z.

Source today (all exist, `git log` shows later fixes only: #1919 authorization codes, #1557/#1301 logging and health redaction, #2399 tenant claim, ADR-021 EventIds):
- `src/Encina.AwsLambda/ApiGatewayResponseExtensions.cs`, `SqsMessageHandler.cs`, `EventBridgeHandler.cs`, `LambdaContextExtensions.cs`, `EncinaAwsLambdaOptions.cs`, `ServiceCollectionExtensions.cs` (`AddEncinaAwsLambda`), `Health/AwsLambdaHealthCheck.cs`, `Log.cs`, `README.md`, `PublicAPI.*.txt`.
- Coverage manifest `.github/coverage-manifest/Encina.AwsLambda.json`.

Tests: the original projects `tests/Encina.AwsLambda.{Tests,GuardTests,ContractTests,PropertyTests}` were deleted by the consolidation commit 658263027. Today:
- Unit: `tests/Encina.UnitTests/AwsLambda/` (ApiGatewayResponseExtensionsTests, EventBridgeHandlerTests, LambdaContextExtensionsTests, LambdaResolveTenantIdTests, ServiceCollectionExtensionsTests, SqsMessageHandlerTests, AwsLambdaHealthCheckTests, EncinaAwsLambdaOptionsTests).
- Guard: `tests/Encina.GuardTests/Cloud/AwsLambda/GuardClauseTests.cs`.
- Benchmarks: `tests/Encina.BenchmarkTests/Encina.AwsLambda.Benchmarks/` (the commit put them under `benchmarks/`).
- Contract and property tests: no file referencing the package's types was found under `tests/Encina.ContractTests` / `PropertyTests` by name search of SqsMessageHandler/EventBridgeHandler/ApiGatewayResponseExtensions (only guard and unit hits), so the original 21 contract and 10 property tests appear lost in consolidation (not verified file by file; the auditor should confirm).
- Integration: no file under `tests/Encina.IntegrationTests` references the package.

Not removed on purpose; nothing deleted from src.

## Destinations

- API Gateway / SQS / EventBridge helpers: docs `src/Encina.AwsLambda/README.md` present; unit tests present.
- Cold start optimized: missing in code. Only README section "Cold Start Optimization" (static constructor, source generators, AOT suggestion). No benchmark of cold start.
- Integration tests with Lambda test utilities: missing; planned in open issue #2699 (record destination `backlog`, `planned`).
- 85%+ coverage: manifest exists; measurement is the test auditor's job.
- ROADMAP.md line 330 marks AWS Lambda as done (#60); this overstates cold start and integration tests.
- Pre-draft destinations "spec-invariant" and "Rules and lessons" (85% coverage, batch processing) were dropped: they restate acceptance criteria, not decisions with a source.

## Successor and duplicate issues

None for #60. #727 (OPEN, "[FEATURE] Add OpenTelemetry instrumentation to Azure Functions and AWS Lambda providers") is adjacent work that #60 never asked for; it is not a tracker of the missing cold-start or integration-test items. The integration-test gap is tracked by #2699 (OPEN, verified with `gh api repos/dlrivada/Encina/issues/2699`: state open, created 2026-10-23, "[TEST] Integration tests with local emulators for the cloud-tied 1.0 packages (AmazonSQS, AzureServiceBus, AwsLambda, AzureFunctions)"; its body lists Encina.AwsLambda and LocalStack Lambda invocation tests). The work is pending, not implemented. No issue was found tracking the cold-start gap (searched titles "Lambda"; bodies of open issues not exhaustively searched by "cold start").

## Lessons for the pipeline

- Pre-draft carried `closed_at: 12/27/2025 00:34:56`, `linked_prs: []`, "specific files unknown" and invented rules/destinations; re-derived from the timeline close commit and `git show --stat`.
- `gh issue view --comments` printed nothing again; `--json title,body,comments` worked.
- The commit-stat test projects (`tests/Encina.AwsLambda.*`) are all deleted by a later consolidation; the commit's "21 contract, 10 property" tests have no obvious successor, check with the test auditor.
