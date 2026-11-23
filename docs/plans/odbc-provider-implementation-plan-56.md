# Implementation Plan: Encina.ODBC (Messaging Stores over System.Data.Odbc, a Dialect Interface, Verified on PostgreSQL through psqlODBC)

> **Issue**: [#56](https://github.com/dlrivada/Encina/issues/56)
> **Type**: Feature (new provider package `Encina.ODBC`)
> **Related**: SPEC-000 DEC-009 (the 1.0 scope widens to deferred items; the issue carries `status-deferred` and a comment moving it post-1.0, now superseded), ADR-009 (Oracle removed) and ADR-024 (SQLite out of the matrix), #721 (OpenTelemetry for the ADO providers), #737, #738, #739 and #747 (tenant and module columns on the messaging rows, open), #717 (leader election), ADR-021, ADR-054, ADR-062
> **Complexity**: Medium (five stores and a dialect seam; the care is in the positional-parameter rewrite, the dialect contract and a test engine that CI can run)
> **Estimated Scope**: ~2,300 lines of production code + ~2,700 lines of tests, in seven pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-23, main `ce352001f`)

- There is no `Encina.ODBC` project, no `System.Data.Odbc` entry in `Directory.Packages.props` (the latest stable on nuget.org on 2026-11-23 is 10.0.15) and no manifest. `ROADMAP.md` lists "ODBC Provider #56" as deferred; the issue comment of the deferral says it would be re-evaluated after 1.0, and SPEC-000 DEC-009 (2026-10-23) widened the 1.0 scope to deferred items, so the feature is plannable.
- The database matrix of `AGENTS.md` section 5 has ten providers (ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, plus MongoDB). Oracle and SQLite are out of the matrix; ODBC is not named. The 25 feature folders of `Encina.ADO.SqlServer` (outbox, inbox, saga, scheduling, dead letter, audit, tenancy, unit of work, repositories, bulk operations, ABAC, anonymization, break-the-glass, sharding, soft delete, temporal, read/write separation, module isolation, health and more) show how large "a provider" is; the ADO.NET project has 69 source files.
- The messaging contracts are in `Encina.Messaging`: `IOutboxStore` (9 methods), `IInboxStore` (8), `ISagaStore` (6), `IScheduledMessageStore` (7) and `IDeadLetterStore` (10), each returning `Task<Either<EncinaError, T>>`. Shared helpers an ODBC store can reuse live in `Encina.Messaging`: `SqlIdentifierValidator`, `StoreValidationMessages`, `Transactions/DbLease` and `IDbTransactionAccessor`; `EitherHelpers.TryAsync` is in core `Encina` (`src/Encina/Results/EitherHelpers.cs`).
- A provider registers everything through `services.AddMessagingServices<TOutboxStore, TOutboxFactory, TInboxStore, TInboxFactory, TSagaStore, TSagaFactory, TScheduledStore, TScheduledFactory, TOutboxProcessor>(config)` and `AddDeadLetterQueueServices<TStore, TFactory>(...)` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, used at `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` lines 69 to 82), with the connection supplied as a scoped `IDbConnection`. These helpers are all-or-nothing: a provider needs all four stores and the processor before it can call them.
- `OutboxStoreADO` (440 lines) takes `(IDbConnection connection, string tableName, TimeProvider? timeProvider, IDbTransactionAccessor? transactionAccessor)`, validates the table name with `SqlIdentifierValidator.ValidateTableName`, writes named SQL parameters (`@BatchSize`, `@NowUtc`) and uses SQL Server syntax (`SELECT TOP (@BatchSize)`). Each provider package holds its own copies of the message classes (`OutboxMessage`, `OutboxMessageFactory`) and an `OutboxProcessor` that only binds `OutboxProcessorBase` to the provider's registration.
- ODBC differs in ways that decide the design: parameters are **positional** (`?`; `OdbcParameter` names are ignored), row limiting and type names are driver specific (`TOP n`, `LIMIT n` or `FETCH FIRST n ROWS ONLY`; `TIMESTAMP` is a date-time in ANSI and PostgreSQL but a row version in SQL Server; there is no portable unbounded text type), connection pooling belongs to the ODBC driver manager (not to `OdbcConnection`), and Microsoft Access runs only on Windows with the ACE driver, so no Linux runner can exercise it.
- The `MessagingConfiguration` flags (`UseOutbox`, `UseInbox`, `UseSagas`, `UseScheduling`, `UseDeadLetterQueue`, `UseRoutingSlips`, `UseTenancy`, `UseModuleIsolation`, ...) are the shared switches; the issue's `config.ConnectionString` and `config.SqlDialect` are not members of it.
- The messaging rows have no `TenantId` or `ModuleId` today (`IOutboxMessage` lists `Id`, `NotificationType`, `Content`, `CreatedAtUtc`, `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc`); #737, #738, #739 and #747 add them.

### What this plan delivers

