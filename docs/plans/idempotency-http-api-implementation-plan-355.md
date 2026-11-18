# Implementation Plan: `Encina.AspNetCore` Stripe-style idempotency for HTTP APIs (replay marker, key policy, error answers)

> **Issue**: [#355](https://github.com/dlrivada/Encina/issues/355)
> **Type**: Feature
> **Verified against**: origin/main `d523eda92` (2026-11-18)
> **Complexity**: Medium (4 phases, no database provider code, ~20 files in 3 pull requests)
> **Estimated Scope**: ~250-350 lines of production code + ~600-800 lines of tests

---

## Summary

Issue #355 asks for Stripe-style API idempotency: a key from an HTTP header, a cached response returned for a duplicate with a clear `Idempotent-Replayed` marker, concurrent duplicates handled with a lock, a TTL, key format validation and distributed stores (Redis, SQL Server, in-memory). Most of its mechanism overlaps [#334](https://github.com/dlrivada/Encina/issues/334) (the store and the behavior) and the issue proposes a second `IIdempotencyStore`, a second `IdempotencyPipelineBehavior` and an `IIdempotentRequest` interface that already exists with another meaning. This plan therefore keeps only what is specific to HTTP and defers the rest to its owners (the disposition table is in Research).

**What the code does today (verified on `origin/main` `d523eda92`)**:

- The host already extracts the key: `EncinaAspNetCoreOptions.IdempotencyKeyHeader` (default `X-Idempotency-Key`, `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs:50`) is read by `HttpContextInboundRequestExtensions.cs:100` into `InboundRequestInfo.IdempotencyKey`, and `EncinaContextMiddleware` runs the request inside the inbound scope. So the issue's `IdempotencyMiddleware` that "extracts the key from the header" would duplicate it.
- `InboundRequestNormalizer.IdempotencyKey` (`src/Encina/Identity/InboundRequestNormalizer.cs`) never refuses: a key longer than 255 characters, blank or with control characters is silently dropped, so the request runs as if it had no key. A request marked idempotent then fails inside the behavior with `idempotency.missing_key`, which the status mapping answers 500 ([#3438](https://github.com/dlrivada/Encina/issues/3438) reports this).
- Replays are decided in the pipeline (`DistributedIdempotencyPipelineBehavior` over the cache, `InboxPipelineBehavior` over the Inbox); the endpoint receives `Right(response)` and cannot tell a first execution from a replay.
- The status mapping `MapErrorCodeToStatusCode` (`src/Encina.AspNetCore/ProblemDetailsExtensions.cs`) matches code text and answers 500 for `idempotency.*` today; ADR-062 replaces it with the error category, delivered by [#3419](https://github.com/dlrivada/Encina/issues/3419) (open).
- No integration test drives an HTTP request through an idempotent endpoint.

What it implements:

- The `Idempotent-Replayed: true` response header on a replayed answer, from the outcome that [#334](https://github.com/dlrivada/Encina/issues/334) records in its ambient `IIdempotencyOutcomeAccessor` (a singleton over an `AsyncLocal` holder: `src/Encina/Dispatchers/Encina.RequestDispatcher.cs:51` opens its own DI scope for every `Send`, so a scoped accessor read from `HttpContext.RequestServices` would never see the replay). The middleware opens the outcome scope around the dispatch and reads it from the response callback.
- A key policy at the HTTP boundary: a request that carries the idempotency header with an unusable value (too long, blank, control characters, or not a UUID when the application chose `Uuid`) is answered 400 problem+json with a fixed detail and without the value.
- The HTTP answers of the idempotency errors, by their ADR-062 category and not by code text: `idempotency.missing_key` and the invalid-key refusal 400, `idempotency.in_progress` 409, a reused key with a different payload (if [#3438](https://github.com/dlrivada/Encina/issues/3438) adds a fingerprint) 400 or 422 (Design Choice 4).
- Observability of the refusal and the replay header, and an in-process end-to-end test through the real middleware.

Standards and specifications: the IETF "Idempotency-Key HTTP header field" draft (in-flight duplicate 409; a reused key with another payload rejected; a missing key on a request that requires one is 400); Stripe idempotent requests (named comparable product: `Idempotent-Replayed: true`); ADR-062 (HTTP status from the category); ADR-054 D4 (tenant); AGENTS.md section 3 (fail-closed gates; `EncinaError.Message` never reaches the client unless the Development option is on); ADR-001 and ADR-006.

Estimated scope: about 20 distinct files (2 new and 4 modified production files, 6 metadata files, 7 test files, 1 documentation file), shipped as three pull requests (A, B, C; see "Pull request split").

Affected packages: `Encina.AspNetCore` only (and its `EventIdRanges` claim in `Encina`). No change in `Encina.Caching` or `Encina.Messaging`: the store, the behavior and the outcome types are [#334](https://github.com/dlrivada/Encina/issues/334).

Provider category: none for the HTTP layer. The Caching category applies through [#334](https://github.com/dlrivada/Encina/issues/334) (the 8 cache providers), the Database category through the Inbox, not through this plan.

---

## Design Choices

<details>
<summary><strong>1. Where the replay marker is written — inside the request scope of <code>EncinaContextMiddleware</code>, or a separate middleware or filter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaContextMiddleware.RunNextAsync` opens an `IdempotencyOutcomeScope` (`accessor.BeginScope()`, the singleton accessor of [#334](https://github.com/dlrivada/Encina/issues/334) resolved with `context.RequestServices.GetService`) around `_next` and registers `Response.OnStarting` with a static callback that holds the scope object and sets `Idempotent-Replayed: true` when `scope.Outcome` is `Replayed`** (the callback runs after the middleware code returned, which is why the scope's `Outcome` stays readable after `Dispose`) | No new middleware, no ordering rule (the middleware already has a misordering latch and already writes the correlation id header at the same spot, `RunNextAsync`); works for controllers, minimal APIs and endpoints that write the response themselves | The header is a side effect of the context middleware, so it exists only when the application called `UseEncinaContext()` (which every idempotent endpoint needs anyway to get the key) |
| **B) A new `UseEncinaIdempotency()` middleware** | Explicit and optional | A second middleware that must sit inside the first; one more ordering error to latch and test; the outcome scope still has to be opened and read the same way |
| **C) An `IEndpointFilter` and an MVC filter** | Per endpoint | Two filter types for two hosting models; endpoints that bypass them (a hub) get no header |

### Chosen Option: **A — `OnStarting` in the existing middleware** (recommended, pending the maintainer)

### Rationale

- A wins when the header must appear for every hosting model with no registration step; B wins when an application wants idempotency answers without the identity middleware, which cannot happen because the key itself comes from that middleware's scope.
- The accessor must be the ambient one of [#334](https://github.com/dlrivada/Encina/issues/334) Design Choice 6: the scoped variant is defeated by the dispatcher's own DI scope (`src/Encina/Dispatchers/Encina.RequestDispatcher.cs:51`), so a header read from `HttpContext.RequestServices` would never be set. This choice depends on that Dilemma; if the maintainer picks another carrier there (for example a returned wrapper), this plan's Phase 2 task 2 changes with it.
- The header is added only when `EncinaAspNetCoreOptions.AddIdempotentReplayedHeader` (default `true`, the issue's `AddReplayedHeader`) is on; the value is the fixed text `true`, never the key.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Which layer replays — the pipeline (as today), or an HTTP response cache in a middleware</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The pipeline behavior replays the stored `Right(response)`; the endpoint maps it as it mapped the first execution** | One mechanism for HTTP, queue consumers and scheduled work; no response buffering; the behavior already exists | The replayed HTTP answer is recomputed from the stored response, so a header that the endpoint derives from state (a `Location` built from the current request) is computed again |
| **B) A middleware stores the full HTTP response (status, headers, body) and replays it byte for byte, as the issue's `IdempotencyMiddleware` does** | Exact replay including status and headers; independent of the pipeline | Buffers every response of an idempotent request; a second store and a second behavior beside the existing ones (two mechanisms for one feature to keep consistent); invisible to non-HTTP callers |

### Chosen Option: **A — replay in the pipeline** (recommended, pending the maintainer)

### Rationale

- A wins when handlers are deterministic mappings of their response, which holds for Encina endpoints (`ToProblemDetails`, `ToActionResult`); B wins when the application returns custom `IResult` objects whose headers must be reproduced exactly, for example a `Location` that embeds the first request's host.
- The issue's request-level `IIdempotentRequest { string? IdempotencyKey }` interface is not adopted: `Encina.Messaging.Inbox.IIdempotentRequest` already exists as a marker, and both existing behaviors read `IRequestContext.IdempotencyKey`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. An unusable key header — refuse the request at the boundary, or drop it as today</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaContextMiddleware` answers 400 problem+json (fixed detail, no value) when the idempotency header is present and unusable (longer than `InboundRequestInfo.MaxIdempotencyKeyLength`, blank, control characters, or not a UUID under `IdempotencyKeyFormat.Uuid`), before the scope opens** | A client that sent a key wants idempotent semantics; silently running without one on a payment endpoint is a correctness trap; fail closed (AGENTS.md section 3); matches the existing refusal pattern (`ForbiddenDetails`) | Rejects a request on an endpoint that is not idempotent when a gateway forwards junk in that header |
| **B) Keep dropping the value; the idempotent behavior answers `idempotency.missing_key` as 400** | Non-idempotent endpoints are unaffected; `InboundRequestNormalizer` keeps its "never refuses" contract | The client cannot tell "missing" from "unusable"; an idempotent endpoint with an unusable key answers as if no key was sent |

### Chosen Option: **A — refuse at the boundary** (recommended, pending the maintainer)

### Rationale

- A wins for APIs where a retried unsafe request must not run twice (the issue's payment example); B wins when the application sits behind a gateway that adds an unrelated header of the same name to every request.
- The format policy is `EncinaAspNetCoreOptions.IdempotencyKeyFormat` (`Opaque` default, `Uuid`), the issue's `ValidateKeyFormat`; Stripe accepts any string up to 255 characters, so `Opaque` is the default. The issue's `RejectExpiredKeys` is not planned: a key has no expiry of its own, the entry's TTL ends it, and a reused key after expiry is a new request.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Status for a reused key with a different payload — 400 or 422</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InvalidRequest` (400), the category of a request that cannot be honored as sent** | Fits the ADR-062 table with no amendment; Stripe answers 400 for this case; a per-endpoint override exists through the `statusCode` argument of `ToProblemDetails` | The IETF draft names 422 |
| **B) 422 through a new ADR-062 row** | Follows the draft | ADR-062 has no 422 category; the amendment is decided with the owners of [#3419](https://github.com/dlrivada/Encina/issues/3419) and [#3420](https://github.com/dlrivada/Encina/issues/3420) |
| **C) `Conflict` (409)** | Existing category | Indistinguishable from the in-progress answer that clients retry |

### Chosen Option: **A — `InvalidRequest` (400)** (recommended, pending the maintainer)

### Rationale

- The case exists only if [#3438](https://github.com/dlrivada/Encina/issues/3438) (open, `needs-decision`) adds a payload fingerprint; this plan records the status and adopts #3438's error code. A wins for API clients written against Stripe's behavior; B wins for clients validated against the IETF draft.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The idempotency store of the issue — consume #334 or a second store here</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the store and the behavior of [#334](https://github.com/dlrivada/Encina/issues/334); no store, lock or behavior here** | One mechanism; the issue's Redis and in-memory stores are the cache providers; the SQL Server store is the question [#334](https://github.com/dlrivada/Encina/issues/334) asks once (durable backend: the Inbox, or a store on the database providers) | This issue's checklist items for stores and locking are met by another issue |
| **B) A second `IIdempotencyStore` here with `TryLockAsync` and `ReleaseLockAsync`** | Matches the issue text | Two stores with one name; the issue's own sketch is the "Inbox pattern only" alternative it rejected, rebuilt |

### Chosen Option: **A — consume #334** (recommended, pending the maintainer)

### Rationale

- The concurrency answer the issue wants (a concurrent duplicate while the first runs) is the `InProgress` claim of the store, answered 409 here.
- A wins while [#334](https://github.com/dlrivada/Encina/issues/334) (which owns the store, the lock-like claim and the TTL) keeps its own store Dilemmas open (its Design Choices 1 and 4): this plan then builds on whichever store #334 ends with and needs no change. B wins only if the maintainer rejects a cache-backed store in #334 and wants the HTTP layer to own a store, which would duplicate #334's contract.
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. The header name default — owned by #3438</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `X-Idempotency-Key` as the default; the property stays configurable** | No behavior change | The `X-` prefix is deprecated (RFC 6648) and the IETF draft names `Idempotency-Key` |
| **B) Default `Idempotency-Key`** | Follows the draft; pre-1.0 allows the break | Existing clients of the default must send the new name |

### Chosen Option: **B — default `Idempotency-Key`, decided with #3438** (recommended, pending the maintainer)

### Rationale

- [#3438](https://github.com/dlrivada/Encina/issues/3438) raises the same point; the answer is asked once, there. A wins when clients already send `X-Idempotency-Key`; B wins for a pre-1.0 library with no existing user (AGENTS.md section 1).
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models — options, policy and error codes

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs`** (modify): `IdempotencyKeyFormat` (new enum `IdempotencyKeyFormat { Opaque, Uuid }`, default `Opaque`), `AddIdempotentReplayedHeader` (bool, default `true`); the header default changes only if Design Choice 6 picks B (one line).
2. **`src/Encina.AspNetCore/IdempotencyKeyHeaderPolicy.cs`** (new, internal static): `IdempotencyKeyHeaderPolicy.IsUsable(string? raw, IdempotencyKeyFormat format)` (the same bounds as `InboundRequestNormalizer.IdempotencyKey`, plus `Guid.TryParse` for `Uuid`); a pure function, no I/O.
3. Error codes and categories used by the answers (created by [#334](https://github.com/dlrivada/Encina/issues/334) Phase 1): `idempotency.missing_key` = `InvalidRequest` (400), `idempotency.in_progress` = `Conflict` (409). This plan adds `idempotency.invalid_key` to the same `IdempotencyErrorCodes` class only if the boundary refusal is expressed as an `EncinaError` (the refusal itself is a fixed problem+json, so no error object is needed; no new code is planned).
4. `PublicAPI.Unshipped.txt` of `Encina.AspNetCore` (enum, two properties).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #355 adds the HTTP face
of idempotency; the store, behavior and outcome accessor are issue #334 (merged before this phase).
TASK: Add IdempotencyKeyFormat and AddIdempotentReplayedHeader to EncinaAspNetCoreOptions and the internal static
IdempotencyKeyHeaderPolicy with IsUsable(string?, IdempotencyKeyFormat).
KEY RULES: XML docs on every public symbol; the options class has no secrets; defaults asserted by unit tests; no
[Obsolete]; update PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina/Identity/InboundRequestNormalizer.cs,
src/Encina/Identity/InboundRequestInfo.cs
```

</details>

### Phase 2: Middleware — refuse an unusable key, mark a replay

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.AspNetCore/EncinaContextMiddleware.cs`** (modify): in `RunInboundAsync`, before `RunHostInboundAsync`, read the header named by `IdempotencyKeyHeader`; when it is present and `IdempotencyKeyHeaderPolicy.IsUsable` is false, answer `Results.Problem(statusCode: 400, title: "Bad Request", detail: <fixed text>)` (the same helper path as the 403 refusals in `Complete`), log the new `IdempotencyKeyRejected` entry (header name only) and do not run the pipeline. Fail closed: a request that carries the header is never run with the value dropped.
2. In `RunNextAsync`, after the correlation id header: when `AddIdempotentReplayedHeader` is on, resolve the singleton `IIdempotencyOutcomeAccessor` with `context.RequestServices.GetService` (absent means no marker, no scope), open `using var outcomeScope = accessor.BeginScope()` before `_next(context)` (so the `AsyncLocal` holder flows into the dispatch although `RequestDispatcher.cs:51` opens its own DI scope), and register `context.Response.OnStarting` with a static callback whose state is that `IdempotencyOutcomeScope`; the callback sets `Idempotent-Replayed: true` when `outcomeScope.Outcome == IdempotencyOutcome.Replayed`, read after the middleware code returned (the scope's `Outcome` stays readable after `Dispose`, per [#334](https://github.com/dlrivada/Encina/issues/334) Phase 1). Nothing scoped is captured by the singleton middleware: the instance holds only options and logger, and the accessor is a singleton.
3. Keep `InvokeAsync` and `RunInboundAsync` under CRAP 10: the refusal check and the `OnStarting` registration are two small private methods.
4. **`src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs`** (modify): `[LoggerMessage]` EventIds 230 (`IdempotencyKeyRejected`, Warning, no value) and 231 (`IdempotentReplayMarked`, Debug), inside the existing `AspNetCore` range (200-249), which the registry shows free from 230; no range registration is needed.
5. The two problem+json refusals for an in-progress duplicate and a missing key come from the ADR-062 category table once [#3419](https://github.com/dlrivada/Encina/issues/3419) S2 and H1 are merged: this plan adds no code-text branch to `MapErrorCodeToStatusCode`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: issue #355, Phase 2. EncinaContextMiddleware binds the inbound scope per request and already refuses ambiguous
identities with a fixed problem+json (ForbiddenDetails). The ambient outcome accessor exists (issue #334).
TASK: Refuse a present but unusable idempotency header with 400 (fixed detail, no value, pipeline not run); open the
IdempotencyOutcomeScope (accessor.BeginScope()) around _next and register Response.OnStarting with a static callback
that sets Idempotent-Replayed: true when that scope's Outcome is Replayed.
KEY RULES: never write the key value into a response, log or metric; the accessor is a singleton over an AsyncLocal
holder (RequestDispatcher opens its own DI scope per Send, so never read a scoped accessor from HttpContext.RequestServices);
resolve it per request with GetService, absent means no marker; [LoggerMessage] ids 230-231 only; do not raise the CRAP of
InvokeAsync; the misordering latch and the 403/500 answers stay as they are.
REFERENCE FILES: src/Encina.AspNetCore/EncinaContextMiddleware.cs, src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs,
tests/Encina.UnitTests/AspNetCore/EncinaContextMiddlewareTests.cs
```

</details>

### Phase 3: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

1. Tenant: no new source; the key's tenant segment is composed by the behavior of [#334](https://github.com/dlrivada/Encina/issues/334) from the identity scope this middleware already opens. A test sends one key from two tenants and expects two executions and no replay marker.
2. Telemetry: the decision counter and activity tag are recorded by the behavior; this phase adds only the two log entries above. No new meter (a second counter for the same decision would double count).
3. Documentation of the answers: a table in the AspNetCore guide (header, replay marker, 400, 409, the 200-or-409 choice of the Inbox owned by [#3440](https://github.com/dlrivada/Encina/issues/3440)).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: issue #355, Phase 3. Everything stateful is issue #334; this phase proves the HTTP face end to end.
TASK: Add the two-tenant HTTP test and hand the guide table to docs-writer.
KEY RULES: tests drive the real middleware and the real behavior over MemoryCacheProvider; no mocks of the code under
test; FakeTimeProvider; no Thread.Sleep.
REFERENCE FILES: tests/Encina.UnitTests/AspNetCore/EncinaContextMiddlewareTests.cs, tests/Encina.IntegrationTests/Web
```

</details>

### Phase 4: Testing and Documentation

<details>
<summary><strong>Tasks</strong></summary>

1. Unit (`tests/Encina.UnitTests/AspNetCore/`): `IdempotencyKeyHeaderPolicyTests` (blank, 255 and 256 characters, control characters, UUID forms), `EncinaContextMiddlewareTests` extended (unusable key answers 400 with no value in the body, usable key passes, replay marker present only for `Replayed`, option off removes the marker, the two log entries), `EncinaAspNetCoreOptionsTests` extended (defaults).
   Also in `EncinaContextMiddlewareTests` (behavior, so UnitTests and not guard): a request with the idempotency header and no accessor registered still passes (no marker, no exception), and a record made inside the dispatcher's inner DI scope sets the marker through the ambient outcome scope.
2. Guard (`tests/Encina.GuardTests/AspNetCore/`): no new guard file and no new guard test: this plan adds no public constructor or method with parameters (the new members are enum values and two option properties; `IdempotencyKeyHeaderPolicy` is internal), `InvokeAsync` keeps its signature, and the existing middleware argument checks live in `tests/Encina.GuardTests/AspNetCore/RequestIdentityHttpGuardTests.cs` (it already exists and holds guard 55).
3. Contract: no contract test. The HTTP contract (400, 409, replay marker) is asserted in the TestServer integration test below, because `tests/Encina.ContractTests` has no `Microsoft.AspNetCore.TestHost` reference (only `Encina.IntegrationTests` has it, with `Encina.AspNetCore`, `Encina.Caching` and `Encina.Caching.Memory` project references), so a contract test there would need a csproj edit and a new package reference for no contract the plan adds (no new public interface); the manifest entry of the middleware lists only the unit and guard flags.
4. Integration (a new file in `tests/Encina.IntegrationTests/Web/`, in-process `TestServer`, Memory cache provider, no container): first request 201, same key 201 with the marker and one handler run, concurrent same key 409, different tenant executes, unusable key 400, missing key on a marked request 400 (the last two status assertions after #3419 S2 and H1). Database features need real databases; this one touches none.
5. Load and benchmark: justification files (the added work per request is one header read and one `OnStarting` registration).
6. Documentation through `docs-writer` and `mechanical-fixer`: XML docs, the two changelog fragments (`355-idempotency-key-refused.changed.md` in PR A, `355-idempotent-replayed-header.added.md` in PR C), the AspNetCore README section, the guide table.
7. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the table below, `crap-gate` table for the changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: issue #355. Tests must execute real package code. Targets never go down: EncinaContextMiddleware.cs keeps
unit 95 and guard 55 (manifest .github/coverage-manifest/Encina.AspNetCore.json).
TASK: Write the unit and TestServer integration tests of the plan (no guard or contract test is added, see the plan) and the justification files;
hand documentation to docs-writer and the PublicAPI, changelog and manifest lines to mechanical-fixer.
KEY RULES: Shouldly through Encina.Testing.Shouldly; builders for test data; deterministic clock; one behavior per test.
REFERENCE FILES: tests/Encina.UnitTests/AspNetCore/EncinaContextMiddlewareTests.cs, docs/en/guides/TESTING.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| IETF Idempotency-Key header draft | A request that requires a key and has none is 400; an in-flight duplicate is 409; reuse with another payload is rejected | Phase 2 and Design Choices 3 and 4 |
| Stripe idempotent requests | Replays carry `Idempotent-Replayed: true`; keys up to 255 characters | Design Choice 1; the format default `Opaque` |
| ADR-062 | HTTP status comes from the error category; hosts do not parse codes | No code-text branch is added |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on | No new telemetry here |
| AGENTS.md section 3 | Gates fail closed; `EncinaError.Message` never reaches the client | The 400 refusal has a fixed detail and no value |
| RFC 6648 | The `X-` prefix is deprecated | Design Choice 6 |

### Disposition of the issue's checklist

| Issue item | Disposition |
|------------|-------------|
| `IIdempotentRequest` interface | Not adopted: the name exists in `Encina.Messaging.Inbox` as a marker; keys come from `IRequestContext.IdempotencyKey` (Design Choice 2) |
| `[Idempotent]` attribute with TTL | Key derivation and the attribute: [#319](https://github.com/dlrivada/Encina/issues/319); the TTL is the global `CachingOptions.IdempotencyTtl` ([#334](https://github.com/dlrivada/Encina/issues/334)) |
| `IIdempotencyStore`, `IdempotencyPipelineBehavior`, response caching, locking, TTL | [#334](https://github.com/dlrivada/Encina/issues/334) |
| Redis and in-memory stores | The cache providers, through [#334](https://github.com/dlrivada/Encina/issues/334) |
| SQL Server store | The durable-backend question of [#334](https://github.com/dlrivada/Encina/issues/334) Design Choice 4 |
| `IdempotencyMiddleware` | Not built: `EncinaContextMiddleware` already extracts the key; the replay marker and the refusal are added to it (Design Choices 1 and 2) |
| Header configuration, `ReturnCachedResponse`, `AddReplayedHeader`, `ValidateKeyFormat`, `RejectExpiredKeys` | `IdempotencyKeyHeader` exists; `AddIdempotentReplayedHeader` and `IdempotencyKeyFormat` are new; `ReturnCachedResponse` is the behavior's replay (always on when the marker is set); `RejectExpiredKeys` is dropped (Design Choice 3) |
| "85%+ test coverage" | Replaced by the per-file, per-flag targets below (AGENTS.md section 9) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaContextMiddleware` | `src/Encina.AspNetCore/EncinaContextMiddleware.cs` | Gets the refusal and the `OnStarting` marker |
| `HttpContextInboundRequestExtensions` | `src/Encina.AspNetCore/HttpContextInboundRequestExtensions.cs` | Existing header read (line 100) |
| `InboundRequestNormalizer`, `InboundRequestInfo.MaxIdempotencyKeyLength` | `src/Encina/Identity/` | The bounds the policy repeats |
| `IIdempotencyOutcomeAccessor`, `IdempotencyOutcomeScope`, `IdempotencyOutcome` | `src/Encina/Idempotency/` (new in #334 PR A; singleton over an `AsyncLocal` holder) | The replay signal, opened by the middleware around the dispatch |
| `DistributedIdempotencyPipelineBehavior`, `InboxPipelineBehavior` | `src/Encina.Caching/Behaviors/`, `src/Encina.Messaging/Inbox/` | The replaying behaviors |
| `ProblemDetailsExtensions` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Maps the answers by category once #3419 lands |
| `EncinaContextMiddlewareLog` | `src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs` | New entries 230-231 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AspNetCore` | **230-231** inside the existing `AspNetCore` = (200, 249) | Used on main: 200-207; claimed by plans: 208-229 (#3443, #710, #2229, #454), so 230-231 are the next free ids; 232-249 stay free. No new range is needed |

### Per-file coverage targets

| File | Unit | Guard | Justification |
|------|-----:|------:|---------------|
| `EncinaContextMiddleware.cs` (modified) | 95 | 55 | The current targets stay (they never go down); the new refusal and the marker are covered by unit and TestServer tests in the same file's existing suite |
| `IdempotencyKeyHeaderPolicy.cs` (new) | 100 | 0 | A pure function whose branches are all asserted; internal static with no `InternalsVisibleTo` for `Encina.GuardTests`, so no guard test can reach it |
| `IdempotencyKeyFormat.cs` (new, enum) | 0 | 0 | An enum with two members and no executable code; its values are exercised through the policy tests, which count on `IdempotencyKeyHeaderPolicy.cs` |
| `EncinaAspNetCoreOptions.cs` (modified) | 100 | 0 | Defaults and setters, as today; the options class has no argument validation |
| `EncinaContextMiddlewareLog.cs` (modified) | 0 | 0 | Source-generated `[LoggerMessage]` partials with no hand-written lines (as in the current entry); the messages are asserted in the middleware tests |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 2 | `IdempotencyKeyHeaderPolicy.cs` and the `IdempotencyKeyFormat` enum (own file) |
| Production source, modified | 4 | `EncinaContextMiddleware.cs`, `EncinaContextMiddlewareLog.cs`, `EncinaAspNetCoreOptions.cs`, `HttpContextInboundRequestExtensions.cs` only if the header read is shared with the policy |
| Metadata | 6 | `PublicAPI.Unshipped.txt` of `Encina.AspNetCore`, manifest `Encina.AspNetCore.json`, two changelog fragments, README, `docs/INVENTORY.md` (if the enum is listed) |
| Tests | 7 | policy unit (new), middleware unit (extended), options unit (extended), TestServer integration (new), load and benchmark justification files (2); no guard or contract file (Phase 4 tasks 2 and 3) |
| Documentation | 1 | AspNetCore guide table |
| **Total** | **~20 distinct** | Three pull requests (the PR table sums to more because the middleware, its log file and its unit test are touched in A and B) |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phase 1 and the refusal of Phase 2 task 1: `EncinaAspNetCoreOptions.cs`, `IdempotencyKeyFormat.cs`, `IdempotencyKeyHeaderPolicy.cs`, `EncinaContextMiddleware.cs`, `EncinaContextMiddlewareLog.cs` (entry 230), `PublicAPI.Unshipped.txt`, manifest `Encina.AspNetCore.json`, and the unit tests (policy, middleware, options); the changelog fragment (`changelog.d/355-idempotency-key-refused.changed.md`, section `changed`: a request with an unusable idempotency header is now answered 400, where it ran with the key dropped before) | 11 |
| B | Phase 2 task 2 (outcome scope and `OnStarting` marker): `EncinaContextMiddleware.cs`, `EncinaContextMiddlewareLog.cs` (entry 231), the middleware unit test file (extended), the TestServer integration test (new file; no csproj edit: `Encina.IntegrationTests` already references `Encina.AspNetCore`, `Encina.Caching`, `Encina.Caching.Memory` and `Microsoft.AspNetCore.TestHost`); starts after [#334](https://github.com/dlrivada/Encina/issues/334) PR A (the accessor) and PR C (the behavior that records the outcome), and, for the 409 and 400 status assertions, after [#3419](https://github.com/dlrivada/Encina/issues/3419) S2 and H1. It does not wait for [#3438](https://github.com/dlrivada/Encina/issues/3438) (`needs-decision`): the marker and the 409 work without the fingerprint-mismatch answer, which is added to this layer by a follow-up when #3438 adds its error code | 4 |
| C | Load and benchmark justification files (2), changelog fragment (`changelog.d/355-idempotent-replayed-header.added.md`), README, guide table, `docs/INVENTORY.md` | 6 |

Order: A can merge first (it needs nothing from #334: the refusal reads only options); B after #334 A and C; C last. Each pull request builds and passes alone: A adds options, policy and the refusal with their own tests; B adds a scope and a callback that are a no-op when the accessor is not registered. A and B are under the 15-file and 1500-line limits including tests, manifest and `PublicAPI`.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility (no [Obsolete], no aliases).
Issue #355 is the HTTP face of idempotency in Encina.AspNetCore: the replay marker header, a key policy at the
boundary and the HTTP answers of the idempotency errors by ADR-062 category. The store, the behavior and the outcome
accessor are issue #334 and are not rebuilt here.

IMPLEMENTATION OVERVIEW:
1. EncinaAspNetCoreOptions: IdempotencyKeyFormat (Opaque default, Uuid), AddIdempotentReplayedHeader (default true).
2. IdempotencyKeyHeaderPolicy.IsUsable (internal static, pure).
3. EncinaContextMiddleware: refuse a present but unusable header with 400 problem+json (fixed detail, no value, pipeline
   not run); open an IdempotencyOutcomeScope from the singleton IIdempotencyOutcomeAccessor around the dispatch and
   register Response.OnStarting to set Idempotent-Replayed: true when that scope's Outcome is Replayed.
4. EncinaContextMiddlewareLog: EventIds 230 (IdempotencyKeyRejected) and 231 (IdempotentReplayMarked) in the existing
   AspNetCore range.
5. Tests: unit, TestServer integration (Memory cache provider); justification files; docs.

KEY PATTERNS:
- Fail closed: a request carrying the header is never run with the value silently dropped.
- The middleware is a singleton: it holds no scoped service. The outcome carrier is the ambient accessor of #334 (a
  singleton over an AsyncLocal holder), because the dispatcher opens its own DI scope per Send.
- The key and EncinaError.Message never reach responses, logs or tags.
- No code-text status branch: the 409 and 400 come from the ADR-062 category table (#3419 S2 and H1).
- Targets never go down (EncinaContextMiddleware.cs unit 95, guard 55); CRAP <= 10 on every changed method.

REFERENCE FILES:
docs/plans/idempotency-http-api-implementation-plan-355.md, src/Encina.AspNetCore/EncinaContextMiddleware.cs,
src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina/Identity/InboundRequestNormalizer.cs,
docs/architecture/adr/062-error-category-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The replayed response comes from the idempotency store of [#334](https://github.com/dlrivada/Encina/issues/334); the HTTP layer adds no cache |
| 2 | OpenTelemetry | ❌ | The decision counter and activity tag are recorded by the behavior of [#334](https://github.com/dlrivada/Encina/issues/334); the HTTP layer only reads the outcome |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 230-231 in the existing `AspNetCore` range for a rejected key and a marked replay, never the key value |
| 4 | Health Checks | ❌ | The HTTP layer has no dependency of its own |
| 5 | Validation | ✅ | The key format and length are validated at the HTTP boundary and an unusable key answers 400 |
| 6 | Resilience | ❌ | The HTTP layer calls no external system |
| 7 | Distributed Locks | ❌ | Concurrent same-key requests are serialized by the claim of the store ([#334](https://github.com/dlrivada/Encina/issues/334)) and answered 409 |
| 8 | Transactions | ❌ | The HTTP layer opens no transaction |
| 9 | Idempotency | ✅ | This is the HTTP face of the idempotency feature: header policy, replay marker, 409 in progress, 400 for a missing or unusable key |
| 10 | Multi-Tenancy | ✅ | The tenant comes from the request identity scope and is part of the store key ([#334](https://github.com/dlrivada/Encina/issues/334)); no new tenant source, and a two-tenant HTTP test proves isolation |
| 11 | Module Isolation | ❌ | The key is not module scoped |
| 12 | Audit Trail | ❌ | A replayed or rejected duplicate is a flow-control answer, covered by telemetry |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#334](https://github.com/dlrivada/Encina/issues/334) | Blocks PR B | Store, behavior, ambient `IIdempotencyOutcomeAccessor` and `IdempotencyOutcomeScope` (its Design Choice 6); asks the durable-backend question once |
| [#3419](https://github.com/dlrivada/Encina/issues/3419) | Blocks the status assertions of PR B | Category-based status table (S2) and the AspNetCore host slice (H1) |
| [#3438](https://github.com/dlrivada/Encina/issues/3438) | Shares the decisions of Design Choices 4 and 6 (`needs-decision`) | Fingerprint, header name, missing key answer |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Related | The 200-or-409 answer and `Retry-After` on a 409 for the Inbox; the same host table |
| [#3439](https://github.com/dlrivada/Encina/issues/3439) | Related | Atomic claim and release in the store |
| [#734](https://github.com/dlrivada/Encina/issues/734) | Not planned here | Webhook idempotency waits for the home decision of [#3388](https://github.com/dlrivada/Encina/issues/3388); it will reuse the key policy |

---

## Next Steps

1. Open PR A when the maintainer has answered Design Choice 3 (refuse at the boundary or drop); it needs nothing from #334.
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer Dilemmas 1 to 6 (replay marker placement (its carrier is #334's Dilemma 6), replay layer, refusal at the boundary, status for a reused key (asked once, with #3438), the store of the issue (asked once, with #334), header default (asked once, with #3438)) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Re-check ids 230-231 in `Encina.AspNetCore` against the claims of the other plans when PR A starts.
4. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
