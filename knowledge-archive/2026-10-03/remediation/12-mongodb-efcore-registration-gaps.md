<!-- issue
title: [BUG] MongoDB never registers InboxPipelineBehavior; EF Core and MongoDB never register ISagaRunner/ISagaNotFoundDispatcher
labels: bug, area-mongodb, area-entityframeworkcore
milestone: v0.14.0 — Hardening
-->

## Description

Found by the SPEC-003 audit of #12 (adversarial-reviewer pass).

Issue #12 centralized messaging-pattern registration into
`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` (`AddMessagingServices<...>`),
used by all 6 Dapper/ADO provider packages. EF Core and MongoDB do not call this shared helper —
they hand-roll their own `UseInbox`/`UseSagas` registration blocks — and that hand-rolled code
has drifted from what the shared helper provides:

1. **MongoDB never registers `InboxPipelineBehavior<,>`.** Both `AddEncinaMongoDB` overloads'
   `UseInbox` blocks (`src/Encina.MongoDB/ServiceCollectionExtensions.cs:100-106` and `:246-252`)
   register `IInboxStore`, `IInboxMessageFactory` and `InboxOrchestrator`, but never
   `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>))`. EF Core
   registers it at `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:176`; ADO and
   Dapper get it through the shared helper at
   `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:93`.
2. **EF Core and MongoDB never register `ISagaRunner`/`ISagaNotFoundDispatcher`.** The shared
   helper registers both at `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:102-105`
   for every ADO/Dapper provider. EF Core's `UseSagas` block
   (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:179-185`) and MongoDB's two
   `UseSagas` blocks (`src/Encina.MongoDB/ServiceCollectionExtensions.cs:108-114` and `:254-260`)
   register `ISagaStore`, `ISagaStateFactory` and `SagaOrchestrator`, but neither interface.
3. **No DI test catches either gap.**
   `tests/Encina.UnitTests/MongoDB/ServiceCollectionExtensionsExtendedTests.cs:236-370` builds a
   provider with `UseInbox`/`UseSagas` enabled but never resolves `IPipelineBehavior<,>` or
   `ISagaRunner` to confirm they are wired. No equivalent test exists for EF Core either. This is
   exactly the registration-completeness DI test `CLAUDE.md` requires (project history #1260,
   #1273, #1285, #1289): "a registration method that adds a service, orchestrator or hosted
   service also registers every option type and dependency it resolves, and that is proven by a
   DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes`."

## Steps to Reproduce

1. `services.AddEncinaMongoDB(connectionString, config => config.UseInbox = true);`
2. Build the `IServiceProvider` and send a request through the pipeline twice with the same
   idempotency key.
3. Observe that no `InboxPipelineBehavior<,>` runs — nothing deduplicates the second request —
   because it was never registered against `IPipelineBehavior<,>` (no exception is thrown; the
   pipeline behavior list is simply missing the entry, which is only visible by inspecting
   `IServiceCollection` or by the concrete idempotency failure).
4. Separately: `services.AddEncinaEntityFrameworkCore<TContext>(config => config.UseSagas = true);`
   then `serviceProvider.GetRequiredService<ISagaRunner>()` throws
   `InvalidOperationException: No service for type 'Encina.Messaging.Sagas.LowCeremony.ISagaRunner'
   has been registered.`

## Expected Behavior

- `AddEncinaMongoDB(...).UseInbox = true` registers `IPipelineBehavior<,> -> InboxPipelineBehavior<,>`
  exactly like EF Core, ADO and Dapper, so Inbox idempotency actually runs for MongoDB users.
- `AddEncinaEntityFrameworkCore(...)` and `AddEncinaMongoDB(...)` with `UseSagas = true` register
  `ISagaRunner` and `ISagaNotFoundDispatcher`, exactly like every ADO/Dapper provider.
- A DI test per provider (EF Core, MongoDB) builds the service provider with `ValidateOnBuild`
  and `ValidateScopes` and resolves `IPipelineBehavior<,>` (asserting an `InboxPipelineBehavior`
  is present when `UseInbox` is on) and `ISagaRunner`/`ISagaNotFoundDispatcher` (when `UseSagas`
  is on), so this class of drift cannot reappear silently.

## Actual Behavior

MongoDB silently skips Inbox idempotency deduplication when `UseInbox = true`. EF Core and
MongoDB throw at first resolution of `ISagaRunner`/`ISagaNotFoundDispatcher` when `UseSagas =
true` (or silently never offer the low-ceremony saga runner API if the application never
resolves it directly, since it's only injected into `SagaRunner`'s own registration — the failure
surfaces only when an application constructor requests `ISagaRunner`).

## Environment

- Encina main branch, as of the SPEC-003 audit of #12 (2026-09-25).
- Packages: `Encina.MongoDB`, `Encina.EntityFrameworkCore`.

## Code Sample

```csharp
services.AddEncinaMongoDB(connectionString, config =>
{
    config.UseInbox = true; // InboxPipelineBehavior<,> is NOT registered — no idempotency
});

services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseSagas = true; // ISagaRunner / ISagaNotFoundDispatcher are NOT registered
});
```

## Stack Trace

```
System.InvalidOperationException: No service for type
'Encina.Messaging.Sagas.LowCeremony.ISagaRunner' has been registered.
```
(Reconstructed from the registration gap; not captured from an actual run.)

## Additional Context

Root cause: EF Core and MongoDB register messaging services by hand instead of calling the
shared `AddMessagingServices<...>`/`AddMessagingServicesCore<...>` helpers in
`Encina.Messaging.MessagingServiceCollectionExtensions`, so a change to the shared helper (adding
`ISagaRunner`, for instance) does not propagate to them and nothing catches the drift. Preferred
fix: either (a) migrate EF Core's and MongoDB's `ServiceCollectionExtensions.cs` to call the
shared generic helper like every other provider, or (b) if a full migration is out of scope, add
the two missing registration lines to each and add a `ValidateOnBuild`/`ValidateScopes` DI test
per provider that resolves every interface the shared helper resolves, so this specific class of
drift is caught mechanically going forward.

## Related Issues

#12 (SPEC-003 audit source)
