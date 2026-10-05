# Implementation Plan: One Request Identity — the caller identity becomes part of `IRequestContext`

> **Issue**: [#1705](https://github.com/dlrivada/Encina/issues/1705)
> **Type**: Bug (design-level fix; planned with the feature prompt because it reshapes public API in five packages)
> **Complexity**: High (7 phases, no database provider matrix, ~100 production files touched across 20+ packages incl. the 30 repository files, ~100 test files)
> **Estimated Scope**: ~2,700-3,400 lines of production code changed or added (of which ~1,000 deleted, including the 31 repository constructor parameters in 30 files) + ~3,500-4,500 lines of tests (about 100 production files touched in total)
> **ADR**: ADR-035 "One request identity model" (number reserved in the Reserved numbers table of `docs/architecture/adr/index.md` by the plan PR; file `docs/architecture/adr/035-one-request-identity-model.md` is written in Phase 7)
> **Specifications**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-015 / DEC-011 (the originating actor is persisted with deferred messages and rebuilt at dispatch) and P-50 ([#1164](https://github.com/dlrivada/Encina/issues/1164)) own deferred dispatch; this plan only provides the identity API they need (Design 1, "Persisted form")
> **Review status**: the 2026-10-04 adversarial review (63 findings) and the PR #1775 review are resolved in "Review resolution (2026-10-05)" at the end of this file. The "PR review amendments" table in the Summary is normative: where an older Design, Phase or Testing sentence conflicts with it, the amendments table wins.

---

## Summary

Today an application that follows the ABAC quick-start is denied on every `[RequirePolicy]`/`[RequireCondition]` request. Three facts combine (verified in the worktree):

1. `AddEncinaABAC` does not register `ISecurityContextAccessor`, which `ABACPipelineBehavior` requires in its constructor (`src/Encina.Security.ABAC/ABACPipelineBehavior.cs:72,87-110`). `ValidateOnBuild` cannot see it (open generic), and the existing DI tests hide it with `Substitute.For<ISecurityContextAccessor>()` (`tests/Encina.UnitTests/Security/ABAC/ABACRegistrationTests.cs:27,59`).
2. Nothing in `src/` ever assigns `ISecurityContextAccessor.SecurityContext`. The only population path is a README snippet that ignores `SecurityOptions` (`src/Encina.Security/README.md:57-65`).
3. Since #1676 the PEP denies with `abac.missing_context` (renamed `abac.unauthenticated_caller` by this plan) when the context is missing or unauthenticated, in every enforcement mode.

Behind the symptom is a structural fault: Encina has **two ambient identity channels** with **three claim configurations**:

| Channel | Populated by | Read by | User-id claim order |
|---|---|---|---|
| `IRequestContext.UserId` (core, `IRequestContextAccessor`, static `AsyncLocal`) | `EncinaContextMiddleware` (`app.UseEncinaContext()`), seeded into every pipeline by `IEncina.Send/Publish/Stream` | persistent PAP (#1677), #751 decision audit plan, Security.Audit, repositories' audit fields, EF interceptors, caching keys, Consent, DSR, GDPR, NIS2, BreachNotification, Secrets, Marten enrichment, Inbox (~30 sites) | `EncinaAspNetCoreOptions.UserIdClaimType` (NameIdentifier), `sub`, Azure AD `objectidentifier` |
| `ISecurityContext` (Encina.Security, `ISecurityContextAccessor`, static `AsyncLocal` registered as Scoped) | nobody | `SecurityPipelineBehavior`, `ABACPipelineBehavior`, `DefaultResourceOwnershipEvaluator`, `DefaultPermissionEvaluator` | `SecurityOptions.UserIdClaimType` (`sub`), NameIdentifier |
| `IPrincipalResolver` (Encina.AspNetCore, Encina.AspNetCore.Blazor) | `HttpContext.User` / Blazor `AuthenticationStateProvider` | `AuthorizationPipelineBehavior` only | n/a (whole principal) |
| DI-registered `IRequestContext` fallback (Encina.EntityFrameworkCore) | the application, by hand | `AuditInterceptor`, `SoftDeleteInterceptor`, `QueryCacheInterceptor` when the accessor is empty | the application's own |

A host that fills one channel but not the other gets a PEP denial (ABAC) or a PAP refusal (`abac.policy_change_principal_required`), and the same principal can yield two different user ids in authorization and in audit.

**This plan removes every second channel (no second identity source remains).** The caller identity becomes an immutable core type, `RequestIdentity` (kind, user id, authenticated flag, principal, roles, permissions), carried by `IRequestContext.Identity`. `IRequestContext.UserId` is a **projection by construction**: it is no longer an interface member but a C# 14 extension property over `Identity` (Design 1), so no `IRequestContext` implementation can report a user id that differs from the identity the gates authorized. `ISecurityContext`, `ISecurityContextAccessor`, `SecurityContextAccessor`, `SecurityContext`, `IPrincipalResolver` (with `AuthenticationStatePrincipalResolver`), the EF Core interceptors' `GetService<IRequestContext>()` fallback and the claim-type options of `SecurityOptions` and `EncinaAspNetCoreOptions` are deleted or folded into the single model (pre-1.0, no shim). How each channel is folded: `AuthorizationPipelineBehavior` evaluates `context.Identity.Principal`; the Blazor package gets a circuit activity handler that fills the request context (Phase 3); the interceptors read only `IRequestContextAccessor`.

The four expected behaviours of #1705 map as follows:

| #1705 item | Delivered by |
|---|---|
| (1) `AddEncinaABAC` registers what it resolves | The PEP reads the `IRequestContext` its `Handle` already receives; the accessor dependency disappears. DI test of `AddEncinaABAC` alone under `ValidateOnBuild`+`ValidateScopes`, resolving the closed behavior, no substitute. |
| (2) Opt-in ASP.NET Core integration | The existing explicit `app.UseEncinaContext()` builds the full identity from `HttpContext.User` through one claim mapper (`IRequestIdentityFactory`). No new middleware, no new package, no new ProjectReference. TestServer tests with authenticated and anonymous requests. |
| (3) Service identity for jobs and handlers | Public core API: service identities declared at startup (`AddEncinaServiceIdentity`), opened through `IRequestContextScopeFactory` (`RunAsServiceAsync`, `BeginServiceScope`, `BeginPrincipal`, `BeginRestored`) that return `Either`, refuse to replace an ambient user identity or run over an inbound request, invalidate the scope on dispose (holder-based accessor) and log every opening. Without a scope a dispatch stays anonymous and is denied (fail closed). A service identity never turns a deny gate into an allow (Design 6, DSR/Consent/GDPR extractors). |
| (4) Docs show the full registration | Quick-start, READMEs, error reference, architecture page, new how-to and ADR-035. |

Two verified hazards on the quick-start path are fixed in the same issue:

- **#1635 subset (fail-open):** `AddEncina` registers configured behaviors with `TryAddEnumerable` (`src/Encina/Core/EncinaConfiguration.cs:239-241`), and both `AddEncinaSecurity` (`src/Encina.Security/ServiceCollectionExtensions.cs:81`) and `AddEncinaABAC` (`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs:142`) use `TryAddTransient(typeof(IPipelineBehavior<,>), ...)`, which is silently skipped once any `IPipelineBehavior<,>` descriptor exists. The documented stack could run with **no ABAC at all**. Both move to `TryAddEnumerable` here (the #751 plan's Phase 4 had claimed this subset; it is pulled forward and removed from #751).
- **`RequestContext.CreateForTest` in production and `UtcNow` reads:** `EncinaContextMiddleware.cs:84` stamps contexts with `DateTimeOffset.UtcNow` through `CreateForTest` (`RequestContext.cs:151`), and `RequestContext.Create(string)` (`:125`, used by `TenantResolutionMiddleware.cs:129-131`) does the same. Both production paths move to the public TimeProvider-based `CreateAt`; `Create(string)` is deleted; `CreateForTest` no longer mints authenticated identities (Design 1).
- **`AddEncinaAuthorization` registers no behavior** although its XML doc says it does (`Encina.AspNetCore/ServiceCollectionExtensions.cs:145-146,164-196`; only `EncinaConfiguration.AddAuthorization` at `:116` registers it). A host that follows the documented call runs `[Authorize]` requests with no gate. Fixed in Phase 3 with `TryAddEnumerable` and a DI test.

**Affected packages**: `Encina` (core), `Encina.Security`, `Encina.Security.ABAC`, `Encina.AspNetCore`, `Encina.AspNetCore.Blazor`, `Encina.Security.Audit`, `Encina.EntityFrameworkCore` (interceptor fallback), `Encina.Compliance.DataSubjectRights`, `Encina.Compliance.Consent`, `Encina.Compliance.GDPR` (data-subject extractor fallbacks), `Encina.Tenancy.AspNetCore` (`TimeProvider`), `Encina.Testing`, `Encina.Testing.FsCheck`.

**Provider category**: none. No store, no SQL, no transport or cache code changes. Repositories (ADO, Dapper, MongoDB, EF Core) change only by losing their optional `IRequestContext` constructor parameter (PR review M2). The PAP actor is exercised by existing SQL Server integration suites (`PersistentPapScopeScenario` and its ADO, Dapper and EF Core users) that are rewritten; see the Testing section.

### Entry-point coverage (how identity reaches every host)

| Entry point | Mechanism | Owner / test |
|---|---|---|
| ASP.NET Core controllers, minimal APIs | `UseEncinaContext()` after `UseAuthentication()` | #1705, TestServer end-to-end |
| gRPC (`GrpcMediatorService`) and GraphQL queries/mutations over HTTP (`GraphQLMediatorBridge`) | Same middleware: both dispatch through `IEncina` inside the HTTP pipeline | #1705, covered by the middleware tests; a dedicated gRPC/GraphQL test is not added because both bridges call `IEncina.Send` without an explicit context (verified `GrpcMediatorService.cs:47-67`, `GraphQLMediatorBridge.cs:55,97`) and the harnesses would add packages; stated here, not silently skipped |
| Blazor Server circuits | `RequestIdentityCircuitHandler` in `Encina.AspNetCore.Blazor` (Phase 3) | #1705, unit test with a fake `AuthenticationStateProvider` |
| SignalR hub invocations, GraphQL subscriptions (WebSocket flows) | Not populated: dispatches see **Anonymous and are denied**; a test pins this interim behaviour | F2 |
| Azure Functions, AWS Lambda | Own claim options today; move to `IRequestIdentityFactory` (GCP gap noted, SPEC-000 DEC-003) | F3 |
| Outbox, Inbox, Scheduling, dead-letter replay (`RuntimeTypeRequestDispatcher.cs:75`), Saga/RoutingSlip runners | Originating actor persisted and rebuilt through `BeginRestored` (Design 1) | SPEC-002 REQ-015 / DEC-011, P-50 (#1164) |
| Recurring Hangfire/Quartz jobs, CDC bridge, compliance monitors (no originating request) | Declared service identity through `RunAsServiceAsync` | F1 |
| Transports (10) | Identity metadata propagation is part of the P-50 persisted form; no transport code in #1705 | P-50 (#1164) |
| `TenantResolutionMiddleware` without `UseEncinaContext` | Creates an anonymous context with `RequestContext.CreateAnonymousAt(TimeProvider)` | #1705 (Phase 3) |

### PR review amendments (2026-10-05, normative: they win over any older wording below)

Review of PR #1775 (`artifacts/pr-review/1775.md`, verdict merge after fixes). Maintainer decisions M1-M5 and the minors are applied to the Designs and Phases below and summarised here so a worker reads one list.

| # | Rule | Replaces |
|---|---|---|
| M1 | Three PAP integration suites exist (`tests/Encina.IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs`, used by `ADO/PersistentPapAdoSqlServerRegistrationTests.cs`, `Dapper/PersistentPapDapperSqlServerRegistrationTests.cs`, `EFCore/PersistentPapEFCoreSqlServerRegistrationTests.cs`, real SQL Server, `[Collection("EFCore-SqlServer")]`) plus `UnitTests/Security/ABAC/Persistence/PersistentPolicyAdministrationPointScopeTests.cs`. They substitute `ISecurityContextAccessor` and call `context.UserId.Returns(...)` on a substituted `IRequestContext`; they are rewritten (Phase 4 task 9). The planned `PolicySeedingServiceIdentityIntegrationTests` is dropped: the scenario is extended instead. | "no integration suite exists" (Summary, Testing, rows 3, 45, 53) |
| M2 | No second channel in repositories: the optional `IRequestContext? requestContext = null` constructor parameter is removed from all 31 sites (30 files); they read the ambient context through `IRequestContextAccessor` (Phase 5 task 14). `AuditedRepository`/`AuditedReadOnlyRepository` do the same and `ExcludeSystemAccess` reads that ambient `Identity`. | "no code change" for ADO/Dapper/Mongo audit fields |
| M3 | Identity is created **only** by `IRequestContextScopeFactory` (validated, logged). `IRequestIdentityFactory` (and its default `ClaimsRequestIdentityFactory`) is **internal**; customisation is through `RequestIdentityOptions`. The scope factory gains `BeginInbound(InboundRequestInfo)` used by `EncinaContextMiddleware` and the Blazor handler (principal, correlation id, tenant header, idempotency key, IP/UserAgent/DataRegion metadata). `RequestContext.CreateAt` is internal; the public factory is `CreateAnonymousAt(...)`. An explicit-context `Send` whose authenticated identity is not the ambient one always logs Warning 165; it returns `Left(scope_conflict)` when the ambient identity is a User or when the identity's issuing scope is no longer active (stale replay). Rule plus architecture test: production assemblies with `InternalsVisibleTo` from `Encina` (ADO.*, Dapper.*, EntityFrameworkCore, MongoDB, Marten, GraphQL, Security.ABAC) reference identity-minting members (`RequestIdentity.ForUser/ForService`, `RequestContext.CreateAt`) only through `RequestContextScopeFactory`; `Encina.Testing`, `Encina.Testing.FsCheck` and the test projects are the declared, ADR-035-listed test-only seam. | public `Create`/`CreateAt`, "trusted paths" list |
| M4 | `BeginRestored` never trusts persisted roles or permissions. `PersistedRequestIdentity(Kind, ActorId, TenantId, CorrelationId, CausationId)` is exactly what SPEC-002 REQ-015 persists (service: `ActorId` is the catalog name). A restored User identity carries **no roles and no permissions**, so role- or permission-gated handlers in deferred dispatch deny (fail closed). Messages whose identity arrived from outside the trust boundary (inbox from an external broker) never restore a User identity (restored Anonymous). Role rehydration is a note for #1164, not an API here. `IRequestContext` gains `string? CausationId`. | Design 1 "Persisted form" |
| M5 | The user id leaves `ReadAuditLog` EventId 1702 (`ReadAuditLog.cs:56`, called from `AuditedRepository.cs:271`, `AuditedReadOnlyRepository.cs:231`) and the `["userId"]` details of `SecurityErrors.cs:72,97,125,148`; identity kind is recorded instead. The sentinel tests cover both (Phase 5 task 15). | Phase 5 task 2 only |
| m1 | EventId 9098 is dropped; the `Disabled`-mode startup warning reuses #751's planned 9085 ("Enforcement Disabled"). Whichever of #1705 and #751 lands first allocates 9085 in `ABACLogMessages` (planned: #1705, so #751 reuses the method and adds its once-per-request-type call); Phase 7 task 4 reconciles the #751 table. 9098-9099 stay free. | 9098 |
| m5 | The origin marker is an **internal typed flag** on `RequestContext` (`internal RequestOrigin Origin`, copied by `ForNestedDispatch`), not a metadata string. SignalR and subscription flows are unmarked until F2, so `RunAsServiceAsync` is permitted there; stated in F2 and the how-to. | `encina.context.origin` metadata |
| m6 | Core EventIds are allocated in the phase that uses them, packed: Phase 1 = 162 `AuthenticatedPrincipalWithoutSubject`, 163 `ReservedServiceSubjectRejected`, 164 `ConflictingAuthenticatedIdentities`, 165 `ExplicitContextIdentityConflict`; Phase 2 = 166 `ServiceIdentityScopeOpened`, 167 `IdentityScopeRefused`, 168 `IdentityScopeClosed`, 169 `PrincipalScopeOpened`, 170 `IdentityScopeDisposedOutOfOrder`, 171 `IdentityRestored`. Older references to 164-167/168/169 read as this table. | EventId text in Design 4, Phases 1-2, Testing |
| m4 | `Encina` grants `InternalsVisibleTo` to `Encina.Testing.FsCheck` (needed by the `Arb`), `Encina.Testing` and `Encina.Security.ABAC`. `Encina.ContractTests` already references `Encina.Security` transitively; no extra ProjectReference for it. | Phase 1 task 2, Testing |

---

## Design Choices

<details>
<summary><strong>1. Identity model — one request context carries the caller identity (ISecurityContext is deleted)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RequestIdentity` on `IRequestContext`; delete `ISecurityContext`/`ISecurityContextAccessor`** | One source of truth: the subject the PEP authorizes is the subject the PAP, #751, Security.Audit and repositories record, by construction. ABAC loses its accessor dependency (item 1 solved structurally). No new ProjectReference. Nested dispatch and `TenantResolutionMiddleware.WithTenantId` already copy the context, so they cannot desync identity. | Breaks every `IRequestContext` implementation (6 hand-written, all in tests), every test substituting `ISecurityContextAccessor` (11 files) and the 67 `UserId.Returns(...)` sites in 44 test files (re-counted after the rebase on 1deeaada; they move to `Identity.Returns(...)`; pre-1.0, test convenience never shapes the design, AGENTS.md section 1). Large PR. |
| **B) Two contexts, one population path (`SecurityContextScope`) + runtime `CheckAgreement` deny (`abac.identity_mismatch`)** | Keeps `ISecurityContext` public shape. | Still two channels; drift is detected at runtime, not prevented. The strict check denies legitimate explicit-context `Send` calls and tenant overrides by `TenantResolutionMiddleware`. Its check never runs while #1635 skips ABAC. Needs a new Encina.AspNetCore → Encina.Security reference. |
| **C) `ISecurityContext` authoritative, `IRequestContext` derived by every writer; declared identities; startup source validator** | Strongest security extras. | Still two channels; its mismatch rule lets the PEP authorize while audit records no user. Its planned ABAC EventIds 9094-9096 collide with ids already used by the PAP and seeding service (`PersistentPolicyAdministrationPoint.cs:806,817,828`, `ABACPolicySeedingHostedService.cs:173`). Its startup validator cannot see middleware order, the most likely misconfiguration. Highest ceremony. |

### Chosen Option: **A — one request context**, hardened with the best ideas of C and B

### Rationale

- Three independent judge passes ranked A first (37/41/39). A removes the root cause (two channels, divergent claim orders) instead of guarding it.
- `IRequestContext` already flows through every pipeline (`AmbientRequestContext.Resolve`, `src/Encina/Core/AmbientRequestContext.cs:47-63`) and every gate already receives it in `Handle`.
- `ISecurityContext.TenantId` has no consumer in `src/` (verified), so tenant stays where it is: `IRequestContext.TenantId`, the effective tenant.
- Grafted from C: declared least-privilege service identities, `Either`-returning scopes that refuse to replace a user, reserved `service:` subject rejection for external principals, `TryAddEnumerable` for the ABAC and Security behaviors. Grafted from B: correct EventId accounting (ABAC has only 9098-9099 free), cached anonymous instance, LIFO/flow tests for scopes.
- `RequestIdentity` lives in core `Encina`; `System.Security.Claims` is BCL, so core gains no ASP.NET dependency.

**Model (exact shape):**

```csharp
namespace Encina;

public enum IdentityKind { Anonymous = 0, User = 1, Service = 2 }

public sealed class RequestIdentity
{
    public const string ServiceSubjectPrefix = "service:";
    public static RequestIdentity Anonymous { get; }            // cached singleton
    public IdentityKind Kind { get; }
    public string? UserId { get; }                              // non-null  <=>  IsAuthenticated
    public bool IsAuthenticated => Kind != IdentityKind.Anonymous;
    public ClaimsPrincipal? Principal { get; }
    public IReadOnlySet<string> Roles { get; }                  // FrozenSet, OrdinalIgnoreCase
    public IReadOnlySet<string> Permissions { get; }            // FrozenSet, OrdinalIgnoreCase
    public bool HasClaim(string type, string? value = null);    // authenticated ClaimsIdentity instances only
    public PersistedRequestIdentity ToPersisted(string? tenantId, string correlationId, string? causationId); // see "Persisted form"
    internal static RequestIdentity ForUser(string userId, ClaimsPrincipal? principal = null,
        IEnumerable<string>? roles = null, IEnumerable<string>? permissions = null);   // factory + tests only
    internal static RequestIdentity ForService(ServiceIdentityDefinition definition); // only via the scope factory
}

// UserId is NOT an IRequestContext member: it is derived, so it cannot diverge from Identity.
public static class RequestContextIdentityExtensions
{
    extension(IRequestContext context)
    {
        public string? UserId => context.Identity?.UserId;   // null Identity (non-conforming context) reads as anonymous
    }
}
```

- **Invariant:** `UserId != null` if and only if `IsAuthenticated`. A principal that is not authenticated maps to `Anonymous` even with a `sub` claim; an authenticated principal without any configured user-id claim maps to `Anonymous` and logs a Warning. This removes today's `SecurityContext` quirk where user id and authenticated flag were independent (`SecurityContext.cs:43,54-55`).
- **User ids are validated by `ForUser` and by the factory with one rule:** not blank, no leading or trailing whitespace, no control characters, and not starting (after trim, `OrdinalIgnoreCase`) with `service:`. A user id that fails the rule maps to `Anonymous` in the factory (Warning, claim type only) and throws `ArgumentException` in the internal `ForUser`. Tests cover `SERVICE:x`, ` service:x` and `service:` variants.
- **Identity cannot be forged by application code.** `ForUser` and the identity-taking test conveniences are `internal` (`InternalsVisibleTo` for the test projects that already have it, plus `Encina.Testing`, whose public `TestIdentity.User/Service/Anonymous` helpers are the supported way for application tests to build identities). An authenticated `RequestIdentity` is obtainable in production code only through `IRequestContextScopeFactory` (`BeginInbound` for HTTP and Blazor, `BeginPrincipal`, `BeginServiceScope`/`RunAsServiceAsync`, `BeginRestored`), each validated and logged. `IRequestIdentityFactory` and `ClaimsRequestIdentityFactory` are `internal` (customisation is `RequestIdentityOptions`; a custom-factory extension point is dropped). `IRequestContext.WithIdentity` is **not** a public interface member; `RequestContext.WithIdentity` and `RequestContext.CreateAt(timestamp, correlationId, identity, ...)` are `internal`; the public factory is `RequestContext.CreateAnonymousAt(DateTimeOffset, string correlationId, string? tenantId = null, string? idempotencyKey = null)`. Rule and architecture test: production assemblies that hold `InternalsVisibleTo` from `Encina` use identity-minting members only through `RequestContextScopeFactory`; `Encina.Testing`, `Encina.Testing.FsCheck` and the test projects are the declared test-only seam (`TestIdentity`), listed in ADR-035.
- **Trusted-path rule on explicit contexts.** `AmbientRequestContext.Resolve` no longer returns an explicit context unchecked: when `Send/Publish/Stream(request, explicitContext)` is called while an ambient context with `Kind == User` exists and `explicitContext.Identity` has a different kind or user id, the dispatch returns `Left(encina.identity.scope_conflict)` and logs Warning 165 with both kinds (never ids). With an Anonymous, Service or absent ambient, an explicit context whose authenticated identity differs from the ambient one is accepted only while the scope that issued that identity is still active (`scope.RequestContext` passed explicitly); it always logs Warning 165, and a stale identity (issuing scope disposed) returns `Left(scope_conflict)`. `AmbientRequestContext.Resolve` returns `Either<EncinaError, IRequestContext>`; `Encina.Send/Publish/Stream` map the `Left`. Nested dispatch (a copy of the ambient context) is not an identity change and logs nothing. The public `IRequestContextAccessor.RequestContext` setter is host infrastructure (middleware, circuit handler, scope factory), documented as such and covered by the same Resolve rule; ADR-035 lists every trusted path.
- Service subjects are `service:<name>`; a service can never collide with an IdP user id in ABAC subject lookups, PAP actor records or audit rows.

**`IRequestContext` changes:** add `RequestIdentity Identity { get; }` (documented never null). `string? UserId` and `WithUserId` are **removed from the interface**; `UserId` is the extension property above, so existing `context.UserId` reads keep compiling and `RequestContext` drops its `UserId` member and init accessor. Substitutes (`Substitute.For<IRequestContext>()`) compile but return `null` for `Identity`; every test of a consumer that reads identity builds a real context (`TestRequestContext.For(TestIdentity.User(...))` from `Encina.Testing`) instead of `UserId.Returns`. Gates read identity as `context.Identity is { IsAuthenticated: true } identity`, so a non-conforming context denies. Phase 1 verifies with a compile that extension members on an interface resolve for `RequestContext` and for NSubstitute proxies; if C# 14 extension properties do not satisfy that, the fallback is an abstract interface member that `RequestContext` implements as `Identity.UserId` plus a contract test and a `Resolve` check that rejects an explicit context whose `UserId` differs from `Identity.UserId` (decide in Phase 1, record in ADR-035).

**`RequestContext` factories:** `CreateAnonymousAt(DateTimeOffset, ...)` is the only public production factory (anonymous identity); the identity-taking `CreateAt` is `internal` and used by the scope factory; `Create(string)` is deleted (callers: `TenantResolutionMiddleware`, which takes `TimeProvider` by method injection); `CreateForTest(...)` in core drops `userId` and takes no identity (always anonymous), so the production assembly has no convenience that mints an authenticated user. Authenticated test contexts come from `Encina.Testing` (`TestIdentity.User/Service(...)` and `TestRequestContext.For(identity, ...)`, the declared test seam). 55 test call sites pass `userId:` and move to `TestRequestContext.For(TestIdentity.User(...))` (mechanical-fixer, list from `Select-String 'CreateForTest\([^)]*userId'`). `ToString()` prints the identity kind, never the user id.

**Persisted form (SPEC-002 REQ-015 / DEC-011, P-50 #1164).** Deferred dispatch rebuilds the originating actor; it never substitutes a service identity. #1705 defines the only identity API P-50 needs:

```csharp
// Exactly what SPEC-002 REQ-015 persists with a deferred message: actor, tenant, correlation id, causation id.
public sealed record PersistedRequestIdentity(
    IdentityKind Kind,
    string? ActorId,            // User: the user id; Service: the catalog name; Anonymous: null
    string? TenantId,
    string CorrelationId,
    string? CausationId);

// on IRequestContextScopeFactory (Design 3)
Either<EncinaError, RequestContextScope> BeginRestored(PersistedRequestIdentity persisted);
```

`RequestIdentity.ToPersisted(tenantId, correlationId, causationId)` produces the record (no roles, no permissions); `IRequestContext` gains `string? CausationId`, carried by copies and set by `BeginRestored`.

Validation at rebuild (any failure returns `Left`, logs a Warning with the kind only, and the dispatcher fails that message per its retry/dead-letter semantics, never runs it anonymously): `Kind == User` needs a valid user id (same rule as above, no `service:` prefix); `Kind == Service` needs an `ActorId` present in the catalog (roles/permissions come from the catalog, so a tampered row cannot escalate); `Kind == Anonymous` opens an explicit anonymous scope; correlation and causation ids are bounded strings. **A restored User identity carries no roles and no permissions**: the row is never a source of authority, so role- or permission-gated handlers in deferred dispatch deny (fail closed) and ABAC decides from the subject attributes it loads itself. A message whose identity arrived from outside the trust boundary (inbox from an external broker) never restores a User identity; the dispatcher restores Anonymous. Note for #1164 (not an API here): if deferred handlers need roles, P-50 decides how to re-resolve them at dispatch (and revocation/staleness). Every rebuild logs Information (kind, correlation id). `PersistedRequestIdentity` carries no `ClaimsPrincipal` (not serializable); a restored user's `Principal` is a synthetic authenticated `ClaimsIdentity` with the subject only.

</details>

<details>
<summary><strong>2. HTTP integration — the existing <code>UseEncinaContext()</code> builds the identity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `EncinaContextMiddleware` (opt-in stays the explicit `app.UseEncinaContext()`)** | One middleware, one ordering rule (`UseAuthentication` → `UseEncinaContext`), already documented (`ApplicationBuilderExtensions.cs:26-28`). No new package or reference. | Middleware grows; must stay under CRAP 10 (extract helpers). |
| **B) New `app.UseEncinaSecurityContext()`** | Separate concern | Second ordering rule; one middleware can overwrite the other's identity. |
| **C) Pipeline step resolving `IPrincipalResolver` at `Send` time** | Covers Blazor circuits | Runs per dispatch, not per request; nested dispatch semantics get complicated; keeps the third channel (rejected: the Blazor circuit handler below covers circuits without it). |

### Chosen Option: **A — `EncinaContextMiddleware` builds the full identity**

### Rationale

- Pay-for-what-you-use holds: nothing is populated unless the application calls `app.UseEncinaContext()`; `AddEncinaAspNetCore` only registers services.
- New `InvokeAsync` flow:
  1. Build `InboundRequestInfo` (principal `context.User`, correlation id, the `TenantIdHeader` fallback value, idempotency key, IP/UserAgent/DataRegion metadata).
  2. `using var scope = scopeFactory.BeginInbound(info)` (Design 3): inside, the internal `IRequestIdentityFactory` maps the principal to the identity (Design 4), resolves the tenant (authenticated principals first, then the header fallback, unchanged; follow-up F5), stamps the timestamp from the injected `TimeProvider`, marks the context's internal `Origin = Inbound`, logs, and sets the holder-based accessor. A `Left` (never expected for a mapped principal) is answered with an anonymous context.
  3. `await _next(context)`; `scope.Dispose()` in `finally` invalidates the holder so every flow that captured the request (`Task.Run`, fire-and-forget, timers) reads no context, which means Anonymous, which means deny.
- `IRequestContextScopeFactory` is injected into `InvokeAsync` (middleware method injection); the middleware no longer touches `IRequestIdentityFactory`, which is internal.
- **Accessor lifetime (holder pattern).** `RequestContextAccessor` keeps a static `AsyncLocal<ContextHolder>` (as `HttpContextAccessor` does) instead of a plain `AsyncLocal<IRequestContext?>`. `ContextHolder` has a mutable `Context`, a `Parent` holder and a `Disposed` flag; reading `RequestContext` returns `holder.Context` only while neither the holder nor any ancestor is disposed. Setting a context creates a new holder; ending a scope (middleware `finally`, `RequestContextScope.Dispose`) sets its holder's `Context = null` and `Disposed = true`, then restores the previous holder in the current flow. Consequences, each with tests: (a) a task started inside a request or scope and run after it ended sees Anonymous and is denied by ABAC and Security; (b) disposing scopes out of order never resurrects a disposed identity (disposing an outer scope invalidates its inner scopes; a later inner dispose restores to a disposed holder, which reads as Anonymous) and logs a Warning with identity kinds only; (c) long-lived connections (SignalR, Blazor circuits) see Anonymous once the establishing request ended, and the Blazor activity handler (below) re-establishes the identity per activity; (d) 100 parallel requests with distinct principals never observe another request's identity.
- **`AuthorizationPipelineBehavior` evaluates `context.Identity.Principal`** (authenticated identities only) and `IPrincipalResolver` is deleted together with `AuthenticationStatePrincipalResolver` (and the `HttpContextAccessor` registration made only for it). The gate denies with `authorization.unauthenticated` when `!context.Identity.IsAuthenticated`, which also closes the case where the factory mapped a token to Anonymous (no subject, reserved `service:` subject): the principal can no longer pass `[Authorize]` while audit records no user. The "requires HTTP context" denial disappears because identity is not read from `HttpContext` any more.
- **Blazor Server.** `Encina.AspNetCore.Blazor` replaces its resolver with `RequestIdentityCircuitHandler : CircuitHandler`; its `CreateInboundActivityHandler` wraps each inbound circuit activity, reads `AuthenticationStateProvider.GetAuthenticationStateAsync()`, and opens `scopeFactory.BeginInbound(...)` (Origin `Inbound`) for the activity, invalidating it afterwards. A unit test with a fake `AuthenticationStateProvider` proves a dispatch inside an activity sees the circuit user and one outside sees Anonymous.
- SignalR hub invocations and GraphQL subscriptions are not populated by #1705 (follow-up F2); until then they dispatch Anonymous and every gate denies. A test pins that interim behaviour. GraphQL queries and mutations and gRPC run inside the HTTP pipeline and are covered by the middleware (entry-point table).

</details>

<details>
<summary><strong>3. Non-HTTP identity — declared service identities and an <code>Either</code>-returning scope API</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Declared identities (`AddEncinaServiceIdentity`) + `IRequestContextScopeFactory` returning `Either`, refusing over an ambient user** | Least privilege (roles/permissions fixed at startup and reviewable in one place); misuse returns `Left`; ROP; every opening logged | One registration line per job identity |
| **B) Ad hoc `BeginServiceScope(id, roles, permissions)`** | Zero ceremony | Any code can mint any role set; elevation inside a user request is only logged |
| **C) Default service identity for all background dispatch** | Convenient | Implicit allow; violates fail-closed (AGENTS.md §3) |
| **D) Documentation only (hand-built `ClaimsIdentity(claims, "service")`)** | No code | Today's state; no logging, no restore, no namespacing |

### Chosen Option: **A**

### Rationale

```csharp
// Startup
services.AddEncinaServiceIdentity("billing-reconciliation", id => id
    .WithRoles("billing-job")
    .WithPermissions("invoices:reconcile"));

// Job / hosted service / message handler
var result = await scopes.RunAsServiceAsync(
    "billing-reconciliation",
    ct => encina.Send(new ReconcileInvoices(), ct),
    tenantId: tenant, cancellationToken: ct);
```

```csharp
public interface IRequestContextScopeFactory
{
    Either<EncinaError, RequestContextScope> BeginServiceScope(string serviceId, string? tenantId = null);
    Either<EncinaError, RequestContextScope> BeginPrincipal(ClaimsPrincipal principal, string? tenantId = null);
    Either<EncinaError, RequestContextScope> BeginInbound(InboundRequestInfo request);  // HTTP middleware, Blazor activity
    Either<EncinaError, RequestContextScope> BeginRestored(PersistedRequestIdentity persisted);  // deferred dispatch (Design 1, SPEC-002 / P-50)
    // Over an inbound-request context (HTTP, circuit activity) every Begin* refuses unless the caller passes
    // allowOverInbound: true, which logs a Warning; the parameter exists on each method (omitted above for brevity).
}

public sealed class RequestContextScope : IDisposable   // invalidates its holder and restores the previous one; idempotent
{
    public IRequestContext RequestContext { get; }
}

public static class RequestContextScopeFactoryExtensions
{
    public static Task<Either<EncinaError, T>> RunAsServiceAsync<T>(this IRequestContextScopeFactory factory,
        string serviceId, Func<CancellationToken, Task<Either<EncinaError, T>>> work,
        string? tenantId = null, CancellationToken cancellationToken = default);
}
```

- **Rules** (implementation `RequestContextScopeFactory(IRequestContextAccessor, IServiceIdentityCatalog, IRequestIdentityFactory, TimeProvider, ILogger<RequestContextScopeFactory>)`, singleton):
  - Unknown `serviceId` → `Left(encina.identity.unknown_service_identity)`.
  - **Refusals (all `Left`, all logged at Warning with kinds only):** ambient identity of `Kind == User` → `encina.identity.scope_conflict` for every `Begin*` (a job scope can never replace or escalate a user's identity); ambient context marked `origin = inbound` (HTTP request, Blazor activity) of **any** kind, including Anonymous → `scope_conflict` unless `allowOverInbound: true` is passed (opt-in per call, logged at Warning), so an anonymous or reserved-subject request cannot elevate itself into a service identity; a built-in identity (`encina.` prefix) opened through the public API → `encina.identity.reserved_service_identity`; `BeginPrincipal` with a tenant argument that differs from the principal's tenant claim → `encina.identity.tenant_conflict`.
  - Ambient `Service`, scope-origin `Anonymous` or no ambient context → allowed; replacing another service identity is logged at Warning.
  - `BeginPrincipal`: maps through `IRequestIdentityFactory` (same rules as HTTP, including reserved-subject rejection and the multi-identity rules of Design 4); an anonymous principal opens an explicit anonymous scope. Logged at Information, and at Warning when it opens a User identity over a Service ambient.
  - **Tenant binding.** A scope never inherits the ambient tenant. `BeginServiceScope`/`RunAsServiceAsync`: tenant = the `tenantId` argument only (null means no tenant). `BeginPrincipal`: tenant = `ResolveTenantId(principal)`; the argument is accepted only when the principal has no tenant claim, and a different argument returns `tenant_conflict`. `BeginRestored`: tenant = the argument (the dispatcher passes the message's persisted tenant). When the new scope's tenant differs from a non-null ambient tenant, Information is logged (no values). **Metadata carried over:** only the correlation id and the origin marker (`scope`); DataRegion and module name are not carried, because a scope is a new unit of work (documented in the how-to).
  - The new context keeps the ambient `CorrelationId` when present (or the one given to `BeginRestored`), otherwise `Activity.Current?.Id` or a new GUID; timestamp from the injected `TimeProvider`.
  - `Dispose` invalidates the scope's holder and restores the captured previous holder only if it is still valid (Design 2, accessor lifetime); it is idempotent; out-of-order disposal logs a Warning and leaves the flow Anonymous.
- `RunAsServiceAsync` opens and awaits inside one method frame, which removes the AsyncLocal pitfall (a scope opened inside an awaited helper does not flow back to the caller).
- **Tenant is per call, not part of the declaration**: tenant is not identity (Design 1), and multi-tenant jobs iterate tenants.
- Declarations are validated at startup (`ServiceIdentityCatalogOptionsValidator` with `ValidateOnStart`): name pattern `^[a-z0-9][a-z0-9.-]{0,62}$`, no wildcard roles or permissions. **The `encina.` prefix is reserved for built-in identities:** application declarations with it are rejected by the validator; library packages declare built-ins through an `internal` API (`InternalsVisibleTo` for `Encina.Security.ABAC`) that marks the catalog entry `IsBuiltIn`, and only that package opens it through the internal `BeginBuiltIn(name)`. `AddEncinaServiceIdentity` is idempotent for an identical declaration (a second `AddEncinaABAC` call or an application re-declaring the same entry does not fail the uniqueness rule); a conflicting redeclaration fails startup.
- **Built-in dispatchers wired in #1705: none, and none is ever wired to a service identity.** Deferred dispatch (Outbox `OutboxProcessorBase.cs:176`, Scheduling, Inbox, dead-letter replay, Saga/RoutingSlip runners) rebuilds the originating actor from the persisted message through `BeginRestored` under SPEC-002 REQ-015 / DEC-011 and P-50 (#1164); until P-50 lands those dispatchers run Anonymous and fail closed. Jobs with no originating request (recurring Hangfire `HangfireRequestJobAdapter.cs:136` and Quartz `QuartzRequestJob.cs:97` jobs, CDC bridge, compliance monitors) get an opt-in declared service identity in F1; serverless adapters are F3.
- **PAP seeding**: `PolicyChangeActorScope` (internal `AsyncLocal<bool>`) is deleted. `AddEncinaABAC` declares the built-in identity `encina.abac.policy-seeding` when seeding is configured, and `ABACPolicySeedingHostedService` runs under the internal `BeginBuiltIn`. The PAP accepts any authenticated identity (User or Service) as actor and records `Identity.UserId` (`service:encina.abac.policy-seeding`); anonymous is still refused with `abac.policy_change_principal_required`. The PAP audit metadata `["actor"]` records the identity kind in lowercase (`user` or `service`) replacing `system | principal`; `PolicyActor.IsSystem` and `SystemActorId` are deleted. This also gives jobs a supported way to make runtime policy changes (#1704 item 2) under their own declared (non-built-in) identities.

</details>

<details>
<summary><strong>4. Claim mapping — one ordered map in core (<code>RequestIdentityOptions</code>)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One core `RequestIdentityOptions` with ordered lists; `IRequestIdentityFactory` is internal and not replaceable** | One configuration for every entry point (HTTP now, serverless/SignalR later); apps state precedence explicitly | Breaking rename of four `SecurityOptions` and two `EncinaAspNetCoreOptions` properties |
| **B) Keep per-package single-value options** | No breaking change | Three configurations that already disagree (NameIdentifier-first vs sub-first) |

### Chosen Option: **A**

### Rationale

| Property | Default | Notes |
|---|---|---|
| `UserIdClaimTypes` | `["sub", ClaimTypes.NameIdentifier, "http://schemas.microsoft.com/identity/claims/objectidentifier"]` | OIDC-first. With ASP.NET Core JWT bearer's default inbound mapping, `sub` arrives as NameIdentifier, so the fallback catches it. Stated as breaking in the changelog and ADR. |
| `RoleClaimTypes` | `["role", ClaimTypes.Role]` | Plus each `ClaimsIdentity.RoleClaimType` when `IncludeIdentityRoleClaimType` (default `true`), matching `IsInRole`. |
| `PermissionClaimTypes` | `["permission"]` | `PermissionClaimSeparator` (`char?`, default `null`) splits space-delimited `scope`-style claims when set. |
| `TenantIdClaimTypes` | `["tenant_id", "tid", "http://schemas.microsoft.com/identity/claims/tenantid"]` | Used only for authenticated principals. |

- `IRequestIdentityFactory` (**internal**): `RequestIdentity Create(ClaimsPrincipal? principal)` and `string? ResolveTenantId(ClaimsPrincipal? principal)`. Default `ClaimsRequestIdentityFactory` (internal sealed; singleton), used only by `RequestContextScopeFactory`. Applications customise mapping through `RequestIdentityOptions`; there is no public factory to call or replace (no unlogged mint path).
- **Reserved subject**: a mapped user id that, trimmed, starts with `service:` (`OrdinalIgnoreCase`) from an external principal maps to `Anonymous` and logs `ReservedServiceSubjectRejected` (Warning, claim type only). User ids with leading/trailing whitespace or control characters get the same treatment. External tokens cannot impersonate a declared service. Treating it as anonymous (rather than a 403 short-circuit) keeps one fail-closed rule for every entry point: anonymous endpoints still work, every gate that needs a caller denies, and a scope cannot elevate it because the HTTP context is marked `inbound` (Design 3).
- **Several `ClaimsIdentity` instances on one principal.** Only **authenticated** `ClaimsIdentity` instances contribute subject, permissions, tenant and `RequestIdentity.HasClaim` (`[RequireClaim]`). Roles also come from authenticated identities only; this is stricter than `ClaimsPrincipal.IsInRole` (which counts unauthenticated identities) on purpose, because claims transformation can add unauthenticated identities and roles now drive authorization. Two authenticated identities that yield different subjects, or different tenants, map to `Anonymous` and log `ConflictingAuthenticatedIdentities` (EventId 168, Warning, claim type only) instead of depending on scheme order. Factory tests: unauthenticated extra identity ignored for sub/permission/tenant/role, two authenticated identities with equal and different subjects, different tenants.
- **Tenant claim map is one map.** `Encina.Tenancy.AspNetCore`'s `ClaimTenantResolver` and the serverless options keep their own single `ClaimType` in #1705; follow-ups F5 and F3 move them to `IRequestIdentityFactory.ResolveTenantId`. Until then an application that changes `TenantIdClaimTypes` must set the Tenancy resolver claim type too (documented in the reference page).
- `RequestIdentityOptionsValidator` (`ValidateOnStart`): every list non-empty, no blank entries.
- Removed (no aliases): `SecurityOptions.UserIdClaimType`, `RoleClaimType`, `PermissionClaimType`, `TenantIdClaimType`, `ThrowOnMissingSecurityContext`; `EncinaAspNetCoreOptions.UserIdClaimType`, `TenantIdClaimType`. `EncinaAspNetCoreOptions.TenantIdHeader` stays. `Encina.AwsLambda`/`Encina.AzureFunctions` own claim options move to the factory in F3.
- **No claim value, user id or role reaches logs, activity tags or `ToString`.** Logs carry only claim **type** names, the authentication type, identity kind, error codes and (for service scopes) the declared service name. This includes the Security gate: the `security.user_id` tag and the `UserId` field of logs 8001/8002 are removed (Phase 5). Tests with sentinel values prove it (Testing section).

</details>

<details>
<summary><strong>5. Registration and startup validation — registration completeness by construction, no "source" validator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every `AddEncina*` that needs identity TryAdds the core identity services; DI tests prove completeness** | Follows the existing precedent (ABAC, Secrets and Consent TryAdd `IRequestContextAccessor`); ABAC alone passes `ValidateOnBuild`+`ValidateScopes` | — |
| **B) `AddEncinaABAC` fails at startup naming `AddEncinaSecurity`** | Explicit | Obsolete: ABAC no longer depends on Encina.Security for identity |
| **C) ValidateOnStart "identity source registered" validator (design C)** | Catches a missing opt-in | Cannot see middleware order or a missing `UseEncinaContext()`, the most likely misconfiguration; adds markers and an opt-out for little signal |

### Chosen Option: **A**

### Rationale

- A shared internal helper `services.TryAddEncinaRequestIdentity()` in core (exposed through a public `AddEncinaRequestIdentity(Action<RequestIdentityOptions>?)`) registers: `TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>`, `TryAddSingleton(TimeProvider.System)`, `AddOptions<RequestIdentityOptions>()` + validator + `ValidateOnStart`, `TryAddSingleton<IRequestIdentityFactory, ClaimsRequestIdentityFactory>`, `TryAddSingleton<IServiceIdentityCatalog, ServiceIdentityCatalog>`, `TryAddSingleton<IRequestContextScopeFactory, RequestContextScopeFactory>`, catalog options + validator.
- Callers: `AddEncina`, `AddEncinaAspNetCore`, `AddEncinaABAC` (replaces its `TryAddSingleton<IRequestContextAccessor>` at `ServiceCollectionExtensions.cs:113`), `AddEncinaServiceIdentity`. An application's own registration wins in any order (TryAdd).
- `AddEncinaSecurity` registers no accessor any more; `SecurityHealthCheck` drops its accessor check (`SecurityHealthCheck.cs:74-77`) and keeps the evaluator checks.
- The runtime guarantee for a missing `UseEncinaContext()` is the PEP denial with `abac.unauthenticated_caller` (EventId 9091, renamed in Design 6); its log message and the error reference point to `UseEncinaContext()` after `UseAuthentication()`.
- `Encina.Security.ABAC.csproj` **keeps** its `Encina.Security` ProjectReference: `RequirePolicyAttribute.cs:57` and `RequireConditionAttribute.cs:54` derive from `SecurityAttribute`. Consequence: `SecurityPipelineBehavior` also discovers `[RequirePolicy]`/`[RequireCondition]` (`SecurityPipelineBehavior.cs:100-105`); see Design 6 for how it treats them.
- `AddEncinaAuthorization` (AspNetCore) also registers `AuthorizationPipelineBehavior<,>` with `TryAddEnumerable` (it documented that it does and did not), proven by the same DI-test pattern.
- **Registration order.** Documentation and READMEs state `AddEncinaSecurity` before `AddEncinaABAC` (maintainer decision of 2026-10-03 recorded in the #751 plan); because `TryAddEnumerable` keeps registration order as execution order, the DI tests of Phases 4-5 assert the resolved behavior order Security, then ABAC for that call order, and presence of both for the reverse order and with a behavior configured through `AddEncina`.

</details>

<details>
<summary><strong>6. Fail-closed rules and the only logged opt-outs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Anonymous is the only "missing" state; every gate denies on it; opt-outs are explicit and logged** | One rule, AGENTS.md §3 compliant | Removes `SecurityPipelineBehavior`'s silent anonymous substitution (visible change) |
| **B) Keep `ThrowOnMissingSecurityContext` and substitution** | No change | Implicit, misnamed flag (returns `Left`, does not throw: `SecurityPipelineBehavior.cs:124-129`) |

### Chosen Option: **A**

### Rationale

- **Gates (deny on `!context.Identity.IsAuthenticated`)**:
  - ABAC PEP: `abac.unauthenticated_caller` (EventId 9091) in every enforcement mode including Warn, before attribute collection.
  - `SecurityPipelineBehavior`: **before evaluating any attribute**, if any `SecurityAttribute` other than `[AllowAnonymous]` is present (including attributes of unknown subclasses such as `[RequirePolicy]`/`[RequireCondition]`) and the identity is not authenticated, return `security.unauthenticated`. This closes the pass-through of empty `[RequireRole]`/`[RequirePermission]` (`SecurityPipelineBehavior.cs:226-229,245-248,265-268`) and unknown subclasses (`:164`) for anonymous callers. The attribute constructors also reject empty role/permission lists (`ArgumentException`). For authenticated callers the behavior keeps ignoring `SecurityAttribute` subclasses it does not own, because their owning gate enforces them (the ABAC PEP owns `[RequirePolicy]`/`[RequireCondition]`); a test with an ABAC-only request covers anonymous (denied by the Security gate pre-check) and user (passed on to the PEP). `[DenyAnonymous]` and `RequireAuthenticatedByDefault` → `security.unauthenticated`.
  - `AuthorizationPipelineBehavior`: `authorization.unauthenticated` (Design 2).
  - `PersistentPolicyAdministrationPoint`: `abac.policy_change_principal_required`.
  - Gates read identity with the pattern `context.Identity is { IsAuthenticated: true } identity`, so a non-conforming `IRequestContext` that returns `null` denies instead of throwing.
- **A service identity never turns a deny into an allow.** Compliance gates that fall back to `context.UserId` as the data subject (`DefaultDataSubjectIdExtractor.cs:71-75`, `ConsentRequiredPipelineBehavior.cs:223-224`, `ILawfulBasisSubjectIdExtractor.cs:55-59`) use the fallback **only when `Identity.Kind == IdentityKind.User`**; a Service or Anonymous identity counts as a missing subject and takes each gate's existing fail-closed path (the DSR restriction gate in Block mode denies with its subject-missing error). Phase 5 task 12 implements it; unit tests per extractor and gate, plus the end-to-end case (`RunAsServiceAsync` + `[RestrictProcessing]` request without a subject property → subject-missing error). This resolves the review's blocker; the former follow-up F7 is removed.
- **Removed implicit paths**: the `SecurityContext.Anonymous` substitution and `ThrowOnMissingSecurityContext`; `SecurityErrors.MissingContext` (`security.missing_context`) and log 8004. "Missing" and "anonymous" are now the same state.
- **Explicit opt-outs (the only ones)**: a declared service identity opened through the scope factory (Information log; Warning when it replaces another service identity or runs over an inbound request); `BeginPrincipal`/`BeginRestored` (Information); `[AllowAnonymous]` (existing `AllowAnonymousBypass` log 8003); ABAC `EnforcementMode.Disabled`, which **now logs** a one-time startup Warning (EventId 9085, #751's planned "Enforcement Disabled" id, allocated by #1705 because it lands first; emitted by the registration validator/hosted startup check), so every gate switch-off leaves a trace; not putting ABAC attributes on requests meant to run without a caller. Identity-creation paths (the scope factory's `BeginInbound`/`BeginPrincipal`/`BeginServiceScope`/`BeginRestored`, the accessor setter as host infrastructure, and the `Encina.Testing` test seam) are listed in ADR-035 with how each is logged; there is no public factory, `ForUser` and `CreateAt` are internal, and the explicit-context rule logs and refuses the rest (Design 1).
- No setting makes the default identity non-anonymous.
- No `EncinaError.Message`, claim value, user id, token or role list reaches logs, tags or `ToString`; only codes, identity kind, service name and claim-type names.
- **Error code renamed on the merits:** `abac.missing_context` becomes `abac.unauthenticated_caller` (the "security context" it named no longer exists; the meaning is "no authenticated caller in the request context"). `ABACErrors.MissingContext` → `UnauthenticatedCaller`, its XML docs and fixed message are rewritten around the caller, `ABACLogMessages.MissingSecurityContext` → `UnauthenticatedCaller`; docs and the #1676 knowledge record are updated (Phase 7).

</details>

<details>
<summary><strong>7. Behavior registration — ABAC and Security move to <code>TryAddEnumerable</code> (#1635 subset)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(X<,>)))` for ABAC and Security in #1705** | The new quick-start cannot run without ABAC; the full-stack DI test can assert both behaviors | Touches the #751 plan (its Phase 4 claimed it) |
| **B) Leave to #1635/#751** | Smaller PR | The quick-start this issue documents is fail-open whenever `AddEncina` configures a behavior or `AddEncinaSecurity` is called first |

### Chosen Option: **A**

### Rationale

- Verified: `EncinaConfiguration.cs:239-241` uses `TryAddEnumerable`; `Security/ServiceCollectionExtensions.cs:81` and `ABAC/ServiceCollectionExtensions.cs:142` use `TryAddTransient` on the open generic.
- Each behavior denies independently, so order is not a fail-closed concern, but the maintainer decision of 2026-10-03 (recorded in the #751 plan) fixes the documented order: `AddEncinaSecurity` before `AddEncinaABAC`. `TryAddEnumerable` makes registration order the execution order, so the DI tests assert it (Design 5); named pipeline stages remain #1678.
- The orchestrator updates #1635 ("ABAC, Encina.Security and Encina.AspNetCore authorization subset fixed by #1705"). The #751 plan did **not** claim this subset (its lines 29, 382, 394, 753 make #1635 a prerequisite), so Phase 7 rewrites those prerequisite statements and the stale accessor references at :194, :217, :322 and :549 rather than deleting a non-existent item.

</details>

<details>
<summary><strong>8. Scope boundary — what #1705 does not do</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core identity, HTTP, Blazor circuits, scope API (incl. `BeginRestored`), PAP seeding, Security/ABAC/Authorization consumers, compliance extractors, EF interceptors, #1635 subset; the rest as follow-up issue files** | One reviewable PR in 7 green phases; no second identity channel remains | Large; SignalR, GraphQL subscriptions, serverless and deferred dispatchers wait for their owners |
| **B) Also `DispatchIdentity` on 7 processors, serverless/SignalR/GraphQL adapters** | Complete | Doubles the PR across 12 more packages; deferred dispatch is owned by SPEC-002 P-50 and must not run as a service identity |

### Chosen Option: **A**

### Rationale

Follow-up issue files written by the worker (the orchestrator opens them, `open-issue` skill). Deferred dispatch is **not** a follow-up of this plan: SPEC-002 REQ-015 / DEC-011 / AC-015 and P-50 (#1164) own it, and #1705 provides `PersistedRequestIdentity` and `BeginRestored` for it.

| Id | Template | Title (draft) |
|---|---|---|
| F1 | `[FEATURE]` | Opt-in declared service identity (`DispatchIdentity`) for jobs with **no originating request**: recurring Hangfire and Quartz jobs, CDC bridge, compliance monitors; logged at startup. Outbox, Inbox, Scheduling, dead-letter replay and Saga/RoutingSlip runners are excluded (P-50) |
| F2 | `[FEATURE]` | Request identity for SignalR hub invocations (hub filter) and GraphQL subscriptions (supersedes #1306); the interim Anonymous-deny behaviour is pinned by a test in #1705 |
| F3 | `[FEATURE]` | Azure Functions middleware and AWS Lambda helper set the request context through `IRequestIdentityFactory`; remove their own claim options (including their `TenantIdClaimType`); GCP gap noted (SPEC-000 DEC-003) |
| F5 | `[BUG]` | `X-Tenant-ID` header becomes the tenant for anonymous callers and for authenticated callers without a tenant claim; `TenantResolutionMiddleware` can override the authenticated tenant; `ClaimTenantResolver` (`TenancyAspNetCoreOptions.ClaimType`) must read `IRequestIdentityFactory.ResolveTenantId` |
| F6 | `[FEATURE]` | ABAC built-in subject attributes from `RequestIdentity` (kind, roles, permissions) when `DefaultAttributeProvider` is used |

F4 (spike on propagating the enqueue-time identity) is dropped in favour of P-50 (#1164); F7 (data-subject fallbacks) is implemented in #1705 (Design 6). Ids are not renumbered so review references stay stable. Orchestrator actions outside this PR: update #1635, #751 plan pointers, #707 and #1704 comments; comment on #1164 with the `BeginRestored` contract; add `"**/Identity/*.cs"` to `FOLDERS` of `.github/workflows/mutation-tests.yml` with `FILTERS` entry `Core.Identity` and a `TIMEOUTS` entry in lock-step (shared hot spot, orchestrator only).

</details>

---

## Implementation Phases

Every phase ends with `dotnet build Encina.slnx --configuration Release` at 0 warnings and the touched test projects green. Phases 1-3 are additive in core and AspNetCore; Phase 4 moves ABAC off the accessor; Phase 5 deletes the Security types once nothing references them.

### Phase 1: Core identity model and claim mapping

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Identity/IdentityKind.cs`** — `public enum IdentityKind { Anonymous, User, Service }`.
2. **`src/Encina/Identity/RequestIdentity.cs`**, **`PersistedRequestIdentity.cs`**, **`RequestContextIdentityExtensions.cs`** (the `UserId` extension property) — sealed class per Design 1: cached `Anonymous`; `internal ForUser(...)` (user-id rule: not blank, trimmed, no control characters, no `service:` prefix `OrdinalIgnoreCase`); `HasClaim` over authenticated identities; `ToPersisted()`; `internal ForService(ServiceIdentityDefinition)` (added in Phase 2); `FrozenSet<string>` with `StringComparer.OrdinalIgnoreCase`. Add `InternalsVisibleTo` for `Encina.Testing` (and `Encina.Security.ABAC` in Phase 2) in `Encina.csproj`. First verify with a throwaway compile that the C# 14 extension property resolves on `RequestContext` and on NSubstitute proxies; otherwise apply the fallback recorded in Design 1.
3. **`src/Encina/Identity/RequestIdentityOptions.cs`** + **`RequestIdentityOptionsValidator.cs`** (`IValidateOptions<RequestIdentityOptions>`) per Design 4.
4. **`src/Encina/Identity/IRequestIdentityFactory.cs`** + **`ClaimsRequestIdentityFactory.cs`** (both internal; sealed, ctor `(IOptions<RequestIdentityOptions>, ILogger<ClaimsRequestIdentityFactory>)`): `Create(ClaimsPrincipal?)`, `ResolveTenantId(ClaimsPrincipal?)`. Small private helpers (`FindFirst(IEnumerable<string>)`, `CollectRoles`, `CollectPermissions`) so each method stays at complexity ≤ 5.
5. **`src/Encina/Abstractions/IRequestContext.cs`** — add `Identity`; remove `UserId` and `WithUserId` from the interface (Design 1); document `Identity` as never null and the extension `UserId` as its projection. Update the XML sample in `src/Encina/Abstractions/IRequestPreProcessor.cs:14-24` (it builds identity from `HttpContext` and calls `WithUserId`, a second population path): replace it with a non-identity example.
6. **`src/Encina/Core/RequestContext.cs`** — store `Identity` (default `RequestIdentity.Anonymous`); delete the `UserId` member and init accessor; copy constructor and `ForNestedDispatch` copy `Identity`, metadata and the internal typed `Origin` flag (`RequestOrigin { Unspecified, Inbound, Scope, Restored }`; `ForNestedDispatch` keeps it); `internal WithIdentity`; `internal CreateAt(DateTimeOffset timestamp, string correlationId, RequestIdentity identity, string? tenantId = null, string? idempotencyKey = null)` and public `CreateAnonymousAt(...)`; add `string? CausationId` to `IRequestContext`/`RequestContext`; **delete `Create(string)`** (its `UtcNow` read) and move `TenantResolutionMiddleware` to `CreateAnonymousAt` with a `TimeProvider`; `CreateForTest(...)` drops `userId` and is anonymous-only; `ToString()` prints kind, not user id. The existing middleware callers (`EncinaContextMiddleware.cs:84`, `TenantResolutionMiddleware.cs:130`) get a minimal compile fix in this phase (Phase 3 rewrites them). `AmbientRequestContext.Resolve` applies the explicit-context rule (Design 1). `RequestContextAccessor` becomes holder-based (Design 2). Migrate the 55 `CreateForTest(... userId: ...)` test call sites to `TestRequestContext.For(TestIdentity.User(...))`; `WithUserId` has no other src caller than `EncinaProperties.cs:206`.
7. **`src/Encina/Diagnostics/RequestIdentityLog.cs`** — `[LoggerMessage]` 162 `AuthenticatedPrincipalWithoutSubject` (Warning: authentication type, configured claim types), 163 `ReservedServiceSubjectRejected` (Warning: claim type), 164 `ConflictingAuthenticatedIdentities` (Warning), 165 `ExplicitContextIdentityConflict` (Warning, both kinds). Phase 1 allocates exactly 162-165; Phase 2 appends 166-171 (no gap between phases). Also add the `encina.identity.kind` tag constant to core `ActivityTagNames` and pass the context's identity kind to `EncinaDiagnostics.SendStarted/StartStreamActivity` so every dispatch activity carries it (unit test for anonymous, user, service).
8. **`src/Encina/Identity/RequestIdentityServiceCollectionExtensions.cs`** — public `AddEncinaRequestIdentity(this IServiceCollection, Action<RequestIdentityOptions>? configure = null)` (TryAdd, idempotent); `AddEncina` calls it.
9. `src/Encina.Testing.FsCheck/EncinaProperties.cs:198-212`: rename the public property `WithUserIdCreatesNewContext` to `WithIdentityCreatesNewContext` and rebuild it on `TestRequestContext.For(TestIdentity.User(...))` (`Encina.Testing.FsCheck` reaches `ForUser` only through that seam); update `tests/Encina.UnitTests/Testing/FsCheck/EncinaPropertiesTests.cs:90-96`. (`EncinaArbitraries.cs:107` and `src/Encina.Testing/Sagas/SagaSpecification.cs:153` call `CreateForTest(correlationId:)`, not `WithUserId`, and only need a compile check.) Add an `Arb` for `RequestIdentity` in `Encina.Testing.FsCheck` (produces Anonymous and User only, documented) with a unit test in `tests/Encina.UnitTests/Testing/FsCheck/EncinaArbitrariesTests.cs`, and `TestIdentity` (`User`, `Service`, `Anonymous`) in `Encina.Testing`. Fix XML samples and `GlobalSuppressions` that mention `WithUserId`.
10. Update the 6 hand-written `IRequestContext` implementations (`BenchmarkRequestContext`; `TestRequestContext` in GuardTests, IntegrationTests, PropertyTests, UnitTests/Marten; `TestInfrastructure/PropertyTests/TestRequestContext.cs`) to expose `Identity`, and migrate the 67 `UserId.Returns(...)` sites in 44 test files to `Identity.Returns(...)` / real contexts. Add a `ProjectReference` to `Encina.Testing` in `tests/Encina.ContractTests/Encina.ContractTests.csproj` and `tests/Encina.TestInfrastructure/Encina.TestInfrastructure.csproj` (neither references it today) so contract tests and shared infrastructure build authenticated contexts with `TestRequestContext.For(TestIdentity.User(...))` like every other test project; check the reference does not create a project cycle before committing.
11. `PublicAPI.Unshipped.txt` of Encina (the removed `WithUserId` lines are in **Unshipped** at `:73,120`, not Shipped; Shipped has none). `Encina.Testing` has no PublicAPI files; `Encina.Testing.FsCheck` has `PublicAPI.Shipped.txt`: check its csproj for `PublicApiAnalyzers` and edit only if tracked.
12. Tests (Phase 1 classes in the Testing section).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Worktree: D:\Proyectos\Encina\.claude\worktrees\w1705.
Issue #1705; plan docs/plans/security-context-population-implementation-plan-1705.md (Design 1 and 4).
The caller identity becomes an immutable core RequestIdentity carried by IRequestContext.Identity.
IRequestContext.UserId stays as a projection of Identity.UserId.

TASK
1. Create src/Encina/Identity/{IdentityKind,RequestIdentity,RequestIdentityOptions,RequestIdentityOptionsValidator,
   IRequestIdentityFactory,ClaimsRequestIdentityFactory,RequestIdentityServiceCollectionExtensions}.cs exactly as Design 1/4.
   Invariant: UserId != null <=> IsAuthenticated. Unauthenticated principal -> Anonymous. Authenticated without user-id
   claim -> Anonymous + Warning 162. Mapped id starting with "service:" -> Anonymous + Warning 163. Phase 1 allocates
   EventIds 162-165 only (164 ConflictingAuthenticatedIdentities, 165 ExplicitContextIdentityConflict).
2. IRequestContext: add Identity (never null); remove UserId and WithUserId from the interface and add the C# 14 extension
   property UserId over Identity (Design 1; verify the compile first, fallback in the plan). RequestContext: Identity field,
   no UserId member, copy in copy-ctor and ForNestedDispatch, internal WithIdentity, internal CreateAt(timestamp, correlationId,
   identity, tenantId, idempotencyKey), public CreateAnonymousAt; internal typed Origin flag; CausationId; delete Create(string);
   CreateForTest drops userId and is anonymous-only; IRequestIdentityFactory/ClaimsRequestIdentityFactory are internal;
   ToString prints the kind; ForUser is internal; AmbientRequestContext.Resolve applies the explicit-context rule;
   RequestContextAccessor becomes holder-based.
3. AddEncina calls AddEncinaRequestIdentity(). Fix every caller (Select-String 'WithUserId' and 'CreateForTest\([^)]*userId' and 'UserId\.Returns' over src, tests).
4. Update the 6 hand-written IRequestContext implementations in tests/benchmarks.
5. PublicAPI.Unshipped.txt for every new public symbol; delete the WithUserId lines from Encina's Unshipped file.
6. Write the Phase 1 tests listed in the plan's Testing section, then run the verification block (see "Verification commands").

KEY RULES
- PowerShell only; Edit tool for files; never commit, push or open issues.
- [LoggerMessage] EventIds 162-163, 168-169 inside EventIdRanges.Core (100-199; 100-161 used). Never log claim values or user ids.
- No DateTime.UtcNow; TimeProvider only. XML docs with <example> on every public symbol. Zero warnings.
- Methods complexity <= 5 where possible (CRAP <= 10 on changed methods; run the crap-gate command of "Verification commands").
- Shouldly via Encina.Testing.Shouldly; no FluentAssertions.

REFERENCE FILES
src/Encina/Abstractions/IRequestContext.cs, src/Encina/Core/RequestContext.cs, src/Encina/Core/AmbientRequestContext.cs,
src/Encina.Security/SecurityContext.cs (role/permission extraction to port), src/Encina.AspNetCore/EncinaContextMiddleware.cs:129-196
(claim fallbacks to port), src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 2: Service identities and the scope API

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Identity/ServiceIdentityDefinition.cs`** (public sealed: `Name`, `Roles`, `Permissions`, `Claims`) and **`ServiceIdentityBuilder.cs`** (`WithRoles`, `WithPermissions`, `WithClaim(type, value)`).
2. **`src/Encina/Identity/ServiceIdentityCatalogOptions.cs`** + **`ServiceIdentityCatalogOptionsValidator.cs`** (name pattern, uniqueness, no `*` entries; `ValidateOnStart`).
3. **`src/Encina/Identity/IServiceIdentityCatalog.cs`** (public: `bool TryGet(string name, out ServiceIdentityDefinition definition)`) and internal **`ServiceIdentityCatalog`** (`FrozenDictionary`, ordinal).
4. **`RequestIdentity.ForService(ServiceIdentityDefinition)`** (internal): principal `new ClaimsIdentity(claims, "encina-service")` with `sub = service:<name>`, `encina:identity_kind = service`, declared roles/permissions/claims under the first configured claim types.
5. **`src/Encina/Identity/IRequestContextScopeFactory.cs`**, **`RequestContextScope.cs`**, internal **`RequestContextScopeFactory.cs`**, **`RequestContextScopeFactoryExtensions.cs`** (`RunAsServiceAsync<T>`) per Design 3. Activity tag `encina.identity.kind` set on `Activity.Current` when a scope opens.
6. **`src/Encina/Identity/RequestIdentityErrorCodes.cs`** (`encina.identity.unknown_service_identity`, `encina.identity.scope_conflict`) and factories in **`RequestIdentityErrors.cs`** (fixed messages, no ids from callers in the message beyond the declared service name).
7. **`AddEncinaServiceIdentity(this IServiceCollection, string name, Action<ServiceIdentityBuilder>? configure = null)`** — validates the name eagerly (`ArgumentException`; `encina.` prefix rejected), adds to catalog options (idempotent for an identical declaration), calls `AddEncinaRequestIdentity()`. Internal `AddBuiltInServiceIdentity` for library packages.
8. Logs (append to `RequestIdentityLog.cs`, packed after Phase 1's 162-165): 166 `ServiceIdentityScopeOpened` (Information: service name, tenant present, correlation id; Warning instead when it replaces another ambient service identity or runs over an inbound request), 167 `IdentityScopeRefused` (Warning: error code, requested kind), 168 `IdentityScopeClosed` (Debug), 169 `PrincipalScopeOpened` (Information; Warning when it opens a User identity over a Service ambient), 170 `IdentityScopeDisposedOutOfOrder` (Warning: kinds), 171 `IdentityRestored` (Information: kind, correlation id). The packed core range is 162-171.
9. Holder-based `RequestContextAccessor`, the internal typed `RequestContext.Origin` flag, `BeginInbound`/`InboundRequestInfo`, `BeginRestored` and `PersistedRequestIdentity` validation (Design 1), tenant rules and metadata carry-over (Design 3). Add the architecture test (`Encina.Testing.Architecture` rule in `tests/Encina.UnitTests/Testing/Architecture`) asserting that, in production assemblies, only `RequestContextScopeFactory` references `RequestIdentity.ForUser/ForService` and `RequestContext.CreateAt`.
10. PublicAPI, tests (including tenant, origin, out-of-order disposal, dead-flow `Task.Run`, built-in refusal, idempotent declaration, `BeginRestored` tamper cases).
11. Decide whether `IsSameAs` counts claims beyond roles and permissions (step-up amr/acr), and route claims/role refresh through a scope (the setter throws when roles change) — see #1705 comments.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan Design 3. Phase 1 added RequestIdentity,
IRequestContext.Identity and IRequestIdentityFactory.

TASK
Add declared service identities and IRequestContextScopeFactory in src/Encina/Identity:
- AddEncinaServiceIdentity(name, builder) -> catalog options validated with ValidateOnStart (pattern ^[a-z0-9][a-z0-9.-]{0,62}$, unique, no wildcard).
- BeginServiceScope(serviceId, tenantId?), BeginPrincipal(principal, tenantId?), BeginInbound(info) and BeginRestored(persisted)
  return Either<EncinaError, RequestContextScope>. Follow the rules of Design 3 exactly: refusals (unknown service, ambient User,
  inbound origin of any kind, built-in name via the public API, tenant_conflict), tenant binding (never inherit the ambient tenant),
  metadata carry-over (correlation id and origin only), holder-based Dispose (invalidate own holder, restore previous only if valid,
  out-of-order Warning), RunAsServiceAsync<T> opens, awaits and disposes in one frame.
- Logs 162-171 packed in EventIdRanges.Core; Activity tag encina.identity.kind (anonymous|user|service); never log user ids, roles or claim values.
Write the Phase 2 tests (scope restore, LIFO and non-LIFO nesting ending Anonymous, Task.Run started inside a scope and run after
Dispose is Anonymous, refusal over a user and over an inbound anonymous context, tenant cases, built-in refusal and the internal
BeginBuiltIn, BeginRestored validation and tamper cases, FakeTimeProvider timestamp, FakeLogger EventIds with sentinel values proving
no user id/role/tenant value is logged, Send inside RunAsServiceAsync reaches a capturing behavior with Kind=Service and Anonymous after).

KEY RULES
ROP (no exceptions for refusals; ArgumentException only for programming errors at registration). TryAdd everywhere.
PowerShell only; no commit/push/issues. PublicAPI.Unshipped.txt. CRAP <= 10.

REFERENCE FILES
src/Encina/Core/AmbientRequestContext.cs:65-170 (scope/restore pattern), src/Encina.Security.ABAC/Administration/PolicyChangeActorScope.cs,
src/Encina/Core/RequestContextAccessor.cs.
```

</details>

### Phase 3: HTTP integration in `EncinaContextMiddleware`

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.AspNetCore/EncinaContextMiddleware.cs`** — `InvokeAsync(HttpContext, IRequestContextScopeFactory)`; build `InboundRequestInfo` and call `BeginInbound` (Origin `Inbound`, TimeProvider inside the factory); dispose the scope in `finally` (Design 2); extract `BuildContext(...)` and `ResolveTenant(...)` helpers; delete `ExtractUserId` and the claim part of `ExtractTenantId` (header fallback stays).
2. **`src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs`** — delete `UserIdClaimType`, `TenantIdClaimType`; fix the XML sample `options.UserIdClaimType = "sub";` in `ServiceCollectionExtensions.cs:65`.
3. **`src/Encina.AspNetCore/ServiceCollectionExtensions.cs`** — `AddEncinaAspNetCore` calls `AddEncinaRequestIdentity()`; **delete `IPrincipalResolver`, its default HTTP implementation and the `HttpContextAccessor` registration made only for it**; `AddEncinaAuthorization` registers `AuthorizationPipelineBehavior<,>` with `TryAddEnumerable`. `AuthorizationPipelineBehavior` evaluates `context.Identity.Principal` and denies `authorization.unauthenticated` when `!Identity.IsAuthenticated` (Design 2).
4. **`src/Encina.AspNetCore.Blazor`** — delete `AuthenticationStatePrincipalResolver`; add `RequestIdentityCircuitHandler` (Design 2) and its registration.
5. **`src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs`** — create the fallback context with `RequestContext.CreateAnonymousAt(timeProvider.GetUtcNow(), ...)` (`TimeProvider` by method injection).
6. **`src/Encina.EntityFrameworkCore`** — remove the `GetService<IRequestContext>()` fallback from `AuditInterceptor.cs:267,380`, `SoftDeleteInterceptor.cs:166` and `QueryCacheInterceptor.cs:382`; they read only `IRequestContextAccessor`. Rewrite `docs/features/audit-tracking.md:682-703` (Phase 7).
7. PublicAPI: remove the deleted `Encina.AspNetCore` symbols from `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` (not Shipped), add the new ones; README section stub (full docs in Phase 7).
8. Existing tests to rewrite: `tests/Encina.UnitTests/AspNetCore/EncinaContextMiddlewareTests.cs` (new constructor/`InvokeAsync` signature at `:25-27` and its 13 `InvokeAsync(context, _accessor)` calls; the claim cases at `:73-139` move to `ClaimsRequestIdentityFactoryTests`), `EncinaAspNetCoreOptionsTests.cs:16-40` and `ServiceCollectionExtensionsTests.cs:103-113` (drop the deleted options), the `AuthorizationPipelineBehavior` tests, `PolicyBasedAuthorizationTests.cs:385-408` (resolve the behavior from DI instead of building it by hand), and the EF Core interceptor tests that register an `IRequestContext`.
9. New tests: `EncinaContextMiddlewareIdentityTests` (TestServer, extends the `RequestContextPropagationTests` pattern), `AspNetCoreIdentityRegistrationTests` (including `AddEncinaAuthorization` alone), `AuthorizationIdentityTests` (no-subject and reserved-subject tokens denied on an `[Authorize]` request), `RequestIdentityCircuitHandlerTests`, a concurrency test (N parallel TestServer requests with distinct principals, each handler asserting its own user id), a captured-task test (task started during a request, run after completion, is denied) and an interim-behaviour test for SignalR-style flows.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan Design 2. Core now has RequestIdentity,
IRequestContextScopeFactory.BeginInbound/BeginPrincipal/BeginRestored (the internal IRequestIdentityFactory is not callable here).

TASK
- EncinaContextMiddleware: build InboundRequestInfo (principal, correlation id, header tenant fallback, idempotency key, IP,
  UserAgent, DataRegion), open scopeFactory.BeginInbound(info) (identity, tenant, TimeProvider timestamp and Origin Inbound are
  set inside the factory), await next, dispose the scope in finally (invalidates the holder). Remove RequestContext.CreateForTest
  from production code.
- Delete EncinaAspNetCoreOptions.UserIdClaimType/TenantIdClaimType; AddEncinaAspNetCore calls AddEncinaRequestIdentity().
- Also do tasks 3-9 of the Phase 3 task list (IPrincipalResolver removal, Blazor handler, TenantResolutionMiddleware TimeProvider,
  EF interceptor fallback removal, AddEncinaAuthorization registration, existing test rewrites); parallel and captured-task tests.
- Tests with TestServer (pattern tests/Encina.UnitTests/AspNetCore/RequestContextPropagationTests.cs): authenticated (Kind=User,
  roles, permissions), anonymous (Anonymous), authenticated without subject (Anonymous + Warning 162), sub "service:x"
  (Anonymous + Warning 163), custom UserIdClaimTypes, FakeTimeProvider timestamp, no identity leak between sequential requests,
  custom RequestIdentityOptions (claim order) honoured; no public identity factory exists.
- DI: AddEncinaAspNetCore alone with ValidateOnBuild/ValidateScopes resolves IRequestContextScopeFactory and IRequestContextAccessor.

KEY RULES
Keep InvokeAsync complexity low (helpers); CRAP <= 10. No new ProjectReference. PowerShell only; no commit/push/issues.

REFERENCE FILES
src/Encina.AspNetCore/EncinaContextMiddleware.cs, EncinaAspNetCoreOptions.cs, ServiceCollectionExtensions.cs, ApplicationBuilderExtensions.cs.
```

</details>

### Phase 4: ABAC PEP, persistent PAP and seeding on the request identity

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Security.ABAC/ABACPipelineBehavior.cs`** — delete `_securityContextAccessor` field and constructor parameter (`:72,90`); `private static string? ResolveUserId(IRequestContext context) => context.Identity is { IsAuthenticated: true } identity ? identity.UserId : null;`; `Handle` passes `context`. Rename the missing-context path to `HandleUnauthenticatedCaller`; rewrite the XML docs and comments at `:28-30,148,360,376`.
2. **`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs`** — `AddEncinaRequestIdentity()` replaces `TryAddSingleton<IRequestContextAccessor>` (`:113`); behavior registration → `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ABACPipelineBehavior<,>)))` (`:142`); when seeding is configured, the internal `AddBuiltInServiceIdentity("encina.abac.policy-seeding")` (idempotent); register the `Disabled`-mode startup warning with EventId 9085 (#751's planned "Enforcement Disabled" id; #1705 lands first and allocates it in `ABACLogMessages`, #751 reuses the method for its per-request-type call).
3. **`src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs`** (line numbers after #1720, merged as 6ba311d6: `TryResolveActor` at `:627`, system branch `:630-638`, `SystemActorId` `:73`, `PolicyActor` `:91`, audit metadata `:818`) — `TryResolveActor` reads `RequestContext.Identity` (User or Service); delete the system-actor branch, `SystemActorId` and `PolicyActor.IsSystem`; audit metadata `["actor"] = identity.Kind` lowercase (`user`/`service`); update the XML docs at the old `:105-110`.
4. **Delete** `src/Encina.Security.ABAC/Administration/PolicyChangeActorScope.cs` and update `docs/knowledge/issues/1677.md` in the same PR (decision at `:39-46` `current: no`, superseded by #1705; destination at `:46` and bullet at `:75` retargeted to the service identity), then run `dotnet run .github/scripts/knowledge-records.cs -- --check` (CI `knowledge-records` fails when a `done` target is missing).
5. **`src/Encina.Security.ABAC/ABACPolicySeedingHostedService.cs`** — inject `IRequestContextScopeFactory`; seed under the internal built-in scope; a `Left` fails startup seeding with the existing seeding failure log (code only). EventId 9096 (`LogSystemActorScopeOpened`, `:65-66,172-176`) is **repurposed**, not deleted (ADR-021 forbids sparse ranges): `SeedingServiceIdentityStarted`, message "ABAC policy seeding started under service identity {ServiceName}".
6. `ABACErrors.MissingContext` (`:54,369-377`) → `UnauthenticatedCaller` with code `abac.unauthenticated_caller`, fixed message and XML docs without "security context" or ids; `ABACLogMessages.MissingSecurityContext` (9091) → `UnauthenticatedCaller`. New EventId 9085 `ABACEnforcementDisabled` (Warning, once at startup; #751's planned id, allocated here because #1705 lands first, #751 reuses it); 9098-9099 stay free. Update the README/EventId note (`README.md:180`) and the allocation test expectations if any.
7. **Keep** the `Encina.Security` ProjectReference (ABAC attributes derive from `SecurityAttribute`). No change to `Encina.Security.ABAC.csproj` except `InternalsVisibleTo` from core.
8. `src/Encina.Security.ABAC/PublicAPI.Unshipped.txt:32` — replace the `ABACPipelineBehavior` constructor line (drops `ISecurityContextAccessor`); add the renamed/new symbols.
9. Tests to rewrite (no `UserId.Returns`; build real contexts with `TestRequestContext.For(TestIdentity.User(...))`): **`tests/Encina.IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs`** (`:40` substitutes `ISecurityContextAccessor`; `:41-45` `UserId.Returns("scope-test-user")` on a substituted context; `:55-61` seeds through `ABACPolicySeedingHostedService` then `AddPolicyAsync`/`UpdatePolicyAsync`: register the accessor and open the actor through the scope factory instead) and its three SQL Server users (`ADO/PersistentPapAdoSqlServerRegistrationTests.cs:25`, `Dapper/PersistentPapDapperSqlServerRegistrationTests.cs:25`, `EFCore/PersistentPapEFCoreSqlServerRegistrationTests.cs:34`, `[Collection("EFCore-SqlServer")]`; the scenario is extended to assert the persisted policy and the `service` audit actor), **`UnitTests/Security/ABAC/Persistence/PersistentPolicyAdministrationPointScopeTests.cs`** (`:107-108,134,150,207`, same two patterns), `ABACRegistrationTests` (no substitute), `ABACPipelineBehaviorTests`, `ABACRequirementEnforcementTests`, `PersistentPolicyAdministrationPointFailClosedTests`, **`Persistence/PersistentPolicyAdministrationPointTests.cs`** (substitute at `:27-31`, 38 tests), **`Persistence/PersistentPolicyAdministrationPointAuditTests.cs`** (`:770-798` becomes a seeding-under-service-identity test asserting `UserId == "service:encina.abac.policy-seeding"` and `Metadata["actor"] == "service"`; a user change writes `"user"`), **`ABACPolicySeedingHostedServiceTests.cs`** (15 constructor calls take `IRequestContextScopeFactory`), guard and contract tests (`ABACPipelineBehaviorGuardTests`, `ABACPipelineBehaviorContractTests`, `PersistentPAPContractTests`). New: `PolicySeedingServiceIdentityTests`, `ABACIdentityProperties`, a registration test with `SeedPolicies` (`ValidateOnBuild`+`ValidateScopes`, then `Host.StartAsync` so `ValidateOnStart` runs, resolving the hosted service; a double `AddEncinaABAC` call; an application re-declaring the built-in name is rejected), a test that application code opening `encina.abac.policy-seeding` gets `Left`. Acceptance: `Get-ChildItem tests\Encina.UnitTests\Security\ABAC, tests\Encina.UnitTests\Security\Audit, tests\Encina.ContractTests\Security\ABAC, tests\Encina.GuardTests\Security\ABAC, tests\Encina.IntegrationTests\Security\ABAC -Recurse -Filter *.cs | Select-String -Pattern 'UserId\.Returns|ISecurityContextAccessor'` returns nothing (`Select-String` has no `-Recurse`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan Designs 3, 5, 6, 7. Core has IRequestContext.Identity
and IRequestContextScopeFactory with declared service identities.

TASK
- ABACPipelineBehavior: drop ISecurityContextAccessor; ResolveUserId(IRequestContext) uses
  `context.Identity is { IsAuthenticated: true } identity ? identity.UserId : null`; denial in every mode, code renamed abac.unauthenticated_caller.
- Do every numbered task of the Phase 4 task list (error renamed to abac.unauthenticated_caller, EventId 9096 repurposed, 9085 Disabled
  warning, audit metadata actor = user|service, knowledge record 1677, PublicAPI line 32, the full test rewrite list).
- AddEncinaABAC: AddEncinaRequestIdentity(); TryAddEnumerable for ABACPipelineBehavior<,>; declare built-in "encina.abac.policy-seeding" when seeding.
- PersistentPolicyAdministrationPoint.TryResolveActor: any authenticated identity (User or Service); anonymous -> policy_change_principal_required.
- Delete PolicyChangeActorScope; seeding hosted service runs under the internal built-in scope.
- Tests: AddEncinaABAC ALONE with ValidateOnBuild+ValidateScopes, resolve IEnumerable<IPipelineBehavior<GuardedRequest,string>> in a scope,
  no substitute; AddEncina(with a configured behavior)+AddEncinaABAC in both orders -> ABAC present. Rebuild PEP tests on
  TestRequestContext.For(TestIdentity.User(...)): anonymous denies in Block/Warn, user evaluated, service evaluated, Disabled skips.
  PAP records "service:encina.abac.policy-seeding" for seeding; anonymous refused. Property: PEP never calls IAttributeProvider for
  an unauthenticated identity. Update guard/contract tests for the new constructor.

KEY RULES
ABAC EventIds: only 9085 is new (Disabled warning, #751's id); 9096 is repurposed; 9098-9099 stay free. CRAP <= 10: keep Handle edits to one call site.
PowerShell only; no commit/push/issues.

REFERENCE FILES
src/Encina.Security.ABAC/ABACPipelineBehavior.cs:87-153,360-392; ServiceCollectionExtensions.cs:97-185 (re-read after #1720);
Administration/PersistentPolicyAdministrationPoint.cs:627-640,818; ABACPolicySeedingHostedService.cs; docs/knowledge/issues/1677.md.
```

</details>

### Phase 5: Encina.Security on the request identity; delete the second channel

<details>
<summary><strong>Tasks</strong></summary>

1. **Delete** `src/Encina.Security/Abstractions/ISecurityContext.cs`, `ISecurityContextAccessor.cs`, `src/Encina.Security/SecurityContext.cs`, `SecurityContextAccessor.cs`.
2. **`SecurityPipelineBehavior.cs`** — constructor loses the accessor; reads `context.Identity`; delete the null branch (`:118-133`); evaluation helpers take `RequestIdentity`. Extract `EvaluateAttributes(RequestIdentity, ...)` so `Handle` complexity drops. Add the pre-check of Design 6 (any non-`AllowAnonymous` `SecurityAttribute` + unauthenticated identity → `security.unauthenticated` before evaluating attributes); `RequireRoleAttribute`/`RequirePermissionAttribute` constructors reject empty lists. **Diagnostics:** delete `SecurityDiagnostics.TagUserId/SetUserId` (`:38,55-59`, call at `SecurityPipelineBehavior.cs:135`) and the `UserId` field of logs 8001/8002 (record identity kind; keep the EventIds, update the templates); update `ObservabilityTests` and `docs/features/security-authorization.md:324`.
3. **`IPermissionEvaluator`**, **`IResourceOwnershipEvaluator`** and defaults — `ISecurityContext context` → `RequestIdentity identity`; `[RequireClaim]` reads `identity.Principal`.
4. **`SecurityOptions.cs`** — delete the four claim types and `ThrowOnMissingSecurityContext`; keep `RequireAuthenticatedByDefault`, `AddHealthCheck`.
5. **`SecurityErrors.cs`** — delete `MissingContext`/`MissingContextCode`. **`SecurityLogMessages.cs`** — delete 8004 (8004-8009 free afterwards).
6. **`ServiceCollectionExtensions.cs`** — no accessor; `TryAddEnumerable` for `SecurityPipelineBehavior<,>`.
7. **`Health/SecurityHealthCheck.cs`** — drop the accessor check.
8. Attribute XML docs that `cref` `ISecurityContext` → `RequestIdentity`.
9. **`src/Encina.Security.Audit/AuditedRepository.cs:253`, `AuditedReadOnlyRepository.cs:213`** — `ExcludeSystemAccess` predicate becomes `Identity is { Kind: IdentityKind.Service }` (service only; anonymous reads are audited), already implemented in Phase 1 by #1824. Rewrite the XML docs of `ReadAuditOptions.cs:60-72` accordingly.
10. Coverage manifests (all of Encina, Encina.Security, Encina.Security.ABAC, Encina.AspNetCore, Encina.AspNetCore.Blazor and the other touched packages): moved to Phase 6 (final), with a check command.
11. Tests: `SecurityPipelineBehaviorTests`, `EvaluatorTests`, `ServiceCollectionExtensionsTests`, `ObservabilityTests` rewritten; `SecurityContextTests` deleted (covered by core identity tests); `BehaviorRegistrationOrderTests` (asserts Security then ABAC for that call order, both present in the reverse order and with `AddEncina` configuring a behavior); Security.Audit tests rebuilt on real contexts: `AuditedRepositoryTests.cs:552-582,662,674` and `AuditedReadOnlyRepositoryTests.cs:441-471,572,584` (`ExcludeSystemAccess_*` currently use `UserId.Returns` on a substitute), plus new user/service/anonymous cases; `Security/SecurityPipelineBehaviorContractTests` and the guard tests listed in the Testing section; an ABAC-only request with an anonymous identity (denied by the pre-check) and with a user (passed to the PEP).
12. **Compliance extractors (blocker fix, Design 6).** `DefaultDataSubjectIdExtractor.cs:71-75`, `ConsentRequiredPipelineBehavior.cs:223-224` and `ILawfulBasisSubjectIdExtractor.cs:55-59`: the `context.UserId` fallback applies only when `context.Identity.Kind == IdentityKind.User`; otherwise the subject is missing and the existing fail-closed path runs. Unit tests per extractor and gate with Service and Anonymous identities; the end-to-end case lives in `RequestIdentityEndToEndTests`.
14. **Repositories read the ambient context (M2, no second channel).** Remove the optional `IRequestContext? requestContext = null` constructor parameter from all 31 sites in 30 files and read `IRequestContextAccessor.RequestContext` (anonymous when null) instead; update their DI registrations and the tests/benchmarks that pass a context:
    - ADO (MySQL, PostgreSQL, SqlServer): `Repository/FunctionalRepositoryADO.cs`, `Sharding/FunctionalShardedRepositoryADO.cs`, `Tenancy/TenantAwareFunctionalRepositoryADO.cs`, `UnitOfWork/UnitOfWorkRepositoryADO.cs` (12 files).
    - Dapper (MySQL, PostgreSQL, SqlServer): `Repository/FunctionalRepositoryDapper.cs`, `Sharding/FunctionalShardedRepositoryDapper.cs`, `Tenancy/TenantAwareFunctionalRepositoryDapper.cs`, `UnitOfWork/UnitOfWorkRepositoryDapper.cs` (12 files).
    - MongoDB: `BulkOperations/BulkOperationsMongoDB.cs`, `Repository/FunctionalRepositoryMongoDB.cs`, `Sharding/FunctionalShardedRepositoryMongoDB.cs`, `SoftDelete/SoftDeletableFunctionalRepositoryMongoDB.cs`, `Tenancy/TenantAwareFunctionalRepositoryMongoDB.cs`, `UnitOfWork/UnitOfWorkRepositoryMongoDB.cs` (6 files).
    - Entity Framework Core repositories and `Encina.Security.Audit/AuditedRepository.cs` and `AuditedReadOnlyRepository.cs` (which take `IRequestContext` by constructor): same change; re-list with `Get-ChildItem src -Recurse -Filter *.cs | Select-String 'IRequestContext\? requestContext = null|IRequestContext _requestContext'` and fix every hit that is an identity reader (`IQueryCacheKeyGenerator`, Marten repositories, `RoutingSlipContext` and `SagaRunner` are checked individually and listed in the PR if they pass a context by design). `ExcludeSystemAccess` (task 9) reads the ambient `Identity`. Add a test that a repository resolved from DI reports the ambient identity and that `docs/features/audit-tracking.md:219-225` no longer recommends `AddScoped<IRequestContext, HttpRequestContext>()` (Phase 7).
15. **User id out of the remaining sinks (M5).** `Encina.Security.Audit/Diagnostics/ReadAuditLog.cs:56` (EventId 1702 `by user '{UserId}'` and its `userId` parameter, called from `AuditedRepository.cs:271` and `AuditedReadOnlyRepository.cs:231`): drop the user id, log the identity kind (keep the EventId; update the template and the allocation test if it asserts text). `Encina.Security/SecurityErrors.cs:72,97,125,148`: remove the `["userId"]` detail (and the `userId` parameters, whose callers are `SecurityPipelineBehavior.cs:234,253,286,299,308,317,337`), keep the code, add `["identityKind"]`. The sentinel tests (Testing rules) extend to `AuditedRepositoryTests`/`AuditedReadOnlyRepositoryTests` (log 1702) and `SecurityPipelineBehaviorTests` (error details contain no user id).
13. **Remaining `UserId.Returns`/`ISecurityContext` sweep.** `Get-ChildItem src,tests,docs -Recurse -File | Select-String` (there is no `samples` directory; examples live in `docs/examples`) for `ISecurityContext`, `SecurityContextAccessor`, `ThrowOnMissingSecurityContext`, `UserIdClaimType`, `WithUserId`, `IPrincipalResolver`, `security.missing_context`, `abac.missing_context` returns only historical release notes, knowledge records marked superseded, and plans. `UserIdClaimType` hits under `src/Encina.AzureFunctions` and `src/Encina.AwsLambda` are excluded until F3 (they keep their own option).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan Designs 1, 6, 7. After Phase 4 nothing outside
Encina.Security references ISecurityContext/ISecurityContextAccessor (verify with Get-ChildItem | Select-String over src, tests, docs/examples).

TASK
Delete ISecurityContext, ISecurityContextAccessor, SecurityContext, SecurityContextAccessor, the claim-type options and
ThrowOnMissingSecurityContext, SecurityErrors.MissingContext, log 8004. SecurityPipelineBehavior and the evaluators work on
RequestIdentity from the IRequestContext passed to Handle; anonymous denies wherever an attribute requires a caller.
TryAddEnumerable for SecurityPipelineBehavior<,>. SecurityHealthCheck drops the accessor check. AuditedRepository /
AuditedReadOnlyRepository ExcludeSystemAccess -> Identity is { Kind: IdentityKind.Service } (already done in Phase 1, #1824). RS0017: delete the lines of removed symbols from
PublicAPI.Unshipped.txt (that is where all of them are). Also tasks 12-13 (compliance extractors, sweep) and the SecurityDiagnostics
user-id removal.
Tests: every attribute against Anonymous/User/Service identities; AddEncinaSecurity alone and Security+ABAC+AddEncina(with a
configured behavior) in both orders with ValidateOnBuild+ValidateScopes -> both behaviors present.

KEY RULES
No [Obsolete], no aliases. CRAP <= 10 (extract helpers from Handle). PowerShell only; no commit/push/issues.

REFERENCE FILES
src/Encina.Security/SecurityPipelineBehavior.cs, ServiceCollectionExtensions.cs:59-96, SecurityOptions.cs, SecurityErrors.cs,
Diagnostics/SecurityLogMessages.cs, DefaultPermissionEvaluator.cs, DefaultResourceOwnershipEvaluator.cs.
```

</details>

### Phase 6: End-to-end verification across the stack

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.UnitTests/AspNetCore/RequestIdentityEndToEndTests.cs`** — TestServer with a test `AuthenticationHandler<AuthenticationSchemeOptions>` selecting a principal by header; `AddEncina` + `AddEncinaAspNetCore` + `AddEncinaSecurity` + `AddEncinaABAC(Block, persistent PAP with an in-memory store stub where needed)`; `UseAuthentication` → `UseEncinaContext` → endpoint calling `IEncina.Send` of a `[RequirePolicy]` request with a permit-if-subject policy. Cases in the Testing section.
2. **Hosted-service scenario** in the same class: a loop that sends an ABAC-guarded request inside `RunAsServiceAsync` is permitted by a policy targeting `service:` subjects; without the scope it is denied. **DSR case:** `RunAsServiceAsync` + a `[RestrictProcessing]` request without a subject property returns the DSR subject-missing error. **Escalation cases:** an anonymous HTTP request calling `RunAsServiceAsync` gets `Left(scope_conflict)`; a task started inside `RunAsServiceAsync` and run after `Dispose` is denied.
3. **Contract tests** (see the Testing section for the manifest file and flag each counts toward): `RequestContextIdentityContractTests` (over `RequestContext` produced by every factory and transformer, plus `TestInfrastructure`'s `TestRequestContext`), `RequestIdentityFactoryContractTests`, `RequestContextScopeFactoryContractTests`, `SecurityPipelineBehaviorContractTests`. Add `ProjectReference`s to `Encina.Security` and `Encina.AspNetCore` in `Encina.ContractTests.csproj` (or move the middleware case to UnitTests).
4. Justification files (integration, load, benchmark) in the AGENTS.md section 9 format, and the one shared-fixture integration test (Testing section).
5. Coverage manifests (final state): `dotnet run --file .github/scripts/generate-coverage-manifest.cs` (append-only) plus removal of stale keys (`PolicyChangeActorScope.cs`, `ISecurityContext.cs`, `ISecurityContextAccessor.cs`, `SecurityContext.cs`, `SecurityContextAccessor.cs`), done by `mechanical-fixer`; acceptance: `dotnet run .github/scripts/coverage-report.cs -- --check-stale-manifest --check-missing-manifest` exits 0.
6. Run the verification block (see "Verification commands"): unit, guard, contract, property suites with per-flag coverage, EventId architecture tests, knowledge-records check, changelog check, CRAP gate; fix gaps. Every flag of the 10 packages in the Testing section reaches its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705. Phases 1-5 are done; this phase proves the issue's
acceptance end to end and closes the per-flag coverage obligations.

TASK
Write RequestIdentityEndToEndTests (TestServer + test authentication handler) and the contract tests listed in the plan.
Run the "Verification commands" block of the plan exactly (per-flag test runs with --collect "XPlat Code Coverage" into
artifacts/test-results/<Flag>, coverage-report, crap-gate with --enforce, manifest checks, knowledge-records and changelog checks);
every flag of Encina, Encina.Security, Encina.Security.ABAC, Encina.AspNetCore, Encina.Security.Audit, Encina.Testing and
Encina.Testing.FsCheck (and the touched Blazor, EF Core and Compliance packages) reaches its manifest target; CRAP <= 10 on every changed method.
Write the justification .md files (integration, load, benchmark) in the AGENTS.md section 9 format.

KEY RULES
Shared [Collection] fixtures only for database tests; outputs under artifacts/. PowerShell only; no commit/push/issues.
```

</details>

### Phase 7: Documentation, ADR, changelog and follow-up issue files

<details>
<summary><strong>Tasks</strong></summary>

1. Docs (encina-docs skill, one Diátaxis quadrant per page, no hand-typed coverage, the 2026-10-05 visual rule of #1759: diagrams and tables, no walls of text) — list in "Documentation" below.
2. **ADR-035** `docs/architecture/adr/035-one-request-identity-model.md` (records: the single model, the extension-property `UserId`, the trusted identity-creation paths and how each is logged, the persisted form for P-50, the Disabled-mode warning, `IRequestContextScopeFactory.BeginInbound`/`BeginPrincipal` (and `BeginServiceScope`/`RunAsServiceAsync`, `BeginRestored`) as the only public identity-minting surface (intentionally callable from background code with any principal, always logged and refused over a User or inbound ambient), the `Encina.Testing` test seam, the entry-point table); move the 035 row from the Reserved table to the ADR table in `index.md`.
3. Changelog fragments (below), written by `mechanical-fixer`; `dotnet run --file .github/scripts/changelog-fragments.cs -- --check` exits 0.
4. `docs/plans/abac-decision-audit-implementation-plan-751.md` — edit `:194` (delete the "ISecurityContextAccessor is not registered" validator clause), `:217` (subject from `context.Identity`), `:322` (constructor list without `securityContextAccessor`), `:549` (table row), and the #1635 prerequisite statements at `:382`, `:394`, `:753` (ABAC and Security subset done by #1705); keep the order marker at `:385` consistent with `TryAddEnumerable`; note `IdentityKind` is available for the decision row.
5. Knowledge records: `docs/knowledge/issues/1677.md` (Phase 4) and `docs/knowledge/issues/1676.md:18,65` (error renamed to `abac.unauthenticated_caller`, forward note to #1705); `dotnet run .github/scripts/knowledge-records.cs -- --check` passes.
6. Follow-up issue files F1, F2, F3, F5, F6 under the worktree's `artifacts/issues/` (orchestrator opens them), drafted by the local model per the worker protocol.
7. XML docs review on every new public symbol; `docs/INVENTORY.md` updated unconditionally (`:3390`, `:5608-5617` and any entry for the new Identity folder).
8. Acceptance sweep (same scope rule as Phase 5 task 13: `Get-ChildItem docs,src -Recurse -File | Select-String`, no `samples`, `AzureFunctions`/`AwsLambda` `UserIdClaimType` excluded until F3) for `ISecurityContext`, `SecurityContextAccessor`, `ThrowOnMissingSecurityContext`, `UserIdClaimType`, `WithUserId`, `IPrincipalResolver`, `security.missing_context`, `abac.missing_context` returns only historical release notes and plans.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705. Code is complete. Load .claude/skills/encina-docs/SKILL.md.

TASK
Update every page in the plan's Documentation section; remove the three "#1705 tracking" sentences
(docs/features/abac/quick-start.md:50, docs/features/abac/reference/errors.md:153, src/Encina.Security.ABAC/README.md:119)
and the hand-written Encina.Security README snippet (README.md:55-66), fixing every link to #3-set-security-context.
Write the how-to "Run background work with a service identity", the reference "Request identity and claim mapping",
ADR-035, the changelog fragments and issue files F1, F2, F3, F5, F6 using the templates verbatim. Also update every page of the
"Documentation" section in full (security-authorization.md is a full rewrite of the Security Context material) and run the
acceptance sweep of Phase 7 task 8.

KEY RULES
English only; Diátaxis; no coverage figures by hand; no AI attribution. PowerShell only; no commit/push/issues.
```

</details>

---

## Consumer changes

| Consumer | Change |
|---|---|
| `ABACPipelineBehavior` | Reads `context.Identity`; accessor dependency removed; error renamed `abac.unauthenticated_caller` |
| `PersistentPolicyAdministrationPoint` | Actor = any authenticated identity; system-actor branch, `IsSystem`, `SystemActorId` and `PolicyChangeActorScope` deleted; audit `actor` = `user`/`service` |
| `ABACPolicySeedingHostedService` | Runs under the built-in identity `encina.abac.policy-seeding` (internal API); log 9096 repurposed |
| #751 decision audit plan | Subject from `context.Identity`; `IdentityKind` recorded; stale accessor references and #1635 prerequisites rewritten |
| `SecurityPipelineBehavior`, `DefaultPermissionEvaluator`, `DefaultResourceOwnershipEvaluator` | Evaluate `RequestIdentity`; null-context branch deleted; anonymous pre-check; user id removed from tag and logs 8001/8002 |
| `SecurityHealthCheck` | Accessor check removed |
| `AuditedRepository`, `AuditedReadOnlyRepository` | `ExcludeSystemAccess` = identity is a service (anonymous reads are audited; Phase 1, #1824) |
| `EncinaContextMiddleware` | Builds identity through the factory; TimeProvider; holder scope invalidated in `finally`; origin `inbound` |
| `AuthorizationPipelineBehavior` | Evaluates `context.Identity.Principal`; denies unauthenticated; `IPrincipalResolver` deleted |
| Blazor `AuthenticationStatePrincipalResolver` | Replaced by `RequestIdentityCircuitHandler` |
| EF Core `AuditInterceptor`, `SoftDeleteInterceptor`, `QueryCacheInterceptor` | `GetService<IRequestContext>()` fallback removed; accessor only |
| `DefaultDataSubjectIdExtractor`, `ConsentRequiredPipelineBehavior`, GDPR `ILawfulBasisSubjectIdExtractor` | Context user-id fallback only for `Kind == User` (Service and Anonymous take the fail-closed path) |
| `TenantResolutionMiddleware` | Fallback context via `CreateAt(TimeProvider)` |
| Encina.Security.PII, Sanitization, Encryption, AntiTampering | No change: they read no identity (verified; `PIIMaskingPipelineBehavior` takes `IRequestContext` but reads no user). Role- or purpose-aware redaction (#1201) will read `context.Identity.Roles` when it lands. |
| Security.Audit factory, Secrets recorder, ADO/Dapper/Mongo audit fields, caching keys, NIS2, BreachNotification, Marten enrichment, Inbox | No code change; `UserId` is derived from the authorized `Identity` by construction. Service subjects (`service:<name>`) appear in audit rows and cache keys |

---

## Testing

Targets (`.github/coverage-manifest`, per flag, no project-wide percentage; AGENTS.md section 9 and the 2026-10-05 obligations rule #1762): Encina unit 70 / guard 20 / contract 15; Encina.Security unit 60 / guard 15 / contract 10; Encina.Security.ABAC unit 70 / guard 20 / contract 15 / property 15; Encina.AspNetCore unit 60 / guard 15; Encina.Security.Audit unit 70 / guard 20 / contract 15; Encina.Testing and Encina.Testing.FsCheck unit 60 / guard 15. For `Encina.AspNetCore.Blazor`, `Encina.EntityFrameworkCore`, `Encina.Compliance.DataSubjectRights`, `Encina.Compliance.Consent`, `Encina.Compliance.GDPR` and `Encina.Tenancy.AspNetCore` read the target in the package manifest before starting and treat it the same way. A line covered by one flag does not count for another.

**Per-file flag plan for the new and deleted-and-replaced files** (flags follow `.github/coverage-manifest/defaults.json`; the generator appends the manifest entries, and each row's flags are the ones the tests below must reach):

| File (new or heavily changed) | Flags | Justification |
|---|---|---|
| `Identity/RequestIdentity.cs`, `PersistedRequestIdentity.cs`, `RequestContextIdentityExtensions.cs` | unit, guard | Value type with invariants; the invariant is also a property (below, informational) |
| `Identity/ClaimsRequestIdentityFactory.cs` | unit, guard (+ property, informational) | `*Factory.cs` default; security-critical mapping, so a property test over generated principals is added beyond the target |
| `Identity/RequestContextScopeFactory.cs`, `RequestContextScope.cs` | unit, guard, contract | Contract test pins restore/never-throws-for-refusal for any implementation |
| `Identity/RequestIdentityOptions.cs`, `ServiceIdentityCatalogOptions.cs` | unit, guard | `*Options.cs` default |
| `Identity/RequestIdentityOptionsValidator.cs`, `ServiceIdentityCatalogOptionsValidator.cs` | unit, guard, property | `*Validator.cs` default adds the property flag: generated option lists (blank, duplicate, wildcard, over-long names) |
| `Identity/RequestIdentityServiceCollectionExtensions.cs` | unit, guard | `*Extensions.cs` default; DI tests with `ValidateOnBuild`+`ValidateScopes` |
| `Identity/RequestIdentityErrors.cs`, `ServiceIdentityBuilder.cs`, `ServiceIdentityCatalog.cs` | unit (+ guard for the builder/catalog) | `*Errors.cs` unit only; builder/catalog default |
| `Core/RequestContext.cs`, `RequestContextAccessor.cs`, `AmbientRequestContext.cs` | unit, guard | Existing manifest flags; contract test over the factories as below |
| `Encina.AspNetCore/EncinaContextMiddleware.cs`, `AuthorizationPipelineBehavior.cs` | unit, guard (+ contract for the behavior) | TestServer tests live in UnitTests (justified below) |
| `Encina.AspNetCore.Blazor/RequestIdentityCircuitHandler.cs` | unit, guard | Fake `AuthenticationStateProvider` |
| `Encina.Security/SecurityPipelineBehavior.cs` | unit, guard, contract | Only contract-applicable Security file (target 10); no contract test exists today, so `SecurityPipelineBehaviorContractTests` is mandatory |
| `Encina.Security/DefaultPermissionEvaluator.cs`, `DefaultResourceOwnershipEvaluator.cs`, attributes, health check | unit, guard | Evaluator contract tests would count toward no obligation, so none is written; guard tests are sized to reach 15% across guard-applicable files |
| `Encina.Security.ABAC/ABACPipelineBehavior.cs` | unit, guard, contract | Existing flags; contract test updated |
| `Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs`, `ABACPolicySeedingHostedService.cs` | unit, guard | PAP contract exists (`PersistentPAPContractTests`) and is updated |
| `Encina.Security.Audit/AuditedRepository.cs`, `AuditedReadOnlyRepository.cs` | unit, guard, contract | Predicate change; manifest flags |
| DSR/Consent/GDPR extractors and pipeline behaviors | unit, guard (+ contract where the manifest says) | Service and Anonymous cases |
| `Encina.Testing/TestIdentity.cs`, `Encina.Testing.FsCheck` `Arb`/`EncinaProperties.cs` | unit, guard | New Arb has its own unit test; it produces Anonymous and User only |

**Rules for rewritten tests.** Every consumer that reads `Identity` is tested with real contexts built by `TestRequestContext.For(TestIdentity.User/Service/Anonymous(...))` from `Encina.Testing` (core `CreateForTest` is anonymous-only and `CreateAt` is internal), never `UserId.Returns`. The private `IRequestContext` doubles in other test projects are updated by compile only. No claim value, user id, role or tenant reaches a log or tag: tests use sentinel values in `ClaimsRequestIdentityFactoryTests`, `RequestContextScopeFactoryTests`, `EncinaContextMiddlewareIdentityTests`, the Security gate tests (`ObservabilityTests`: tags contain `encina.identity.kind` and no `security.user_id`, user id or role) and assert absence from every `FakeLogger` record (formatted message and structured state) and from `Activity` tags.

| Flag | Project | Classes |
|---|---|---|
| Unit | `Encina.UnitTests` | `Core/Identity/RequestIdentityTests` (invariant, cached anonymous, `ForUser` guards incl. `service:` prefix, case-insensitive sets); `Core/Identity/ClaimsRequestIdentityFactoryTests` (null/unauthenticated → anonymous, authenticated without subject → anonymous + 162, reserved subject → anonymous + 163, precedence sub > NameIdentifier > oid and custom order, `RoleClaimType` honoured, permission separator, tenant only when authenticated); `Core/Identity/RequestIdentityOptionsValidatorTests`; `Core/RequestContextIdentityTests` (`With*`, `ForNestedDispatch` keep identity, `CreateAt` timestamp); `Core/Identity/ServiceIdentityCatalogTests` + validator; `Core/Identity/RequestContextScopeFactoryTests` (restore, LIFO, idempotent dispose, `Task.Run` isolation, refusal over user, unknown name, service-over-service log, correlation kept, FakeTimeProvider, FakeLogger 166-171, activity tag; the factory tests use FakeLogger 162-165); `Core/Identity/ServiceIdentityDispatchTests`; `Core/Identity/IdentityRegistrationTests` (`AddEncina`, `AddEncinaServiceIdentity`, `ValidateOnBuild`+`ValidateScopes`, `ValidateOnStart` failures via `Host.StartAsync`); `AspNetCore/EncinaContextMiddlewareIdentityTests`; `AspNetCore/AspNetCoreIdentityRegistrationTests`; `AspNetCore/RequestIdentityEndToEndTests` (authenticated → handler sees Kind=User and same `UserId` in PAP/audit capture; anonymous → `abac.unauthenticated_caller`, handler not invoked; no-subject → denied + 162; custom `UserIdClaimTypes` honoured by ABAC and audit; `[DenyAnonymous]`/`[RequirePermission]` on the same identity; no leak between requests; hosted-service scenario); `Security/ABAC/ABACRegistrationTests` (ABAC alone, both orders with `AddEncina`); `Security/ABAC/ABACPipelineBehaviorTests`, `ABACRequirementEnforcementTests` (rebuilt on real contexts); `Security/ABAC/Persistence/PersistentPolicyAdministrationPointFailClosedTests`, `PolicySeedingServiceIdentityTests`; `Security/SecurityPipelineBehaviorTests`, `EvaluatorTests`, `ServiceCollectionExtensionsTests`, `ObservabilityTests`, `BehaviorRegistrationOrderTests`; `Security/Audit/AuditedRepositoryExcludeSystemAccessTests`; `Testing/Architecture/EncinaEventIdAllocationTests` (unchanged map, must pass); `Core/Identity/IdentityDiagnosticsTests` (`encina.identity.kind` on `Encina.Send` for anonymous, user, service); `Core/Identity/ExplicitContextConflictTests` (explicit-context `Send` over an ambient user returns `Left(scope_conflict)` and logs 169); `Core/Identity/AccessorLifetimeTests` (dead-flow `Task.Run`, non-LIFO disposal ends Anonymous, parallel flows); `Core/Identity/TenantBindingTests`; `Core/Identity/BeginRestoredTests`; `AspNetCore/AuthorizationIdentityTests`, `RequestIdentityCircuitHandlerTests`, `AspNetCoreIdentityRegistrationTests` (incl. `AddEncinaAuthorization` alone); existing files rewritten (named in Phases 3-5): `EncinaContextMiddlewareTests`, `EncinaAspNetCoreOptionsTests`, `AspNetCore/ServiceCollectionExtensionsTests`, `PersistentPolicyAdministrationPointTests`, `PersistentPolicyAdministrationPointAuditTests`, `ABACPolicySeedingHostedServiceTests`, `AuditedRepositoryTests`, `AuditedReadOnlyRepositoryTests`, `EncinaPropertiesTests`, `EncinaArbitrariesTests`; DSR/Consent/GDPR extractor and gate tests with Service and Anonymous identities; EF Core interceptor tests (accessor only). The end-to-end TestServer tests stay in UnitTests: they are the cheapest place for in-memory hosting and run in-process with no external service (location justified in the Integration row). |
| Guard | `Encina.GuardTests` | `Core/Identity/RequestIdentityGuardTests`, `ClaimsRequestIdentityFactoryGuardTests`, `RequestContextScopeFactoryGuardTests`, `ServiceIdentityRegistrationGuardTests`, `RequestContextGuardTests` (`WithIdentity`, `CreateAt`); `Security/ABAC/ABACPipelineBehaviorGuardTests` (new constructor); `Security/SecurityPipelineBehaviorGuardTests`, evaluator guards; `AspNetCore/EncinaContextMiddlewareGuardTests`; `Security/` guard tests for attributes, evaluators, health check, `ServiceCollectionExtensions` and `SecurityPipelineBehavior` sized to reach the Encina.Security 15% guard target; guards for the DSR/Consent/GDPR changes, `TestIdentity` and the circuit handler |
| Contract | `Encina.ContractTests` | `Core/RequestContextIdentityContractTests` (counts toward `Encina` `Core/RequestContext.cs`; a contract over `RequestContext` as produced by every factory and transformer: `Create`-replacement `CreateAt`, `CreateForTest`, `With*`, `ForNestedDispatch`, plus `TestInfrastructure`'s `TestRequestContext`; `Identity` non-null, `UserId == Identity.UserId`); `Core/RequestContextScopeFactoryContractTests` (identity creation only through the scope factory: `BeginPrincipal`, `BeginInbound`, `BeginRestored` honour `RequestIdentityOptions`; the middleware case sits in UnitTests because ContractTests does not reference `Encina.AspNetCore`; the factory is internal so no custom-factory contract exists; also restore-on-dispose, never throws for refusals); `Security/SecurityPipelineBehaviorContractTests` (counts toward the only contract-applicable Encina.Security file: Anonymous, User, Service and null-Identity denial); `Security/ABAC/ABACPipelineBehaviorContractTests` (updated). `Encina.ContractTests` already reaches `Encina.Security` transitively through `Encina.Security.ABAC`, so no ProjectReference is added. Evaluator contract tests are not written: they count toward no obligation. |
| Property | `Encina.PropertyTests` | `Core/RequestIdentityProperties` (generated `ClaimsPrincipal` inputs: null, unauthenticated with `sub`, authenticated without subject, `service:`/`SERVICE:`/` service:` subjects, two authenticated identities, valid users, mapped through `ClaimsRequestIdentityFactory`: `UserId != null ⇔ IsAuthenticated`; nested and non-LIFO scopes never leave a disposed identity ambient); validator properties (`RequestIdentityOptionsValidator`, `ServiceIdentityCatalogOptionsValidator`: these two count toward the `*Validator.cs` property flag); `Security/ABAC/ABACIdentityProperties` (crossed with Block/Warn modes: the PEP never calls the attribute provider or PDP for an unauthenticated identity; informational, since `ABACPipelineBehavior.cs` carries no property flag). The FsCheck `Arb` produces Anonymous and User only (documented). |
| Integration | `Encina.IntegrationTests` | **Three existing suites exercise the PAP actor** (found by searching `IPolicyAdministrationPoint`/`AddEncinaABAC`, not the class name): `Security/ABAC/PersistentPapScopeScenario.cs` and its ADO, Dapper and EF Core SQL Server users (real SQL Server, shared `[Collection("EFCore-SqlServer")]`-style fixtures, added by #1707/#1720). They are rewritten (Phase 4 task 9: accessor registered, no `ISecurityContextAccessor`, actor through the scope factory) and extended to run `ABACPolicySeedingHostedService` under the built-in identity and assert the persisted policy and the `service` audit actor; no parallel class is added. The feature has no SQL of its own, so the other providers add nothing; `Core/Identity/RequestIdentity.md` (AGENTS.md section 9 format) records that reason and why the TestServer end-to-end tests live in UnitTests (in-process hosting, no external service) |
| Load / Benchmark | `Encina.LoadTests`, `Encina.BenchmarkTests` | `Encina.LoadTests/Core/Identity/RequestIdentity.md` and `Encina.BenchmarkTests/Core/Identity/RequestIdentity.md`: the accessor is a static `AsyncLocal` shared by all requests, so the justification cites the concurrency unit tests instead of claiming "no shared state": N parallel TestServer requests with distinct principals each asserting its own user id, and parallel `RunAsServiceAsync` flows (both in UnitTests, stated in the file); benchmark: one holder allocation per request, not a hot path |

---

## Verification commands

Run from the worktree (`Set-Location` first). Every phase prompt refers to this block.

```powershell
# 1. Build, zero warnings
dotnet build Encina.slnx --configuration Release

# 2. Per-flag test runs WITH coverage (coverage-report takes the flag from the first folder under --input)
foreach ($flag in 'UnitTests','GuardTests','ContractTests','PropertyTests') {
  dotnet test "tests/Encina.$flag" -c Release --collect "XPlat Code Coverage" --results-directory "artifacts/test-results/$flag"
}
dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/test-results --output artifacts/coverage
dotnet run --file .github/scripts/coverage-report.cs -- --check-stale-manifest --check-missing-manifest

# 3. CRAP gate (exit code 0 required; same as CI)
New-Item -ItemType Directory -Force artifacts/crap-gate | Out-Null
git diff -U0 origin/main...HEAD > artifacts/crap-gate/diff.patch
dotnet run --file .github/scripts/crap-gate.cs -- --enforce --threshold 10 --diff artifacts/crap-gate/diff.patch @((Get-ChildItem artifacts/test-results -Recurse -Filter coverage.cobertura.xml).FullName)

# 4. Records, changelog, architecture
dotnet run .github/scripts/knowledge-records.cs -- --check
dotnet run --file .github/scripts/changelog-fragments.cs -- --check
dotnet test tests/Encina.UnitTests -c Release --filter "FullyQualifiedName~EncinaEventIdAllocationTests"

# 5. Acceptance sweeps (each returns nothing outside historical notes / plans)
Get-ChildItem src,docs -Recurse -File | Where-Object { $_.FullName -notmatch 'Encina\.(AzureFunctions|AwsLambda)' } | Select-String -Pattern 'ISecurityContext|SecurityContextAccessor|ThrowOnMissingSecurityContext|UserIdClaimType|WithUserId|IPrincipalResolver|security\.missing_context|abac\.missing_context'
Select-String -Path (Get-ChildItem src -Recurse -Filter *.cs).FullName -Pattern 'DateTime(Offset)?\.UtcNow' | Where-Object { $_.Path -match 'RequestContext\.cs|EncinaContextMiddleware|TenantResolutionMiddleware|Identity\\' }
Select-String -Path (Get-ChildItem src -Recurse -Filter *.cs).FullName -Pattern 'CreateForTest\(' | Where-Object { $_.Path -notmatch 'Encina\.Testing' }   # only the declaration
```

The integration test class of the Testing section runs through `dotnet run --file .github/scripts/run-integration-tests.cs` (Docker/Testcontainers).

## Documentation

All pages follow the encina-docs skill and the 2026-10-05 visual rule (#1759): a diagram or table per concept, no walls of text; one Diátaxis quadrant per page; no hand-typed coverage. Pages that document removed API are **fully** revised, not patched by line number.

- **`docs/features/security-authorization.md` — full revision of every section that uses removed API**: `:38` (SecurityContext concept row), `:148-149`, `:196-213` ("Security Context" section, claim types, `SecurityContext.Anonymous`), `:236-282` (custom `IPermissionEvaluator`/`IResourceOwnershipEvaluator` samples now taking `RequestIdentity`), `:296-309` (`ThrowOnMissingSecurityContext` and the four claim options), `:324` (`security.user_id` tag), `:349` (log 8004), `:359`, `:390` (`security.missing_context`), `:413-414`.
- `docs/architecture/request-pipeline.md:61` (`WithUserId`), `docs/features/audit-tracking.md:682-703` (EF interceptors read the accessor only; configure `RequestIdentityOptions`; `IRequestIdentityFactory` is internal and neither replaceable nor configurable), `docs/features/abac/reference/observability.md:177,183,189,363` (9091 text, 9096, system actor), `docs/features/abac/reference/cheat-sheet.md:220`, `docs/features/abac/advanced/security.md:418` ("startup seeding runs under UserId system"), `docs/features/abac/reference/persistent-pap.md:205,231`, `docs/INVENTORY.md:3390,5608-5617`, `src/Encina.Security/README.md:99,105` (error and EventId tables) and the Blazor/AspNetCore samples that show `IPrincipalResolver` or the removed options (find with the acceptance sweep over `docs`, `src` and `samples`).
- `changelog.d/1677-abac-pap-audit-fail-closed.security.md:1` ("startup seeding runs in an explicit, logged system-actor scope"): amend or state that #1705's `.changed.md` supersedes it.
- `docs/features/abac/quick-start.md:21-50` — full registration (`AddEncina`, `AddEncinaAspNetCore`, `AddEncinaABAC`; `UseAuthentication` → `UseEncinaContext`), "Requests without a user" pointing to the how-to; remove the #1705 line.
- `src/Encina.Security.ABAC/README.md:115,119,171` — identity from the request context; remove the #1705 sentence.
- `docs/features/abac/reference/errors.md:35,138-153` — `abac.unauthenticated_caller` (renamed from `abac.missing_context`) resolution: `UseEncinaContext` after `UseAuthentication`; `RunAsServiceAsync` for jobs without a request; deferred messages carry the originating actor (SPEC-002, P-50); no ABAC attributes on caller-less requests; the entry-point table (SignalR/subscriptions deny until F2).
- `docs/features/abac/xacml/architecture.md:138,149`, `docs/features/abac/advanced/advanced-topics.md:543`, `docs/features/abac/reference/persistent-pap.md` (service actors; #1704 item 2).
- `src/Encina.Security/README.md` — replace "3. Set Security Context" and claim options; attributes evaluate `IRequestContext.Identity`.
- `src/Encina.AspNetCore/README.md` and `src/Encina.AspNetCore.Blazor/README.md` — identity built by `UseEncinaContext` and the circuit handler, `RequestIdentityOptions` (the only customisation point; the identity factory is internal), ordering, removed options and `IPrincipalResolver`, interim SignalR behaviour; the `ServiceCollectionExtensions.cs:65` XML sample.
- New how-to: `docs/en/guides/SERVICE_IDENTITY.md` "Run background work with a service identity" (declare, scope, refusals, tenant rules, why a deferred message must not use it).
- New reference: "Request identity and claim mapping" (`docs/features/request-identity.md`), with the entry-point table, the multi-identity rules and the Tenancy claim-type note.
- `docs/plans/abac-decision-audit-implementation-plan-751.md` (see Phase 7); `docs/knowledge/issues/1676.md:18,65` and `docs/knowledge/issues/1677.md` (Phase 4).
- ADR-035 (`docs/architecture/adr/035-one-request-identity-model.md`); `docs/architecture/adr/index.md` (the reservation row is added by the plan PR; Phase 7 moves it).
- Changelog fragments (written by `mechanical-fixer`; `changelog-fragments.cs --check` exits 0): `changelog.d/1705-one-request-identity.security.md` (ABAC, Security and Authorization behaviors silently not registered whenever another behavior exists (#1635 subset, fail-open); anonymous substitution removed; external `service:` subjects rejected; service identities never turn compliance deny gates into allows; user id removed from Security tags and logs), `.fixed.md` (ABAC quick-start works; `AddEncinaAuthorization` registers its behavior), `.added.md` (`RequestIdentity`, `IRequestIdentityFactory`, `RequestIdentityOptions`, service identities, scope API incl. `BeginRestored`/`PersistedRequestIdentity`, Blazor circuit handler, `TestIdentity`), `.changed.md` (claim precedence sub-first; authenticated-without-subject is anonymous; authenticated identities only; `ExcludeSystemAccess` semantics; `abac.missing_context` renamed `abac.unauthenticated_caller`; PAP audit `actor` is `user`/`service` and seeding records `service:encina.abac.policy-seeding` instead of `system`; `[Authorize]` evaluates the request identity; middleware timestamps from `TimeProvider`; `CreateForTest` takes an identity), `.removed.md` (`ISecurityContext`, `ISecurityContextAccessor`, `SecurityContext`, `IPrincipalResolver`, claim options, `ThrowOnMissingSecurityContext`, `security.missing_context`, `WithUserId`, `IRequestContext.UserId` as an interface member, `RequestContext.Create(string)`, EF interceptor `IRequestContext` fallback).

---

## Research

### Standards and specifications

| Standard | Relevance |
|---|---|
| OpenID Connect Core 1.0 §5.1 (`sub`) / RFC 7519 §4.1.2 | `sub` is the stable subject identifier; first in `UserIdClaimTypes` |
| XACML 3.0 (subject category) / NIST SP 800-162 | The PEP must present one authenticated subject to the PDP |
| GDPR Art. 5(2) accountability, Art. 32 | Authorization and audit must name the same actor; service actors are distinguishable |
| OWASP ASVS V4.1 (access control fails securely) | Anonymous denies; no implicit identity for background work |

### Existing infrastructure to leverage

| Component | Location | Usage |
|---|---|---|
| `AmbientRequestContext.Resolve/Enter` | `src/Encina/Core/AmbientRequestContext.cs:47-170` | Seeding and restore pattern for scopes |
| `RequestContextAccessor` (static AsyncLocal) | `src/Encina/Core/RequestContextAccessor.cs:21-30` | The single ambient channel |
| `EncinaContextMiddleware` | `src/Encina.AspNetCore/EncinaContextMiddleware.cs` | HTTP entry point |
| `SecurityContext` role/permission extraction | `src/Encina.Security/SecurityContext.cs:38-69` | Ported into `ClaimsRequestIdentityFactory` |
| `PolicyChangeActorScope` | `src/Encina.Security.ABAC/Administration/PolicyChangeActorScope.cs` | Replaced by the built-in service identity |
| `HttpContextAccessor` holder pattern | ASP.NET Core | Model for the holder-based `RequestContextAccessor` |
| SPEC-002 REQ-015 / DEC-011, P-50 (#1164) | `docs/specifications/SPEC-002-eu-regulatory-readiness.md:281,466,592,759` | Owner of deferred-dispatch identity; consumes `BeginRestored` |
| `RequestContextPropagationTests` | `tests/Encina.UnitTests/AspNetCore/RequestContextPropagationTests.cs` | TestServer pattern |

### Event ID allocation

| Package | Range | Ids | Notes |
|---|---|---|---|
| Encina (core) | `Core` 100-199 | 162-171 (new, packed: 162-165 Phase 1; 166-171 Phase 2) | 100-161 used (verified); no new range, `AssemblyRanges` already maps `Encina` → `Core` |
| Encina.AspNetCore | `AspNetCore` 200-249 | none | Identity logs live in core factory/scope |
| Encina.Security.ABAC | `SecurityABAC` 9000-9099 | 9096 repurposed (`SeedingServiceIdentityStarted`), 9085 allocated early for the Disabled-mode warning (#751's planned id; #751 reuses it) | 9094, 9095, 9097 (PAP) unchanged; 9098-9099 remain free |
| Encina.Security | `Security` 8000-8009 | 8004 removed; 8001/8002 templates lose the user id | 8004-8009 free afterwards |

### Estimated file count

| Category | Files |
|---|---|
| New production (core Identity folder, Blazor handler, `TestIdentity`) | ~20 |
| Changed production (core, AspNetCore, Blazor, Security, ABAC, Audit, EF Core, Compliance x3, Tenancy.AspNetCore, Testing, FsCheck) | ~40 |
| Changed production: repositories losing `IRequestContext? requestContext = null` (ADO x12, Dapper x12, MongoDB x6, plus EF Core repositories) | ~30+ |
| Deleted production | 8 (`ISecurityContext`, `ISecurityContextAccessor`, `SecurityContext`, `SecurityContextAccessor`, `PolicyChangeActorScope`, `IPrincipalResolver` + default, `AuthenticationStatePrincipalResolver`) |
| New/changed tests | ~65 (incl. 44 files with `UserId.Returns`) |
| Docs, ADR, changelog, issue files | ~30 |

---

## Combined AI Agent Prompt

<details>
<summary><strong>All phases</strong></summary>

```text
PROJECT CONTEXT
Encina, .NET 10 / C# 14, pre-1.0 (best design, breaking changes welcome, no shims). Worktree
D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan docs/plans/security-context-population-implementation-plan-1705.md.
AGENTS.md binds: PowerShell only; fail closed with explicit logged opt-outs; registration completeness proven by
ValidateOnBuild+ValidateScopes DI tests; ROP; TimeProvider; [LoggerMessage] in registered ranges; per-flag coverage; CRAP <= 10.

IMPLEMENTATION OVERVIEW
1. Core RequestIdentity on IRequestContext (UserId = C# 14 extension projection), one claim map (RequestIdentityOptions), IRequestIdentityFactory,
   PersistedRequestIdentity; ForUser internal; holder-based accessor.
2. Declared service identities + IRequestContextScopeFactory (Either; refuses over a user and over inbound requests; tenant rules;
   BeginRestored for SPEC-002/P-50; invalidates on dispose; RunAsServiceAsync).
3. EncinaContextMiddleware builds the identity, TimeProvider timestamps, origin inbound; AuthorizationPipelineBehavior on Identity.Principal,
   IPrincipalResolver deleted, Blazor circuit handler, EF interceptor fallback removed, AddEncinaAuthorization registers its behavior.
4. ABAC PEP/PAP/seeding read context.Identity; TryAddEnumerable; PolicyChangeActorScope deleted; abac.unauthenticated_caller; 9096/9085.
5. Encina.Security deletes ISecurityContext/Accessor/SecurityContext/claim options; behaviors and evaluators on RequestIdentity;
   user id leaves tags/logs; DSR/Consent/GDPR extractors fall back only for Kind == User.
6. End-to-end TestServer tests, contract/property tests, manifests, coverage per flag, CRAP and knowledge/changelog checks
   (the "Verification commands" block).
7. Docs (full pages), ADR-035, changelog fragments (incl. .security), #751 plan update, follow-up issue files F1, F2, F3, F5, F6.

KEY PATTERNS
- Gates: `context.Identity is { IsAuthenticated: true } identity` (null denies). A service identity never turns a deny into an allow.
- Every phase builds green with 0 warnings before the next starts.
- EventIds: core 162-171 packed; ABAC 9096 repurposed and 9085 (Disabled warning) allocated.
- Deferred dispatch is SPEC-002 REQ-015 / DEC-011 / P-50 (#1164): rebuild the originating actor, never a service identity.

REFERENCE FILES
src/Encina/Abstractions/IRequestContext.cs; src/Encina/Core/{RequestContext,AmbientRequestContext,RequestContextAccessor}.cs;
src/Encina.AspNetCore/EncinaContextMiddleware.cs; src/Encina.Security/{SecurityPipelineBehavior,SecurityContext,ServiceCollectionExtensions}.cs;
src/Encina.Security.ABAC/{ABACPipelineBehavior,ServiceCollectionExtensions,ABACPolicySeedingHostedService}.cs;
src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs; src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Identity is built once per request/scope; nothing read benefits from a cache. #707's "cache SecurityContext per scope" item becomes moot (orchestrator comments on #707). |
| 2 | OpenTelemetry | ✅ | Activity tag `encina.identity.kind` (anonymous/user/service) on the dispatch activity (`EncinaDiagnostics.SendStarted` receives the kind; core `ActivityTagNames` constant; unit test) and when a scope opens; the `security.user_id` tag is removed; never the user id. |
| 3 | Structured Logging | ✅ | Core 162-171 (`[LoggerMessage]`), Security 8004 removed and 8001/8002 without user id, ABAC 9091 renamed, 9096 repurposed, 9085 allocated (Disabled warning). |
| 4 | Health Checks | ✅ | `SecurityHealthCheck` drops the accessor check (no external dependency added). |
| 5 | Validation | ✅ | `RequestIdentityOptionsValidator` and `ServiceIdentityCatalogOptionsValidator` with `ValidateOnStart`. |
| 6 | Resilience | ❌ | No external calls. |
| 7 | Distributed Locks | ❌ | No shared state; AsyncLocal per flow. |
| 8 | Transactions | ❌ | No multi-operation writes. |
| 9 | Idempotency | ❌ | Identity does not change idempotency keys; nested dispatch already clears the key. |
| 10 | Multi-Tenancy | ✅ | Tenant stays on `IRequestContext`; one tenant claim map for the middleware and `BeginPrincipal`; scopes never inherit the ambient tenant (Design 3, tests per rule). `Encina.Tenancy.AspNetCore`'s `ClaimTenantResolver` and the serverless options keep their own claim type until F5/F3 (documented). Header fallback surface → F5. |
| 11 | Module Isolation | ❌ | `ModuleId` (metadata) untouched; scopes keep ambient metadata only via correlation. |
| 12 | Audit Trail | ✅ | PAP, Security.Audit, repositories, EF interceptors and #751 read the same identity; service actors recorded as `service:<name>`; PAP audit `actor` = `user`/`service`; `ExcludeSystemAccess` semantics updated. |

---

## Dependencies on open issues

| Issue | Relation |
|---|---|
| #1635 (open-generic `TryAddTransient`) | ABAC, Security and AspNetCore authorization subset delivered here; orchestrator updates #1635 to say so. Remaining ~19 packages stay there. |
| SPEC-002 REQ-015 / DEC-011, P-50 (#1164) | Owns deferred dispatch (outbox, inbox, scheduler); #1705 delivers `PersistedRequestIdentity` and `BeginRestored`; the orchestrator comments the contract on #1164. |
| #751 (ABAC decision audit, plan merged) | Should start after #1705. Its plan is updated in Phase 7 (lines :194, :217, :322, :382, :394, :549, :753). |
| #1678 (named pipeline stages) | Not blocking: ABAC and Security deny independently, order irrelevant for fail-closed. |
| #1674 (audit store coherence spike) | Not blocking; `IdentityKind` becomes available for audit rows. |
| #1704 (persistent PAP debt) | Item (2) resolved: jobs change policies under a declared service identity; persistent-pap.md updated. Orchestrator comments on #1704. |
| #1707 (PAP captures scoped `IPolicyStore`) | Merged as 6ba311d6 (PR #1720); the branch is rebased on it and Phase 4 references are refreshed to the post-#1720 file. |
| #707 (security context caching) | Its SecurityContext item becomes obsolete; orchestrator comments. |
| #1306 (authorization docs miss `IPrincipalResolver`) | Resolved by deleting `IPrincipalResolver`; GraphQL subscriptions part stays in F2. |
| #1201 (PII role-aware redaction) | Will read `context.Identity.Roles`; no conflict. |

Open PRs checked on 2026-10-03 (#1718, #1713, #1527): none touches the files of this plan; #1720 has since merged (6ba311d6) and the plan is rebased on it.

---

## Maintainer decisions

Taken (2026-10-05, not to be revisited):

1. Deferred dispatch follows SPEC-002 REQ-015 / DEC-011 and P-50 (#1164): the originating actor is persisted and rebuilt, never replaced by a service identity; F1 covers only jobs with no originating request; F4 is dropped; the persisted form and the validated, logged `BeginRestored` are defined in Design 1.
2. Fail closed everywhere: a service identity never turns a deny gate (DSR restriction gate included) into an allow; anonymous and unattributable identities deny at security and compliance gates.
3. `UserId` is a projection of `Identity` by construction (extension property).
4. No claim value or user id reaches logs, activity tags or `ToString`; tests prove it.
5. `IPrincipalResolver` and the EF Core interceptor identity channel are removed or folded into the single model in this plan.
6. Scopes: AsyncLocal holder with explicit invalidation, defined out-of-order disposal and tenant binding, each with tests.
7. Per-flag targets with justification for every new or touched file, CRAP <= 10 with the local command, shared collection fixtures, no step relies on a suite that does not exist.
8. Documentation covers every page and sample that documents removed API and follows the visual rule (#1759).
9. ADR-035 "One request identity model" is reserved.

Everything else follows from AGENTS.md (pre-1.0 best design, fail closed with explicit logged opt-outs, registration completeness, pay-for-what-you-use) and was ranked by three independent design reviews; the user-visible consequences (sub-first claim order, authenticated-without-subject becomes anonymous, `abac.unauthenticated_caller`, deferred dispatch anonymous until P-50, deleted `ISecurityContext`/`IPrincipalResolver` API) are recorded in ADR-035 and the changelog fragments.

---

## Review resolution (2026-10-05)

Source: `artifacts/plan-review-1705/findings.json` (63 findings: 1 blocker, 37 major, 25 minor as listed there). Every finding was re-verified against the plan text and the code (Select-String over the rebased worktree, origin/main 10ac10b4) because part of the refutation was cut by the usage limit. Result: 60 applied, 3 applied in part (rows 30, 38, 56; rejected parts stated). None rejected whole. The PR #1775 review then reopened rows 3, 45, 53 and tightened row 14; see the "PR review (2026-10-05)" sub-table below. "Where" names the plan section changed.

| # | Sev | Finding | Result | Where |
|---|---|---|---|---|
| 1 | major | Deleting `PolicyChangeActorScope` breaks knowledge-records; record 1677 missing | Applied (verified `1677.md:46,75`) | Phase 4 task 4, Phase 7 task 5 |
| 2 | major | Seeding system-actor leftovers (9096, `IsSystem`, docs, fragment) | Applied | Design 3 PAP bullet, Phase 4 tasks 3, 5, 6, Documentation |
| 3 | major | Existing PAP and seeding unit suites missing; integration suites do not exist | Reopened by PR review M1: the first check was wrong (searched the class name); the PAP unit and integration suites are all listed now | Phase 4 task 9, Testing |
| 4 | major | Existing Security.Audit `ExcludeSystemAccess` tests and `ReadAuditOptions` docs missing | Applied | Phase 5 tasks 9, 11 |
| 5 | major | Phase 3 skips existing AspNetCore tests and the XML sample | Applied | Phase 3 tasks 2, 8 |
| 6 | major | `security-authorization.md` has 9 affected sections | Applied | Documentation (full revision) |
| 7 | major | EF interceptors keep a second channel | Applied (fallback removed, doc rewritten) | Summary, Phase 3 task 6, Consumer changes |
| 8 | minor | ABAC attributes derive from `SecurityAttribute`; reference cannot go | Applied (task 7 inverted: reference stays) | Design 5, Design 6, Phase 4 task 7 |
| 9 | minor | ABAC error message, XML docs and observability page still say "security context" | Applied | Design 6, Phase 4 tasks 1, 6, Documentation |
| 10 | minor | FsCheck property `WithUserIdCreatesNewContext` and wrong references | Applied | Phase 1 task 9 |
| 11 | minor | Other pages and the `IRequestPreProcessor` sample | Applied | Phase 1 task 5, Phase 7 task 7, Documentation |
| 12 | minor | PublicAPI removals are in Unshipped; ABAC ctor line | Applied | Phase 1 task 11, Phase 3 task 7, Phase 4 task 8, Phase 5 prompt |
| 13 | major | `UserId` independent interface member | Applied (extension property) | Design 1, Summary |
| 14 | major | Public unlogged identity creation bypasses declarations | Applied, tightened by PR review M3 (identity only through the scope factory; factory and `CreateAt` internal; explicit-context rule logs 165) | Design 1, Design 6 |
| 15 | major | `AuthorizationPipelineBehavior` keeps `IPrincipalResolver` | Applied (option a: deleted, Blazor handler pulled in) | Design 2, Phase 3 tasks 3-4 |
| 16 | major | F1/F4 conflict with SPEC-002 REQ-015/DEC-011/P-50 | Applied (cited; F1 narrowed; F4 dropped; persisted form and `BeginRestored`) | Header, Design 1, Design 3, Design 8 |
| 17 | major | #751 plan premise false; order decision ignored | Applied | Design 5, Design 7, Phase 7 task 4 |
| 18 | major | Remnants of system actor and "security context" wording | Applied; `abac.missing_context` renamed `abac.unauthenticated_caller` on the merits | Design 6, Phase 4 |
| 19 | minor | Documentation list misses pages | Applied | Documentation, Phase 7 task 8 |
| 20 | minor | Stale references; ABAC to Security reference; missing test files | Applied (rebased on #1720; refs refreshed) | Phase 4, Dependencies |
| 21 | blocker | Service identities turn the DSR restriction gate into an allow | Applied (fix pulled into #1705; F7 removed) | Design 6, Phase 5 task 12, Phase 6 task 2 |
| 22 | major | Public unlogged ways to mint or swap identity | Applied (four rules a-d) | Design 1, Design 3, Design 6 |
| 23 | major | Ambient identity outlives scope (plain AsyncLocal) | Applied (holder pattern, tests) | Design 2, Phase 2, Testing |
| 24 | major | Out-of-order disposal resurrects an identity | Applied | Design 2, Design 3 rules, Phase 2 |
| 25 | major | Any code can open the built-in seeding identity | Applied (`encina.` reserved, internal `BeginBuiltIn`, actor kind) | Design 3, Phase 4 |
| 26 | major | Authorization authorizes principals mapped to Anonymous | Applied | Design 2, Design 6, Phase 3 task 9 |
| 27 | major | Scopes open inside anonymous inbound requests | Applied (internal typed origin flag, refusal, opt-in) | Design 3 rules, Phase 2 |
| 28 | major | Tenant binding unspecified | Applied (`tenant_conflict`, never inherit) | Design 3 rules |
| 29 | major | Several `ClaimsIdentity` instances | Applied (authenticated only; conflict maps to Anonymous, EventId 164) | Design 4 |
| 30 | major | Security gate "deny anonymous" not true for empty lists / unknown attributes | Applied in part: anonymous pre-check and constructor guards applied; "deny unknown attribute types" rejected for authenticated callers because ABAC attributes derive from `SecurityAttribute` and are enforced by the PEP (the pre-check already closes the anonymous hole) | Design 6, Phase 5 task 2 |
| 31 | major | User id still reaches tags, logs, `ToString` | Applied | Design 4, Phase 1 task 6, Phase 5 task 2 |
| 32 | major | `UserId` not enforced as projection | Applied (same change as 13) | Design 1 |
| 33 | minor | Reserved `service:` check case and whitespace | Applied | Design 1, Design 4 |
| 34 | minor | `Disabled` mode not logged | Applied (EventId 9085, reusing #751's planned id; 9098 dropped by the PR review) | Design 6, Phase 4 task 6 |
| 35 | major | Security behavior keeps logging and tagging the user id | Applied | Phase 5 task 2 |
| 36 | major | Dispatch-activity identity tag has no task or test | Applied | Phase 1 task 7, Testing, Matrix #2 |
| 37 | major | Tenant and metadata behavior of scopes undefined | Applied | Design 3 rules, Testing |
| 38 | major | Non-HTTP entry-point coverage incomplete (gRPC, GraphQL claim, dispatchers) | Applied in part: entry-point table, corrected GraphQL/gRPC classification and dispatcher list added; dedicated gRPC/GraphQL TestServer tests replaced by the stated justification (both call `IEncina.Send` inside the HTTP pipeline) | Summary entry-point table, Design 2 |
| 39 | major | `IPrincipalResolver` remains a third channel; Blazor drift | Applied (same change as 15) | Design 2, Phase 3 |
| 40 | major | `AddEncinaAuthorization` does not register the behavior | Applied (verified `ServiceCollectionExtensions.cs:164-196`) | Summary, Design 5, Phase 3 task 3 |
| 41 | minor | PAP `actor` metadata and test | Applied | Phase 4 tasks 3, 9 |
| 42 | minor | EventId 9096 stale | Applied (repurposed) | Phase 4 task 5, EventId table |
| 43 | minor | Matrix #10 claims one tenant claim map | Applied | Matrix #10, Design 4, F5/F3 |
| 44 | minor | DI tests do not cover seeding chain; uniqueness validator | Applied | Design 3, Phase 4 task 9 |
| 45 | minor | Integration suites do not exist; Audit not in targets | Audit targets applied; the "no suite" premise was wrong (PR review M1): `PersistentPapScopeScenario` and its three SQL Server users exist and are rewritten | Summary, Phase 4 task 9, Testing |
| 46 | minor | More `security-authorization.md` sections | Applied (same as 6) | Documentation |
| 47 | minor | `RequestContext.Create(string)` reads `UtcNow` | Applied (deleted, `TenantResolutionMiddleware` on `TimeProvider`) | Summary, Phase 1 task 6, Phase 3 task 5 |
| 48 | major | Consumer tests stubbing `UserId` missing | Applied (checkable `UserId.Returns` acceptance) | Phase 1 task 10, Phase 4 task 9, Phase 5 task 11, Testing |
| 49 | major | Per-flag coverage skips packages; command gives no per-flag data | Applied | Testing, Verification commands |
| 50 | major | Encina.Security contract flag has no planned test | Applied | Testing (Contract row) |
| 51 | major | PAP `actor` metadata replacement unspecified | Applied | Design 3, Phase 4 task 9 |
| 52 | major | No test proves ids and claims stay out of logs/tags | Applied (sentinel-value tests) | Testing rules |
| 53 | major | Integration row relies on suites that do not exist | Corrected by PR review M1: the existing shared-fixture scenario is extended instead of a new class; justification file kept | Testing (Integration row) |
| 54 | major | CRAP gate has no runnable command | Applied | Verification commands |
| 55 | minor | Coverage-manifest upkeep | Applied | Phase 6 task 5 |
| 56 | minor | PublicAPI steps point at wrong files | Applied in part: Unshipped targets and ABAC line applied; the claim that `Encina.Testing.FsCheck` is untracked is not fully accurate (it has `PublicAPI.Shipped.txt`), so the step is "verify the csproj, edit only if tracked" | Phase 1 task 11 |
| 57 | minor | `.security` changelog fragment | Applied | Documentation |
| 58 | minor | ADR-035 reservation row | Applied (row committed; branch rebased) | Header, Phase 7 task 2 |
| 59 | minor | "Every implementation" contract test unreachable | Applied | Testing (Contract row) |
| 60 | minor | Property domain vacuous; Arb cannot produce services | Applied | Testing (Property row) |
| 61 | minor | Load justification ignores the static AsyncLocal | Applied | Testing (Load row), Phase 3 task 9 |
| 62 | minor | No mutation coverage for `Identity/` | Applied as an orchestrator step (hot spot) | Design 8 |
| 63 | minor | TimeProvider and production `CreateForTest` acceptances not checkable | Applied | Verification commands sweeps, Phase 1 task 6 |

### PR review (2026-10-05)

Source: `artifacts/pr-review/1775.md` (verdict merge after fixes; 5 major, 6 minor, 5 nit). Inventory greps were re-run after rebasing on 1deeaada: `UserId.Returns` 67 sites in 44 files, `ISecurityContextAccessor` in 11 test files, `Substitute.For<IRequestContext>` 397 calls in 176 files, `CreateForTest(... userId ...)` 55 of 344, `IRequestContext? requestContext = null` 31 in 30 files.

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| M1 | major | Three PAP integration suites and `PersistentPolicyAdministrationPointScopeTests` exist and are missing | Applied; rows 3, 45, 53 corrected; the new parallel integration class is dropped, the existing scenario is extended | Summary, Phase 4 task 9, Testing (Integration row) |
| M2 | major | Repositories take `IRequestContext` by constructor (second channel) | Applied (31 parameters removed, ambient accessor, `ExcludeSystemAccess` on ambient identity) | Phase 5 task 14, Consumer changes |
| M3 | major | Public unlogged mint path (factory `Create` + `CreateAt` + explicit `Send`) | Applied (identity only through the scope factory; `IRequestIdentityFactory` internal; `CreateAt` internal; `BeginInbound`; logged explicit-context rule; architecture test) | Summary amendments, Design 1, 2, 4, Phases 1-3 |
| M4 | major | `BeginRestored` trusts persisted roles and permissions | Applied (`PersistedRequestIdentity` = SPEC-002 REQ-015 fields with causation id; restored User has no roles or permissions; note for #1164) | Design 1 |
| M5 | major | User id still in log 1702 and `SecurityErrors` details | Applied (tasks and sentinel tests) | Phase 5 task 15 |
| m1 | minor | 9098 duplicates #751's 9085 | Applied (9085, #1705 lands first, #751 reuses) | Design 6, Phase 4, EventId table |
| m2 | minor | Unrunnable `Select-String -Recurse`; `UserIdClaimType` sweep and nonexistent `samples` | Applied | Phase 4 task 9, Phase 5 task 13, Phase 7 task 8, Verification commands |
| m3 | minor | Stale `abac.missing_context` text; Phase 3 prompt "restore in finally" | Applied | Testing, Phase 3 prompt |
| m4 | minor | `InternalsVisibleTo` for `Encina.Testing.FsCheck`; unnecessary ContractTests reference | Applied | Summary amendments, Testing (Contract row) |
| m5 | minor | Origin marker is a spoofable metadata string | Applied (internal typed `RequestOrigin`; SignalR note kept for F2 and the how-to) | Summary amendments, Design 3, Phase 1 task 6 |
| m6 | minor | EventIds 164-167 left empty after Phase 1 | Applied (162-165 Phase 1, 166-171 Phase 2) | Phase 1 task 7, Phase 2 task 8, EventId table |
| n1 | nit | "61 applied, 2 in part" | Applied (60 / 3) | Review resolution intro |
| n2 | nit | Counts drifted | Applied (67 in 44; 11 files; 397 in 176) | Design 1, Phase 1 task 10 |
| n3 | nit | Phase 1 breaks middleware callers before Phase 3 | Applied (minimal compile fix in Phase 1) | Phase 1 task 6 |
| n4 | nit | `Resolve` signature change | Applied (`Either<EncinaError, IRequestContext>`, callers map the `Left`) | Design 1 |
| n5 | nit | Plan file name still says "security-context" | Applied by decision: kept; ADR-035 cites it as its plan | Phase 7 task 2 |
