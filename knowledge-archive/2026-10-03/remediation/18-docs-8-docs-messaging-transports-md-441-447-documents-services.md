<!--
title: [DEBT] docs/messaging/transports.md gRPC sample uses a ServerAddress option and a typed client that do not exist
labels: technical-debt
milestone:
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The gRPC sample in `docs/messaging/transports.md` configures a `ServerAddress` option that does not exist and sends a typed command through a client that `Encina.gRPC` does not provide. The sample describes a use of the package that it does not support.

## Location

- **File(s)**: `docs/messaging/transports.md:441-447`; `src/Encina.gRPC/EncinaGrpcOptions.cs`; `src/Encina.gRPC/IGrpcMediatorService.cs:17`
- **Package(s)**: Encina.gRPC

## Current Behavior

Lines 441-447 document `services.AddEncinaGrpc(options => { options.ServerAddress = "https://localhost:5001"; });` and `var result = await grpcClient.SendAsync(new CreateOrder(items));`.

`src/Encina.gRPC/EncinaGrpcOptions.cs` has no `ServerAddress` property; its members are `EnableReflection`, `EnableHealthChecks`, `MaxReceiveMessageSize`, `MaxSendMessageSize`, `EnableLoggingInterceptor`, `DefaultDeadline` and `EnableCompression`.

The only `SendAsync` in the package, `IGrpcEncinaService.SendAsync` in `src/Encina.gRPC/IGrpcMediatorService.cs:17`, is `ValueTask<Either<EncinaError, byte[]>> SendAsync(string requestType, byte[] requestData, CancellationToken cancellationToken = default)` on a server-side mediator bridge. It is not a typed client returning a typed result for a `CreateOrder` call: `Encina.gRPC` is a server-side bridge like `Encina.GraphQL`, not an outbound RPC client.

## Expected Behavior

The gRPC section shows how the package is actually used: registration with `AddEncinaGrpc` and options that exist, and the server-side bridge service, with no outbound typed client.

## Root Cause

The sample was written for an outbound client API that the package never had, and was not checked against `EncinaGrpcOptions.cs` or `IGrpcMediatorService.cs`.

## Proposed Fix

Rewrite lines 441-447: configure `AddEncinaGrpc` with members of `EncinaGrpcOptions` (for example `DefaultDeadline`), remove the `ServerAddress` option and the `grpcClient.SendAsync(new CreateOrder(items))` call, and describe `Encina.gRPC` as a server-side bridge that dispatches serialized requests through `IGrpcEncinaService`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
