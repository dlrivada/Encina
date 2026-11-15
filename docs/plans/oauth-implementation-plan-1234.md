# Implementation Plan: Outbound OAuth Grants — `Encina.Security.OAuth`

> **Issue**: [#1234](https://github.com/dlrivada/Encina/issues/1234) (SPEC-002 P-32, REQ-040, AC-039, AC-043, scenarios S10 and S20; parent [#1186](https://github.com/dlrivada/Encina/issues/1186))
> **Type**: Feature
> **Complexity**: Very High (new package, a new store on the 10 database providers, a lock-protected refresh, an outbox notification, an ASP.NET Core helper)
> **Estimated Scope**: ~2,400-3,000 lines of production code + ~3,500-4,500 lines of tests; ~45 new and ~25 modified production files (split into 8 pull requests, see Phase 0 of Next Steps)

---

## Summary

A practice reaches calendar and mail APIs over OAuth on behalf of each tenant (SPEC-002 note H, H-R36). The refresh token is a long-lived secret and must be encrypted at rest (S20). Two hosts that refresh the same token at the same time invalidate each other, and an `invalid_grant` answer must pause the dependent work instead of being retried forever (S10). This plan adds `Encina.Security.OAuth`: an encrypted, tenant-scoped token store on the 10 database providers, a single-flight refresh guarded by `IDistributedLockProvider`, a `GrantRevoked` notification written to the outbox in the same transaction as the revocation, and a PKCE authorisation-code callback helper for ASP.NET Core minimal APIs.

### What exists today (verified on `main` at `7a0049c0`)

| Area | Current state | Evidence |
|------|---------------|----------|
| Package | `src/Encina.Security.OAuth` does not exist; no OAuth client code in `src/` | directory listing of `src/` |
| Field encryption | `IFieldEncryptor` encrypts a string with an `EncryptionContext` and returns `EncryptedValue` | `src/Encina.Security.Encryption/Abstractions/IFieldEncryptor.cs:36`, `:48`, `:63` |
| Column-level encryption (P-46) | Planned only (SPEC-002 REQ-055, #1250); no `EncryptedField` type in `Encina.Security.Encryption` | `docs/specifications/SPEC-002-eu-regulatory-readiness.md:377` |
| Distributed lock | `TryAcquireAsync(resource, expiry, wait, retry, ct)` returns a disposable handle or `null` (`null` conflates contention and fault; ADR-052 moves it to `Either<EncinaError, Option<ILockHandle>>`, and until that migration lands `oauth.lock_unavailable` maps both, while cancellation propagates) | `src/Encina.DistributedLock/IDistributedLockProvider.cs:53`, `:77` |
| Lock providers | `InMemory`, `Redis`, `SqlServer` exist; PostgreSQL, MySQL, Azure Blob and DynamoDB are tracked elsewhere (AGENTS.md section 5) | `src/Encina.DistributedLock.*` |
| Outbox | `IOutboxStore.AddAsync(IOutboxMessage)`, public `OutboxOrchestrator.AddAsync<T>`, `TransactionalOutboxPipelineBehavior`; `OutboxWriter` is `internal sealed` and not callable from another assembly (Design Choice 7) | `src/Encina.Messaging/Outbox/IOutboxStore.cs:31`, `OutboxOrchestrator.cs:87`, `OutboxWriter.cs:15`, `TransactionalOutboxPipelineBehavior.cs:42` |
| Transaction lease | `ITransactionLeaseSource.BeginOrJoinAsync`; `IDbTransactionAccessor` (ADO, Dapper, EF Core), `IMongoSessionAccessor` | `src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs:22`, `IDbTransactionAccessor.cs:38`, `src/Encina.MongoDB/Transactions/IMongoSessionAccessor.cs:21` |
| Partition pause (#469) | Does not exist; `Partition` appears only in sharding code | SPEC-002 gap table (REQ-042 "Missing") |
| `Encina.Http` (P-31) | Does not exist (only `Encina.Refit`) | SPEC-002 REQ-039 "Missing" |
| Endpoint helper pattern | `MapDPIAEndpoints(this IEndpointRouteBuilder, ...)` returns a `RouteGroupBuilder` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs:69` |
| Tenant | `IRequestContext.TenantId` is `string?`; Consent resolves it fail-closed with `RequireTenantContext` | `src/Encina/Abstractions/IRequestContext.cs:125`; `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:121`, `:128`, `:382` |
| Most recent multi-provider store with tenant | `BreakTheGlass` (SqlServer and PostgreSQL only for ADO/Dapper; EF Core; MongoDB option) | `src/Encina.EntityFrameworkCore/BreakTheGlass/BreakTheGlassElevationEntity.cs:18`; `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:113-118` |
| SQL scripts | Highest ADO script is `030_CreateBreakTheGlassElevationsTable.sql` (SqlServer, PostgreSQL); MySQL stops at `029` | `src/Encina.ADO.*/Scripts/` |

### Standards covered

OAuth 2.0 (RFC 6749, authorisation-code and refresh-token grants, `invalid_grant` in section 5.2), PKCE (RFC 7636, `S256`), OAuth 2.0 Security Best Current Practice (RFC 9700), GDPR Art. 32 (encryption of credentials at rest), SPEC-002 REQ-040, REQ-061, REQ-062, AC-039, AC-043, AC-044.

### Affected packages

- New: `Encina.Security.OAuth` (abstractions, options, in-memory store, refresh service, revocation service, health check, diagnostics).
- Modified: `Encina.AspNetCore` (PKCE callback helper), `Encina.Messaging` (`MessagingConfiguration` flag `UseOAuthTokenStore`), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `tests/Encina.TestInfrastructure` (schemas), `src/Encina/Diagnostics/EventIdRanges.cs`.
- Used, not modified: `Encina.Security.Encryption`, `Encina.DistributedLock`, `Encina.Security.Audit`.

### Provider category

**Database (10)** for the token store: ADO.NET x3, Dapper x3, EF Core (one project, run against SqlServer, PostgreSQL and MySQL) and MongoDB. Secondary category: **Distributed locks** is consumed through the abstraction only; the 1.0 lock set (AGENTS.md section 5) is covered by whichever provider the application registers.

---

## Design Choices

<details>
<summary><strong>1. Package placement — a new <code>Encina.Security.OAuth</code> package with the PKCE helper in <code>Encina.AspNetCore</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New core package `Encina.Security.OAuth`; the minimal-API PKCE helper lives in `Encina.AspNetCore`** | Follows the existing endpoint-helper pattern (`DPIAEndpointExtensions`); the OAuth package keeps no ASP.NET Core dependency | The helper is far from the types it uses |
| **B) New package plus a satellite `Encina.Security.OAuth.AspNetCore`** | Everything OAuth in two packages; smaller `Encina.AspNetCore` | One more package to version, document and cover for 120 lines of code |
| **C) Put the grants in `Encina.Security.Secrets`** | No new package | Secret managers are not built for frequent per-tenant writes; the issue rejects it (Alternatives Considered 1) |

### Chosen Option: **A — new package, helper in `Encina.AspNetCore`**

**Class:** Trivial
**Settled by:** `src/Encina.AspNetCore/DPIAEndpointExtensions.cs:69` (the existing endpoint-helper convention) and the rejection of C in issue #1234 (Alternatives Considered 1)

### Rationale

- The endpoint helper is a thin mapping over `IOAuthAuthorizationFlow`; the same shape already exists for DPIA.
- Naming follows the `Encina.Security.*` family (`Encina.Security.Secrets`, `Encina.Security.Encryption`).

</details>

<details>
<summary><strong>2. Encrypting the refresh token — use <code>IFieldEncryptor</code> in the store layer now</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Encrypt with `IFieldEncryptor` before the store writes, persisting the serialised `EncryptedValue`; the encryption context carries the tenant and the connection name** | Works today on all 10 providers with one code path; the store never sees plaintext; key rotation follows `IKeyProvider` | When P-46 lands, the columns could be encrypted twice unless the store opts out |
| **B) Wait for P-46 (ORM value converters, #1250) and store plaintext columns until then** | No duplicated mechanism | Blocks the feature on an unplanned issue; plaintext tokens are not acceptable even temporarily (S20) |
| **C) Encrypt in the database (TDE / MongoDB CSFLE)** | No application code | Provider-specific, not available on every provider; the token is plaintext in the application |

### Chosen Option: **A — `IFieldEncryptor` in the store layer** (recommended, pending the maintainer)

### Rationale

- We recommend A because `IFieldEncryptor` exists today (`IFieldEncryptor.cs:48`) and S20 needs encrypted refresh tokens before P-46 exists. The store contract takes and returns a plaintext `OAuthTokenSet`; encryption stays in one `OAuthTokenProtector` class, so P-46 can later replace it by changing that class only.
- B wins if the maintainer prefers a single encryption mechanism and accepts that this feature ships after #1250.
- The access token is short-lived but is stored encrypted too; the expiry timestamp stays in clear to allow queries.

</details>

<details>
<summary><strong>3. Single-flight refresh — a lock per (tenant, connection) with a re-read after acquiring</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDistributedLockProvider.TryAcquireAsync` on `oauth:refresh:{tenant}:{connection}`, re-read the token after acquiring, refresh only if still due** | Works across hosts; waiting callers reuse the token the winner stored; one refresh call per expiry | Needs a lock provider registered; a lost lock during a slow refresh is possible |
| **B) Row-level optimistic version on the grant row** | No lock provider needed | Both hosts still call the token endpoint; the loser's refresh token may already be invalidated (rotation) |
| **C) In-process `SemaphoreSlim` per key** | Simple | Does not cover several hosts, which is the stated problem (S10) |

### Chosen Option: **A — distributed lock with re-read**

**Class:** Settled
**Settled by:** issue #1234 (Proposed Solution: "a distributed lock per (tenant, connection) around refresh"), AGENTS.md section 6 function 7

### Rationale

- The issue fixes the mechanism; the plan adds the re-read after acquisition, which is what turns N waiting callers into one refresh.
- When no `IDistributedLockProvider` is registered the options validator fails at start-up; there is no silent in-process fallback (fail closed). The InMemory provider serves single-host development and tests.
- Lock expiry is the refresh timeout plus a margin, and the handle is released in `await using`.

</details>

<details>
<summary><strong>4. Revocation — a grant status, a gate and a <code>GrantRevoked</code> outbox notification in one transaction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) On `invalid_grant`, in one transaction: set the grant to `Revoked` and write `OAuthGrantRevoked` (an `INotification`) to the outbox; consumers ask `IOAuthGrantGate` before working | Works now without #469; the notification survives a crash; the gate is explicit | The partition pause of #469 is not wired until #469 exists |
| **B) Wait for #469 and pause the partition from the revocation** | Matches S10 wording exactly | Blocks on a missing feature; no effect until #469 ships |
| **C) Publish the notification in process** | Simplest | At-most-once; lost on a crash (the reason P-34 exists) |

