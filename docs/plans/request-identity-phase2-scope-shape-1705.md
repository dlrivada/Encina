# Request identity, Phase 2: the shape of `IRequestContextScopeFactory`

> **Issue**: [#1705](https://github.com/dlrivada/Encina/issues/1705) (comments of 2026-10-03 to 2026-10-05)
> **Plan**: [security-context-population-implementation-plan-1705.md](security-context-population-implementation-plan-1705.md), Design 2 ("Accessor lifetime"), Design 3 (scope API), Phase 2, and the "PR review amendments" table
> **Phase 1 code**: merged in #1824: `src/Encina/Core/RequestContextAccessor.cs`, `src/Encina/Core/AmbientRequestContext.cs`, `src/Encina/Core/RequestContext.cs`, `src/Encina/Identity/*`, `src/Encina.Testing/Identity/TestIdentity.cs`
> **Status**: design analysis for a maintainer decision. It changes no code. The adversarial review of this document (PR #1849, review of 2026-10-05) is applied; [section 9](#9-review-log) maps each finding to its change. Once accepted, the edits of [section 7](#7-concrete-changes-to-the-plan) go into the plan before the Phase 2 brief is written.
>
> **Historical record (added 2026-10-09).** This analysis was decided on #1705 on 2026-10-05 and implemented in Phase 2 (#1893), Phase 3 (#1905) and Phase 4 (#1982). The body below is the pre-decision text and is deliberately not rewritten: section 7 was applied to the plan as amendment M6 ([plan](security-context-population-implementation-plan-1705.md), [review log](reviews/security-context-population-1705-review-log.md)), and section 8 is closed (Q1 adopted, Q2 adopted with a different default list, Q3 adopted, Q4 only on benchmark evidence). Where the body and the code differ, the code and the plan win. Divergences: the default per-token claim list keeps `auth_time` out of the exclusion list and adds `nonce`, `at_hash`, `c_hash` (section 5.1); the setter never clears and accepts a tenant change only while no dispatch is in flight (section 5.4); `RunRestoredAsync` takes a required `PersistedIdentitySource` and a `configuredTenantId`, and the internal surface also has extra factory methods (section 3); the middleware skips WebSocket upgrades, extended CONNECT, `text/event-stream` GETs and `HubMetadata` endpoints under an anonymous connection marker, and a misordered `UseEncinaContext` (before `UseRouting`) is detected, logs Critical 202 and answers 500 (section 4.1); a refused Blazor activity (the `Left` branch) runs under an anonymous mask (section 4.4); EventIds 166-175 exist in code (sections 4.3, 7). Line numbers are those of `c3626ed`, before Phase 2.

---

## 1. The decision and the short answer

Phase 2 adds `IRequestContextScopeFactory`, the only public way to bind an identity (service, principal, inbound request, restored actor) to a region of code.

**What the plan specifies today.**

- The factory has the classic `using` shape: `Begin*` returns `Either<EncinaError, RequestContextScope>`, and `RequestContextScope.Dispose()` ends the scope.
- `Dispose` "invalidates the scope's holder and restores the captured previous holder only if it is still valid" (plan, Design 3, line 261).

**The hole the #1824 review found.**

- A `Task.Run` child forked after the scope opened inherits the same `AsyncLocal` holder as the scope's owner.
- Nothing written at open time can tell the two flows apart.
- So if the child is the one that disposes the scope, the restore branch of `Pop` runs in the child and installs the **parent** identity there.

**Recommendation: a delegate-only public API (option C1 below).**

- Every public scope is `Run*Async(…, work, …)`, and the scope ends when `work` completes.
- Ending a scope only invalidates its holder. The caller's identity comes back because the C# async machinery restores the caller's `ExecutionContext`, not because Encina writes it back.
- `RequestContextScope` (the `IDisposable`) is deleted from the public surface.
- The framework's own internal, frame-bound swaps stay as they are: the dispatcher's `AmbientRequestContext.Enter` and the stream `Flow`.

**Why, decisive arguments first:**

1. **No handle, so no foreign-flow disposal.** A circuit, connection, or `StartAsync`/`StopAsync` pair cannot dispose a scope from another flow.
2. **No forgotten `using`.** A restored actor cannot leak from one outbox message into the next.
3. **No helper that silently loses its scope.**
4. **The API is Railway Oriented by construction.** A refusal is a `Left` and never runs `work`.
5. **The child-pop elevation.** It needs application code that nests scopes, but no `using`-shaped API can prevent it.

C1 is a **correctness guard against accidental misuse, not a security boundary.** In-process code that wants a service identity can already call the public factory from a flow with no holder, for example through `ExecutionContext.SuppressFlow()`. ADR-035 must say so (section 7, item 25).

**The review found a larger issue, independent of the shape** (section 4.1). Connection-hosted flows (Blazor Server circuits, SignalR hubs) run inside the ExecutionContext of the HTTP request that opened the connection.

- Under WebSockets that request, and its inbound scope, lives as long as the connection.
- This breaks the plan's Blazor and SignalR assumptions.
- The fix: `EncinaContextMiddleware` binds no identity on connection endpoints.

## 2. Background: what Phase 1 built

The facts below are from `main` after #1824.

- **Holders.**
  - `RequestContextAccessor` stores a `ContextHolder` in one static `AsyncLocal` (`RequestContextAccessor.cs`).
  - A holder links to its `Parent`.
  - A holder reads as no context (Anonymous) once it, or any holder it is bound to, has ended (`ContextHolder.IsValid`).
  - Ending a holder clears its context (`Invalidate`, `:189`).
- **`Push` and `Pop`.**
  - `Push(context)` (`:114`) installs a new **scope** holder chained to the current live one.
  - `Pop(holder)` (`:140`) invalidates the holder. When the holder is the current scope of the flow running `Pop` ("in order"), it also writes `holder.Parent` back into that flow (`:149`).
- **The dispatcher swap.**
  - The dispatcher swaps the ambient context per dispatch with `AmbientRequestContext.Enter` and `AmbientSwap.Apply` (`AmbientRequestContext.cs:316`).
  - The swap is entered with `using var ambient = …` inside the async `ExecuteAsync` methods (`Encina.RequestDispatcher.cs:46`, `Encina.NotificationDispatcher.cs:44`).
  - The restore reinstalls the captured holder as-is (`RequestContextAccessor.Install`, `:108`), so an ended scope is never revived.
  - With an accessor that is not the default type, the swap falls back to plain sets (`ApplyPlain`, `:354`).
- **The public setter.**
  - `IRequestContextAccessor.RequestContext { get; set; }` (`IRequestContextAccessor.cs:60`) applies the explicit-context rule (`AmbientRequestContext.EnsureReplaceable`, `:121`).
  - It **refuses** to replace an ambient User with a different authenticated identity.
  - It **accepts and logs** (Warning 165) every other identity change, including the two-step "clear, then set another user".
  - Its remaining production callers: `EncinaContextMiddleware.cs:100` (moves to the factory in Phase 3) and `TenantResolutionMiddleware.cs:120` (`WithTenantId`). The dispatcher's internal swap also uses it, only for a custom accessor.
- **Explicit contexts.**
  - `Resolve` snapshots an explicit context with `RequestContext.CopyOf` (`RequestContext.cs:176-189`).
  - A foreign `IRequestContext` is rebuilt into a new `RequestContext` that carries the **same** `RequestIdentity` object.
  - `CheckExplicitContext` (`AmbientRequestContext.cs:95`) refuses an identity change over an ambient User and accepts it, with Warning 165, otherwise.
- **`IsSameAs`.** `RequestIdentity.IsSameAs` (`RequestIdentity.cs:187`) compares kind, user id, roles and permissions. It ignores other claims and the tenant.

### 2.1 The hazard, reproduced

A probe copies the holder semantics above (`Push`, `Pop` with the in-order restore, `IsValid`). Its source is in [Appendix A](#appendix-a-probe). It was run with `dotnet run` on .NET 10.0.112 in the authoring session. The scenario is **application code that nests scopes**:

- an outer service scope (`service:orchestrator`), for example an F1 job opened with `RunAsServiceAsync`;
- an inner restored-user scope (`user:alice(restored)`) opened by that job;
- a child forked inside the inner scope, which pops the inner scope.

The output, annotated:

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

1. **The child gets the outer identity.** The child disposed a scope it inherited, so `Pop` judged it "in order" and wrote the parent holder into the child's flow. A role-less restored user is now running as a declared service.
2. **The owner loses its own identity.** The child invalidated the owner's scope holder, so the owner's own `Dispose` is "out of order" and writes nothing. The owner stays Anonymous instead of returning to its outer identity. This fails closed, but it is wrong.

**Scope of the hazard.** The built-in deferred dispatchers (Outbox, Scheduling, Inbox, Saga) run Anonymous until P-50 (plan, Design 3: "Built-in dispatchers wired in #1705: none"). With them, a child-pop installs Anonymous, which fails closed. The elevation needs application code that nests a restored actor inside its own service scope, like the F1 jobs.

### 2.2 Why a `using` scope cannot tell the owner from the child

**Every token is inherited.**

- `AsyncLocal<T>` values live in the `ExecutionContext`, which is immutable and copy-on-write.
- `Task.Run` captures the current `ExecutionContext` by reference. The child starts with the *same* holder reference, the same `IsScope` flag and the same parent chain as the owner.
- Any token written at `Push` (an owner marker, a second `AsyncLocal`, a GUID) is captured too and is identical in both flows.
- The runtime exposes no "logical flow identity". Thread ids change at every `await`.

**So only two outcomes are possible.**

- `Pop` writes something back into the flow that calls it. Then a child can install the parent.
- `Pop` writes nothing. Then the owner of a `using` block nested inside the same method cannot get its outer identity back.

**What does tell them apart is the frame.** An `async` method's caller gets its own `ExecutionContext` back by two mechanisms (`dotnet/runtime`, `release/10.0`):

1. **Synchronous return.** `AsyncMethodBuilderCore.Start` (`System.Private.CoreLib/src/System/Runtime/CompilerServices/AsyncMethodBuilderCore.cs:21-56`) saves the thread's `ExecutionContext` before running the state machine. Its `finally` restores it through `ExecutionContext.RestoreChangedContextToThread`. This covers the caller up to the callee's first `await`.
2. **After the first `await`.** The caller's own continuation runs on the context the caller captured at its `await`: the state-machine box runs it through `ExecutionContext.RunInternal` with that captured context.

Neither mechanism reaches a child, which never returns into the frame that opened the scope.

## 3. The options

Every option keeps Phase 1's holder chain, the refusal rules of Design 3, logging and tenant binding. The refusals are: unknown service, over a User, over an inbound request, built-in names, and `tenant_conflict`. The options differ only in who ends a scope and what ending it writes.

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
}   // the caller's ExecutionContext is restored by the frame (section 2.2)
```

**Implementation constraint.** `Push` runs only inside the `async` body of this private method, never in a non-async wrapper. A non-async wrapper's write would stay in the caller's flow. A test pins it: call a `Run*Async` **without awaiting** it, and assert the caller's ambient right after the call returns.

**Results.**

- A refusal returns `Left` without invoking `work`.
- A `Left` from `work` passes through unchanged.
- An exception from `work` propagates after the holder is invalidated. Exceptions are not business logic, and `IEncina.Send` already converts handler failures into `Left`.

### C. Hybrids

| # | Hybrid | Verdict |
|---|---|---|
| C1 | **Delegate-only public API; frame-bound internal swaps kept.** The dispatcher's `Enter`/`AmbientSwap` and the stream `Flow` stay `using`-shaped but internal. Their handle is a `readonly struct` that never leaves the dispatcher's own async frame, so no other flow can dispose it. | **Recommended.** This is B, plus a statement of why the existing internal swaps are already safe. |
| C2 | `using` scope plus an owner check at `Dispose` (a token, a second `AsyncLocal`, a thread id). | **Not possible.** Section 2.2: any token is inherited by the child, and thread ids change at every `await`. |
| C3 | `using` scope whose `Dispose` only invalidates and never restores. | **Rejected.** It closes the elevation but breaks plain nesting. After `using (inner) { … }` the owner reads Anonymous until its own async method returns. Every outer scope or dispatch that runs code after an inner scope loses its identity. It is correct only when the `using` is the last statement of its own async method, and nothing can enforce that. |
| C4 | `using` scope plus a Roslyn analyzer: the scope must be a `using` declaration in an async method, never stored, returned, captured by a lambda or passed as an argument. | **Rejected.** It needs a new analyzer package and its own tests, and it is still bypassable (an `IDisposable` parameter, a generic helper, `dynamic`). It turns a structural guarantee into a lint rule. A `ref struct` scope, which the compiler would keep on the stack, does not help either: it cannot live across an `await`, and every scope body awaits. |
| C5 | Both shapes public: delegate first, `using` for "advanced" hosts. | **Rejected.** The guarantee is only as strong as the weakest public path. Every reviewer would have to tell the two apart, and ADR-035's trusted-path list would grow. |

## 4. Behaviour by scenario

The A and C1 columns describe the code as each option would implement it. The examples assume an outer identity P and an inner scope S.

| Scenario | A: `using` | B / C1: delegate |
|---|---|---|
| `Task.Run` child forked inside S, run while S is alive | Sees S | Sees S |
| Same child, run after S ended | Anonymous (holder invalid) | Anonymous |
| Child disposes S (handle passed or captured) | Child gets P installed; the owner is left Anonymous | Impossible: there is no handle |
| Out-of-order disposal in one flow (outer ended first) | Outer end invalidates the inner chain; the flow is Anonymous; the inner `Dispose` returns `false` and logs 170 | Impossible: call-stack nesting is LIFO by construction |
| A scope in a forked flow outlives an enclosing scope | The forked scope's holder is chained to the ended outer one, so it reads Anonymous | Same; its `End` finds the holder already invalid and logs 170 (renamed, section 7) |
| Scope opened inside an awaited helper (`async Task<RequestContextScope> OpenAsync()`) | **Silently does nothing**: the `AsyncLocal` change does not flow back to the caller | Not expressible |
| Forgotten `using` | The identity leaks into the rest of the flow (section 4.7) | Not expressible |
| Body completes synchronously, or the call is not awaited | Not applicable | Caller restored (probe, last line; constraint in section 3) |
| Connection-hosted flows (Blazor circuit, SignalR hub) | Same as the delegate column: both depend on section 4.1 | Anonymous outside per-activity and per-invocation scopes, **once section 4.1's middleware rule exists** |

### 4.1 Connection-hosted flows inherit the connection request's scope

This fact is independent of the scope shape, and it changes the Blazor and SignalR analysis. Sources are `dotnet/aspnetcore`, `release/10.0`.

**How the inheritance happens.**

- `HttpConnectionContext.TryActivatePersistentConnection` starts the connection application inside the request's own flow: `ApplicationTask ??= ExecuteApplication(connectionDelegate)` (`src/SignalR/common/Http.Connections/src/Internal/HttpConnectionContext.cs:421`; long polling `:460`).
- `ExecuteApplication` (`:626`) does `await Task.Yield(); await connectionDelegate(this);` and never suppresses ExecutionContext flow.
- So the hub connection handler, every hub invocation and every Blazor Server circuit activity run in an ExecutionContext forked from the `/_blazor` or hub request. That fork happens after `UseEncinaContext` opened its inbound scope.
- Under WebSockets, `HttpConnectionDispatcher` awaits the connection for its whole life (`HttpConnectionDispatcher.cs:158`, `:328`). The inbound scope therefore ends only at disconnect. This is also why `IHttpContextAccessor.HttpContext` is non-null inside hubs.

**Consequences without a fix, from Phase 3 on.**

- **Blazor.** Every per-activity `RunInboundAsync` finds an ambient User with `Origin = Inbound`. Design 3 refuses both cases with `scope_conflict`.
- **SignalR.** Hub dispatches run as the connect-time user, with connect-time roles, for hours. A role revocation does not apply until reconnect, so this fails open. It also contradicts the plan's "until F2, SignalR dispatches stay Anonymous" (plan line 197, Design 2 (c), m5).
- **Transport dependence.** Under long polling and SSE the same flows read Anonymous once the first poll ends, so behaviour depends on the transport.

**Fix: connection endpoints get no request identity.**

- `EncinaContextMiddleware` binds no identity for an endpoint that carries `HubMetadata`, which covers every `MapHub` and Blazor's `/_blazor` hub. It runs `next` outside any inbound scope.
- Connection flows then start Anonymous whatever the transport. The per-activity (Blazor) and per-invocation (SignalR, F2) scopes establish the identity, as intended.
- **Ordering requirement.** The middleware must see the endpoint, so it must run after routing. Minimal hosting adds `UseRouting` at the start of the pipeline unless the application calls it explicitly. Phase 3 documents "`UseEncinaContext` after `UseRouting`" and tests the case where routing comes later. When no endpoint is resolved, the middleware cannot tell a hub request and binds as today; the test pins this.

### 4.2 `Task.Run` children

- **Delegate.**
  - A child forked inside `work` inherits the scope holder. It reads S while `work` runs and Anonymous after.
  - It cannot end the scope, so it can never install P.
  - A child that opens its own `Run*Async` gets a nested holder chained to S's. That holder dies when S ends, which fails closed and matches Phase 1's rule.
- **`using`.** The same reads hold. The difference is that any code that can reach the scope object can end it from the wrong flow (section 2.1).

### 4.3 Out-of-order disposal

With the delegate shape the question disappears inside a flow: scopes nest like the call stack. Across flows the Phase 1 rule still applies, and it fails closed: a holder chained to an ended scope reads Anonymous.

`IdentityScopeDisposedOutOfOrder` (EventId 170, allocated in Phase 2, not yet in code) is renamed `IdentityScopeOutlivedParent`. It is logged when `End` finds the holder already invalidated by an enclosing scope.

### 4.4 Blazor Server: re-establishing the identity per activity

`CircuitHandler.CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)` is itself delegate-shaped. With the section 4.1 rule, the circuit flow has no ambient identity, so the per-activity scope opens cleanly:

```csharp
public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
    Func<CircuitInboundActivityContext, Task> next) =>
    async activity =>
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var outcome = await _scopes.RunInboundAsync(
            InboundRequestInfo.ForCircuit(state.User, _circuitCorrelationId),
            async (_, _) => { await next(activity); return Right<EncinaError, Unit>(unit); });

        if (outcome.IsLeft)
        {
            // Log 167 (error code only), then run the activity with no identity: it fails closed.
            // Never drop the activity (the circuit would stop processing events and JS interop),
            // and never reuse the connection's identity.
            Log.IdentityScopeRefused(_logger, outcome);
            await next(activity);
        }
    };
