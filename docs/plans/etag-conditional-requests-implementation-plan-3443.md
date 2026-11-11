# Implementation Plan: ETag and `If-Match` helpers — optimistic concurrency exposed over REST

> **Issue**: [#3443](https://github.com/dlrivada/Encina/issues/3443)
> **Type**: Feature
> **Complexity**: Medium (6 phases, no database provider code, about 30 files, three pull requests)
> **Estimated Scope**: ~700-900 lines of production code + ~1,400-1,800 lines of tests

---

## Summary

Expose the optimistic concurrency that the repositories already enforce to REST clients: derive a strong `ETag` from the entity's concurrency token and return it on reads; answer `If-None-Match` on reads with `304 Not Modified`; require `If-Match` on writes when an endpoint opts in (`428 Precondition Required` when the header is absent, `412 Precondition Failed` when it does not match). Everything is opt-in per endpoint (AGENTS.md section 1, pay-for-what-you-use).

Facts checked against `origin/main`:

- Two concurrency token shapes exist in `Encina.DomainModeling` (`src/Encina.DomainModeling/IAuditable.cs`): `IConcurrencyAware.RowVersion` (`byte[]?`, line 400; `IConcurrencyAwareEntity` with a setter, line 453) and `IVersioned.Version` (`long`, line 509; `IVersionedEntity` with an `int` setter, line 563).
- The stores are the final arbiter today: `FunctionalRepositoryEF` catches `DbUpdateConcurrencyException`, the ADO and Dapper repositories compare `Version` in the `UPDATE`, and all return `RepositoryErrors.ConcurrencyConflict` (code `Repository.ConcurrencyConflict`, `src/Encina.DomainModeling/RepositoryErrors.cs:36`). That code answers HTTP 500 today because the status mapping is a suffix match (#3419).
- `ProblemDetailsExtensions.ToProblemDetails(error, httpContext, statusCode)` accepts an explicit status (`src/Encina.AspNetCore/ProblemDetailsExtensions.cs:67`), so the helper's 412 and 428 do not depend on the code-to-status table; the table entries are the contract #3419 owes (the issue's blocker).
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
| **A — Explicit**: the endpoint parses `If-Match` into an `EntityTag?` that is a property of the command; the handler calls the check helper on the entity it loaded | Typed, visible in the command contract, testable without HTTP; no hidden channel | Every conditional handler repeats one call |
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
| **A — 412 when the request carried `If-Match`, otherwise the table's status for `Repository.ConcurrencyConflict`** | The client that used a precondition always gets the status its protocol defines (RFC 9110 section 15.5.13); the race between check and write looks the same as a stale tag | The status depends on request context, not only on the error code |
| **B — Always the table's status** (409 per #3419) | One rule per error code | A conditional client sees 409 in the race and 412 otherwise; two retry paths |
| **C — Always 412** for `Repository.ConcurrencyConflict` | One rule per error code | An unconditional update that loses a race is not a failed precondition; wrong for clients without `If-Match` |

### Chosen Option: **A — 412 when `If-Match` was present** (recommended, pending the maintainer)

### Rationale

A wins for clients that implement conditional requests; the endpoint knows whether the header was present (the filter records it), so `ConditionalRequestResults.Problem(error, httpContext)` passes `statusCode: 412` explicitly. B wins when the maintainer wants the mapping fully table-driven (#3419 decides the mechanism, D-7 in that issue); C is wrong for unconditional writers. The choice reads the table decision of #3419, so it is confirmed when work starts.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Metrics and trace now, or deferred (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Include now**: counters `encina.http.concurrency_conflict_total` and `encina.http.precondition_missing_total`, span `encina.http.etag_validation` (tag `etag.status`) | The issue's own Observability Details and Implementation Tasks list them; no retrofit; first `Meter` and `ActivitySource` of `Encina.AspNetCore` are created with a conscious name | Adds one file and a public naming convention to an assembly that has none |
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
<summary><strong>5. ETag derivation: strong, hashed from the concurrency token</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Strong ETag = base64url(SHA-256(domain separator, token kind, token bytes))** | Aligns the HTTP precondition exactly with the persistence conflict detection; opaque (does not reveal how many updates happened); fixed length | The tag cannot be turned back into a token, so a write handler must load the entity to compare (no blind update from a detached entity) |
| **B — Encode the raw token** (`"7"`, base64 of `RowVersion`) | Reversible: a detached entity could receive the token directly | Exposes the update counter; couples the wire format to the token type |
| **C — Hash of the representation** | Independent of tokens | A no-op write that bumps the token yields the same tag, a changed token without content change does not match; disagrees with the store |

### Chosen Option: **A — Hashed strong ETag**

### Rationale

RFC 9110 section 8.8.3 asks an ETag to identify a version of the representation; the token is that version in Encina. The issue's own task list says "SHA-256 or similar strong hash". The consequence (the handler loads the entity, compares, and the loaded token then flows to the store, which still arbitrates the race) is stated in Phase 2. Only strong tags are produced; weak tags are accepted when parsing `If-None-Match` (weak comparison, RFC 9110 section 13.1.2) and never match for `If-Match` (strong comparison, section 13.1.1).

**Class:** Settled
**Settled by:** RFC 9110 sections 8.8.3, 13.1.1 and 13.1.2; #3443 Alternatives Considered 1 and Implementation Tasks

</details>

<details>
<summary><strong>6. Opt-in surface, tenancy and fail-closed rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Opt-in per endpoint** (`RequireIfMatch()` on `RouteHandlerBuilder` and `RouteGroupBuilder`, `[RequireIfMatch]` for MVC, `WriteETag`/`TryNotModified` response helpers); no tenant in the tag; a resource without a token or an unparsable header is never a match | Existing endpoints are untouched; both host styles of the package are served (as `ToProblemDetails` and `ToActionResult` are); tenant isolation stays the repository's | Two thin entry points over one evaluator |
| **B — Global middleware** for every repository-backed endpoint | Zero per-endpoint code | Changes the behavior of every existing application; cannot know which endpoints return which entity |
| **C — Tenant id inside the tag** | Defence in depth against a shared cache keyed only by URL | A tag leaks nothing across tenants because the compared token is that of the tenant-isolated entity; adds a dependency on `ITenantContext` in the generator |

### Chosen Option: **A — Opt-in per endpoint, repository isolation for tenants**

### Rationale

AGENTS.md section 1 makes every feature opt-in, and `ProblemDetailsExtensions` already sets the precedent of serving both minimal API and MVC. Tenant isolation is the tenant-aware repositories' job (AGENTS.md section 5, multi-tenancy is required for the database providers); an integration test with two tenants proves that a tag of one tenant never matches the other's resource. Missing or unparsable `If-Match` on an opted-in endpoint is `428`/`400`-class handling, never a pass: a resource with no token cannot satisfy a precondition (fail closed, AGENTS.md section 3).

**Class:** Settled
**Settled by:** AGENTS.md sections 1, 3 and 5; #3443 Alternatives Considered 2; `src/Encina.AspNetCore/ProblemDetailsExtensions.cs:67,161`

</details>

---

## Implementation Phases

### Phase 1: Neutral core in `Encina.DomainModeling` (`Concurrency/`)

<details>
<summary><strong>Tasks</strong></summary>

1. `EntityTag.cs`: `public readonly record struct EntityTag(string Value, bool IsWeak)`; `Parse(string)`/`TryParse(string, out EntityTag)` for a single quoted tag (`"abc"`, `W/"abc"`), `TryParseList(string, out IReadOnlyList<EntityTag>, out bool isAny)` for `If-Match`/`If-None-Match` values (`*` sets `isAny`), `ToString()` returning the wire form, `StrongEquals(EntityTag)` and `WeakEquals(EntityTag)` (RFC 9110 section 8.8.3.2).
2. `IETagGenerator.cs`: `EntityTag? Create(object resource)` (null when the resource exposes no token), `EntityTag Create(ReadOnlySpan<byte> rowVersion)`, `EntityTag Create(long version)`.
3. `StrongETagGenerator.cs`: `public sealed class StrongETagGenerator : IETagGenerator`; reads `IConcurrencyAware.RowVersion` first, then `IVersioned.Version`; SHA-256 over a fixed domain separator, a token-kind byte and the token bytes; base64url without padding; empty `RowVersion` counts as no token.
4. `ConcurrencyPreconditionErrors.cs` and `ConcurrencyPreconditions.cs`: codes `encina.concurrency.precondition_failed` and `encina.concurrency.precondition_required`; `Either<EncinaError, Unit> ConcurrencyPreconditions.EnsureIfMatch(this IETagGenerator generator, EntityTag? ifMatch, object resource)`: `Right` when `ifMatch` is null (no precondition was sent; the endpoint filter enforces presence), `Right` when it is `*` and the resource has a token, `Right` when a strong tag equals the resource's tag, otherwise `Left` (`precondition_failed`, details: resource type name only).
5. `PublicAPI.Unshipped.txt` lines (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Issue #3443 exposes optimistic concurrency over REST. Decisions D1 (explicit property on the command), D2 (412 when If-Match was present) and D4 (neutral pieces in Encina.DomainModeling) are recommended, pending the maintainer. The token interfaces are IConcurrencyAware (RowVersion) and IVersioned (Version) in src/Encina.DomainModeling/IAuditable.cs.
TASK: Add EntityTag, IETagGenerator, StrongETagGenerator, the precondition error factory and EnsureIfMatch under src/Encina.DomainModeling/Concurrency/.
KEY RULES: Either<EncinaError, Unit> for the check, never exceptions; no HTTP types in this package; SHA-256 via System.Security.Cryptography, no allocation beyond the string on the hot path where practical; weak tags never satisfy If-Match; null token means no match; XML docs with examples; no logging here.
REFERENCE FILES: src/Encina.DomainModeling/IAuditable.cs, src/Encina.DomainModeling/RepositoryErrors.cs, src/Encina.DomainModeling/Concurrency/ConcurrencyConflictInfo.cs
```

</details>

### Phase 2: HTTP surface in `Encina.AspNetCore` (`ConditionalRequests/`)

<details>
<summary><strong>Tasks</strong></summary>

1. `IfMatchOutcome.cs`: `enum IfMatchOutcome { Match, Mismatch, Missing }`.
2. `ConditionalRequestEvaluator.cs`: `static IfMatchOutcome EvaluateIfMatch(HttpRequest, EntityTag? current)` (no header: `Missing`; `*` with a token: `Match`; a strong tag equal to `current`: `Match`; otherwise `Mismatch`; an unparsable header is `Mismatch`, never a pass) and `static bool IsNotModified(HttpRequest, EntityTag current)` (`GET`/`HEAD` only, weak comparison, `*` never matches a missing resource). Evaluation order follows RFC 9110 section 13.2.2: `If-Match` before `If-None-Match`; an `If-None-Match` match on an unsafe method is `412`.
3. `ConditionalRequestErrors.cs`: re-exports the DomainModeling codes for hosts and builds `PreconditionRequired(HttpRequest)` details (method and route template only, never header values).
4. `IfMatchEndpointFilter.cs`: `IEndpointFilter` that answers `428` when the header is absent and records on `HttpContext.Items` that `If-Match` was present (for D2); the parsed `EntityTag?` is placed on `HttpContext.Items` for the endpoint to copy into the command.
5. `ConditionalRequestEndpointExtensions.cs`: `RequireIfMatch(this RouteHandlerBuilder)` and `RequireIfMatch(this RouteGroupBuilder)`.
6. `RequireIfMatchAttribute.cs`: MVC action filter with the same behavior.
7. `ETagResponseExtensions.cs`: `HttpResponse.WriteETag(EntityTag)`, `HttpContext.TryNotModified(EntityTag)` (writes `ETag`, answers 304 and repeats the tag), `HttpContext.ParseIfMatch()` returning `EntityTag?`.
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
2. `ConditionalRequestDiagnostics.cs` (D3 option A): `Meter` and `ActivitySource` named `Encina.AspNetCore`, counters `encina.http.concurrency_conflict_total` and `encina.http.precondition_missing_total`, span `encina.http.etag_validation` with tag `etag.status` (`match`, `mismatch`, `missing`, `not_modified`).
3. `Encina.AspNetCore` is already mapped to `EventIdRanges.AspNetCore` in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; run that test to verify 208-211 stay inside the range.
4. Transactions: the check runs in the handler, inside the same unit of work scope as the write, so the loaded token is the token the store arbitrates against.

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

1. **Unit** (`tests/Encina.UnitTests/DomainModeling/Concurrency/`, `.../AspNetCore/ConditionalRequests/`): `EntityTag` parsing and comparison (strong, weak, list, `*`, quotes, whitespace), generator for both token shapes and for a resource with no token, `EnsureIfMatch` outcomes, evaluator outcomes and RFC order, filters (428, pass-through, `Items` flag), response helpers (200 with `ETag`, 304 with the tag, no body).
2. **Guard**: every new public method with parameters.
3. **Contract**: `IETagGenerator` (the real generator keeps: same token gives the same tag, different tokens differ), the 428/412/304 response shape.
4. **Property** (FsCheck): tag stability for equal tokens, difference for different tokens, parse/print round trip, a weak tag never satisfies `If-Match`.
5. **Integration**: ASP.NET Core test host with minimal API endpoints and real repositories: `[Collection("EFCore-SqlServer")]` (RowVersion) and `[Collection("Dapper-PostgreSQL")]` (Version): GET returns the tag, conditional GET returns 304, two concurrent PUTs with the same tag produce one success and one 412, a PUT without the header is 428, a tag of tenant A never matches tenant B's resource.
6. **Load / Benchmark**: `.md` justification files (the cost is one SHA-256 over a few bytes; concurrency control is not a throughput bottleneck, as the issue states).
7. Targets proposed for the manifests (`mechanical-fixer` writes them, never lower than existing): `EntityTag.cs` unit 95, guard 90, property 90; `StrongETagGenerator.cs` unit 95, property 90; `ConditionalRequestEvaluator.cs` unit 95; `IfMatchEndpointFilter.cs` unit 90, guard 85; `ETagResponseExtensions.cs` unit 95, guard 90; `ConditionalRequestLog.cs` 0 with the usual source-generated justification.

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

1. XML docs with examples on every public API; `PublicAPI.Unshipped.txt` for both packages (`mechanical-fixer`).
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
| `ProblemDetailsExtensions` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Renders 412 and 428 with an explicit status; table replaced by #3419 |
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
| Production, `Encina.DomainModeling/Concurrency/` (new) | 5 | `EntityTag`, `IETagGenerator`, `StrongETagGenerator`, `ConcurrencyPreconditionErrors`, `ConcurrencyPreconditions` |
| Production, `Encina.AspNetCore/ConditionalRequests/` (new) | 9 | `IfMatchOutcome`, `ConditionalRequestEvaluator`, `ConditionalRequestErrors`, `IfMatchEndpointFilter`, `ConditionalRequestEndpointExtensions`, `RequireIfMatchAttribute`, `ETagResponseExtensions`, `ConditionalRequestResults`, `ConditionalRequestLog` (+ `ConditionalRequestDiagnostics` if D3 is include) |
| Production (modified) | 4 | `ServiceCollectionExtensions.cs`, two `PublicAPI.Unshipped.txt`, `README.md` |
| Tests | 14 | About 7 unit, 1 guard, 1 contract, 1 property, 2 integration, 2 load and benchmark justification `.md` |
| Documentation and records | 5 | Guide, changelog fragment, two coverage manifests, INVENTORY row |

Total about 37 files and more than 1,500 lines: above one worker pull request. Proposed split (each within 15 files and 1,500 lines): **PR 1** Phase 1 (5 production files, tests, `PublicAPI`, manifest); **PR 2** Phases 2 and 3 (9 production files, DI, tests); **PR 3** Phases 4 to 6 (logging, diagnostics, integration tests, guide and records).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError,T>). Issue #3443 exposes the repositories' optimistic concurrency over REST with ETag, If-Match and If-None-Match. Dilemmas D1 (explicit property on the command), D2 (412 when If-Match was present), D3 (observability now) and D4 (neutral pieces in Encina.DomainModeling) are recommended and pending the maintainer: do not start before they are answered. #3419 (status table with 412 and 428) is the prerequisite for the table entries; the helper itself passes the status explicitly.
IMPLEMENTATION OVERVIEW: Phase 1 EntityTag, IETagGenerator, StrongETagGenerator, EnsureIfMatch in Encina.DomainModeling. Phase 2 evaluator, endpoint filter, MVC filter, response helpers in Encina.AspNetCore. Phase 3 AddEncinaConditionalRequests. Phase 4 EventIds 208-211, counters and span. Phase 5 tests in 7 types (RowVersion through EF Core, Version through Dapper). Phase 6 documentation.
KEY PATTERNS: opt-in per endpoint; hashed strong ETag; a resource without a token never satisfies a precondition; the handler loads the entity, compares, and the store still arbitrates the race; no header values or EncinaError.Message in logs; TryAdd registration proven by a ValidateOnBuild/ValidateScopes test; CRAP <= 10; Shouldly via Encina.Testing.Shouldly; PublicAPI, changelog and manifests through mechanical-fixer; documentation through docs-writer.
REFERENCE FILES: src/Encina.DomainModeling/IAuditable.cs, src/Encina.DomainModeling/RepositoryErrors.cs, src/Encina.AspNetCore/ProblemDetailsExtensions.cs, src/Encina.AspNetCore/DPIAEndpointExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Concurrency tokens are not server-side response caching (#712); the ETag only enables client and proxy revalidation. |
| 2 | OpenTelemetry | ✅ | Counters for 412 and 428 and the `encina.http.etag_validation` span (Phase 4), subject to Dilemma D3; the issue's row 2 says deferred. |
| 3 | Structured Logging | ✅ | EventIds 208-211 inside the registered `AspNetCore` range, route template and method only (Phase 4). |
| 4 | Health Checks | ❌ | A precondition outcome says nothing about the health of a dependency. |
| 5 | Validation | ✅ | The `If-Match` header is parsed and validated by `EntityTag.TryParseList` in the endpoint filter; an unparsable value is never a pass. |
| 6 | Resilience | ❌ | A conflict is a deterministic outcome, not a transient failure. |
| 7 | Distributed Locks | ❌ | Optimistic concurrency is the alternative to a lock. |
| 8 | Transactions | ✅ | The handler's check runs inside the unit of work that performs the write, so the loaded token is the one the store compares (Phase 4, task 4). |
| 9 | Idempotency | ⏭️ | ETag handling complements idempotency keys but is a different mechanism; the inbox behavior is unchanged. Deferred, no issue needed until a write path combines both. |
| 10 | Multi-Tenancy | ✅ | The tag carries no tenant; tenant-aware repositories isolate the entity, proven by a two-tenant integration test (Phase 5). |
| 11 | Module Isolation | ❌ | HTTP concurrency control is cross-module. |
| 12 | Audit Trail | ❌ | Version conflict detection is not an audit event; who changed what stays with the entity-change audit store. |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|---|---|---|
| #3419 status table (412 and 428 in the table; mechanism D-7) | Open | Phase 1 and 2 can start; the codes `encina.concurrency.precondition_*` must be in the table before release, and D2 is confirmed against that decision |
| #3420, #3426 (denial statuses, exposure of error codes) | Open, `needs-decision` | Not blocking: the helper passes the status explicitly and its details carry no internal code beyond what #3426 decides |
| #287 optimistic concurrency abstractions | Closed | Provides the token interfaces this plan reads |
| #2514 OpenAPI documentation of 412/428 | Open | The guide links to it once it exists |

---

## Next Steps

1. The maintainer answers D1 to D4 when work on #3443 starts; the answers go into `## Maintainer Decisions`.
2. Confirm #3419's mechanism and add `encina.concurrency.precondition_failed` (412) and `encina.concurrency.precondition_required` (428) to the table.
3. Brief PR 1 (Phase 1) first; it is independent of HTTP.
4. Re-check the 12-function row 2 against D3 and update the issue text.