### Chosen Option: **A — status, gate and outbox notification in one transaction** (recommended, pending the maintainer)

### Rationale

- We recommend A: the revocation (state change) and the notification (outbox row) share the lease from `ITransactionLeaseSource.BeginOrJoinAsync`, so a rolled-back revocation leaves no notification (AGENTS.md section 3, errors are never swallowed: a `Left` from the store fails the operation).
- The gate is the behaviour S10 needs today: `GetStatusAsync` returns `Revoked`, and callers stop. When #469 lands, a handler of `OAuthGrantRevoked` can pause the partition without changing this package; that wiring is deferred to an issue file.
- B wins only if the maintainer wants no revocation behaviour until partitioned messaging exists.

</details>

<details>
<summary><strong>5. Token endpoint client — <code>IHttpClientFactory</code> behind a small interface until <code>Encina.Http</code> exists</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOAuthTokenEndpointClient` implemented with `IHttpClientFactory` and a named client; replaced by `Encina.Http` later** | Testable with WireMock; no dependency on a missing package | A small HTTP layer in this package |
| **B) Depend on P-31 `Encina.Http`** | Shared resilience and telemetry | The package does not exist; blocks this feature |
| **C) Use a third-party OAuth library** | Less code | New dependency, its own token handling; secret exposure risk |

### Chosen Option: **A — `IHttpClientFactory` behind `IOAuthTokenEndpointClient`**

**Class:** Trivial
**Settled by:** AGENTS.md section 6 function 6 (resilience for external calls) and SPEC-002 REQ-039 being "Missing" (no `Encina.Http` to depend on)

### Rationale

- The interface is the seam for P-31: the implementation moves to `Encina.Http` when it exists.
- Transient failures (network, 5xx, 429 with `Retry-After`) are retried by a Polly pipeline on the named client; `invalid_grant` and other 4xx answers are never retried.

</details>

<details>
<summary><strong>6. Tenant scoping — the key is (TenantId, ConnectionName) and a missing tenant fails closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every row carries `TenantId`; the store reads the ambient tenant and fails closed when tenancy is required and the tenant is missing; with tenancy off a fixed default tenant applies** | Meets AC-043; same decision table as Consent (#1315) | One more option (`RequireTenantContext`) |
| **B) Caller passes the tenant on every call** | Explicit | Every call site can forget it; no fail-closed default |

### Chosen Option: **A — ambient tenant, fail closed**

**Class:** Settled
**Settled by:** SPEC-002 REQ-061 and AC-043, AGENTS.md section 3 (security gates fail closed), `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:128`

### Rationale

- The unique key is `(TenantId, ConnectionName)`.
- Trivial defaults, each an internal constant or an option that one line changes and that no other package reads: the tenant used when tenancy is off is the sentinel `"-"` (a non-null, non-empty value so the unique key never holds NULL; no other store in `src/` defines it, so it is this package's own choice); `RefreshSkew` is 2 minutes (refresh before the token expires; an option); the access token is stored encrypted and its expiry in clear so the expiry check needs no decryption.
- The store contract also accepts an explicit tenant for background work, validated against the ambient value when both exist.


</details>

<details>
<summary><strong>7. Writing the revocation notification to the outbox — which public seam <code>Encina.Security.OAuth</code> calls</strong></summary>

### Options Considered

`OutboxWriter` is `internal sealed` (`src/Encina.Messaging/Outbox/OutboxWriter.cs:15`) and `Encina.Security.OAuth` is another assembly, so it cannot be called.

| Option | Pros | Cons |
|--------|------|------|
| **A) Call the public `OutboxOrchestrator.AddAsync<TNotification>` (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:87`), which returns `Either<EncinaError, Unit>`** | No change to `Encina.Messaging`; the existing public seam | Couples this package to the orchestrator type |
| **B) Call the public `IOutboxStore.AddAsync` with `IOutboxMessageFactory` and `IMessageSerializer`** | Lowest level, fully public | This package rebuilds what the writer does |
| **C) Make a writer public in `Encina.Messaging` (plus PublicAPI lines)** | One reusable seam for #1234 and #1235 | Public API growth; touches another package |