```

- **Refresh.** Each activity reads the current `AuthenticationState`. A role or claims refresh (a new `AuthenticationState` after re-authentication) therefore takes effect at the next activity, without the setter. This is the path the #1705 comment of 2026-10-05 requires: "a Phase 2 claims-refresh or circuit handler must go through a scope, not the setter".
- **Code outside an activity reads Anonymous and fails closed, on every transport, once the section 4.1 rule exists.** This covers continuations that outlive the activity and code on the circuit's synchronization context, such as an `AuthenticationStateChanged` handler. Without the rule, under WebSockets, that code reads the connect-time user.
- **The `using` risk.** With `using`, the correct handler is equally short. The tempting variant is a circuit-lifetime scope opened in `OnCircuitOpenedAsync`, stored in a field and disposed in `OnCircuitClosedAsync`, which runs in another flow. The delegate API cannot express it.

### 4.5 Hosted services

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
- **`using`.** The same works with `using` inside the loop, but `using` also allows two misuses:
  - a scope opened in `StartAsync` and disposed in `StopAsync`, which run in different flows: a no-op `Pop`, and a service identity alive for the life of the host;
  - a `using var` at the top of `ExecuteAsync`, which keeps every fire-and-forget task authorised until shutdown.
- The delegate API does not forbid wrapping the whole loop, but the documented pattern and the samples make per-iteration the default.
- `ABACPolicySeedingHostedService` runs once under the internal built-in identity, `RunAsBuiltInAsync("encina.abac.policy-seeding", …)`.

### 4.6 SignalR (follow-up F2)

`IHubFilter.InvokeMethodAsync(invocationContext, next)`, `OnConnectedAsync` and `OnDisconnectedAsync` are delegate-shaped. With the section 4.1 rule, an F2 hub filter calls `RunInboundAsync` per hub invocation from `invocationContext.Context.User`.

- **No `ValueTask` overload.** A `ValueTask<object?>` next is awaited inside the `Task`-returning `work`.
- **Streaming hub methods.** F2 must cover them (`IAsyncEnumerable<T>` or `ChannelReader<T>` results). Their items are produced after `InvokeMethodAsync` returns, so outside the per-invocation scope. This is the section 4.8 stream case one level up: they read Anonymous and fail closed until F2 scopes each item step, or binds the stream to its invocation.
- **With `using`.** The realistic mistake is a connection-lifetime scope kept in `Context.Items` and disposed in `OnDisconnectedAsync`. It lives for hours and is disposed from a foreign flow. The delegate API only offers per-invocation scopes.
- **Unchanged in both shapes.** SignalR does not refresh `Context.User` during a connection. F2 must state how long-lived connections pick up role changes.
- **Until F2.** Hub dispatches are Anonymous on every transport *once Phase 3's section 4.1 rule exists*. Until Phase 3, Phase 1 leaves every HTTP request anonymous anyway (maintainer decision of 2026-10-05). m5 ("SignalR … unmarked until F2, so `RunAsServiceAsync` is permitted there") becomes true under the rule. Without the rule, these flows would carry the connection request's Inbound-marked context and refuse it.

### 4.7 Deferred dispatch: SPEC-002 REQ-015, `BeginRestored` → `RunRestoredAsync`

Outbox, Scheduling, Inbox, dead-letter replay and the Saga/RoutingSlip runners process messages in a loop, one restored actor per message (P-50, #1164):

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

- **Forgotten `using`.** The restored actor of message *n* stays ambient for message *n+1*.
  - If it was a User, every later `BeginRestored` is refused (`scope_conflict`, ambient User): a processing outage.
  - If it was a Service, the next one is accepted with a Warning, and the chain grows for the whole batch.
  - The delegate API cannot leak between iterations.
- **Errors are never swallowed** (AGENTS.md §3).
  - A refusal (a tampered `PersistedRequestIdentity`, an unknown declared service) is a `Left` that flows into `MarkFailedAsync`.
  - With `Either<EncinaError, RequestContextScope>` the processor must remember to handle the `Left` *and* to dispose the `Right`.
- **Child-pop.** With the built-in processors, which run with no service identity, a child-pop installs Anonymous and fails closed. The elevation of section 2.1 needs an application job that restores actors inside its own service scope. The delegate shape removes it there too.
- **No change to M4.** A restored User carries no roles and no permissions, and an external inbox never restores a User.

### 4.8 `RunAsServiceAsync`, streams and synchronous callers

- **`RunAsServiceAsync`.** In the plan it is already an extension built on `BeginServiceScope`: the delegate shape for one case. Under C1 it and its siblings become the interface members, and `BeginServiceScope` no longer exists. The plan's line "opens and awaits inside one method frame, which removes the AsyncLocal pitfall" (line 262) becomes the rule for the whole API.
- **Streams.**
  - `IEncina.Stream` returns an `IAsyncEnumerable<T>`, and `AmbientRequestContext.Flow` reinstalls the dispatch holder at every step.
  - A stream enumerated *after* `work` returned reads Anonymous from then on. This fails closed in both shapes; the `using` equivalent is a stream returned out of the `using` block.
  - The how-to says "consume a stream inside `work`", and one test pins it.
- **Synchronous callers.** No overload is offered. Every entry point is asynchronous: the middleware, the circuit handler, hub filters, hosted services, Hangfire and Quartz jobs, and the outbox processors.

## 5. Impact on the points carried into Phase 2

### 5.1 `IsSameAs`: claims and tenant

Under C1, `IsSameAs` keeps two callers: the explicit-context check in `Resolve`, and the setter rule (section 5.4). Neither binds identities to code regions any more; the factory does that. So `IsSameAs` only decides whether "this context is the caller I already have".

**Claims count, except per-token ones.**

- `[RequireClaim]` evaluates `RequestIdentity.HasClaim` over the authenticated identities (Design 4), so any claim can drive authorization. An explicit context that differs only in `amr` or `acr` (step-up) is a different authorization subject, and today it is accepted silently.
- Comparing *all* claims would make two snapshots of the same session differ after every token refresh, on `exp`, `iat`, `nbf`, `auth_time`, `jti`, `uti`, `rh` and `aio`.
- **Recommendation:**
  - `IsSameAs` compares kind, user id, roles, permissions and the authenticated claims **minus a set of per-token claim types**.
  - That set is a new `RequestIdentityOptions.PerTokenClaimTypes`, defaulting to the eight types above.
  - `RequestIdentity` computes the comparison set once at construction; its claims are already frozen (`RequestIdentity.cs:234`).
- **Why an exclusion list and not an allow-list.** An allow-list (`amr`, `acr`, plus the configured subject, role, permission and tenant types) misses the custom claim types that `[RequireClaim]` tests, so a change in one of them would be accepted silently. An exclusion list errs towards "different identity", which is refused or logged, so it fails closed.

**Tenant is not identity, but it gets its own rule.**

- Tenant is not identity (Design 1), so `IsSameAs` ignores it.
- Two paths have no tenant rule today:
  - an explicit-context `Send(request, ambient.WithTenantId("other"))`: `IsSameAs` is true, so it is accepted silently;
  - the setter, which `TenantResolutionMiddleware.cs:120` uses for `WithTenantId`.
- **Recommendation for explicit dispatch:** `Resolve` returns `Left(encina.identity.tenant_conflict)` when the ambient identity is an authenticated User and the explicit context's tenant differs from the ambient tenant. It logs Warning 165 with kinds only. This is a cross-tenant dispatch under a user, a classic escalation.
- **Recommendation for the setter:** it keeps accepting `WithTenantId` in #1705, because `TenantResolutionMiddleware` needs it. F5 moves tenant resolution into `InboundRequestInfo`; once it has, the setter refuses tenant changes too. F5's issue file must carry this acceptance criterion.

### 5.2 Role and claims refresh

- In Phase 1 the setter throws when the same user arrives with different roles: `IsSameAs` counts roles, and the ambient is a User.
- Under C1 a refresh is a new scope. The next HTTP request, the next circuit activity (section 4.4) or the next hub invocation (section 4.6) runs `RunInboundAsync` with the current principal. For Blazor and SignalR this depends on section 4.1's rule.
- Inside one region the identity is immutable. A handler that "refreshes" mid-region is not supported, and it should not be: an authorization decision taken earlier in the same region would no longer match the identity that region later runs under.
- Nothing calls the setter for a refresh, so the throw stays as a guard.

### 5.3 Custom accessors

- `Push` and `End` are static members of `RequestContextAccessor`. `AddEncinaRequestIdentity` registers the accessor with `TryAdd`, so an application can register its own `IRequestContextAccessor`.
- In that case a scope would write to a store the dispatcher never reads (`Resolve` reads the *registered* accessor), and dispatches would silently run Anonymous. The same applies under `using`.

**Recommendation, independent of the shape:**

- The scope factory takes `IRequestContextAccessor` in its constructor. When the accessor is not the default `RequestContextAccessor`, every `Run*Async` returns `Left(encina.identity.unsupported_accessor)` without running `work`.
- A startup validator (`ValidateOnStart`, registered by `AddEncinaRequestIdentity`) fails the host with the same message, so the misconfiguration surfaces at boot, not on the first job.
- The validator also removes the case the #1705 comment raised: a decorated accessor whose restore can throw.
- `IRequestContextAccessor` stops being a replacement extension point (pre-1.0, no shim).
- The dispatcher's `ApplyPlain` path can stay for unit tests that construct `Encina` with a substituted accessor. Production hosts cannot reach it once the validator exists.

### 5.4 The two-step setter bypass

Under C1 no production code needs the public setter to change identity:

- the middleware and the circuit handler use `RunInboundAsync`;
- jobs use `RunAsServiceAsync`;
- deferred dispatch uses `RunRestoredAsync`.

**Recommendation: the setter accepts identity-preserving sets only.**

- A set is accepted when the new context is unauthenticated (a downgrade, which fails closed) or `IsSameAs` the current identity.
- Any change *to* a different authenticated identity is refused (Warning 165, `InvalidOperationException`), whatever the ambient kind.
- The two-step bypass closes. Clearing is allowed, but setting a user afterwards is a change from Anonymous to an authenticated identity, so it is refused.
- Exactly one way to bind an identity remains: the factory, which validates and logs (M3).
- Tenant changes through the setter follow section 5.1.

### 5.5 `TestIdentity.Service` and how tests bind identities

**`TestIdentity.Service` is a builder, nothing more.**

- `TestIdentity.Service(string name, IEnumerable<string>? roles = null, IEnumerable<string>? permissions = null)` builds a `ServiceIdentityDefinition` and calls the internal `RequestIdentity.ForService`. `InternalsVisibleTo` already covers `Encina.Testing`.
- It is a builder for explicit test contexts passed straight to a handler or behavior. It never binds an ambient identity.
- Whether a dispatch accepts such a context follows section 5.6.

**No test-only binding path.**

- `Encina.Testing` is a published package. Any application that references it could use a `RunAsAsync`-style helper over an internal seam to bind undeclared identities, with arbitrary roles, to production code. The architecture test only covers Encina's own assemblies.
- Tests therefore bind identities through the production path instead:
  - **Service identities.** The test host declares the service (`AddEncinaServiceIdentity("billing-reconciliation", …)`) and calls the public `RunAsServiceAsync`.
  - **User identities.** The test calls `RunAsPrincipalAsync` with a test `ClaimsPrincipal`. `TestIdentity.Principal(userId, roles, permissions, claims)` builds the principal, again only as a builder.

```csharp
services.AddEncinaServiceIdentity("billing-reconciliation", id => id.WithRoles("billing-job"));
// …
var result = await scopes.RunAsServiceAsync(
    "billing-reconciliation",
    (_, ct) => encina.Send(new ReconcileInvoices(), ct));
