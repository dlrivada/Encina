# Audit result: Encina.ADO.PostgreSQL

- Checklist version: SPEC-003 §5.2, version 1 (2026-09-24 draft)
- Audit date: 2026-09-24
- Auditor: Claude (issue-worker, pilot 2 of SPEC-003)
- Unit: `src/Encina.ADO.PostgreSQL` (one project = one audit unit)
- Manifest: `.github/coverage-manifest/Encina.ADO.PostgreSQL.json` (66 files, targets unit 30 / guard 10 / contract 5 / integration 25)

## Method note

Coverage percentages below were computed by running each flag's test project filtered to
`FullyQualifiedName~ADO.PostgreSQL` with `--collect "XPlat Code Coverage"`, then summing
Cobertura `<line>` elements restricted to (a) files physically under
`src/Encina.ADO.PostgreSQL/`, and (b) whose manifest entry declares that flag (`override` wins
over `defaultTests`). This reproduces the obligations model of `.github/scripts/coverage-report.cs`
at the file level but was not run through that script itself (it expects the full CI-produced
docref pipeline); the unit-flag figure should be treated as close but not authoritative and is
flagged as a minor finding below (see F-08).

## Checklist rows

| ID | Outcome | Evidence |
|---|---|---|
| AUD-01 | **finding (major)** | The TimeProvider decision (#543, #667) is contradicted by 4 call sites that still read `DateTime.UtcNow`/`DateTimeOffset.UtcNow` directly (see AUD-14). No ADR, SPEC or later issue records an exception for this package. Class B drift per SPEC-003 §5.2 — escalated, not silently fixed (INV-004). |
| AUD-02 | **finding (major)**, several functions deferred/absent | Caching: absent, no `ICacheProvider`/`HybridCache` reference anywhere in the package (contrast: `Encina.EntityFrameworkCore/Caching/*` exists for the same feature area). OpenTelemetry: absent, no `ActivitySource`/`Meter` anywhere. Resilience: absent, no Polly/`ResiliencePipeline`. Distributed locks: correctly absent (out of scope — `pg_advisory_lock` belongs to a would-be `Encina.DistributedLock.PostgreSQL`, itself missing from `src/`, tracked separately). Structured logging: present but thin — only 5 `[LoggerMessage]` call sites (`ReadWriteRoutingPipelineBehavior.cs:149`, `TemporalRepositoryADO.cs:743,753,762,772`) versus ~20 store/factory classes with none (Outbox, Inbox, Sagas, Auditing ×3, ABAC, Anonymization, Scheduling, BulkOperations). Health checks: integrated (`Health/PostgreSqlHealthCheck.cs`, `Health/PostgreSqlDatabaseHealthMonitor.cs`, `ReadWriteSeparation/ReadWriteSeparationHealthCheck.cs`). Transactions: integrated (`UnitOfWork/UnitOfWorkADO.cs`). None of the absent functions has a recorded deferral issue. |
| AUD-03 | **partial** | Manifest declares all 66 source files with no dangling or missing entries (verified: every manifest key maps to a file on disk under `src/Encina.ADO.PostgreSQL/`, and the file count matches `totalFiles: 66`). Measured against target (see Coverage section): guard 26.0% (target 10%, pass), contract 92.9% (target 5%, pass), integration 39.2% (target 25%, pass), unit 29.6% (target 30%, effectively at target but measured 0.4 pt short by the proxy method — see F-08, needs the official script to confirm pass/fail). |
| AUD-04 | pass | Sampled `InboxStoreADOGuardsTests.cs`, `ServiceCollectionExtensionsGuardsTests.cs`, `ReadWriteRoutingPipelineBehaviorContractTests.cs`: all instantiate real classes (`new InboxStoreADO(connection)`, `services.AddEncinaADO(...)` on a real `ServiceCollection`, `new ReadWriteRoutingPipelineBehavior<...>(...)`) and call real methods. One `typeof(...).IsAssignableFrom` check exists but sits alongside a real invocation in the same test, not standalone. |
| AUD-05 | **finding (minor)** | `Encina.ADO.SqlServer` has `tests/Encina.GuardTests/ADO/SqlServer/BulkOperationsADOGuardTests.cs`; no equivalent exists for PostgreSQL or MySQL (`tests/Encina.GuardTests/ADO/PostgreSQL/` has no `BulkOperations*` file, and no justification `.md` sits in its place). Everything else (unit/guard/contract/integration folders for all other features) is present and populated. |
| AUD-06 | n/a | No closed issue labeled `bug` and touching `ADO.PostgreSQL` was found (`gh issue list --state closed --search "ADO.PostgreSQL is:issue label:bug"` → 0 results; the 15 closed issues found under a plain "ADO.PostgreSQL" search are all `enhancement`/`technical-debt`/`area-testing`). AUD-06 does not apply for lack of a bug record; it will apply to any future bug fix. |
| AUD-07 | **finding (major)**, one item partial | ADO.NET triangle (PostgreSQL/SqlServer/MySQL) is symmetric for every `*StoreADO.cs`/`*RepositoryADO.cs` in this package — no store/repository is missing relative to its ADO siblings (MySQL is missing `Temporal/*`, but that is a MySQL-side gap, not a PostgreSQL one). Comparing against `Encina.EntityFrameworkCore` (same database, different technology): EF Core ships `Caching/*` (query-result caching, 6 files) and `DomainEvents/*` (dispatch-on-`SaveChanges`, 3 files) that ADO.PostgreSQL has no equivalent of and no deferral issue for. `Encina.Dapper.PostgreSQL` has full feature parity with ADO.PostgreSQL (same folders, `Scripts/`/`TypeHandlers/` are Dapper-only mechanics, not a feature gap). |
| AUD-08 | pass | 5 `[LoggerMessage]` EventIds (3250–3254) all fall inside the registered `EventIdRanges.ADOPostgreSQL = (3250, 3299)` (`EventIdRanges.cs:138`); `EncinaEventIdAllocationTests.cs:43` lists `"Encina.ADO.PostgreSQL"` in `AssemblyRanges`. No `LoggerMessage.Define`/bare `new EventId(` usage. |
| AUD-09 | pass | `dotnet build src/Encina.ADO.PostgreSQL/Encina.ADO.PostgreSQL.csproj -c Release` → "Compilación correcta. 0 Advertencia(s). 0 Errores." `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` both exist. |
| AUD-10 | pass | `.csproj:9-10` sets `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; `NoWarn` only lists PublicAPI analyzer codes (`RS0016;RS0017;RS0026;RS0036`), not CS1591; `GlobalSuppressions.cs` suppresses only CA/IDE rules. Missing XML docs would fail the build. |
| AUD-11 | **finding (major)** | `README.md` exists, names real types (`NpgsqlConnection`, `INotification`) and states database coverage in its first line. Its Quick Start code sample calls `AddEncinaADOPostgreSQL(...)` (`README.md:37`), a method that does not exist — the real extension methods are the `AddEncinaADO(...)` overloads in `ServiceCollectionExtensions.cs:39,118,150`. A reader following the README gets a compile error. |
| AUD-12 | pass | No catch block in `AuditStoreADO.cs`, `ReadAuditStoreADO.cs`, `PolicyStoreADO.cs` or `TokenMappingStoreADO.cs` swallows a security/compliance failure into a default-allow; all failures propagate as `Left(EncinaError...)` (ROP). The one bare catch (`AuditStoreADO.cs:510`) only defaults metadata *deserialization* to an empty dictionary, not a security decision. |
| AUD-13 | **finding (minor)** | `AuditStoreADO.cs:143,178,215,248,326,350` and `ReadAuditStoreADO.cs:120,155,192,251,275` build `EncinaError.New($"... {ex.Message}")`; `ex.Message` from Npgsql can include constraint/column names. `TokenMappingStoreADO.cs:66` does the same (`AnonymizationErrors.StoreError("Store", ex.Message)`) — this store maps tokens to original values, so this is the more sensitive of the two families. Queries are parameterized (no raw SQL text with values is interpolated), so this is not a direct data-subject-identifier leak, but the exception message is not sanitized before it reaches `EncinaError`, which #856/SPEC-002 REQ-034 says must never happen for a security-classified defect. |
| AUD-14 | **finding (major)** | `Sharding/Migrations/PostgreSqlSchemaIntrospector.cs:111` (`new ShardSchema(shardId, tables, DateTimeOffset.UtcNow)`) and `Sharding/Migrations/AdoMigrationHistoryStore.cs:82,116,191` (`DateTime.UtcNow` passed as a SQL parameter) read wall-clock time directly instead of taking `TimeProvider`. (Two other apparent hits, `TemporalRepositoryADO.cs:69,623`, are inside a doc comment and a string literal respectively — not real violations.) |
| AUD-15 | pass | Only options class in the package is `Tenancy/ADOTenancyOptions.cs`; none of its properties (`AutoFilterTenantQueries`, `AutoAssignTenantId`, `ValidateTenantOnModify`, `ThrowOnMissingTenantContext`, `TenantColumnName`) is a password/connection-string/token/key, so `[JsonIgnore]`/`ToString()` do not apply. |
| AUD-16 | **finding (blocker for UnitOfWorkADO, major elsewhere)** | Synchronous ADO.NET calls found at: `UnitOfWork/UnitOfWorkADO.cs:153,175` (`.BeginTransaction()`/`.Commit()` — hot path, once per unit of work, the exact pattern #794/#897 fixed elsewhere); `BulkOperations/BulkOperationsPostgreSQL.cs:381`, `ABAC/PolicyStoreADO.cs:435`, `Modules/SchemaValidatingConnection.cs:81`, `Temporal/TemporalRepositoryADO.cs:635,662,672,682`, `Anonymization/TokenMappingStoreADO.cs:252`, `Sharding/ReferenceTables/ReferenceTableStoreFactoryADO.cs:21`, `Sharding/ReferenceTables/ReferenceTableStoreADO.cs:96,98`, `Sharding/Migrations/AdoHelper.cs:18,28,38`, `Sharding/Migrations/AdoMigrationExecutor.cs:53`, `Modules/SchemaValidatingCommand.cs:118,132` (`.Open()`, `.ExecuteReader()`, `.ExecuteScalar()`, `.ExecuteNonQuery()`, `.Read()`). 17 call sites across 10 files. |
| AUD-17 | pass (mechanically verified), test gap noted under AUD-05-adjacent | `AddEncinaADO` delegates to the shared `AddMessagingServices<...>` (`Encina.Messaging/MessagingServiceCollectionExtensions.cs:78-135`), which registers `OutboxOptions`/`InboxOptions`/`SagaOptions`/`SchedulingOptions` gated by the same `Use*` flag that registers the matching store. `OutboxProcessor` is the only store/processor with an `IOptions<T>`-shaped dependency (`OutboxOptions`), and it is registered whenever the processor is. No #1273-style resolution failure exists. However, no test in `tests/` builds the provider with `ValidateOnBuild`+`ValidateScopes` for this package's registrations — the safety net AUD-17 asks for is itself missing (folded into F-05 below as a test gap, not a live defect). |
| AUD-18 | pass | No `[Obsolete]` anywhere in `src/Encina.ADO.PostgreSQL`. |

## Coverage per flag vs manifest target

| Flag | Target | Measured (proxy method, see note above) | Verdict |
|---|---|---|---|
| unit | 30% | 29.6% (1006/3398 lines, files whose manifest flag includes `unit`) | at target, 0.4pt under by proxy — re-verify with official script (F-08) |
| guard | 10% | 26.0% (884/3398 lines) | pass |
| contract | 5% | 92.9% (52/56 lines) | pass |
| integration | 25% | 39.2% (4268/10884 lines); 213 passed, 4 skipped (ReadWriteSeparation tests requiring a distinct read replica connection string not configured in the test fixture), 0 failed | pass |
| property | n/a | no property test project entries for this package in the manifest (no `property` flag declared for any file) | n/a — not required by manifest |

Test run commands (from `D:\Proyectos\Encina\.claude\worktrees\waud-adopg`):
```
dotnet test tests/Encina.UnitTests/Encina.UnitTests.csproj -c Release --filter "FullyQualifiedName~ADO.PostgreSQL" --collect "XPlat Code Coverage" --results-directory artifacts/audit/coverage/unit
  -> 224 passed, 0 failed
dotnet test tests/Encina.GuardTests/Encina.GuardTests.csproj -c Release --filter "FullyQualifiedName~ADO.PostgreSQL" --collect "XPlat Code Coverage" --results-directory artifacts/audit/coverage/guard
  -> 277 passed, 0 failed
dotnet test tests/Encina.ContractTests/Encina.ContractTests.csproj -c Release --filter "FullyQualifiedName~ADO.PostgreSQL" --collect "XPlat Code Coverage" --results-directory artifacts/audit/coverage/contract
  -> 3 passed, 0 failed
dotnet test tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ADO.PostgreSQL" --collect "XPlat Code Coverage" --results-directory artifacts/audit/coverage/integration
  -> 213 passed, 4 skipped, 0 failed (real PostgreSQL container via Testcontainers/Docker Desktop, confirmed running)
```

## Closed issues consulted

- #545 [TEST] Bulk Operations Integration Tests for ADO.NET and Dapper
- #667 [DEBT] TimeProvider parameter missing from OutboxStoreDapper/InboxStoreDapper (PostgreSQL/MySQL/SqlServer) — directly relevant to F-01/AUD-14
- #536 [FEATURE] missing provider support for #279-283, #534, #380
- #113 [FEATURE] Automatic health checks per infrastructure provider
- #534 [FEATURE] Module Isolation by Database Permissions
- #623 [FEATURE] Auto-populate IAuditableEntity fields for Dapper/ADO.NET/MongoDB
- #279 [FEATURE] Generic Repository Pattern
- #280 [FEATURE] Specification Pattern
- #281 [FEATURE] Unit of Work Pattern
- #403, #407, #413 — compliance module features referencing ADO providers
- #549, #547, #548, #568, #538 — provider test-coverage issues (MongoDB/EF Core/benchmarks/load tests), read for context on what "done" looked like for sibling providers
- No closed `bug`-labeled issue references this package (checked directly, see AUD-06).