- **`Encina.ODBC`** (references `Encina`, `Encina.Messaging`, `System.Data.Odbc`): `IOdbcSqlDialect` (limit clause, type names for guid, timestamp, unbounded text, integer and boolean, the health query) with the built-in `AnsiOdbcDialect`; `OdbcSqlTemplate` (rewrites the store SQL written with `@Name` markers into `?` and the ordered parameter list, ignoring markers inside string literals); `EncinaOdbcOptions` (`ConnectionString`, `Dialect`, table names; the connection string is `[JsonIgnore]` and `ToString()` hides it).
- **Five stores**, named by `AGENTS.md` section 4: `OutboxStoreODBC`, `InboxStoreODBC`, `SagaStoreODBC`, `ScheduledMessageStoreODBC`, `DeadLetterStoreODBC`, with their message classes, factories, `OutboxProcessor` and one SQL script per table (ANSI names; engines with other type names supply a dialect).
- **Registration** `services.AddEncinaOdbc(string connectionString, IOdbcSqlDialect dialect, Action<MessagingConfiguration> configure)`: a scoped `OdbcConnection` as `IDbConnection`, `AddMessagingServices`, `AddDeadLetterQueueServices`, the health check; a `MessagingConfiguration` flag that the ADO providers satisfy with their own provider-specific registration and ODBC does not (`UseAuditLogStore`, `UseReadAuditStore`, `UseBreakTheGlassStore`, `UseAnonymization`, `UseABACPolicyStore`, `UseModuleIsolation` and `UseReadWriteSeparation`, `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` lines 96 to 177) makes registration throw `InvalidOperationException` naming the flag, so nothing is silently skipped; the flags that the shared `Encina.Messaging` helpers register themselves (`UseRoutingSlips`, `UseRecoverability`, `UseContentRouter`, `UseScatterGather`) keep working through `AddMessagingServices`.
- **Verification**: SQL-text tests for every dialect member and store; integration, contract and property tests against **PostgreSQL through psqlODBC** (Design Choice 4), using a test dialect over `AnsiOdbcDialect` for PostgreSQL's type names.
- **Not delivered**: a built-in Access dialect (Design Choice 3), the other 20 feature folders of the ADO providers (Design Choice 1), pooling settings (a driver-manager feature), and telemetry (follow-up issue file `artifacts/issues/odbc-telemetry.md`).
- Affected packages: `Encina.ODBC` (new). Provider category: Database, an eleventh provider outside the ten-provider matrix (documented in the ADR and in Next Steps).

---

## Design Choices

<details>
<summary><strong>1. Which Encina features the ODBC provider implements</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The messaging stores only (outbox, inbox, saga, scheduling, dead letter), as the issue lists; the provider is documented as messaging-only; any other feature needs its own plan** | One bounded, testable slice; the issue's feature list ("Messaging patterns") is met; the dead letter store is included because `AddMessagingServices` and the recoverability flags need it | A team that picks ODBC cannot use audit, unit of work, repositories or tenancy stores on it |
| **B) Parity with the 25 feature folders of the ADO providers** | A full provider | Roughly 60 more source files and their tests for a provider the issue calls an "isolated feature (lower priority)"; each feature must also be correct on an engine-agnostic dialect |
| **C) Outbox and inbox only** | Smallest | `AddMessagingServices` needs all four stores and the processor; sagas and scheduling would throw at registration |

### Chosen Option: **A - Messaging stores only** (recommended, pending the maintainer)

### Rationale

- A wins for a legacy-database bridge, which is the issue's stated purpose; B wins if a customer needs ODBC as a full peer of the ten providers, each further feature then being planned on this plan's dialect seam; C is not viable because the shared registration is all-or-nothing. An unsupported `MessagingConfiguration` flag fails at registration (fail closed, `AGENTS.md` section 3 and its registration-completeness rule).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How SQL dialects are expressed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An interface `IOdbcSqlDialect` with the built-in `AnsiOdbcDialect`; "custom" means the application implements the interface** | A dialect can carry behavior (limit syntax, type names, the health query); no enum switch in every store; custom dialects are first class | A public interface to keep stable |
| **B) The issue's `enum SqlDialect { Generic, Access, Custom }` with a `switch` in the stores** | Matches the issue text | `Custom` has no way to carry behavior; every store grows a switch; adding an engine edits every store |

### Chosen Option: **A - Interface and one built-in dialect** (recommended, pending the maintainer)

### Rationale

- A wins because a user-defined dialect needs code, not an enum value, and the stores stay free of engine knowledge; B wins only if the maintainer wants the enum names verbatim and accepts switches in five stores. The dialect members are the ones the stores need: `string Limit(string selectList, string from, string orderBy, int count)` (or a limit suffix), `GuidType`, `TimestampType`, `UnboundedTextType`, `IntegerType`, `BooleanType` and `HealthQuery`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Whether a built-in Access dialect ships</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only `AnsiOdbcDialect` ships; the guide shows an Access dialect as a worked example that the application owns** | Everything shipped is exercised by CI; no unsupported engine in the package | The issue's `Access` member is not delivered as code |
| **B) Ship `AccessOdbcDialect` too** | Meets the issue's list | No Linux runner has the ACE driver; the dialect would be verified by SQL text only, never by an engine, and `AGENTS.md` section 9 expects tests that execute real behavior |