result.ShouldBeSuccess();
```

**This unblocks the missing tests** for the Service branches of `ExcludeSystemAccess` and the `VaryByUser` bypass (comment of 2026-10-05). The architecture test the comment asks for, "production assemblies do not reference `Encina.Testing`", goes into Phase 2 task 9.

### 5.6 M3's "issuing scope no longer active" check

M3 (plan line 75) and Design 1 (line 145) key the check on the **identity**: "when the identity's issuing scope is no longer active".

**A stamp on the context is not enough.** It is bypassable through `RequestContext.CopyOf`, which rebuilds a foreign `IRequestContext` and loses any internal field on the old object:

1. Capture `context.Identity` inside `work`.
2. After `work` ends, return it from a three-line `IRequestContext` implementation.
3. Dispatch with no ambient context. `CheckExplicitContext` sees an identity change over a non-User ambient and accepts it.

**Recommendation: stamp the issuer on the identity.**

- **Mechanism.** The factory stamps each `RequestIdentity` it creates with an internal reference to its holder (`RequestIdentity.Issuer`), set only by the factory. The same identity object travels through `With*`, `ForNestedDispatch` and `CopyOf`, so the stamp survives every copy.
- **`Resolve`.** An authenticated explicit identity whose issuer has ended returns `Left(scope_conflict)`: a stale replay.
- **No issuer at all.** An authenticated explicit identity with no issuer, such as one built with `TestIdentity.User(...)` or `TestIdentity.Service(...)`, returns `Left(scope_conflict)` **outside an active scope of the same identity**.
  - It is never accepted just because the ambient is Anonymous or a Service.
  - Tests that dispatch with an identity use the factory (section 5.5). The builders remain for tests that call a handler or behavior directly.
  - Phase 2 rewrites the existing tests that `Send` explicit `TestIdentity` contexts (for example `ExplicitContextConflictTests`).
  - ADR-035 records the rule.
- **`IsSameAs` ignores the issuer.** The issuer decides staleness, not sameness.
- **Why the delegate shape helps.** A scope ends exactly when its `work` completes, so "issuer ended" has a precise meaning that does not depend on where a `Dispose` happens to run.

## 6. Recommendation and reasoning

**Adopt C1.** The public API is delegate-only: `RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync`, plus the internal `RunAsBuiltInAsync`. Ending a scope only invalidates its holder. The dispatcher's internal frame-bound swaps stay as they are.

1. **It removes failure modes instead of documenting them.** No handle means:
   - no disposal from a foreign flow (Blazor circuit, SignalR connection, `StartAsync`/`StopAsync`);
   - no forgotten `using` that leaks an actor into the next outbox message;
   - no helper that opens a scope and loses it on return;
   - no out-of-order disposal inside a flow.
2. **It is Railway Oriented by construction** (ADR-001, ADR-006).
   - `Task<Either<EncinaError, T>>` composes with `BindAsync` and `MatchAsync`, and a refusal never reaches `work`.
   - With `Either<EncinaError, RequestContextScope>` the caller must handle the `Left` *and* dispose the `Right` inside the match: two obligations, one of them invisible to the type system.
3. **It matches the hosts.** The Blazor activity handler, `IHubFilter`, `RequestDelegate`, `BackgroundService` loops and the outbox processors are all "wrap the next step" APIs. No host needs a scope that outlives a call.
4. **It is the only shape that rules out the child-pop elevation of application-nested scopes.**
   - Section 2.2: a `using` scope cannot tell its owner from an inherited child. Every `using`-based alternative trades the hole for broken nesting (C3) or a lint rule (C4).
   - This is a correctness guard, not a security boundary: hostile in-process code can reach the public factory from a flow with no holder.
5. **It deletes code.**
   - The restore branch of `Pop` (`RequestContextAccessor.cs:149`) and its "in order" computation go, as do `RequestContextScope` and its idempotent dispose.
   - The out-of-order tests become "outlived parent" tests across flows.
   - Phase 1's holder chain and invalidation stay as they are.
6. **It costs a lambda per scope.**
   - Callers write a lambda instead of a `using`: one closure and one async state machine per scope, negligible against a dispatch.
   - Code that needs an identity across non-nested steps must restructure into one `work`. That is the intended constraint.

**Independent of the shape**, Phase 2 and Phase 3 also need:

- the connection-endpoint rule (section 4.1);
- the identity-stamped issuer check (section 5.6);
- the claims and tenant rules (section 5.1);
- the identity-preserving setter (section 5.4);
- the accessor startup validator (section 5.3).

## 7. Concrete changes to the plan

These edits go into `docs/plans/security-context-population-implementation-plan-1705.md` in the Phase 2 plan update. They are listed here, not applied, so that the decision comes first. Line numbers are those of `main` at `c3626ed`. "M6 (scope shape)" names the new amendment, to keep it apart from the existing lowercase `m6` (EventIds).

1. **Amendments table (after `m4`, line 81):** add a normative row:

   > **M6 (scope shape)** — `IRequestContextScopeFactory` is delegate-only: `RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync` (internal `RunAsBuiltInAsync`), each returning `Task<Either<EncinaError, T>>`. Ending a scope invalidates its holder and writes nothing; the caller's context is restored by the async frame. No public `RequestContextScope`, and no test-only binding seam. `EncinaContextMiddleware` binds no identity on `HubMetadata` endpoints. Replaces: `Begin*`, `RequestContextScope : IDisposable`, "restores the captured previous holder", connection flows "Anonymous until F2" without a mechanism.

2. **M3 (line 75):**
   - `BeginInbound(InboundRequestInfo)` → `RunInboundAsync(InboundRequestInfo, work)`.
   - "when the identity's issuing scope is no longer active" gains the mechanism: the internal `RequestIdentity.Issuer`.
   - Add the no-issuer refusal (section 5.6).
   - The test-only seam is limited to **building** identities and principals; **binding** goes through the factory (section 5.5).
3. **m5 (line 79):** "SignalR and subscription flows are unmarked until F2" becomes "connection flows start with no context, because the middleware skips `HubMetadata` endpoints (M6 (scope shape)); `RunAsServiceAsync` is therefore permitted there until F2".
4. **Summary, #1705 item (3) (line 40):** "opened through `IRequestContextScopeFactory` (`RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync`), which return `Either` and invalidate the scope when the work completes". Drop "invalidate the scope on dispose".
5. **Entry-point table:**
   - SignalR row (line 60): "Not populated: the middleware skips `HubMetadata` endpoints, so hub dispatches see Anonymous on every transport; streaming hub methods included in F2".
   - Deferred-dispatch row (line 62): `BeginRestored` → `RunRestoredAsync`.

6. **Design 1 (lines 144-145, 164):**
   - In the "Persisted form" signature (line 164), `BeginRestored` → `RunRestoredAsync`.
   - In the trusted-path rule (line 145), "while the scope that issued that identity is still active" becomes "while the identity's issuer (`RequestIdentity.Issuer`, internal, set only by the factory) is running its `work`; an authenticated explicit identity with no issuer is refused".
   - Add the per-token-claim exclusion to the identity comparison and the `tenant_conflict` rule for explicit dispatch under an ambient User (section 5.1).
   - Add the identity-preserving setter rule (section 5.4). The forging note on line 144 refers to the builders only.
7. **Design 2, middleware flow (line 191).**
   - Step 2 becomes: skip binding when `context.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>()` is not null; otherwise `await scopeFactory.RunInboundAsync(info, (_, _) => next(context), context.RequestAborted)`.
   - Step 3 (`Dispose` in `finally`) is deleted.
   - The `Left` cases, all logged with 167 (code only):
     - `scope_conflict`: an inbound or User context is already ambient, because `UseEncinaContext` is registered twice or the host runs the pipeline inside another scope;
     - `unsupported_accessor`: already fails startup through the validator.
   - Both are host misconfigurations. The middleware answers **500 without calling `next`**, so it fails closed and loudly.
   - This replaces the plan's "answered with an anonymous context". Producing an anonymous context there would need a binding path outside the validated factory, and it would hide the misconfiguration. Mapping problems (no subject, a reserved subject, conflicting identities) are never a `Left`: the factory maps them to Anonymous, as in Phase 1.
   - Add the ordering rule "`UseEncinaContext` after `UseRouting`" (section 4.1).
8. **Design 2, accessor lifetime (line 194).**
   - "ending a scope … then restores the previous holder in the current flow" becomes "ending a scope invalidates its holder; the caller's holder comes back through the async frame (section 2.2)".
   - Consequence (b) becomes "scopes nest LIFO by construction; a scope in a forked flow that outlives an enclosing scope reads Anonymous and logs 170".
   - Consequence (c) becomes "connection flows start with no context (`HubMetadata` endpoints are skipped); the Blazor activity handler establishes the identity per activity".
9. **Design 2, Blazor (line 196):** `BeginInbound` → `RunInboundAsync` wrapping `next(activity)`. Add:
   - "on `Left`: log 167 and run the activity with no identity";
   - "a role or claims refresh takes effect at the next activity";
   - "code outside an inbound activity runs Anonymous on every transport".
10. **Design 2, SignalR (line 197):** cite the mechanism (`HubMetadata` skip) for "dispatch Anonymous", and add streaming hub methods to F2.
11. **Design 3, option B row (line 209):** reword around `RunAsServiceAsync`. The row's argument is unchanged.
12. **Design 3, API block (lines 231-251):**
    - Replace it with the interface of section 3, option B, plus the `Task`-returning extension overloads.
    - Delete `RequestContextScope`.
    - `allowOverInbound` moves into `IdentityScopeOptions`.
13. **Design 3, rules (lines 252-262):**
    - Rename `BeginServiceScope`, `BeginPrincipal` and `BeginRestored` throughout (lines 256-260).
    - The `Dispose` bullet (line 261) becomes "when `work` completes, faults or is cancelled, the holder is invalidated (logs 168); Encina restores no holder".
    - The `RunAsServiceAsync` bullet (line 262) becomes the general rule, plus the implementation constraint of section 3 ("`Push` only inside the private `async` body").
    - Add `encina.identity.unsupported_accessor` (section 5.3).
14. **Design 6, explicit opt-outs (line 348):** "a declared service identity opened through the scope factory" now names `RunAsServiceAsync`.

15. **Task 5 (line 481):** the files become `IRequestContextScopeFactory.cs`, `IdentityScopeOptions.cs`, internal `RequestContextScopeFactory.cs` and `RequestContextScopeFactoryExtensions.cs` (the `Task`-returning overloads). `RequestContextScope.cs` is deleted.
16. **Task 6 (line 482):** add `encina.identity.unsupported_accessor`. `encina.identity.tenant_conflict` gains its explicit-dispatch use (section 5.1).
17. **Task 8 (line 484):**
    - 170 becomes `IdentityScopeOutlivedParent` (Warning, kinds only).
    - 168 `IdentityScopeClosed` is logged from the factory's `finally`.
18. **Task 9 (line 485):**
    - `RequestContextAccessor.Pop` becomes `End`: invalidate only, and the in-order restore at `RequestContextAccessor.cs:149` is removed.
    - Add `RequestIdentity.Issuer` (internal), the stale and no-issuer checks in `Resolve`, the per-token-claim comparison and `RequestIdentityOptions.PerTokenClaimTypes`, and the explicit-dispatch tenant rule.
    - Make the setter identity-preserving only, and add the accessor-type startup validator.
    - Add a second architecture test: production assemblies do not reference `Encina.Testing`.
19. **Task 10 (line 486), tests.** Replace "out-of-order disposal" with "outlived parent across flows". Add:
    - **Scope boundaries:** a child forked inside a scope cannot change the owner's identity; a body that completes synchronously restores the caller; a `Run*Async` call **not awaited** leaves the caller's ambient unchanged; refusal does not invoke `work`; `work`'s `Left` and exceptions propagate after invalidation; no identity leaks across two `RunRestoredAsync` iterations.
    - **Explicit contexts:** a stale explicit identity, including one wrapped in a foreign `IRequestContext`, and an issuer-less explicit identity each return `scope_conflict`; a different tenant under an ambient User returns `tenant_conflict`; per-token claim changes are the same identity, while `amr`/`acr` changes are a different one.
    - **Other:** a stream enumerated after `work` reads Anonymous; a custom accessor returns `unsupported_accessor` and fails startup; the Service branches of `ExcludeSystemAccess` and the `VaryByUser` bypass, through a declared test service and `RunAsServiceAsync`.
20. **Task 11 (line 487):**
    - Resolved by section 5.1: per-token claims are excluded and other claims count; tenant is not identity but has its own rule.
    - Resolved by section 5.2: a refresh goes through a new scope, and the setter keeps throwing.
    - Add `TestIdentity.Service` and `TestIdentity.Principal` as builders only (section 5.5).
21. **Phase 2 prompt (lines 502, 510):** rewrite the TASK block to the delegate API and the rules above. Drop "holder-based Dispose (invalidate own holder, restore previous only if valid, out-of-order Warning)" and the `BeginBuiltIn`/`BeginRestored` names.

22. **Phase 3, task 1 (line 529):** `RunInboundAsync`, the `HubMetadata` skip, the `Left` handling of item 7 and the ordering rule. **Task 4 (line 532):** the circuit handler of section 4.4. **Prompt (lines 547, 551):** the same names.
23. **Phase 3, task 9:** add TestServer tests over the **WebSocket** transport:
    - before F2, a hub method and a circuit activity read Anonymous;
    - a circuit activity reads the identity of the current `AuthenticationState`, and a changed `AuthenticationState` applies at the next activity;
    - with routing after `UseEncinaContext`, the documented behaviour holds.
24. **Testing table:**
    - **Unit row (line 806).** `RequestContextScopeFactoryTests`: "restore, LIFO, idempotent dispose" becomes "caller restored by the frame, not-awaited call, refusal without invoking work, `Left` pass-through, outlived parent". `AccessorLifetimeTests`: "non-LIFO disposal" becomes "child cannot end the owner's scope".
    - **Contract row (line 808):** "restore-on-dispose" becomes "invalidate-on-completion".
25. **Phase 7 / ADR-035 (line 720) and the combined prompt (line 931).** Record this decision with the probe of Appendix A as evidence, and list the delegate API as the only trusted path that **binds** an identity. State that it guards against accidental misuse, not hostile in-process code (section 1). Record the no-issuer rule. Line 931's "invalidates on dispose" becomes "invalidates when the work completes".

## 8. Open questions for the maintainer

- **Q1. Opening a scope in a dead flow.**
  - **The gap.** After an inbound request or circuit activity ends, a forked task sees no ambient context, so Design 3 lets it open a service scope.
  - **The option.** Refuse `RunAsServiceAsync` and `RunAsPrincipalAsync` in a flow whose holder chain has ended and whose root was `Inbound`, with `AllowOverInbound` as the logged escape.
  - **Implementation cost.** `ContextHolder.Invalidate` clears the context (`RequestContextAccessor.cs:189`), and the m5 `Origin` flag lives on the context. The holder would need its own origin field that survives invalidation.
  - **Limits.** `ExecutionContext.SuppressFlow()` bypasses it, so it only protects against accidental fire-and-forget code, not hostile code.
  - **Recommendation:** adopt it as that accidental-misuse guard, with the holder change, or leave it out. Do not present it as a security control.
- **Q2. Per-token claim types.** Is the default exclusion list right (`exp`, `iat`, `nbf`, `auth_time`, `jti`, `uti`, `rh`, `aio`)? Should it be configurable (recommended: yes, through `RequestIdentityOptions.PerTokenClaimTypes`)?
- **Q3. No-issuer explicit identities.** Should they be refused outside an active scope of the same identity (recommended, section 5.6), knowing that existing tests which `Send` explicit `TestIdentity` contexts must move to the factory?
- **Q4. `TState` overloads** to avoid the closure in `EncinaContextMiddleware`: now, or only if a benchmark shows a need? Recommended: only on evidence.

## 9. Review log

The adversarial review of `6a78455` (PR #1849, 2026-10-05) was applied as follows.

| # | Severity | Finding | Change |
|---|---|---|---|
| 1 | major | Connection flows inherit the connection request's inbound scope; Blazor and SignalR analysis wrong under WebSockets | Verified against `HttpConnectionContext.cs:421,626-633` (release/10.0). New section 4.1 (`HubMetadata` skip, ordering); sections 4.4 and 4.6 rewritten (`Left` handling, streaming hubs); plan items 3, 5, 7-10, 22, 23 |
| 2 | major | Context-stamped stale check bypassable through `CopyOf`; no-issuer case undefined | Verified against `RequestContext.cs:176-189`. Section 5.6: issuer on `RequestIdentity`; no-issuer refusal (Q3); items 2, 6, 18, 19, 25 |
| 3 | major | Test seam `RunWithIdentityAsync` is a public binding path in a published package | Section 5.5: seam dropped; tests use declared services and `RunAsPrincipalAsync`; items 1, 2, 20 |
| 4 | minor | The elevation needs application-nested scopes; C1 is not a security boundary; Q1 not implementable as written | Sections 1, 2.1, 4.7, 6 re-scoped; Q1 rewritten; item 25 |
| 5 | minor | "All claims" counts per-token claims | Section 5.1: exclusion list; Q2 |
| 6 | minor | No tenant rule for explicit dispatch or the setter | Section 5.1: `tenant_conflict` for explicit dispatch under a User; setter via F5 |
| 7 | minor | Middleware `Left` handling misdescribed | Item 7: each `Left`, 500 fail-closed, reason for leaving the anonymous fallback |
| 8 | minor | Section 7 incomplete; line numbers off; missing tests; M6 naming | Items 2, 3, 6, 11, 13, 14, 21, 22, 24, 25 added; lines recomputed on `c3626ed`; Service-branch tests in item 19; "M6 (scope shape)" |
| 9 | info | ExecutionContext claims correct; cite both restore mechanisms; non-awaited test; `ref struct` | Section 2.2 cites both; the constraint and test are in section 3 and item 19; C4 mentions `ref struct` |

## Appendix A: probe

A self-contained C# 14 file-based app. Run it with `dotnet run probe.cs`; the output is in section 2.1.

`Push`, `Pop` and `Read` mirror `RequestContextAccessor.Push`, `Pop` (including the in-order restore) and `ContextHolder.IsValid`. The probe simplifies two details, and neither changes the scenario:

- `Push` does not drop an ended parent (`LiveOrNull`);
- `Pop` compares with `ReferenceEquals` instead of `NearestScope`.

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
