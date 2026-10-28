# Integration Tests - AwsLambda LocalStack runtime

## Status: Not Implemented

The in-process integration tests of this folder drive `Encina.AwsLambda` with real Amazon.Lambda event objects and the real Encina pipeline. A test that deploys a function to the LocalStack Lambda service and invokes it through the emulator is not implemented.

## Justification

1. **The emulator can run Lambda; the shared fixture cannot, and the function cannot be built from this PR's scope.** Probe of 2026-10-28 on the maintainer's machine, with the pinned image `localstack/localstack:4.14.0` started without `LOCALSTACK_AUTH_TOKEN` and with `/var/run/docker.sock` mounted: `/_localstack/health` reported `lambda: available`; `CreateFunction` (runtime `provided.al2023`, a `bootstrap` shell script as the zip) returned the state `Pending`, then `Active` after about 2 s; `Invoke` returned the function's response in 6.6 s. So the free image runs a custom-runtime function.
2. **The shared `Cloud-LocalStack` fixture does not mount the Docker socket.** LocalStack starts the function in a sibling container through the socket. `LocalStackFixture` (PR #2802) builds on the `Testcontainers.LocalStack` module, and a search of the module assembly finds no `docker.sock` and no `LAMBDA_*` setting, so a function cannot run in that container. Adding the mount changes the fixture in `tests/Encina.TestInfrastructure`, which is shared by the SQS tests and outside the files this change may touch.
3. **Encina targets .NET 10 only, so the function needs a custom runtime that does not exist in the repository.** A test must publish a self-contained `linux-x64` (or AOT) function project that references `Encina.AwsLambda`, with a runtime bootstrap (`Amazon.Lambda.RuntimeSupport`, or a hand-written Runtime API loop), zip it and deploy it. `Amazon.Lambda.RuntimeSupport` and `AWSSDK.Lambda` are not in `Directory.Packages.props`; calling the Lambda REST API by hand avoids `AWSSDK.Lambda` but not the function project, the runtime pack restore (`Microsoft.NETCore.App.Runtime.linux-x64`) or the extra minutes of `dotnet publish` per run.
4. **The first invocation pulls the Lambda runtime image through the socket.** The probe machine already had Docker images cached, so the cold-start time on a clean CI runner was not measured; it adds a network dependency to a test that the local-only policy wants deterministic.
5. **What the emulator would add is small for this package.** Every public type of `Encina.AwsLambda` is a pure function over event objects (SQS, EventBridge, API Gateway) or reads environment variables; none calls the Lambda service. The cold start, the serializer wiring and an SQS event-source mapping belong to the AWS runtime, not to Encina code.

## Adequate Coverage from Other Test Types

- **Integration (in-process, this folder)**: `SqsMessageHandler`, `EventBridgeHandler`, `ApiGatewayResponseExtensions`, `LambdaContextExtensions`, the health check and `AddEncinaAwsLambda` run with events deserialized by `DefaultLambdaJsonSerializer` (the serializer the Lambda runtime uses for these types) and a real `IEncina` pipeline built from DI with `ValidateOnBuild` and `ValidateScopes`.
- **Unit and guard**: the mapping tables, tenant rules and null guards in `tests/Encina.UnitTests/AwsLambda` and `tests/Encina.GuardTests`.

## Recommended Alternative

Open a follow-up `[TEST]` issue for a LocalStack Lambda smoke test: a `Cloud-LocalStack` fixture variant (or an option of the current one) that mounts the Docker socket, a small function project published for `provided.al2023`, and a decision on whether `Amazon.Lambda.RuntimeSupport` and `AWSSDK.Lambda` enter `Directory.Packages.props`. Until then, the package README states that AWS Lambda support is tested only locally in-process and that running it in a real Lambda runtime is the user's responsibility.

## Related Files

- `tests/Encina.IntegrationTests/Cloud/AwsLambda/AwsLambdaTestSupport.cs`
- `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs`
- `src/Encina.AwsLambda/README.md`
- `.github/coverage-manifest/Encina.AwsLambda.json`

## Date: 2026-10-28

## Issue: #2699
