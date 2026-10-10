---
title: "ABAC Decision Audit Reference"
layout: default
parent: "Features"
---

# ABAC Decision Audit Reference

This page is for developers who enable the ABAC decision audit trail and need to look up what is recorded, which limits apply, how the recorder and the reader behave and which errors they return. It describes the trail as it exists today (pre-1.0); the design is in the [implementation plan for #751](../../../plans/abac-decision-audit-implementation-plan-751.md).

## Overview

The Policy Enforcement Point (PEP) can write one audit entry for every decision it enforces. The entry is an `OperationAuditEntry` stored through the application's `IOperationAuditStore` ([ADR-036](../../../architecture/adr/036-three-audit-stores.md)), so no new store, table or provider is introduced. The audit is opt-in and off by default (`ABACDecisionAuditOptions.Enabled`); when it is off the PEP builds no record, reads no clock and calls no recorder. The regulatory background is [SPEC-002](../../../specifications/SPEC-002-eu-regulatory-readiness.md).

## Entry shape

| Entry field | Value |
|-------------|-------|
| `Id` | The decision id (`ABACDecisionRecord.DecisionId`). A retried write is idempotent. |
| `Action` | `ABACDecision` (`ABACDecisionAuditSchema.Action`). |
| `EntityType` | The request type name. |
| `EntityId` | The declared resource id: `IABACResourceIdentity.ResourceId`, or the resource attribute named by `ResourceIdAttributeName` (default `resourceId`). Empty when the request declares neither. |
| `UserId` | The caller. `service:<name>` for a service identity; `null` for an unauthenticated caller. |
| `TenantId` | The tenant of the request, when it carries one. |
| `CorrelationId` | The correlation id of the request. |
| `Outcome` | See [Outcome mapping](#outcome-mapping). |
| `ErrorMessage` | The reason code only (an `ABACErrors` code, or `abac.permit`). |
| `TimestampUtc`, `CompletedAtUtc` | The single clock read at the end of the decision. |
| `StartedAtUtc` | When the decision started. |
| `IpAddress`, `UserAgent` | From the request context, subject to the [column-limit rule](#column-limit-rule). |
| `Metadata` | String values under `abac.*` keys (below). |

### Metadata keys

Every metadata value is a string, so relational JSON columns, MongoDB and Marten encryption round-trip them identically. The keys are the constants of `ABACDecisionAuditSchema`.

| Key | Value |
|-----|-------|
| `abac.schema` | `encina.abac.decision/1` |
| `abac.stage` | `pep` |
| `abac.enforced` | `true`, or `false` for a would-deny verdict that Warn mode let through |
| `abac.enforcement_mode` | The `ABACEnforcementMode` in force |
| `abac.identity_kind` | The `IdentityKind` of the caller |
| `abac.effect` | The effect the PDP answered (when an evaluation was reached) |
| `abac.policy_id` | The deciding policy id (`condition:<index>` for an unmet condition) |
| `abac.rule_id` | The representative decisive rule id |
| `abac.module_id` | The module id (there is no queryable module column); hashed beyond 256 characters |
| `abac.trace` | The evaluation trace as JSON (`IncludeEvaluationTrace`, at most `MaxTraceEntries` nodes) |
| `abac.trace_truncated` | `true` when the trace is incomplete |
| `abac.obligations`, `abac.advice` | The obligation or advice ids, each a JSON array of strings (ids chosen by policy authors may contain any character). At most `ABACDecisionAuditSchema.MaxListedIds` (64) ids are kept per key; an id beyond 256 characters is stored as its `sha256:<64 hex>` hash |
| `abac.attribute_names` | The attribute names by category, as JSON |
| `abac.attr.<name>` | The value of an attribute listed in `RecordedAttributeValues` |
| `abac.started_at_utc`, `abac.completed_at_utc` | Round-trip (`O`) timestamps of the decision |
| `abac.hashed_fields`, `abac.dropped_fields`, `abac.truncated_fields` | The names of the fields the [column-limit rule](#column-limit-rule) changed, each marker a JSON array of strings (a name containing a comma stays one entry) |

### Outcome mapping

| Enforced outcome | Reason code | Stored `Outcome` | Notes |
|------------------|-------------|------------------|-------|
| Granted | `abac.permit` | `Success` | |
| Definite denial in Warn mode (would-deny) | the denial code | `Success` | `abac.enforced` is `false` |
| Denied | the denial code | `Denied` | |
| Denied | `abac.indeterminate` or `abac.evaluation_failed` | `Error` | |

`ABACDecisionAuditOptions.Outcomes` selects which of Granted, Denied and NotEnforced are recorded (default all). A filter never hides an error: an indeterminate decision, an evaluation failure and an unauthenticated caller are recorded whenever the audit is enabled.

### Never stored

`EncinaError.Message`, `DecisionStatus.StatusMessage`, exception messages, and request or response payloads. Attribute values are stored only for the names in `RecordedAttributeValues` (empty by default, because values are personal data).

## Column-limit rule

A value never fails the insert. The mapper (`ABACDecisionAuditEntryMapper`) bounds each field to its column and names the fields it changed in the `abac.*_fields` markers.

| Field | Limit | When over the limit or invalid | Marker |
|-------|-------|--------------------------------|--------|
| `UserId`, `EntityType`, `EntityId`, `CorrelationId` | 256 | Replaced by `sha256:<64 hex>` (SHA-256 of the UTF-8 bytes, lowercase hex) | `abac.hashed_fields` |
| `TenantId` | 128 | Replaced by `sha256:<64 hex>` | `abac.hashed_fields` |
| `IpAddress` | 45 | Kept only as canonical IPv4 or IPv6 text; otherwise stored as `null` (longer than the limit, short forms such as `123`, zone ids such as `fe80::1%eth0`, anything that does not read back as itself) | `abac.dropped_fields` |
| `UserAgent` | 512 | Truncated | `abac.truncated_fields` |
| Attribute names (`abac.attribute_names`) | 128 names in total | The list is cut | `abac.truncated_fields` |
| Recorded attribute value (`abac.attr.<name>`) | 256 | Truncated | `abac.truncated_fields` |
| `abac.policy_id`, `abac.rule_id`, `abac.module_id` | 256 | Replaced by `sha256:<64 hex>` | `abac.hashed_fields` (named by the metadata key) |
| `abac.obligations`, `abac.advice` | 64 ids per key | The list is cut | `abac.truncated_fields` (names the key) |
| Each id in `abac.obligations`, `abac.advice` | 256 | Replaced by `sha256:<64 hex>` | `abac.hashed_fields` (names the key) |
| `ErrorMessage` (reason code) | 2048 | Replaced by `sha256:<64 hex>` | `abac.hashed_fields` |

The reader applies the same rule to its filters, so a subject, request type, resource or tenant stored as a hash is still found by its original value.

## Recorder

`AuditStoreABACDecisionRecorder` is the registered `IABACDecisionRecorder` (singleton, registered with `TryAdd`, so an application implementation registered first wins). The PEP awaits it before the protected handler runs.

| Behavior | Detail |
|----------|--------|
| Isolation | Each write runs in its own DI scope under `TransactionScope(Suppress)`, so a denied request that rolls back its unit of work keeps its record. |
| Cancellation | The client's token is never linked to the write: a disconnect must not erase the evidence of a denied attempt. |
| Bound | `ABACDecisionAuditOptions.WriteTimeout` (default 5 seconds, must be greater than zero, validated when the application starts). The write is also raced against the bound, so a store that ignores its token cannot hold the request. A store that observes the bound token and answers with a `Left` after the bound fired is treated as the same timeout as one that ignores the token. |
| Idempotent re-check | When the write returns `Left`, throws or times out, the recorder looks the entry up by correlation id and decision id from a fresh scope, under a second bound of the same length. A committed entry counts as written (logged with EventId 9089). A failed write therefore holds the request for at most twice `WriteTimeout`. When the look-up fails too, the write stays unconfirmed and is logged as Debug EventId 9099 (error codes and exception types only). |
| Abandoned write | A write abandoned at the timeout keeps running after the recorder returns and its scope is disposed. It may fail late (for example with `ObjectDisposedException` on a scoped `DbContext`; the fault is observed) or commit after the request was denied. The decision id identifies such a row. |
| Failure result | A store `Left` is returned with the store's code and a fixed message, never the store's message or exception. An exception is rethrown for the PEP to log in its redacted form. |
| No store | No `IOperationAuditStore` registered returns `abac.decision_audit_store_unavailable`. |

Marten caveat: the Marten store reads through an asynchronous projection, so the re-check may not see an entry that was written. The failure then stands (fail closed); the result is never a false success.

### Failure mode

`ABACDecisionAuditOptions.FailureMode` decides what the PEP does with a request that would proceed when its record cannot be written or built (a request whose resource id getter throws is an audit failure like a failed write).

| Value | Request that would proceed | Request denied anyway |
|-------|----------------------------|-----------------------|
| `FailClosed` (default) | Denied with `abac.decision_audit_failed` | Stays denied with its original error; the failure is logged |
| `BestEffort` | Proceeds; the failure is logged | Stays denied with its original error; the failure is logged |

## Reader

`IABACDecisionAuditReader` (registered scoped) reads the trail. It resolves `IOperationAuditStore` on each call, so it is registered whether or not a store exists; without one every call returns `abac.decision_audit_store_unavailable`.

| Method | Returns |
|--------|---------|
| `QueryAsync(ABACDecisionAuditQuery, CancellationToken)` | `Either<EncinaError, PagedResult<ABACDecisionAuditRecord>>`, newest first |
| `ExportAsync(ABACDecisionAuditQuery, Stream, CancellationToken)` | `Either<EncinaError, int>`: the number of decisions written as JSON Lines (UTF-8, one `ABACDecisionAuditRecord` per line, schema `encina.abac.decision/1`). The stream is not closed. |

A stored entry whose metadata cannot be read back makes `QueryAsync` and `ExportAsync` return a `Left` with `abac.decision_audit_record_unreadable` (logged with EventId 9098, exception type and stack trace only), never an exception.

`ExportAsync` ignores the paging of the query and reads pages of `OperationAuditQuery.MaxPageSize`. Pages are read newest first: set `ToUtc` to export a stable range while decisions are still being recorded, or a new decision can shift a page and repeat a line. A failure before the first line returns its own error and writes nothing. A failure after lines were written returns `abac.decision_audit_export_incomplete` (`ABACErrors.DecisionAuditExportIncompleteCode`), whose details give `linesWritten` and `cause` (the code of the failing page): the destination holds a partial file that must be discarded.

The operation audit stores order pages by timestamp only, so two decisions with the same timestamp can swap across a page boundary (one line repeated, another missing) until the stores add a unique tiebreaker (#2135). This sits beside the Marten limit in [Providers](#providers) and the live-write caveat above.

`reader` below is an injected `IABACDecisionAuditReader`; `AuditOutcome` is in `Encina.Security.Audit`, the query in `Encina.Security.ABAC.DecisionAudit`. Both calls return `Either<EncinaError, T>`.

```csharp
var page = await reader.QueryAsync(new ABACDecisionAuditQuery
{
    UserId = "user-42",
    Outcome = AuditOutcome.Denied,
    FromUtc = DateTime.UtcNow.AddDays(-7),
    PageSize = 100
}, cancellationToken);

await using var file = File.Create("abac-decisions.jsonl");
var written = await reader.ExportAsync(
    new ABACDecisionAuditQuery { ToUtc = DateTime.UtcNow }, file, cancellationToken);
```

The reader does not authorize its callers: it is a plain service, not an endpoint, and it exposes the access history of every subject it can see. Put it behind the application's own authorization. Reads of the trail are not themselves audited (tracked in #1639).

### Query validation

| Argument | Rule | Error |
|----------|------|-------|
| `PageNumber` | At least 1 | `validation.abac_decision_audit_query_invalid` |
| `PageSize` | 1 to `OperationAuditQuery.MaxPageSize` (default `OperationAuditQuery.DefaultPageSize`) | `validation.abac_decision_audit_query_invalid` |
| `FromUtc`, `ToUtc` | `FromUtc` must not be after `ToUtc` | `validation.abac_decision_audit_query_invalid` |
| `TenantId` | A blank (empty or whitespace) value is invalid | `validation.abac_decision_audit_query_invalid` |

All filters are optional and combine with AND. The error `details["reason"]` names the argument (`pageNumber`, `pageSize`, `dateRange`, `tenantId`).

### Tenant gate

The gate fails closed. It uses the tenant of the request context (`IRequestContextAccessor`) and the `MultiTenancyMarker` signal, which `AddEncinaTenancy` registers; an application that resolves tenants itself registers the marker itself.

| Request tenant | Query `TenantId` | Multi-tenancy (marker) | Result |
|----------------|------------------|------------------------|--------|
| Present | None, or equal | Any | Query forced to the request tenant |
| Present | Different | Any | `encina.authorization.abac_audit_tenant_mismatch` (HTTP 403) |
| None | Any | Registered, `AllowCrossTenantQueries` false | `encina.authorization.abac_audit_tenant_required` (HTTP 403) |
| None | Any | Registered, `AllowCrossTenantQueries` true | Query runs with the given filter (or none); EventId 9090 (Warning) is logged |
| None | Any | Not registered (single-tenant application) | Query runs with the given filter (or none) |

An ambient tenant is always forced, whatever `AllowCrossTenantQueries` says.

## Configuration

```csharp
services.AddEncinaABAC(options =>
{
    options.DecisionAudit.Enabled = true;
    options.DecisionAudit.WriteTimeout = TimeSpan.FromSeconds(3);
    options.DecisionAudit.AllowCrossTenantQueries = false; // true only for operator tooling
});
```

`ABACOptions.AuditDecisions(Action<ABACDecisionAuditOptions>? configure = null)` is the short form: it sets `DecisionAudit.Enabled = true`, then runs the action (which may turn it off again), and returns the `ABACOptions` for chaining.

### Startup check

`AddEncinaABAC` registers a hosted service, `ABACDecisionAuditStartupCheck`, that reads the final `IOptions<ABACOptions>` when the host starts. The options validator also runs at start and rejects a `WriteTimeout` that is zero, negative or above `int.MaxValue` milliseconds, `MaxTraceEntries` below 1, a `HealthFailureWindow` of zero or less, `Outcomes` with bits outside `ABACDecisionAuditOutcomes.All`, and an undefined `FailureMode`. It also rejects `DecisionAudit.Enabled = true` together with `EnforcementMode = Disabled`, because the disabled enforcement point bypasses evaluation and would record nothing; the host fails to start with an `OptionsValidationException`.

| Situation | Result |
|-----------|--------|
| Audit disabled | Nothing happens; the check never resolves the store |
| Enabled, no `IOperationAuditStore` registered | Critical log, EventId 9087; the host start fails with `InvalidOperationException` |
| Enabled, store is `InMemoryOperationAuditStore` | Warning, EventId 9086: the trail is lost on restart. Only the registered `InMemoryOperationAuditStore` itself is detected; an in-memory store wrapped by a decorator is not |
| Enabled, `FailureMode` is `BestEffort` | Warning, EventId 9084: the trail may be incomplete |

`AddEncinaABAC` never registers an `IOperationAuditStore`, and nothing it registers takes the store in its constructor, so with the audit disabled the container builds without any store. The messages are in the [observability reference](observability.md#decision-audit-messages-9079-9090).

### Registration order

Pipeline behaviors run in registration order. Call `AddEncinaSecurity` before `AddEncinaABAC` so the security behavior runs before the ABAC one. No startup warning exists for the wrong order; the execution-order contract is tracked in #1783.

```csharp
services.AddEncinaSecurity();
services.AddEncinaABAC(o => o.AuditDecisions(a => a.Outcomes = ABACDecisionAuditOutcomes.Denied));
services.AddScoped<IOperationAuditStore, MyOperationAuditStore>(); // the application's own store
```

All `ABACDecisionAuditOptions` properties are in the [configuration reference](configuration.md#decisionaudit-options). The error codes are in the [error reference](errors.md), the log messages in the [observability reference](observability.md#decision-audit-messages-9079-9090).

## Health

When the audit is enabled, `ABACHealthCheck` (name `encina-abac`) also reports the trail. The check is registered only when `ABACOptions.AddHealthCheck` is `true` (default `false`); without it a failure is visible only in the logs (EventIds 9080 to 9083, see the [observability reference](observability.md#decision-audit-messages-9079-9090)). With the audit disabled, the result is the engine's alone.

The check never writes to the store. It reads `ABACDecisionAuditHealthState`, a singleton that the PEP updates after every audited decision: a failed write (a store error, an exception, a timeout or a record that cannot be built) stores the time of the failure, and the next successful write clears it. The state therefore changes only while audited traffic flows; after a failure, no traffic means the failure stays until a write succeeds.

The conditions are evaluated top to bottom, with the failure rows before the store row: a failed write on an `InMemoryOperationAuditStore` reports `write_failed`, not `in_memory_store`.

| Status | Condition | `decision_audit` code |
|--------|-----------|-----------------------|
| `Unhealthy` | No `IOperationAuditStore` is registered | `no_store` |
| `Unhealthy` | `FailureMode` is `FailClosed` and the last write failed within `HealthFailureWindow` | `write_failed` |
| `Degraded` | `FailureMode` is `BestEffort` and the last write failed | `write_failed` |
| `Degraded` | The last write failed more than `HealthFailureWindow` ago, whatever the mode | `write_failed` |
| `Degraded` | The store is `InMemoryOperationAuditStore` | `in_memory_store` |
| `Healthy` | None of the above | `ok` |

`ABACDecisionAuditOptions.HealthFailureWindow` (default 5 minutes, must be greater than zero, validated at start) bounds how long a failure keeps a `FailClosed` application Unhealthy. The engine and the audit are combined and the worse status wins.

The description and the data carry fixed text and the `decision_audit` code only: never a subject, tenant, resource or error message.

## Providers

The trail is stored by whatever `IOperationAuditStore` the application registers.

| Store | Provider | Note |
|-------|----------|------|
| `OperationAuditStoreADO` | SqlServer, PostgreSQL, MySQL | Ids and metadata in plaintext |
| `OperationAuditStoreDapper` | SqlServer, PostgreSQL, MySQL | Ids and metadata in plaintext |
| `OperationAuditStoreEF` | SqlServer, PostgreSQL, MySQL | Ids and metadata in plaintext; the MySQL round trip is not exercised until Pomelo ships EF Core 10 (#2086) |
| `OperationAuditStoreMongoDB` | MongoDB | Ids and metadata in plaintext |
| `MartenOperationAuditStore` | Marten (PostgreSQL) | Needs `Events.StreamIdentity = StreamIdentity.AsString` and a running projection daemon; encrypts `UserId`, `IpAddress`, `UserAgent` and `Metadata`, not `EntityId` or `TenantId`; reads lag behind writes; the store rejects a UTC `DateTime` in `FromUtc` or `ToUtc` (a follow-up issue tracks it), so a Marten export cannot pin its range with `ToUtc` yet |
| `InMemoryOperationAuditStore` | none | Registered by `AddEncinaAudit`; not durable, for tests and development |

## Known limits

- Only requests that pass through the PEP are audited. A direct call to `IPolicyDecisionPoint` is not.
- A missing row means the decision was not evaluated or not recorded, not that access was granted.
- Without `AddHealthCheck`, a failing trail is visible only in the logs (see [Health](#health)). Metrics for the trail are not available yet.
- No startup warning exists for registering `AddEncinaABAC` before `AddEncinaSecurity` (see [Registration order](#registration-order)).