### Chosen Option: **A — `OutboxOrchestrator.AddAsync<TNotification>`** (recommended, pending the maintainer)

### Rationale

- We recommend A: it is public today and already returns an `Either`, so a `Left` rolls the revocation back (AGENTS.md section 3).
- C wins if the maintainer wants one shared writer for this plan and the #1235 plan, which has the same defect.

</details>

<details>
<summary><strong>8. OAuth <code>state</code> storage when several hosts run the application</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory `IOAuthStateStore` by default; start-up validation fails when the lock provider is not distributed (more than one host) and no durable state store is registered** | No new table; fails loudly | A single host works with no setup, several hosts need an explicit store |
| **B) Persist `state` through the token store of the 10 providers** | Works across hosts without setup | A second table or row kind in 10 providers |
| **C) In-memory only** | Simplest | A callback landing on another host fails with `oauth.state_invalid` |

### Chosen Option: **A — in-memory default, start-up validation** (recommended, pending the maintainer)

### Rationale

- We recommend A: the issue premise is several hosts (S10), so a silent in-memory default would lose callbacks; the validator makes the gap visible.
- B wins if the maintainer wants multi-host to work with no extra registration.

</details>

---

## Implementation Phases

### Phase 1: Core Model, Abstractions and Errors (`Encina.Security.OAuth`)

