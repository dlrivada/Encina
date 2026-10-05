# Request identity, Phase 2: the shape of `IRequestContextScopeFactory`

> **Issue**: [#1705](https://github.com/dlrivada/Encina/issues/1705) (comments of 2026-10-03 to 2026-10-05)
> **Plan**: [security-context-population-implementation-plan-1705.md](security-context-population-implementation-plan-1705.md), Design 2 ("Accessor lifetime"), Design 3 (scope API), Phase 2, and the "PR review amendments" table
> **Phase 1 code**: branch `feat/request-identity-1705-p1` (PR #1824): `src/Encina/Core/RequestContextAccessor.cs`, `src/Encina/Core/AmbientRequestContext.cs`, `src/Encina/Identity/*`
> **Status**: design analysis for a maintainer decision. It changes no code. Once it is accepted, the changes listed in [section 7](#7-concrete-changes-to-the-plan) go into the plan before the Phase 2 brief is written.

---

## 1. The decision and the short answer

Phase 2 adds `IRequestContextScopeFactory`, the only public way to bind an identity (service, principal, inbound request, restored actor) to a region of code. The plan gives it the classic `using` shape: `Begin*` returns `Either<EncinaError, RequestContextScope>`, and `RequestContextScope.Dispose()` ends the scope. Dispose "invalidates the scope's holder and restores the captured previous holder only if it is still valid" (plan, Design 3, line 261).

The #1824 review found a hole that this shape cannot close. A `Task.Run` child forked after the scope opened inherits the same `AsyncLocal` holder as the scope's owner. Nothing written at open time can tell the two flows apart. So if the child is the one that disposes the scope, the restore branch of `Pop` runs in the child and installs the **parent** identity there.

**Recommendation: the delegate shape, applied to the whole public API** (option C1 below):

- Every public scope is `Run*Async(…, work, …)`.
- The scope ends when `work` completes. Ending only invalidates the holder.
- The caller's identity comes back because the C# async machinery restores the caller's `ExecutionContext`, not because Encina writes it back.
- `RequestContextScope` (the `IDisposable`) is deleted from the public surface.
- The framework's own internal, frame-bound swaps stay as they are: the dispatcher's `AmbientRequestContext.Enter` and the stream `Flow`.

The rest of this document gives the evidence and works through each scenario and each point carried into Phase 2. It ends with the concrete edits to the plan.

## 2. Background: what Phase 1 built

The facts below are from branch `feat/request-identity-1705-p1`.

- `RequestContextAccessor` stores a `ContextHolder` in one static `AsyncLocal` (`RequestContextAccessor.cs`). A holder links to its `Parent`. A holder reads as no context (Anonymous) once it, or any holder it is bound to, has ended (`ContextHolder.IsValid`).
- `Push(context)` (`:114`) installs a new **scope** holder chained to the current live one.
- `Pop(holder)` (`:140`) invalidates the holder. When the holder is the current scope of the flow running `Pop` ("in order"), it also writes `holder.Parent` back into that flow (`:149`).
- The dispatcher swaps the ambient context for each dispatch:
  - `AmbientRequestContext.Enter` and `AmbientSwap.Apply` (`AmbientRequestContext.cs:316`) do the swap.
  - `Enter` is called with `using var ambient = …` inside the async `ExecuteAsync` of `Encina.RequestDispatcher.cs:46` and `Encina.NotificationDispatcher.cs:44`.
  - The restore reinstalls the captured holder as-is (`RequestContextAccessor.Install`, `:108`), so an ended scope is never revived.
  - With an accessor that is not the default type, the swap falls back to plain sets (`ApplyPlain`, `:354`).
- The public setter (`IRequestContextAccessor.RequestContext { get; set; }`, `IRequestContextAccessor.cs:60`) applies the explicit-context rule (`AmbientRequestContext.EnsureReplaceable`, `:121`). It **refuses** replacing an ambient User with a different authenticated identity. It **accepts and logs** (Warning 165) every other identity change, including the two-step "clear, then set another user".
- `RequestIdentity.IsSameAs` (`RequestIdentity.cs:187`) compares kind, user id, roles and permissions. It ignores other claims and the tenant.

### 2.1 The hazard, reproduced

A probe copies the holder semantics above (`Push`, `Pop` with the in-order restore, `IsValid`). Its source is in [Appendix A](#appendix-a-probe). It was run with `dotnet run` on .NET 10.0.112. The scenario:

- the outer scope is a service (`service:orchestrator`);
- the inner scope is a restored user (`user:alice(restored)`);
- a child forked inside the inner scope pops the inner scope.

```text
using/child after Pop    : service:orchestrator   <- the child now runs as the outer service
using/owner after child  : <anonymous>
using/owner after own Pop: <anonymous>            <- the owner lost even its outer identity
delegate/body            : user:bob(restored)
delegate/caller after    : service:orchestrator   <- restored by the async frame, not by Encina
delegate/late child      : <anonymous>            <- forked child that outlives the scope
delegate/caller after sync body: service:orchestrator
```

Two failures are visible in the `using` rows:

1. **Elevation in the child.** The child disposed a scope it inherited, so `Pop` judged it "in order" and wrote the parent holder into the child's flow. A role-less restored user is now running as a declared service.
2. **Collateral loss in the owner.** The owner's scope holder was invalidated by the child, so the owner's own `Dispose` is "out of order" and writes nothing. The owner stays Anonymous instead of returning to its outer identity. This fails closed, but it is wrong.

### 2.2 Why a `using` scope cannot tell the owner from the child

- `AsyncLocal<T>` values live in the `ExecutionContext`. `Task.Run` captures the current `ExecutionContext` by reference, and it is immutable copy-on-write. So the child starts with the *same* holder reference, the same `IsScope` flag and the same parent chain as the owner.
- Any token written at `Push` (an owner marker, a second `AsyncLocal`, a GUID) is captured too. It is identical in both flows.
- The runtime exposes no "logical flow identity". Thread ids change at every `await`, so they are no help either.

Only two outcomes are possible. Either `Pop` writes something back into the flow that calls it, and then a child can install the parent. Or `Pop` writes nothing, and then the owner of a `using` block nested inside the same method cannot get its outer identity back.

The one thing that tells the owner from the child is the **frame**. An `async` method restores, on return, the `ExecutionContext` that was current when it was called; this is `AsyncMethodBuilderCore.Start`. Only the frame that opened the scope gets its caller's context back. A child never returns into that frame.

## 3. The options

Every option keeps Phase 1's holder chain, the refusal rules of Design 3 (unknown service, over a User, over an inbound request, built-in names, `tenant_conflict`), logging and tenant binding. They differ only in who ends a scope and what ending it writes.

### A. `using` scopes (the plan as written)

```csharp
var opened = scopes.BeginServiceScope("billing-reconciliation", tenantId: tenant);
await opened.MatchAsync(
    async scope =>
    {
        using (scope)
        {
            return await encina.Send(new ReconcileInvoices(), ct);
        }
    },
    error => Task.FromResult(Left<EncinaError, Unit>(error)));
```

`Dispose` = `Pop`: invalidate, then restore the parent in the disposing flow if it is still valid.

### B. Delegate scopes

```csharp
var result = await scopes.RunAsServiceAsync(
    "billing-reconciliation",
    (context, ct) => encina.Send(new ReconcileInvoices(), ct),
    new IdentityScopeOptions(TenantId: tenant),
    ct);
```

```csharp
public interface IRequestContextScopeFactory
{
    Task<Either<EncinaError, T>> RunAsServiceAsync<T>(
        string serviceId,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, T>> RunAsPrincipalAsync<T>(
        ClaimsPrincipal principal,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, T>> RunInboundAsync<T>(           // HTTP middleware, Blazor activity, SignalR filter (F2)
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, T>> RunRestoredAsync<T>(          // deferred dispatch, SPEC-002 REQ-015
        PersistedRequestIdentity persisted,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);
}

public sealed record IdentityScopeOptions(string? TenantId = null, bool AllowOverInbound = false);

// Extension overloads for work that returns Task (no Either): result Either<EncinaError, Unit>.
```

The implementation is one private `async` method:

```csharp
private async Task<Either<EncinaError, T>> RunAsync<T>(IRequestContext context, Func<…> work, CancellationToken ct)
{
    var holder = RequestContextAccessor.Push(context);   // visible to work and to everything it forks
    try
    {
        return await work(context, ct).ConfigureAwait(false);
    }
    finally
    {
        RequestContextAccessor.End(holder);              // invalidate only; never writes the AsyncLocal
    }
}   // returning from this async frame restores the caller's ExecutionContext
```

A refusal returns `Left` without invoking `work`. A `Left` from `work` passes through unchanged. An exception from `work` propagates after the holder is invalidated. Exceptions are not business logic, and `IEncina.Send` already converts handler failures into `Left`.

### C. Hybrids

| # | Hybrid | Verdict |
|---|---|---|
| C1 | **Delegate-only public API; frame-bound internal swaps kept.** The dispatcher's `Enter`/`AmbientSwap` and the stream `Flow` stay `using`-shaped but internal. Their handle is a `readonly struct` that never leaves the dispatcher's own async frame, so no other flow can dispose it. | **Recommended.** This is B, plus a statement of why the existing internal swaps are already safe. |
| C2 | `using` scope plus an owner check at `Dispose` (a token, a second `AsyncLocal`, a thread id). | **Not possible.** Section 2.2: any token is inherited by the child, and thread ids change at every `await`. |
| C3 | `using` scope whose `Dispose` only invalidates and never restores. | **Rejected.** It closes the elevation but breaks plain nesting. After `using (inner) { … }` the owner reads Anonymous until its own async method returns. Every outer scope or dispatch that runs code after an inner scope loses its identity. It is correct only when the `using` is the last statement of its own async method, and nothing can enforce that. |
| C4 | `using` scope plus a Roslyn analyzer: the scope must be a `using` declaration in an async method, never stored, returned, captured by a lambda or passed as an argument. | **Rejected.** It needs a new analyzer package and its own tests, and it is still bypassable (an `IDisposable` parameter, a generic helper, `dynamic`, a test seam). It turns a structural guarantee into a lint rule. |
| C5 | Both shapes public: delegate first, `using` for "advanced" hosts. | **Rejected.** The guarantee is only as strong as the weakest public path. Every reviewer would have to tell the two apart, and ADR-035's trusted-path list would grow. |

## 4. Behaviour by scenario

The A and C1 columns describe the code as each option would implement it. The examples assume an outer identity P (for example a hosted service's declared service) and an inner scope S.

| Scenario | A: `using` | B / C1: delegate |
|---|---|---|
| `Task.Run` child forked inside S, run while S is alive | Sees S | Sees S |
| Same child, run after S ended | Anonymous (holder invalid) | Anonymous |
| Child disposes S (handle passed or captured) | **Child gets P installed** (elevation); the owner is left Anonymous | Impossible: there is no handle |
| Out-of-order disposal in one flow (outer ended first) | Outer end invalidates the inner chain; the flow is Anonymous; the inner `Dispose` returns `false` and logs 170 | Impossible: call-stack nesting is LIFO by construction |
| A scope in a forked flow outlives an enclosing scope | The forked scope's holder is chained to the ended outer one, so it reads Anonymous | Same; its `End` finds the holder already invalid and logs 170 (renamed, section 7) |
| Scope opened inside an awaited helper (`async Task<RequestContextScope> OpenAsync()`) | **Silently does nothing**: the `AsyncLocal` change does not flow back to the caller (the pitfall the plan already notes for `RunAsServiceAsync`) | Not expressible |
| Forgotten `using` | The identity leaks into the rest of the flow (see deferred dispatch below) | Not expressible |
| Body completes synchronously | Not applicable | Caller restored (probe, last line) |

### 4.1 `Task.Run` children

- **Delegate.** A child forked inside `work` inherits the scope holder. It reads S while `work` runs and Anonymous after. It cannot end the scope, so it can never install P. A child that opens its own `Run*Async` gets a nested holder chained to S's. That holder dies when S ends, which fails closed and matches Phase 1's rule.
- **`using`.** The same reads hold. The difference is that any code that can reach the scope object can end it from the wrong flow. The probe shows the result.

**Hardening, either shape.** A dead-flow child (S ended) reads Anonymous, so *no* ambient context exists. Design 3 allows opening a service scope there, because the refusal "over an inbound request" sees nothing to refuse. That would let a fire-and-forget task forked from an HTTP request run as a service after the request ended. See open question Q1.

### 4.2 Out-of-order disposal

With the delegate shape the question disappears inside a flow: scopes nest like the call stack. Across flows the Phase 1 rule still applies, and it fails closed: a holder chained to an ended scope reads Anonymous.

`IdentityScopeDisposedOutOfOrder` (EventId 170, not yet allocated in code) is renamed `IdentityScopeOutlivedParent`. It is logged when `End` finds the holder already invalidated by an enclosing scope.

### 4.3 Blazor Server: re-establishing the identity per activity

`CircuitHandler.CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)` is itself delegate-shaped, so the delegate API fits without adaptation:

```csharp
public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
    Func<CircuitInboundActivityContext, Task> next) =>
    async activity =>
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        await _scopes.RunInboundAsync(
            InboundRequestInfo.ForCircuit(state.User, _circuitCorrelationId),
            async (_, _) => { await next(activity); return Right<EncinaError, Unit>(unit); });
    };
```

- Each activity reads the current `AuthenticationState`. A role or claims refresh (a new `AuthenticationState` after re-authentication) therefore takes effect at the next activity, without the setter. This is exactly the path the #1705 comment of 2026-10-05 requires: "a Phase 2 claims-refresh or circuit handler must go through a scope, not the setter".
- Continuations that outlive the activity read Anonymous and fail closed. So does code that runs on the circuit's synchronization context outside an inbound activity, such as an `AuthenticationStateChanged` handler. Both shapes behave the same here; the how-to must say so.
- With `using`, the correct handler is equally short (`using` inside the lambda). The risk is the tempting variant: a circuit-lifetime scope opened in `OnCircuitOpenedAsync`, stored in a field and disposed in `OnCircuitClosedAsync`, which runs in another flow. The delegate API cannot express it.

### 4.4 Hosted services

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var result = await _scopes.RunAsServiceAsync(
            "billing-reconciliation",
            (_, ct) => _encina.Send(new ReconcileInvoices(), ct),
            cancellationToken: stoppingToken);
        result.IfLeft(error => Log.ReconciliationFailed(_logger, error.GetCode()));   // code only, never the message
        await Task.Delay(_interval, _timeProvider, stoppingToken);
    }
}
```

- **Delegate.** One scope per unit of work. Anything forked by one iteration dies with that iteration's scope.
- **`using`.** The same works with `using` inside the loop. What `using` also allows is a scope opened in `StartAsync` and disposed in `StopAsync`, which run in different flows: a no-op `Pop` and a service identity alive for the life of the host. Another allowed misuse is a `using var` at the top of `ExecuteAsync`, which keeps every fire-and-forget task authorised until shutdown. The delegate API does not forbid wrapping the whole loop, but the documented pattern and the samples make per-iteration the default.
- `ABACPolicySeedingHostedService` runs once under the internal built-in identity: `RunAsBuiltInAsync("encina.abac.policy-seeding", …)`.

### 4.5 SignalR (follow-up F2)

`IHubFilter.InvokeMethodAsync(invocationContext, next)`, `OnConnectedAsync` and `OnDisconnectedAsync` are delegate-shaped. A filter calls `RunInboundAsync` per hub invocation, from `invocationContext.Context.User`.

- A `ValueTask<object?>` next is awaited inside the `Task`-returning `work`, so no `ValueTask` overload is needed.
- With `using`, the realistic mistake is a connection-lifetime scope kept in `Context.Items` and disposed in `OnDisconnectedAsync`. It lives for hours and is disposed from a foreign flow. Per-invocation scopes are the only shape the delegate API offers.
- Unchanged in both shapes: SignalR does not refresh `Context.User` during a connection. F2 must state how long-lived connections pick up role changes.

Until F2, SignalR dispatches stay Anonymous, as the plan's entry-point table says.

### 4.6 Deferred dispatch: SPEC-002 REQ-015, `BeginRestored` → `RunRestoredAsync`

Outbox, Scheduling, Inbox, dead-letter replay and the Saga/RoutingSlip runners process messages in a loop, one restored actor per message:

```csharp
foreach (var message in batch)
{
    var outcome = await _scopes.RunRestoredAsync(
        message.Identity,                                   // PersistedRequestIdentity: Kind, ActorId, TenantId, CorrelationId, CausationId
        (_, ct) => _dispatcher.DispatchAsync(message, ct),
        cancellationToken);
    await outcome.MatchAsync(_ => MarkProcessedAsync(message), error => MarkFailedAsync(message, error));
}
```

This is the scenario where the shape matters most:

- **Child-pop elevation.** A processor that itself runs under a declared service (P), restoring a role-less user (S), is exactly the probe. With `using`, a child forked by a handler that ends the scope runs as the processor's service, with its roles and permissions. With the delegate shape it cannot happen.
- **Forgotten `using`.** The restored actor of message *n* stays ambient for message *n+1*. If it was a User, every later `BeginRestored` is refused (`scope_conflict`, ambient User): a processing outage. If it was a Service, the next one is accepted with a Warning and the chain grows for the whole batch. The delegate API cannot leak between iterations.
- **Errors are never swallowed** (AGENTS.md §3). A refusal (tampered `PersistedRequestIdentity`, unknown declared service) is a `Left` that flows into `MarkFailedAsync`. With `Either<EncinaError, RequestContextScope>` the processor must remember to handle the `Left` *and* to dispose the `Right`.
- **No change to M4.** A restored User carries no roles and no permissions, and an external inbox never restores a User.

### 4.7 `RunAsServiceAsync`

In the plan, `RunAsServiceAsync` is already an extension built on `BeginServiceScope`; it is the delegate shape for one case. Under C1 it, and its siblings, become the interface members, and `BeginServiceScope` no longer exists. The line "opens and awaits inside one method frame, which removes the AsyncLocal pitfall" (plan line 262) becomes the rule for the whole API rather than an exception.

### 4.8 Two cases the plan does not name

- **Streams.** `IEncina.Stream` returns an `IAsyncEnumerable<T>`, and `AmbientRequestContext.Flow` reinstalls the dispatch holder at every step. A stream enumerated *after* `work` returned reads Anonymous from then on. This fails closed in both shapes, because the `using` equivalent is a stream returned out of the `using` block. The how-to says "consume a stream inside `work`", and one test pins it.
- **Synchronous callers.** No overload is offered. Every entry point is asynchronous: the middleware, the circuit handler, hub filters, hosted services, Hangfire and Quartz jobs, and the outbox processors. A sync overload would need `ExecutionContext.Run` and adds nothing.

## 5. Impact on the points carried into Phase 2

### 5.1 `IsSameAs`: claims and tenant

- Under C1 `IsSameAs` keeps two callers: the explicit-context check in `Resolve`, and the setter rule (narrowed in 5.4).
- Neither caller binds identities to code regions any more; the factory does that. So `IsSameAs` only decides whether "this context is the caller I already have".
- **Claims count.** `[RequireClaim]` evaluates `RequestIdentity.HasClaim` over the authenticated identities (Design 4), so any claim can drive authorization. An explicit context that differs only in `amr` or `acr` (step-up) is a different authorization subject today, and it is accepted silently.

**Recommendation:**

- `IsSameAs` compares kind, user id, roles, permissions **and the set of authenticated claims** as (type, value) pairs, ordinal.
- To keep it cheap, `RequestIdentity` computes an immutable claims fingerprint at construction: a frozen set, or a hash plus a set compared on a hash match.
- **Tenant does not count.** Tenant is not identity (Design 1). Tenant changes are governed by the tenant rules (`tenant_conflict` at `RunAsPrincipalAsync`, follow-up F5 for `TenantResolutionMiddleware`).
- Nothing in this decision depends on the scope shape; the shape only removes the pressure to widen `IsSameAs` for refresh scenarios (5.2).

### 5.2 Role and claims refresh

- In Phase 1 the setter throws when the same user arrives with different roles: `IsSameAs` counts roles, and the ambient is a User.
- Under C1 a refresh is a new scope. The next HTTP request, the next circuit activity (4.3) or the next hub invocation (4.5) runs `RunInboundAsync` with the current principal.
- Inside one region the identity is immutable. A handler that "refreshes" mid-region is not supported, and it should not be: an authorization decision taken earlier in the same region would no longer match the identity that region later runs under.
- Nothing calls the setter for a refresh, so the throw stays as a guard.

### 5.3 Custom accessors

- `Push` and `End` are static members of `RequestContextAccessor`. `AddEncinaRequestIdentity` registers the accessor with `TryAdd`, so an application can register its own `IRequestContextAccessor`.
- In that case a scope would write to a store the dispatcher never reads (`Resolve` reads the *registered* accessor), and dispatches would run silently Anonymous. The same applies under `using`.

**Recommendation, independent of the shape, and the shape makes it simpler:**

- The scope factory takes `IRequestContextAccessor` in its constructor. When the accessor is not the default `RequestContextAccessor`, every `Run*Async` returns `Left(encina.identity.unsupported_accessor)` without running `work`.
- A startup validator (`ValidateOnStart`, registered by `AddEncinaRequestIdentity`) fails the host with the same message, so the misconfiguration surfaces at boot, not on the first job.
- `IRequestContextAccessor` stops being a replacement extension point (pre-1.0, no shim).
- The dispatcher's `ApplyPlain` path can stay for unit tests that construct `Encina` with a substituted accessor. Production hosts cannot reach it once the validator exists.

### 5.4 The two-step setter bypass

- Under C1 no production code needs the public setter to change identity:
  - the middleware and the circuit handler use `RunInboundAsync`;
  - jobs use `RunAsServiceAsync`;
  - deferred dispatch uses `RunRestoredAsync`.
- The remaining production setter call is `TenantResolutionMiddleware.cs:120`, which sets `WithTenantId(...)`: same identity, new tenant. (`EncinaContextMiddleware.cs:100` moves to `RunInboundAsync` in Phase 3.)

**Recommendation: the setter becomes identity-preserving only.** A set is accepted when the new context is unauthenticated (a downgrade, harmless because it fails closed) or `IsSameAs` the current identity. Any change *to* a different authenticated identity is refused (Warning 165, `InvalidOperationException`), whatever the ambient kind.

- The two-step bypass closes. Clearing is allowed; setting a user afterwards is a change from Anonymous to an authenticated identity, so it is refused.
- Exactly one way remains to bind an identity: the factory, which validates and logs (M3).

### 5.5 `TestIdentity.Service`

- `TestIdentity.Service(string name, IEnumerable<string>? roles = null, IEnumerable<string>? permissions = null)` builds a `ServiceIdentityDefinition` and calls the internal `RequestIdentity.ForService` (`InternalsVisibleTo` already covers `Encina.Testing`).
- Tests that need an ambient identity use a test seam with the same shape as production, so test code cannot rely on behaviour production does not have. `RunAsAsync` calls an internal `RequestContextScopeFactory.RunWithIdentityAsync(RequestIdentity, …)`, part of the test-only seam M3 lists for ADR-035.

```csharp
await TestIdentity.RunAsAsync(TestIdentity.Service("billing-reconciliation"), async context =>
{
    var result = await encina.Send(new ReconcileInvoices(), CancellationToken.None);
    result.ShouldBeSuccess();
});
```

`TestIdentity.Service` also:

- Unblocks the missing tests of the Service branches of `ExcludeSystemAccess` and the `VaryByUser` bypass (comment of 2026-10-05).
- Makes a public test helper reachable from production code, so the architecture test the comment asks for, "production assemblies do not reference `Encina.Testing`", goes into Phase 2 task 9.

### 5.6 M3's "issuing scope no longer active" check

The check is deferred from Phase 1 because no scopes existed yet. With the delegate shape it has an exact meaning: a context issued by the factory is valid while its `work` runs.

- **Mechanism.** The factory stamps each context it creates with an internal reference to its holder (`RequestContext.IssuingScope`). `Resolve` returns `Left(scope_conflict)` for an explicit context whose issuing scope has ended (a stale replay). `ForNestedDispatch` copies the reference.
- **Behaviour.** A context captured from `work` and replayed after `work` completed is refused.
- **Under `using` it would work too.** But the scope's lifetime would then depend on where `Dispose` runs, which section 2 shows is not reliable.

## 6. Recommendation and reasoning

**Adopt C1.** The public API is delegate-only (`RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync`, internal `RunAsBuiltInAsync`). Ending a scope only invalidates its holder. The dispatcher's internal frame-bound swaps stay as they are.

1. **It is the only shape that closes the elevation structurally.** Section 2.2: a `using` scope cannot tell its owner from an inherited child, and the async-frame restore is the one mechanism that can. Every `using`-based alternative trades the hole for broken nesting (C3) or a lint rule (C4).
2. **It removes failure modes instead of documenting them.** No handle means:
   - no disposal from a foreign flow (Blazor circuit, SignalR connection, `StartAsync`/`StopAsync`);
   - no forgotten `using` that leaks an actor into the next outbox message;
   - no helper that opens a scope and loses it on return;
   - no out-of-order disposal inside a flow.
3. **It is Railway Oriented by construction** (ADR-001, ADR-006).
   - `Task<Either<EncinaError, T>>` composes with `BindAsync` and `MatchAsync`, and a refusal never reaches `work`.
   - With `Either<EncinaError, RequestContextScope>` the caller must handle the `Left` *and* dispose the `Right` inside the match. That is two obligations, one of them invisible to the type system.
4. **It matches the hosts.** The Blazor activity handler, `IHubFilter`, `RequestDelegate`, `BackgroundService` loops and outbox processors are all "wrap the next step" APIs. No host needs a scope that outlives a call.
5. **It deletes code.**
   - The restore branch of `Pop` (`RequestContextAccessor.cs:149`) and its "in order" computation go.
   - `RequestContextScope` and its idempotent-dispose logic go.
   - The out-of-order tests become "outlived parent" tests across flows.
   - Phase 1's holder chain and invalidation stay as they are.
6. **What it costs.**
   - Callers write a lambda instead of a `using`: one closure and one async state machine per scope, which is negligible against a dispatch. A `TState` overload can remove the closure on the HTTP hot path if a benchmark shows a need.
   - Code that needs an identity across non-nested steps must restructure into one `work`. That is the intended constraint.

## 7. Concrete changes to the plan

These edits go into `docs/plans/security-context-population-implementation-plan-1705.md` in the Phase 2 plan update. They are listed here rather than applied, because PR #1824 is still editing the same file. Line numbers are those of `main` at the time of writing.

1. **Amendments table (Summary, after `m4`, line 81):** add a normative row:

   > **M6** — Scope shape. `IRequestContextScopeFactory` is delegate-only: `RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync` (internal `RunAsBuiltInAsync`, test seam `RunWithIdentityAsync`), each returning `Task<Either<EncinaError, T>>`. Ending a scope invalidates its holder and writes nothing; the caller's context is restored by the async frame. No public `RequestContextScope`. Replaces: `Begin*`, `RequestContextScope : IDisposable`, "restores the captured previous holder".

2. **Summary, #1705 item (3) (line 40):** "opened through `IRequestContextScopeFactory` (`RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync`), which return `Either` and invalidate the scope when the work completes". Drop "invalidate the scope on dispose".
3. **Entry-point table (deferred-dispatch row):** `BeginRestored` → `RunRestoredAsync`.
4. **Design 1, trusted-path rule (line 145):**
   - "while the scope that issued that identity is still active" becomes "while the `work` of the scope that issued it is running (`RequestContext.IssuingScope`, internal)".
   - Add the claims fingerprint to the identity comparison (5.1).
   - Add the identity-preserving setter rule (5.4): a set to a different authenticated identity is refused whatever the ambient kind.
5. **Design 2, middleware flow (line 191):**
   - Step 2 becomes `await scopeFactory.RunInboundAsync(info, (_, _) => next(context), context.RequestAborted)`.
   - Step 3 (`Dispose` in `finally`) is deleted.
   - A `Left` (only possible as inbound-over-inbound, when `UseEncinaContext` is registered twice) is logged (167) and `next` runs under the existing inbound context. It is not answered with a fresh anonymous context.
6. **Design 2, accessor lifetime (line 194):**
   - "ending a scope … then restores the previous holder in the current flow" becomes "ending a scope invalidates its holder; the caller's holder comes back when the scope's async frame returns".
   - Consequence (b) becomes "scopes nest LIFO by construction; a scope in a forked flow that outlives an enclosing scope reads Anonymous and logs 170".
7. **Design 2, Blazor (line 196):** `BeginInbound` → `RunInboundAsync` wrapping `next(activity)`. Add: "a role or claims refresh takes effect at the next activity; code outside an inbound activity (for example `AuthenticationStateChanged` handlers) runs Anonymous".
8. **Design 3, API block (lines 231-251):** replace with the interface of section 3, option B, plus the `Task`-returning extension overloads. Delete `RequestContextScope`. `allowOverInbound` moves into `IdentityScopeOptions`.
9. **Design 3, rules (lines 252-262):**
   - The `Dispose` bullet (line 261) becomes "when `work` completes, faults or is cancelled, the holder is invalidated (logs 168); no holder is restored by Encina".
   - The `RunAsServiceAsync` bullet (line 262) becomes the general rule.
   - Add `encina.identity.unsupported_accessor` (5.3).
10. **Phase 2, task 5 (line 481):** files become `IRequestContextScopeFactory.cs`, `IdentityScopeOptions.cs`, internal `RequestContextScopeFactory.cs` and `RequestContextScopeFactoryExtensions.cs` (the `Task`-returning overloads). `RequestContextScope.cs` is deleted.
11. **Phase 2, task 6:** add `encina.identity.unsupported_accessor`.
12. **Phase 2, task 8 (line 484):**
    - 170 becomes `IdentityScopeOutlivedParent` (Warning, kinds only).
    - 168 `IdentityScopeClosed` is logged from the factory's `finally`.
13. **Phase 2, task 9 (line 485):**
    - `RequestContextAccessor.Pop` becomes `End` (invalidate only; remove the in-order restore at `RequestContextAccessor.cs:149`).
    - Add `RequestContext.IssuingScope` (internal) and the stale-context check in `Resolve`.
    - Make the setter identity-preserving only (5.4).
    - Add the accessor-type startup validator (5.3).
    - Add a second architecture test: production assemblies do not reference `Encina.Testing`.
14. **Phase 2, task 10 (line 486), tests:**
    - Replace "out-of-order disposal" with "outlived parent across flows".
    - Add: child forked inside a scope cannot change the owner's identity; a body that completes synchronously restores the caller; refusal does not invoke `work`; `work`'s `Left` and exceptions propagate after invalidation; a forgotten-scope leak is impossible across two `RunRestoredAsync` iterations; a stale explicit context returns `scope_conflict`; a stream enumerated after `work` reads Anonymous; a custom accessor gives `unsupported_accessor` and fails startup.
15. **Phase 2, task 11:**
    - Resolved by 5.1: claims count, tenant does not.
    - Resolved by 5.2: refresh goes through a new scope; the setter keeps throwing.
    - Add `TestIdentity.Service` and `TestIdentity.RunAsAsync` (5.5).
16. **Phase 2 prompt:** rewrite the TASK block to the delegate API and the rules above. Drop "holder-based Dispose (invalidate own holder, restore previous only if valid, out-of-order Warning)".
17. **Phase 3, tasks 1 and 4 (lines 528, 531):** `RunInboundAsync`, as in items 5 and 7.
18. **Testing table, Unit row (line 805):**
    - `RequestContextScopeFactoryTests`: "restore, LIFO, idempotent dispose" becomes "caller restored by the frame, refusal without invoking work, `Left` pass-through, outlived parent".
    - `AccessorLifetimeTests`: "non-LIFO disposal" becomes "child cannot end the owner's scope".
19. **Phase 7 / ADR-035:** record this decision, with the probe of Appendix A as evidence, and list the delegate API as the only trusted path that binds an identity.

## 8. Open questions for the maintainer

- **Q1. Dead-flow scope opening.** After an inbound request or circuit activity ends, a forked task sees no ambient context, and Design 3 lets it open a service scope. Should a flow whose holder chain has ended, and whose root was `Inbound`, refuse `RunAsServiceAsync` and `RunAsPrincipalAsync` (`scope_conflict`, with `AllowOverInbound` as the logged escape)?
  - Recommended: **yes**. Fire-and-forget work from a request should go through a queue or a hosted service, not elevate in place.
- **Q2. Claims fingerprint scope.** Should all authenticated claims count (recommended, because `[RequireClaim]` can test any claim), or only an allow-list (`amr`, `acr`, `auth_time`, plus the configured role, permission and subject types)?
- **Q3. `TState` overloads** to avoid the closure in `EncinaContextMiddleware`: now, or only if a benchmark shows a need? Recommended: only on evidence.

## Appendix A: probe

A self-contained C# 14 file-based app. Run it with `dotnet run probe.cs`; the output is reproduced in section 2.1. `Push`, `Pop` and `Read` mirror `RequestContextAccessor.Push`, `Pop` (including the in-order restore) and `ContextHolder.IsValid` from Phase 1.

```csharp
var outer = Push("service:orchestrator");

// using shape: a child forked after Push pops the scope it inherited
var inner = Push("user:alice(restored)");
string? childAfterPop = null;
await Task.Run(() => { Pop(inner); childAfterPop = Read(); });
Console.WriteLine($"using/child after Pop  : {childAfterPop ?? "<anonymous>"}");
Console.WriteLine($"using/owner after child: {Read() ?? "<anonymous>"}");
Pop(inner);
Console.WriteLine($"using/owner after own Pop: {Read() ?? "<anonymous>"}");
Program.Current.Value = outer;

// delegate shape: ending only invalidates; the async frame restores the caller
Task? late = null;
string? lateRead = "unset";
await RunInScopeAsync("user:bob(restored)", async () =>
{
    Console.WriteLine($"delegate/body           : {Read()}");
    late = Task.Run(async () => { await Task.Delay(50); lateRead = Read(); });
    await Task.Yield();
});
Console.WriteLine($"delegate/caller after   : {Read()}");
await late!;
Console.WriteLine($"delegate/late child     : {lateRead ?? "<anonymous>"}");
await RunInScopeAsync("user:carol", () => Task.CompletedTask);
Console.WriteLine($"delegate/caller after sync body: {Read()}");

static async Task RunInScopeAsync(string ctx, Func<Task> body)
{
    var h = Push(ctx);
    try { await body(); } finally { h.Disposed = true; }
}

static Holder Push(string ctx) { var h = new Holder(ctx, Program.Current.Value); Program.Current.Value = h; return h; }

static void Pop(Holder h)
{
    var inOrder = !h.Disposed && ReferenceEquals(Program.Current.Value, h);
    h.Disposed = true;
    if (inOrder) { Program.Current.Value = h.Parent; }
}

static string? Read()
{
    for (var h = Program.Current.Value; h is not null; h = h.Parent) { if (h.Disposed) { return null; } }
    return Program.Current.Value?.Ctx;
}

sealed class Holder(string ctx, Holder? parent)
{
    public string Ctx = ctx;
    public Holder? Parent = parent;
    public volatile bool Disposed;
}

static partial class Program
{
    internal static readonly AsyncLocal<Holder?> Current = new();
}
```
