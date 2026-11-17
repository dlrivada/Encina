# Implementation Plan: `Encina.AspNetCore` Stripe-style idempotency for HTTP APIs (replay marker, key policy, error answers)

> **Issue**: [#355](https://github.com/dlrivada/Encina/issues/355)
> **Type**: Feature
> **Verified against**: origin/main `ec129cae5` (2026-11-17)
> **Complexity**: Medium (4 phases, no database provider code, ~21 files in 3 pull requests)
> **Estimated Scope**: ~250-350 lines of production code + ~600-800 lines of tests

---

## Summary

Issue #355 asks for Stripe-style API idempotency: a key from an HTTP header, a cached response returned for a duplicate with a clear `Idempotent-Replayed` marker, concurrent duplicates handled with a lock, a TTL, key format validation and distributed stores (Redis, SQL Server, in-memory). Most of its mechanism overlaps [#334](https://github.com/dlrivada/Encina/issues/334) (the store and the behavior) and the issue proposes a second `IIdempotencyStore`, a second `IdempotencyPipelineBehavior` and an `IIdempotentRequest` interface that already exists with another meaning. This plan therefore keeps only what is specific to HTTP and defers the rest to its owners (the disposition table is in Research).

**What the code does today (verified on `origin/main` `ec129cae5`)**:

- The host already extracts the key: `EncinaAspNetCoreOptions.IdempotencyKeyHeader` (default `X-Idempotency-Key`, `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs:50`) is read by `HttpContextInboundRequestExtensions.cs:100` into `InboundRequestInfo.IdempotencyKey`, and `EncinaContextMiddleware` runs the request inside the inbound scope. So the issue's `IdempotencyMiddleware` that "extracts the key from the header" would duplicate it.
- `InboundRequestNormalizer.IdempotencyKey` (`src/Encina/Identity/InboundRequestNormalizer.cs`) never refuses: a key longer than 255 characters, blank or with control characters is silently dropped, so the request runs as if it had no key. A request marked idempotent then fails inside the behavior with `idempotency.missing_key`, which the status mapping answers 500 ([#3438](https://github.com/dlrivada/Encina/issues/3438) reports this).
- Replays are decided in the pipeline (`DistributedIdempotencyPipelineBehavior` over the cache, `InboxPipelineBehavior` over the Inbox); the endpoint receives `Right(response)` and cannot tell a first execution from a replay.
- The status mapping `MapErrorCodeToStatusCode` (`src/Encina.AspNetCore/ProblemDetailsExtensions.cs`) matches code text and answers 500 for `idempotency.*` today; ADR-062 replaces it with the error category, delivered by [#3419](https://github.com/dlrivada/Encina/issues/3419) (open).
- No integration test drives an HTTP request through an idempotent endpoint.

What it implements:

- The `Idempotent-Replayed: true` response header on a replayed answer, from the outcome that [#334](https://github.com/dlrivada/Encina/issues/334) records in `IIdempotencyOutcomeAccessor`.
- A key policy at the HTTP boundary: a request that carries the idempotency header with an unusable value (too long, blank, control characters, or not a UUID when the application chose `Uuid`) is answered 400 problem+json with a fixed detail and without the value.
- The HTTP answers of the idempotency errors, by their ADR-062 category and not by code text: `idempotency.missing_key` and the invalid-key refusal 400, `idempotency.in_progress` 409, a reused key with a different payload (if [#3438](https://github.com/dlrivada/Encina/issues/3438) adds a fingerprint) 400 or 422 (Design Choice 4).
- Observability of the refusal and the replay header, and an in-process end-to-end test through the real middleware.

Standards and specifications: the IETF "Idempotency-Key HTTP header field" draft (in-flight duplicate 409; a reused key with another payload rejected; a missing key on a request that requires one is 400); Stripe idempotent requests (named comparable product: `Idempotent-Replayed: true`); ADR-062 (HTTP status from the category); ADR-054 D4 (tenant); AGENTS.md section 3 (fail-closed gates; `EncinaError.Message` never reaches the client unless the Development option is on); ADR-001 and ADR-006.

Estimated scope: about 21 distinct files (2 new and 4 modified production files, 5 metadata files, 9 test files, 1 documentation file), shipped as three pull requests (A, B, C; see "Pull request split").

Affected packages: `Encina.AspNetCore` only (and its `EventIdRanges` claim in `Encina`). No change in `Encina.Caching` or `Encina.Messaging`: the store, the behavior and the outcome types are [#334](https://github.com/dlrivada/Encina/issues/334).

Provider category: none for the HTTP layer. The Caching category applies through [#334](https://github.com/dlrivada/Encina/issues/334) (the 8 cache providers), the Database category through the Inbox, not through this plan.

---

## Design Choices

<details>
<summary><strong>1. Where the replay marker is written — inside the request scope of <code>EncinaContextMiddleware</code>, or a separate middleware or filter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaContextMiddleware.RunNextAsync` registers `Response.OnStarting`, which reads the scoped `IIdempotencyOutcomeAccessor` from `HttpContext.RequestServices` and sets `Idempotent-Replayed: true` when the outcome is `Replayed`** | No new middleware, no ordering rule (the middleware already has a misordering latch and already writes the correlation id header at the same spot, `RunNextAsync`); works for controllers, minimal APIs and endpoints that write the response themselves | The header is a side effect of the context middleware, so it exists only when the application called `UseEncinaContext()` (which every idempotent endpoint needs anyway to get the key) |
| **B) A new `UseEncinaIdempotency()` middleware** | Explicit and optional | A second middleware that must sit inside the first; one more ordering error to latch and test; the outcome accessor still has to be read the same way |
| **C) An `IEndpointFilter` and an MVC filter** | Per endpoint | Two filter types for two hosting models; endpoints that bypass them (a hub) get no header |

### Chosen Option: **A — `OnStarting` in the existing middleware** (recommended, pending the maintainer)

### Rationale

- A wins when the header must appear for every hosting model with no registration step; B wins when an application wants idempotency answers without the identity middleware, which cannot happen because the key itself comes from that middleware's scope.
- The header is added only when `EncinaAspNetCoreOptions.AddIdempotentReplayedHeader` (default `true`, the issue's `AddReplayedHeader`) is on; the value is the fixed text `true`, never the key.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Which layer replays — the pipeline (as today), or an HTTP response cache in a middleware</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The pipeline behavior replays the stored `Right(response)`; the endpoint maps it as it mapped the first execution** | One mechanism for HTTP, queue consumers and scheduled work; no response buffering; the behavior already exists | The replayed HTTP answer is recomputed from the stored response, so a header that the endpoint derives from state (a `Location` built from the current request) is computed again |
| **B) A middleware stores the full HTTP response (status, headers, body) and replays it byte for byte, as the issue's `IdempotencyMiddleware` does** | Exact replay including status and headers; independent of the pipeline | Buffers every response of an idempotent request; a second store and a second behavior beside the existing ones (AGENTS.md section 3: no second mechanism); invisible to non-HTTP callers |

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
<summary><strong>5. The idempotency store of the issue — owned by #334</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the store and the behavior of [#334](https://github.com/dlrivada/Encina/issues/334); no store, lock or behavior here** | One mechanism; the issue's Redis and in-memory stores are the cache providers; the SQL Server store is the question [#334](https://github.com/dlrivada/Encina/issues/334) asks once (durable backend: the Inbox, or a store on the database providers) | This issue's checklist items for stores and locking are met by another issue |
| **B) A second `IIdempotencyStore` here with `TryLockAsync` and `ReleaseLockAsync`** | Matches the issue text | Two stores with one name; the issue's own sketch is the "Inbox pattern only" alternative it rejected, rebuilt |

### Chosen Option: **A — consume #334**

### Rationale

- The concurrency answer the issue wants (a concurrent duplicate while the first runs) is the `InProgress` claim of the store, answered 409 here.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (a feature has one mechanism; no second implementation beside an existing one) and [#334](https://github.com/dlrivada/Encina/issues/334) (owns the idempotency store, the lock-like claim and the TTL)

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
2. In `RunNextAsync`, after the correlation id header: when `AddIdempotentReplayedHeader` is on, `context.Response.OnStarting` with a static callback that resolves `IIdempotencyOutcomeAccessor` from `context.RequestServices` (`GetService`; absent means no marker) and sets `Idempotent-Replayed: true` when `Outcome == IdempotencyOutcome.Replayed`. No scoped service is captured by a singleton: the middleware instance holds only options and logger; the accessor is resolved per request.
3. Keep `InvokeAsync` and `RunInboundAsync` under CRAP 10: the refusal check and the `OnStarting` registration are two small private methods.
4. **`src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs`** (modify): `[LoggerMessage]` EventIds 230 (`IdempotencyKeyRejected`, Warning, no value) and 231 (`IdempotentReplayMarked`, Debug), inside the existing `AspNetCore` range (200-249), which the registry shows free from 230; no range registration is needed.
5. The two problem+json refusals for an in-progress duplicate and a missing key come from the ADR-062 category table once [#3419](https://github.com/dlrivada/Encina/issues/3419) S2 and H1 are merged: this plan adds no code-text branch to `MapErrorCodeToStatusCode`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: issue #355, Phase 2. EncinaContextMiddleware binds the inbound scope per request and already refuses ambiguous
identities with a fixed problem+json (ForbiddenDetails). The outcome accessor exists (issue #334).
TASK: Refuse a present but unusable idempotency header with 400 (fixed detail, no value, pipeline not run) and register
Response.OnStarting to set Idempotent-Replayed: true when the scoped outcome is Replayed.
KEY RULES: never write the key value into a response, log or metric; the middleware is a singleton: resolve the scoped
accessor from HttpContext.RequestServices per request; [LoggerMessage] ids 230-231 only; do not raise the CRAP of
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
2. Guard (`tests/Encina.GuardTests/AspNetCore/`): the middleware guard file extended: a request with the idempotency header and a context without the accessor registered still passes (no marker, no exception).
3. Contract: an HTTP contract test that a marked request with a missing key answers 400, an in-progress duplicate 409, a replay 200 with `Idempotent-Replayed: true` (after #3419 S2 and H1).
4. Integration (in-process `TestServer`, `tests/Encina.IntegrationTests/Web`, Memory cache provider, no container): first request 201, same key 201 with the marker and one handler run, concurrent same key 409, different tenant executes, unusable key 400. Database features need real databases; this one touches none.
5. Load and benchmark: justification files (the added work per request is one header read and one `OnStarting` registration).
6. Documentation through `docs-writer` and `mechanical-fixer`: XML docs, changelog fragment `changelog.d/355-http-idempotency.added.md`, the AspNetCore README section, the guide table.
7. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the table below, `crap-gate` table for the changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: issue #355. Tests must execute real package code. Targets never go down: EncinaContextMiddleware.cs keeps
unit 95 and guard 55 (manifest .github/coverage-manifest/Encina.AspNetCore.json).
TASK: Write the unit, guard, contract and TestServer integration tests of the plan and the justification files;
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
| `IIdempotencyOutcomeAccessor`, `IdempotencyOutcome` | `src/Encina/Idempotency/` (new in #334 PR A) | The replay signal |
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
| `EncinaAspNetCoreOptions.cs` (modified) | 100 | 0 | Defaults and setters, as today; the options class has no argument validation |
| `EncinaContextMiddlewareLog.cs` (modified) | 0 | 0 | Source-generated `[LoggerMessage]` partials with no hand-written lines (as in the current entry); the messages are asserted in the middleware tests |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 2 | `IdempotencyKeyHeaderPolicy.cs` and the `IdempotencyKeyFormat` enum (own file) |
| Production source, modified | 4 | `EncinaContextMiddleware.cs`, `EncinaContextMiddlewareLog.cs`, `EncinaAspNetCoreOptions.cs`, `HttpContextInboundRequestExtensions.cs` only if the header read is shared with the policy |
| Metadata | 5 | `PublicAPI.Unshipped.txt` of `Encina.AspNetCore`, manifest `Encina.AspNetCore.json`, changelog fragment, README, `docs/INVENTORY.md` (if the enum is listed) |
| Tests | 9 | policy unit, middleware unit (extended), options unit (extended), middleware guard (extended), HTTP contract, TestServer integration, load and benchmark justification files (2), the DI test extended if the options are validated |
| Documentation | 1 | AspNetCore guide table |
| **Total** | **~21 distinct** | Three pull requests (the PR table sums to 23 because the middleware and its unit test are touched in A and B) |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phase 1 and the refusal of Phase 2 task 1: options, enum, policy, middleware refusal, log entries, `PublicAPI` and manifest, unit and guard tests | 11 |
| B | Phase 2 task 2 (`OnStarting` marker), the HTTP contract test and the TestServer integration test; starts after [#334](https://github.com/dlrivada/Encina/issues/334) PR A (the accessor) and PR C (the behavior that records the outcome), and, for the 409 and 400 contract assertions, after [#3419](https://github.com/dlrivada/Encina/issues/3419) S2 and H1 | 6 |
| C | Load and benchmark justification files, changelog fragment, README, guide table, `docs/INVENTORY.md` | 6 |

Order: A can merge first (it needs nothing from #334: the refusal reads only options); B after #334 A and C; C last. Each pull request builds and passes alone: A adds options, policy and the refusal with their own tests; B adds a callback that reads an accessor and is a no-op when the accessor is not registered.

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
   not run); register Response.OnStarting to set Idempotent-Replayed: true when the scoped IIdempotencyOutcomeAccessor
   says Replayed.
4. EncinaContextMiddlewareLog: EventIds 230 (IdempotencyKeyRejected) and 231 (IdempotentReplayMarked) in the existing
   AspNetCore range.
5. Tests: unit, guard, contract, TestServer integration (Memory cache provider); justification files; docs.

KEY PATTERNS:
- Fail closed: a request carrying the header is never run with the value silently dropped.
- The middleware is a singleton: resolve scoped services from HttpContext.RequestServices per request.
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
| [#334](https://github.com/dlrivada/Encina/issues/334) | Blocks PR B | Store, behavior, `IIdempotencyOutcomeAccessor`; asks the durable-backend question once |
| [#3419](https://github.com/dlrivada/Encina/issues/3419) | Blocks the status assertions of PR B | Category-based status table (S2) and the AspNetCore host slice (H1) |
| [#3438](https://github.com/dlrivada/Encina/issues/3438) | Shares the decisions of Design Choices 4 and 6 (`needs-decision`) | Fingerprint, header name, missing key answer |
| [#3440](https://github.com/dlrivada/Encina/issues/3440) | Related | The 200-or-409 answer and `Retry-After` on a 409 for the Inbox; the same host table |
| [#3439](https://github.com/dlrivada/Encina/issues/3439) | Related | Atomic claim and release in the store |
| [#734](https://github.com/dlrivada/Encina/issues/734) | Not planned here | Webhook idempotency waits for the home decision of [#3388](https://github.com/dlrivada/Encina/issues/3388); it will reuse the key policy |

---

## Next Steps

1. Open PR A when the maintainer has answered Design Choice 3 (refuse at the boundary or drop); it needs nothing from #334.
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer Dilemmas 1, 2, 3, 4 and 6 (replay marker placement, replay layer, refusal at the boundary, status for a reused key (asked once, with #3438), header default (asked once, with #3438)) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Re-check ids 230-231 in `Encina.AspNetCore` against the claims of the other plans when PR A starts.
4. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
