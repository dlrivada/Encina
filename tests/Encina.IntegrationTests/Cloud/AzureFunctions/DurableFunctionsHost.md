# Integration Tests - AzureFunctions Durable Functions host

## Status: Partially Implemented

`DurableSagaBuilder` and `OrchestrationContextExtensions` of `Encina.AzureFunctions` are tested locally on the Durable Task in-process test host (`Microsoft.DurableTask.InProcessTestHost`, preview): `DurableSagaOnTestHostIntegrationTests.cs` and `OrchestrationContextExtensionsOnTestHostIntegrationTests.cs` run real orchestrations and activities through the Durable engine (replay, retries, timers, external events, sub-orchestrations). `FanOutFanInExtensions` is deferred to issues #3214 and #3216. The Azure Functions host, its bindings and Azure Storage are not tested; the real local host smoke test is issue #3252. Nothing is tested against the real cloud (maintainer decisions D11 and D10 of issue #2699: cloud features are tested only locally).

## Justification

1. **The test host runs the Durable engine, not the Functions host.** It starts the Durable Task engine and an in-memory orchestration service in process, so `TaskOrchestrationContext` is real, but triggers, bindings, `AzureWebJobsStorage` and the Functions worker pipeline are not involved. Those need the Core Tools or host image, a compiled function app and Azurite; issue #3252 tracks a smoke test of that.
2. **`FanOutFanInExtensions` is deferred.** Its integration tests are part of the work of issues #3214 and #3216, so its integration target stays 0 until they land.
3. **The `durable.saga_exception` and `durable.compensation_exception` catch blocks are not exercised by any flag.** `CallEncinaActivityWithResultAsync` converts every `TaskFailedException` to a `Left`, so a failing activity never enters the `catch` blocks of `DurableSaga<TData>.ExecuteAsync` and its compensation step. Whether another exception type (for example a result that fails to deserialize) can reach them on the test host was not established. Likewise the `durable.event_failed` catch of `WaitForEncinaExternalEventAsync` is not exercised.
4. **The test host package is a preview.** `Microsoft.DurableTask.InProcessTestHost` is pinned to `0.2.3-preview.1` and moves to the first stable release (issue #3114).
5. **Behaviour under change is not asserted.** Tests assert error codes, never messages (#3205), and do not assert the step-throws-versus-Left choice (#3207), saga timeouts (#3212) or retry defaults (#3211). The built-in error codes (`durable.activity_failed`, `durable.suborchestrator_failed`, `durable.event_timeout`, `durable.event_cancelled`) are asserted as literals and move with #3217.

## Adequate Coverage from Other Test Types

- **Unit**: `DurableSagaBuilderTests`, `FanOutFanInExtensionsTests`, `OrchestrationContextExtensionsTests`, `ActivityResultTests` and the options, registration and health-check tests in `tests/Encina.UnitTests/AzureFunctions/Durable`.
- **Guard**: `DurableGuardTests` in `tests/Encina.GuardTests` for every public parameter of the Durable types.
- **Integration (this folder)**: the middleware, context and response extensions, the health check and `AddEncinaAzureFunctions` run in process with a real `IEncina` pipeline (`DurableFunctionsInProcessIntegrationTests.cs` also covers the host-free Durable files), and the two Durable files above run on the test host.

## Recommended Alternative

Issues #3214 and #3216 add the fan-out and fan-in tests on the same test host; issue #3252 adds a smoke test on a real local Functions host with Azurite.

## Related Files

- `tests/Encina.IntegrationTests/Cloud/AzureFunctions/DurableSagaOnTestHostIntegrationTests.cs`
- `tests/Encina.IntegrationTests/Cloud/AzureFunctions/OrchestrationContextExtensionsOnTestHostIntegrationTests.cs`
- `tests/Encina.IntegrationTests/Cloud/AzureFunctions/DurableTestHostSupport.cs`
- `tests/Encina.IntegrationTests/Cloud/AzureFunctions/DurableFunctionsInProcessIntegrationTests.cs`
- `src/Encina.AzureFunctions/Durable/`
- `src/Encina.AzureFunctions/README.md`
- `.github/coverage-manifest/Encina.AzureFunctions.json`

## Date: 2026-11-06

## Issue: #3114 (earlier: #2699; follow-ups #3214, #3216, #3252; related #523)
