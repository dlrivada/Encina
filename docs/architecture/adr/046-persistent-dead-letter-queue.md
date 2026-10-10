---
title: "ADR-046: Persistent Dead Letter Queue"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-046: Persistent Dead Letter Queue: Oldest-First Contract, Idempotent Capture by Source Message, Tenant Column

## Status

**Accepted** - decided by the maintainer on 2026-10-09 (Design Choices 1, 5, 6 and 7 and decisions B3, B7 and B9 of the [plan for #583](../../plans/dead-letter-stores-implementation-plan-583.md#maintainer-decisions)), plus four decisions added the same day while the stores were being finished (listed under "Decisions added during implementation"), and four more after the stores were finished (listed under "Decisions added after the stores were finished"), and five on 2026-10-10 with the source capture of #1991 (decisions 10 to 14).

## Context

Before #583, `IDeadLetterStore` was a contract with one implementation, an in-memory test fake. A production application could not keep dead letters across a restart. SPEC-000 DEC-008 (decided 2026-10-07) brings the dead letter queue (DLQ) into 1.0 complete, on the 10 providers of the database matrix: ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, plus MongoDB (AGENTS.md section 5).

Persisting the queue makes the shape of the record and the behavior of the store expensive to change later, because every change after 1.0 is a migration on 10 providers. The contract as it stood had five defects that a persistent store turns from test-only into production problems:

- the record carried an `ErrorMessage` that held an error code, and an `ExceptionMessage` that was never written because exception text can hold personal data;
- the order of `GetMessagesAsync` was undefined, and the orchestrator and the fake assumed opposite orders;
- statistics and bulk delete loaded the whole queue into memory and turned a store `Left` into "nothing to delete";
- nothing identified the source message, so a source that retries (SPEC-002 REQ-017 keeps dead-lettered outbox rows in the outbox table and lets them be requeued) could create two dead letters for one message;
- the record had no tenant, although SPEC-000 section 2 states that retrofitting a tenant key into persisted shapes after 1.0 would break the API and the schema.

## Decision

### The record is reshaped before it is persisted (plan Choice 1)

`IDeadLetterMessage`, `DeadLetterData` and the columns use `ErrorCode` (was `ErrorMessage`), drop `ExceptionMessage`, and add `SourceMessageId` (required string) and `TenantId` (optional string). A column that must always stay empty is an invitation to fill it, and a column named after a message that holds a code misleads; both are removed while renames are free. `ActorId` and `CausationId` are not added here; #2013 adds them.

### The store contract defines one order and set-based operations (plan Choice 5)

- `GetMessagesAsync` returns the oldest first, ordered by `DeadLetteredAtUtc` and then `Id`, and has a `newestFirst` flag. The `DeadLetteredAtUtc` order is part of the cross-provider contract; the `Id` tie-break is not (see below).
- `DeleteManyAsync(DeadLetterFilter)` deletes by filter in one statement and returns the count. `DeleteAllAsync(DeadLetterFilter.All)` on the manager deletes the whole queue and has no confirmation parameter: the caller owns that decision (B9).
- `DeadLetterFilter.ExpiresAtOrBeforeUtc` lets the orchestrator count expired rows in the database instead of loading them.
- `MarkAsReplayedAsync` returns `Either<EncinaError, bool>` and updates only while `ReplayedAtUtc IS NULL`. `TryClaimForReplayAsync` claims a message atomically before dispatch (`ReplayClaimedAtUtc`, expiring after `DeadLetterOptions.ReplayClaimTimeout`), so two hosts cannot replay one message at once (B3).
- Expiry is `ExpiresAtUtc <= now` on every store, with "now" from the store's `TimeProvider`; the record exposes `IsExpiredAt(DateTime utcNow)` instead of a property that reads the clock (plan Choice 2).
- Every store `Left` fails the operation that received it. The replay outcome stored is an outcome code, never error text.

### Capture is idempotent by source message (plan Choice 6)

A unique index on `(SourcePattern, SourceMessageId)` exists from table creation, and `AddAsync` returns `Either<EncinaError, bool>`: `true` when stored, `false` when that source message is already dead-lettered (not an error). `DeadLetterContext.SourceMessageId` is the idempotency key. When the caller omits it, `DeadLetterOrchestrator` uses the new dead letter id, so the capture is unique and not deduplicated. At the store, the key is required: every store and the fake store throw `ArgumentException` for an empty one, and the fake no longer substitutes the message id. The key has no tenant, so `SourceMessageId` must be unique per `SourcePattern` across tenants; if two tenants dead-letter the same source id, the second capture returns the first tenant's message. The built-in sources use globally unique ids (GUIDs, inbox message ids). On PostgreSQL the ADO.NET and Dapper stores insert with `ON CONFLICT ("SourcePattern", "SourceMessageId") DO NOTHING`, so a duplicate raises no server error and is safe inside an open transaction. A duplicate capture returns the existing dead letter and does not invoke `DeadLetterOptions.OnDeadLetter`. `SourceMessageId` is a string because the sources use different identifiers (`Guid` for outbox, scheduled messages and sagas, `string` for inbox message ids). On MongoDB without the unique index, and on an EF Core lost race, a duplicate is not yet reported as `Right(false)`; this is tracked in [#2079](https://github.com/dlrivada/Encina/issues/2079).

### The tenant is a column stamped at capture (plan Choice 7)

`TenantId` is stamped from `IRequestContext.TenantId` at capture (`DeadLetterContext.TenantId` may override it, for sources that restore a persisted tenant). The store contract is explicit: a store returns every tenant unless `DeadLetterFilter.TenantId` names one. `DeadLetterManager` reads, replays and deletes default to the ambient tenant when there is one, with `DeadLetterFilter.AllTenants` as the explicit opt-out for operator tooling (B7, SPEC-002 REQ-061). The cleanup processor and the health check stay deployment-wide. Telemetry never carries the tenant id (SPEC-002 REQ-062).

### Decisions added during implementation (2026-10-09)

1. **Binary collation on the six filter-key columns on SQL Server and MySQL.** `RequestType`, `ErrorCode`, `CorrelationId`, `SourcePattern`, `SourceMessageId` and `TenantId` use `Latin1_General_100_BIN2` on SQL Server and `utf8mb4_bin` on MySQL, so the unique source key and every filter compare case-sensitively, as PostgreSQL and MongoDB do by default. This is provider coherence (AGENTS.md section 5): switching the DI registration must not change which messages count as duplicates.
2. **`DeadLetterMessageConfiguration` requires the collation argument.** There is no parameterless constructor and no compatibility overload; the application passes `DeadLetterMessageConfiguration.SqlServerBinaryCollation`, `DeadLetterMessageConfiguration.MySqlBinaryCollation` or an explicit `null` on PostgreSQL. A default would give SQL Server and MySQL models a case-insensitive schema without anyone choosing it. This follows the pre-1.0 rule: no compatibility layer (AGENTS.md section 1).
3. **The stores require a `DbConnection`.** The ADO.NET and Dapper stores take an `IDbConnection` but reject one that does not derive from `DbConnection`, so every call is asynchronous with a `CancellationToken` and no synchronous fallback exists (AGENTS.md section 3).
4. **The `Id` tie-break is not part of the cross-provider contract.** Providers compare GUIDs in different byte orders, so rows with the same `DeadLetteredAtUtc` can come in a different order on another provider. The tie-break is stable within one provider, which is what paging within a provider needs; consumers must not depend on it across providers.

### Decisions added after the stores were finished (2026-10-09)

These continue the numbering above. Decision 5 refines the tenant paragraph above: the store is unchanged, and the manager now denies when tenancy is in use and no tenant is resolved.

- **Decision 5: tenancy fails closed in `DeadLetterManager`.** When `AddEncinaTenancy` is registered (it registers the `TenancyInUse` marker) and no tenant is resolved, `GetMessagesAsync`, `GetCountAsync`, `ReplayAllAsync` and `DeleteAllAsync` are denied unless the filter sets `AllTenants = true` or names a `TenantId` (an empty one counts as none). Operations by message id and `GetStatisticsAsync` have no filter to opt out with and are denied too; whether they need an opt-out is an open question tracked in [#2080](https://github.com/dlrivada/Encina/issues/2080). `CleanupExpiredAsync` is retention maintenance and is not gated. The denial is the code `encina.authorization.dlq_tenant_required`, which `Encina.AspNetCore` maps to 403. Every `AllTenants` use under tenancy is logged (EventId 2993) and every denial (2994), by operation and error code only. Without `AddEncinaTenancy` behavior is unchanged. This follows the fail-closed rule for compliance gates (AGENTS.md section 3, SPEC-002 DEC-006) and refines the earlier decision to leave the store open: the store still returns every tenant; the gate lives in the manager, so the cleanup processor and health check keep working without a tenant.
- **Decision 6: input rules are enforced once, in the orchestrator and manager.** Instants (`FirstFailedAtUtc`, `FirstAttemptAtUtc`, the filter instants) must be `DateTimeKind.Utc` and are rejected, not converted, because an `Unspecified` value has no defined instant and `ToUniversalTime` would guess the server's zone while providers disagreed on what they stored. `SourceMessageId`, `SourcePattern`, `RequestType` and `TenantId` must not start or end with white space, because SQL Server and MySQL ignore trailing spaces in comparisons and PostgreSQL and MongoDB do not. Stores return instants with `Kind.Utc` (a UTC value converter on EF Core).
- **Decision 7: a store error aborts `ReplayAllAsync`.** A `Left` from the store for one message fails the whole operation with that error (earlier replays stay recorded; EventId 2995). Per-message problems remain per-message failed results; a store exception propagates; a handler's own `OperationCanceledException` is a failed replay and only the caller's cancellation propagates.
- **Decision 8: test support.** `AddFakeDeadLetterStore` takes the container's `TimeProvider` (`TimeProvider.System` otherwise), and the SQL Server `029` script has an integration test like the PostgreSQL and MySQL scripts. The store contract also runs the expiry boundary under a non-UTC PostgreSQL session time zone (`SET TIME ZONE 'America/Los_Angeles'`) for ADO.NET, Dapper and EF Core (maintainer decision B4), so a `TIMESTAMPTZ` comparison cannot depend on the session zone.
- **Decision 9: options registration is first-wins.** `AddDeadLetterQueueServices` registers `DeadLetterOptions` with the same first-wins semantics as the store and the factory. A later call, for example `AddEncinaDeadLetterQueue` after a provider registered the queue with `UseDeadLetterQueue`, keeps the first options, so the three registrations cannot disagree. Configure the queue in the first call.

### Decisions added with the source capture (#1991, 2026-10-10)

- **Decision 10: the five sources are wired.** The maintainer chose option (a) on 2026-10-07: the `IntegrateWith*` flags of `DeadLetterOptions` do what they promise, instead of being removed.
- **Decision 11: sources capture through `DeadLetterSourceCapture`, each capture in a scope of its own.** The queue registration adds it and the sources take it as an optional dependency. On EF Core the store shares the scoped `DbContext`, so a capture in the source's scope would save the source's tracked changes or be broken by them.
- **Decision 12: stored content is kept as is.** `DeadLetterOrchestrator.AddSerializedAsync` stores the type name and content a source already holds (outbox, scheduling, sagas, undeliverable delayed retries) without serializing them again.
- **Decision 13: outbox, scheduling and delayed retries capture before they record their terminal state.** A failed capture leaves the message un-exhausted (outbox), due for a later cycle (scheduling) or the row pending (delayed retries, where `OnPermanentFailure` also waits for a successful capture), so a message is never reported as dead-lettered without a dead letter.
- **Decision 14: the inbox terminal point is the attempt that uses up `InboxOptions.MaxRetries`.** A handler `Left` is a cached business result and never retries; only a thrown exception counts as an attempt.

## Alternatives rejected

- **Persist the interface as it was, plus `TenantId`** (Choice 1, B): 10 schemas would carry a misnamed column and a column that must stay empty forever, and nothing would identify the source message.
- **The issue's snake_case schema** (Choice 1, C): it does not match `IDeadLetterMessage` and breaks the PascalCase convention of every Encina table.
- **Fix only the orchestrator to read in the fake's order** (Choice 5, B): the order stays outside the contract, so 10 stores could each pick one.
- **A store-side grouped `GetStatisticsAsync`** (Choice 5, C): a grouped query per dialect and a MongoDB aggregation for an operator screen; the indexed counts of option A are enough.
- **Defer the source key to #1991** (Choice 6, B): the 10 schemas would change twice before 1.0.
- **No deduplication** (Choice 6, C): duplicate dead letters on every retry of a source that keeps its row, and a replay of both dispatches twice.
- **Implicit tenant scoping inside the store, failing closed** (Choice 7, B): the DLQ is an operator tool and the cleanup processor has no tenant, so every background call would need an opt-out. Decision 5 puts the fail-closed gate in the manager instead.
- **Converting non-UTC instants** (decision 6): `Unspecified` has no defined instant, so any conversion is a guess.
- **No tenant column now** (Choice 7, C): exactly the post-1.0 retrofit SPEC-000 section 2 warns against.
- **A default collation (or a compatibility overload) on `DeadLetterMessageConfiguration`**: see decision 2 above.

## Consequences

- **Positive**: the queue survives restarts on all 10 providers; `OnDeadLetter` runs once per source message; statistics, cleanup and bulk delete are set-based; #1991 (wiring the five `IntegrateWith*` sources, delivered as decisions 10 to 14) and the post-1.0 store families #584-#589 implement a fixed contract, and the record needs no schema change for them.
- **Negative**: every store implements two more predicates and three more methods than before; EF Core detects duplicates with a query before tracking, so a race between two hosts surfaces as a unique-violation `Left`; applications on SQL Server and MySQL must pass the collation when they apply the EF configuration; under `AddEncinaTenancy`, a manager call with no resolved tenant is denied (403) unless the filter opts out (decision 5).
- **Neutral**: the tie-break order differs between providers; the older messaging tables keep their unquoted PostgreSQL identifiers, while the DLQ uses quoted PascalCase so ADO.NET, Dapper and EF Core share one table (plan Choice 9).

## Related

- [How to enable the persistent dead letter queue](../../features/dead-letter-queue.md)
- [Implementation plan for #583](../../plans/dead-letter-stores-implementation-plan-583.md), including its [Maintainer Decisions](../../plans/dead-letter-stores-implementation-plan-583.md#maintainer-decisions)
- [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md), section 2 and DEC-008; [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md), REQ-017, REQ-061 and REQ-062
- [ADR-001](001-railway-oriented-programming.md) and [ADR-006](006-pure-rop-exception-handling.md) (`Either` contracts)
- [ADR-018](018-cross-cutting-integration-principle.md) (cross-cutting functions evaluated in the plan)
- [ADR-029](029-recoverability-error-classification.md) (permanent failures reach the dead letter queue)
- Issues: #583 (this work), #2012 (replay claim and plaintext outcome), #1991 (wiring the sources), #2013 (replay under the stored tenant, operator audit), #584-#589 (further store families, post-1.0)

## Date

2026-10-09
