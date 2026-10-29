# Integration Tests - AzureFunctions Durable Functions host

## Status: Not Implemented

Durable Functions support of `Encina.AzureFunctions` (`DurableSagaBuilder`, `FanOutFanInExtensions`, `OrchestrationContextExtensions`, `DurableFunctionsHealthCheck`, `DurableServiceCollectionExtensions`, `ActivityResult`) is not exercised by an integration test on a real Azure Functions host. Running it on a real host is untested, and a user who chooses it does so under their own responsibility (maintainer decision D11 and D10 of issue #2699: cloud features are tested only locally, never against the real cloud).

## Justification

1. **Durable orchestrations run inside a Functions host.** `TaskOrchestrationContext` and the durable task client are created by the Durable Task extension of the host; the orchestrator replays, checkpoints and schedules activities through a storage provider. `Microsoft.Azure.Functions.Worker.Extensions.DurableTask` offers no way to run an orchestration in process. A preview in-process test host exists on NuGet (`Microsoft.DurableTask.InProcessTestHost`); it was not evaluated, and the follow-up issue should decide on it.
2. **A real host needs infrastructure this repository does not have.** It needs the Azure Functions Core Tools or the host image, a compiled function app that references `Encina.AzureFunctions`, and a storage account for `AzureWebJobsStorage` (Azurite locally). None of them is in `Directory.Packages.props`, the Docker compose profiles or the shared fixtures; `Testcontainers.Azurite` is the subject of the `Cloud-Azurite` fixture of PR #2971.
3. **Doubling the orchestration context would not be an integration test.** The unit flag already runs the builder, the fan-out helpers and the activity result with substituted contexts (`tests/Encina.UnitTests/AzureFunctions/Durable`); repeating that in the integration project would execute the same lines with the same doubles and prove nothing about a host.
4. **Four Durable files need no host but are left to the unit flag for now.** `ActivityResult`, `DurableFunctionsOptions`, `DurableServiceCollectionExtensions` and `DurableFunctionsHealthCheck` (which verifies option values only) could run in process; the scope of #2699 leaves all of `Durable/*` to the unit flag until the local-only follow-up (issue #523 tracks Durable test coverage), so their integration target is 0. This is a deferral, not an impossibility; the follow-up should give these four files an in-process integration test.

## Adequate Coverage from Other Test Types

- **Unit**: `DurableFunctionsOptionsTests`, `DurableFunctionsHealthCheckTests`, `DurableSagaBuilderTests`, `FanOutFanInExtensionsTests`, `OrchestrationContextExtensionsTests` and `ActivityResultTests` in `tests/Encina.UnitTests/AzureFunctions/Durable`.
- **Guard**: `DurableGuardTests` in `tests/Encina.GuardTests` for every public parameter of the Durable types.
- **Integration (in-process, this folder)**: the middleware, the context and response extensions, the health check and `AddEncinaAzureFunctions` run with concrete worker types and a real `IEncina` pipeline; they do not reach `Durable/*`.

## Recommended Alternative

A local-only follow-up `[TEST]` issue: a real Functions host (Core Tools or host image) plus the Azurite fixture running a small function app with one orchestration built by `DurableSagaBuilder` and one fan-out/fan-in. Until it lands, the package README states that Durable Functions on a real host is untested.

## Related Files

- `tests/Encina.IntegrationTests/Cloud/AzureFunctions/AzureFunctionsTestSupport.cs`
- `src/Encina.AzureFunctions/Durable/`
- `src/Encina.AzureFunctions/README.md`
- `.github/coverage-manifest/Encina.AzureFunctions.json`

## Date: 2026-10-28

## Issue: #2699
