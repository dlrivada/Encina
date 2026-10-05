# Implementation Plan: One Request Identity — the caller identity becomes part of `IRequestContext`

> **Issue**: [#1705](https://github.com/dlrivada/Encina/issues/1705)
> **Type**: Bug (design-level fix; planned with the feature prompt because it reshapes public API in five packages)
> **Complexity**: High (7 phases, no database provider matrix, ~100 production files touched across 20+ packages incl. the 30 repository files, ~100 test files)
> **Estimated Scope**: ~2,700-3,400 lines of production code changed or added (of which ~1,000 deleted, including the 31 repository constructor parameters in 30 files) + ~3,500-4,500 lines of tests (about 100 production files touched in total)
> **ADR**: ADR-035 "One request identity model" (number reserved in the Reserved numbers table of `docs/architecture/adr/index.md` by the plan PR; file `docs/architecture/adr/035-one-request-identity-model.md` is written in Phase 7)
> **Specifications**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-015 / DEC-011 (the originating actor is persisted with deferred messages and rebuilt at dispatch) and P-50 ([#1164](https://github.com/dlrivada/Encina/issues/1164)) own deferred dispatch; this plan only provides the identity API they need (Design 1, "Persisted form")
> **Review status**: the 2026-10-04 adversarial review (63 findings) and the PR #1775 review are resolved in "Review resolution (2026-10-05)" at the end of this file. The "PR review amendments" table in the Summary is normative: where an older Design, Phase or Testing sentence conflicts with it, the amendments table wins.
> **Phase 2 scope shape (2026-10-05)**: the design record is PR [#1849](https://github.com/dlrivada/Encina/pull/1849) (`docs/plans/request-identity-phase2-scope-shape-1705.md`, not edited after the decision). Its section 7, the maintainer decisions on #1705 (C1, Q1-Q4, the later "Decision update" winning) and both reviews of #1849 are applied here as amendment **M6 (scope shape)**; "Review log (Phase 2 scope shape)" at the end maps each finding to its change. Phase 1 (#1824, 13a1a493) is merged; the `src/` citations added or changed by this update (M6, Designs 1-3, Phases 2-3) were re-checked on `origin/main` 61d5dc3d. Older citations in the Summary, Phase 1 and Phases 4-7 describe the pre-Phase-1 code they were written against.
> **Adversarial review of PR #1862 (2026-10-05)**: majors 1-5, minors 6-15 and the maintainer decisions D1-D4 are applied; the `src/` citations they touch were re-checked on `origin/main` ab4ec134. "Review log (PR #1862 adversarial review)" at the end maps each finding to its change.

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
| (3) Service identity for jobs and handlers | Public core API: service identities declared at startup (`AddEncinaServiceIdentity`), opened through `IRequestContextScopeFactory` (`RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync`), which return `Either`, refuse to replace an ambient user identity or run over an inbound request, invalidate the scope when the work completes (holder-based accessor) and log every opening. Without a scope a dispatch stays anonymous and is denied (fail closed). A service identity never turns a deny gate into an allow (Design 6, DSR/Consent/GDPR extractors). |
| (4) Docs show the full registration | Quick-start, READMEs, error reference, architecture page, new how-to and ADR-035. |

Two verified hazards on the quick-start path are fixed in the same issue:

- **#1635 subset (fail-open):** `AddEncina` registers configured behaviors with `TryAddEnumerable` (`src/Encina/Core/EncinaConfiguration.cs:239-241`), and both `AddEncinaSecurity` (`src/Encina.Security/ServiceCollectionExtensions.cs:81`) and `AddEncinaABAC` (`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs:142`) use `TryAddTransient(typeof(IPipelineBehavior<,>), ...)`, which is silently skipped once any `IPipelineBehavior<,>` descriptor exists. The documented stack could run with **no ABAC at all**. Both move to `TryAddEnumerable` here (the #751 plan's Phase 4 had claimed this subset; it is pulled forward and removed from #751).
- **`RequestContext.CreateForTest` in production and `UtcNow` reads (fixed in Phase 1, #1824):** `EncinaContextMiddleware.cs:84` stamps contexts with `DateTimeOffset.UtcNow` through `CreateForTest` (`RequestContext.cs:151`), and `RequestContext.Create(string)` (`:125`, used by `TenantResolutionMiddleware.cs:129-131`) does the same. Both production paths move to the public TimeProvider-based `CreateAt`; `Create(string)` is deleted; `CreateForTest` no longer mints authenticated identities (Design 1).
- **`AddEncinaAuthorization` registers no behavior** although its XML doc says it does (`Encina.AspNetCore/ServiceCollectionExtensions.cs:145-146,164-196`; only `EncinaConfiguration.AddAuthorization` at `:116` registers it). A host that follows the documented call runs `[Authorize]` requests with no gate. Fixed in Phase 3 with `TryAddEnumerable` and a DI test.

**Affected packages**: `Encina` (core), `Encina.Security`, `Encina.Security.ABAC`, `Encina.AspNetCore`, `Encina.AspNetCore.Blazor`, `Encina.Security.Audit`, `Encina.EntityFrameworkCore` (interceptor fallback), `Encina.Compliance.DataSubjectRights`, `Encina.Compliance.Consent`, `Encina.Compliance.GDPR` (data-subject extractor fallbacks), `Encina.Tenancy.AspNetCore` (`TimeProvider`), `Encina.Testing`, `Encina.Testing.FsCheck`.

**Provider category**: none. No store, no SQL, no transport or cache code changes. Repositories (ADO, Dapper, MongoDB, EF Core) change only by losing their optional `IRequestContext` constructor parameter (PR review M2). The PAP actor is exercised by existing SQL Server integration suites (`PersistentPapScopeScenario` and its ADO, Dapper and EF Core users) that are rewritten; see the Testing section.

### Entry-point coverage (how identity reaches every host)

| Entry point | Mechanism | Owner / test |
|---|---|---|
| ASP.NET Core controllers, minimal APIs | `UseEncinaContext()` after `UseAuthentication()` | #1705, TestServer end-to-end |
| gRPC (`GrpcMediatorService`) and GraphQL queries/mutations over HTTP (`GraphQLMediatorBridge`) | Same middleware: both dispatch through `IEncina` inside the HTTP pipeline | #1705, covered by the middleware tests; a dedicated gRPC/GraphQL test is not added because both bridges call `IEncina.Send` without an explicit context (verified `GrpcMediatorService.cs:47-67`, `GraphQLMediatorBridge.cs:55,97`) and the harnesses would add packages; stated here, not silently skipped |
| Blazor Server circuits | `RequestIdentityCircuitHandler` in `Encina.AspNetCore.Blazor` (Phase 3): one `RunInboundAsync` per circuit activity, from the current `AuthenticationState` | #1705, unit test with a fake `AuthenticationStateProvider`, TestServer test over WebSockets |
| SignalR hub invocations, GraphQL subscriptions, raw WebSocket endpoints (connection flows) | Not populated: the middleware binds no identity on `HubMetadata` endpoints, on every WebSocket upgrade request (detected from the request itself, before `UseWebSockets` runs: Design 2 step 1) and on every request that sends `Accept: text/event-stream`, and runs the connection under an anonymous connection-origin marker (M6, MQ-1 (c)), so dispatches see **Anonymous on every transport and are denied** (WebSockets and SSE in any pipeline order; long polling when `UseEncinaContext` runs after `UseRouting`, otherwise the first negotiate request logs Critical 202 and every later request is answered 500, MQ-1 (b) with the D1 trigger); streaming hub methods included in F2; a test pins this interim behaviour | F2 |
| Ordinary SSE endpoints (`TypedResults.ServerSentEvents`, hand-written `text/event-stream` responses) | The request runs under the anonymous connection marker (MQ-1 (c)); the endpoint opts in **per event or per dispatch** by building `InboundRequestInfo` from the `HttpContext` with the public `CreateInboundRequestInfo()` and calling `RunInboundAsync` around that one unit of work, never around the whole stream (D2) | #1705 (Phase 3 task 11), TestServer test |
| Azure Functions, AWS Lambda | Own claim options today; move to `IRequestIdentityFactory` (GCP gap noted, SPEC-000 DEC-003) | F3 |
| Outbox, Inbox, Scheduling, dead-letter replay (`RuntimeTypeRequestDispatcher.cs:75`), Saga/RoutingSlip runners | Originating actor persisted and rebuilt through `RunRestoredAsync`, one scope per message (Design 1) | SPEC-002 REQ-015 / DEC-011, P-50 (#1164) |
| Recurring Hangfire/Quartz jobs, CDC bridge, compliance monitors (no originating request) | Declared service identity through `RunAsServiceAsync` | F1 |
| Transports (10) | Identity metadata propagation is part of the P-50 persisted form; no transport code in #1705 | P-50 (#1164) |
| `TenantResolutionMiddleware` without `UseEncinaContext` | Creates an anonymous context with `RequestContext.CreateAnonymousAt(TimeProvider)` | #1705 (Phase 3) |

### PR review amendments (2026-10-05, normative: they win over any older wording below)

Review of PR #1775 (`artifacts/pr-review/1775.md`, verdict merge after fixes). Maintainer decisions M1-M5 and the minors are applied to the Designs and Phases below and summarised here so a worker reads one list. **M6 (scope shape)** comes from the Phase 2 scope-shape decision (PR #1849, 2026-10-05); it is named so to keep it apart from the lowercase `m6` (EventIds), and it wins over M3, M4, m5 and m6 where they differ.

| # | Rule | Replaces |
|---|---|---|
| M1 | Three PAP integration suites exist (`tests/Encina.IntegrationTests/Security/ABAC/PersistentPapScopeScenario.cs`, used by `ADO/PersistentPapAdoSqlServerRegistrationTests.cs`, `Dapper/PersistentPapDapperSqlServerRegistrationTests.cs`, `EFCore/PersistentPapEFCoreSqlServerRegistrationTests.cs`, real SQL Server, `[Collection("EFCore-SqlServer")]`) plus `UnitTests/Security/ABAC/Persistence/PersistentPolicyAdministrationPointScopeTests.cs`. They substitute `ISecurityContextAccessor` and call `context.UserId.Returns(...)` on a substituted `IRequestContext`; they are rewritten (Phase 4 task 9). The planned `PolicySeedingServiceIdentityIntegrationTests` is dropped: the scenario is extended instead. | "no integration suite exists" (Summary, Testing, rows 3, 45, 53) |
| M2 | No second channel in repositories: the optional `IRequestContext? requestContext = null` constructor parameter is removed from all 31 sites (30 files); they read the ambient context through `IRequestContextAccessor` (Phase 5 task 14). `AuditedRepository`/`AuditedReadOnlyRepository` do the same and `ExcludeSystemAccess` reads that ambient `Identity`. | "no code change" for ADO/Dapper/Mongo audit fields |
| M3 | Identity is created **only** by `IRequestContextScopeFactory` (validated, logged). `IRequestIdentityFactory` (and its default `ClaimsRequestIdentityFactory`) is **internal**; customisation is through `RequestIdentityOptions`. The scope factory gains `RunInboundAsync(InboundRequestInfo, work)` used by `EncinaContextMiddleware` and the Blazor handler (principal, correlation id, tenant header, idempotency key, IP/UserAgent/DataRegion metadata). `RequestContext.CreateAt` is internal; the public factory is `CreateAnonymousAt(...)`. An explicit-context `Send` whose authenticated identity is not the ambient one always logs Warning 165; it returns `Left(scope_conflict)` when the ambient identity is a User, when the identity's issuer has ended (stale replay; mechanism: the internal `RequestIdentity.Issuer`, M6), or when the identity has no issuer and is used outside an active scope of the same identity (Design 1, rule order). Rule plus architecture test: production assemblies with `InternalsVisibleTo` from `Encina` (ADO.*, Dapper.*, EntityFrameworkCore, MongoDB, Marten, GraphQL, Security.ABAC; Encina.AspNetCore and Encina.AspNetCore.Blazor from Phase 3, M6) reference identity-minting members (`RequestIdentity.ForUser/ForService`, `RequestContext.CreateAt`) only through `RequestContextScopeFactory`. `Encina.Testing`, `Encina.Testing.FsCheck` and the test projects are the declared, ADR-035-listed test seam for **building** identities and principals only; **binding** an identity to code always goes through the factory (no test-only binding path, M6). | public `Create`/`CreateAt`, "trusted paths" list |
| M4 | `RunRestoredAsync` never trusts persisted roles or permissions. `PersistedRequestIdentity(Kind, ActorId, TenantId, CorrelationId, CausationId)` is exactly what SPEC-002 REQ-015 persists (service: `ActorId` is the catalog name). A restored User identity carries **no roles and no permissions**, so role- or permission-gated handlers in deferred dispatch deny (fail closed). Messages whose identity arrived from outside the trust boundary (inbox from an external broker) never restore a User identity (restored Anonymous). Role rehydration is a note for #1164, not an API here. `IRequestContext` gains `string? CausationId`. | Design 1 "Persisted form" |
| M5 | The user id leaves `ReadAuditLog` EventId 1702 (`ReadAuditLog.cs:56`, called from `AuditedRepository.cs:271`, `AuditedReadOnlyRepository.cs:231`) and the `["userId"]` details of `SecurityErrors.cs:72,97,125,148`; identity kind is recorded instead. The sentinel tests cover both (Phase 5 task 15). | Phase 5 task 2 only |
| m1 | EventId 9098 is dropped; the `Disabled`-mode startup warning reuses #751's planned 9085 ("Enforcement Disabled"). Whichever of #1705 and #751 lands first allocates 9085 in `ABACLogMessages` (planned: #1705, so #751 reuses the method and adds its once-per-request-type call); Phase 7 task 4 reconciles the #751 table. 9098-9099 stay free. | 9098 |
| m5 | The origin marker is an **internal typed flag** on `RequestContext` (`internal RequestOrigin Origin`, copied by `ForNestedDispatch`), not a metadata string. Each scope holder also keeps its own immutable origin and kind, which survive `Invalidate` (M6). Connection flows (SignalR, GraphQL subscriptions, raw WebSockets, SSE, Blazor circuits) start with no identity because the middleware skips `HubMetadata` endpoints, WebSocket upgrade requests and `Accept: text/event-stream` requests; from Phase 3 they run under an anonymous connection-origin marker (maintainer-confirmed decision, MQ-2, 2026-10-05), so `RunAsServiceAsync` and `RunAsPrincipalAsync` there need `AllowOverInbound` (logged Warning), while the per-activity and per-invocation `RunInboundAsync` is permitted; stated in F2 and the how-to. | `encina.context.origin` metadata; "unmarked until F2, so `RunAsServiceAsync` is permitted there" |
| m6 | Core EventIds are allocated in the phase that uses them, packed: Phase 1 = 162 `AuthenticatedPrincipalWithoutSubject`, 163 `ReservedServiceSubjectRejected`, 164 `ConflictingAuthenticatedIdentities`, 165 `ExplicitContextIdentityConflict`; Phase 2 = 166 `ServiceIdentityScopeOpened`, 167 `IdentityScopeRefused`, 168 `IdentityScopeClosed`, 169 `PrincipalScopeOpened`, 170 `IdentityScopeOutlivedParent` (renamed by M6), 171 `IdentityRestored`, 172 `InboundScopeOpened`, 173 `ScopeTenantChanged`, 174 `ScopeOpenedOverInbound` (172-174 added by the PR #1862 review, finding 12). Older references to 164-167/168/169 read as this table. Phase 3 adds 202 `EncinaContextBeforeRouting` in the `AspNetCore` range (M6). | EventId text in Design 4, Phases 1-2, Testing |
| m4 | `Encina` grants `InternalsVisibleTo` to `Encina.Testing.FsCheck` (needed by the `Arb`), `Encina.Testing` and `Encina.Security.ABAC`. `Encina.ContractTests` already references `Encina.Security` transitively; no extra ProjectReference for it. | Phase 1 task 2, Testing |
| M6 (scope shape) | **Delegate-only scope API (C1).** `IRequestContextScopeFactory` has only `RunAsServiceAsync`, `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync`, each returning `Task<Either<EncinaError, T>>`; the internal `IInternalRequestContextScopeFactory` adds `RunAsBuiltInAsync` and `RunAnonymousMarkerAsync`. No public `RequestContextScope`, no `TState` overloads (Q4), no test-only binding seam. Ending a scope invalidates its holder and writes nothing; the caller's context comes back through the async frame (Design 3). | `Begin*`, `RequestContextScope : IDisposable`, "restores the captured previous holder", out-of-order disposal semantics, the test-only seam as a binding path |
| M6: holder origin (Q1) | Each scope holder keeps an immutable origin and kind that survive `Invalidate`; the refusals walk the whole chain, ended holders included. **Every new holder inherits the facts of the holder that was current when it was created** (that holder's own recorded facts plus those walked from its chain), whatever `LiveOrNull` or `NearestScope` then drops, so nested dispatch and successive sets never lose a fact (Design 3, rule 4; PR #1862 review, finding 3). | Refusals that read only the current context |
| M6: setter | Identity- and origin-preserving only, never clears; a tenant change through it only while `!IsDispatchInFlight`, until F5 (Design 1). | "accepts and logs every other identity change" |
| M6: issuer (Q3) | One `RequestIdentity` per scope, stamped with an internal `IdentityIssuer`; an identity reads as no context once its issuer has ended (`ContextHolder.ReadContext`); `Resolve` applies the rule order of Design 1 (issuer-less explicit identities refused outside an active scope of the same identity). | "accepted only while the scope that issued that identity is still active" without a mechanism |
| M6: comparison, accessor | `IsSameAs` counts claims minus `RequestIdentityOptions.PerTokenClaimTypes` (Q2); `tenant_conflict` for explicit dispatch under a User; principals are cloned; a non-default `IRequestContextAccessor` returns `unsupported_accessor` and fails startup (Designs 1, 3, 5). | Roles and permissions only; custom accessors silently unread |
| M6: connection flows (Phase 3) | `EncinaContextMiddleware` binds nothing and `TenantResolutionMiddleware` writes no context on `HubMetadata` endpoints, WebSocket upgrade requests (detected through `IHttpUpgradeFeature`/`IHttpExtendedConnectFeature`, not only `IHttpWebSocketFeature`) and `Accept: text/event-stream` requests (MQ-1 (c)); the connection runs under the anonymous connection-origin marker (MQ-2, maintainer-confirmed); the Blazor `Left` branch runs inside an anonymous masking scope; `UseEncinaContext` before `UseRouting` is detected when the endpoint is null before `next` and the endpoint resolved after `next` carries `HubMetadata` (D1), logged Critical 202, and from then on that middleware instance answers 500 to every request without calling `next` (MQ-1 (b), fail closed; the latch is an instance field, never static) (Design 2). | Connection flows "Anonymous until F2" without a mechanism |

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
    public ClaimsPrincipal? Principal { get; }                  // a clone, never the caller's object (M6)
    public IReadOnlySet<string> Roles { get; }                  // FrozenSet, OrdinalIgnoreCase
    public IReadOnlySet<string> Permissions { get; }            // FrozenSet, OrdinalIgnoreCase
    public bool HasClaim(string type, string? value = null);    // authenticated ClaimsIdentity instances only
    public PersistedRequestIdentity ToPersisted(string? tenantId, string correlationId, string? causationId); // see "Persisted form"
    internal static RequestIdentity ForUser(string userId, ClaimsPrincipal? principal = null,
        IEnumerable<string>? roles = null, IEnumerable<string>? permissions = null,
        IdentityIssuer? issuer = null,
        IReadOnlySet<string>? perTokenClaimTypes = null);   // factory + builders only; null = RequestIdentityOptions.DefaultPerTokenClaimTypes (Phase 2)
    internal static RequestIdentity ForService(ServiceIdentityDefinition definition,
        IdentityIssuer? issuer = null,
        IReadOnlySet<string>? perTokenClaimTypes = null);   // Phase 2; the scope factory (a new instance per scope, with an issuer) and the TestIdentity builder (no issuer)
    internal IdentityIssuer? Issuer { get; }                    // Phase 2 (M6): set at construction by the factory only; null for builders
}

// Phase 2 (M6). Created before the identity, passed to its constructor and bound once to the scope's holder.
internal sealed class IdentityIssuer
{
    internal bool IsLive { get; }                               // bound holder is still valid (ContextHolder.IsValid)
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
- **Identity cannot be forged by application code.** `ForUser` and the identity-taking test conveniences are `internal` (`InternalsVisibleTo` for the test projects that already have it, plus `Encina.Testing`, whose public `TestIdentity.User/Service/Principal/Anonymous` helpers are the supported way for application tests to **build** identities and principals; they never bind one, see M6). An authenticated `RequestIdentity` is obtainable in production code only through `IRequestContextScopeFactory` (`RunInboundAsync` for HTTP and Blazor, `RunAsPrincipalAsync`, `RunAsServiceAsync`, `RunRestoredAsync`), each validated and logged. `IRequestIdentityFactory` and `ClaimsRequestIdentityFactory` are `internal` (customisation is `RequestIdentityOptions`; a custom-factory extension point is dropped). `IRequestContext.WithIdentity` is **not** a public interface member; `RequestContext.WithIdentity` and `RequestContext.CreateAt(timestamp, correlationId, identity, ...)` are `internal`; the public factory is `RequestContext.CreateAnonymousAt(DateTimeOffset, string correlationId, string? tenantId = null, string? idempotencyKey = null)`. Rule and architecture test: production assemblies that hold `InternalsVisibleTo` from `Encina` use identity-minting members only through `RequestContextScopeFactory`; `Encina.Testing`, `Encina.Testing.FsCheck` and the test projects are the declared test-only seam (`TestIdentity`), listed in ADR-035.
- **Issuer (M6, Phase 2).** A stamp on the *context* is not enough: `RequestContext.CopyOf` (`RequestContext.cs:176-190`) rebuilds a foreign `IRequestContext` and keeps only the **same** `RequestIdentity` object, so any field on the old context is lost. The stamp therefore lives on the identity:
  - The factory creates an internal `IdentityIssuer` token first, builds a **new** `RequestIdentity` with it (`ForUser`/`ForService` are called once per scope and never cached per declared service, so concurrent scopes never share a token), then pushes the holder and binds the token to it once. The identity stays frozen; no post-construction mutation, and `RequestIdentity` never references `ContextHolder`.
  - "Issuer ended" means `!holder.IsValid()` for the bound holder, so ending an enclosing scope ends every issuer nested in it.
  - The same identity object travels through `With*`, `ForNestedDispatch` and `CopyOf`, so the stamp survives every copy. Builders (`TestIdentity`) produce identities with no issuer.
  - **Read-time liveness.** `ContextHolder.ReadContext()` returns null (Anonymous) when the held context's `Identity.Issuer` is not null and no longer live. An explicit context accepted while its issuer lived and installed by `AmbientSwap.Apply` through `SetUnchecked` (`AmbientRequestContext.cs:334`) therefore stops reading as that identity in handlers, nested dispatches, stream steps and forked tasks the moment its issuer ends; `Resolve` is not the only check.
- **Trusted-path rule on explicit contexts (rule order).** `AmbientRequestContext.Resolve` (`AmbientRequestContext.cs:72`) returns `Either<EncinaError, IRequestContext>` (`Encina.Send/Publish/Stream` map the `Left`) and applies, for `Send/Publish/Stream(request, explicitContext)`, to the `CopyOf` snapshot it already takes (`:100-102`). "The chain has a User" means the current holder's recorded facts (Design 3, rule 4), which include every holder it was pushed or set over, ended ones too; it is not the readable ambient identity alone. The **identity rules** pick the first match:
  1. The explicit identity is not authenticated: accepted (it never changes the identity).
  2. The explicit identity has an issuer that has ended: `Left(scope_conflict)`, Warning 165, **even when it `IsSameAs` the ambient identity** (stale replay).
  3. The chain has a User and the explicit identity is not `IsSameAs` the readable ambient identity (Anonymous when none): `Left(scope_conflict)`, Warning 165 with both kinds (never ids). Reading the chain, not the ambient, closes the two-step path "explicit anonymous `Send` inside a user request, then a different live identity from the nested handler": the nested ambient reads Anonymous but its holder still records the User (PR #1862 review, finding 11).
  4. The explicit identity has no issuer: accepted only inside an active scope of the same identity (the ambient identity is authenticated, its issuer is live and it `IsSameAs` the explicit one); otherwise `Left(scope_conflict)`, Warning 165. It is never accepted just because the ambient is Anonymous, a Service or absent (Q3).
  5. The explicit identity is live and differs from the ambient identity, and the chain has no User: accepted, Warning 165.
  6. Otherwise (same identity, nested dispatch): accepted, nothing logged. Nested dispatch (a copy of the ambient context) is not an identity change.

  **Tenant rule (applies to every context an identity rule accepts, rules 1, 4, 5 and 6 alike):** when the chain has a User and the explicit context's tenant differs from the ambient tenant, `Left(encina.identity.tenant_conflict)`, Warning 165 with kinds only. There is no opt-out on purpose: a cross-tenant operator flow opens its own scope (`RunAsPrincipalAsync` or a declared service with a tenant argument). Older text that numbers this "rule 5" and the last two identity rules "6" and "7" reads as this list.
- **Identity comparison (`IsSameAs`, Q2).** `RequestIdentity.IsSameAs` (`RequestIdentity.cs:187`) compares kind, user id, roles, permissions and the authenticated claims **minus** the per-token claim types of `RequestIdentityOptions.PerTokenClaimTypes`, default `exp`, `iat`, `nbf`, `jti`, `uti`, `rh`, `aio`, `nonce`, `at_hash`, `c_hash` (`auth_time` is **not** excluded: like `amr` and `acr` it is a step-up and max-age signal). The comparison set is computed once at construction; the private constructor has no options access, so the factory passes the configured excluded set in. **Identities built outside the factory** (`ForUser`/`ForService` called by the `TestIdentity` builders, `RequestIdentity.Anonymous`) take the optional `perTokenClaimTypes` parameter, and `null` means the public static `RequestIdentityOptions.DefaultPerTokenClaimTypes` (the default list above, frozen). Each identity keeps the set it was built with, and `IsSameAs` removes the **union** of both identities' excluded types from both claim sets before it compares them, so a builder identity and a factory identity compare the same way whichever options the host configured (PR #1862 review, finding 13). Type comparison is `OrdinalIgnoreCase` and value comparison `Ordinal`, the same as `HasClaim` (`RequestIdentity.cs:131-138`). `RequestIdentityOptionsValidator` rejects a `PerTokenClaimTypes` entry that names a subject, role, permission, `amr` or `acr` type. `IsSameAs` ignores the issuer (it decides staleness, not sameness) and the tenant (tenant is not identity; the tenant rule and the setter rule cover it).
- **Principal cloning (M6).** `RequestIdentity` keeps the caller's `ClaimsPrincipal` today (`RequestIdentity.cs:73,104`), and `ClaimsPrincipal.AddIdentity` is public, so the principal Phase 3's `AuthorizationPipelineBehavior` evaluates could differ from the frozen claims `IsSameAs` compared. `ForUser`, `ForService` and the factory store a principal built at construction, and `Principal` returns a fresh clone of it per read, so no reader can change what the other gates saw. **Stored form (D4):** the stored principal keeps **only the authenticated identities** of the caller's principal, each copied as a plain `ClaimsIdentity` (`new ClaimsIdentity(claims, authenticationType, nameType, roleType)` over copies of its claims), never `identity.Clone()`: a `WindowsIdentity` clone would duplicate its token handle on every read. `Actor` and `BootstrapContext` are not copied (the bootstrap context can hold the raw token). Unauthenticated identities are dropped, so `Principal.IsInRole` and claim requirements see the same identities that `Roles`, `Permissions` and `HasClaim` read (`RequestIdentity.cs:234-240` freezes only authenticated identities; Design 4). The per-read clone is taken from that stored principal, so it duplicates no handle.
- **Public setter (M6).** `IRequestContextAccessor.RequestContext { set; }` (`IRequestContextAccessor.cs:60`) is identity- **and** origin-preserving: a set is accepted only when the new context `IsSameAs` the current identity and carries the same origin; it never clears (`null` is refused). **No readable context** (none set, or a chain that has ended): the reference is Anonymous with origin `Unspecified`, so the only accepted set is an anonymous context of origin `Unspecified`, which is what `RequestContext.CreateAnonymousAt` builds (the Phase 1 middleware between Phases 2 and 3, `EncinaContextMiddleware.cs:85-100`, and the `TenantResolutionMiddleware` fallback, `TenantResolutionMiddleware.cs:119,129`, rely on it). The facts of an ended chain are not the setter's reference, but the new holder inherits them (Design 3, rule 4), so a set over an ended inbound chain keeps every refusal. **Snapshot:** the setter checks and stores `RequestContext.CopyOf(value)` (`RequestContext.cs:176-190`), as `CheckExplicitContext` does (`AmbientRequestContext.cs:100-102`); today it stores the caller's object (`RequestContextAccessor.cs:82-83`), so a foreign `IRequestContext` could return one identity to the check and another to readers (PR #1862 review, finding 9). Any other set throws `InvalidOperationException` and logs Warning 165 (kinds only), whatever the ambient kind. A tenant change through the setter is accepted only while `!AmbientRequestContext.IsDispatchInFlight` (`AmbientRequestContext.cs:32`), so `TenantResolutionMiddleware` keeps working and a handler cannot retarget its own dispatch; F5 moves tenant resolution into `InboundRequestInfo` and then the setter refuses tenant changes too (F5 acceptance criterion). This closes the two-step bypass ("clear, then set another user") and the one-line downgrade (`RequestContext = null` then `RunAsServiceAsync` inside a user's request). The only way to bind an identity is the scope factory; ADR-035 lists every trusted path.
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

// Where the dispatcher read the message from (D3). No default: every P-50 dispatcher states it.
public enum PersistedIdentitySource { Internal = 1, External = 2 }

// on IRequestContextScopeFactory (Design 3, M6): one scope per message, ended when work completes
Task<Either<EncinaError, T>> RunRestoredAsync<T>(
    PersistedRequestIdentity persisted,
    PersistedIdentitySource source,
    Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
    CancellationToken cancellationToken = default);
```

**External sources (D3).** `source == External` means the message crossed the trust boundary (an inbox fed by an external broker): the factory restores **Anonymous whatever the row says** (M4) and gives the holder origin **`Inbound`**, not `Restored`, so handler code that runs `RunAsServiceAsync`/`RunAsPrincipalAsync` from that untrusted input is refused unless it passes `AllowOverInbound` (logged 174), exactly as over an anonymous HTTP request. `Internal` keeps origin `Restored` and the validation below. `PersistedIdentitySource` has no zero value, so `default` is rejected as an argument error (`Left`, logged 167). The rule and its tests are Phase 2; which dispatcher passes which value is wired by P-50 (#1164).

`RequestIdentity.ToPersisted(tenantId, correlationId, causationId)` produces the record (no roles, no permissions); `IRequestContext` gains `string? CausationId`, carried by copies and set by `RunRestoredAsync`. A processor that records a refusal or a failed message persists the error **code** only (`error.GetCode()` returns `Option<string>`, `EncinaErrors.cs:100`; use `IfNone("unknown")`), never `EncinaError.Message` (AGENTS.md section 3).

Validation at rebuild (any failure returns `Left`, logs a Warning with the kind only, and the dispatcher fails that message per its retry/dead-letter semantics, never runs it anonymously): `Kind == User` needs a valid user id (same rule as above, no `service:` prefix); `Kind == Service` needs an `ActorId` present in the catalog (roles/permissions come from the catalog, so a tampered row cannot escalate); `Kind == Anonymous` opens an explicit anonymous scope; correlation and causation ids are bounded strings. **A restored User identity carries no roles and no permissions**: the row is never a source of authority, so role- or permission-gated handlers in deferred dispatch deny (fail closed) and ABAC decides from the subject attributes it loads itself. A message whose identity arrived from outside the trust boundary (inbox from an external broker) never restores a User identity: the dispatcher passes `PersistedIdentitySource.External` and the factory restores Anonymous with origin `Inbound` (D3, above). Note for #1164 (not an API here): if deferred handlers need roles, P-50 decides how to re-resolve them at dispatch (and revocation/staleness). Every rebuild logs Information (kind, correlation id). `PersistedRequestIdentity` carries no `ClaimsPrincipal` (not serializable); a restored user's `Principal` is a synthetic authenticated `ClaimsIdentity` with the subject only.

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
- New `InvokeAsync` flow (M6):
  0. **Fail closed after a detected misordering (MQ-1 (b)).** Before anything else, when the latched misorder flag of step 5 is set, the middleware answers **500 without calling `next`** (no request data logged).
  1. **Connection requests bind no identity.** `IsConnectionRequest(HttpContext)` is true for:
     - **A WebSocket upgrade, detected from the request itself.** `context.WebSockets.IsWebSocketRequest` alone is not enough: it is true only once `WebSocketMiddleware` has set `IHttpWebSocketFeature` (`DefaultWebSocketManager.cs:45-49`, `WebSocketMiddleware.cs:61-64`, `dotnet/aspnetcore` `release/10.0`), and SignalR adds `UseWebSockets` inside its own endpoint pipeline (`ConnectionEndpointRouteBuilderExtensions.cs:98-99,110-111`), after `UseEncinaContext`; a host that calls `UseWebSockets` after `UseEncinaContext` (GraphQL over WebSocket, raw WebSocket endpoints) is the same case. The predicate is therefore true when any of these holds: `IHttpUpgradeFeature.IsUpgradableRequest` and the `Upgrade` header lists the token `websocket` (`OrdinalIgnoreCase`, comma-separated tokens); `IHttpExtendedConnectFeature.IsExtendedConnect` and its `Protocol` is `websocket` (`OrdinalIgnoreCase`; HTTP/2 and HTTP/3 extended CONNECT); or `context.WebSockets.IsWebSocketRequest` (a feature already set by an earlier `UseWebSockets` or by `TestServer`, `WebSocketClient.cs:92,129`). It needs no routing (PR #1862 review, finding 1).
     - **An SSE request.** The `Accept` header lists the media type `text/event-stream`: parsed with `MediaTypeHeaderValue.TryParseList(context.Request.Headers.Accept, ...)`, media type compared `OrdinalIgnoreCase`, parameters and `q` ignored (`q=0` still counts: skipping only makes the request anonymous), wildcards such as `*/*` or `text/*` do not count; when parsing fails, a raw `OrdinalIgnoreCase` search for `text/event-stream` in the header values decides, so a malformed header errs on the skip (MQ-1 (c): every SSE request, hub or not, the connect-time identity hole closed whatever the pipeline order; an ordinary SSE endpoint opts in per event or per dispatch, D2, Phase 3 task 11).
     - **A hub endpoint:** `context.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>()` is not null (every `MapHub`, including the `/negotiate` endpoint and Blazor's `/_blazor` hub, so long polling is covered once routing ran).

     For a connection request the middleware runs `next` inside the internal anonymous connection-origin marker: `internalScopes.RunAnonymousMarkerAsync(AnonymousMarker.Connection, _ => next(context), context.RequestAborted)`, where `internalScopes` is the injected `IInternalRequestContextScopeFactory` (Design 3). Why: a connection's application runs in an `ExecutionContext` forked from the connecting request after `UseEncinaContext` opened its scope (`HttpConnectionContext.TryActivatePersistentConnection`, `ExecuteApplication`, `dotnet/aspnetcore` `release/10.0`), and under WebSockets and SSE that request, and so its inbound scope, lives as long as the connection: `HttpConnectionDispatcher.cs:156-158` is the SSE branch (`TryActivatePersistentConnection`, then `await DoPersistentConnection`), and the WebSocket branch awaits the connection the same way. Only long polling ends with its first poll (`HttpConnectionContext.cs:460`), so a flow there reads Anonymous after it. Without the skip, hub dispatches would run as the connect-time user, with connect-time roles, for hours (fail open), and the Blazor per-activity scope would be refused.
  2. Otherwise build `InboundRequestInfo` with `context.CreateInboundRequestInfo()` (the public builder of Phase 3 task 11, D2: principal `context.User`, correlation id, the `TenantIdHeader` fallback value, idempotency key, IP/UserAgent/DataRegion metadata) and `await scopeFactory.RunInboundAsync(info, async (_, _) => { await next(context); return Right<EncinaError, Unit>(unit); }, context.RequestAborted)` (Design 3): inside, the internal `IRequestIdentityFactory` maps the principal to the identity (Design 4), resolves the tenant (authenticated principals first, then the header fallback, unchanged; follow-up F5), stamps the timestamp from the injected `TimeProvider`, marks the context's internal `Origin = Inbound` and logs 172. Mapping problems (no subject, a reserved subject, conflicting identities) are never a `Left`: the factory maps them to Anonymous, as in Phase 1. Nor is any other client-controlled input: `RunInboundAsync` normalizes, never refuses, what the request carries (Design 3, "Inbound input").
  3. When the work completes the factory invalidates the holder, so every flow that captured the request (`Task.Run`, fire-and-forget, timers) reads no context, which means Anonymous, which means deny. There is no `finally` in the middleware.
  4. **The `Left` results.**
     - **`RequestCancelled`** (the client aborted before the scope opened: `RequestAborted` already cancelled, Design 3 rule 2, no 167): the middleware returns without calling `next`, writes no status code and logs nothing; the client is gone, and a 500 would count an abort as a server fault (PR #1862 review, finding 12).
     - **Any other `Left` is a host misconfiguration and fails closed and loudly.** `scope_conflict` means an inbound or User context is already ambient (`UseEncinaContext` registered twice, or the host runs the pipeline inside another scope); `unsupported_accessor` already fails startup through the validator (Design 3). The factory logs 167 (code only) and the middleware answers **500 without calling `next`**. Answering with an anonymous context instead would need a binding path outside the validated factory and would hide the misconfiguration. No client-controlled input reaches this branch (step 2).
  5. **Ordering: `UseEncinaContext` after `UseRouting`.** The `HubMetadata` check needs the endpoint. Minimal hosting adds `UseRouting` at the start of the pipeline unless the application calls it explicitly. **Trigger (D1):** the endpoint is null before `next` **and** the endpoint resolved after `next` returns carries `HubMetadata`, the only case where the connection skip was actually missed. A non-hub endpoint resolved after `next` does not trip it, so `UseRewriter` (`RewriteMiddleware.cs:73-81`) and `UseStatusCodePagesWithReExecute` (`StatusCodePagesExtensions.cs:152-156,184-188`) re-routes and plain 404s on a correctly ordered pipeline never do (PR #1862 review, finding 2). On the trigger the middleware logs **Critical** 202 `EncinaContextBeforeRouting` once (no request data) and sets the latch, after which step 0 answers **500 to every later request without calling `next`** (MQ-1 (b): a misconfigured host fails closed and loudly; the request that revealed the misorder has already run). **The latch is an instance field of the middleware** (`private int _misordered`, set with `Interlocked.Exchange` so 202 is logged once), **never static**: one pipeline build creates one middleware instance, so the latch lives as long as that application, and other hosts in the same process (TestServer tests) are never affected. The `/negotiate` endpoint carries `HubMetadata`, so the first short negotiate request reveals the misorder. The WebSocket and `Accept: text/event-stream` checks of step 1 do not depend on routing, so those connections are skipped in either order and no SSE connection can keep the connect-time identity; the only flow left to the detection is the first long poll, which ends with its first poll and is followed by the 500s. The Blazor masking scope (below) stays as defence in depth for circuits.
- `IRequestContextScopeFactory` and the internal `IInternalRequestContextScopeFactory` are injected into `InvokeAsync` (middleware method injection; both resolve to the same singleton); the middleware no longer touches `IRequestIdentityFactory`, which is internal. **Visibility (CS0051, decided):** a public `InvokeAsync` on the public `EncinaContextMiddleware` (`src/Encina.AspNetCore/PublicAPI.Unshipped.txt:24-26`) cannot take an internal parameter type, and the same holds for a public `RequestIdentityCircuitHandler` constructor. Both classes become **`internal sealed`**; the alternative, resolving the internal interface from `context.RequestServices` inside `InvokeAsync`, is rejected because it hides a dependency from `ValidateOnBuild` and the DI tests and adds a per-request service-locator lookup. Nothing is lost: `UseMiddleware<T>` activates internal types, applications reach the middleware only through `UseEncinaContext()` and the circuit handler only through `AddEncinaBlazorAuthorization()`, so neither type had consumer value as public API. Their PublicAPI lines are removed (Phase 3 task 7), and `Encina.AspNetCore` and `Encina.AspNetCore.Blazor` add `InternalsVisibleTo` for `Encina.GuardTests` and `Encina.AspNetCore.Benchmarks` (they grant it today to UnitTests, IntegrationTests, ContractTests and PropertyTests only) so the guard tests of the Testing section and the Phase 3 benchmark keep compiling. `Encina` grants `InternalsVisibleTo` to `Encina.AspNetCore` and `Encina.AspNetCore.Blazor` for `IInternalRequestContextScopeFactory.RunAnonymousMarkerAsync` (Phase 3; listed in ADR-035 and allowed by the M3 architecture test, because it mints no authenticated identity).
- **`TenantResolutionMiddleware` applies the same skip, to the context write only.** `TenantResolutionMiddleware.cs:117-121` sets `WithTenantId(...)` through the setter whenever a tenant resolves; on a connection request it would install a never-invalidated holder with the connect-time tenant. On WebSocket upgrade requests, `Accept: text/event-stream` requests and `HubMetadata` endpoints it skips **only that write** (`:117-121`): tenant resolution (`:85`), tenant validation (`:88-97`) and the `RequireTenant` 400 (`:100-113`) still run, because the `Accept` header is client-controlled and a skip at the start of `InvokeAsync` would let any client bypass them (PR #1862 review, finding 14). The predicate is the same as the middleware's, WebSocket detection included (a private copy: `Encina.Tenancy.AspNetCore` does not reference `Encina.AspNetCore`, and no ProjectReference is added; a shared test table runs both copies over the same cases).
- **Accessor lifetime (holder pattern, Phase 1 code, M6 semantics).** `RequestContextAccessor` keeps a static `AsyncLocal<ContextHolder>` (`RequestContextAccessor.cs:45-200`). A holder links to its `Parent`, reads as no context once it or any holder it is bound to has ended (`ContextHolder.IsValid`, `:196`), and ending it clears its context (`Invalidate`, `:189`). **Ending a scope invalidates its holder; the caller's holder comes back through the async frame**, not through Encina: `AsyncMethodBuilderCore.Start` restores the caller's `ExecutionContext` on synchronous return, and the caller's continuation runs on the context it captured at its `await` (PR #1849, section 2.2). `Pop` becomes `End`: the in-order restore at `RequestContextAccessor.cs:149` is removed. Consequences, each with tests: (a) a task started inside a request or scope and run after it ended sees Anonymous and is denied by ABAC and Security; (b) scopes nest LIFO by construction inside a flow; a scope in a forked flow that outlives an enclosing scope reads Anonymous, and its `End` logs 170 `IdentityScopeOutlivedParent`; a child forked inside a scope can never end it, so it can never install the parent identity (the child-pop elevation of PR #1849, section 2.1); (c) connection flows start with no identity (WebSocket upgrades, `Accept: text/event-stream` requests and `HubMetadata` endpoints are skipped, step 1) and run under the anonymous connection-origin marker; the Blazor activity handler establishes the identity per activity; (d) 100 parallel requests with distinct principals never observe another request's identity.
- **`AuthorizationPipelineBehavior` evaluates `context.Identity.Principal`** (authenticated identities only) and `IPrincipalResolver` is deleted together with `AuthenticationStatePrincipalResolver` (and the `HttpContextAccessor` registration made only for it). The gate denies with `authorization.unauthenticated` when `!context.Identity.IsAuthenticated`, which also closes the case where the factory mapped a token to Anonymous (no subject, reserved `service:` subject): the principal can no longer pass `[Authorize]` while audit records no user. The "requires HTTP context" denial disappears because identity is not read from `HttpContext` any more. **User id out of its logs and errors:** once the identity is real again, the `UserId` fields of EventIds 200 `AuthorizationSucceeded` and 201 `AuthorizationDenied` (`AuthorizationPipelineBehavior.cs:85-95`, value `context.UserId` at `:176`, call sites `:164,182,210,243,280,309,333`) would print it; both templates record the identity kind instead (EventIds kept), and the `["userId"]` details of its errors (`:221,254,291,320`) become `["identityKind"]`, as M5 does for `SecurityErrors` (PR #1862 review, finding 4).
- **Blazor Server.** `Encina.AspNetCore.Blazor` replaces its resolver with `RequestIdentityCircuitHandler : CircuitHandler`. Its `CreateInboundActivityHandler(next)` reads `AuthenticationStateProvider.GetAuthenticationStateAsync()` per activity and wraps `next(activity)` in `scopeFactory.RunInboundAsync(InboundRequestInfo.ForCircuit(state.User, circuitCorrelationId), …)` (Origin `Inbound`); the scope ends when the activity completes.
  - **Refresh.** A role or claims refresh (a new `AuthenticationState`) takes effect at the next activity, without the setter.
  - **On `Left`** (only reachable when the circuit flow carries an inherited inbound or User context, that is the first long poll of a misordered pipeline, before the fail-closed 500s of step 5, or `unsupported_accessor`): the factory has logged 167 (code only); the handler then runs `next(activity)` inside the internal anonymous **masking** scope, `IInternalRequestContextScopeFactory.RunAnonymousMarkerAsync(AnonymousMarker.Mask, _ => next(activity))`. The masking scope reads Anonymous, and its holder keeps the inbound origin of the chain it covers, so `RunAsServiceAsync`/`RunAsPrincipalAsync` inside it are still refused. The activity is never dropped (the circuit would stop processing events and JS interop) and never runs under the connection identity.
  - **Outside an activity** (continuations that outlive it, an `AuthenticationStateChanged` handler on the circuit's synchronization context) code reads Anonymous on every transport.
  - Tests: a unit test with a fake `AuthenticationStateProvider` (a dispatch inside an activity sees the circuit user, one outside sees Anonymous, the `Left` branch dispatches Anonymous) and the TestServer WebSocket tests of Phase 3 task 9.
- **Ordinary SSE endpoints (D2).** An endpoint that streams `text/event-stream` (for example `TypedResults.ServerSentEvents`) runs under the connection marker like every SSE request (step 1). It opts in through the public `HttpContext.CreateInboundRequestInfo()` (Phase 3 task 11; the same builder step 2 uses) and `scopeFactory.RunInboundAsync(info, work)` around **one event or one dispatch**, never around the whole stream: each scope ends with its unit of work, so a stream that outlives the token never keeps an identity between events. `RunInboundAsync` is permitted over the connection marker. The principal is the one the connection authenticated with, so the endpoint decides when it is stale (token expiry, revocation); the how-to says so, as F2 does for hubs.
- **SignalR hub invocations, GraphQL subscriptions and raw WebSocket endpoints** are not populated by #1705 (follow-up F2). Mechanism: step 1 skips them, so they run under the anonymous connection-origin marker and dispatch Anonymous on every transport (for long polling, when the pipeline order of step 5 holds, otherwise the host fails closed); every gate denies, and `RunAsServiceAsync`/`RunAsPrincipalAsync` there need `AllowOverInbound`. A test pins that interim behaviour. F2 adds a hub filter that opens `RunInboundAsync` per invocation (permitted over the marker), covers streaming hub methods (`IAsyncEnumerable<T>`/`ChannelReader<T>` items are produced after `InvokeMethodAsync` returns, so they read Anonymous until F2 scopes each item step), and states how long-lived connections pick up role changes (SignalR does not refresh `Context.User`). GraphQL queries and mutations and gRPC run inside the HTTP pipeline and are covered by the middleware (entry-point table).

</details>

<details>
<summary><strong>3. Non-HTTP identity — declared service identities and an <code>Either</code>-returning scope API</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Declared identities (`AddEncinaServiceIdentity`) + `IRequestContextScopeFactory` returning `Either`, refusing over an ambient user** | Least privilege (roles/permissions fixed at startup and reviewable in one place); misuse returns `Left`; ROP; every opening logged | One registration line per job identity |
| **B) Ad hoc `RunAsServiceAsync(id, roles, permissions, work)`** | Zero ceremony | Any code can mint any role set; elevation inside a user request is only logged |
| **C) Default service identity for all background dispatch** | Convenient | Implicit allow; violates fail-closed (AGENTS.md §3) |
| **D) Documentation only (hand-built `ClaimsIdentity(claims, "service")`)** | No code | Today's state; no logging, no restore, no namespacing |

### Chosen Option: **A**

### Rationale

```csharp
// Startup
services.AddEncinaServiceIdentity("billing-reconciliation", id => id
    .WithRoles("billing-job")
    .WithPermissions("invoices:reconcile"));

// Job / hosted service: one scope per unit of work
var result = await scopes.RunAsServiceAsync(
    "billing-reconciliation",
    (context, ct) => encina.Send(new ReconcileInvoices(), ct),
    new IdentityScopeOptions(TenantId: tenant),
    ct);
result.IfLeft(error => Log.ReconciliationFailed(logger, error.GetCode().IfNone("unknown")));   // the code only, never the message
```

**Delegate-only API (M6, decision C1 of 2026-10-05).** The public surface has no handle: a scope is the region in which `work` runs, and it ends when `work` completes. This removes disposal from a foreign flow (a circuit, a connection, a `StartAsync`/`StopAsync` pair), the forgotten `using` that leaks one outbox message's actor into the next, the helper that opens a scope and loses it on return, and the child-pop elevation of application-nested scopes (PR #1849, sections 2.1-2.2). It is a **correctness guard against accidental misuse, not a security boundary**: in-process code can reach the public factory from a flow with no holder, for example under `ExecutionContext.SuppressFlow()` (ADR-035 says so).

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

    Task<Either<EncinaError, T>> RunInboundAsync<T>(              // HTTP middleware, Blazor activity, F2 hub filter
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, T>> RunRestoredAsync<T>(             // deferred dispatch (Design 1, SPEC-002 REQ-015 / P-50)
        PersistedRequestIdentity persisted,
        PersistedIdentitySource source,                           // D3: External restores Anonymous with origin Inbound
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);
}

public sealed record IdentityScopeOptions(string? TenantId = null, bool AllowOverInbound = false);

// Phase 2, public: everything an entry point read from its caller. Every member is client-controlled
// except Principal (validated by the authentication handler); the factory normalizes, never refuses, them.
public sealed record InboundRequestInfo(
    ClaimsPrincipal? Principal,
    string? CorrelationId = null,
    string? TenantHeaderValue = null,
    string? IdempotencyKey = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? DataRegion = null)
{
    public const int MaxIdLength = 128;          // correlation id, tenant header value, idempotency key
    public const int MaxUserAgentLength = 512;   // longer values are truncated
    public const int MaxDataRegionLength = 16;   // longer values are dropped
    public static InboundRequestInfo ForCircuit(ClaimsPrincipal? principal, string correlationId); // Phase 3 Blazor handler
}

// RequestContextScopeFactoryExtensions: one overload per member for work that returns Task (no Either);
// the result is Either<EncinaError, Unit>. No TState overloads (Q4: only if a benchmark shows a need).

// Internal surface, reached only through InternalsVisibleTo. RequestContextScopeFactory implements both interfaces;
// AddEncinaRequestIdentity registers the one singleton for IRequestContextScopeFactory and, forwarding to it,
// for IInternalRequestContextScopeFactory, so callers inject the internal interface.
internal interface IInternalRequestContextScopeFactory
{
    Task<Either<EncinaError, T>> RunAsBuiltInAsync<T>(           // Phase 2; Encina.Security.ABAC seeding
        string builtInName,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);

    Task<Either<EncinaError, Unit>> RunAnonymousMarkerAsync(     // Phase 3; Encina.AspNetCore, Encina.AspNetCore.Blazor
        AnonymousMarker marker,                                  // Connection | Mask
        Func<CancellationToken, Task> work,                      // wraps next(context) / next(activity)
        CancellationToken cancellationToken = default);
}
```

The implementation is one private `async` method: `Push` (visible to `work` and everything it forks), `try { return await work(context, ct); } finally { End(holder); }`. `End` only invalidates; it never writes the `AsyncLocal`. **Implementation constraint:** `Push` runs only inside that private `async` body, never in a non-async wrapper, whose write would stay in the caller's flow; a test calls a `Run*Async` **without awaiting** it and asserts the caller's ambient right after the call returns.

- **Rules** (implementation `RequestContextScopeFactory(IRequestContextAccessor, IServiceIdentityCatalog, IRequestIdentityFactory, IOptions<RequestIdentityOptions>, TimeProvider, ILogger<RequestContextScopeFactory>)`, singleton). Checks run in this order and a `Left` never invokes `work`:
  1. **Accessor.** When the injected `IRequestContextAccessor` is not the default `RequestContextAccessor`, every member returns `Left(encina.identity.unsupported_accessor)`: `Push` and `End` are static members of `RequestContextAccessor`, so a scope would write to a store the dispatcher never reads and dispatches would silently run Anonymous. A startup validator registered by `AddEncinaRequestIdentity` (`ValidateOnStart`) fails the host with the same message, so the misconfiguration surfaces at boot; it also removes the decorated-accessor case whose restore could throw. `IRequestContextAccessor` stops being a replacement extension point (pre-1.0, no shim); the dispatcher's `ApplyPlain` path (`AmbientRequestContext.cs:354`) stays only for unit tests that build `Encina` with a substituted accessor.
  2. **Cancellation.** A token already cancelled returns `Left(EncinaErrorCodes.RequestCancelled)` before `Push`, without logging 167 (cancellation is not a refusal; same code `Encina.Send` uses, `Encina.RequestDispatcher.cs:146-161`).
  3. **Arguments.** Unknown `serviceId` → `Left(encina.identity.unknown_service_identity)`; a built-in identity (`encina.` prefix) through the public API → `encina.identity.reserved_service_identity`; `RunAsPrincipalAsync` with `options.TenantId` that differs from the principal's tenant claim → `encina.identity.tenant_conflict`; `RunRestoredAsync` validation failures, including an undefined `PersistedIdentitySource` value (Design 1).
  4. **Holder chain (Q1, all `Left`, all logged 167 at Warning with kinds only).** Each holder stores an immutable `Origin` (`RequestOrigin`, `RequestOrigin.cs`: `Unspecified`, `Inbound`, `Scope`, `Restored`; `Connection` added in Phase 3) and `Kind`, set at `Push`/`SetUnchecked` and **kept by `Invalidate`**. The refusals read the current holder's facts: its own origin and kind plus the facts it inherited. **Inheritance (PR #1862 review, finding 3):** every new holder, whether created by `Push` (`RequestContextAccessor.cs:114-121`) or `SetUnchecked` (`:92-97`), records the facts (any User, any Inbound, any Connection) of the holder that is **current when it is created** (`CurrentHolder.Value`, read before anything is dropped), that is that holder's own origin and kind plus everything it inherited, ended holders included. Only then may `LiveOrNull` (`:124-125`) drop an ended parent and `NearestScope` (`:128-129`) skip a non-scope holder from the read chain; neither can lose a fact. Example that failed before: in a `Task.Run` forked from an ended request, `Send(r1)` sets N1 (facts `{User, Inbound}` from the ended chain); the handler's `Send(r2)` sets N2 with parent `NearestScope(N1) = N1.Parent = null`, so N2 used to start with no facts and `RunAsServiceAsync` was allowed. With inheritance N2 copies N1's facts and the call is refused. `Install` (`:108`) reinstalls a captured holder as it is, facts included.
     - A User anywhere in the chain → `scope_conflict` for every public member and `RunAsBuiltInAsync` (a job scope can never replace or escalate a user's identity; no opt-out).
     - An `Inbound` origin anywhere in the chain, of **any** kind including Anonymous and ended holders → `scope_conflict` unless `IdentityScopeOptions.AllowOverInbound` is set on `RunAsServiceAsync`/`RunAsPrincipalAsync` (opt-in per call, logged Warning 174 `ScopeOpenedOverInbound`); `RunInboundAsync` and `RunRestoredAsync` have no opt-out. A message restored with `PersistedIdentitySource.External` gets origin `Inbound` (D3), so this rule covers it. So an anonymous or reserved-subject request, and a task forked from a request that has ended (a dead flow), cannot elevate itself into a service identity. This is the accidental-misuse guard of Q1; `ExecutionContext.SuppressFlow()` bypasses it.
     - A `Connection` origin (Phase 3 marker) → treated as `Inbound`, except that `RunInboundAsync` is **permitted** (per-activity and per-invocation scopes).
     - `RunAnonymousMarkerAsync` is always permitted: it only downgrades to Anonymous.
  5. Ambient `Service`, scope-origin `Anonymous` or no ambient context → allowed; replacing another service identity is logged at Warning.
  - **Opening logs (one EventId per opening, PR #1862 review, finding 12).** `RunAsServiceAsync` logs 166 (Information; Warning when it replaces another service identity); `RunAsPrincipalAsync` logs 169 (Information; Warning when it opens a User identity over a Service ambient); `RunRestoredAsync` logs 171; `RunInboundAsync` logs 172 `InboundScopeOpened` (Debug: it runs once per HTTP request or circuit activity; identity kind and correlation id only). When `AllowOverInbound` lets `RunAsServiceAsync` or `RunAsPrincipalAsync` open over an inbound or connection chain, the opening logs 174 `ScopeOpenedOverInbound` (Warning: member, requested kind) **instead of** 166 or 169, so the opt-out has one EventId to alert on.
  - `RunAsPrincipalAsync`: maps through `IRequestIdentityFactory` (same rules as HTTP, including reserved-subject rejection and the multi-identity rules of Design 4); an anonymous principal opens an explicit anonymous scope.
  - **Inbound input (PR #1862 review, finding 12).** `RunInboundAsync` **never returns `Left` for client-controlled input**; its only `Left` results are `unsupported_accessor`, `RequestCancelled` and the holder-chain `scope_conflict`, all host or transport conditions. Each client-controlled `InboundRequestInfo` member is normalized instead: a correlation id that is blank, longer than `InboundRequestInfo.MaxIdLength` (128) or contains control characters is replaced by `Activity.Current?.Id` or a new GUID; a tenant header value or idempotency key over the same bound or with control characters is dropped (treated as absent: the context carries no header tenant or idempotency key, as for a request without the header); `UserAgent` is truncated to `InboundRequestInfo.MaxUserAgentLength` (512); an `IpAddress` that does not parse and a `DataRegion` over `InboundRequestInfo.MaxDataRegionLength` (16) are dropped. Each normalization logs nothing with the value (at most the member name at Debug). A long `X-Correlation-ID` therefore never turns into a 500.
  - **One `RequestIdentity` per scope.** Every member builds a new identity with a new `IdentityIssuer` (Design 1); `ForService` never caches per declared service.
  - **Tenant binding.** A scope never inherits the ambient tenant. `RunAsServiceAsync`: tenant = `options.TenantId` only (null means no tenant). `RunAsPrincipalAsync`: tenant = `ResolveTenantId(principal)`; `options.TenantId` is accepted only when the principal has no tenant claim, and a different value returns `tenant_conflict`. `RunRestoredAsync`: tenant = the persisted tenant. When the new scope's tenant differs from a non-null ambient tenant, Information 173 `ScopeTenantChanged` is logged (member and identity kind, no tenant values). **Metadata carried over:** only the correlation id and the origin marker (`scope`); DataRegion and module name are not carried, because a scope is a new unit of work (documented in the how-to).
  - The new context keeps the ambient `CorrelationId` when present (or the persisted one for `RunRestoredAsync`), otherwise `Activity.Current?.Id` or a new GUID; timestamp from the injected `TimeProvider`.
  - **Results.** A `Left` from `work` passes through unchanged. An exception from `work`, `OperationCanceledException` included, propagates unchanged after the holder is invalidated: the factory never converts exceptions (`IEncina.Send` already turns a cancelled or failed dispatch into a `Left`).
  - **Ending.** When `work` completes, faults or is cancelled, the factory's `finally` invalidates the holder and logs 168 `IdentityScopeClosed`; Encina restores no holder. When `End` finds `!holder.IsValid()` (an enclosing scope ended first: `Invalidate`, `RequestContextAccessor.cs:189-193`, flags only its own holder, so the check is `IsValid()`, not `IsDisposed`), it logs 170 `IdentityScopeOutlivedParent` (Warning, kinds only).
- **Streams.** `IEncina.Stream` resolves its context lazily, at the first `MoveNextAsync` (`Encina.Stream.cs:37-39`), and `AmbientRequestContext.Flow` reinstalls the dispatch holder at every step. So a stream **first enumerated after** `work` returned runs under the context of the caller that enumerates it, not under the scope; a stream **started inside** `work` and enumerated further after `work` ended reads Anonymous from then on (fails closed). The how-to says "consume a stream inside `work`"; tests pin both cases.
- **Encina-owned long-lived loops.** A loop, timer or channel reader that Encina starts lazily (on first use) captures the `ExecutionContext` of the flow that first used it, so a loop first touched inside a scope would read that scope's holder for its whole life (Anonymous once the scope ends, which fails closed but silently breaks later work). Such loops start under `ExecutionContext.SuppressFlow()`; Phase 2 sweeps core and records each one found in the PR, and the how-to tells application authors the same.
- **No synchronous overloads.** Every entry point is asynchronous (middleware, circuit handler, hub filters, hosted services, Hangfire and Quartz jobs, outbox processors).
- **Tenant is per call, not part of the declaration**: tenant is not identity (Design 1), and multi-tenant jobs iterate tenants.
- Declarations are validated at startup (`ServiceIdentityCatalogOptionsValidator` with `ValidateOnStart`): name pattern `^[a-z0-9][a-z0-9.-]{0,62}$`, no wildcard roles or permissions. **The `encina.` prefix is reserved for built-in identities:** application declarations with it are rejected by the validator; library packages declare built-ins through an `internal` API (`InternalsVisibleTo` for `Encina.Security.ABAC`) that marks the catalog entry `IsBuiltIn`, and only that package opens it through the internal `RunAsBuiltInAsync(name, work)`. `AddEncinaServiceIdentity` is idempotent for an identical declaration (a second `AddEncinaABAC` call or an application re-declaring the same entry does not fail the uniqueness rule); a conflicting redeclaration fails startup.
- **Built-in dispatchers wired in #1705: none, and none is ever wired to a service identity.** Deferred dispatch (Outbox `OutboxProcessorBase.cs:176`, Scheduling, Inbox, dead-letter replay, Saga/RoutingSlip runners) rebuilds the originating actor from the persisted message through `RunRestoredAsync`, one scope per message, under SPEC-002 REQ-015 / DEC-011 and P-50 (#1164); until P-50 lands those dispatchers run Anonymous and fail closed. Jobs with no originating request (recurring Hangfire `HangfireRequestJobAdapter.cs:136` and Quartz `QuartzRequestJob.cs:97` jobs, CDC bridge, compliance monitors) get an opt-in declared service identity in F1; serverless adapters are F3.
- **PAP seeding**: `PolicyChangeActorScope` (internal `AsyncLocal<bool>`) is deleted. `AddEncinaABAC` declares the built-in identity `encina.abac.policy-seeding` when seeding is configured, and `ABACPolicySeedingHostedService` runs once under the internal `RunAsBuiltInAsync("encina.abac.policy-seeding", …)`. The PAP accepts any authenticated identity (User or Service) as actor and records `Identity.UserId` (`service:encina.abac.policy-seeding`); anonymous is still refused with `abac.policy_change_principal_required`. The PAP audit metadata `["actor"]` records the identity kind in lowercase (`user` or `service`) replacing `system | principal`; `PolicyActor.IsSystem` and `SystemActorId` are deleted. This also gives jobs a supported way to make runtime policy changes (#1704 item 2) under their own declared (non-built-in) identities.

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
| `PerTokenClaimTypes` (Phase 2, M6) | `["exp", "iat", "nbf", "jti", "uti", "rh", "aio", "nonce", "at_hash", "c_hash"]` | Claim types `IsSameAs` ignores, so two snapshots of one session stay the same identity after a token refresh. `auth_time` is not in the list (step-up and max-age signal). The validator rejects an entry that is a configured subject, role or permission type, `amr` or `acr`. |

- `IRequestIdentityFactory` (**internal**): `RequestIdentity Create(ClaimsPrincipal? principal)` and `string? ResolveTenantId(ClaimsPrincipal? principal)`. Default `ClaimsRequestIdentityFactory` (internal sealed; singleton), used only by `RequestContextScopeFactory`. Applications customise mapping through `RequestIdentityOptions`; there is no public factory to call or replace (no unlogged mint path).
- **Reserved subject**: a mapped user id that, trimmed, starts with `service:` (`OrdinalIgnoreCase`) from an external principal maps to `Anonymous` and logs `ReservedServiceSubjectRejected` (Warning, claim type only). User ids with leading/trailing whitespace or control characters get the same treatment. External tokens cannot impersonate a declared service. Treating it as anonymous (rather than a 403 short-circuit) keeps one fail-closed rule for every entry point: anonymous endpoints still work, every gate that needs a caller denies, and a scope cannot elevate it because the HTTP context is marked `inbound` (Design 3).
- **Several `ClaimsIdentity` instances on one principal.** Only **authenticated** `ClaimsIdentity` instances contribute subject, permissions, tenant and `RequestIdentity.HasClaim` (`[RequireClaim]`). Roles also come from authenticated identities only; this is stricter than `ClaimsPrincipal.IsInRole` (which counts unauthenticated identities) on purpose, because claims transformation can add unauthenticated identities and roles now drive authorization. Two authenticated identities that yield different subjects, or different tenants, map to `Anonymous` and log `ConflictingAuthenticatedIdentities` (EventId 164, Warning, claim type only) instead of depending on scheme order. Factory tests: unauthenticated extra identity ignored for sub/permission/tenant/role, two authenticated identities with equal and different subjects, different tenants.
- **Tenant claim map is one map.** `Encina.Tenancy.AspNetCore`'s `ClaimTenantResolver` and the serverless options keep their own single `ClaimType` in #1705; follow-ups F5 and F3 move them to `IRequestIdentityFactory.ResolveTenantId`. Until then an application that changes `TenantIdClaimTypes` must set the Tenancy resolver claim type too (documented in the reference page).
- `RequestIdentityOptionsValidator` (`ValidateOnStart`): every list non-empty, no blank entries; from Phase 2, no `PerTokenClaimTypes` entry that names a subject, role, permission, `amr` or `acr` type.
- Removed (no aliases): `SecurityOptions.UserIdClaimType`, `RoleClaimType`, `PermissionClaimType`, `TenantIdClaimType`, `ThrowOnMissingSecurityContext`; `EncinaAspNetCoreOptions.UserIdClaimType`, `TenantIdClaimType`. `EncinaAspNetCoreOptions.TenantIdHeader` stays. `Encina.AwsLambda`/`Encina.AzureFunctions` own claim options move to the factory in F3.
- **No claim value, user id or role reaches logs, activity tags or `ToString`.** Logs carry only claim **type** names, the authentication type, identity kind, error codes and (for service scopes) the declared service name. This includes the Security gate: the `security.user_id` tag and the `UserId` field of logs 8001/8002 are removed (Phase 5). It also includes three log sites that print the user id today and that Phase 3 rewrites: `AuthorizationPipelineBehavior` EventIds 200/201 (`AuthorizationPipelineBehavior.cs:85-95`) and its `["userId"]` error details, `SoftDeleteInterceptor` EventId 3050 (`SoftDeleteInterceptor.cs:182-189`, "by user {UserId}") and `AuditInterceptor` EventId 3000 (`AuditInterceptor.cs:498-506`); each records the identity kind instead and keeps its EventId (Phase 3 tasks 3 and 6). Tests with sentinel values prove it (Testing section).

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

- A shared internal helper `services.TryAddEncinaRequestIdentity()` in core (exposed through a public `AddEncinaRequestIdentity(Action<RequestIdentityOptions>?)`) registers: `TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>`, `TryAddSingleton(TimeProvider.System)`, `AddOptions<RequestIdentityOptions>()` + validator + `ValidateOnStart`, `TryAddSingleton<IRequestIdentityFactory, ClaimsRequestIdentityFactory>`, `TryAddSingleton<IServiceIdentityCatalog, ServiceIdentityCatalog>`, `TryAddSingleton<IRequestContextScopeFactory, RequestContextScopeFactory>`, catalog options + validator, and (Phase 2, M6) the accessor-type startup validator that fails the host when the registered `IRequestContextAccessor` is not the default `RequestContextAccessor`.
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
- **Explicit opt-outs (the only ones)**: a declared service identity opened through `RunAsServiceAsync` (Information log; Warning when it replaces another service identity or runs over an inbound request with `AllowOverInbound`); `RunAsPrincipalAsync`/`RunRestoredAsync` (Information); `[AllowAnonymous]` (existing `AllowAnonymousBypass` log 8003); ABAC `EnforcementMode.Disabled`, which **now logs** a one-time startup Warning (EventId 9085, #751's planned "Enforcement Disabled" id, allocated by #1705 because it lands first; emitted by the registration validator/hosted startup check), so every gate switch-off leaves a trace; not putting ABAC attributes on requests meant to run without a caller. Identity-binding paths (the scope factory's `RunInboundAsync`/`RunAsPrincipalAsync`/`RunAsServiceAsync`/`RunRestoredAsync` and the internal `RunAsBuiltInAsync`; the setter only preserves the identity it finds, M6) and the `Encina.Testing` builders (build only, never bind) are listed in ADR-035 with how each is logged; there is no public factory, `ForUser` and `CreateAt` are internal, and the explicit-context rule logs and refuses the rest (Design 1).
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
| **A) Core identity, HTTP, Blazor circuits, scope API (incl. `RunRestoredAsync`), PAP seeding, Security/ABAC/Authorization consumers, compliance extractors, EF interceptors, #1635 subset; the rest as follow-up issue files** | One reviewable PR in 7 green phases; no second identity channel remains | Large; SignalR, GraphQL subscriptions, serverless and deferred dispatchers wait for their owners |
| **B) Also `DispatchIdentity` on 7 processors, serverless/SignalR/GraphQL adapters** | Complete | Doubles the PR across 12 more packages; deferred dispatch is owned by SPEC-002 P-50 and must not run as a service identity |

### Chosen Option: **A**

### Rationale

Follow-up issue files written by the worker (the orchestrator opens them, `open-issue` skill). Deferred dispatch is **not** a follow-up of this plan: SPEC-002 REQ-015 / DEC-011 / AC-015 and P-50 (#1164) own it, and #1705 provides `PersistedRequestIdentity` and `RunRestoredAsync` for it.

| Id | Template | Title (draft) |
|---|---|---|
| F1 | `[FEATURE]` | Opt-in declared service identity (`DispatchIdentity`) for jobs with **no originating request**: recurring Hangfire and Quartz jobs, CDC bridge, compliance monitors; logged at startup. Outbox, Inbox, Scheduling, dead-letter replay and Saga/RoutingSlip runners are excluded (P-50) |
| F2 | `[FEATURE]` | Request identity for SignalR hub invocations (hub filter opening `RunInboundAsync` per invocation over the connection-origin marker), streaming hub methods (`IAsyncEnumerable<T>`/`ChannelReader<T>` items), GraphQL subscriptions and raw WebSocket endpoints (supersedes #1306); states how long-lived connections pick up role changes; the interim Anonymous-deny behaviour is pinned by a test in #1705 |
| F3 | `[FEATURE]` | Azure Functions middleware and AWS Lambda helper set the request context through `IRequestIdentityFactory`; remove their own claim options (including their `TenantIdClaimType`); GCP gap noted (SPEC-000 DEC-003) |
| F5 | `[BUG]` | `X-Tenant-ID` header becomes the tenant for anonymous callers and for authenticated callers without a tenant claim; `TenantResolutionMiddleware` can override the authenticated tenant; `ClaimTenantResolver` (`TenancyAspNetCoreOptions.ClaimType`) must read `IRequestIdentityFactory.ResolveTenantId`. Acceptance criterion (M6): tenant resolution moves into `InboundRequestInfo`, and then the accessor setter refuses every tenant change (until then it accepts one only while `!IsDispatchInFlight`) |
| F6 | `[FEATURE]` | ABAC built-in subject attributes from `RequestIdentity` (kind, roles, permissions) when `DefaultAttributeProvider` is used |

F4 (spike on propagating the enqueue-time identity) is dropped in favour of P-50 (#1164); F7 (data-subject fallbacks) is implemented in #1705 (Design 6). Ids are not renumbered so review references stay stable. Orchestrator actions outside this PR: update #1635, #751 plan pointers, #707 and #1704 comments; comment on #1164 with the `RunRestoredAsync` contract (one scope per message, error code only when a message fails); add `"**/Identity/*.cs"` to `FOLDERS` of `.github/workflows/mutation-tests.yml` with `FILTERS` entry `Core.Identity` and a `TIMEOUTS` entry in lock-step (shared hot spot, orchestrator only).

</details>

---

## Implementation Phases

Every phase ends with `dotnet build Encina.slnx --configuration Release` at 0 warnings and the touched test projects green. Phases 1-3 are additive in core and AspNetCore; Phase 4 moves ABAC off the accessor; Phase 5 deletes the Security types once nothing references them.

### Phase 1: Core identity model and claim mapping (merged: #1824, 13a1a493)

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Identity/IdentityKind.cs`** — `public enum IdentityKind { Anonymous, User, Service }`.
2. **`src/Encina/Identity/RequestIdentity.cs`**, **`PersistedRequestIdentity.cs`**, **`RequestContextIdentityExtensions.cs`** (the `UserId` extension property) — sealed class per Design 1: cached `Anonymous`; `internal ForUser(...)` (user-id rule: not blank, trimmed, no control characters, no `service:` prefix `OrdinalIgnoreCase`); `HasClaim` over authenticated identities; `ToPersisted()`; `internal ForService(ServiceIdentityDefinition)` (added in Phase 2); `FrozenSet<string>` with `StringComparer.OrdinalIgnoreCase`. Add `InternalsVisibleTo` for `Encina.Testing` (and `Encina.Security.ABAC` in Phase 2) in `Encina.csproj`. First verify with a throwaway compile that the C# 14 extension property resolves on `RequestContext` and on NSubstitute proxies; otherwise apply the fallback recorded in Design 1.
3. **`src/Encina/Identity/RequestIdentityOptions.cs`** + **`RequestIdentityOptionsValidator.cs`** (`IValidateOptions<RequestIdentityOptions>`) per Design 4.
4. **`src/Encina/Identity/IRequestIdentityFactory.cs`** + **`ClaimsRequestIdentityFactory.cs`** (both internal; sealed, ctor `(IOptions<RequestIdentityOptions>, ILogger<ClaimsRequestIdentityFactory>)`): `Create(ClaimsPrincipal?)`, `ResolveTenantId(ClaimsPrincipal?)`. Small private helpers (`FindFirst(IEnumerable<string>)`, `CollectRoles`, `CollectPermissions`) so each method stays at complexity ≤ 5.
5. **`src/Encina/Abstractions/IRequestContext.cs`** — add `Identity`; remove `UserId` and `WithUserId` from the interface (Design 1); document `Identity` as never null and the extension `UserId` as its projection. Update the XML sample in `src/Encina/Abstractions/IRequestPreProcessor.cs:14-24` (it builds identity from `HttpContext` and calls `WithUserId`, a second population path): replace it with a non-identity example.
6. **`src/Encina/Core/RequestContext.cs`** — store `Identity` (default `RequestIdentity.Anonymous`); delete the `UserId` member and init accessor; copy constructor and `ForNestedDispatch` copy `Identity`, metadata and the internal typed `Origin` flag (`RequestOrigin { Unspecified, Inbound, Scope, Restored }`; `ForNestedDispatch` keeps it); `internal WithIdentity`; `internal CreateAt(DateTimeOffset timestamp, string correlationId, RequestIdentity identity, string? tenantId = null, string? idempotencyKey = null)` and public `CreateAnonymousAt(...)`; add `string? CausationId` to `IRequestContext`/`RequestContext`; **delete `Create(string)`** (its `UtcNow` read) and move `TenantResolutionMiddleware` to `CreateAnonymousAt` with a `TimeProvider`; `CreateForTest(...)` drops `userId` and is anonymous-only; `ToString()` prints kind, not user id. The existing middleware callers (`EncinaContextMiddleware.cs:84`, `TenantResolutionMiddleware.cs:130`) get a minimal compile fix in this phase (Phase 3 rewrites them). `AmbientRequestContext.Resolve` applies the explicit-context rule (Design 1). `RequestContextAccessor` becomes holder-based (Design 2). Migrate the 55 `CreateForTest(... userId: ...)` test call sites to `TestRequestContext.For(TestIdentity.User(...))`; `WithUserId` has no other src caller than `EncinaProperties.cs:206`.
7. **`src/Encina/Diagnostics/RequestIdentityLog.cs`** — `[LoggerMessage]` 162 `AuthenticatedPrincipalWithoutSubject` (Warning: authentication type, configured claim types), 163 `ReservedServiceSubjectRejected` (Warning: claim type), 164 `ConflictingAuthenticatedIdentities` (Warning), 165 `ExplicitContextIdentityConflict` (Warning, both kinds). Phase 1 allocates exactly 162-165; Phase 2 appends 166-174 (no gap between phases). Also add the `encina.identity.kind` tag constant to core `ActivityTagNames` and pass the context's identity kind to `EncinaDiagnostics.SendStarted/StartStreamActivity` so every dispatch activity carries it (unit test for anonymous, user, service).
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
- [LoggerMessage] EventIds 162-165 inside EventIdRanges.Core (100-199; 100-161 used). Never log claim values or user ids.
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
4. **`RequestIdentity`** (Design 1, M6): internal **`ForService(ServiceIdentityDefinition, IdentityIssuer? issuer = null)`** (principal `new ClaimsIdentity(claims, "encina-service")` with `sub = service:<name>`, `encina:identity_kind = service`, declared roles/permissions/claims under the first configured claim types); `ForUser` gains the same optional `issuer`; internal **`IdentityIssuer`** (created first, bound once to the scope holder; live while that holder `IsValid()`) and the internal `Issuer` property; the stored principal keeps only the authenticated identities as plain `ClaimsIdentity` copies (no `Actor`, no `BootstrapContext`, never `identity.Clone()`) and `Principal` returns a clone of it per read (D4, Design 1); `IsSameAs` compares the authenticated claims minus the per-token set, computed once at construction, and at comparison removes the **union** of both identities' sets (type `OrdinalIgnoreCase`, value `Ordinal`, as `HasClaim`). `ForUser` and `ForService` gain `IReadOnlySet<string>? perTokenClaimTypes = null` (the factory passes the configured set; `null`, used by the `TestIdentity` builders, means `RequestIdentityOptions.DefaultPerTokenClaimTypes`). **`RequestIdentityOptions.PerTokenClaimTypes`** (default `exp`, `iat`, `nbf`, `jti`, `uti`, `rh`, `aio`, `nonce`, `at_hash`, `c_hash`, exposed as the public static frozen `DefaultPerTokenClaimTypes`) and the validator rule rejecting subject, role, permission, `amr` and `acr` types.
5. **Scope API** (Design 3): **`src/Encina/Identity/IRequestContextScopeFactory.cs`**, **`IdentityScopeOptions.cs`**, **`InboundRequestInfo.cs`** (public sealed record with the shape of Design 3, the bound constants `MaxIdLength` 128 and `MaxUserAgentLength` 512, and `ForCircuit(principal, correlationId)` for Phase 3; the factory normalizes every client-controlled member and never returns `Left` for one), **`PersistedIdentitySource.cs`** (`Internal = 1`, `External = 2`, no zero value; `RunRestoredAsync` takes it, and `External` restores Anonymous with holder origin `Inbound`, D3), internal **`RequestContextScopeFactory.cs`** (one private `async` method that pushes, awaits `work` and ends) implementing the public interface and the internal **`IInternalRequestContextScopeFactory.cs`** (`RunAsBuiltInAsync` in Phase 2; `RunAnonymousMarkerAsync` added in Phase 3), registered as one singleton behind both interfaces (`TryAddSingleton` of the concrete type plus forwarding registrations, covered by the DI test), **`RequestContextScopeFactoryExtensions.cs`** (the `Task`-returning overloads, result `Either<EncinaError, Unit>`). No `RequestContextScope.cs` and no `TState` overloads. Activity tag `encina.identity.kind` set on `Activity.Current` when a scope opens.
6. **`RequestIdentityErrorCodes.cs`** (Phase 1 has only `ScopeConflict`, `:23`): add `encina.identity.unknown_service_identity`, `encina.identity.reserved_service_identity`, `encina.identity.tenant_conflict` (scope tenant argument and explicit dispatch under a User) and `encina.identity.unsupported_accessor`; factories in **`RequestIdentityErrors.cs`** (fixed messages, no ids from callers in the message beyond the declared service name).
7. **`AddEncinaServiceIdentity(this IServiceCollection, string name, Action<ServiceIdentityBuilder>? configure = null)`** — validates the name eagerly (`ArgumentException`; `encina.` prefix rejected), adds to catalog options (idempotent for an identical declaration), calls `AddEncinaRequestIdentity()`. Internal `AddBuiltInServiceIdentity` for library packages. `AddEncinaRequestIdentity` registers the accessor-type startup validator (`ValidateOnStart`, same message as `unsupported_accessor`).
8. Logs (append to `RequestIdentityLog.cs`, packed after Phase 1's 162-165): 166 `ServiceIdentityScopeOpened` (Information: service name, tenant present, correlation id; Warning instead when it replaces another ambient service identity or runs over an inbound request), 167 `IdentityScopeRefused` (Warning: error code, requested kind), 168 `IdentityScopeClosed` (Debug, from the factory's `finally`), 169 `PrincipalScopeOpened` (Information; Warning when it opens a User identity over a Service ambient), 170 `IdentityScopeOutlivedParent` (Warning: kinds; logged when `End` finds `!holder.IsValid()`), 171 `IdentityRestored` (Information: kind, correlation id), 172 `InboundScopeOpened` (Debug: identity kind, correlation id; once per `RunInboundAsync`), 173 `ScopeTenantChanged` (Information: member, identity kind; no tenant values), 174 `ScopeOpenedOverInbound` (Warning: member, requested kind; logged instead of 166/169 when `AllowOverInbound` opens a scope over an inbound or connection chain). 166's Warning case is therefore only "replaces another ambient service identity". The packed core range is 162-174; verified free on `origin/main` ab4ec134 (`EventIdRanges.Core` is 100-199, `src/Encina` uses 100-165, the last four in `Diagnostics/RequestIdentityLog.cs:14-26`).
9. **Accessor and `Resolve`** (`src/Encina/Core/RequestContextAccessor.cs`, `AmbientRequestContext.cs`):
   - `Pop` (`RequestContextAccessor.cs:140`) becomes `End`: invalidate only; the in-order restore at `:149` and its `inOrder` computation are removed.
   - `ContextHolder` (`:160`) gains immutable `Origin` and `Kind` that survive `Invalidate` (`:189`), plus inherited facts: `Push` (`:114`) and `SetUnchecked` (`:92`) copy the facts of the holder current when they run (its own origin and kind plus what it inherited, ended or not) **before** `LiveOrNull` (`:124`) or `NearestScope` (`:128`) drop anything; the factory's refusals and `Resolve`'s "chain has a User" read them (Q1, Design 3 rule 4).
   - `ContextHolder.ReadContext()` (`:186`) returns null when the held identity's issuer is not live.
   - The setter (`EnsureReplaceable`, `AmbientRequestContext.cs:121`) becomes identity- and origin-preserving with no clear; with no readable context its reference is Anonymous with origin `Unspecified`; it checks and stores `RequestContext.CopyOf(value)` instead of the caller's object (today `RequestContextAccessor.cs:82-83`); a tenant change only while `!IsDispatchInFlight` (`:32`) (Design 1).
   - `CheckExplicitContext` (`:95`) applies the rule order of Design 1: identity rules (stale issuer; a User in the chain and a different identity; no issuer outside an active scope of the same identity) and then the tenant rule on **every** accepted context (`tenant_conflict` when the chain has a User).
   - `RunRestoredAsync` with `PersistedIdentitySource` (D3) and `PersistedRequestIdentity` validation (Design 1), tenant rules and metadata carry-over (Design 3), `InboundRequestInfo` normalization (Design 3, "Inbound input").
   - Sweep core for Encina-owned long-lived loops started lazily (`Task.Run`, `Task.Factory.StartNew`, `new Timer`, `PeriodicTimer`, channel readers started on first use) and start them under `ExecutionContext.SuppressFlow()`; list each one found (or "none") in the PR.
10. **Architecture tests** (`Encina.Testing.Architecture` rules in `tests/Encina.UnitTests/Testing/Architecture`): in production assemblies only `RequestContextScopeFactory` references `RequestIdentity.ForUser/ForService` and `RequestContext.CreateAt` (M3); and **production assemblies do not reference `Encina.Testing`** (#1705 comment of 2026-10-05).
11. **XML docs and builders.**
    - `src/Encina/Abstractions/IEncina.cs`: the explicit `Send` overload's remarks (`:61-64`, "Use this overload from entry points that have no ambient context: background jobs, webhooks, outbox or scheduled-message dispatch") are rewritten. An explicit context needs a live, issued identity, so jobs use `RunAsServiceAsync` and deferred dispatch `RunRestoredAsync`. The `<param name="context">` texts of `Send`, `Publish` and `Stream` (`:53-58`, `:86-90`, `:137-141`) state the full refusal list.
    - `IRequestContextAccessor.cs:60` documents the identity- and origin-preserving setter.
    - `src/Encina.Testing/Identity/TestIdentity.cs` gains **`Service(name, roles, permissions)`** and **`Principal(userId, roles, permissions, claims)`** as builders only (no issuer), and its remarks lose "their test helper arrives with the scope API". `TestRequestContext.cs` remarks drop "the supported way for tests to run a request as a user": it builds contexts for handlers and behaviors called directly; a dispatch with an identity goes through the factory.
12. **Test migrations (budgeted in this phase, PR #1849 review F1).** The setter contract and the issuer rule break existing tests; each is rewritten in Phase 2, never deleted:
    - **Setter.** `git grep -n "\.RequestContext = " origin/main -- tests` finds 86 assignment lines in 11 files: `Core/Identity/AccessorLifetimeTests.cs` (9, e.g. `:94`), `Core/Identity/ExplicitContextConflictTests.cs` (19), `Core/AmbientRequestContextTests.cs` (10), `Core/NestedDispatchContextTests.cs` (26), `Core/RequestContextAccessorTests.cs` (9), `Core/Identity/IdentityDiagnosticsTests.cs` (2), `AspNetCore/EncinaContextMiddlewareTests.cs` (1), `EntityFrameworkCore/SoftDelete/SoftDeleteInterceptorTests.cs` (2), `Tenancy/AspNetCore/TenantResolutionMiddlewareTests.cs` (3), `tests/Encina.ContractTests/Core/EncinaExplicitContextContractTests.cs` (1, `:101`), `tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks/RequestContextAccessorBenchmarks.cs` (4). An assignment that binds an authenticated identity moves into `RunAsPrincipalAsync`/`RunAsServiceAsync`. The Phase 1 setter tests (refusal, two-step bypass, accepted change) are rewritten to the new contract. They assert that an identity- and origin-preserving set is accepted, and that a different identity, a downgrade, a clear, an origin change and a tenant change during a dispatch each throw with Warning 165. Assignments of a substituted accessor are unaffected.
    - **Direct `Push`/`Pop` calls (`Pop` becomes `End`).** `git grep -n -E "RequestContextAccessor\.(Push|Pop)\(" origin/main -- tests` finds 40 calls: 38 in `tests/Encina.UnitTests/Core/Identity/AccessorLifetimeTests.cs` and 2 in `tests/Encina.GuardTests/Core/Identity/RequestIdentityGuardTests.cs:65-66` (the `Push(null!)`/`Pop(null!)` guards). Removing the in-order restore changes what every `Pop` call asserts: the `AccessorLifetimeTests` that expect the parent back after `Pop` move to the delegate API (the frame restores the caller) or assert that `End` only invalidates; the guard becomes `End(null!)`. These are counted in this task, not in the 86 setter lines.
    - **Issuer-less explicit dispatch.** Tests that `Send`/`Publish`/`Stream` a context built by `TestRequestContext.For(...)` **or `TestRequestContext.WithIdentity(...)`** with an authenticated identity move into a factory scope of the same identity, or assert `scope_conflict`: `ExplicitContextConflictTests`; `EncinaExplicitContextContractTests.cs` (`JobContext()` at `:26-27`, used at `:35,57,79,103,106,109`, and `:196`; `:219-221` call a fake `IEncina` and are checked case by case); `tests/Encina.UnitTests/AspNetCore/RequestContextPropagationTests.cs:230-242` (sends a `TestRequestContext.WithIdentity(…, TestIdentity.User("background-job-owner"))` context with no ambient, which rule 4 now refuses: it moves into `RunAsPrincipalAsync` with a `TestIdentity.Principal(...)`, or into `RunAsServiceAsync` with a declared test service if it means a job). Re-list with `git grep -n -E "TestRequestContext\.(For|WithIdentity)\(" origin/main -- tests` (76 lines on ab4ec134) and keep the hits passed to `Send(`, `Publish(` or `Stream(`. `WithIdentity` appears in 5 files: `ExplicitContextConflictTests` and `RequestContextPropagationTests` dispatch with it and are rewritten; `RequestContextIdentityContractTests`, `RequestContextIdentityTests` and `RequestIdentityGuardTests` (`:80-81`, null guards) build contexts without dispatching and stay. `EncinaExplicitContextContractTests` uses only `TestRequestContext.For(...)`. Tests that pass a built context straight to a handler, behavior or orchestrator (for example `EncryptionPipelineIntegrationTests.cs:72,373`, which call `IEncryptionOrchestrator.EncryptAsync`) are builder use and stay.
13. PublicAPI (`PublicAPI.Unshipped.txt`, lines decided by the worker and written by `mechanical-fixer`) and the Phase 2 tests of the Testing section, including the Service branches of `ExcludeSystemAccess` (`AuditedRepositoryTests`, `AuditedReadOnlyRepositoryTests`) and of the `VaryByUser` bypass (`Caching/QueryCachingVaryByUserTests.cs`) through a declared test service and `RunAsServiceAsync`. The PR #1862 review adds these Phase 2 tests:
    - **Fact inheritance (finding 3):** nested dispatch from a dead flow (`Task.Run` from an ended request: `Send(r1)`, whose handler sends `r2`, whose handler calls `RunAsServiceAsync` → `scope_conflict`); two successive sets over an ended inbound chain keep the refusal; a set over a live non-scope holder whose parent is null keeps that holder's facts.
    - **Rule order (finding 11):** an explicit anonymous `Send` inside a user scope, then a nested `Send` with a different live identity → `scope_conflict`; `tenant_conflict` on the rule 4 accept path (issuer-less identity inside an active scope of the same identity, different tenant) and on the rule 6 path.
    - **Setter (finding 9):** with no readable context, an anonymous `CreateAnonymousAt` context is accepted and an authenticated or other-origin one throws; a foreign `IRequestContext` whose `Identity` changes between reads is stored as its `CopyOf` snapshot.
    - **Per-token set (finding 13):** a `TestIdentity` builder identity and a factory identity with the same claims `IsSameAs` each other under default and under custom `PerTokenClaimTypes`.
    - **Principal (D4):** an unauthenticated identity on the caller's principal is absent from `Principal`; a `WindowsIdentity`-like subclass is stored as a plain `ClaimsIdentity`; `Actor` and `BootstrapContext` are not copied.
    - **External restore (D3):** `RunRestoredAsync` with `External` and a persisted User row runs Anonymous, and `RunAsServiceAsync` inside it is refused without `AllowOverInbound` (logs 174 with it); `Internal` keeps origin `Restored`; `default(PersistedIdentitySource)` returns `Left` without invoking `work`.
    - **Inbound input and logs (finding 12):** a correlation id of 10,000 characters or with control characters, an over-long tenant header and idempotency key, and an unparsable IP each open the scope (`Right`, `work` invoked) with the normalized values; 172 on every `RunInboundAsync`, 173 on a tenant change, 174 for `AllowOverInbound`; sentinel values absent from every record.

The former task 11 (claims in `IsSameAs`, refresh through a scope) is decided: per-token claims are excluded and the others count (Q2); a refresh is a new scope (the next request, circuit activity or hub invocation), and the setter keeps throwing on a role change.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan Designs 1-3 and amendment "M6 (scope shape)".
Phase 1 (#1824) added RequestIdentity, IRequestContext.Identity, the internal IRequestIdentityFactory and the holder-based
RequestContextAccessor. Design record: PR #1849 (docs/plans/request-identity-phase2-scope-shape-1705.md on branch
docs/1705-phase2-scope-shape; read it with git show, do not edit it).

TASK
Do every numbered task of the Phase 2 task list (1-13). Where this summary and the task list differ, the task list wins.
- Declared service identities: AddEncinaServiceIdentity(name, builder), catalog options validated with ValidateOnStart
  (pattern ^[a-z0-9][a-z0-9.-]{0,62}$, unique, no wildcard, encina. reserved); internal AddBuiltInServiceIdentity.
- IRequestContextScopeFactory is delegate-only: RunAsServiceAsync, RunAsPrincipalAsync, RunInboundAsync,
  RunRestoredAsync(persisted, PersistedIdentitySource source, work, ct) (internal RunAsBuiltInAsync), each
  Task<Either<EncinaError, T>> over Func<IRequestContext, CancellationToken, Task<Either<...>>>; IdentityScopeOptions(TenantId,
  AllowOverInbound); public InboundRequestInfo record (Principal, CorrelationId, TenantHeaderValue, IdempotencyKey, IpAddress,
  UserAgent, DataRegion; ForCircuit); PersistedIdentitySource { Internal = 1, External = 2 } (no zero value); Task-returning
  extension overloads; no RequestContextScope, no TState overloads. One private async method pushes, awaits work and ends; Push
  never runs in a non-async wrapper.
- Check order: unsupported_accessor -> cancelled token (Left RequestCancelled, before Push, no 167) -> arguments
  (unknown_service_identity, reserved_service_identity, tenant_conflict, RunRestoredAsync validation incl. an undefined source)
  -> holder-chain refusals (User anywhere -> scope_conflict; Inbound or Connection anywhere, ended holders included ->
  scope_conflict unless AllowOverInbound on service/principal, logged 174). A Left never invokes work; work's Left and
  exceptions (OperationCanceledException included) propagate after the holder is invalidated.
- RunInboundAsync never returns Left for client-controlled input: correlation id (blank, > 128 chars or control characters ->
  Activity id or new GUID), tenant header and idempotency key (> 128 or control characters -> dropped), UserAgent (truncated to
  512), unparsable IP and DataRegion > 16 (dropped). Its only Lefts: unsupported_accessor, RequestCancelled, scope_conflict.
- External restore (D3): RunRestoredAsync with External restores Anonymous whatever the row says and gives the holder origin
  Inbound, so RunAsServiceAsync/RunAsPrincipalAsync from that work need AllowOverInbound. Internal keeps origin Restored.
- Accessor: Pop -> End (invalidate only; delete the restore at RequestContextAccessor.cs:149); immutable Origin and Kind on every
  holder, surviving Invalidate. Fact inheritance: Push and SetUnchecked copy the facts of the holder current when they run (its
  own origin and kind plus what it inherited) BEFORE LiveOrNull or NearestScope drop anything, so nested dispatch from a dead
  flow and successive sets keep every fact. ReadContext returns null once the identity's issuer is not live.
- Setter: identity- and origin-preserving, never clears; with no readable context the reference is Anonymous/Unspecified (what
  CreateAnonymousAt builds); checks and stores RequestContext.CopyOf(value), never the caller's object; tenant change only while
  !IsDispatchInFlight; startup validator for a non-default accessor.
- RequestIdentity: one instance per scope with an internal IdentityIssuer (created first, bound once to the holder, live while
  the holder IsValid()). Stored principal = authenticated identities only, copied as plain ClaimsIdentity (no Clone(), no Actor,
  no BootstrapContext); Principal returns a clone of it per read (D4). IsSameAs minus the UNION of both identities' per-token
  sets; ForUser/ForService take perTokenClaimTypes (null = RequestIdentityOptions.DefaultPerTokenClaimTypes: exp, iat, nbf, jti,
  uti, rh, aio, nonce, at_hash, c_hash; auth_time counts); validator rejects excluding subject, role, permission, amr, acr.
- Resolve rule order (Design 1), on the CopyOf snapshot. "Chain has a User" = the current holder's facts, not the readable
  ambient. Identity rules: unauthenticated -> accepted; ended issuer -> scope_conflict even if same; chain has a User and the
  explicit identity is not IsSameAs the ambient -> scope_conflict; no issuer -> accepted only inside an active scope of the same
  identity; live and different with no User in the chain -> accepted + 165; same -> accepted silently. Then the tenant rule on
  EVERY accepted context: chain has a User and a different tenant -> tenant_conflict.
- Logs packed in EventIdRanges.Core after 162-165 (free on origin/main ab4ec134): 166 ServiceIdentityScopeOpened, 167
  IdentityScopeRefused, 168 IdentityScopeClosed (from finally), 169 PrincipalScopeOpened, 170 IdentityScopeOutlivedParent (End
  finds !holder.IsValid()), 171 IdentityRestored, 172 InboundScopeOpened (Debug), 173 ScopeTenantChanged (Information, no tenant
  values), 174 ScopeOpenedOverInbound (Warning, instead of 166/169). Add 166-174 to RequestIdentityLog.cs. Activity tag
  encina.identity.kind; never log user ids, roles, tenants, claim values or EncinaError.Message.
- Architecture tests (identity minting only through RequestContextScopeFactory; production assemblies do not reference
  Encina.Testing); IEncina.cs XML docs (:53-64, :86-90, :137-141); TestIdentity.Service/Principal builders; SuppressFlow sweep.
- Test migrations of task 12: 86 setter lines in 11 files; 40 direct Push/Pop calls (38 in AccessorLifetimeTests, 2 guards in
  RequestIdentityGuardTests.cs:65-66); issuer-less explicit dispatches built with TestRequestContext.For or .WithIdentity,
  including RequestContextPropagationTests.cs:230-242.
- Tests: the Phase 2 rows of the Testing section and every bullet of task 13 (fact inheritance, rule order, setter, per-token
  set, principal, external restore, inbound input and logs 172-174), plus: caller restored by the frame; a call not awaited
  leaves the caller unchanged; synchronous body; a child forked inside a scope cannot change the owner's identity; refusal does
  not invoke work; Left and exception pass-through after invalidation; cancelled token; no identity leaks across two
  RunRestoredAsync iterations; outlived parent across flows logs 170; stale, wrapped-foreign and issuer-less explicit identities
  -> scope_conflict; read-time staleness of an explicit context installed during dispatch; per-token vs amr/acr/auth_time
  claims; principal mutation after construction not observed; stream first enumerated after work vs started inside work;
  unsupported_accessor and startup failure; Service branches of ExcludeSystemAccess and VaryByUser through a declared test
  service; FakeTimeProvider; FakeLogger EventIds with sentinel values.

KEY RULES
ROP (no exceptions for refusals; ArgumentException only for programming errors at registration). TryAdd everywhere.
PowerShell only; Edit tool; no push/PR/issues. PublicAPI.Unshipped.txt via mechanical-fixer. CRAP <= 10 (run the crap-gate
command of "Verification commands"). Per-flag coverage of the Testing section.

REFERENCE FILES
src/Encina/Core/RequestContextAccessor.cs (CurrentHolder :47, setter :77-85, SetUnchecked :92-97, Install :108, Push :114,
LiveOrNull :124-125, NearestScope :128-129, Pop :140, restore :149, ContextHolder :160, ReadContext :186, Invalidate :189),
src/Encina/Core/AmbientRequestContext.cs (IsDispatchInFlight :32, Resolve :72, CheckExplicitContext :95 with the snapshot at
:100-102, EnsureReplaceable :121, AmbientSwap.Apply :316 and SetUnchecked call :334),
src/Encina/Core/RequestContext.cs (CopyOf :176-190), src/Encina/Identity/RequestIdentity.cs (HasClaim :131, ForUser :166,
IsSameAs :187, FreezeClaims :234-240), src/Encina/Diagnostics/RequestIdentityLog.cs, src/Encina/Abstractions/IEncina.cs,
src/Encina.Testing/Identity/TestIdentity.cs, the PR #1849 design record (sections 2-5, Appendix A).
```

</details>

### Phase 3: HTTP integration in `EncinaContextMiddleware`

<details>
<summary><strong>Tasks</strong></summary>

0. **Core support for connection flows** (M6, reuses the Phase 2 holder-origin mechanism): add `RequestOrigin.Connection` (`src/Encina/Core/RequestOrigin.cs`), the internal `AnonymousMarker { Connection, Mask }` and `IInternalRequestContextScopeFactory.RunAnonymousMarkerAsync(marker, Func<CancellationToken, Task> work, ct)` returning `Task<Either<EncinaError, Unit>>` (Design 3). `Connection` pushes an anonymous holder of origin `Connection`, over which `RunAsServiceAsync`/`RunAsPrincipalAsync` are refused without `AllowOverInbound` and `RunRestoredAsync` is refused, while `RunInboundAsync` is permitted. `Mask` pushes an anonymous holder that keeps the chain's facts, so the refusals still see the inbound or User context it hides. Grant `InternalsVisibleTo` from `Encina` to `Encina.AspNetCore` and `Encina.AspNetCore.Blazor`, and extend the M3 architecture test's allow-list (these assemblies reach only `RunAnonymousMarkerAsync`).
1. **`src/Encina.AspNetCore/EncinaContextMiddleware.cs`** — the class becomes **`internal sealed`** (CS0051 decision, Design 2: a public `InvokeAsync` cannot take the internal `IInternalRequestContextScopeFactory`; resolving it from `RequestServices` is rejected). `InvokeAsync(HttpContext, IRequestContextScopeFactory, IInternalRequestContextScopeFactory)` per Design 2's flow:
   - first, when the latch is set, **500 without calling `next`** (MQ-1 (b));
   - then the connection skip runs `next` under `RunAnonymousMarkerAsync(Connection)`. `IsConnectionRequest(HttpContext)` is the predicate of Design 2 step 1: `IHttpUpgradeFeature.IsUpgradableRequest` with an `Upgrade` header listing `websocket`, `IHttpExtendedConnectFeature.IsExtendedConnect` with `Protocol` `websocket`, or `context.WebSockets.IsWebSocketRequest`; an `Accept` header listing `text/event-stream` (parsed media types, wildcards excluded, raw search when parsing fails); or a `HubMetadata` endpoint;
   - otherwise it builds `InboundRequestInfo` with `context.CreateInboundRequestInfo()` (task 11) and runs `next` inside `RunInboundAsync` (Origin `Inbound`, TimeProvider inside the factory; no `finally`);
   - a `Left` of `RequestCancelled` returns without calling `next`, writing no status code and logging nothing; any other `Left` answers **500 without calling `next`** (the factory already logged 167);
   - the misorder check: endpoint null before `next` **and the endpoint after `next` carries `HubMetadata`** (D1) logs Critical **202 `EncinaContextBeforeRouting`** once and sets the latch that makes every later request answer 500. The latch is an **instance field** (`int`, `Interlocked.Exchange`), never static. Register 202 in `EncinaContextMiddleware`'s log class inside `EventIdRanges.AspNetCore` 200-249 (200-201 are used by `AuthorizationPipelineBehavior.cs:88,94`).

   Extract `IsConnectionRequest(...)`, `IsMisordered(Endpoint? before, Endpoint? after)` and `ResolveTenant(...)` helpers (the `BuildInboundRequest` logic becomes the public builder of task 11); delete the claim part of the tenant extraction (`EncinaContextMiddleware.cs:136` reads `_options.TenantIdClaimType`; the header fallback stays). Phase 1 already removed the user-id extraction and stamps the interim anonymous context with `CreateAnonymousAt` (`:85`). Document "`UseEncinaContext` after `UseRouting`" in the XML docs of `ApplicationBuilderExtensions.UseEncinaContext`.
2. **`src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs`** — delete `UserIdClaimType`, `TenantIdClaimType`; fix the XML sample `options.UserIdClaimType = "sub";` in `ServiceCollectionExtensions.cs:65`.
3. **`src/Encina.AspNetCore/ServiceCollectionExtensions.cs`** — `AddEncinaAspNetCore` calls `AddEncinaRequestIdentity()`; **delete `IPrincipalResolver`, its default HTTP implementation and the `HttpContextAccessor` registration made only for it**; `AddEncinaAuthorization` registers `AuthorizationPipelineBehavior<,>` with `TryAddEnumerable`. `AuthorizationPipelineBehavior` evaluates `context.Identity.Principal` and denies `authorization.unauthenticated` when `!Identity.IsAuthenticated` (Design 2). **User id out of its logs (PR #1862 review, finding 4):** EventIds 200 and 201 (`AuthorizationPipelineBehavior.cs:85-95`) drop `{UserId}` and record the identity kind (EventIds kept); the `userId` local (`:176`) and its call sites (`:182,210,243,280,309,333`; `:164` passes `null`) pass the kind; the `["userId"]` error details (`:221,254,291,320`) become `["identityKind"]`.
4. **`src/Encina.AspNetCore.Blazor`** — delete `AuthenticationStatePrincipalResolver`; add **`internal sealed`** `RequestIdentityCircuitHandler` (CS0051, as task 1; registered by `AddEncinaBlazorAuthorization`; Design 2: `RunInboundAsync` around `next(activity)` from the current `AuthenticationState`; on `Left`, `next(activity)` inside `RunAnonymousMarkerAsync(Mask)`) and its registration. Add `InternalsVisibleTo` for `Encina.GuardTests` and for the benchmark assembly of task 10 (`Encina.AspNetCore.Benchmarks`) here and in `Encina.AspNetCore.csproj` (today both grant it only to UnitTests, IntegrationTests, ContractTests and PropertyTests).
5. **`src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs`** — the fallback context already uses `RequestContext.CreateAnonymousAt(timeProvider.GetUtcNow(), ...)` on main (`:126-130`); take the `TimeProvider` by method injection. **On WebSocket upgrade requests, `Accept: text/event-stream` requests and `HubMetadata` endpoints skip only the context write** (`:117-121`, which runs only when a tenant resolved) with the same predicate as the middleware, WebSocket detection through `IHttpUpgradeFeature`/`IHttpExtendedConnectFeature` included (private copy, no new ProjectReference), so no never-invalidated holder carries the connect-time tenant. Tenant resolution (`:85`), validation (`:88-97`) and the `RequireTenant` 400 (`:100-113`) still run on those requests: the `Accept` header is client-controlled, so a skip at the start would bypass them (finding 14). The setter call at `:120` stays legal under the M6 setter rule (identity- and origin-preserving, outside a dispatch).
6. **`src/Encina.EntityFrameworkCore`** — remove the `GetService<IRequestContext>()` fallback from `AuditInterceptor.cs:267,380`, `SoftDeleteInterceptor.cs:166` and `QueryCacheInterceptor.cs:382`; they read only `IRequestContextAccessor`. **User id out of their logs (finding 4):** `SoftDeleteInterceptor` EventId 3050 (`SoftDeleteInterceptor.cs:182-189`, called at `:150`) and `AuditInterceptor` EventId 3000 (`AuditInterceptor.cs:498-506`, called at `:213`) log "by user {UserId}"; both record the identity kind instead (EventIds kept, templates updated). Rewrite `docs/features/audit-tracking.md:682-703` (Phase 7).
7. PublicAPI: remove the deleted `Encina.AspNetCore` symbols from `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` (not Shipped), including the three `EncinaContextMiddleware` lines (`:24-26`) now that the class is internal, and the `AuthenticationStatePrincipalResolver` lines of `src/Encina.AspNetCore.Blazor/PublicAPI.Unshipped.txt`; add the new ones (the task 11 builder); README section stub (full docs in Phase 7).
8. Existing tests to rewrite: `tests/Encina.UnitTests/AspNetCore/EncinaContextMiddlewareTests.cs` (new constructor/`InvokeAsync` signature at `:25-27` and its 13 `InvokeAsync(context, _accessor)` calls; the claim cases at `:73-139` move to `ClaimsRequestIdentityFactoryTests`), `EncinaAspNetCoreOptionsTests.cs:16-40` and `ServiceCollectionExtensionsTests.cs:103-113` (drop the deleted options), the `AuthorizationPipelineBehavior` tests, `PolicyBasedAuthorizationTests.cs:385-408` (resolve the behavior from DI instead of building it by hand), and the EF Core interceptor tests that register an `IRequestContext`.
9. New tests: `EncinaContextMiddlewareIdentityTests` (TestServer, extends the `RequestContextPropagationTests` pattern), `AspNetCoreIdentityRegistrationTests` (including `AddEncinaAuthorization` alone), `AuthorizationIdentityTests` (no-subject and reserved-subject tokens denied on an `[Authorize]` request), `RequestIdentityCircuitHandlerTests` (inside an activity the circuit user; outside Anonymous; the `Left` branch dispatches Anonymous under the masking scope and refuses `RunAsServiceAsync`), a concurrency test (N parallel TestServer requests with distinct principals, each handler asserting its own user id), a captured-task test (task started during a request, run after completion, is denied, and its `RunAsServiceAsync` is refused, Q1) and the middleware `Left` test (`UseEncinaContext` twice → 500, `next` not called). **TestServer tests over the WebSocket transport** (`ConnectionFlowIdentityTests`): before F2 a hub method and a raw WebSocket endpoint read Anonymous, and `RunAsServiceAsync` there is refused without `AllowOverInbound`; a circuit activity reads the identity of the current `AuthenticationState`, and a changed `AuthenticationState` applies at the next activity; `TenantResolutionMiddleware` writes no context on a connection request, and still answers 400 on one when `RequireTenant` is set and no tenant resolves (finding 14).

   **Tests that run in a misordered pipeline (finding 8).** A negotiate request trips the latch (D1), so every test below that uses `UseRouting` after `UseEncinaContext` builds a **fresh host per case** and makes the connection its **first request**, or connects the SignalR client with `HttpConnectionOptions.SkipNegotiation = true` over WebSockets; none shares a host with the 500-after-detection test.
   - **WebSocket still skipped when misordered:** fresh host, `SkipNegotiation`, the WebSocket connection is the first request; while it is open a hub method reads Anonymous under the connection marker. `next` returns only when the connection closes; the endpoint then carries `HubMetadata`, so the close trips the latch and logs 202 (the pipeline is misordered, D1), and the test asserts that too.
   - **SSE skip** (MQ-1 (c)): a request with `Accept: text/event-stream`, with and without an endpoint, in both pipeline orders, each on a fresh host as the first request (the misordered case uses a plain, non-hub SSE endpoint so no negotiate runs); it runs under the connection marker, reads Anonymous and the connect-time user never reaches a dispatch; the same request through `TenantResolutionMiddleware` gets no context write.
   - **500 after detection** (MQ-1 (b), D1): own host; with `UseEncinaContext` before `UseRouting` the first negotiate request logs Critical 202 once, every later request, connection or not, is answered 500 and `next` is not called; with the correct order no latch is set.
   - **No latch from re-routing (D1, finding 2):** correctly ordered pipeline with `UseEncinaContext` before `UseRewriter` and before `UseStatusCodePagesWithReExecute`; a rewritten URL and a 404 re-executed to an error endpoint both succeed, no 202 is logged and later requests are not answered 500. A second host built in the same test process is not affected by a first host's latch (instance field).

   **WebSocket detection without the TestServer feature (finding 1).** `TestServer` sets `IHttpWebSocketFeature` up front (`WebSocketClient.cs:92,129`), so it cannot prove the detection. `ConnectionRequestPredicateTests` runs a table of `DefaultHttpContext` cases with **no** `IHttpWebSocketFeature`: a stub `IHttpUpgradeFeature { IsUpgradableRequest = true }` with `Upgrade: websocket` (and `Upgrade: h2c, WebSocket`) is a connection request; the same stub with `Upgrade: h2c` is not; a stub `IHttpExtendedConnectFeature { IsExtendedConnect = true, Protocol = "websocket" }` is; `Accept: text/event-stream;q=0`, `Accept: TEXT/EVENT-STREAM` and a malformed `Accept` containing `text/event-stream` are; `Accept: */*` and `Accept: text/*` are not. The same table runs against the private copy in `TenantResolutionMiddleware`. One **Kestrel loopback** test (`WebApplication` on port 0, in process, no external service) maps a raw WebSocket endpoint with `UseWebSockets` **after** `UseEncinaContext`, connects a `ClientWebSocket` with an authenticated principal, and asserts the endpoint reads Anonymous under the connection marker.

   **Other Phase 3 tests from the PR #1862 review.** Middleware `RequestCancelled`: a request whose `RequestAborted` is already cancelled gets no 500 and `next` is not called (finding 12). A 10,000-character `X-Correlation-ID` gets a normal response with a generated correlation id (finding 12). Sentinel user ids absent from EventIds 200/201 and the authorization error details, 3050 and 3000 (finding 4). Ordinary SSE opt-in (D2, task 11): an endpoint that streams three events, each produced inside `RunInboundAsync(context.CreateInboundRequestInfo(), …)`, sees the user in each event's dispatch and Anonymous between events.
10. **Benchmark (Q4).** `tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks/`: a BenchmarkDotNet benchmark of `EncinaContextMiddleware` (one `RunInboundAsync` per request) and of the per-activity circuit scope, next to `RequestContextAccessorBenchmarks.cs` (`BenchmarkSwitcher`, `--list flat --filter` checked first, results under `artifacts/performance/`). `TState` overloads are added only if it shows the closure matters.
11. **Public `InboundRequestInfo` builder (D2).** `src/Encina.AspNetCore/HttpContextInboundRequestExtensions.cs`: public `InboundRequestInfo CreateInboundRequestInfo(this HttpContext context)` reads `context.User`, the correlation id (the `CorrelationIdHeader`, else `Activity.Current`), the `TenantIdHeader` fallback, the idempotency key, IP, `UserAgent` and `DataRegion` with the header names of `EncinaAspNetCoreOptions` resolved from `context.RequestServices` (defaults when not registered). `EncinaContextMiddleware` step 2 uses it, so there is one builder. XML docs and the how-to state the contract: call it inside an SSE endpoint and pass the result to `RunInboundAsync` around **one event or one dispatch**, never around the whole stream; the principal is the connection's, so the endpoint decides when it is stale. Unit and guard tests; PublicAPI line through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT
Worktree D:\Proyectos\Encina\.claude\worktrees\w1705, issue #1705, plan Design 2 and amendment "M6 (scope shape)". Core now has
RequestIdentity and the delegate-only IRequestContextScopeFactory (RunInboundAsync, RunAsPrincipalAsync, RunAsServiceAsync,
RunRestoredAsync with PersistedIdentitySource; the internal IRequestIdentityFactory is not callable here). InboundRequestInfo is a
public record whose client-controlled members the factory normalizes; RunInboundAsync never returns Left for them.

TASK
Do every numbered task of the Phase 3 task list (0-11). Where this summary and the task list differ, the task list wins.
- Task 0: RequestOrigin.Connection, internal AnonymousMarker { Connection, Mask } and
  IInternalRequestContextScopeFactory.RunAnonymousMarkerAsync (Func<CancellationToken, Task> work); InternalsVisibleTo
  from Encina to Encina.AspNetCore and Encina.AspNetCore.Blazor; architecture-test allow-list.
- EncinaContextMiddleware becomes internal sealed (CS0051: a public InvokeAsync cannot take the internal
  IInternalRequestContextScopeFactory; resolving it from RequestServices is rejected). InvokeAsync(HttpContext,
  IRequestContextScopeFactory, IInternalRequestContextScopeFactory):
  1. latch set -> 500 without calling next;
  2. IsConnectionRequest -> next under RunAnonymousMarkerAsync(Connection). The predicate: IHttpUpgradeFeature.IsUpgradableRequest
     with an Upgrade header listing "websocket", or IHttpExtendedConnectFeature.IsExtendedConnect with Protocol "websocket", or
     context.WebSockets.IsWebSocketRequest; or an Accept header listing the media type text/event-stream (parsed, OrdinalIgnoreCase,
     q ignored, */* and text/* do not count, raw search when parsing fails); or a HubMetadata endpoint. Do not rely on
     IsWebSocketRequest alone: it is false until UseWebSockets runs, and SignalR adds UseWebSockets inside its endpoint;
  3. otherwise info = context.CreateInboundRequestInfo() (task 11) and next inside scopeFactory.RunInboundAsync(info, ...)
     (identity, tenant, TimeProvider timestamp and Origin Inbound set inside the factory; holder invalidated when the work
     completes; no finally);
  4. Left RequestCancelled -> return, no status code, no log, next not called; any other Left -> 500, next not called;
  5. endpoint null before next AND the endpoint after next carries HubMetadata (D1) -> Critical 202 EncinaContextBeforeRouting
     once and set the latch. The latch is an instance field (int + Interlocked.Exchange), never static. UseRewriter,
     UseStatusCodePagesWithReExecute and 404s never trip it.
- AuthorizationPipelineBehavior on context.Identity.Principal, authorization.unauthenticated when not authenticated; EventIds
  200/201 log the identity kind, never {UserId}; error details ["userId"] -> ["identityKind"].
- Blazor: internal sealed RequestIdentityCircuitHandler (RunInboundAsync per activity; Left -> next(activity) under
  RunAnonymousMarkerAsync(Mask)); InternalsVisibleTo for Encina.GuardTests from Encina.AspNetCore and Encina.AspNetCore.Blazor.
- TenantResolutionMiddleware: same predicate (private copy); on a connection request skip ONLY the context write (:117-121);
  resolution, validation and the RequireTenant 400 still run; TimeProvider by method injection.
- EF interceptors: no GetService<IRequestContext>() fallback; EventIds 3000 and 3050 log the identity kind, never {UserId}.
- Task 11: public HttpContext.CreateInboundRequestInfo() in Encina.AspNetCore, used by the middleware and by ordinary SSE
  endpoints with RunInboundAsync per event or per dispatch, never for the whole stream (D2).
- Delete EncinaAspNetCoreOptions.UserIdClaimType/TenantIdClaimType; AddEncinaAspNetCore calls AddEncinaRequestIdentity();
  IPrincipalResolver removal; AddEncinaAuthorization registers its behavior with TryAddEnumerable; PublicAPI removals incl. the
  EncinaContextMiddleware lines (:24-26) and the Blazor resolver lines.
- Tests (task 9): every misordered-pipeline test on a fresh host with the connection as first request or SkipNegotiation;
  ConnectionRequestPredicateTests over DefaultHttpContext with stub upgrade/extended-CONNECT features and no
  IHttpWebSocketFeature, run against both predicate copies; one Kestrel loopback test with UseWebSockets after UseEncinaContext;
  no latch from UseRewriter/StatusCodePages re-execute or a 404; second host unaffected by a first host's latch; RequestCancelled
  -> no 500; 10,000-char X-Correlation-ID -> normal response; SSE opt-in per event; TenantResolutionMiddleware 400 still on a
  connection request; sentinel user ids absent from 200/201, authorization error details, 3000 and 3050.
- TestServer identity tests (pattern tests/Encina.UnitTests/AspNetCore/RequestContextPropagationTests.cs): authenticated
  (Kind=User, roles, permissions), anonymous, authenticated without subject (Anonymous + Warning 162), sub "service:x"
  (Anonymous + Warning 163), custom UserIdClaimTypes, FakeTimeProvider timestamp, no identity leak between sequential requests,
  custom RequestIdentityOptions honoured; no public identity factory exists.
- DI: AddEncinaAspNetCore alone with ValidateOnBuild/ValidateScopes resolves IRequestContextScopeFactory and IRequestContextAccessor.

KEY RULES
Keep InvokeAsync complexity low (helpers); CRAP <= 10. No new ProjectReference. PowerShell only; no commit/push/issues.
PublicAPI lines via mechanical-fixer.

REFERENCE FILES
src/Encina.AspNetCore/EncinaContextMiddleware.cs, EncinaAspNetCoreOptions.cs, ServiceCollectionExtensions.cs,
ApplicationBuilderExtensions.cs, AuthorizationPipelineBehavior.cs (:85-95, :176, :221,254,291,320), PublicAPI.Unshipped.txt (:24-26);
src/Encina.AspNetCore.Blazor/{AuthenticationStatePrincipalResolver,ServiceCollectionExtensions}.cs;
src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs (:85, :88-97, :100-113, :117-121, :126-130);
src/Encina.EntityFrameworkCore/SoftDelete/SoftDeleteInterceptor.cs (:150, :166, :182-189),
Auditing/AuditInterceptor.cs (:213, :267, :380, :498-506), Caching/QueryCacheInterceptor.cs (:382).
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
2. **Hosted-service scenario** in the same class: a loop that sends an ABAC-guarded request inside `RunAsServiceAsync` is permitted by a policy targeting `service:` subjects; without the scope it is denied. **DSR case:** `RunAsServiceAsync` + a `[RestrictProcessing]` request without a subject property returns the DSR subject-missing error. **Escalation cases:** an anonymous HTTP request calling `RunAsServiceAsync` gets `Left(scope_conflict)`; a task started inside `RunAsServiceAsync` and run after its work completed is denied.
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
2. **ADR-035** `docs/architecture/adr/035-one-request-identity-model.md` (records: the single model, the extension-property `UserId`, the trusted identity-creation paths and how each is logged, the persisted form for P-50, the Disabled-mode warning, the entry-point table; and the **Phase 2 scope-shape decision (M6)** with the PR #1849 probe (its Appendix A) as evidence:
   - the delegate API (`RunInboundAsync`, `RunAsPrincipalAsync`, `RunAsServiceAsync`, `RunRestoredAsync`, internal `RunAsBuiltInAsync`) is the only trusted path that **binds** an identity, and a scope invalidates when its work completes; the internal `RunAnonymousMarkerAsync` (connection marker, Blazor mask) only downgrades;
   - it guards against accidental misuse, **not against hostile in-process code** (the factory is reachable from a flow with no holder, for example under `ExecutionContext.SuppressFlow()`); the same holds for the Q1 dead-flow refusal;
   - `RunAsPrincipalAsync` binds **any** principal the caller holds (Information log): it is the public path for non-HTTP hosts that authenticate callers themselves, refused over a User or inbound chain, and the same reasoning that dropped the test-only binding seam applies to it, so it is listed as a trusted path and not presented as a security boundary;
   - the no-issuer rule and the rule order of Design 1, read-time issuer liveness, the identity- and origin-preserving setter, the `Encina.Testing` builders (build only, never bind), the connection-endpoint rule (WebSocket upgrades detected from the request, `Accept: text/event-stream` requests and `HubMetadata` endpoints skipped under the anonymous connection marker; the marker requiring `AllowOverInbound` is a maintainer-confirmed decision, MQ-2; ordinary SSE endpoints opt in per event or per dispatch, D2) and the `UseRouting` ordering rule (Critical 202 when a hub endpoint is reached through a misordered pipeline, then 500 to every later request from that middleware instance, MQ-1 (b) with the D1 trigger);
   - the persisted-identity source (D3): `PersistedIdentitySource.External` restores Anonymous with origin `Inbound`, so untrusted messages cannot open service or principal scopes without the logged opt-out; the stored principal keeps only authenticated identities as plain `ClaimsIdentity` copies (D4);
   move the 035 row from the Reserved table to the ADR table in `index.md`.
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
| `EncinaContextMiddleware` | Becomes `internal sealed`; runs `next` inside `RunInboundAsync` with the info from the public `CreateInboundRequestInfo()` (identity through the factory; TimeProvider; holder invalidated when the work completes; origin `inbound`); connection requests (WebSocket upgrade detected from the request, `Accept: text/event-stream`, `HubMetadata`) run under the anonymous connection marker; `Left` `RequestCancelled` → no response, any other `Left` → 500; misordered routing (endpoint after `next` carries `HubMetadata`, D1) logged Critical 202, then 500 on every later request (instance latch) |
| `AuthorizationPipelineBehavior` | Evaluates `context.Identity.Principal`; denies unauthenticated; `IPrincipalResolver` deleted; EventIds 200/201 and error details carry the identity kind, not the user id |
| Blazor `AuthenticationStatePrincipalResolver` | Replaced by the internal `RequestIdentityCircuitHandler` |
| EF Core `AuditInterceptor`, `SoftDeleteInterceptor`, `QueryCacheInterceptor` | `GetService<IRequestContext>()` fallback removed; accessor only; EventIds 3000 and 3050 log the identity kind, not the user id |
| Ordinary SSE endpoints | Read Anonymous under the connection marker; opt in per event or per dispatch with `CreateInboundRequestInfo()` + `RunInboundAsync` (D2) |
| `DefaultDataSubjectIdExtractor`, `ConsentRequiredPipelineBehavior`, GDPR `ILawfulBasisSubjectIdExtractor` | Context user-id fallback only for `Kind == User` (Service and Anonymous take the fail-closed path) |
| `TenantResolutionMiddleware` | Fallback context via `CreateAnonymousAt(TimeProvider)`; on WebSocket upgrade requests, `Accept: text/event-stream` requests and `HubMetadata` endpoints skips only the context write (resolution, validation and the `RequireTenant` 400 still run); its setter call stays legal under the M6 setter rule |
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
| `Identity/RequestContextScopeFactory.cs`, `RequestContextScopeFactoryExtensions.cs`, `IdentityScopeOptions.cs`, `InboundRequestInfo.cs` | unit, guard, contract | Contract test pins invalidate-on-completion, refusal without invoking `work` and never-throws-for-refusal (`RequestContextScope.cs` is not created, M6) |
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

**Rules for rewritten tests.** Every consumer that reads `Identity` is tested with real contexts built by `TestRequestContext.For(TestIdentity.User/Service/Anonymous(...))` from `Encina.Testing` (core `CreateForTest` is anonymous-only and `CreateAt` is internal), never `UserId.Returns`. Those builders serve handlers and behaviors called directly; a test that **dispatches** with an identity, or needs one ambient, binds it through the factory (`RunAsPrincipalAsync` with a `TestIdentity.Principal(...)`, or `RunAsServiceAsync` with a service the test host declares), never through the setter (M6). The private `IRequestContext` doubles in other test projects are updated by compile only. No claim value, user id, role or tenant reaches a log or tag: tests use sentinel values in `ClaimsRequestIdentityFactoryTests`, `RequestContextScopeFactoryTests` (166-174), `EncinaContextMiddlewareIdentityTests`, `AuthorizationIdentityTests` (EventIds 200/201 and the authorization error details), `SoftDeleteInterceptorTests` (3050) and `AuditInterceptorTests` (3000) (PR #1862 review, finding 4), the Security gate tests (`ObservabilityTests`: tags contain `encina.identity.kind` and no `security.user_id`, user id or role) and assert absence from every `FakeLogger` record (formatted message and structured state) and from `Activity` tags.

| Flag | Project | Classes |
|---|---|---|
| Unit (core identity and catalog) | `Encina.UnitTests` | `Core/Identity/RequestIdentityTests` (invariant, cached anonymous, `ForUser` guards incl. `service:` prefix, case-insensitive sets); `Core/Identity/ClaimsRequestIdentityFactoryTests` (null/unauthenticated → anonymous, authenticated without subject → anonymous + 162, reserved subject → anonymous + 163, precedence sub > NameIdentifier > oid and custom order, `RoleClaimType` honoured, permission separator, tenant only when authenticated); `Core/Identity/RequestIdentityOptionsValidatorTests`; `Core/RequestContextIdentityTests` (`With*`, `ForNestedDispatch` keep identity, `CreateAt` timestamp); `Core/Identity/ServiceIdentityCatalogTests` + validator |
| Unit (scope API, Phase 2) | `Encina.UnitTests` | `Core/Identity/RequestContextScopeFactoryTests` (caller restored by the frame, a `Run*Async` call not awaited leaves the caller's ambient unchanged, synchronous body restores the caller, refusal without invoking `work`, `Left` and exception pass-through after invalidation, cancelled token → `RequestCancelled` without `Push`, `unsupported_accessor`, outlived parent across flows logs 170, `Task.Run` isolation, refusal over a User and over an Inbound chain including ended holders and a set over an ended inbound chain (Q1), `AllowOverInbound` logged 174, unknown name, service-over-service log, one identity per scope, correlation kept, FakeTimeProvider, FakeLogger 166-174, activity tag; the factory tests use FakeLogger 162-165); `Core/Identity/ScopeBoundaryTests` (a child forked inside a scope cannot change the owner's identity; no identity leaks across two `RunRestoredAsync` iterations; a stream **first enumerated after** `work` runs under the enumerating caller's context, a stream **started inside** `work` reads Anonymous after it; principal mutation after construction is not observed); `Core/Identity/ServiceIdentityDispatchTests`; `Core/Identity/IdentityRegistrationTests` (`AddEncina`, `AddEncinaServiceIdentity`, `ValidateOnBuild`+`ValidateScopes`, `ValidateOnStart` failures via `Host.StartAsync`) |
| Unit (AspNetCore, end to end) | `Encina.UnitTests` | `AspNetCore/EncinaContextMiddlewareIdentityTests`; `AspNetCore/AspNetCoreIdentityRegistrationTests`; `AspNetCore/RequestIdentityEndToEndTests` (authenticated → handler sees Kind=User and same `UserId` in PAP/audit capture; anonymous → `abac.unauthenticated_caller`, handler not invoked; no-subject → denied + 162; custom `UserIdClaimTypes` honoured by ABAC and audit; `[DenyAnonymous]`/`[RequirePermission]` on the same identity; no leak between requests; hosted-service scenario) |
| Unit (Security, ABAC, Audit) | `Encina.UnitTests` | `Security/ABAC/ABACRegistrationTests` (ABAC alone, both orders with `AddEncina`); `Security/ABAC/ABACPipelineBehaviorTests`, `ABACRequirementEnforcementTests` (rebuilt on real contexts); `Security/ABAC/Persistence/PersistentPolicyAdministrationPointFailClosedTests`, `PolicySeedingServiceIdentityTests`; `Security/SecurityPipelineBehaviorTests`, `EvaluatorTests`, `ServiceCollectionExtensionsTests`, `ObservabilityTests`, `BehaviorRegistrationOrderTests`; `Security/Audit/AuditedRepositoryExcludeSystemAccessTests`; `Testing/Architecture/EncinaEventIdAllocationTests` (unchanged map, must pass) |
| Unit (diagnostics, explicit contexts, setter, gates) | `Encina.UnitTests` | `Core/Identity/IdentityDiagnosticsTests` (`encina.identity.kind` on `Encina.Send` for anonymous, user, service); `Core/Identity/ExplicitContextConflictTests` (rewritten to the rule order of Design 1: explicit-context `Send` over an ambient user returns `Left(scope_conflict)` and logs 165; a stale explicit identity, including one wrapped in a foreign `IRequestContext`, and an issuer-less one outside an active scope of the same identity each return `scope_conflict`; an explicit context installed during dispatch reads Anonymous once its issuer ends; a different tenant under an ambient User returns `tenant_conflict`; per-token claim changes are the same identity, `amr`/`acr`/`auth_time` changes a different one); `Core/Identity/AccessorLifetimeTests` (dead-flow `Task.Run`, a child cannot end the owner's scope, parallel flows; setter contract: identity- and origin-preserving set accepted, a different identity, a downgrade, a clear, an origin change and a tenant change during dispatch throw with 165); `Core/Identity/TenantBindingTests`; `Core/Identity/RunRestoredAsyncTests`; `Core/Identity/ServiceIdentityGateTests` (Service branches of `ExcludeSystemAccess` and the `VaryByUser` bypass through a declared test service and `RunAsServiceAsync`); `Testing/Architecture` (identity minting only through `RequestContextScopeFactory`; production assemblies do not reference `Encina.Testing`); `AspNetCore/ConnectionFlowIdentityTests` (Phase 3, WebSocket TestServer, a fresh host per misordered case); `AspNetCore/ConnectionRequestPredicateTests` (Phase 3: `DefaultHttpContext` table without `IHttpWebSocketFeature`, run against the middleware and the `TenantResolutionMiddleware` copies, plus the Kestrel loopback WebSocket test); `Core/Identity/HolderFactInheritanceTests` (Phase 2: nested dispatch from a dead flow, successive sets) |
| Unit (authorization, circuit, rewritten files) | `Encina.UnitTests` | `AspNetCore/AuthorizationIdentityTests`, `RequestIdentityCircuitHandlerTests`, `AspNetCoreIdentityRegistrationTests` (incl. `AddEncinaAuthorization` alone); existing files rewritten (named in Phases 3-5): `EncinaContextMiddlewareTests`, `EncinaAspNetCoreOptionsTests`, `AspNetCore/ServiceCollectionExtensionsTests`, `PersistentPolicyAdministrationPointTests`, `PersistentPolicyAdministrationPointAuditTests`, `ABACPolicySeedingHostedServiceTests`, `AuditedRepositoryTests`, `AuditedReadOnlyRepositoryTests`, `EncinaPropertiesTests`, `EncinaArbitrariesTests`; DSR/Consent/GDPR extractor and gate tests with Service and Anonymous identities; EF Core interceptor tests (accessor only). The end-to-end TestServer tests stay in UnitTests: they are the cheapest place for in-memory hosting and run in-process with no external service (location justified in the Integration row). |
| Guard | `Encina.GuardTests` | `Core/Identity/RequestIdentityGuardTests`, `ClaimsRequestIdentityFactoryGuardTests`, `RequestContextScopeFactoryGuardTests`, `ServiceIdentityRegistrationGuardTests`, `RequestContextGuardTests` (`WithIdentity`, `CreateAt`); `Security/ABAC/ABACPipelineBehaviorGuardTests` (new constructor); `Security/SecurityPipelineBehaviorGuardTests`, evaluator guards; `AspNetCore/EncinaContextMiddlewareGuardTests`; `Security/` guard tests for attributes, evaluators, health check, `ServiceCollectionExtensions` and `SecurityPipelineBehavior` sized to reach the Encina.Security 15% guard target; guards for the DSR/Consent/GDPR changes, `TestIdentity` and the circuit handler |
| Contract | `Encina.ContractTests` | `Core/RequestContextIdentityContractTests` (counts toward `Encina` `Core/RequestContext.cs`; a contract over `RequestContext` as produced by every factory and transformer: `Create`-replacement `CreateAt`, `CreateForTest`, `With*`, `ForNestedDispatch`, plus `TestInfrastructure`'s `TestRequestContext`; `Identity` non-null, `UserId == Identity.UserId`); `Core/RequestContextScopeFactoryContractTests` (identity creation only through the scope factory: `RunAsPrincipalAsync`, `RunInboundAsync`, `RunRestoredAsync` honour `RequestIdentityOptions`; the middleware case sits in UnitTests because ContractTests does not reference `Encina.AspNetCore`; the factory is internal so no custom-factory contract exists; also invalidate-on-completion, never throws for refusals, `work` not invoked on `Left`); `Security/SecurityPipelineBehaviorContractTests` (counts toward the only contract-applicable Encina.Security file: Anonymous, User, Service and null-Identity denial); `Security/ABAC/ABACPipelineBehaviorContractTests` (updated). `Encina.ContractTests` already reaches `Encina.Security` transitively through `Encina.Security.ABAC`, so no ProjectReference is added. Evaluator contract tests are not written: they count toward no obligation. |
| Property | `Encina.PropertyTests` | `Core/RequestIdentityProperties` (generated `ClaimsPrincipal` inputs: null, unauthenticated with `sub`, authenticated without subject, `service:`/`SERVICE:`/` service:` subjects, two authenticated identities, valid users, mapped through `ClaimsRequestIdentityFactory`: `UserId != null ⇔ IsAuthenticated`; generated nestings of `Run*Async` scopes with forked children never leave an ended identity readable, and a forked child never reads its parent's outer identity after the inner scope ends); validator properties (`RequestIdentityOptionsValidator`, `ServiceIdentityCatalogOptionsValidator`: these two count toward the `*Validator.cs` property flag); `Security/ABAC/ABACIdentityProperties` (crossed with Block/Warn modes: the PEP never calls the attribute provider or PDP for an unauthenticated identity; informational, since `ABACPipelineBehavior.cs` carries no property flag). The FsCheck `Arb` produces Anonymous and User only (documented). |
| Integration | `Encina.IntegrationTests` | **Three existing suites exercise the PAP actor** (found by searching `IPolicyAdministrationPoint`/`AddEncinaABAC`, not the class name): `Security/ABAC/PersistentPapScopeScenario.cs` and its ADO, Dapper and EF Core SQL Server users (real SQL Server, shared `[Collection("EFCore-SqlServer")]`-style fixtures, added by #1707/#1720). They are rewritten (Phase 4 task 9: accessor registered, no `ISecurityContextAccessor`, actor through the scope factory) and extended to run `ABACPolicySeedingHostedService` under the built-in identity and assert the persisted policy and the `service` audit actor; no parallel class is added. The feature has no SQL of its own, so the other providers add nothing; `Core/Identity/RequestIdentity.md` (AGENTS.md section 9 format) records that reason and why the TestServer end-to-end tests live in UnitTests (in-process hosting, no external service) |
| Load / Benchmark | `Encina.LoadTests`, `Encina.BenchmarkTests` | Load: `Encina.LoadTests/Core/Identity/RequestIdentity.md`: the accessor is a static `AsyncLocal` shared by all requests, so the justification cites the concurrency unit tests instead of claiming "no shared state": N parallel TestServer requests with distinct principals each asserting its own user id, and parallel `RunAsServiceAsync` flows (both in UnitTests, stated in the file). Benchmark (Q4, Phase 3 task 10): implemented, not justified: `EncinaContextMiddleware` per request and the per-activity circuit scope, next to `Encina.AspNetCore.Benchmarks/RequestContextAccessorBenchmarks.cs`; `TState` overloads only if it shows a need |

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
- New how-to: `docs/en/guides/SERVICE_IDENTITY.md` "Run background work with a service identity" (declare, scope, refusals, tenant rules, why a deferred message must not use it). It shows the delegate pattern with one scope per unit of work (per loop iteration, per message), consumes streams inside `work`, starts long-lived loops under `ExecutionContext.SuppressFlow()`, logs and persists the error code only (`error.GetCode().IfNone("unknown")`), and states that the scope API guards against accidental misuse, not hostile in-process code.
- `src/Encina.AspNetCore/README.md` and the how-to state the ordering rule "`UseEncinaContext` after `UseRouting`" (Critical 202 on the first detection, that is when a hub endpoint is reached through a misordered pipeline, then HTTP 500 for every later request, otherwise) and that connection endpoints (SignalR, Blazor's `/_blazor`, GraphQL over WebSocket, raw WebSockets, any request sending `Accept: text/event-stream`) carry no request identity until F2. They also show the **ordinary SSE opt-in** (D2): `RunInboundAsync(context.CreateInboundRequestInfo(), …)` around one event or one dispatch, never around the stream, and that the endpoint decides when the connection's principal is stale.
- New reference: "Request identity and claim mapping" (`docs/features/request-identity.md`), with the entry-point table, the multi-identity rules and the Tenancy claim-type note.
- `docs/plans/abac-decision-audit-implementation-plan-751.md` (see Phase 7); `docs/knowledge/issues/1676.md:18,65` and `docs/knowledge/issues/1677.md` (Phase 4).
- ADR-035 (`docs/architecture/adr/035-one-request-identity-model.md`); `docs/architecture/adr/index.md` (the reservation row is added by the plan PR; Phase 7 moves it).
- Changelog fragments (written by `mechanical-fixer`; `changelog-fragments.cs --check` exits 0): `changelog.d/1705-one-request-identity.security.md` (ABAC, Security and Authorization behaviors silently not registered whenever another behavior exists (#1635 subset, fail-open); anonymous substitution removed; external `service:` subjects rejected; service identities never turn compliance deny gates into allows; user id removed from Security tags and logs, from the authorization logs 200/201 and error details and from the EF Core interceptor logs 3000/3050; WebSocket upgrades detected before `UseWebSockets` runs; anonymous messages from external brokers fail closed for scope opening), `.fixed.md` (ABAC quick-start works; `AddEncinaAuthorization` registers its behavior), `.added.md` (`RequestIdentity`, `RequestIdentityOptions`, service identities, delegate-only scope API incl. `RunRestoredAsync`/`PersistedRequestIdentity`/`PersistedIdentitySource`, `InboundRequestInfo` and `HttpContext.CreateInboundRequestInfo()`, Blazor circuit handler, `TestIdentity`), `.changed.md` (claim precedence sub-first; authenticated-without-subject is anonymous; authenticated identities only; `ExcludeSystemAccess` semantics; `abac.missing_context` renamed `abac.unauthenticated_caller`; PAP audit `actor` is `user`/`service` and seeding records `service:encina.abac.policy-seeding` instead of `system`; `[Authorize]` evaluates the request identity; middleware timestamps from `TimeProvider`; `CreateForTest` is anonymous-only; the accessor setter preserves identity and origin and never clears; connection endpoints carry no request identity; `UseEncinaContext` must run after `UseRouting`), `.removed.md` (`ISecurityContext`, `ISecurityContextAccessor`, `SecurityContext`, `IPrincipalResolver`, claim options, `ThrowOnMissingSecurityContext`, `security.missing_context`, `WithUserId`, `IRequestContext.UserId` as an interface member, `RequestContext.Create(string)`, EF interceptor `IRequestContext` fallback, `EncinaContextMiddleware` as a public type).

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
| `AmbientRequestContext.Resolve/Enter` | `src/Encina/Core/AmbientRequestContext.cs:72` (`Resolve`), `:176` (`Enter`) | How a dispatch seeds its context and puts the previous holder back when it returns; identity scopes do **not** restore anything (they only invalidate, Design 3) |
| `RequestContextAccessor` (static AsyncLocal) | `src/Encina/Core/RequestContextAccessor.cs:47` | The single ambient channel |
| `EncinaContextMiddleware` | `src/Encina.AspNetCore/EncinaContextMiddleware.cs` | HTTP entry point |
| `SecurityContext` role/permission extraction | `src/Encina.Security/SecurityContext.cs:38-69` | Ported into `ClaimsRequestIdentityFactory` |
| `PolicyChangeActorScope` | `src/Encina.Security.ABAC/Administration/PolicyChangeActorScope.cs` | Replaced by the built-in service identity |
| `HttpContextAccessor` holder pattern | ASP.NET Core | Model for the holder-based `RequestContextAccessor` |
| SPEC-002 REQ-015 / DEC-011, P-50 (#1164) | `docs/specifications/SPEC-002-eu-regulatory-readiness.md:281,466,592,759` | Owner of deferred-dispatch identity; consumes `RunRestoredAsync` |
| `RequestContextPropagationTests` | `tests/Encina.UnitTests/AspNetCore/RequestContextPropagationTests.cs` | TestServer pattern |

### Event ID allocation

| Package | Range | Ids | Notes |
|---|---|---|---|
| Encina (core) | `Core` 100-199 | 162-174 (new, packed: 162-165 Phase 1, merged; 166-174 Phase 2, of which 172-174 were added by the PR #1862 review) | 100-165 used on `origin/main` ab4ec134 (verified: 100-161 in `Encina.Stream.cs`, `Encina.cs` and the Sharding logs, 162-165 in `RequestIdentityLog.cs:14-26`); 175-199 stay free; no new range, `AssemblyRanges` already maps `Encina` → `Core` |
| Encina.AspNetCore | `AspNetCore` 200-249 | 202 `EncinaContextBeforeRouting` (Critical, Phase 3, M6) | 200-201 used by `AuthorizationPipelineBehavior.cs:88,94` (verified; their templates lose `{UserId}` in Phase 3); identity logs live in the core factory |
| Encina.EntityFrameworkCore | `EntityFrameworkCore` 3000-3099, `EntityFrameworkCoreSoftDelete` 1500-1599 | none new | 3000 (`AuditInterceptor.cs:499`) and 3050 (`SoftDeleteInterceptor.cs:183`) keep their ids and lose `{UserId}` in Phase 3; 3050 sits in the `EntityFrameworkCore` range today, which the allocation test already accepts, so nothing moves |
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
2. Declared service identities + delegate-only IRequestContextScopeFactory (RunAsServiceAsync, RunAsPrincipalAsync, RunInboundAsync,
   RunRestoredAsync for SPEC-002/P-50; Either; refuses over a User and over an inbound chain, ended holders included; tenant rules;
   invalidates when the work completes; issuer-stamped identities checked at Resolve and at read time; identity- and
   origin-preserving setter; accessor-type validator) - amendment M6.
3. EncinaContextMiddleware (internal) runs next inside RunInboundAsync (TimeProvider timestamps, origin inbound), skips connection
   requests (WebSocket upgrade detected through IHttpUpgradeFeature/IHttpExtendedConnectFeature, Accept: text/event-stream,
   HubMetadata) under an anonymous connection marker, answers 500 on a non-cancellation Left, logs misordered routing (Critical
   202, only when the endpoint after next carries HubMetadata) and answers 500 to every later request (instance latch);
   public CreateInboundRequestInfo() for SSE opt-in per event; user id out of authorization and EF interceptor logs;
   AuthorizationPipelineBehavior on Identity.Principal,
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
- EventIds: core 162-174 packed; AspNetCore 202 (EncinaContextBeforeRouting, Critical); ABAC 9096 repurposed and 9085 (Disabled warning) allocated.
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
| 3 | Structured Logging | ✅ | Core 162-174 (`[LoggerMessage]`), AspNetCore 200/201 and EF Core 3000/3050 without the user id, AspNetCore 202 `EncinaContextBeforeRouting` (Critical, Phase 3), Security 8004 removed and 8001/8002 without user id, ABAC 9091 renamed, 9096 repurposed, 9085 allocated (Disabled warning). |
| 4 | Health Checks | ✅ | `SecurityHealthCheck` drops the accessor check (no external dependency added). |
| 5 | Validation | ✅ | `RequestIdentityOptionsValidator` and `ServiceIdentityCatalogOptionsValidator` with `ValidateOnStart`. |
| 6 | Resilience | ❌ | No external calls. |
| 7 | Distributed Locks | ❌ | No shared state; AsyncLocal per flow. |
| 8 | Transactions | ❌ | No multi-operation writes. |
| 9 | Idempotency | ❌ | Identity does not change idempotency keys; nested dispatch already clears the key. |
| 10 | Multi-Tenancy | ✅ | Tenant stays on `IRequestContext`; one tenant claim map for the middleware and `RunAsPrincipalAsync`; `tenant_conflict` for explicit dispatch under a User; tenant changes through the setter only outside a dispatch (until F5); connection requests get no tenant; scopes never inherit the ambient tenant (Design 3, tests per rule). `Encina.Tenancy.AspNetCore`'s `ClaimTenantResolver` and the serverless options keep their own claim type until F5/F3 (documented). Header fallback surface → F5. |
| 11 | Module Isolation | ❌ | `ModuleId` (metadata) untouched; scopes keep ambient metadata only via correlation. |
| 12 | Audit Trail | ✅ | PAP, Security.Audit, repositories, EF interceptors and #751 read the same identity; service actors recorded as `service:<name>`; PAP audit `actor` = `user`/`service`; `ExcludeSystemAccess` semantics updated. |

---

## Dependencies on open issues

| Issue | Relation |
|---|---|
| #1635 (open-generic `TryAddTransient`) | ABAC, Security and AspNetCore authorization subset delivered here; orchestrator updates #1635 to say so. Remaining ~19 packages stay there. |
| SPEC-002 REQ-015 / DEC-011, P-50 (#1164) | Owns deferred dispatch (outbox, inbox, scheduler); #1705 delivers `PersistedRequestIdentity`, `PersistedIdentitySource` and `RunRestoredAsync`; P-50 wires which dispatcher passes `External` (D3: an inbox fed by an external broker); the orchestrator comments the contract on #1164. |
| #1855 (dead-flow scope refusal, Q1) | Closed as absorbed: the holder-origin refusal is Phase 2 (M6). |
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

1. Deferred dispatch follows SPEC-002 REQ-015 / DEC-011 and P-50 (#1164): the originating actor is persisted and rebuilt, never replaced by a service identity; F1 covers only jobs with no originating request; F4 is dropped; the persisted form and the validated, logged `RunRestoredAsync` are defined in Design 1.
2. Fail closed everywhere: a service identity never turns a deny gate (DSR restriction gate included) into an allow; anonymous and unattributable identities deny at security and compliance gates.
3. `UserId` is a projection of `Identity` by construction (extension property).
4. No claim value or user id reaches logs, activity tags or `ToString`; tests prove it.
5. `IPrincipalResolver` and the EF Core interceptor identity channel are removed or folded into the single model in this plan.
6. Scopes: AsyncLocal holder with explicit invalidation and tenant binding, each with tests; since the scope-shape decision (item 10) scopes are delegate-shaped, so out-of-order disposal no longer exists and a forked scope that outlives its parent logs 170.
7. Per-flag targets with justification for every new or touched file, CRAP <= 10 with the local command, shared collection fixtures, no step relies on a suite that does not exist.
8. Documentation covers every page and sample that documents removed API and follows the visual rule (#1759).
9. ADR-035 "One request identity model" is reserved.
10. Phase 2 scope shape (2026-10-05, PR #1849; the later "Decision update" on #1705 wins): **C1** (delegate-only public scope API, ending only invalidates, `RequestContextScope` removed, a misuse guard and not a security boundary); **Q1** in Phase 2 (immutable holder origin and kind, chain walk including ended holders, identity- and origin-preserving setter with no clear; #1855 absorbed); **Q2** per-token claim exclusion configurable, `auth_time` counts, `nonce`/`at_hash`/`c_hash` excluded, validator rejects excluding subject, role, permission, `amr`, `acr`; **Q3** issuer-less explicit identities refused outside an active scope of the same identity, issuer liveness at read time, rule order stated; **Q4** no `TState` overloads, Phase 3 benchmarks the middleware and the circuit scope.

11. Phase 2 plan update, maintainer decisions on #1705 (2026-10-05): **MQ-1** options (b) and (c) together: `EncinaContextMiddleware` and `TenantResolutionMiddleware` also skip requests that send `Accept: text/event-stream` under the connection-origin marker, and after the first detection of `UseEncinaContext` before `UseRouting` (Critical 202) the middleware answers 500 to every later request without calling `next`; **MQ-2** confirmed: the anonymous connection-origin marker (adversarial minor 6, included in Phase 3 with the endpoint skip) refuses `RunAsServiceAsync`/`RunAsPrincipalAsync` without `AllowOverInbound` (logged) and permits per-invocation and per-activity `RunInboundAsync`.

12. Adversarial review of PR #1862, maintainer decisions on #1705 (2026-10-05):
    - **D1 (MQ-1 (b) latch).** The 500 latch trips only when the endpoint resolved after `next` carries `HubMetadata` (and the endpoint was null before `next`), the only case where the connection skip was actually missed; the Critical 202 log stays; the latch is a field of the middleware instance, never static. `UseRewriter`, `UseStatusCodePagesWithReExecute` re-routes and 404s no longer trip it (Design 2 step 5, Phase 3 tasks 1 and 9).
    - **D2 (ordinary SSE endpoints).** Phase 3 defines the opt-in: the public `HttpContext.CreateInboundRequestInfo()`, used with `RunInboundAsync` per event or per dispatch, never for the whole stream (Design 2, Phase 3 task 11).
    - **D3 (anonymous messages restored from an external broker).** They count as `Inbound` origin for the scope refusals, so they fail closed; the rule is built in Phase 2 (`PersistedIdentitySource.External`), the wiring belongs to P-50 (#1164) (Design 1 "Persisted form", Design 3 rule 4, Phase 2 task 5).
    - **D4 (principal cloning).** The stored principal keeps only the authenticated identities, as plain `ClaimsIdentity` copies, consistent with Design 4; the per-read clone is taken from it, so no `WindowsIdentity` token handle is duplicated (Design 1, Phase 2 task 4).

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

---

## Review log (Phase 2 scope shape)

Sources, all from PR [#1849](https://github.com/dlrivada/Encina/pull/1849) (the design record, not edited): section 7 of `docs/plans/request-identity-phase2-scope-shape-1705.md` (25 plan edits, "S7-n"), the pr-reviewer review `artifacts/pr-review/1849.md` (F1-F11) and the adversarial review `artifacts/pr-review/1849-adversarial.md` (majors A-M1 to A-M4, minors A-m1 to A-m12), plus the maintainer decisions on #1705 of 2026-10-05. Every `src/` citation was re-checked on `origin/main` 61d5dc3d; the plan and the Phase 1 files are unchanged since `c3626ed`, so the section 7 line numbers held. The historical review tables above keep the `Begin*` names they were written with; M6 supersedes them.

### Section 7 edits

| Item | Edit | Where |
|---|---|---|
| S7-1 | M6 (scope shape) amendment row | Summary amendments |
| S7-2 | M3: `RunInboundAsync`, issuer mechanism, no-issuer refusal, seam builds only | Summary amendments (M3) |
| S7-3 | m5: connection flows start with no context (superseded in part by the decision on A-m6, confirmed by the maintainer as MQ-2 on 2026-10-05: the marker requires `AllowOverInbound`) | Summary amendments (m5) |
| S7-4 | #1705 item (3) names the delegate API, "invalidate when the work completes" | Summary |
| S7-5 | Entry-point rows: connection flows, `RunRestoredAsync` | Entry-point table |
| S7-6 | Design 1: `RunRestoredAsync` signature, issuer, per-token claims, `tenant_conflict`, setter, builders | Design 1 |
| S7-7 | Middleware flow: skip, `RunInboundAsync`, no `finally`, `Left` → 500, ordering | Design 2 |
| S7-8 | Accessor lifetime: frame restore, LIFO by construction, 170, connection flows | Design 2 |
| S7-9 | Blazor: per-activity `RunInboundAsync`, `Left`, refresh, outside-activity reads | Design 2 |
| S7-10 | SignalR: mechanism cited, streaming hub methods in F2 | Design 2, Design 8 (F2) |
| S7-11 | Option B row reworded | Design 3 |
| S7-12 | API block replaced; `RequestContextScope` deleted; `IdentityScopeOptions` | Design 3 |
| S7-13 | Rules renamed; ending = invalidate + 168; implementation constraint; `unsupported_accessor` | Design 3 |
| S7-14 | Opt-outs name `RunAsServiceAsync` | Design 6 |
| S7-15 | Task 5 files | Phase 2 task 5 |
| S7-16 | Task 6 error codes | Phase 2 task 6 |
| S7-17 | Task 8: 170 renamed, 168 from `finally` | Phase 2 task 8 |
| S7-18 | Task 9: `Pop` → `End`, issuer, comparison, setter, validator, second architecture test | Phase 2 tasks 4, 9, 10 |
| S7-19 | Task 10 tests | Phase 2 task 13, Testing (Unit row) |
| S7-20 | Task 11 resolved; `TestIdentity.Service`/`Principal` builders | Phase 2 task 11 and closing note |
| S7-21 | Phase 2 prompt rewritten | Phase 2 prompt |
| S7-22 | Phase 3 tasks 1 and 4, prompt | Phase 3 tasks 0, 1, 4, prompt |
| S7-23 | WebSocket TestServer tests | Phase 3 task 9 |
| S7-24 | Unit and contract rows | Testing |
| S7-25 | ADR-035 content; combined prompt | Phase 7 task 2, Combined prompt |

### Review findings

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| A-M1 | major | One-line downgrade (`RequestContext = null`, or an anonymous explicit context) bypasses the "over a User / over inbound" refusals | Applied (Q1 in Phase 2): immutable holder origin and kind surviving `Invalidate`; refusals walk the chain including ended holders; `Push`/`SetUnchecked` record the walked facts before dropping an ended parent; setter identity- and origin-preserving, no clear; #1855 absorbed | Design 1 (setter), Design 3 rule 4, Phase 2 task 9, M6 |
| A-M2 | major | Blazor `Left` branch runs under the connection identity; misordered routing silently fail-open | Applied: `next(activity)` inside the internal anonymous masking scope; endpoint null before `next` and non-null after → Critical 202, once | Design 2, Phase 3 tasks 0, 1, 4, 9 |
| A-M3 | major | `HubMetadata` skip misses GraphQL-over-WebSocket and raw WebSocket endpoints | Applied: every WebSocket upgrade request is skipped (`context.WebSockets.IsWebSocketRequest`, works before routing) | Entry-point table, Design 2, Phase 3 task 1 |
| A-M4 | major | Issuer liveness only checked in `Resolve`; an accepted explicit context outlives its issuer | Applied: `ContextHolder.ReadContext()` returns null once the identity's issuer is not live | Design 1 (Issuer), Phase 2 task 9 |
| A-m1 | minor | Event 170 must test `!IsValid()`, not `IsDisposed` | Applied | Design 3 (Ending), Phase 2 task 8 |
| A-m2 | minor | Issuer under-specified; one identity per scope; rule order | Applied (rule order 1-7) | Design 1, Design 3 |
| A-m3 | minor | Per-token list: keep `auth_time`, add `nonce`, `at_hash`, `c_hash`; options passed in; `HasClaim` comparison; validator | Applied (Q2) | Design 1, Design 4, Phase 2 task 4 |
| A-m4 | minor | Tenant setter bypass until F5 | Applied: tenant change only while `!IsDispatchInFlight`; F5 acceptance criterion | Design 1 (setter), Design 8 (F5) |
| A-m5 | minor | `TenantResolutionMiddleware` installs a connection-lifetime tenant | Applied: same skip | Design 2, Phase 3 task 5, Consumer changes |
| A-m6 | minor | The skip removes the inbound guard from hub methods | Applied in Phase 3 (maintainer-confirmed, MQ-2, 2026-10-05): anonymous connection-origin marker; service/principal scopes need `AllowOverInbound`; `RunInboundAsync` permitted | M6, m5, Design 2, Design 3 rule 4, Phase 3 task 0 |
| A-m7 | minor | Stream claim wrong: `Resolve` runs at the first `MoveNextAsync` | Applied (verified `Encina.Stream.cs:37-39`) | Design 3 (Streams), Testing (Unit row) |
| A-m8 | minor | Section 7 misses `IEncina` XML docs, remarks, plan lines, extra test migrations | Applied for `IEncina.cs:53-64,86-90,137-141` (the remark is at `:61-64` on main), `TestIdentity`/`TestRequestContext` remarks, plan lines 145, 348, 505-508, 518, 552, 802 and every `JobContext()` use in `EncinaExplicitContextContractTests.cs` (the review listed `:27,35,57,196`; `:79,103,106,109` were added). **Not applied** for `EncryptionPipelineIntegrationTests.cs:72,373`: verified that they pass the context to `IEncryptionOrchestrator.EncryptAsync`, not to a dispatch, so they are builder use and stay | Phase 2 tasks 11, 12 |
| A-m9 | minor | The seam-drop reason also fits public `RunAsPrincipalAsync` | Applied: ADR-035 statement (public, logged, refused over User/inbound chains, not a security boundary) | Phase 7 task 2 |
| A-m10 | minor | `IsSameAs` compares frozen claims but gates evaluate the live mutable principal | Applied: clone at construction and per read | Design 1, Phase 2 task 4 |
| A-m11 | minor | Encina-owned long-lived loops started inside a scope capture it | Applied: `SuppressFlow` sweep in core; how-to | Design 3, Phase 2 task 9, Documentation |
| A-m12 | nit | Deferred-dispatch sample persists more than the code | Applied: error code only | Design 1 (Persisted form), Design 8, Documentation |
| F1 | major | Identity-preserving setter breaks far more tests than budgeted | Applied: 86 setter lines in 11 files plus issuer-less explicit dispatches listed and budgeted in Phase 2, with what the rewritten setter tests assert | Phase 2 task 12 |
| F2 | major | Routing-order fallback still fails open | Applied, as A-M2 and A-M3 plus MQ-1 (maintainer decision 2026-10-05): WebSockets are skipped before routing, circuits are masked, misordering logs Critical 202; the SSE hole is closed by skipping every `Accept: text/event-stream` request (MQ-1 (c)) and a detected misordering fails closed with 500 on every later request (MQ-1 (b)) | Design 2 steps 0, 1, 5 |
| F3 | major | Blazor sample runs the activity under the connection identity | Applied as A-M2 | Design 2 |
| F4 | minor | SSE mis-described: SSE keeps its GET open like WebSockets | Applied (`HttpConnectionDispatcher.cs:156-158` is the SSE branch; only long polling ends at the first poll) | Design 2 step 1 |
| F5 | minor | Section 7 misses plan lines 76, 167, 192, 265-266, 382, 389, 399, 508, 765, 786, 809, 865, 890, 971, 982, 1000 | Applied: every `Begin*` name outside the historical review tables replaced; 192, 508, 765, 786 and 809 rewritten for meaning | M4, Design 1-3, Design 8, Phase 2 prompt, Consumer changes, Testing, Documentation, Research, Matrix, Dependencies, Decisions |
| F6 | minor | `RequestIdentity.Issuer` construction cycle; identity per open | Applied: internal `IdentityIssuer` created first, passed to the identity, bound once to the holder | Design 1, Phase 2 task 4 |
| F7 | minor | `RequestIdentity.ForService` presented as existing | Applied: "Phase 2 internal" (only `ForUser` exists, `RequestIdentity.cs:166`) | Design 1 model |
| F8 | minor | Samples log the whole outcome; `GetCode()` is `Option<string>` | Applied: `error.GetCode().IfNone("unknown")` (`EncinaErrors.cs:100`); the factory logs 167 itself, so the Blazor handler logs nothing extra | Design 1, Design 2, Design 3 |
| F9 | minor | Cancellation and exception semantics unspecified | Applied: a cancelled token returns `Left(EncinaErrorCodes.RequestCancelled)` before `Push`; exceptions from `work`, `OperationCanceledException` included, propagate after invalidation | Design 3 (rules 2, Results), Testing |
| F10 | minor | Tenant interplay on hub endpoints; no opt-out for cross-tenant operator flows | Applied: tenant middleware skip (A-m5); `tenant_conflict` has no opt-out on purpose (rule 5) | Design 1, Design 2 |
| F11 | minor | PR #1849 body stale | Not applied here: PR #1849 is the design record and is not edited (maintainer process decision); orchestrator item | — |
| N1-N3 | nit | Source path prefixes, Mermaid diagram, second review line | N1 not applicable (the plan cites `aspnetcore` paths by file name); N2 and N3 left to the design record | — |
| Plan | correction | `ExplicitContextConflictTests` row said "logs 169"; the explicit-context conflict is 165 | Applied | Testing (Unit row) |

### Maintainer decisions on MQ-1 and MQ-2 (2026-10-05)

| Id | Question | Decision | Where applied |
|---|---|---|---|
| MQ-1 | A pipeline with `UseEncinaContext` before `UseRouting` cannot see `HubMetadata`, so a SignalR connection over SSE (or the first long poll) keeps the connect-time identity. Log only, block after detection, or skip SSE requests? | **Decided: (b) and (c) together.** (c) `EncinaContextMiddleware` and `TenantResolutionMiddleware` also skip requests that send `Accept: text/event-stream`, under the connection-origin marker, so the SSE connect-time identity hole is closed whatever the pipeline order (an ordinary SSE endpoint also reads Anonymous until it opts in per invocation); (b) after the first detection (Critical 202) the middleware answers 500 to every later request without calling `next` | Entry-point table, M6 connection-flows row, Design 2 steps 0, 1, 5, Phase 3 tasks 1, 5, 9 and prompt, ADR-035 items, Combined prompt, Maintainer decisions item 11 |
| MQ-2 | Does the connection-origin marker make `RunAsServiceAsync` inside a hub method need `AllowOverInbound`? | **Decided: confirmed.** Connection endpoints run under the connection-origin anonymous marker; `RunAsServiceAsync` and `RunAsPrincipalAsync` inside a hub method require `AllowOverInbound` (logged), `RunInboundAsync` per invocation and per activity is permitted (m5 consequence kept) | m5, Design 2, Design 3 rule 4, Phase 3 task 0, Maintainer decisions item 11 |

---

## Review log (PR #1862 adversarial review)

Source: `artifacts/pr-review/1862-adversarial.md` (verdict merge after fixes; 5 major, 10 minor, 4 maintainer decisions), reviewed at plan commit 54a89b73 against `origin/main` ab4ec134, and the maintainer decisions D1-D4 in the last #1705 comment of 2026-10-05. Every `src/` and `tests/` citation added or changed in this round was re-checked with `git show origin/main:<path>` on ab4ec134. The `dotnet/aspnetcore` `release/10.0` citations (`DefaultWebSocketManager.cs`, `WebSocketMiddleware.cs`, `ConnectionEndpointRouteBuilderExtensions.cs`, `WebSocketClient.cs`, `RewriteMiddleware.cs`, `StatusCodePagesExtensions.cs`) are taken from the review and were not re-read here.

| Id | Sev | Finding | Result | Where |
|---|---|---|---|---|
| 1 | major | `context.WebSockets.IsWebSocketRequest` is false until `WebSocketMiddleware` runs; SignalR adds `UseWebSockets` inside its endpoint, so raw and GraphQL WebSockets after `UseEncinaContext` keep the connect-time user; TestServer hides it | Applied: the predicate also reads `IHttpUpgradeFeature.IsUpgradableRequest` + `Upgrade: websocket` and `IHttpExtendedConnectFeature.IsExtendedConnect` + `Protocol` `websocket`; `ConnectionRequestPredicateTests` without `IHttpWebSocketFeature` plus a Kestrel loopback test; the same predicate (private copy, same test table) in `TenantResolutionMiddleware` | Entry-point table, M6 connection-flows row, Design 2 step 1 and tenant bullet, Phase 3 tasks 1, 5, 9, prompt, Testing |
| 2 | major | A benign re-route (`UseRewriter`, `UseStatusCodePagesWithReExecute`) or a 404 trips the 500 latch; a static latch breaks other hosts in the process | Applied as D1: trip only when the endpoint after `next` carries `HubMetadata`; instance field with `Interlocked.Exchange`; test that re-routing and 404 set no latch and that a second host is unaffected | Design 2 step 5, M6 connection-flows row, Phase 3 tasks 1, 9, prompt, ADR-035 item |
| 3 | major | Nested dispatch from a dead flow loses the chain facts because `SetUnchecked` drops non-scope holders through `NearestScope` (`RequestContextAccessor.cs:94,128-129`) | Applied: every new holder inherits the facts of the holder current when it is created, before `LiveOrNull` or `NearestScope` drop anything; tests for nested dispatch and two successive sets | M6 holder-origin row, Design 3 rule 4, Phase 2 tasks 9, 13, prompt, Testing (`HolderFactInheritanceTests`) |
| 4 | major | User ids still logged by `AuthorizationPipelineBehavior` 200/201 and the EF interceptors 3050/3000 | Applied: kind instead of user id (EventIds kept), `["userId"]` error details of the behavior become `["identityKind"]`; added to Phase 3 tasks 3 and 6 and the sentinel list | Design 2 (Authorization bullet), Design 4, Phase 3 tasks 3, 6, 9, prompt, Testing rules, Consumer changes, EventId table, changelog `.security` |
| 5 | major | Ordinary SSE endpoints have no opt-in | Applied as D2: public `HttpContext.CreateInboundRequestInfo()`, `RunInboundAsync` per event or per dispatch, never the whole stream; `InboundRequestInfo` shape and visibility defined (public record, Phase 2) | Entry-point table, Design 2, Design 3 API block, Phase 2 task 5, Phase 3 tasks 9, 11, Documentation |
| 6 | minor | CS0051: public `InvokeAsync`/constructor with an internal parameter type | Applied: `EncinaContextMiddleware` and `RequestIdentityCircuitHandler` become `internal sealed` (`UseMiddleware` activates internal types; resolving from `RequestServices` rejected: hidden from `ValidateOnBuild`, per-request service locator); PublicAPI lines `:24-26` removed; `InternalsVisibleTo` for `Encina.GuardTests` | Design 2, Phase 3 tasks 1, 4, 7, prompt, Consumer changes, changelog `.removed` |
| 7 | minor | Test budget misses 38 + 2 `Push`/`Pop` calls and the issuer-less `WithIdentity` dispatch of `RequestContextPropagationTests.cs:232-242` | Applied: 40 calls budgeted (`AccessorLifetimeTests` 38, `RequestIdentityGuardTests.cs:65-66` 2); the re-list pattern covers `TestRequestContext.WithIdentity(` and names the propagation test | Phase 2 task 12, prompt |
| 8 | minor | Two Phase 3 tests contradict the latch (negotiate trips it first) | Applied: fresh host per misordered case with the connection as first request, or `SkipNegotiation`; the WebSocket case asserts the latch on close (D1) | Phase 3 task 9 |
| 9 | minor | Setter "same origin" undefined with no readable context; setter stores a foreign context without a snapshot | Applied: no readable context → reference Anonymous/`Unspecified`; the setter checks and stores `CopyOf(value)` | Design 1 (setter), Phase 2 tasks 9, 13, prompt |
| 10 | minor | Principal cloning keeps unauthenticated identities; `WindowsIdentity` clone duplicates the handle | Applied as D4 | Design 1 (cloning), Phase 2 tasks 4, 13, prompt, ADR-035 item |
| 11 | minor | Rule 5 skipped on rule 4's accept path; rule 3 reads the ambient, not the chain | Applied: the tenant rule runs on every accepted context; rule 3 reads the holder facts ("chain has a User"); identity rules renumbered 1-6 with a note for older references | Design 1 (rule order), Phase 2 tasks 9, 13, prompt |
| 12 | minor | Missing EventIds; `RequestCancelled` in the middleware; `RunInboundAsync` must not return `Left` for client input | Applied: 172 `InboundScopeOpened`, 173 `ScopeTenantChanged`, 174 `ScopeOpenedOverInbound` (free in `EventIdRanges.Core` on ab4ec134); `RequestCancelled` → no response, no log; client-controlled `InboundRequestInfo` members normalized, never refused | m6 row, Design 2 steps 2 and 4, Design 3 (opening logs, inbound input), Phase 2 tasks 5, 8, 13, Phase 3 tasks 1, 9, EventId table |
| 13 | minor | Per-token exclusion set has no path into identities built outside the factory | Applied: `perTokenClaimTypes` parameter on `ForUser`/`ForService` (null = `DefaultPerTokenClaimTypes`), `IsSameAs` removes the union of both sets | Design 1 model and comparison, Phase 2 tasks 4, 13 |
| 14 | minor | `TenantResolutionMiddleware` skip location ambiguous; a skip at the start bypasses `RequireTenant` and validation | Applied: only the context write (`:117-121`) is skipped; test that the 400 still happens on a connection request | Design 2 (tenant bullet), Phase 3 tasks 5, 9, Consumer changes |
| 15 | nit | Research rows; `:117-121` writes only when a tenant resolves; PR body lists MQ-1/MQ-2 open; `Accept` matching unspecified | Applied: Research rows rewritten (`Resolve :72`, `Enter :176`, `AsyncLocal :47`, no "restore pattern for scopes"); "whenever a tenant resolves"; `Accept` matching specified (parsed media types, wildcards excluded, raw fallback). **Not applied here:** the PR #1862 body is the orchestrator's (this worker does not edit PRs) | Research, Design 2 step 1 and tenant bullet, Phase 3 task 5 |
| Plan | correction | Design 4 said `ConflictingAuthenticatedIdentities` is EventId 168; it is 164 (m6, `RequestIdentityLog.cs`) | Applied | Design 4 |
| DR-1 | minor | docs-reviewer: the `IsSameAs` bullet still pointed at "rule 5" for the tenant | Applied ("the tenant rule") | Design 1 |
| DR-2 | minor | docs-reviewer: wrong `WithIdentity` file list | Applied (verified with `git grep -l`: the five files and which ones dispatch) | Phase 2 task 12 |
| DR-3 | minor | docs-reviewer: the benchmark cannot reach the now-internal middleware | Applied: `InternalsVisibleTo` for `Encina.AspNetCore.Benchmarks` | Design 2 (Visibility), Phase 3 task 4 |
| DR-4 | nit | docs-reviewer: bound constants missing from the `InboundRequestInfo` sketch; `:164` passes `null`, not `userId` | Applied (`MaxIdLength`, `MaxUserAgentLength`, `MaxDataRegionLength`; call-site list corrected) | Design 3, Phase 3 task 3 |

Maintainer decisions D1-D4 are recorded in "Maintainer decisions", item 12.