> **Goal**: The vocabulary and contracts, with no persistence.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Security.OAuth/` (namespace `Encina.Security.OAuth`)

1. `Encina.Security.OAuth.csproj`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `README.md` — copy the layout of `src/Encina.Security.BreakTheGlass/` (references `Encina`, `Encina.Security.Encryption`, `Encina.DistributedLock`, `Encina.Messaging`, `Encina.Tenancy`; `InternalsVisibleTo` for the five test projects).
2. `Model/OAuthTokenSet.cs` — `sealed record OAuthTokenSet(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAtUtc, string? Scope)`; `ToString()` returns a fixed redacted text.
3. `Model/OAuthGrant.cs` — `sealed record OAuthGrant(string TenantId, string ConnectionName, OAuthGrantStatus Status, OAuthTokenSet Tokens, DateTimeOffset CreatedAtUtc, DateTimeOffset LastUpdatedAtUtc, DateTimeOffset? RevokedAtUtc)`.
4. `Model/OAuthGrantStatus.cs` — enum `Active = 1`, `Revoked = 2`.
5. `Model/OAuthConnectionOptions.cs` — per-connection settings: `TokenEndpoint`, `AuthorizationEndpoint`, `ClientId`, `ClientSecret` (`[JsonIgnore]`), `Scopes`, `RedirectUri`; `ToString()` override that omits the secret (AGENTS.md section 3).
6. `Abstractions/IOAuthTokenStore.cs` — `GetAsync`, `UpsertAsync`, `MarkRevokedAsync` (all `ValueTask<Either<EncinaError, ...>>`, with `CancellationToken`; `GetAsync` returns `Option<OAuthGrant>`).
7. `Abstractions/IOAuthTokenService.cs` — `GetAccessTokenAsync(string connectionName, CancellationToken)` returning the valid access token, refreshing under the lock when due.
8. `Abstractions/IOAuthTokenEndpointClient.cs` — `ExchangeCodeAsync`, `RefreshAsync`; returns `Either<EncinaError, OAuthTokenSet>`.
9. `Abstractions/IOAuthGrantGate.cs` — `GetStatusAsync(connectionName, ct)` returning `OAuthGrantStatus`.
10. `Abstractions/IOAuthAuthorizationFlow.cs` — `BeginAsync` (returns authorisation URL, state and stores the PKCE verifier) and `CompleteAsync(state, code)`.
11. `OAuthErrors.cs` — codes `oauth.grant_not_found`, `oauth.grant_revoked`, `oauth.refresh_failed`, `oauth.tenant_required`, `oauth.lock_unavailable`, `oauth.state_invalid`; metadata carries the connection name, never a token.
12. `Notifications/OAuthGrantRevoked.cs` — `sealed record OAuthGrantRevoked(string TenantId, string ConnectionName, DateTimeOffset RevokedAtUtc) : INotification`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1234 (Encina.Security.OAuth).

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>).
- New package src/Encina.Security.OAuth; copy the layout of src/Encina.Security.BreakTheGlass.
- Plan: docs/plans/oauth-implementation-plan-1234.md, Design Choices 1, 2, 4 and 6.

TASK:
Create the csproj, README, PublicAPI files, the model records, the abstractions, OAuthErrors and the
OAuthGrantRevoked notification exactly as listed in the Phase 1 Tasks. Add every public symbol to PublicAPI.Unshipped.txt.

KEY RULES:
- Options with a secret mark it [JsonIgnore] and override ToString() (AGENTS.md section 3).
- Token types override ToString() with a redacted text; no token reaches logs, tags or error metadata.
- Every method returns Either; no exceptions for business outcomes; CancellationToken on every async method.
- XML docs on all public members; nullable enabled; no [Obsolete].

REFERENCE FILES:
- src/Encina.Security.BreakTheGlass/ (layout, errors, options)
- src/Encina.Security.Encryption/Abstractions/IFieldEncryptor.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.Messaging/Outbox/IHasNotifications.cs
```

</details>

---

### Phase 2: Services, In-Memory Store and Configuration

> **Goal**: Refresh, revocation and the default in-memory store, wired through DI.

<details>
<summary><strong>Tasks</strong></summary>

#### New files

