# Implementation Plan: ETag and `If-Match` helpers — optimistic concurrency exposed over REST

> **Issue**: [#3443](https://github.com/dlrivada/Encina/issues/3443)
> **Type**: Feature
> **Complexity**: Medium (6 phases, no database provider code, about 30 files, three pull requests)
> **Estimated Scope**: ~700-900 lines of production code + ~1,400-1,800 lines of tests

---

## Summary

Expose the optimistic concurrency that the repositories already enforce to REST clients: derive a strong `ETag` from the entity's concurrency token and return it on reads; answer `If-None-Match` on reads with `304 Not Modified`; require `If-Match` on writes when an endpoint opts in (`428 Precondition Required` when the header is absent, `412 Precondition Failed` when it does not match). Everything is opt-in per endpoint (AGENTS.md section 1, pay-for-what-you-use).

Facts checked against `origin/main` (`18ec8dd19`):

- Two concurrency token shapes exist in `Encina.DomainModeling` (`src/Encina.DomainModeling/IAuditable.cs`): `IConcurrencyAware.RowVersion` (`byte[]?`, line 400; `IConcurrencyAwareEntity` with a setter, line 453) and `IVersioned.Version` (`long`, line 509; `IVersionedEntity` with an `int` setter, line 563).
- The stores are the final arbiter today: `FunctionalRepositoryEF` catches `DbUpdateConcurrencyException`, the ADO and Dapper repositories compare `Version` in the `UPDATE`, and all return `RepositoryErrors.ConcurrencyConflict` (code `Repository.ConcurrencyConflict`, `src/Encina.DomainModeling/RepositoryErrors.cs:36`). That code has no `ErrorCategory` yet: it answers by the legacy suffix inference until the producer slice `P-DomainModeling` of ADR-062 (not this plan) adds its category (`Conflict`, rule R2, HTTP 409).
- ADR-062 (accepted 2026-11-11, on main) replaced the code-to-status table that #3419 first planned: hosts map an `ErrorCategory` set where the error is created to HTTP, gRPC and GraphQL. No category yields 412 or 428 (`Conflict` is 409, `InvalidRequest` is 400, `FailedPrecondition` is 400 and provisional, owned by #3420). The helper's 412 and 428 are therefore produced only by the explicit `statusCode` argument of `ToProblemDetails(error, httpContext, statusCode)` (`src/Encina.AspNetCore/ProblemDetailsExtensions.cs:67`); whether ADR-062 gets categories for them is Dilemma D7.
- `Encina.AspNetCore` has no `IEndpointFilter`, no MVC filter, no `Meter` and no `ActivitySource` yet; its EventId range 200-249 has 200-207 in use. It reaches `Encina.DomainModeling` transitively through `Encina.Security.Audit`, `Encina.Compliance.DPIA` and `Encina.Compliance.DataResidency`.
- The issue lists `ITagGenerator`, `StrongETagGenerator` and `ConcurrencyResult`; the names are settled here (the issue itself says the plan settles them): `IETagGenerator`, `StrongETagGenerator`, `IfMatchOutcome`.

**Standards**: RFC 9110 (ETag, `If-Match`, `If-None-Match`, 304, 412), RFC 6585 (428), RFC 9457 (Problem Details).
**Affected packages**: `Encina.AspNetCore` (HTTP surface), `Encina.DomainModeling` (neutral token pieces, Dilemma D4).
**Provider category**: none. No provider code changes; the feature reads tokens that all repository providers already expose. Integration tests cover the two token shapes (RowVersion and Version) through one EF Core and one Dapper collection.

---

## Design Choices

<details>
<summary><strong>1. How the client's <code>If-Match</code> reaches the write logic (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Explicit**: the endpoint parses `If-Match` into a non-null `IfMatchPrecondition` (`NotSent`, or `Sent(tags)`) that is a property of the command; the handler calls the check helper on the entity it loaded. On an opted-in endpoint `EnsureIfMatch` denies a `NotSent` precondition, so a command whose author forgot to copy the header fails closed instead of skipping the check | Typed, visible in the command contract, testable without HTTP; no hidden channel (the filter stores nothing on `HttpContext.Items`) | Every conditional handler repeats one call |
| **B — Pipeline behavior** reads the tag from `IRequestContext` metadata for requests marked `IConditionalWrite`, and checks the entity | One place; handlers stay unaware | The behavior needs the entity, which only the handler loads; hidden channel; the metadata bag becomes a second contract next to the command |
| **C — Endpoint-level pre-read**: the endpoint loads the entity itself, compares, then sends the command | No handler changes | Bypasses the handler and its pipeline (authorization, validation, tenancy) for a read; two reads per write |

### Chosen Option: **A — Explicit property on the command** (recommended, pending the maintainer)

### Rationale

A wins when the application layer is tested without HTTP, which Encina's request model encourages; the contract is in the command, and `EnsureIfMatch` makes the handler code one line. B wins when many write handlers share exactly the same check and the team accepts a hidden channel; the loaded entity still has to be handed to the behavior, which removes most of its benefit. C wins only for the thinnest services without handlers.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. HTTP status of a conflict the store detects after a matching <code>If-Match</code> (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — 412 when the request carried `If-Match`, otherwise the status of the error's category (`Conflict`, 409)** | The client that used a precondition always gets the status its protocol defines (RFC 9110 section 15.5.13); the race between check and write looks the same as a stale tag | The status depends on request context, not only on the category |
| **B — Always the status of the category** (409) | One rule per category | A conditional client sees 409 in the race and 412 otherwise; two retry paths |
| **C — Always 412** for `Repository.ConcurrencyConflict` | One rule per code | An unconditional update that loses a race is not a failed precondition; wrong for clients without `If-Match` |

### Chosen Option: **A — 412 when `If-Match` was present** (recommended, pending the maintainer)

### Rationale

A wins for clients that implement conditional requests; `ConditionalRequestResults.Problem(error, httpContext)` reads whether `If-Match` is on `httpContext.Request` (no state is stored by the filter) and passes `statusCode: 412` explicitly to `ToProblemDetails`. B wins when the maintainer wants every status category-driven (ADR-062); C is wrong for unconditional writers. The categories themselves are fixed by ADR-062 rules and do not depend on this choice: `encina.concurrency.precondition_failed` is `Conflict` (R2, a version mismatch) and `encina.concurrency.precondition_required` is `InvalidRequest` (R3, a missing header); an ill-formed `If-Match` is `InvalidRequest` too and is answered 400 (RFC 9110: an ill-formed field is a client error), not 412.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Metrics and trace now, or deferred (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Include now**: counters `encina.http.concurrency_conflict` and `encina.http.precondition_missing` (no `_total` suffix, ADR-054 decision 4; the unit lives in the metadata), span `encina.http.etag_validation` (tag `etag.status`; tenant attribute and tag only when multi-tenancy is on, ADR-054 D4) | The issue's own Observability Details and Implementation Tasks list them; no retrofit; first `Meter` and `ActivitySource` of `Encina.AspNetCore` are created with a conscious name | Adds one file and a public naming convention to an assembly that has none |
| **B — Defer**, as the issue's cross-cutting row 2 says | Smaller pull request; the observability convention for `Encina.AspNetCore` can be set once for all its features | Operations cannot see 412 and 428 rates until the follow-up lands; the issue contradicts itself (row 2 deferred, tasks and metric tables included) |

### Chosen Option: **A — Include now** (recommended, pending the maintainer)

### Rationale

A wins because 412 and 428 rates are the operational signal that clients are racing or are not sending the header, and the metric and span names are already specified. B wins if the maintainer prefers to decide the meter name and version of `Encina.AspNetCore` once for the package. The log messages (EventIds 208-211) are included under both options.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the neutral pieces live (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Everything in `Encina.AspNetCore`**, as the issue states | One package; no change elsewhere | A command that carries `EntityTag?` and a handler that calls the check force the application layer to reference ASP.NET Core |
| **B — Neutral pieces in `Encina.DomainModeling`** (`EntityTag`, `IETagGenerator`, `StrongETagGenerator`, the check helper and the error factory, in its `Concurrency/` folder next to `ConcurrencyConflictInfo`); HTTP pieces (parser into `EntityTag`, filters, response helpers, DI) in `Encina.AspNetCore` | The application layer stays HTTP-free; the token interfaces already live there; AspNetCore already references it transitively | Two packages and two `PublicAPI` files change; the issue's Affected Packages list gains DomainModeling |

### Chosen Option: **B — Neutral pieces in `Encina.DomainModeling`** (recommended, pending the maintainer)

### Rationale

B wins in every application that follows Encina's own layering (handlers in an application project that references no ASP.NET Core). A wins for a team that builds endpoints and handlers in one web project and wants one package to read. The issue's Affected Packages already names "Repository abstractions (concurrency tokens)" as a possible package, which B makes explicit.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. ETag derivation: strong, hashed from the concurrency token and the resource identity (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Strong ETag = base64url(SHA-256 over a length-prefixed encoding of: domain separator, resource type, resource id, tenant id (a distinct marker when tenancy is off), token kind, token bytes)** | Per-resource and per-tenant: two resources, or two tenants, with the same `Version` get different tags; the encoding is injective (length prefixes), so no two inputs share a tag by concatenation; opaque (does not reveal how many updates happened); fixed length; defence in depth against a shared cache keyed only by URL | The tag cannot be turned back into a token, so a write handler must load the entity to compare; `IETagGenerator.Create` takes the resource identity; a resource deleted and recreated with the same id and a restarted `Version` gets its old tag back (ABA), which the guide documents (counters must not restart) |
| **B — Hash of the token kind and token bytes only** | Simpler signature | Equal `Version` gives equal tags across resources and tenants, so a tag of one resource matches another; "a tag of tenant A never matches tenant B" is false by construction |
| **C — Encode the raw token** (`"7"`, base64 of `RowVersion`) | Reversible: a detached entity could receive the token directly | Exposes the update counter; same cross-resource equality as B |
| **D — Hash of the representation** | Independent of tokens | A no-op write that bumps the token yields the same tag, a changed token without content change does not match; disagrees with the store |

### Chosen Option: **A — Hashed strong ETag including the resource identity and tenant** (recommended, pending the maintainer)

### Rationale

RFC 9110 section 8.8.3 asks an ETag to identify a version of the representation; the token is that version in Encina, and the resource type and id say which representation. B, C and D also satisfy the RFC, so the choice is the maintainer's; A is the only one that makes the tenant-isolation test true: tenant B's resource, or any other resource, never matches tenant A's tag, while the repository stays the arbiter of access (tenant B's resource is simply not found for tenant A). The issue's own task list says "SHA-256 or similar strong hash". The consequence (the handler loads the entity, compares, and the loaded token then flows to the store, which still arbitrates the race) is stated in Phase 2. Only strong tags are produced; weak tags are accepted when parsing `If-None-Match` (weak comparison, RFC 9110 section 13.1.2) and never match for `If-Match` (strong comparison, section 13.1.1).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Opt-in surface, tenancy and fail-closed rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Opt-in per endpoint** (`RequireIfMatch()` on `RouteHandlerBuilder` and `RouteGroupBuilder`, `[RequireIfMatch]` for MVC, `WriteETag`/`TryNotModified` response helpers); a resource without a token, a missing header on an opted-in endpoint or an unparsable header is never a match | Existing endpoints are untouched; both host styles of the package are served (as `ToProblemDetails` and `ToActionResult` are) | Two thin entry points over one evaluator |
| **B — Global middleware** for every repository-backed endpoint | Zero per-endpoint code | Changes the behavior of every existing application; cannot know which endpoints return which entity; violates "every feature opt-in" |

### Chosen Option: **A — Opt-in per endpoint**

### Rationale

AGENTS.md section 1 makes every feature opt-in (pay-for-what-you-use), which excludes B; `ProblemDetailsExtensions` already sets the precedent of serving both minimal API and MVC. Tenant isolation is the tenant-aware repositories' job (AGENTS.md section 5) and the tag also carries the tenant (D5 option A); an integration test with two tenants proves that tenant B's resource is not found for tenant A and that tags of different tenants and resources differ. A missing `If-Match` on an opted-in endpoint is 428, an ill-formed one is 400, a mismatch is 412 (or 409 by category when D2 is not A), never a pass: a resource with no token cannot satisfy a precondition (fail closed, AGENTS.md section 3).

**Class:** Settled
**Settled by:** AGENTS.md sections 1 and 3; #3443 Alternatives Considered 2; `src/Encina.AspNetCore/ProblemDetailsExtensions.cs:67,161`

</details>

<details>
<summary><strong>7. Categories for 412 and 428 in ADR-062 (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Use the existing categories and pass `statusCode` 412 or 428 explicitly in the HTTP helper** (`precondition_failed` = `Conflict`, `precondition_required` = `InvalidRequest`) | No change to ADR-062 or its tables; HTTP clients get the protocol statuses | gRPC and GraphQL see `ABORTED` and `INVALID_ARGUMENT` / `CONFLICT` and `BAD_REQUEST`, and the explicit status lives in the helper, not in the table |
| **B — Add categories `PreconditionFailed` (412) and `PreconditionRequired` (428) to ADR-062**, one row in the HTTP, gRPC and GraphQL tables, the property tests and the value test | Every protocol maps them; no explicit status needed | Amends an accepted ADR and its tables (an ADR update owned with #3419/#3545 slices); `FailedPrecondition` already exists provisionally (#3420) and may overlap |

### Chosen Option: **A — existing categories plus an explicit status** (recommended, pending the maintainer)

### Rationale

- ADR-062 is additive and owned by #3419; this plan states the missing HTTP statuses (412, 428) rather than deciding the ADR. B wins if the maintainer wants gRPC and GraphQL to answer these two conditions with their own statuses; choosing B changes only the category argument and removes the explicit `statusCode`.

</details>

---

## Implementation Phases

### Phase 1: Neutral core in `Encina.DomainModeling` (`Concurrency/`)

<details>
<summary><strong>Tasks</strong></summary>

1. `EntityTag.cs`: `public readonly record struct EntityTag(string Value, bool IsWeak)`; `Parse(string)`/`TryParse(string, out EntityTag)` for a single quoted tag (`"abc"`, `W/"abc"`), `TryParseList(string, out IReadOnlyList<EntityTag>, out bool isAny)` for `If-Match`/`If-None-Match` values (`*` sets `isAny`), `ToString()` returning the wire form, `StrongEquals(EntityTag)` and `WeakEquals(EntityTag)` (RFC 9110 section 8.8.3.2).
2. `ETagScope.cs` and `IfMatchPrecondition.cs`: `public readonly record struct ETagScope(string ResourceType, string ResourceId, string? TenantId)` (`TenantId` null only when tenancy is off; with tenancy on and no tenant the caller passes no scope and the check denies, see task 4); `public readonly record struct IfMatchPrecondition` with `NotSent`, `Any`, `Sent(IReadOnlyList<EntityTag>)` (`default` is `NotSent`).
3. `IETagGenerator.cs`: `EntityTag? Create(object resource, ETagScope scope)` (null when the resource exposes no token), `EntityTag Create(ReadOnlySpan<byte> rowVersion, ETagScope scope)`, `EntityTag Create(long version, ETagScope scope)`. `StrongETagGenerator.cs`: `public sealed class StrongETagGenerator : IETagGenerator`; reads `IConcurrencyAware.RowVersion` first, then `IVersioned.Version`; SHA-256 over a length-prefixed encoding of the domain separator, resource type, resource id, tenant id (a distinct marker byte when there is none), a token-kind byte and the token bytes (injective: a test feeds colliding concatenations); base64url without padding; empty `RowVersion` counts as no token.
4. `ConcurrencyPreconditionErrors.cs` and `ConcurrencyPreconditions.cs`: codes `encina.concurrency.precondition_failed` (`ErrorCategory.Conflict`, R2) and `encina.concurrency.precondition_required` (`ErrorCategory.InvalidRequest`, R3), built through the categorized `EncinaErrors.Create(code, category, ...)` (ADR-062; the package is outside the pending list for these new codes); two methods, both `Either<EncinaError, Unit>`: `EnsureIfMatch(this IETagGenerator generator, IfMatchPrecondition precondition, object resource, ETagScope scope)` for an opted-in write: `NotSent` is `Left` (`precondition_required`, so a command that forgot to carry the header fails closed), `Any` is `Right` when the resource has a token, a strong tag equal to the resource's is `Right`, otherwise `Left` (`precondition_failed`, details: resource type name only); and `EnsureIfMatchWhenSent(...)` for a write whose precondition is optional: `NotSent` is `Right`, otherwise as above. A resource with no token never satisfies a precondition. With tenancy on, a missing tenant means no `ETagScope` can be built, and the caller returns `Left` (`InvalidRequest`).
5. `PublicAPI.Unshipped.txt` lines (`mechanical-fixer`); the new types are public, so XML docs name the ADR-062 category of each code.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Issue #3443 exposes optimistic concurrency over REST. Decisions D1 (explicit property on the command), D2 (412 when If-Match was present), D4 (neutral pieces in Encina.DomainModeling), D5 (tag includes resource identity and tenant) and D7 (existing ADR-062 categories plus an explicit status) are recommended, pending the maintainer. The token interfaces are IConcurrencyAware (RowVersion) and IVersioned (Version) in src/Encina.DomainModeling/IAuditable.cs.
TASK: Add EntityTag, IETagGenerator, StrongETagGenerator, the precondition error factory and EnsureIfMatch under src/Encina.DomainModeling/Concurrency/.
KEY RULES: Either<EncinaError, Unit> for the check, never exceptions; no HTTP types in this package; SHA-256 via System.Security.Cryptography, no allocation beyond the string on the hot path where practical; weak tags never satisfy If-Match; null token means no match; XML docs with examples; no logging here.
REFERENCE FILES: src/Encina.DomainModeling/IAuditable.cs, src/Encina.DomainModeling/RepositoryErrors.cs, src/Encina.DomainModeling/Concurrency/ConcurrencyConflictInfo.cs
```

</details>

### Phase 2: HTTP surface in `Encina.AspNetCore` (`ConditionalRequests/`)

<details>
<summary><strong>Tasks</strong></summary>

1. `IfMatchOutcome.cs`: `enum IfMatchOutcome { Match, Mismatch, Missing, Invalid }`.
2. `ConditionalRequestEvaluator.cs`: `static IfMatchOutcome EvaluateIfMatch(HttpRequest, EntityTag? current)` (no header: `Missing`; an ill-formed header: `Invalid`, answered 400 and never a pass; `*` with a token: `Match`; a strong tag equal to `current`: `Match`; otherwise `Mismatch`) and `static bool IsNotModified(HttpRequest, EntityTag current)` (`GET`/`HEAD` only, weak comparison, `*` never matches a missing resource). Evaluation order follows RFC 9110 section 13.2.2: `If-Match` before `If-None-Match`; an `If-None-Match` match on an unsafe method is `412`.
3. `ConditionalRequestErrors.cs`: re-exports the DomainModeling codes for hosts and builds `PreconditionRequired(HttpRequest)` details (method and route template only, never header values); `ErrorCategory` per code as in Phase 1 task 4, and an `InvalidRequest` code for an ill-formed header.
4. `IfMatchEndpointFilter.cs`: `IEndpointFilter` that answers `428` when the header is absent and `400` when it is ill-formed; it stores nothing on `HttpContext.Items`. The endpoint obtains the typed `IfMatchPrecondition` with `HttpContext.ParseIfMatch()` and puts it in the command (D1); `ConditionalRequestResults.Problem` re-reads the header from the request to apply D2, so there is no hidden channel.
5. `ConditionalRequestEndpointExtensions.cs`: `RequireIfMatch(this RouteHandlerBuilder)` and `RequireIfMatch(this RouteGroupBuilder)`.
6. `RequireIfMatchAttribute.cs`: MVC action filter with the same behavior.
7. `ETagResponseExtensions.cs`: `HttpResponse.WriteETag(EntityTag)`, `HttpContext.TryNotModified(EntityTag)` (writes `ETag`, answers 304 and repeats the tag), `HttpContext.ParseIfMatch()` returning the non-null `IfMatchPrecondition`.
8. `ConditionalRequestResults.cs`: `Problem(EncinaError, HttpContext)` applying D2 through `ToProblemDetails(error, httpContext, statusCode)`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 delivered EntityTag, IETagGenerator and EnsureIfMatch in Encina.DomainModeling. Encina.AspNetCore has no endpoint or MVC filters yet; ProblemDetailsExtensions.ToProblemDetails accepts an explicit status.
TASK: Implement the evaluator, the minimal-API endpoint filter, the MVC filter, the response helpers and the result helper under src/Encina.AspNetCore/ConditionalRequests/.
KEY RULES: opt-in per endpoint only; absent If-Match on an opted-in endpoint is 428; an unparsable or mismatching header is never a pass; never log or return header values; 304 repeats the ETag and has no body; weak comparison only for If-None-Match; both host styles share one evaluator.
REFERENCE FILES: src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.AspNetCore/DPIAEndpointExtensions.cs, src/Encina.AspNetCore/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary><strong>Tasks</strong></summary>

1. `ServiceCollectionExtensions.AddEncinaConditionalRequests(this IServiceCollection)`: `TryAddSingleton<IETagGenerator, StrongETagGenerator>()`; nothing else is registered, so an unused feature costs nothing.
2. DI test: provider built with `ValidateOnBuild` and `ValidateScopes` resolves `IETagGenerator` and an application override registered before the call wins.
3. README section for registration and the three entry points.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: The feature has no options; the only service is IETagGenerator.
TASK: Add AddEncinaConditionalRequests with TryAdd and the DI completeness test.
KEY RULES: the application's own IETagGenerator registration wins in any order; no options class unless a setting appears; nothing registered by AddEncinaAspNetCore.
REFERENCE FILES: src/Encina.AspNetCore/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/AspNetCore/AspNetCoreIdentityRegistrationTests.cs
```

</details>

### Phase 4: Observability and cross-cutting

<details>
<summary><strong>Tasks</strong></summary>

1. `ConditionalRequestLog.cs` (`[LoggerMessage]` partial, EventIds inside the registered `EventIdRanges.AspNetCore` range 200-249): `208 PreconditionRequired` (warning), `209 PreconditionFailed` (warning), `210 NotModified` (debug), `211 TokenUnavailable` (warning: the opted-in resource exposes no token). Arguments are the route template and method only.
2. `ConditionalRequestDiagnostics.cs` (D3 option A): `Meter` and `ActivitySource` named `Encina.AspNetCore`, counters `encina.http.concurrency_conflict` and `encina.http.precondition_missing` (no `_total` suffix: ADR-054 decision 4), span `encina.http.etag_validation` with tag `etag.status` (`match`, `mismatch`, `missing`, `invalid`, `not_modified`). Per ADR-054 D4 the tenant id is a span attribute and a metric tag only when multi-tenancy is on, and no tenant tag is emitted when it is off (the cardinality risk is documented by #906); logs never carry the tenant id.
3. `Encina.AspNetCore` is already mapped to `EventIdRanges.AspNetCore` in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; run that test to verify 208-211 stay inside the range.
4. Transactions: the check runs in the handler on the entity it loaded inside the unit of work; the handler and the orchestrator never call `SaveChangesAsync` (the unit of work does). The compared token is the loaded entity's original value, which is the value EF Core's `RowVersion` check and the ADO and Dapper `UPDATE ... WHERE Version` arbitrate; a handler that builds a detached entity from the request bypasses both, and the guide says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Encina.AspNetCore owns EventIds 200-249 (200-207 used) and has no Meter or ActivitySource.
TASK: Add ConditionalRequestLog (208-211) and, if D3 is answered include, ConditionalRequestDiagnostics.
KEY RULES: never log header values, entity ids in messages or EncinaError.Message; sequential EventIds; XML docs name the range; counters are Counter<long> with tags; no new EventId range.
REFERENCE FILES: src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs, src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Refit/Diagnostics/OutboundTelemetry.cs
```

</details>

### Phase 5: Testing — 7 test types

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/DomainModeling/Concurrency/`, `.../AspNetCore/ConditionalRequests/`): `EntityTag` parsing and comparison (strong, weak, list, `*`, quotes, whitespace), generator for both token shapes and for a resource with no token, `EnsureIfMatch` outcomes, evaluator outcomes and RFC order, filters (428, 400 for an ill-formed header, pass-through, no `HttpContext.Items` written), `EnsureIfMatch` with `NotSent` denies and `EnsureIfMatchWhenSent` with `NotSent` passes, generator injectivity (colliding concatenations of type, id and tenant), response helpers (200 with `ETag`, 304 with the tag, no body), diagnostics (instrument names without `_total`, tenant tag only when tenancy is on).
2. **Guard**: every new public method with parameters.
3. **Contract**: `IETagGenerator` (the real generator keeps: same token gives the same tag, different tokens differ), the 428/412/304 response shape.
4. **Property** (FsCheck): tag stability for equal tokens, difference for different tokens, parse/print round trip, a weak tag never satisfies `If-Match`.
5. **Integration**: ASP.NET Core test host with minimal API endpoints and real repositories: `[Collection("EFCore-SqlServer")]` (RowVersion) and `[Collection("Dapper-PostgreSQL")]` (Version): GET returns the tag, conditional GET returns 304, two concurrent PUTs with the same tag produce one success and one 412, a PUT without the header is 428, an ill-formed header is 400, tenant B's resource is not found for tenant A, and the tags of two resources (or two tenants) with the same `Version` differ.
6. **Load / Benchmark**: `.md` justification files (the cost is one SHA-256 over a few bytes; concurrency control is not a throughput bottleneck, as the issue states).
7. Per-file targets for `.github/coverage-manifest/Encina.DomainModeling.json` and `Encina.AspNetCore.json` (`mechanical-fixer` writes them, never lower than existing; `-` = flag not applicable; each PR reports the measured values of its own files; the integration column is met only once PR 4 lands):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| `Concurrency/EntityTag.cs` | 95 | 90 | - | 90 | 80 | Pure parsing and comparison, all branches reachable without HTTP; property tests cover the parse/print round trip. |
| `Concurrency/ETagScope.cs`, `IfMatchPrecondition.cs` | 90 | 80 | - | - | 70 | Small value types; guard covers the null list; the integration host proves the real header flow. |
| `Concurrency/IETagGenerator.cs` | 0 | 0 | 90 | - | - | An interface with no executable code; the contract test runs the real generator against it. |
| `Concurrency/StrongETagGenerator.cs` | 95 | 90 | 90 | 90 | 80 | The tag derivation (token kinds, injective encoding, no token) is pure; property tests cover stability and difference. |
| `Concurrency/ConcurrencyPreconditionErrors.cs` | 95 | 90 | - | - | - | Error factories with fixed code and category; unit and guard tests cover each. |
| `Concurrency/ConcurrencyPreconditions.cs` | 95 | 90 | 85 | 85 | 80 | The check outcomes (`NotSent`, `Any`, match, mismatch, no token) are pure and fully testable; the integration flag runs them against real tokens. |
| `ConditionalRequests/IfMatchOutcome.cs` | 0 | 0 | - | - | - | An enum with no executable code. |
| `ConditionalRequests/ConditionalRequestEvaluator.cs` | 95 | 90 | 85 | - | 80 | RFC 9110 evaluation order over a mocked request; the contract test pins the 304/412/428 shapes. |
| `ConditionalRequests/ConditionalRequestErrors.cs` | 90 | 85 | - | - | - | Error and details builders with fixed code and category. |
| `ConditionalRequests/ETagResponseExtensions.cs` | 95 | 90 | 85 | - | 80 | Response helpers over `DefaultHttpContext`; the integration host proves real 304 and tag headers. |
| `ConditionalRequests/ConditionalRequestResults.cs` | 90 | 85 | - | - | 75 | Applies D2 through `ToProblemDetails`; reachable with a fake context. |
| `ConditionalRequests/IfMatchEndpointFilter.cs` | 90 | 85 | - | - | 80 | 428 and 400 paths and pass-through are unit testable with a fake invocation context. |
| `ConditionalRequests/ConditionalRequestEndpointExtensions.cs` | 85 | 85 | - | - | 75 | Thin registration of the filter on route builders. |
| `ConditionalRequests/RequireIfMatchAttribute.cs` | 90 | 85 | 85 | - | 70 | The MVC twin of the endpoint filter, same outcomes. |
| `ConditionalRequests/ConditionalRequestDiagnostics.cs` | 90 | 85 | - | - | - | Counters and span with a tenant tag only when tenancy is on; both branches testable with a meter listener. |
| `ConditionalRequests/ConditionalRequestLog.cs` | 0 | 0 | - | - | - | Source-generated `[LoggerMessage]` partial methods with no hand-written code. |
| `ServiceCollectionExtensions.cs` (the `AddEncinaConditionalRequests` change) | its current target, never lowered | 90 | - | - | - | One `TryAdd`; the DI test and null guards cover it. |

The CRAP table of the changed methods (comp, cov, CRAP <= 10) is reported with each PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Phases 1-4 add the neutral pieces to Encina.DomainModeling and the HTTP surface to Encina.AspNetCore.
TASK: Write unit, guard, contract, property and integration tests and the load/benchmark justification files; measure per-flag coverage of every touched file.
KEY RULES: Shouldly via Encina.Testing.Shouldly; integration tests use shared [Collection] fixtures and ClearAllDataAsync in InitializeAsync; builders for entities; no Thread.Sleep (use a barrier for the concurrent PUT test); tests execute real code; run the CRAP gate table locally.
REFERENCE FILES: tests/Encina.UnitTests/AspNetCore/, tests/Encina.IntegrationTests/Dapper/Collections.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Collections.cs, .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

### Phase 6: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples on every public API (the `PublicAPI.Unshipped.txt` lines ship in the PR that adds each symbol, Phase 1 task 5, and are not repeated here).
2. `src/Encina.AspNetCore/README.md` section; guide `docs/en/guides/rest-optimistic-concurrency.md` explaining ETag, `If-Match`, 304, 412 and 428 and the handler pattern (`docs-writer`); a link from the OpenAPI documentation work (#2514) once it exists.
3. Changelog fragment `changelog.d/3443-etag-conditional-requests.added.md` (`mechanical-fixer`); `docs/INVENTORY.md` rows; ROADMAP check.
4. Build and test verification: `dotnet build --configuration Release` with 0 warnings; every flag reaches its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: The feature is implemented and tested; documentation and records remain.
TASK: Write the README section and guide through docs-writer; add the changelog fragment, PublicAPI lines and manifest entries through mechanical-fixer.
KEY RULES: never type coverage figures by hand (covref markers); documentation in English; the guide shows the handler pattern with EnsureIfMatch and the status table 304/412/428; link RFC 9110 and RFC 6585.
REFERENCE FILES: src/Encina.AspNetCore/README.md, changelog.d/README.md, docs/INVENTORY.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Reference | Relevance to this feature |
|---|---|---|
| RFC 9110 section 8.8.3 | ETag | Syntax and meaning of an entity tag |
| RFC 9110 section 8.8.3.2 | Strong and weak comparison | `If-Match` uses strong comparison, `If-None-Match` weak |
| RFC 9110 sections 13.1.1, 13.1.2, 13.2.2 | `If-Match`, `If-None-Match`, evaluation order | The evaluator's rules |
| RFC 9110 sections 15.4.5, 15.5.13 | 304 Not Modified, 412 Precondition Failed | Response statuses |
| RFC 6585 section 3 | 428 Precondition Required | Response status when the header is absent |
| RFC 9457 | Problem Details for HTTP APIs | Body of 412 and 428 answers |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|---|---|---|
| `IConcurrencyAware`, `IConcurrencyAwareEntity` | `src/Encina.DomainModeling/IAuditable.cs` | Token source 1 (`RowVersion`) |
| `IVersioned`, `IVersionedEntity` | `src/Encina.DomainModeling/IAuditable.cs` | Token source 2 (`Version`) |
| `RepositoryErrors.ConcurrencyConflict`, `NotFound` | `src/Encina.DomainModeling/RepositoryErrors.cs` | The persistence-side conflict that D2 maps to 412 |
| `ConcurrencyConflictInfo<TEntity>` | `src/Encina.DomainModeling/Concurrency/ConcurrencyConflictInfo.cs` | Folder and precedent for the neutral pieces |
| `ProblemDetailsExtensions` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Renders 412 and 428 with an explicit status (ADR-062 has no category for them, D7) |
| `DPIAEndpointExtensions` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Pattern for endpoint extension methods |
| `EncinaContextMiddlewareLog` | `src/Encina.AspNetCore/EncinaContextMiddlewareLog.cs` | Pattern for `[LoggerMessage]` classes in the range |
| `AddEncinaAspNetCore` | `src/Encina.AspNetCore/ServiceCollectionExtensions.cs` | Neighbour of `AddEncinaConditionalRequests` |
| `FunctionalRepositoryEF` and the ADO and Dapper repositories | `src/Encina.EntityFrameworkCore/Repository/`, `src/Encina.ADO.*/Repository/` | The store remains the arbiter of the write race |
| Tenant-aware repositories | e.g. `src/Encina.ADO.MySQL/Tenancy/TenantAwareFunctionalRepositoryADO.cs` | Tenant isolation the tag relies on |

### Event ID Allocation

| Package | Range | Notes |
|---|---|---|
| `Encina.AspNetCore` | 208-211 | Inside the registered `EventIdRanges.AspNetCore` (200-249); 200-207 used; 212-249 stay free; no new range |
| `Encina.DomainModeling` | none | The neutral pieces do not log |

### Estimated File Count

| Category | Files | Notes |
|---|---|---|
| Production, `Encina.DomainModeling/Concurrency/` (new) | 7 | `EntityTag`, `ETagScope`, `IfMatchPrecondition`, `IETagGenerator`, `StrongETagGenerator`, `ConcurrencyPreconditionErrors`, `ConcurrencyPreconditions` |
| Production, `Encina.AspNetCore/ConditionalRequests/` (new) | 10 | `IfMatchOutcome`, `ConditionalRequestEvaluator`, `ConditionalRequestErrors`, `IfMatchEndpointFilter`, `ConditionalRequestEndpointExtensions`, `RequireIfMatchAttribute`, `ETagResponseExtensions`, `ConditionalRequestResults`, `ConditionalRequestLog`, `ConditionalRequestDiagnostics` (D3 option A) |
| Production (modified) | 4 | `ServiceCollectionExtensions.cs`, two `PublicAPI.Unshipped.txt`, `README.md` (counted once; Phase 6 does not repeat them) |
| Tests | 22 | About 10 unit, 3 guard, 2 contract, 2 property, 2 integration, 1 DI, 2 load and benchmark justification `.md` |
| Documentation and records | 5 | Guide, changelog fragment, two coverage manifests, INVENTORY row |

Total about 48 files and more than 1,500 lines: above one worker pull request. Each PR counts its code, tests, `PublicAPI` lines, manifest rows and test-project edits, builds and passes alone, and stays within 15 files and 1,500 lines (the targets of a file ship with the file):

| PR | Content | Files |
|----|---------|-------|
| 1 | `Encina.DomainModeling` Phase 1: 7 production files, `PublicAPI.Unshipped.txt`, `Encina.DomainModeling.json` manifest, unit tests for `EntityTag`, the generator and the preconditions (3), guard, property and contract tests (3 more) | 15 |
| 2 | `Encina.AspNetCore` evaluator and response helpers: `IfMatchOutcome`, `ConditionalRequestEvaluator`, `ConditionalRequestErrors`, `ETagResponseExtensions`, `ConditionalRequestResults`, `PublicAPI.Unshipped.txt`, manifest, evaluator, response, results unit tests and a guard test | 11 |
| 3 | Filters, DI, log and diagnostics: `IfMatchEndpointFilter`, `ConditionalRequestEndpointExtensions`, `RequireIfMatchAttribute`, `ServiceCollectionExtensions.cs`, `ConditionalRequestLog`, `ConditionalRequestDiagnostics`, `PublicAPI.Unshipped.txt`, manifest, filter, extensions, diagnostics unit tests and the DI test | 14 |
| 4 | Integration tests (two collections and one host helper), the two load and benchmark justification `.md`, contract test for the 304/412/428 shape | about 6 |
| 5 | Documentation: README section, guide, changelog fragment, INVENTORY row | 4 |

The integration-flag targets of PR 1 to 3 files are met only by PR 4; each of those PRs reports its other flags and states the integration flag as pending PR 4.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError,T>). Issue #3443 exposes the repositories' optimistic concurrency over REST with ETag, If-Match and If-None-Match. Dilemmas D1 (explicit IfMatchPrecondition on the command), D2 (412 when If-Match was present), D3 (observability now), D4 (neutral pieces in Encina.DomainModeling), D5 (tag includes resource identity and tenant) and D7 (existing ADR-062 categories plus an explicit 412/428) are recommended and pending the maintainer: do not start before they are answered. ADR-062 maps ErrorCategory to HTTP; the new codes are categorized (precondition_failed Conflict, precondition_required InvalidRequest) and the helper passes 412 and 428 explicitly.
IMPLEMENTATION OVERVIEW: Phase 1 EntityTag, IETagGenerator, StrongETagGenerator, EnsureIfMatch in Encina.DomainModeling. Phase 2 evaluator, endpoint filter, MVC filter, response helpers in Encina.AspNetCore. Phase 3 AddEncinaConditionalRequests. Phase 4 EventIds 208-211, counters and span. Phase 5 tests in 7 types (RowVersion through EF Core, Version through Dapper). Phase 6 documentation.
KEY PATTERNS: opt-in per endpoint; hashed strong ETag over resource type, id, tenant and token (injective encoding); a resource without a token never satisfies a precondition; the handler loads the entity, compares, and the store still arbitrates the race; no header values or EncinaError.Message in logs; TryAdd registration proven by a ValidateOnBuild/ValidateScopes test; CRAP <= 10; Shouldly via Encina.Testing.Shouldly; PublicAPI, changelog and manifests through mechanical-fixer; documentation through docs-writer.
REFERENCE FILES: src/Encina.DomainModeling/IAuditable.cs, src/Encina.DomainModeling/RepositoryErrors.cs, src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.AspNetCore/DPIAEndpointExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Concurrency tokens are not server-side response caching (#712); the ETag only enables client and proxy revalidation. |
| 2 | OpenTelemetry | ✅ | Counters for 412 and 428 (no `_total`) and the `encina.http.etag_validation` span, tenant tag only when tenancy is on (ADR-054 D4) (Phase 4), subject to Dilemma D3; the issue's row 2 says deferred. |
| 3 | Structured Logging | ✅ | EventIds 208-211 inside the registered `AspNetCore` range, route template and method only (Phase 4). |
| 4 | Health Checks | ❌ | A precondition outcome says nothing about the health of a dependency. |
| 5 | Validation | ✅ | The `If-Match` header is parsed and validated by `EntityTag.TryParseList` in the endpoint filter; an unparsable value is never a pass. |
| 6 | Resilience | ❌ | A conflict is a deterministic outcome, not a transient failure. |
| 7 | Distributed Locks | ❌ | Optimistic concurrency is the alternative to a lock. |
| 8 | Transactions | ✅ | The handler's check runs on the entity it loaded inside the unit of work (which alone calls `SaveChangesAsync`), so the loaded token is the one the store compares; a detached entity built from the request bypasses it (Phase 4, task 4). |
| 9 | Idempotency | ⏭️ | ETag handling complements idempotency keys but is a different mechanism; the inbox behavior is unchanged. Deferred, no issue needed until a write path combines both. |
| 10 | Multi-Tenancy | ✅ | The tag is derived from the resource type, id and tenant (D5 option A); tenant-aware repositories isolate the entity; with tenancy on and no tenant the check denies; a two-tenant integration test proves tenant B's resource is not found for tenant A and tags differ (Phase 5). |
| 11 | Module Isolation | ❌ | HTTP concurrency control is cross-module. |
| 12 | Audit Trail | ❌ | Version conflict detection is not an audit event; who changed what stays with the entity-change audit store. |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|---|---|---|
| ADR-062 (error category contract, accepted 2026-11-11) and its implementation #3545 | ADR accepted | Replaces the status table #3419 planned: the new codes are categorized at the call site and the categorized `EncinaErrors.Create` overload must be on main; 412 and 428 are passed explicitly, and D7 asks whether ADR-062 gets categories for them. The category of `Repository.ConcurrencyConflict` belongs to slice `P-DomainModeling`, not to this plan |
| #3420, #3426 (denial statuses, exposure of error codes) | Open, `needs-decision` | Not blocking: the helper passes the status explicitly and its details carry no internal code beyond what #3426 decides |
| #287 optimistic concurrency abstractions | Closed | Provides the token interfaces this plan reads |
| #2514 OpenAPI documentation of 412/428 | Open | The guide links to it once it exists |

---

## Next Steps

1. The maintainer answers D1 to D5 and D7 when work on #3443 starts; the answers go into `## Maintainer Decisions`.
2. Confirm that the categorized `EncinaErrors.Create` overload of ADR-062 is on main (#3545); if D7 is answered B, the ADR-062 category change goes first.
3. Brief PR 1 (Phase 1) first; it is independent of HTTP.
4. Re-check the 12-function row 2 against D3 and update the issue text.