### Chosen Option: **A - Ansi only, Access as a documented example** (recommended, pending the maintainer)

### Rationale

- A wins while CI cannot run Access; B wins if the maintainer has a Windows runner or accepts text-only verification for that dialect. The same reasoning applies to any engine without a CI path.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Which engine proves the provider in CI</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) PostgreSQL through psqlODBC** (`odbc-postgresql` and `unixodbc` from the distribution's package archive), reusing the existing PostgreSQL fixture | Open-source driver installed with one package manager call; the repository already runs PostgreSQL in Testcontainers (`PostgreSqlFixture`) | A runner step installs the driver (a workflow change owned by the orchestrator) |
| **B) SQL Server through Microsoft's ODBC Driver 18** | The SQL Server fixture exists; the driver is Microsoft's | The driver comes from Microsoft's package repository with a licence acceptance step; `TIMESTAMP` and limit syntax need SQL Server specific dialect members |
| **C) Both** | Two engines prove the dialect seam | Two driver installs and two collections for a provider the issue marks low priority |

### Chosen Option: **A - PostgreSQL through psqlODBC** (recommended, pending the maintainer)

### Rationale

- A wins on installation cost; B wins if the maintainer prefers the Microsoft driver, whose behavior is the one most Windows ODBC users have; C wins when the maintainer wants a second engine to prove that the dialect seam is not PostgreSQL-shaped. The test engine does not make `AnsiOdbcDialect` run unmodified on PostgreSQL: the integration tests use `PostgreSqlOdbcTestDialect : AnsiOdbcDialect` that overrides the type names (`TEXT` for unbounded text), which also proves the extension point.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Store SQL is written with `@Name` markers like the ADO stores and rewritten by `OdbcSqlTemplate` to `?` plus the ordered parameter list; a repeated marker repeats the parameter; markers inside string literals are ignored | Trivial | `src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs` (named parameters); `System.Data.Odbc` positional parameters |
| C2 | Table names go through `SqlIdentifierValidator.ValidateTableName` and are never quoted or concatenated from caller input beyond that whitelist | Settled | `src/Encina.Messaging/SqlIdentifierValidator.cs`; `AGENTS.md` section 3 |
| C3 | Database calls are asynchronous with a `CancellationToken` (`OpenAsync`, `ExecuteNonQueryAsync`, `ExecuteReaderAsync`); never `IDbConnection.Open()` | Settled | `AGENTS.md` section 3 (Sonar S6966) |
| C4 | Time comes from `TimeProvider` (an optional constructor parameter, default `TimeProvider.System`); timestamps are UTC with the `AtUtc` suffix; no `DateTime.UtcNow` | Settled | `AGENTS.md` sections 3 and 4 |
| C5 | A store returns `Either<EncinaError, T>` and wraps database failures with `EitherHelpers.TryAsync` like the ADO stores; no new `EncinaError` code is introduced, so no new ADR-062 category; misconfiguration (blank connection string, missing dialect) fails at startup through `IValidateOptions`, and an unsupported messaging flag throws at registration | Settled | `AGENTS.md` section 3 (ROP); ADR-062; the ADO stores' error handling |
| C6 | `EncinaOdbcOptions.ConnectionString` is `[JsonIgnore]` and `ToString()` prints no secret; no connection string appears in a log or message | Settled | `AGENTS.md` section 3 (#851) |
| C7 | No pooling option: ODBC pooling is a driver-manager setting (an ODBC administrator setting on Windows, `Pooling` in `odbcinst.ini` for unixODBC); the guide says so | Settled | `System.Data.Odbc` documentation (the issue's References) |
| C8 | Concurrency parity: like every ADO provider the stores take no claim lock; delivery is at-least-once and the inbox deduplicates, so no portable `SKIP LOCKED` is emulated | Settled | `OutboxStoreADO.GetPendingMessagesAsync` (plain `SELECT`); `AGENTS.md` section 3 (outbox: at-least-once) |
| C9 | Names: `{Pattern}Store{Provider}` with the provider written `ODBC` (`OutboxStoreODBC`), as `OutboxStoreADO` | Settled | `AGENTS.md` section 4 |
| C10 | EventIds: a block `20000-20049` named `ODBC` (area Database, first 50-id slot of the 20000 block), used by the health check and the registration log only (three events); claimed here, registered in pull request O5 | Settled | `AGENTS.md` section 7; `docs/engineering/eventid-block-registry.md` |
| C11 | No tenant-qualified id is built and nothing is keyed by tenant until #737, #738, #739 and #747 add the columns; the reserved `default` tenant segment does not apply here | Settled | ADR-054 D4 |

---

## Implementation Phases

### Delivery order and size

Seven pull requests; each builds and passes alone with its own tests (counts include csproj, `Directory.Packages.props`, `Encina.slnx`, PublicAPI, SQL scripts, manifests, test-project references and changelog). `AddEncinaOdbc` appears only in O5, because `AddMessagingServices` needs all four stores and the processor; O2 to O4 add public store classes that nothing registers yet.

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| O1 | 1 | Package, dialect interface, `AnsiOdbcDialect`, `OdbcSqlTemplate` | 14 |
| O2 | 2 | Outbox and inbox stores, processor, scripts | 14 |
| O3 | 3 | Saga and scheduling stores, scripts | 13 |
| O4 | 4 | Dead letter store, script | 8 |
| O5 | 5 | Options, registration, health check, logging, EventIds | 14 |
| O6 | 6 | Integration, contract and property tests on PostgreSQL | 13 |
| O7 | 7 | README, guide, ADR, INVENTORY, ROADMAP, changelog | 6 |

### Phase 1: Package, Dialect Seam and Template Rewrite (pull request O1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.ODBC/Encina.ODBC.csproj` (references `Encina`, `Encina.Messaging`; `System.Data.Odbc`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `InternalsVisibleTo` for UnitTests, GuardTests, IntegrationTests, ContractTests, PropertyTests), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; **modify** `Directory.Packages.props` (`System.Data.Odbc`), `Encina.slnx`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj`.
2. **Create** `IOdbcSqlDialect.cs` (members of Design Choice 2, XML docs with an example of a custom dialect), `AnsiOdbcDialect.cs` (`public class`, so a test or application dialect can derive: `FETCH FIRST n ROWS ONLY`, `CHAR(36)`, `TIMESTAMP`, `CLOB`, `INTEGER`, `SMALLINT`, health query `SELECT 1`) and `OdbcSqlTemplate.cs` (`internal static`; `Rewrite(string sql, IReadOnlyDictionary<string, object?> values)` returns the `?` SQL and the ordered `OdbcParameter` list; unknown marker is `ArgumentException`).
3. `.github/coverage-manifest/Encina.ODBC.json` and the PublicAPI lines through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/ODBC/OdbcSqlTemplateTests.cs` (order, repeated marker, literal `'@x'`, unknown marker, no markers), `AnsiOdbcDialectTests.cs` (each member's text), new `tests/Encina.GuardTests/ODBC/OdbcSqlTemplateGuardTests.cs` (null sql, null values; a derived dialect with null arguments).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #56, plan docs/plans/odbc-provider-implementation-plan-56.md, phase 1.
New provider Encina.ODBC over System.Data.Odbc; ODBC parameters are positional (?), so store SQL uses @Name markers that a template rewrites.

TASK:
1. Create the Encina.ODBC project, add it to Encina.slnx and Directory.Packages.props, reference it from the two test csproj files.
2. IOdbcSqlDialect, AnsiOdbcDialect (public, derivable) and OdbcSqlTemplate.
3. Manifest and PublicAPI through mechanical-fixer; unit and guard tests.

KEY RULES:
- No exceptions for business logic; template misuse is an ArgumentException; markers inside string literals are ignored.
- XML docs with examples on every public member; zero warnings; no Oracle/SQLite/Access code.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Encina.ADO.SqlServer.csproj, src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs
- src/Encina.Messaging/SqlIdentifierValidator.cs
```

</details>

---

### Phase 2: Outbox and Inbox Stores (pull request O2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.ODBC/Outbox/`: `OutboxMessage.cs` (`IOutboxMessage`, same members as `src/Encina.ADO.SqlServer/Outbox/OutboxMessage.cs`), `OutboxMessageFactory.cs`, `OutboxStoreODBC.cs` (`IOutboxStore`; constructor `(IDbConnection connection, IOdbcSqlDialect dialect, string tableName = "OutboxMessages", TimeProvider? timeProvider = null, IDbTransactionAccessor? transactionAccessor = null)`; all nine methods over `DbLease` and `OdbcSqlTemplate`; `GetPendingMessagesAsync` uses the dialect's limit clause and `ORDER BY CreatedAtUtc`; requeue batches stay under 1000 ids as in ADO), `OutboxProcessor.cs` (binds `OutboxProcessorBase`); in `src/Encina.ODBC/Inbox/`: `InboxMessage.cs`, `InboxMessageFactory.cs`, `InboxStoreODBC.cs` (`IInboxStore`, eight methods).
2. **Create** `src/Encina.ODBC/Scripts/001_CreateOutboxMessagesTable.sql` and `002_CreateInboxMessagesTable.sql` (ANSI type names, primary key, the processed and retry index).
3. **Modify** `PublicAPI.Unshipped.txt`; the manifest through `mechanical-fixer`.
4. Tests: new `tests/Encina.UnitTests/ODBC/OutboxStoreODBCTests.cs` and `InboxStoreODBCTests.cs` (the SQL text and the ordered parameters each method sends, through a recording `IDbConnection` substitute; exhausted and requeue paths; cancellation passed through), new `tests/Encina.GuardTests/ODBC/OdbcStoresGuardTests.cs` (null connection, null dialect, blank and unsafe table names, batch size zero, negative retries).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #56 plan docs/plans/odbc-provider-implementation-plan-56.md, phase 2. O1 merged.

TASK:
1. OutboxStoreODBC, InboxStoreODBC, their messages, factories, OutboxProcessor and the two SQL scripts.
2. PublicAPI and manifest (mechanical-fixer); unit and guard tests.

KEY RULES:
- Mirror the ADO.NET stores method for method; every database call is async with a CancellationToken; time through TimeProvider.
- All SQL goes through OdbcSqlTemplate and the dialect; table names through SqlIdentifierValidator; no claim lock (at-least-once).
- Return Either<EncinaError, T> through EitherHelpers.TryAsync; never swallow a failure.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/*.cs, Inbox/*.cs, Scripts/001_CreateOutboxMessagesTable.sql
- src/Encina.Messaging/Outbox/IOutboxStore.cs, Inbox/IInboxStore.cs
```

</details>

---

### Phase 3: Saga and Scheduling Stores (pull request O3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.ODBC/Sagas/`: `SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreODBC.cs` (`ISagaStore`, six methods, `GetStuckSagasAsync` and `GetExpiredSagasAsync` with the dialect's limit clause); in `src/Encina.ODBC/Scheduling/`: `ScheduledMessage.cs`, `ScheduledMessageFactory.cs`, `ScheduledMessageStoreODBC.cs` (`IScheduledMessageStore`, seven methods, recurring reschedule).
2. **Create** `Scripts/003_CreateSagaStatesTable.sql` and `004_CreateScheduledMessagesTable.sql`; **modify** `PublicAPI.Unshipped.txt`; manifest through `mechanical-fixer`.
3. Tests: new `SagaStoreODBCTests.cs` and `ScheduledMessageStoreODBCTests.cs` (recorded SQL and parameters, `UpdatedAt` handling through `FakeTimeProvider`), edit `OdbcStoresGuardTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #56 plan docs/plans/odbc-provider-implementation-plan-56.md, phase 3. O1 and O2 merged.

TASK:
1. SagaStoreODBC and ScheduledMessageStoreODBC with their entities, factories and scripts.
2. PublicAPI and manifest (mechanical-fixer); unit and extended guard tests.

KEY RULES:
- Same rules as phase 2; saga and schedule timestamps are UTC with the AtUtc suffix; naming per AGENTS.md section 4.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sagas/*.cs, Scheduling/*.cs
- src/Encina.Messaging/Sagas/ISagaStore.cs, Scheduling/IScheduledMessageStore.cs
```

</details>

---

### Phase 4: Dead Letter Store (pull request O4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** in `src/Encina.ODBC/DeadLetter/`: `DeadLetterMessage.cs`, `DeadLetterMessageFactory.cs`, `DeadLetterStoreODBC.cs` (`IDeadLetterStore`, ten methods; `TryClaimForReplayAsync` is one conditional `UPDATE` whose affected-row count decides the claim, so two replayers never both win) and `Scripts/005_CreateDeadLetterMessagesTable.sql`.
2. **Modify** `PublicAPI.Unshipped.txt`; manifest through `mechanical-fixer`.
3. Tests: new `DeadLetterStoreODBCTests.cs`, edit `OdbcStoresGuardTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #56 plan docs/plans/odbc-provider-implementation-plan-56.md, phase 4. O1 to O3 merged.

TASK:
1. DeadLetterStoreODBC, its message, factory and script. PublicAPI and manifest (mechanical-fixer); unit and guard tests.

KEY RULES:
- The replay claim is a single conditional UPDATE decided by the affected-row count. Same rules as phase 2.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DeadLetter/*.cs, src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs
```

</details>

---

### Phase 5: Options, Registration, Health Check and Logging (pull request O5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `EncinaOdbcOptions.cs` (`ConnectionString` `[JsonIgnore]`, `Dialect`, table names, `ToString()` without the secret), `EncinaOdbcOptionsValidator.cs` (`internal sealed`, `IValidateOptions<EncinaOdbcOptions>`: blank connection string and missing dialect fail at startup), `ServiceCollectionExtensions.cs` (`AddEncinaOdbc(this IServiceCollection, string connectionString, IOdbcSqlDialect dialect, Action<MessagingConfiguration> configure)`: scoped `OdbcConnection` as `IDbConnection`, `AddMessagingServices<OutboxStoreODBC, ..., OutboxProcessor>`, `AddDeadLetterQueueServices<DeadLetterStoreODBC, DeadLetterMessageFactory>`, the health check; throws `InvalidOperationException` naming any unsupported enabled flag; `TryAdd*` so an application's own registration wins), `OdbcHealthCheck.cs` (`IEncinaHealthCheck`, `DefaultName` const, static `Tags`, resolves the connection from a created scope, runs the dialect's health query with `OpenAsync` and a token, reports the exception type only) and `OdbcLog.cs` (`[LoggerMessage]`, EventIds `20000-20002`: dialect configured, health check failed by exception type, unsupported option refused).
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs` (field `ODBC`), `docs/engineering/eventid-block-registry.md` (row to `registered`), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (map entry `["Encina.ODBC"]`), `src/Encina/PublicAPI.Unshipped.txt` and `src/Encina.ODBC/PublicAPI.Unshipped.txt`; manifest through `mechanical-fixer`.
3. Tests: new `tests/Encina.UnitTests/ODBC/AddEncinaOdbcRegistrationTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`; every `IOutboxStore`, `IInboxStore`, `ISagaStore`, `IScheduledMessageStore` and `IDeadLetterStore` resolves to the ODBC type; an application's own store registration wins in either order; an unsupported flag throws), `OdbcHealthCheckTests.cs` (healthy, failing connection, no exception message in the result), `tests/Encina.GuardTests/ODBC/AddEncinaOdbcGuardTests.cs` (null services, blank connection string, null dialect, null configure).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #56 plan docs/plans/odbc-provider-implementation-plan-56.md, phase 5. O1 to O4 merged.

TASK:
1. EncinaOdbcOptions (secret hidden), AddEncinaOdbc, OdbcHealthCheck and OdbcLog in the claimed block 20000-20049.
2. Register the range, the registry row and the allocation-test map; PublicAPI and manifest (mechanical-fixer).
3. Registration, health and guard tests.

KEY RULES:
- Registration completeness: every store, factory, processor and option the registration resolves is registered; a database store wins over an
  in-memory default in any order without overriding the application's own registration.
- The health check reports the exception type only; a connection string never reaches a log or result.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs, Health/SqlServerHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 6: Integration, Contract and Property Proof (pull request O6)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.IntegrationTests/ODBC/OdbcCollections.cs` (`[CollectionDefinition("ODBC-PostgreSQL")]` over the existing `PostgreSqlFixture`), `PostgreSqlOdbcTestDialect.cs`, and one integration class per store (`OutboxStoreODBCIntegrationTests.cs`, `InboxStoreODBCIntegrationTests.cs`, `SagaStoreODBCIntegrationTests.cs`, `ScheduledMessageStoreODBCIntegrationTests.cs`, `DeadLetterStoreODBCIntegrationTests.cs`), each `[Collection("ODBC-PostgreSQL")]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`, with `ClearAllDataAsync()` in `InitializeAsync()`: add, read, mark, retry, exhaust and requeue against the real driver, and the replay claim won by exactly one of two concurrent callers.
2. **Create** `tests/Encina.ContractTests/ODBC/OdbcStoreContractTests.cs` (the ODBC stores satisfy the same contract assertions as the ADO PostgreSQL stores for the shared interfaces) and `tests/Encina.PropertyTests/ODBC/OdbcSqlTemplatePropertyTests.cs` (FsCheck: the `?` count equals the marker count, parameter order equals marker order, literals are never rewritten).
3. **Modify** the `ProjectReference` entries of `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj`, `Encina.ContractTests.csproj` and `Encina.PropertyTests.csproj`; manifest through `mechanical-fixer`. Prerequisite owned by the orchestrator: the runner step that installs `unixodbc` and `odbc-postgresql` (workflow files are not part of any worker pull request).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #56 plan docs/plans/odbc-provider-implementation-plan-56.md, phase 6. O1 to O5 merged; the CI runner has unixODBC and psqlODBC.

TASK:
1. Integration tests for the five stores on PostgreSQL through psqlODBC in a shared ODBC-PostgreSQL collection.
2. A contract test against the shared store contracts and a FsCheck property test for OdbcSqlTemplate.

KEY RULES:
- Shared [Collection] fixtures only; never a per-class fixture, never IClassFixture, never dispose the fixture from a test.
- Tests execute real package code; no Thread.Sleep (FakeTimeProvider); the test dialect derives from AnsiOdbcDialect.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/Collections.cs (the `ADO-PostgreSQL` collection), tests/Encina.TestInfrastructure/Fixtures/PostgreSqlFixture.cs, docs/testing/integration-tests.md
```

</details>

---

### Phase 7: Documentation and Finalization (pull request O7)

<details>
<summary><strong>Tasks</strong></summary>

1. Through `docs-writer`: `src/Encina.ODBC/README.md`, `docs/features/odbc-provider.md` (setup, the dialect interface, an Access dialect example, the PostgreSQL test engine, pooling is a driver-manager setting, what is not supported), the ADR (ODBC as an eleventh provider outside the ten-provider matrix, messaging-only; the number is assigned when the ADR pull request opens), `docs/INVENTORY.md`, `ROADMAP.md` (the deferred table row for #56 becomes planned).
2. `changelog.d/56-odbc-provider.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every coverage flag reaches the targets below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #56 plan docs/plans/odbc-provider-implementation-plan-56.md, phase 7. All code merged.

TASK:
1. Ask docs-writer for the README, the guide, the ADR, and the INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.
2. Report measured per-file coverage against the plan.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers); public text never names a private client application.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, src/Encina.ADO.SqlServer/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| `System.Data.Odbc` documentation (the issue's References) | Positional parameters, driver-manager pooling | Template rewrite (C1) and no pooling option (C7) |
| `AGENTS.md` sections 3, 4, 5 | Async database calls; naming; provider coherence; ten-provider matrix | Store design; the eleventh-provider note |
| SPEC-000 DEC-009 | 1.0 scope widened to deferred items | Why #56 is planned |
| ADR-009, ADR-024 | Oracle and SQLite out of the matrix | Precedent for a provider outside the ten |
| ADR-021, ADR-054, ADR-062 | EventIds; telemetry rules; error categories | C5, C10, telemetry deferral |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IOutboxStore`, `IInboxStore`, `ISagaStore`, `IScheduledMessageStore`, `IDeadLetterStore` | `src/Encina.Messaging/` | The contracts the stores implement |
| `AddMessagingServices`, `AddDeadLetterQueueServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | Registration |
| `SqlIdentifierValidator`, `DbLease`, `IDbTransactionAccessor`, `StoreValidationMessages` | `src/Encina.Messaging/` | Shared store helpers |
| `EitherHelpers.TryAsync` | `src/Encina/Results/EitherHelpers.cs` | ROP wrapping |
| `OutboxStoreADO` and its siblings | `src/Encina.ADO.SqlServer/` | Method-by-method reference |
| `PostgreSqlFixture` and the `ADO-PostgreSQL` collection | `tests/Encina.TestInfrastructure/Fixtures/PostgreSqlFixture.cs`, `tests/Encina.IntegrationTests/ADO/Collections.cs` | Test engine |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.ODBC` | claim `20000-20049` named `ODBC` (area Database, first 50-id slot of the 20000 block) | Reserved by this plan; O5 registers it; packed from 20000; three events used |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| O1 | Phase 1 | 6 (csproj, 2 PublicAPI, dialect interface, Ansi dialect, template) | 0 | 3 | 2 (`Encina.UnitTests.csproj`, `Encina.GuardTests.csproj`) | 1 | 2 (`Directory.Packages.props`, `Encina.slnx`) | 14 | ~700 |
| O2 | Phase 2 | 7 (outbox 4, inbox 3) + 2 SQL = 9 | 1 (`PublicAPI.Unshipped.txt`) | 3 | 0 | 1 | 0 | 14 | ~1,500 |
| O3 | Phase 3 | 6 + 2 SQL = 8 | 1 | 2 | 1 (`OdbcStoresGuardTests.cs`) | 1 | 0 | 13 | ~1,100 |
| O4 | Phase 4 | 3 + 1 SQL = 4 | 1 | 1 | 1 (`OdbcStoresGuardTests.cs`) | 1 | 0 | 8 | ~650 |
| O5 | Phase 5 | 5 (options, options validator, registration, health check, log) | 3 (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt` of `Encina`, `PublicAPI.Unshipped.txt` of `Encina.ODBC`) | 3 | 1 (allocation test) | 1 | 1 (registry doc) | 14 | ~900 |
| O6 | Phase 6 | 0 | 0 | 9 (collection, dialect, 5 integration, contract, property) | 3 (the three test csproj files) | 1 | 0 | 13 | ~1,300 |
| O7 | Phase 7 | 0 | 0 | 0 | 0 | 0 | 6 (README, guide, ADR, `INVENTORY.md`, `ROADMAP.md`, changelog) | 6 | ~400 |
| **Total** | | **32** | **6** | **21** | **8** | **6** | **9** | **82** | **~6,550** |

Columns sum: 32 + 6 + 21 + 8 + 6 + 9 = 82 = 14 + 14 + 13 + 8 + 14 + 13 + 6. New src counts SQL scripts and the project's own PublicAPI files. Test doubles and implementers: no existing interface changes; every `IOutboxStore`-family interface gains one more implementer, but the stores of the other providers are untouched. When #737, #738, #739 and #747 add `TenantId` and `ModuleId` to the message interfaces they must list the ODBC classes as implementers (an interface change lands implementers first).

Per-file coverage targets (the package manifest does not exist yet; O1 creates `Encina.ODBC.json`; targets below are per file and demanding for the flags that can reach the code):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `OdbcSqlTemplate.cs` | 95 | 60 | 0 | 90 | 30 | Rewrite branches are table-driven and property tested; the integration run sends the rewritten SQL to a real driver |
| `AnsiOdbcDialect.cs` | 95 | 40 | 0 | 0 | 40 | Every member's text is asserted; the integration dialect derives from it and runs |
| `Outbox/OutboxStoreODBC.cs` | 55 | 50 | 40 | 0 | 80 | Unit tests assert SQL text and parameters through a recording connection; guard covers constructor and argument checks; real behavior is proven on PostgreSQL |
| `Inbox/InboxStoreODBC.cs`, `Sagas/SagaStoreODBC.cs`, `Scheduling/ScheduledMessageStoreODBC.cs` | 55 | 50 | 40 | 0 | 80 | Same shape as the outbox store, per file |
| `DeadLetter/DeadLetterStoreODBC.cs` | 55 | 50 | 40 | 0 | 80 | Same shape; the concurrent replay claim is an integration case |
| message and factory classes (10 files) | 80 | 0 | 0 | 0 | 60 | Plain data and `new` factories reached by the store tests; no validated parameter |
| `OutboxProcessor.cs` | 60 | 60 | 0 | 0 | 0 | Binds the base class; the constructor checks are guarded; the base class owns the behavior |
| `ServiceCollectionExtensions.cs` | 90 | 70 | 0 | 0 | 0 | Registration, order independence and the unsupported-flag error are DI and guard tests |
| `OdbcHealthCheck.cs` | 85 | 50 | 0 | 0 | 50 | Healthy and failing paths are unit tested; the integration run uses the real driver |
| `EncinaOdbcOptions.cs` | 85 | 50 | 0 | 0 | 0 | Secret hiding and validator branches are unit tested |
| `OdbcLog.cs` | 70 | 0 | 0 | 0 | 0 | Source-generated partial methods reached by the health and registration tests; no validated parameter |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #56 - the Encina.ODBC messaging provider over System.Data.Odbc - in seven small pull requests (O1 to O7),
in order. O6 needs a runner with unixODBC and psqlODBC installed by the orchestrator (workflow files are not yours).

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI, manifests and
  changelog through mechanical-fixer; README, guide and ADR through docs-writer.
- ODBC parameters are positional: store SQL uses @Name markers and OdbcSqlTemplate rewrites them. Engine differences live in IOdbcSqlDialect.
- Messaging stores only (outbox, inbox, saga, scheduling, dead letter). AddEncinaOdbc exists only from O5 (AddMessagingServices is all-or-nothing).

IMPLEMENTATION OVERVIEW:
1. O1: package, IOdbcSqlDialect, AnsiOdbcDialect, OdbcSqlTemplate. 2. O2: outbox and inbox. 3. O3: saga and scheduling.
4. O4: dead letter. 5. O5: options, registration, health check, EventIds 20000-20049. 6. O6: PostgreSQL proof. 7. O7: docs.

KEY PATTERNS:
- Mirror the ADO.NET stores; async with CancellationToken; TimeProvider; Either<EncinaError, T> through EitherHelpers.TryAsync.
- Table names through SqlIdentifierValidator; the connection string is [JsonIgnore] and hidden in ToString.
- Integration tests use the shared ODBC-PostgreSQL collection, ClearAllDataAsync in InitializeAsync.
- Guard tests for every public constructor and method parameter; a null optional parameter gets a valid-result test.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ (Outbox, Inbox, Sagas, Scheduling, DeadLetter, ServiceCollectionExtensions.cs, Health/)
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The stores are write-mostly queues and no other provider caches them (the issue's "cache-aside for store reads" has no consumer) |
| 2 | OpenTelemetry | ⏭️ | Deferred to a follow-up (issue file `artifacts/issues/odbc-telemetry.md`): the database providers' telemetry follows #721's approach, and ODBC joins it once that design is implemented |
| 3 | Structured Logging | ✅ Phase 5 | `[LoggerMessage]` in `20000-20049`: dialect configured, health failure by exception type, unsupported option |
| 4 | Health Checks | ✅ Phase 5 | `OdbcHealthCheck` runs the dialect's health query |
| 5 | Validation | ✅ Phase 5 | `IValidateOptions` for the connection string and dialect; `SqlIdentifierValidator` for table names |
| 6 | Resilience | ❌ N/A | Retry and backoff are in the processors (`OutboxRetryBackoff`) for every provider; the stores add no retry |
| 7 | Distributed Locks | ❌ N/A | No provider's stores take locks (register row C8); leader election is #717 |
| 8 | Transactions | ✅ Phases 2-4 | Stores join the scope's shared transaction through `DbLease` and `IDbTransactionAccessor`, like ADO.NET; the stores never call commit |
| 9 | Idempotency | ✅ Phase 2 | `InboxStoreODBC` is the deduplication store |
| 10 | Multi-Tenancy | ⏭️ | The columns come from #737, #738 and #739; the interface change lists ODBC among the implementers; no tenant-qualified id is built here (C11) |
| 11 | Module Isolation | ⏭️ | `ModuleId` on messaging rows is #747; the same implementer note applies |
| 12 | Audit Trail | ❌ N/A | The audit stores are outside this provider's scope (Design Choice 1); an ODBC audit store needs its own plan |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Runner step installing `unixodbc` and `odbc-postgresql` | Orchestrator task (workflow files) | O6 only; O1 to O5 run without a driver |
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant and module columns on messaging rows | #737, #738, #739, #747 | ODBC joins as an implementer |
| ADO telemetry design | #721 | ODBC telemetry follows it (issue file `artifacts/issues/odbc-telemetry.md`) |
| Amend `AGENTS.md` section 5 to name ODBC as a documented eleventh provider outside the matrix | Maintainer decision | This plan never edits `AGENTS.md` |

---

## Next Steps

1. Link this plan from #56 and ask the maintainer to answer Design Choices 1 to 4 when work starts; the orchestrator schedules the runner step for O6.
2. Implement O1 to O7 in order (O6 needs the runner step; O7 can follow O5).
3. Maintainer action: amend the acceptance criteria of #56 that name the `SqlDialect` enum with `Access` and `Custom`, `config.ConnectionString` on the messaging configuration and "connection pooling" to the outcome of this plan, and update the `status-deferred` label, since DEC-009 supersedes the post-1.0 comment (this plan never comments on an issue).