1. `Services/OAuthTokenProtector.cs` — wraps `IFieldEncryptor`; `ProtectAsync(OAuthTokenSet, tenantId, connection)` returns two serialised `EncryptedValue` strings; `UnprotectAsync` reverses it (Design Choice 2).
2. `Services/DefaultOAuthTokenService.cs` — implements `IOAuthTokenService`: read the grant; if `Revoked` return `oauth.grant_revoked`; if the access token is valid for more than `RefreshSkew` return it; otherwise `TryAcquireAsync("oauth:refresh:{tenant}:{connection}")`, re-read, refresh through `IOAuthTokenEndpointClient`, `UpsertAsync`, release. Constructor: `IOAuthTokenStore`, `IOAuthTokenEndpointClient`, `IDistributedLockProvider`, `IOAuthRevocationService`, `IOptions<OAuthOptions>`, `TimeProvider`.
3. `Services/OAuthRevocationService.cs` — `RevokeAsync(tenant, connection)`: `ITransactionLeaseSource.BeginOrJoinAsync`, `MarkRevokedAsync`, `OutboxOrchestrator.AddAsync<OAuthGrantRevoked>` writes the notification (Design Choice 7), commit; a `Left` anywhere rolls back and returns the `Left`.
4. `Services/DefaultOAuthGrantGate.cs` — implements `IOAuthGrantGate` over the store.
5. `Services/DefaultOAuthAuthorizationFlow.cs` — PKCE `S256` verifier and challenge, `state` stored with a short expiry (`IOAuthStateStore`, in-memory default; start-up validation per Design Choice 8), code exchange, `UpsertAsync`.
6. `Services/HttpOAuthTokenEndpointClient.cs` — `IHttpClientFactory` named client `encina-oauth`; maps `invalid_grant` (RFC 6749 section 5.2) to `oauth.grant_revoked`, other 4xx to `oauth.refresh_failed` (not retried), 5xx and network faults to a retryable error.
7. `Stores/InMemoryOAuthTokenStore.cs` — thread-safe default; tenant-scoped key.
8. `Tenancy/OAuthTenantResolver.cs` — ambient tenant, `RequireTenantContext` decision table copied from `DefaultConsentService` (`:121-128`).
9. `OAuthOptions.cs` — `RefreshSkew` (default 2 minutes), `LockExpiry`, `LockWait`, `RequireTenantContext` (nullable), `Connections` dictionary; validator `OAuthOptionsValidator.cs` (fails start-up when a connection lacks an endpoint or when no lock provider is registered).
10. `ServiceCollectionExtensions.cs` — `AddEncinaSecurityOAuth(Action<OAuthOptions>)`: `TryAdd*` for every service and for `TimeProvider.System`, `IHttpClientFactory` client with a Polly retry pipeline, options validation `ValidateOnStart`, health check opt-in.
11. `src/Encina.Messaging/MessagingConfiguration.cs` — add `UseOAuthTokenStore` flag and the enum entry (pattern of `UseBreakTheGlassStore`, `MessagingConfiguration.cs:557`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1234.

CONTEXT:
- Phase 1 created the abstractions in src/Encina.Security.OAuth.
- Plan: docs/plans/oauth-implementation-plan-1234.md, Design Choices 2-6.

TASK:
Implement the services, the in-memory store, the options, the tenant resolver and AddEncinaSecurityOAuth as listed
in the Phase 2 Tasks, plus the UseOAuthTokenStore flag in MessagingConfiguration.

KEY RULES:
- Time comes from TimeProvider; never DateTime.UtcNow.
- The refresh path re-reads the grant after acquiring the lock and releases the handle with await using.
- A Left from a store, the outbox or the endpoint client fails the operation; never report success.
- invalid_grant revokes the grant; other 4xx answers are not retried; only transient errors are.
- Registration completeness: AddEncinaSecurityOAuth registers every option and dependency it resolves and is proven
  by a DI test with ValidateOnBuild and ValidateScopes (AGENTS.md section 3).
- Tokens never reach logs, activity tags, metric tags or error metadata; EncinaError.Message never reaches logs.
- Every changed method CRAP <= 10: one private method per step.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (tenant decision table)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs and TransactionalOutboxPipelineBehavior.cs
- src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
```

</details>

---

### Phase 3: Persistence — Entity, Mapper and Scripts

> **Goal**: The shared persistence shape of the grant.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Security.OAuth/Persistence/OAuthGrantEntity.cs` — `TenantId`, `ConnectionName`, `Status` (int), `AccessTokenProtected`, `RefreshTokenProtected` (nullable), `ExpiresAtUtc`, `Scope`, `CreatedAtUtc`, `LastUpdatedAtUtc`, `RevokedAtUtc`.
2. `Persistence/OAuthGrantMapper.cs` — entity to `OAuthGrant` through `OAuthTokenProtector`.
3. SQL scripts `031_CreateOAuthGrantsTable.sql` in `src/Encina.ADO.SqlServer/Scripts/`, `src/Encina.ADO.PostgreSQL/Scripts/` and `src/Encina.ADO.MySQL/Scripts/` (MySQL has no 030: the maintainer-visible gap is recorded in Prerequisites), the Dapper packages that carry scripts, and the `000_CreateAllTables.sql` of each; primary key `(TenantId, ConnectionName)`.
4. `tests/Encina.TestInfrastructure/Schemas/{SqlServerSchema,PostgreSqlSchema,MySqlSchema}.cs` — `CreateOAuthGrantSchemaAsync`; `Fixtures/EntityFrameworkCore/EFCoreSchema.cs` entry.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md. Phase 1-2 exist.

TASK:
Create OAuthGrantEntity, OAuthGrantMapper, the 031 scripts for the three ADO providers (and the Dapper script folders
that carry them), the aggregate 000 scripts and the test schemas.

KEY RULES:
- Primary key (TenantId, ConnectionName); text columns sized for an encrypted payload.
- SQL Server: nvarchar with binary collation on the key; PostgreSQL: case-sensitive identifiers; MySQL: backticks.
- Dates in UTC with the AtUtc suffix; no database default for time.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scripts/030_CreateBreakTheGlassElevationsTable.sql
- tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs
- src/Encina.EntityFrameworkCore/BreakTheGlass/BreakTheGlassElevationEntity.cs
```

</details>

---

### Phase 4: Multi-Provider Stores — All 10 Database Providers

> **Goal**: `IOAuthTokenStore` on every provider, enlisting in the ambient transaction.

<details>
<summary><strong>Tasks</strong></summary>

1. ADO: `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/OAuth/OAuthTokenStoreADO.cs` (3 files); upsert with `MERGE` (SqlServer), `ON CONFLICT` (PostgreSQL), `ON DUPLICATE KEY UPDATE` (MySQL); each takes `IDbTransactionAccessor?` and joins the lease.
2. Dapper: `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/OAuth/OAuthTokenStoreDapper.cs` (3 files), same SQL.
3. EF Core: `src/Encina.EntityFrameworkCore/OAuth/` — `OAuthGrantEntityConfiguration.cs`, `OAuthModelBuilderExtensions.cs` (`ApplyOAuthConfiguration`), `OAuthTokenStoreEF.cs`.
4. MongoDB: `src/Encina.MongoDB/OAuth/` — `OAuthGrantDocument.cs`, `OAuthTokenStoreMongoDB.cs` (writes through the session when present); index creation in `MongoDbIndexCreator.cs`; option `UseOAuthTokenStore` and collection name in `EncinaMongoDbOptions.cs`.
5. Registration: each provider's `ServiceCollectionExtensions.cs` — `RemoveInMemoryDefault<IOAuthTokenStore, InMemoryOAuthTokenStore>` then `TryAddScoped`, gated by `UseOAuthTokenStore` (pattern `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:113-118`); EF through `RegisterOAuthTokenStore<TDbContext>` (pattern `:488`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md, Design Choices 2 and 6. Phases 1-3 exist.
- The 10 database providers all implement IOAuthTokenStore (AGENTS.md section 5).

TASK:
Implement the store and its registration on ADO x3, Dapper x3, EF Core and MongoDB as listed in the Phase 4 Tasks.

KEY RULES:
- Every database call is async with a CancellationToken; no synchronous overloads.
- A database store must win over the in-memory default in any registration order without overriding the application's
  own registration.
- Every query filters by TenantId; the write path joins the ambient transaction (IDbTransactionAccessor,
  DbContext, IMongoSessionAccessor).
- Providers differ only in implementation; no provider code in the abstractions.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/ (OutboxStoreADO/Dapper/EF/MongoDB for transaction joining)
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:113-118
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 5: PKCE Callback Helper (`Encina.AspNetCore`)

> **Goal**: Two minimal-API endpoints for the authorisation-code flow.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.AspNetCore/OAuthEndpointExtensions.cs` — `MapEncinaOAuthEndpoints(this IEndpointRouteBuilder, string prefix = "/oauth")` returning a `RouteGroupBuilder` with `GET {prefix}/{connection}/authorize` (redirect to the authorisation URL) and `GET {prefix}/{connection}/callback` (validates `state`, completes the exchange, returns 204 or a problem details with the error code only).
2. `src/Encina.AspNetCore/Encina.AspNetCore.csproj` reference to `Encina.Security.OAuth`, and the PublicAPI lines.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md, Design Choice 1.

TASK:
Add MapEncinaOAuthEndpoints to Encina.AspNetCore over IOAuthAuthorizationFlow.

KEY RULES:
- The callback fails closed on a missing or unknown state, a missing tenant or a replayed state.
- Responses carry error codes only; authorisation codes and tokens are never echoed or logged.
- The endpoints require authorisation by default (the caller passes a policy name, as `MapDPIAEndpoints` does at `src/Encina.AspNetCore/DPIAEndpointExtensions.cs`; the method throws at start-up when the name is blank or the policy is not registered); there is no anonymous mode.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.AspNetCore/ProblemDetailsExtensions.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Health, validation, audit, resilience and tenancy.

<details>
<summary><strong>Tasks</strong></summary>

1. `Health/OAuthTokenStoreHealthCheck.cs` — `DefaultName = "encina-oauth"`, static `Tags`; reports store reachability and the count of revoked grants (count only).
2. Audit: `Services/OAuthAuditWriter.cs` emits grant created, refresh failed and revoked events through the Security.Audit store (identifiers: tenant and connection name only).
3. Polly pipeline for the named client in `ServiceCollectionExtensions.cs` (retry on transient, honouring `Retry-After` through `src/Encina/Recoverability/RetryAfterParser.cs`).
4. `OAuthOptionsValidator` completed (Phase 2 item 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md, cross-cutting matrix.

TASK:
Add the health check, the audit writer and the resilience pipeline listed in the Phase 6 Tasks.

KEY RULES:
- Health result data carries counts and codes only; never a token, a tenant secret or EncinaError.Message.
- Audit events never carry token values.
- Health check: DefaultName const, Tags static array, scoped resolution through CreateScope().

REFERENCE FILES:
- src/Encina.Security.BreakTheGlass/Health/BreakTheGlassHealthCheck.cs
- src/Encina/Recoverability/RetryAfterParser.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Activity, metrics and logging with the tenant as an attribute (REQ-062).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs` — register `public static readonly (int Min, int Max) SecurityOAuth = (5500, 5549);` and add `["Encina.Security.OAuth"] = [nameof(EventIdRanges.SecurityOAuth)],` to `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `Diagnostics/OAuthDiagnostics.cs` — `ActivitySource` and `Meter` named `Encina.Security.OAuth`; counters `encina.oauth.refreshes` (tag `outcome`), `encina.oauth.revoked`; span `encina.oauth.refresh` with `encina.tenant_id` and `encina.oauth.connection`.
3. `Diagnostics/OAuthLogMessages.cs` — `[LoggerMessage]` EventIds 5500-5507, packed (grant created, refresh started, refresh succeeded, refresh failed, grant revoked, lock unavailable, state invalid, tenant required), logging the error code or `ForLogging()` only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md; EventIds 5500-5549 (SecurityOAuth).

TASK:
Register the range, map the assembly in EncinaEventIdAllocationTests, and add OAuthDiagnostics and OAuthLogMessages,
then call them from the services.

KEY RULES:
- Register the range in EventIdRanges.cs before any EventId; pack IDs sequentially; XML doc "Event IDs: 5500-5507".
- No token, authorisation code, client secret or subject identifier in tags, metrics or messages.
- The tenant id is the attribute encina.tenant_id.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Security.BreakTheGlass/Diagnostics/
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Testing

> **Goal**: Every flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: refresh flow (valid, due, revoked, lock unavailable, endpoint failure), revocation atomicity with a fake lease, PKCE challenge, tenant resolver table, options validator, DI test with `ValidateOnBuild` and `ValidateScopes`.
2. Guard: every public constructor and method of the new package and the endpoint extension.
3. Contract: one `OAuthTokenStoreContract` run against the in-memory store and the 10 providers.
4. Property (FsCheck): N concurrent callers with a due token produce exactly one endpoint call (in-memory lock provider and a fake endpoint).
5. Integration: `[Collection("<Family>-<Database>")]` on the 10 providers; a WireMock OAuth server for the end-to-end refresh and `invalid_grant`; a two-tenant test (AC-043); an in-memory exporter test (REQ-062); a test that no tag, metric or log carries a token.
6. Load and benchmark: `.md` justifications (not a concurrent hot path), as the issue states.
7. `.github/coverage-manifest/Encina.Security.OAuth.json` and per-file entries in the provider manifests, with one-sentence justifications.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md; manifest targets in .github/coverage-manifest/.

TASK:
Write the tests listed in the Phase 8 Tasks and the manifest entries.

KEY RULES:
- Tests execute real package code; no reflection-only tests. Assertions use Shouldly through Encina.Testing.Shouldly.
- Integration tests use shared [Collection] fixtures; ClearAllDataAsync in InitializeAsync; never IClassFixture for databases.
- No Thread.Sleep; time through FakeTimeProvider.
- A test never swallows an exception thrown by the driven code.
- Each flag reaches its own target; measure per flag with coverage-report.cs.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Security/BreakTheGlass/
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- .github/coverage-manifest/Encina.Security.BreakTheGlass.json
```

</details>

---

### Phase 9: Documentation and Finalization

> **Goal**: Documentation, changelog, public API and build verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every public member (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/1234-security-oauth.added.md`.
3. `src/Encina.Security.OAuth/README.md`; `docs/features/security-oauth.md` (usage, configuration, PKCE flow diagram); `docs/INVENTORY.md` entry; ROADMAP entry if the milestone changes.
4. `docs/architecture/adr/` — an ADR numbered at implementation time recording Design Choices 2 and 4.
5. `PublicAPI.Unshipped.txt` for `Encina.Security.OAuth` and `Encina.AspNetCore`.
6. `dotnet build --configuration Release` with zero warnings; per-flag coverage report; CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1234.

CONTEXT:
- Plan: docs/plans/oauth-implementation-plan-1234.md.

TASK:
Write the documentation, the changelog fragment, the ADR and the PublicAPI lines listed in the Phase 9 Tasks.

KEY RULES:
- Documentation never types coverage figures by hand; cite them with covref markers.
- The PR records the ADR-018 evaluation of the 12 functions.
- English only; no private client application is named (write "the reference application").

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance to this plan |
|--------|-----------|------------------------|
| RFC 6749 | Sections 4.1, 5.2, 6 | Authorisation-code grant, `invalid_grant`, refresh-token grant |
| RFC 7636 | PKCE `S256` | The callback helper and `DefaultOAuthAuthorizationFlow` |
| RFC 9700 | OAuth 2.0 Security BCP | Refresh-token rotation, state validation, no tokens in URLs or logs |
| GDPR | Art. 32 | Encryption of stored credentials |
| SPEC-002 | REQ-040, REQ-055, REQ-061, REQ-062, AC-039, AC-043, AC-044, S10, S20 | Requirement, encryption, tenancy, telemetry, scenarios |
| ADR-018 | Cross-cutting integration | The 12-function matrix |
| ADR-021 | EventId registry | Range 5500-5549 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Use in this feature |
|-----------|----------|---------------------|
| `IFieldEncryptor` | `src/Encina.Security.Encryption/Abstractions/IFieldEncryptor.cs:36` | Encrypts the access and refresh token |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs:53` | Single-flight refresh |
| `ITransactionLeaseSource`, `IDbTransactionAccessor`, `IMongoSessionAccessor` | `src/Encina.Messaging/Transactions/`, `src/Encina.MongoDB/Transactions/` | Revocation and outbox row in one transaction |
| `OutboxOrchestrator`, `IOutboxStore` | `src/Encina.Messaging/Outbox/` | `OAuthGrantRevoked` notification |
| `RetryAfterParser`, `OutboundErrorCodes` | `src/Encina/Recoverability/` | Retry classification of the token endpoint |
| `DefaultConsentService` tenant decision table | `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:121-128` | `OAuthTenantResolver` |
| `DPIAEndpointExtensions` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs:69` | Shape of the endpoint helper |
| BreakTheGlass store family | `src/Encina.EntityFrameworkCore/BreakTheGlass/`, `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:113` | File layout and registration pattern |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/` | OAuth server stub |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.OAuth` | 5500-5549 (new `SecurityOAuth`) | 5500-5507 used by this plan; first free block after `MessagingRecoverability` 5450-5499 |
| `Encina.Notifications*` (plan #1235) | 5550-5699 | Reserved by plan #1235 in this family |
| Domain-event outbox (plan #1236) | 5700-5749 | Reserved by plan #1236 |
| Sequences (plan #1238) | 5750-5799 | Reserved by plan #1238 |

### Estimated File Count

| Category | New | Modified | Notes |
|----------|----:|---------:|-------|
| Package scaffold, model, abstractions | 16 | 0 | Phase 1 |
| Services, in-memory store, options, DI | 11 | 1 | Phase 2 |
| Entity, mapper, scripts, schemas | 12 | 4 | Phase 3 |
| Provider stores and registrations | 17 | 9 | Phase 4 (8 stores and 2 EF/Mongo support files, 9 registrations) |
| PKCE helper | 1 | 2 | Phase 5 |
| Health, audit | 2 | 0 | Phase 6 |
| Diagnostics | 2 | 2 | Phase 7 |
| Tests | ~45 | ~3 | Phase 8, including two `.md` justifications |
| Documentation | 5 | 4 | Phase 9 |

### Findings in the Current Code

- P-46 column encryption does not exist, so the store encrypts with `IFieldEncryptor` (Design Choice 2).
- #469 does not exist: "pause the partition" is reduced to a grant status plus a gate and a notification (Design Choice 4).
- ADO MySQL has no script `030`; the next free number differs from SqlServer and PostgreSQL by one gap, to be aligned when scripts are written.
- `Encina.Http` (P-31) does not exist, so the token endpoint client is local (Design Choice 5).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, Railway Oriented Programming with Either<EncinaError, T>).
Issue #1234 (SPEC-002 P-32, REQ-040, AC-039, AC-043, S10, S20) adds Encina.Security.OAuth: an encrypted tenant-scoped
OAuth token store on the 10 database providers, single-flight refresh through IDistributedLockProvider, an outbox
GrantRevoked notification written with the revocation, and a PKCE callback helper in Encina.AspNetCore.
Plan: docs/plans/oauth-implementation-plan-1234.md. Rules: AGENTS.md.

IMPLEMENTATION OVERVIEW:
1. New package: model, abstractions, errors, OAuthGrantRevoked notification.
2. Services: protector (IFieldEncryptor), token service (lock, re-read, refresh), revocation service (one transaction),
   gate, PKCE flow, HTTP token endpoint client, in-memory store, tenant resolver, options and validator, DI.
3. Persistence: entity, mapper, 031 scripts, test schemas.
4. Stores on ADO x3, Dapper x3, EF Core, MongoDB; database store wins over the in-memory default.
5. PKCE endpoints in Encina.AspNetCore.
6. Health check, audit events, Polly pipeline.
7. EventIds 5500-5549, ActivitySource, Meter, [LoggerMessage].
8. Tests on every flag; two-tenant and no-secret-in-telemetry tests; load and benchmark .md justifications.
9. Docs, ADR, changelog, PublicAPI, build, coverage, CRAP.

KEY PATTERNS:
- Everything returns Either; a Left from a store, the outbox or the endpoint fails the operation.
- Tokens are never logged, tagged or placed in error metadata; options with secrets use [JsonIgnore] and ToString().
- TimeProvider for time; async database calls with CancellationToken; fail closed on a missing tenant.
- Registration completeness proven by a DI test with ValidateOnBuild and ValidateScopes.
- Integration tests on shared [Collection] fixtures; every flag reaches its manifest target; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Security.Encryption/Abstractions/IFieldEncryptor.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs
- src/Encina.Security.BreakTheGlass/ and its provider stores
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The token is read once per use and must never be served stale; no read path benefits from caching |
| 2 | OpenTelemetry | ✅ | `encina.oauth.refresh` span and `refreshes` / `revoked` counters with `encina.tenant_id` (Phase 7, REQ-062) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 5500-5507 in the new `SecurityOAuth` range (Phase 7) |
| 4 | Health Checks | ✅ | `OAuthTokenStoreHealthCheck`: store reachable, number of revoked grants (Phase 6) |
| 5 | Validation | ✅ | `OAuthOptionsValidator` at start-up; PKCE `state` validation in the flow (Phases 2 and 5) |
| 6 | Resilience | ✅ | Polly retry on the named HTTP client for transient errors only (Phase 6) |
| 7 | Distributed Locks | ✅ | One lock per (tenant, connection) around refresh (Phase 2, Design Choice 3) |
| 8 | Transactions | ✅ | Revocation and outbox row in one lease (Phase 2, Design Choice 4) |
| 9 | Idempotency | ❌ | Not a message or request entry point; a repeated revocation is a no-op on a revoked grant |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every row, ambient tenant, fail closed, default tenant when tenancy is off (Design Choice 6, AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Grant created, refresh failed and revoked audited through the Security.Audit store (Phase 6) |

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #1186 EU regulatory readiness (SPEC-002) | open (EPIC) | Parent |
| P-46 / #1250 Column-level encryption | open | Not blocking; the store uses `IFieldEncryptor` (Design Choice 2) |
| #469 Partitioned Sequential Messaging | open | Not blocking; the `OAuthGrantRevoked` handler that pauses a partition is deferred to an issue file |
| P-31 `Encina.Http` | planned | Not blocking; the endpoint client moves there later |
| A lock provider for production | InMemory, Redis, SqlServer exist | PostgreSQL, MySQL, Azure Blob and DynamoDB providers are tracked separately (AGENTS.md section 5) |
| Plans #1235, #1236, #1238 | this family | EventId ranges 5550-5799 reserved so the four plans do not collide; this plan owns 5500-5549 |
| Issue file `plan-1234-oauth-partition-pause.md` | to open | Wire `OAuthGrantRevoked` to the partition pause once #469 exists |

---

## Next Steps

1. The maintainer decides Design Choices 2, 4, 7 and 8 (the Dilemmas); the orchestrator records the answers in a `## Maintainer Decisions` section and updates each Chosen Option.
2. The feature exceeds one worker pull request (15 files, 1500 lines). Phases 1, 2 and 7 alone are about 29 new files plus the modified ones and their tests, so the split is eight pull requests, each at most 15 files and 1500 changed lines (sizes are estimates, re-measured when each sub-issue is written): (a1) Phase 1, the model, abstractions, errors and options, about 12 files with their unit and guard tests; (a2) Phase 2 services and the in-memory store, about 12 files; (a3) Phase 7 (EventId range registration, allocation test, diagnostics, log messages) and the `UseOAuthTokenStore` flag, about 8 files; (b) Phase 3 and the three ADO providers; (c) the three Dapper providers; (d) EF Core, MongoDB and the schemas; (e1) Phase 5 (PKCE callback helper) and Phase 6 cross-cutting, about 10 files; (e2) the Phase 8 integration and load tests and Phase 9 documentation, about 12 files. Each PR carries its own manifest entries and tests.
3. The orchestrator opens the issue file `artifacts/issues/plan-1234-oauth-partition-pause.md` and links it from #1234.
4. Link this plan from #1234 and start sub-issue (a) with an `issue-worker`.
