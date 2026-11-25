# Implementation Plan: Persistent `ITenantStore` for the 10 database providers

> **Issue**: [#596](https://github.com/dlrivada/Encina/issues/596)
> **Type**: Feature
> **Complexity**: High (7 phases, 10 providers, 11 pull requests)
> **Estimated Scope**: ~1,900 lines of production code + ~3,000 lines of tests, split over 11 pull requests of at most 15 files each
> **Milestone**: v0.18.0 — Integration & Platform Capabilities (SPEC-002 DEC-009 (b), REQ-061, AC-043)

---

## Summary

Implement a persistent tenant catalog: `TenantStoreADO`, `TenantStoreDapper`, `TenantStoreEF` and `TenantStoreMongoDB` on the 10 providers of the database matrix, so that `ITenantStore` stops having `InMemoryTenantStore` as its only implementation. SPEC-002 AC-043 lists #596 among the issues that must be closed before multi-tenancy is accepted. The issue is written against an interface that does not exist (`Task<Tenant?> GetByIdAsync`, `CreateAsync` returning `Tenant`, 13 providers including SQLite, a `tenant_features` table and a `metadata` column); this plan replaces it with what the code needs.

What the code looks like today (`origin/main` `4b0a600c7`):

- `ITenantStore` (`src/Encina.Tenancy/Abstractions/ITenantStore.cs`) is **read-only**: `ValueTask<TenantInfo?> GetTenantAsync`, `ValueTask<IReadOnlyList<TenantInfo>> GetAllTenantsAsync`, `ValueTask<bool> ExistsAsync` (default implementation over `GetTenantAsync`). It has no write method and no `Either`. `TenantInfo` is `(TenantId, Name, Strategy, ConnectionString, SchemaName)`; `ConnectionString` is `[JsonIgnore]` and masked by `ToString()` because it is a credential.
- The only implementation is `InMemoryTenantStore` (`src/Encina.Tenancy/Stores/InMemoryTenantStore.cs`), registered by `AddEncinaTenancy` with `TryAddSingleton` from `TenancyOptions.Tenants`; it compares ids with `StringComparer.OrdinalIgnoreCase` and `InMemoryTenantStoreTests.GetTenantAsync_IsCaseInsensitive` pins that. `AddTenantStore<TStore>()` does `RemoveAll<ITenantStore>()` and so overrides whatever the application registered before it.
- 11 production classes take the interface (counted by grep on `origin/main`: `TenantConnectionFactory` in ADO and Dapper x3 each, `TenantDbContextFactory`, `TenantAwareMongoCollectionFactory`, `TenantResolutionMiddleware`, `DefaultTenantProvider`, `TenantConnectionFactoryBase`) and none catches an exception of the store: a lookup that throws fails the request, which is the fail-closed behavior `AGENTS.md` section 3 asks for.
- The tenancy EventId range is `Tenancy = (1800, 1899)`; `TenancyLog` uses 1800-1804 and #711 owns 1805-1810.
- Siblings, cited by number and not duplicated: #346 (pipeline gate), #1257 (reserved `default` tenant), #752 (tenant lifecycle administration and its audit; it recommends a write port plus an administration service), #711 (tenant connection cache), #2676 (tenant telemetry tag), #4182 (retention enumerates tenants through `ITenantStore`), #597 (Redis store) and #598 (cloud stores), which reuse the contract this plan owns.

**Provider category**: database, all 10 providers of `AGENTS.md` section 5 (ADO.NET x3, Dapper x3, EF Core x3, MongoDB); SQLite is out of the matrix (ADR-024), so the issue's "13 providers" is 10. **Affected packages**: `Encina.Tenancy` (contract, default registration, diagnostics, health check), `Encina.ADO.SqlServer/PostgreSQL/MySQL`, `Encina.Dapper.SqlServer/PostgreSQL/MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, test projects. All provider packages already reference `Encina.Tenancy` (their `TenantConnectionFactory`), so no ProjectReference changes.

---

## Design Choices

<details>
<summary><strong>1. Write contract: a separate `ITenantStoreWriter` port, not new methods on `ITenantStore` (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new `ITenantStoreWriter` (Encina.Tenancy) with `CreateAsync`, `UpdateAsync`, `DeleteAsync` returning `ValueTask<Either<EncinaError, Unit>>`; `ITenantStore` stays read-only; persistent stores and `InMemoryTenantStore` implement both** | Read-only stores (options, claims, a custom adapter) stay valid; the 11 consumers do not change; matches the write port that the #752 plan recommends; ROP on every write (`AGENTS.md` section 3) | A second interface to register and resolve; a store class implements two interfaces |
| **B) Add the write methods to `ITenantStore`** (the issue's shape) | One abstraction | Every read-only implementer must implement or reject writes; the issue's `Task<Tenant>`/`Task` signatures do not match the existing `ValueTask<TenantInfo?>` style and have no `Either` |
| **C) Write through `ITenantAdministration` only (#752) and give the persistent stores no public write API** | One place that audits every lifecycle change | #596 cannot be closed or tested on its own; the administration service is a larger prerequisite than this issue |

### Chosen Option: **A — `ITenantStoreWriter`** (recommended, pending the maintainer)

### Rationale

- A wins when read-only stores exist, which is the case today (`InMemoryTenantStore` built from `TenancyOptions.Tenants`). B wins only if the maintainer decides that every tenant store must be writable. C wins if #752's administration service is built first; under C this plan still delivers the port that the service writes through, so A is compatible with both.
- #752 (plan, open) names this port "a small write port" under its own Dilemma; the maintainer should answer both together.

</details>

<details>
<summary><strong>2. Tenant id identity: ordinal, case-sensitive, `default` reserved (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ordinal and case-sensitive everywhere: `InMemoryTenantStore` switches to `StringComparer.Ordinal`; the id column uses a binary collation on SQL Server (`Latin1_General_100_BIN2`) and MySQL (`utf8mb4_bin`); PostgreSQL and MongoDB are already case-sensitive; the id is 1-128 characters, no control characters, no leading or trailing whitespace, never `default`** | Identity is injective: no two ids map to one catalog row (a binary collation fixes case, and the whitespace rule fixes trailing spaces, which SQL Server and MySQL `PAD SPACE` comparison ignores even under a binary collation), and the catalog agrees with the outbox, inbox and saga tenant columns, which compare ordinally; the same behavior on all 10 providers | `GetTenantAsync("ACME")` no longer finds `acme` (it fails closed); one existing test and the documented behavior of `InMemoryTenantStore` change |
| **B) Case-insensitive everywhere** (today's in-memory behavior): store a case-folded key next to the id, unique on the folded key | Header, claim and route spellings resolve to the same tenant | Two spellings resolve to one catalog row while the data stores treat them as two tenants (ordinal), so the catalog and the data disagree; a folded key per provider |
| **C) Whatever each database's default collation gives** | Least code | Different behavior per provider (SQL Server and MySQL case-insensitive, PostgreSQL and MongoDB case-sensitive); the same application behaves differently when the provider is switched, which `AGENTS.md` section 5 (provider coherence) forbids |

### Chosen Option: **A — ordinal identity with a binary collation** (recommended, pending the maintainer)

### Rationale

- A wins when the tenant id is also the key of tenant-stamped data (outbox, inbox, saga, audit, SPEC-002 stores), which it is: those stores compare the id as the application supplied it. B wins if tenant ids come from user-typed input with no canonical spelling; that case is better solved by the resolver normalizing the spelling before lookup, not by the catalog. C is rejected by provider coherence.
- The reserved id `default` (SPEC-002 REQ-061 "a fixed default tenant applies"; #1257 plan, which also makes the options validator reject it) is rejected on write with `tenancy.tenant_id_reserved` (`ErrorCategory.InvalidRequest`, ADR-062 rule R3); `TenantIdRules` in `Encina.Tenancy` holds the one rule set that every provider calls and **this plan (PR 1) owns it**: 1-128 characters, no control characters, leading or trailing whitespace rejected with `tenancy.tenant_id_invalid` (never trimmed; without it `acme` and `acme ` are two ids on PostgreSQL and MongoDB but one row on SQL Server and MySQL, whose `=` and unique comparisons ignore trailing spaces even under `Latin1_General_100_BIN2` and `utf8mb4_bin`), and not `default`. The #1257 plan decides the same padded-id rejection for the resolver and the options validator; its validator (PR 1257-A2) calls `TenantIdRules` instead of keeping a second copy, so there is one rule set.

</details>

<details>
<summary><strong>3. Where the tenants table lives and which connection it uses</strong></summary>

**Class:** Settled
**Settled by:** `TenantConnectionFactory` in the provider packages calls `ITenantStore` to find a tenant's connection string, so a catalog read through the tenant-aware connection would recurse; `AGENTS.md` section 5 (provider coherence: providers share the abstraction and differ only in implementation)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The catalog connection: the application's default (non-tenant) connection, injected like the other stores (`IDbConnection`, `DbContext`, `IMongoDatabase`)** | No recursion; the same pattern as `DeadLetterStoreADO` | The application must point the catalog at a database that exists before any tenant |
| **B) Resolve the connection through `ITenantConnectionFactory`** | Reuses the factory | The factory needs the store: infinite recursion for the first lookup |

### Chosen Option: **A — the default catalog connection**

### Rationale

- Only A can work: B needs the store to answer the question the store is asked. The table is named `Tenants` (ADO and Dapper take `tableName` like `DeadLetterStoreADO`), with a `SqlIdentifierValidator` check.

</details>

<details>
<summary><strong>4. Storing the connection string at rest (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A port `ITenantSecretProtector` (`Protect(string)`, `Unprotect(string)`) in `Encina.Tenancy`; the store protects the connection string before it writes and unprotects after it reads; with no protector registered, writing a non-null connection string is a `Left` (`tenancy.protector_required`, `ErrorCategory.FailedPrecondition`) and reading a protected value throws; the application registers an adapter (for example over `IFieldEncryptor`)** | No new package edge (`Encina.Security.Encryption` references only `Encina`); fails closed; no plaintext credential is ever written by Encina | The application supplies the adapter before it can use database-per-tenant |
| **B) Store a reference (a secret name) and resolve it through `ISecretReader` (`Encina.Security.Secrets`)** | The credential never enters the catalog | Changes `TenantInfo.ConnectionString` semantics (value versus reference) and adds a package edge to every provider |
| **C) Store plaintext and document database-level encryption** | Least code | A credential in a plaintext column; a backup or a SQL injection elsewhere exposes every tenant database; against the spirit of the `[JsonIgnore]` and `ToString()` masking of `TenantInfo` (#851) |

### Chosen Option: **A — `ITenantSecretProtector`** (recommended, pending the maintainer)

### Rationale

- A wins when the application already has an encryption or key-management story (it must, for SPEC-002 column encryption, #1250); B wins when the application keeps database credentials in a vault and never wants them in the catalog; C is rejected on security grounds and listed only to show it was weighed.
- A tenant with `SharedSchema` or `SchemaPerTenant` has a null connection string and needs no protector.
- Protected versus plaintext: the stored text carries a version marker prefix (`tp1:`); a read accepts only a marked value, and a non-null unmarked value is never returned as a connection string (the read throws, fail closed), so a plaintext value written by hand cannot pass as a protected one. `Protect` and `Unprotect` return `Either<EncinaError, string>`: a failing protector is a `Left` on write (`tenancy.protector_failed`, `ErrorCategory.Unavailable`, exception type only in the log) and a throw on read, never an exception from a business rule.
- `TenantRecord` (the EF entity) and every row type that holds the protected text mark the property `[JsonIgnore]` and override `ToString()` to mask it, like `TenantInfo` (`AGENTS.md` section 3, #851).

</details>

<details>
<summary><strong>5. Scope of the persisted record: the `TenantInfo` fields only (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Persist exactly `TenantInfo` (id, name, strategy, protected connection string, schema name) plus `CreatedAtUtc` and `UpdatedAtUtc` from `TimeProvider`; no `is_enabled`, no `tenant_features` table, no `metadata` column** | Every column has a reader today (`AGENTS.md` section 3: every line serves a current purpose); the later fields (`IsActive` from #752's lifecycle issue, `TenantCountry` and `TenantRegulations` from #686) arrive with the issue that reads them, each as a new script and column | The issue's feature-flag and metadata ideas wait for a consumer |
| **B) The issue's schema now: `is_enabled`, `metadata`, and a `tenant_features` table** | Matches the issue text | Nothing reads them: `TenantInfo` has no such members, so the columns would be written and never returned; `tenant_features` needs its own contract (a feature-flag store, #29 family) |

### Chosen Option: **A — `TenantInfo` fields plus timestamps** (recommended, pending the maintainer)

### Rationale

- A wins when no code reads the extra data; B wins if the maintainer wants the catalog schema stable before the lifecycle (#752) and applicability (#686) fields land. Adding a column later is one script per provider, which is cheap before 1.0.

</details>

<details>
<summary><strong>6. Registration: a persistent store replaces only the default, in any order</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3, registration completeness ("a database store MUST win over an in-memory default in any registration order without overriding the application's own registration"; #1269, #1295)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaTenancy` registers the default store through a marked descriptor (a static factory instance); each `AddEncinaTenantStore{Provider}` calls `TenantStoreRegistration.Replace<TStore>(services)` in `Encina.Tenancy`, which replaces the descriptor only when it is the marked default and otherwise leaves an application registration alone; the registration is also safe when called before `AddEncinaTenancy`, whose `TryAdd` then does nothing** | Wins in both orders; never overrides the application; one helper for #597 and #598 | A small piece of descriptor logic with its own tests |
| **B) Call `AddTenantStore<TStore>()`** (`RemoveAll`) | Exists today | Overrides the application's own registration when it ran earlier; wins over the default only if called after `AddEncinaTenancy` |
| **C) `TryAddSingleton<ITenantStore>` in each provider** | Simple | When `AddEncinaTenancy` ran first, the in-memory default stays: the persistent store loses |

### Chosen Option: **A — marked default descriptor and one replace helper**

### Rationale

- Only A satisfies both halves of the rule at once (B violates the second half, C the first). The helper registers the store as a singleton factory that builds the concrete store from a scoped catalog connection through `IServiceScopeFactory` per call, never injecting a scoped service into the singleton.

</details>

<details>
<summary><strong>7. Observability and health through one decorator, not ten implementations</strong></summary>

**Class:** Trivial
**Settled by:** `AGENTS.md` sections 6 and 7 (ActivitySource, Meter and `[LoggerMessage]` in the package, EventIds inside the registered range) and the existing `InstrumentedDeadLetterStore` (`src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs`), which wraps every dead letter store once

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InstrumentedTenantStore` in `Encina.Tenancy` wraps the store that `TenantStoreRegistration.Replace<TStore>(services, provider)` registers (only those stores, never the default `InMemoryTenantStore`); logging, spans, counters and the health check live there, using `TenancyLog` ids 1811-1816** | One implementation of telemetry for 10 providers and for #597 and #598; the stores stay free of logging and need no new EventId range; the `provider` tag is the `provider` argument of `Replace` (`"sqlserver-ado"`, `"redis"`, `"cosmosdb"`...), passed to the decorator's constructor, so #597 and #598 have a defined way to set it; `GetRequiredService<InMemoryTenantStore>()` and its tests (`ServiceCollectionExtensionsTests`, `TenancyServiceCollectionExtensionsGuardTests`) keep resolving the undecorated default | One more wrapper in the resolution chain of persistent stores |
| **B) Each store logs and traces itself** | No wrapper | Ten copies of the same telemetry; ten EventId ranges in the database zone |

### Chosen Option: **A — the decorator**

### Rationale

- Telemetry carries the operation, the outcome and the provider; it never carries the tenant id of the record (an id is not a bounded tag) nor `EncinaError.Message`. The ambient-tenant attribute of ADR-054 D4 comes from the helper that #2676 introduces and is added when that lands.

</details>

---

## Implementation Phases

### Phase 1: Contract, id rules and the in-memory implementation (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Tenancy/Abstractions/ITenantStoreWriter.cs` (new): `CreateAsync(TenantInfo, CancellationToken)`, `UpdateAsync(TenantInfo, CancellationToken)`, `DeleteAsync(string tenantId, CancellationToken)`, each `ValueTask<Either<EncinaError, Unit>>`.
2. `src/Encina.Tenancy/Abstractions/ITenantSecretProtector.cs` (new): `Either<EncinaError, string> Protect(string plaintext)`, `Either<EncinaError, string> Unprotect(string protectedValue)`; the stored text carries the `tp1:` marker of Design Choice 4.
3. `src/Encina.Tenancy/Core/TenantIdRules.cs` (new, public static, owned by this plan): `Validate(string? tenantId)` returning `Either<EncinaError, string>`; rules of Design Choice 2 (length, control characters, leading or trailing whitespace, reserved `default`).
4. `src/Encina.Tenancy/TenancyErrors.cs` (edit): `TenantExistsCode = "tenancy.tenant_exists"` (`AlreadyExists`), `TenantIdInvalidCode` and `TenantIdReservedCode` (`InvalidRequest`), `ProtectorRequiredCode` (`FailedPrecondition`), `ProtectorFailedCode = "tenancy.protector_failed"` (`Unavailable`); fixed messages with no tenant id. A missing update or delete target returns the existing `TenancyErrors.TenantNotFoundCode` (`tenancy.tenant_not_found`, `NotFound`, already on `origin/main`), not a new code.
5. `src/Encina.Tenancy/Stores/InMemoryTenantStore.cs` (edit): ordinal comparer; implements `ITenantStoreWriter`; `RegisterTenant`, `RemoveTenant` and `Clear` stay for tests.
6. `tests/Encina.TestInfrastructure/Tenancy/TenantStoreBehaviorSuite.cs` (new): the abstract behavior suite every store runs (create, duplicate, update, missing update, delete, missing delete, exists, get-all, case-sensitivity, padded id (`acme` and `acme ` are rejected, so two ids can never share a row on SQL Server and MySQL), reserved id, over-long id, protector round trip, tampered unmarked value, no protector with connection string), over a `CreateStore()` factory.
7. Tests: `InMemoryTenantStoreTests` (edit: the case test becomes the ordinal test), `TenantIdRulesTests` (new), `InMemoryTenantStoreBehaviorTests` (new, derives from the suite), `TenantStoreWriterGuardTests` (new, `Encina.GuardTests`), `.github/coverage-manifest/Encina.Tenancy.json` (edit), `src/Encina.Tenancy/PublicAPI.Unshipped.txt` (edit), changelog fragment `changelog.d/596-persistent-tenant-store.added.md`.

Files: 13. No call site of `new InMemoryTenantStore(` changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina.Tenancy has a read-only ITenantStore and InMemoryTenantStore (case-insensitive today).
- Design Choices 1, 2 and 4 fix the write port, the id rules and the secret protector.

TASK:
Add ITenantStoreWriter, ITenantSecretProtector, TenantIdRules and the four error codes; make InMemoryTenantStore ordinal and a writer; add the shared behavior suite in Encina.TestInfrastructure and run it on the in-memory store.

KEY RULES:
- Either<EncinaError, Unit> on every write; fixed error messages with no tenant id; ADR-062 categories as listed.
- TimeProvider for CreatedAtUtc/UpdatedAtUtc where the writer stamps them; never DateTime.UtcNow.
- Update InMemoryTenantStoreTests.GetTenantAsync_IsCaseInsensitive to the ordinal behavior.
- XML docs on every public member; PublicAPI.Unshipped.txt lines.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/ITenantStore.cs
- src/Encina.Tenancy/Stores/InMemoryTenantStore.cs
- src/Encina.Tenancy/TenancyErrors.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 2: Registration, decorator, telemetry and health check (pull request 2)

<details>
<summary>Tasks</summary>

1. `src/Encina.Tenancy/ServiceCollectionExtensions.cs` (edit): the default registration becomes a marked descriptor (the direct `InMemoryTenantStore` registration that resolves `ITenantStore as InMemoryTenantStore` stays as it is, because the default is never decorated); new public `TenantStoreRegistration.Replace<TStore>(IServiceCollection services, string provider)` helper (in `Stores/TenantStoreRegistration.cs`, new), which registers `TStore` and a factory for `ITenantStore` and `ITenantStoreWriter` that wraps it in `InstrumentedTenantStore` with the `provider` tag, and `AddEncinaTenantSecretProtector<T>()`.
2. `src/Encina.Tenancy/Diagnostics/InstrumentedTenantStore.cs` (new): decorates `ITenantStore` and `ITenantStoreWriter`; constructor `(ITenantStore inner, ITenantStoreWriter? writer, string provider, ...)`; `ActivitySource` "Encina.Tenancy" spans `tenancy.store.{operation}`; counter `encina.tenancy.store.operations` (tags `operation`, `outcome`, `provider`; unit `{operations}`) and histogram `encina.tenancy.store.duration` (unit `s`) added to `TenancyMetrics`. Naming decision, with evidence: ADR-054 decision 4 ("durations in seconds with unit `s`; no `_total` suffix on counters; the unit lives in the metadata") binds every instrument created after it, and these are new instruments, so they follow it. The existing `encina.tenancy.resolutions_total` and `encina.tenancy.resolution_duration_ms` in `TenancyMetrics` pre-date the ADR and are not renamed by this plan (a rename of existing instruments belongs to the ADR-054 meter migration, not to a catalog feature); #597 and #598 inherit the new names.
3. `src/Encina.Tenancy/Diagnostics/TenancyLog.cs` (edit): ids 1811 lookup failed (exception type, never its message), 1812 tenant created, 1813 tenant updated, 1814 tenant deleted, 1815 write rejected (error code), 1816 protector missing. Inside `Tenancy = (1800, 1899)`; #711 owns 1805-1810.
4. `src/Encina.Tenancy/Health/TenantStoreHealthCheck.cs` (new): `DefaultName = "encina-tenant-store"`, static `Tags`, probes `ExistsAsync` with the reserved id `default` (never present, so no tenant data is read); a throw is `Unhealthy` with the exception type only.
5. Tests: `TenantStoreRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`, both orders, application registration kept), `InstrumentedTenantStoreTests`, `TenantStoreHealthCheckTests`, `PublicAPI.Unshipped.txt`, manifest.

Files: 10 (`ServiceCollectionExtensions.cs`, `TenantStoreRegistration.cs`, `InstrumentedTenantStore.cs`, `TenancyMetrics.cs`, `TenancyLog.cs`, `TenantStoreHealthCheck.cs`, three test files, `PublicAPI.Unshipped.txt`) plus the manifest edit makes 11. The `TenantStoreHealthCheck` is registered by the same `ServiceCollectionExtensions.cs` edit and covered by the DI test (inside `TenantStoreRegistrationTests`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 added ITenantStoreWriter and the in-memory writer.
- AddTenantStore<T>() uses RemoveAll and overrides the application; the persistent stores must not.

TASK:
Mark the default ITenantStore descriptor, add TenantStoreRegistration.Replace<TStore>, the instrumented decorator, the TenancyLog ids 1811-1816 and the store health check.

KEY RULES:
- A persistent store wins over the default in any registration order and never replaces an application registration.
- No scoped service injected into a singleton: use IServiceScopeFactory per call.
- Telemetry: operation, outcome, provider only; never the record's tenant id or EncinaError.Message; exception type only.
- Health check: DefaultName const, Tags static array.

REFERENCE FILES:
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Tenancy/Diagnostics/TenancyLog.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs
- src/Encina.Tenancy/Health/TenantHealthCheck.cs
```

</details>

---

### Phase 3: ADO.NET stores (pull requests 3, 4, 5: SqlServer, PostgreSQL, MySQL)

<details>
<summary>Tasks</summary>

Per provider package (`Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`):

1. `Tenancy/TenantStoreADO.cs` (new): `sealed class TenantStoreADO : ITenantStore, ITenantStoreWriter`; constructor `(IDbConnection connection, ITenantSecretProtector? protector = null, string tableName = "Tenants", TimeProvider? timeProvider = null)`; async ADO calls with `CancellationToken` (`OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync`), never the synchronous overloads; parameters only; the duplicate-key violation of the provider maps to `tenancy.tenant_exists` (SQL Server error 2627/2601, PostgreSQL `23505`, MySQL 1062).
2. `Scripts/<next>_CreateTenantsTable.sql` (new) and the same table in `Scripts/000_CreateAllTables.sql` (edit). `<next>` is the next free number when the pull request is cut: the last script is 030 on SqlServer and PostgreSQL and 029 on MySQL today, and the plans of #1234 and #1220 each name 031, so the number is taken then and never fixed here.
3. `Tenancy/TenancyServiceCollectionExtensions.cs` (edit): `AddEncinaTenantStoreADO()` through `TenantStoreRegistration.Replace`.
4. Tests: `TenantStoreADOTests` (unit, throwing and stubbed connection), `TenantStoreADOGuardTests`, `TenantStoreADOIntegrationTests` (derives from `TenantStoreBehaviorSuite`; `[Collection("ADO-SqlServer")]` and its siblings, `ClearAllDataAsync` in `InitializeAsync`), `TenantStoreADORegistrationTests` (DI test for `AddEncinaTenantStoreADO`: builds the provider with `ValidateOnBuild` and `ValidateScopes`, registered before and after `AddEncinaTenancy`, application registration kept), `PublicAPI.Unshipped.txt`, manifest.

Files per pull request: 10. Provider details: SQL Server `nvarchar(128) COLLATE Latin1_General_100_BIN2`; PostgreSQL `varchar(128)`; MySQL `varchar(128) COLLATE utf8mb4_bin`; the connection string column holds the protected text, `nvarchar(max)`/`text`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- The contract, registration helper and behavior suite exist (Phases 1 and 2).
- The catalog uses the application's default connection (Design Choice 3), not the tenant-aware factory.

TASK:
Implement TenantStoreADO for one provider package with its script, registration, tests and manifest entry.

KEY RULES:
- Async database calls with CancellationToken only; parameterized SQL; table name through SqlIdentifierValidator.
- Reject the reserved and invalid ids with TenantIdRules before touching the database.
- Protect the connection string on write, unprotect on read; no protector with a connection string is a Left.
- Duplicate key maps to tenancy.tenant_exists; never log or return the connection string.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs
- src/Encina.ADO.SqlServer/Scripts/029_CreateDeadLetterMessagesTable.sql
- src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/ADO/SqlServer/DeadLetter/DeadLetterStoreADOTests.cs
```

</details>

---

### Phase 4: Dapper stores (pull requests 6, 7, 8: SqlServer, PostgreSQL, MySQL)

<details>
<summary>Tasks</summary>

Per provider package (`Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`):

1. `Tenancy/TenantStoreDapper.cs` (new), same contract and error mapping as Phase 3, `QueryAsync`/`ExecuteAsync` with `CommandDefinition` carrying the token.
2. `Scripts/<next>_CreateTenantsTable.sql` (new) and the table in `Scripts/000_CreateAllTables.sql` (edit) of the Dapper package, which has its own `Scripts/` folder (`src/Encina.Dapper.SqlServer/Scripts/029_CreateDeadLetterMessagesTable.sql` is the precedent).
3. `Tenancy/TenancyServiceCollectionExtensions.cs` (edit): `AddEncinaTenantStoreDapper()`.
4. Tests: unit, guard, integration (`[Collection("Dapper-SqlServer")]` and siblings), the DI registration test (`ValidateOnBuild`, `ValidateScopes`, both orders), `PublicAPI.Unshipped.txt`, manifest.

Files per pull request: 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- The ADO stores exist; the Dapper store has the same behavior and the same table.

TASK:
Implement TenantStoreDapper for one provider package, registration, tests and manifest entry.

KEY RULES:
- Same rules as Phase 3; CommandDefinition with the CancellationToken.
- The behavior suite from Encina.TestInfrastructure is the integration test body; add no store-specific assertion that the suite does not make.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/DeadLetter/DeadLetterStoreDapper.cs
- src/Encina.Dapper.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Dapper/Collections.cs
```

</details>

---

### Phase 5: EF Core store (pull request 9)

<details>
<summary>Tasks</summary>

1. `Tenancy/TenantRecord.cs` and `Tenancy/TenantRecordConfiguration.cs` (new): entity and `IEntityTypeConfiguration`; table `Tenants`; id `HasMaxLength(128)` with the provider's binary collation applied through `UseCollation` per provider (`Latin1_General_100_BIN2`, `utf8mb4_bin`; PostgreSQL none).
2. `Tenancy/TenantStoreEF.cs` (new): takes a `DbContext` type through `IDbContextFactory<TCatalogContext>`-style options, `ITenantSecretProtector?`, `TimeProvider?`; no `SaveChanges` call outside its own method (the store owns its unit of work, it is not the orchestrator).
3. `Tenancy/TenancyServiceCollectionExtensions.cs` (new: `src/Encina.EntityFrameworkCore/Tenancy/` holds only `EfCoreTenancyOptions.cs`, `ITenantSchemaConfigurator.cs`, `TenantDbContext.cs` and `TenantDbContextFactory.cs` today, and `EfCoreTenancyOptions.cs` is an options class): `AddEncinaTenantStoreEF<TContext>()` through `TenantStoreRegistration.Replace`.
4. Tests: unit (in-memory provider is not used; a substituted context), guard, three integration classes (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`), the DI registration test (`ValidateOnBuild`, `ValidateScopes`, both orders), `PublicAPI.Unshipped.txt`, manifest.

Files: 13.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- One package, Encina.EntityFrameworkCore, serves the three EF databases.

TASK:
Implement TenantStoreEF with its entity configuration and the three integration classes.

KEY RULES:
- Unique key violation maps to tenancy.tenant_exists on each provider; token on every async call.
- The binary collation is applied per provider so ids are ordinal on all three.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterStoreEF.cs
- src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/Tenancy/TenantDbContextFactory.cs
```

</details>

---

### Phase 6: MongoDB store (pull request 10)

<details>
<summary>Tasks</summary>

1. `Tenancy/TenantStoreMongoDB.cs` (new): collection `tenants`, `_id` is the tenant id (unique by construction, ordinal), `InsertOneAsync` duplicate-key to `tenancy.tenant_exists`; the registration `AddEncinaTenantStoreMongoDB()` in `Tenancy/TenancyServiceCollectionExtensions.cs` (edit).
2. Tests: unit (substituted driver interfaces), guard, integration (`MongoDB` collection), the DI registration test (`ValidateOnBuild`, `ValidateScopes`, both orders), `PublicAPI.Unshipped.txt`, manifest.

Files: 8.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- MongoDB ids are ordinal; the catalog uses the default database, not the tenant-aware collection factory.

TASK:
Implement TenantStoreMongoDB, its registration, tests and manifest entry.

KEY RULES:
- Cancellation token on every driver call; duplicate key maps to tenancy.tenant_exists.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.MongoDB/Tenancy/TenancyServiceCollectionExtensions.cs
```

</details>

---

### Phase 7: Cross-cutting integration, documentation and finalization (pull request 11)

<details>
<summary>Tasks</summary>

1. Documentation by `docs-writer`: `src/Encina.Tenancy/README.md` (create: it does not exist on `origin/main`; persistent stores, the write port, the protector, id rules), a feature page under `docs/features/` for the catalog (create the tenants table, register the store, register a protector), `docs/INVENTORY.md` (edit).
2. Cross-cutting: the matrix below; the deferred items point to #711 (cache), #752 (audit) and the issue files named there.
3. Build and test verification: `dotnet build --configuration Release` with 0 warnings, `dotnet test` per flag against the targets of the Research section.

Files: 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- All ten stores are merged and pass the shared behavior suite.

TASK:
Write the Tenancy README section and the feature page; update INVENTORY.md.

KEY RULES:
- Cite coverage with covref markers, never typed figures; English only; no private application names (write "the reference application").

REFERENCE FILES:
- src/Encina.Tenancy/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-002 REQ-061, AC-043, DEC-009 (b) | Multi-tenancy in 1.0; #596 must be closed for AC-043 |
| SPEC-000 section 6 | Database matrix of 10; SQLite and Oracle excluded |
| ADR-024, ADR-009 | SQLite and Oracle are out of the matrix |
| ADR-062 | `ErrorCategory` of every new error code: `tenancy.tenant_exists` AlreadyExists, `tenancy.tenant_id_invalid` and `tenancy.tenant_id_reserved` InvalidRequest, `tenancy.protector_required` FailedPrecondition |
| ADR-054 D4 | Tenant attribute on telemetry only through the #2676 helper; the record's own id is never a tag |
| AGENTS.md section 3 | Async database calls, `TimeProvider`, fail closed, registration completeness |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ITenantStore`, `InMemoryTenantStore`, `TenantInfo` | `src/Encina.Tenancy/` | The read contract and default; the writer extends the in-memory store |
| `TenantConnectionFactory` (x6), `TenantDbContextFactory`, `TenantAwareMongoCollectionFactory`, `TenantResolutionMiddleware` | provider packages, `Encina.Tenancy.AspNetCore` | `TenantConnectionFactoryBase` is the 11th consumer; all 11 keep working unchanged |
| `DeadLetterStoreADO` and siblings | `src/Encina.*/DeadLetter/` | Pattern for constructor, table name validation, error mapping and tests |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Pattern for the telemetry decorator |
| `TenancyLog`, `TenancyMetrics`, `TenancyActivitySource` | `src/Encina.Tenancy/Diagnostics/` | Extended, not duplicated |
| Integration collections and fixtures | `tests/Encina.TestInfrastructure/Fixtures/`, `tests/Encina.IntegrationTests/**/Collections.cs` | `ADO-`, `Dapper-`, `EFCore-` and `MongoDB` collections |

The provider file inventory below was drafted by the local model (`artifacts/local-ai/out/tenant-store-provider-table.md`, ledger row `tenant-store-provider-table`) and reviewed against the code.

| Provider | Store class (new) | Folder | Table or model file (new) | Registration (new) | Notes |
|----------|-------------------|--------|---------------------------|--------------------|-------|
| ADO.NET SqlServer | `TenantStoreADO` | `Tenancy/` | `Scripts/<next>_CreateTenantsTable.sql` | `AddEncinaTenantStoreADO` | `nvarchar(128) COLLATE Latin1_General_100_BIN2` |
| ADO.NET PostgreSQL | `TenantStoreADO` | `Tenancy/` | `Scripts/<next>_CreateTenantsTable.sql` | `AddEncinaTenantStoreADO` | `varchar(128)` |
| ADO.NET MySQL | `TenantStoreADO` | `Tenancy/` | `Scripts/<next>_CreateTenantsTable.sql` | `AddEncinaTenantStoreADO` | `varchar(128) COLLATE utf8mb4_bin` |
| Dapper SqlServer / PostgreSQL / MySQL | `TenantStoreDapper` | `Tenancy/` | `Scripts/<next>_CreateTenantsTable.sql` in each Dapper package (each has its own `Scripts/` folder) | `AddEncinaTenantStoreDapper` | same column types |
| EF Core SqlServer / PostgreSQL / MySQL | `TenantStoreEF` | `Tenancy/` | `TenantRecord.cs`, `TenantRecordConfiguration.cs` | `AddEncinaTenantStoreEF` | `UseCollation` per provider |
| MongoDB | `TenantStoreMongoDB` | `Tenancy/` | collection `tenants`, `_id` | `AddEncinaTenantStoreMongoDB` | unique by `_id` |

Per-file coverage targets (numeric, per flag; from the `DeadLetterStore*` rows of the same manifests):

| File | unit | guard | integration | Justification |
|------|-----:|------:|------------:|---------------|
| `Tenancy/TenantStoreADO.cs` (x3) | 90 | 8 | 90 | Unit tests run every statement on a stubbed or throwing connection except provider-specific paging and the duplicate-key code, which the real-database suite covers; guard reaches only constructor and argument checks |
| `Tenancy/TenantStoreDapper.cs` (x3) | 90 | 8 | 90 | Same reasoning for the Dapper mapping |
| `Tenancy/TenantStoreEF.cs` | 85 | 8 | 90 | The context is substituted in unit tests; the three real databases cover query translation and unique violations |
| `Tenancy/TenantStoreMongoDB.cs` | 100 | 8 | 80 | Substituted driver interfaces cover every `Left`; the real server covers the unique `_id` |
| `Core/TenantIdRules.cs`, `Stores/TenantStoreRegistration.cs`, `Diagnostics/InstrumentedTenantStore.cs` | 100 | 10 | 0 | Pure logic and DI; integration is not applicable because the stores' integration suites run through the decorator already |
| `Health/TenantStoreHealthCheck.cs` | 100 | 10 | 0 | A single probe with two outcomes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy` | 1811-1816 inside `Tenancy = (1800, 1899)` | 1811 lookup failed, 1812 created, 1813 updated, 1814 deleted, 1815 write rejected, 1816 protector missing; 1805-1810 belong to #711; no new `EventIdRanges.cs` field and no registry row, because the range exists. The range moves to 10600-10699 with #4007 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Contract, rules and in-memory (PR 1) | 13 | Includes the shared behavior suite and the changelog fragment |
| Registration, decorator, health (PR 2) | 11 | One PublicAPI, one manifest |
| ADO.NET x3 (PRs 3-5) | 10 each | Store, script, `000` edit, registration, PublicAPI, manifest, 4 test files (unit, guard, integration, DI) |
| Dapper x3 (PRs 6-8) | 10 each | Same shape as ADO: each Dapper package has its own `Scripts/` folder |
| EF Core (PR 9) | 13 | Entity, configuration, store, new registration file, 3 integration classes, unit, guard, DI test, PublicAPI, manifest |
| MongoDB (PR 10) | 8 | |
| Documentation (PR 11) | 3 | Written by `docs-writer` |

Every pull request is at most 15 files and builds and passes alone; PR 2 depends on PR 1, and PRs 3-10 on PR 2.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is pre-1.0, .NET 10, C# 14, nullable enabled; ROP with Either<EncinaError, T>.
- ITenantStore is read-only and has one implementation, InMemoryTenantStore. The goal is a persistent, writable tenant catalog on the 10 database providers.

IMPLEMENTATION OVERVIEW:
1. Phase 1: ITenantStoreWriter, ITenantSecretProtector, TenantIdRules, four error codes, ordinal InMemoryTenantStore, shared behavior suite in Encina.TestInfrastructure.
2. Phase 2: marked default registration plus TenantStoreRegistration.Replace, InstrumentedTenantStore, TenancyLog 1811-1816, TenantStoreHealthCheck.
3. Phases 3-6: TenantStoreADO x3, TenantStoreDapper x3, TenantStoreEF, TenantStoreMongoDB, each with its table, registration, tests and manifest entry, one pull request per package.
4. Phase 7: README, feature page, INVENTORY.

KEY PATTERNS:
- Async database calls with CancellationToken; parameterized SQL; table names through SqlIdentifierValidator.
- Reserved id default and invalid ids rejected by TenantIdRules; ids are ordinal and case-sensitive.
- Connection strings are protected at rest through ITenantSecretProtector; never logged or returned in errors.
- A persistent store wins over the in-memory default in any registration order and never replaces an application registration.
- Telemetry carries operation, outcome and provider only.

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/ITenantStore.cs
- src/Encina.Tenancy/Stores/InMemoryTenantStore.cs
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs
- src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterStoreEF.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- docs/plans/config-change-audit-implementation-plan-752.md (by issue number: #752)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred to #711 (open, tenant connection cache); a read-through cache over `ITenantStore` belongs to that plan, which already owns the invalidation question |
| 2 | OpenTelemetry | ✅ | `InstrumentedTenantStore` spans and counters (Phase 2); no record tenant id as a tag, ambient tenant tag through #2676 |
| 3 | Structured Logging | ✅ | `TenancyLog` ids 1811-1816 (Phase 2); exception type only |
| 4 | Health Checks | ✅ | `TenantStoreHealthCheck` probes the catalog with the reserved id (Phase 2) |
| 5 | Validation | ✅ | `TenantIdRules` at the write boundary on every provider (Phase 1); request-level validation is not applicable to a store |
| 6 | Resilience | ❌ | A transient failure throws and the request fails closed; retry belongs to the caller or the provider pipeline, and retrying inside the store would hide an outage from the health check |
| 7 | Distributed Locks | ❌ | Every write is one statement protected by the unique key; no coordination is needed |
| 8 | Transactions | ❌ | One row per operation; the `tenant_features` table that needed atomic multi-row writes is deferred (Design Choice 5) |
| 9 | Idempotency | ✅ | A second create with the same id returns `Left(tenancy.tenant_exists)`, never a silent success (`ErrorCategory.AlreadyExists`) |
| 10 | Multi-Tenancy | ✅ | This is the tenant catalog: it is global by design, never tenant-filtered; `default` reserved; ids ordinal and injective |
| 11 | Module Isolation | ❌ | The catalog is cross-module by design |
| 12 | Audit Trail | ⏭️ | Deferred to #752 (open): the administration service audits create, update and deactivate through the write port delivered here |

---

## Prerequisites & Dependencies

- None blocks PR 1. PR 3 onward takes the next free `Scripts/` number at that moment (the plans of #1234 and #1220 name 031, so the pull request rebases and renumbers).
- #752 and #686 add `IsActive`, `TenantCountry` and `TenantRegulations` later; each adds its own column script to every provider.
- #597 and #598 reuse `ITenantStoreWriter`, `TenantIdRules`, `ITenantSecretProtector`, `TenantStoreRegistration`, `InstrumentedTenantStore` and the behavior suite owned by this plan.
- #4182 (retention) enumerates tenants through `ITenantStore.GetAllTenantsAsync`; the persistent stores return every tenant, in no promised order.

---

## Next Steps

1. The maintainer answers Dilemmas D1-D4 when work on the issue starts (D1 together with #752's write-port Dilemma).
2. Cut PR 1 from `origin/main`; run `dotnet test` for the Tenancy unit, guard and contract flags and report the per-file measurements against the targets above.
3. Open the provider pull requests one package at a time, each with its integration collection green on a real database.
4. Cite this plan from #597 and #598, which build on its contract.
