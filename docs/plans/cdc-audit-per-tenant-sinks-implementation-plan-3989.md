# Implementation Plan: Per-tenant sinks and a tenant field for CDC audit streaming

> **Issue**: [#3989](https://github.com/dlrivada/Encina/issues/3989) (`area-cdc`, `p1-recommended`, `needs-decision`)
> **Type**: Feature
> **Complexity**: Medium (one routing seam, one tenant extractor, a startup validator, tenant tags on the stream telemetry)
> **Estimated Scope**: ~700-900 lines of production code + ~1,500-2,000 lines of tests (about 48 files and up to about 3,400 line-changes in total with manifests, PublicAPI and documentation), delivered in six pull requests of at most 15 files and 1,500 lines each
> **Related plans of the same family**: builds on the plan of #636 (the audit stream: `CdcAuditEntry`, `ICdcAuditSink`, `AuditStreamObserver`, `CdcAuditOptions`), which owns every type this plan extends. #633 and #3995 are unrelated plans written in the same run. Sibling plans are referred to by issue number only.

---

## Summary

The audit stream of #636 writes every entry to the same sinks. In a multi-tenant deployment each entry must carry the tenant it belongs to and, where a tenant requires its own destination, reach only that tenant's sinks. A row without a tenant is rejected (the gate fails closed).

### What the code does today (2026-11-24, `main` at `278c164f1`)

1. **Nothing of the stream exists yet.** `src/Encina.Cdc/Auditing` is absent; #636 is open and its plan is on `main`. The types named here (`CdcAuditEntry`, `ICdcAuditSink`, `AuditStreamObserver`, `CdcAuditOptions`, `AuditFieldPolicy`, `CdcAuditErrors`, `CdcAuditLog`) are the ones that plan specifies, and every pull request below lands after the pull request of #636 that creates the type it edits. If #636 renames one of them, this plan follows the rename.
2. **`ChangeEvent` has no tenant.** It carries `TableName`, `Operation`, `Before`, `After` (`object?`) and `ChangeMetadata` (`src/Encina.Cdc/ChangeEvent.cs`). A tenant can only come from a column of the row: a `JsonElement` from the SQL Server and Debezium connectors, a dictionary from the PostgreSQL and MySQL connectors, a JSON `string` from MongoDB.
3. **There is no ambient tenant in a CDC consumer.** `CdcProcessor` is a hosted service; no request context exists. `ITenantProvider.GetCurrentTenantId()` (`src/Encina.Tenancy/Abstractions/ITenantProvider.cs`) and the ambient-tenant `ITenantConfigurationResolver<TConfig>` that the plan of #1257 adds both read the request, so neither can answer "which tenant owns this row". `Encina.Cdc` references `Encina`, `Encina.Caching` and `Encina.Messaging` only, not `Encina.Tenancy`.
4. **The dispatcher fails the whole event on a `Left`** (the observer contract of #636, Design Choice 4), and a `Left` is neither retried nor dead-lettered today (#2343, open). A row that cannot be routed therefore stops the connector for every tenant until #2343 lands, then dead-letters.
5. **SQL Server Change Tracking supplies no before image for an update** (`SqlServerCdcConnector.cs`, "Change Tracking limitation"), so a tenant change by an update cannot be seen there (Design Choice 4).
6. **Telemetry convention.** ADR-054 D4: the tenant id is a span attribute and a metric tag when multi-tenancy is on, with the cardinality risk documented (#906); #2676 owns the shared tag helper.

### Scope

- **Implements**: a `TenantIdField` per audited table, `CdcAuditEntry.TenantId`, `ICdcAuditSinkResolver` (the seam that turns a tenant into the sinks that receive its entry), a shared-sink resolver (today's behavior) and a per-tenant resolver, startup validation that no two tenants share a destination, and tenant tags on the stream telemetry. A row without a tenant, or with a tenant that has no sink, is rejected.
- **Affected packages**: `Encina.Cdc` only (`Auditing/`, `CdcConfiguration`, `ServiceCollectionExtensions`, diagnostics). Nothing changes in the dispatcher, the processors or the connectors.
- **Provider category**: Database through the four CDC connectors (SQL Server, PostgreSQL, MySQL, MongoDB; Debezium carries the same shapes); the ten providers collapse onto them (provider matrix in Phase 6). No caching, transport, lock or validation category applies.
- **Corrections to the issue text**: the "sink selector by tenant" is a resolver returning a list of sinks, so that an entry can go to a tenant sink and to nothing else; the tenant column is read before projection and, under the recommended Design Choice 2, is subject to the allow list like every other field.
- **Split into pull requests** (counts include tests, manifest edits and PublicAPI lines; the numbering is the merge order; every pull request builds alone and keeps today's shared-sink behavior until the registration of PR 3, and the optional resolver parameter of PR 2 keeps the DI container valid in either merge order with #636 PR 7):

  | PR | Content (phase) | Files | Lines (est.) | Depends on |
  |----|-----------------|-------|--------------|------------|
  | 1 | `CdcAuditEntry.TenantId`, per-table `TenantIdField` and `TenantRouting` options with validation, three error codes with categories, `CdcAuditLog` ids 4886-4889, `AuditTenantExtractor`, PublicAPI, manifest, tests (Phase 1) | 12 | ~650 | #636 PRs 1 and 4 |
  | 2 | `ICdcAuditSinkResolver`, `TenantSinkResolver`, `AuditStreamObserver` change (the resolver is an optional constructor parameter, so an observer registered by #636 PR 7 before or after this PR still resolves; without a resolver it writes to every sink, as #636), PublicAPI, manifest, unit, guard, contract and property tests (Phase 2) | 10 | ~800 | 1, #636 PRs 3 and 5 |
  | 3 | `CdcConfiguration` registration methods, `ServiceCollectionExtensions` (registers `TenantSinkResolver` with `TryAdd`, in the PR that depends on #636 PR 7), the startup validator rules, PublicAPI, manifest, DI proof, validator and guard tests (Phase 3) | 9 | ~600 | 2, #636 PR 7 |
  | 4 | Tenant tags on `CdcAuditMetrics` and `CdcAuditActivitySource`, rejection counter, PublicAPI, manifest, two unit tests (Phase 4) | 6 | ~350 | 3, #636 PR 8 |
  | 5 | Tenant cases in the cross-connector shape contract test, four connector-driven isolation integration classes, benchmark case, load justification edit, manifest (the integration targets of the observer and the resolver are set here, with their tests) (Phase 5) | 8 | ~850 | 3, 4, #636 PR 9 |
  | 6 | Changelog fragment, `Encina.Cdc` README, `docs/features/cdc.md` (Phase 6) | 3 | ~200 | 5 |

  Total: 6 PRs, 48 files, about 3,400 line-changes (sum of the per-PR upper estimates); no PR exceeds 15 files or 1,500 lines. If the maintainer chooses option A of Design Choice 1 (one shared sink set), PR 2 drops `TenantSinkResolver` and PR 3 drops the per-tenant registration, about 12 files fewer.

---

## Design Choices

<details>
<summary><strong>1. How audit entries are routed to sinks by tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tenant field plus one shared sink set**: the entry carries `TenantId`, every sink receives every entry | Smallest change; one destination to operate; a downstream system (a SIEM, an index) can partition by the field | No separation of destinations: a tenant that contractually requires its own storage is not served, and the file sink holds all tenants in one file |
| **B) A sink set resolved per tenant**: `ICdcAuditSinkResolver` maps the row's tenant to the sinks registered for it; the entry carries the tenant as well | Separation of destinations (a directory, an outbox, a store per tenant); an entry cannot reach another tenant's sink by construction | Sink lifecycle per tenant (registration, disposal, directory per tenant); configuration grows with the tenant count |
| **C) Defer to #1257** | No code now | #1257 does not name CDC audit and its ambient-tenant resolver cannot answer for a row, so the work would be only postponed; the entry would still carry no tenant |

### Chosen Option: **B — sink set resolved per tenant** (recommended, pending the maintainer)

### Rationale

A wins when one audit destination already exists and the consumer of the stream partitions by tenant (the entry still gets the field, so A is a strict subset of B and costs nothing more later). B wins when tenants require separate destinations (contract, data residency, offboarding by deleting a directory), which is the situation the issue describes ("tenants may require separate destinations"). C wins only if the maintainer wants one tenant primitive designed first; the cost is that streaming personal data stays tenant-blind meanwhile. The recommendation keeps the `TenantId` field and the rejection rules common to A and B and isolates the difference in one interface, so switching between A and B is a registration choice.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Whether the tenant column goes through the allow list</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant field is read from the raw row, bypassing the allow list** | Routing works even when the table policy forgets to list the column | The tenant id leaves the allow list; it can be personal data when a tenant is a person or a practice |
| **B) The tenant field goes through the same allow list as every other field (and as `EntityId` and `ChangedBy` under Design Choice 8 of #636); a startup check fails when the configured field is not listed, and a runtime value of `[REDACTED]` is a missing tenant** | Fails closed; consistent with #636; the misconfiguration is found at startup, not on the first row | One more line of configuration per table |

### Chosen Option: **B — same allow list, checked at startup** (recommended, pending the maintainer)

### Rationale

Compliance gates fail closed (AGENTS.md section 3) and #636 recommends the same rule for the other identifying fields, so B keeps one rule. A wins where tenant ids are opaque technical ids and the maintainer prefers fewer ways to misconfigure a table; the startup check of B makes the cost of B small. This choice follows Design Choice 8 of #636: if the maintainer picks the other option there, this one follows.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the per-tenant sinks come from (applies to option B of Design Choice 1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registered at startup: `AuditTenantSink<TSink>(tenantId, Action<TSinkOptions>)` registers a .NET keyed service whose key is the tenant id; the resolver looks the key up; every destination (a directory, a connection) is written by the operator, never derived from the row's tenant value** | Fixed, auditable set; no path or name is ever built from row data; disposal by the container; startup validation sees every destination | A new tenant needs a configuration change and a restart; an offboarded tenant is removed the same way |
| **B) A factory creates a sink on first use from per-tenant configuration (a lazy cache with disposal)** | Onboarding without a restart | Destinations derive from tenant ids (needs injective, case-insensitive-safe path encoding and a traversal check); a cache with eviction and disposal; harder to validate at startup |

### Chosen Option: **A — registered at startup** (recommended, pending the maintainer)

### Rationale

B wins for a SaaS that onboards tenants at runtime and cannot restart the audit consumer; A wins by default because the CDC consumer is infrastructure with a small, known tenant set and because a path or table name built from a column value is an injection and collision surface. Under A the keyed-service key is compared exactly (an object key, no string concatenation), so two tenant ids never share a sink. A seam for B stays open: a third-party `ICdcAuditSinkResolver` can be registered instead.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. An update that changes the tenant column</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject: when the before and the after image name different tenants the entry fails with `Left` (`ErrorCategory.Forbidden`, ADR-062 rule R6, a tenant conflict)** | No entry ever carries one tenant's data to another's sink | A legitimate tenant merge or move stops the stream until the event is dead-lettered or the move is excluded |
| **B) Split into two entries**: the old tenant receives the entry with `Before` only, the new tenant the entry with `After` only | Both tenants' trails stay complete and separate | Two writes for one event (a partial failure re-writes the first, so the deduplication key needs a tenant suffix); more code in the observer |

### Chosen Option: **A — reject** (recommended, pending the maintainer)

### Rationale

B wins where moving rows between tenants is a routine operation (a merge tool) and both trails are required; A wins by default because a tenant column should be immutable, and sending one tenant's before image to another tenant's sink is the failure this feature exists to prevent. On SQL Server Change Tracking an update has no before image (fact 5), so the check cannot run there; the documentation states it per connector and the integration test for SQL Server asserts the documented behavior.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. A tenant value equal to the reserved <code>default</code> id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Treat `default` as a missing tenant: the row is rejected** | Consistent with the recommended reserved-id rule of #1257 (the resolver rejects `default` as a real tenant when multi-tenant); a single-tenant row cannot slip into a tenant sink | A table that stores `default` for untenanted rows is rejected until it is excluded from the audit or backfilled |
| **B) Route `default` like any other tenant id** | No special case | Untenanted rows and a tenant that happens to be named `default` share one sink; contradicts the reserved id if #1257 keeps it |

### Chosen Option: **A — `default` is a missing tenant** (recommended, pending the maintainer)

### Rationale

B wins if the maintainer decides in #1257 that `default` is an ordinary tenant id; A wins while the reserved id is the recommendation there, because two meanings of one value in one sink is the collision the reserved segment prevents. The constant is read from the type #1257 creates when it lands; until then the extractor holds the literal in one place with a test that fails when the two diverge.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. A tenant with no configured sink (an offboarded tenant, a new tenant, a typo)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject: `Left` (`ErrorCategory.Internal`, ADR-062 rule R1, a missing topology entry); the event fails and follows the CDC retry and dead-letter path of #2343** | Fails closed; no tenant data lands anywhere it was not configured to | Until #2343 lands the connector retries the same event and stops the stream for every tenant; afterwards the event is dead-lettered |
| **B) A configured quarantine sink receives the entries of unrouted tenants (explicit, logged, counted)** | One unconfigured tenant cannot stop the stream of the others; the entries are not lost | The quarantine holds data of tenants that required separate destinations, which is the separation the feature promises; a second sink to secure |

### Chosen Option: **A — reject** (recommended, pending the maintainer)

### Rationale

B wins where availability of the audit stream for all tenants outweighs the separation of an unconfigured tenant's data (a large tenant set with frequent onboarding); A wins by default because the only opt-out of a gate is explicit and logged (AGENTS.md section 3) and because a shared quarantine defeats the reason a tenant asked for its own destination. Rejection is made survivable by the startup validator (every table's tenants are listed) and by #2343; the plan does not change the dispatcher.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. A table with no <code>TenantIdField</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unset means untenanted: the table streams exactly as #636, with `TenantId` null** | Pay-for-what-you-use: nothing changes for a table that does not opt in; reference tables with no tenant need no configuration | A multi-tenant application that enables the stream and forgets the field streams untenanted rows silently, against the issue's "a row without a tenant is rejected" |
| **B) When `MultiTenancyMarker` is registered, startup validation fails if any audited table has no `TenantIdField`, unless the table is listed in an explicit opt-out (`AuditUntenantedTable(name)`), which is logged in one startup summary line (id 4889: the number of per-tenant tables, tenants and opted-out tables; no counter, the log is the record)** | Fails closed: the unsafe case needs an explicit, logged choice; the issue's criterion holds by default | Tables without a tenant (shared reference data) need one line of configuration; the check depends on the marker being registered (`AddEncinaTenancy`) |

### Chosen Option: **B — required when multi-tenancy is registered** (recommended, pending the maintainer)

### Rationale

The only opt-out of a compliance gate is explicit and logged (AGENTS.md section 3), so a default that lets the unsafe case through needs a reason; A wins where the stream is enabled by deployments that are single-tenant by nature and the marker is never registered (B then changes nothing for them), or where the maintainer prefers fewer startup failures. An application that resolves tenants by hand without the marker is not covered by B; the documentation says so, and the shared-activation rule of #1257 (marker or an ambient tenant) is the place that would extend it.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. The tenant tag on the stream telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A tenant tag only for tenants resolved to a configured sink (under option B of Design Choice 1; no tenant tag under option A of Design Choice 1); rejected rows are counted by `reason` with no tenant tag** | The tag set is bounded by configuration; row values cannot create tag values | A rejected row cannot be attributed to a tenant in the metric |
| **B) The tenant tag carries the tenant of every row, including rows that are rejected** | Literal reading of ADR-054 D4 and SPEC-002 AC-044 (the tenant tag in every package when multi-tenancy is on) | Cardinality is bounded only by what a table contains: one bad column creates unbounded tag values |

### Chosen Option: **A — bounded tag** (recommended, pending the maintainer)

### Rationale

ADR-054 D4 requires the tenant tag when multi-tenancy is on and documents the cardinality cost (#906), so both options comply and the rule does not choose. B wins where operators must see which unrouted tenant is failing and the metrics backend caps cardinality; A wins by default because the tag values of A are the configured tenants.

**Class:** Dilemma

</details>

### Smaller decisions (register)

Each entry holds whichever option is chosen in Design Choices 1 to 8; choices that depend on an option are written inside that Design Choice.

| Id | Decision | Class | Basis |
|----|----------|-------|-------|
| R1 | In a table that has a `TenantIdField`, a row whose tenant column is absent, null, blank or whitespace is rejected with `encina.cdc.audit_tenant_missing`, `ErrorCategory.InvalidRequest` | Settled | ADR-062 rule R3 (a missing tenant is `InvalidRequest`); AGENTS.md section 3 (gates fail closed) |
| R2 | A cancellation while resolving or writing propagates as `OperationCanceledException`; it is never converted into a `Left` | Settled | AGENTS.md section 3 (cancellation is not an error and propagates, ADR-052); ADR-062 rule R5 |
| R3 | The new log ids are 4886-4889 of the `Cdc` range 4800-4899, packed directly after the six ids 4880-4885 of #636 (4886-4891 are the slice the #636 plan leaves unassigned inside its block 4854-4891 of the registry's family table, so no registry edit is needed); they log the error code and the table name, never a tenant value, row content or `EncinaError.Message` | Trivial | AGENTS.md section 7 (ids inside the package's registered range, packed) and section 3 (`EncinaError.Message` never reaches logs) |
| R4 | Sink option members that hold a connection string or a key (the outbox target of a tenant sink) are `[JsonIgnore]` with a `ToString()` override; a file directory is not a secret but, as in #636, it is kept out of logs and error messages | Settled | AGENTS.md section 3 (options with secrets, #851) |

---

## Implementation Phases

### Phase 1: Tenant on the entry, options and extraction

> **Goal**: the entry carries a tenant and a row without one is rejected; no routing yet.

<details>
<summary><strong>Tasks</strong></summary>

1. `Auditing/CdcAuditEntry.cs` (#636 PR 1): add `string? TenantId` (null when per-tenant routing is off).
2. `Auditing/CdcAuditOptions.cs`: per-table `TenantIdField` (nullable) and `TenantRouting` (`Shared` or `PerTenant`, default `Shared`, Design Choice 1); `CdcAuditOptionsValidator.cs`: a `PerTenant` table needs a `TenantIdField`, and the field must be on the table's allow list (Design Choice 2).
3. `Auditing/CdcAuditErrors.cs`: `encina.cdc.audit_tenant_missing` (`InvalidRequest`, R1), `encina.cdc.audit_tenant_unrouted` (`Internal`, Design Choice 6 option A) and `encina.cdc.audit_tenant_conflict` (`Forbidden`, Design Choice 4 option A), each declared through `EncinaErrors.Create(code, ErrorCategory.X, ...)` with a constant message and a `GetCategory` test. Under other options of Design Choices 4 and 6 the code is dropped or re-categorised.
4. `Auditing/CdcAuditLog.cs`: ids 4886 tenant missing, 4887 tenant unrouted, 4888 tenant changed by an update, 4889 one startup summary line (per-tenant routing enabled: counts of tenants, per-tenant tables and opted-out tables; Design Choice 7), never one line per table.
5. `Auditing/AuditTenantExtractor.cs` (`internal sealed`): reads the configured field from `After`, or from `Before` for a delete, in each connector shape (`JsonElement`, dictionary, JSON string) through the projector of #636; compares both images when both exist (Design Choice 4); applies the `default` rule (Design Choice 5); a malformed row is a `Left`, never an exception. Returns `Either<EncinaError, string?>`.
6. Tests: extractor unit tests for the four shapes and every rejection; options validator cases; `GetCategory` cases; guard tests for the new public members. Manifest rows and PublicAPI lines through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: The CDC audit stream (#636) writes redacted entries to sinks. A tenant can only come from a column of the row;
connectors hand over JsonElement, dictionaries or a JSON string.
TASK: Add TenantId to CdcAuditEntry, the per-table TenantIdField and TenantRouting options with validation, the three error
codes with ErrorCategory, ids 4886-4889 of CdcAuditLog and the internal AuditTenantExtractor.
KEY RULES: fail closed (missing tenant is a Left); error codes declared with ADR-062 categories and constant messages;
never log a tenant value, row content or EncinaError.Message; cancellation propagates; no [Obsolete]; Either returns.
REFERENCE FILES: docs/plans/audit-log-streaming-cdc-implementation-plan-636.md, src/Encina.Cdc/ChangeEvent.cs,
src/Encina.Cdc/Errors/CdcErrors.cs, src/Encina.Cdc/CdcLog.cs.
```

</details>

### Phase 2: Sink resolution and the observer

> **Goal**: an entry reaches only the sinks of its tenant.

<details>
<summary><strong>Tasks</strong></summary>

1. `Auditing/ICdcAuditSinkResolver.cs` (public): `ValueTask<Either<EncinaError, IReadOnlyList<ICdcAuditSink>>> ResolveAsync(string? tenantId, CancellationToken)`.
2. No shared resolver class: the observer takes `ICdcAuditSinkResolver? resolver = null` as an optional constructor parameter and, when it is null (`TenantRouting.Shared`, today's behavior), writes to every registered sink, so the DI container stays valid whether #636 PR 7 or this PR 2 merges first.
3. `Auditing/TenantSinkResolver.cs` (Design Choices 1 and 3): `GetKeyedServices<ICdcAuditSink>(tenantId)` from the processor's scope (the dispatcher is a singleton and `OutboxAuditSink` is scoped; observers and resolvers come from the scope in the context, #636 R6); a tenant with no keyed sink returns `audit_tenant_unrouted` (Design Choice 6); the lookup uses the exact tenant id, never a prefix or a case-folded form.
4. `Auditing/AuditStreamObserver.cs`: per table, extract the tenant (Phase 1), resolve the sinks, write the entry (with `TenantId`) to the resolved sinks only; the first `Left` fails the event; with `Shared` routing and no `TenantIdField` the path is unchanged.
5. Tests: unit (each rejection, order, only the tenant's sinks written, a null resolver writes to every sink), guard (the optional resolver parameter accepts null: valid-result test), contract (`ICdcAuditSinkResolver` cases against `TenantSinkResolver`), property (for any generated rows and tenants no entry is written to a sink of another tenant, and an entry's `TenantId` equals the sink's tenant). Manifest and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: AuditStreamObserver (#636) writes each entry to every sink and returns the first Left.
TASK: Add ICdcAuditSinkResolver (an optional observer constructor parameter) and TenantSinkResolver and make the observer write only to the resolved
sinks of the entry's tenant.
KEY RULES: resolve from the processor's scope, never the root provider; exact tenant-id lookup (no concatenated string keys);
a tenant with no sink is a Left, never a fallback to another tenant's or a shared sink; ValueTask/Either returns; a property
test proves isolation.
REFERENCE FILES: docs/plans/audit-log-streaming-cdc-implementation-plan-636.md (Phases 2-4), src/Encina.Cdc/Processing/CdcDispatcher.cs.
```

</details>

### Phase 3: Registration and startup validation

> **Goal**: `WithAuditStreaming` registers per-tenant sinks and rejects unsafe configurations.

<details>
<summary><strong>Tasks</strong></summary>

1. `CdcConfiguration.cs` (#636 PR 7): `AuditTenantField(table, field)`, `AuditTenantSink<TSink>(tenantId, Action<TSinkOptions>)` and `AuditUntenantedTable(name)` (Design Choice 7 option B), all three new public members listed in `PublicAPI.Unshipped.txt` and covered by argument tests in `tests/Encina.GuardTests` (null and blank checks); `ServiceCollectionExtensions.cs`: register the resolver by routing mode (`TryAdd`, so an application's own `ICdcAuditSinkResolver` wins in any order) and the keyed sinks; the sinks and the resolver are registered together with the services they resolve, so the provider builds under `ValidateOnBuild` and `ValidateScopes`.
2. `CdcAuditStreamingStartupValidator.cs`: every `PerTenant` table has a tenant field on its allow list; under Design Choice 7 option B, when `MultiTenancyMarker` is registered every audited table has a `TenantIdField` or is listed with `AuditUntenantedTable(name)` (in the one summary line, id 4889); at least one tenant sink exists; no tenant id is blank, `default` or duplicated; no two tenants share a destination (file directories compared as normalized full paths, case-insensitively and by containment; the same outbox target; the same connection); the outbox sink per tenant needs the outbox module (ADR-050). Failures name the table and tenant position, never print a destination.
3. DI test per sink type with `ValidateOnBuild` and `ValidateScopes`; validator unit tests for every rejection; two guard test files in `tests/Encina.GuardTests` (one for the `CdcConfiguration` methods, one for the registration extension and the validator constructor).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: WithAuditStreaming (#636) registers options, observer and sinks; several CDC plans edit CdcConfiguration and
ServiceCollectionExtensions, so keep each edit isolated.
TASK: Add the per-tenant registration methods, the resolver registration and the startup validator rules.
KEY RULES: disabled by default; TryAdd for the resolver; every resolved dependency is registered; the validator never prints a
path, connection string or tenant value; tests build the provider with ValidateOnBuild and ValidateScopes.
REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/ServiceCollectionExtensions.cs,
docs/plans/audit-log-streaming-cdc-implementation-plan-636.md (Phase 5).
```

</details>

### Phase 4: Observability

> **Goal**: operators see per-tenant volume and rejections without row data in the tag space.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/CdcAuditMetrics.cs` (#636 PR 8): the existing counter `encina.cdc.audit.entries` gets a `tenant` tag only in `PerTenant` mode and only for a tenant resolved to a configured sink (Design Choice 8, option A); new counter `encina.cdc.audit.rejected` (tag `reason`: `tenant_missing`, `tenant_unrouted`, `tenant_conflict`; no tenant tag).
2. `Diagnostics/CdcAuditActivitySource.cs`: the tenant attribute on the entry span in the same cases, through the helper of #2676 once it exists (until then a local constant in one place).
3. Unit tests with a meter and an activity listener; manifest and PublicAPI through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: ADR-054 D4 puts the tenant on spans and metrics when multi-tenancy is on; row data must not create tag values.
TASK: Add the tenant tag and the rejection counter to the audit stream telemetry.
KEY RULES: tag only configured tenants; no tenant on rejection counters; tag names lowercase dotted; tests assert every tag.
REFERENCE FILES: src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationMetrics.cs, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md.
```

</details>

### Phase 5: Provider shapes and isolation tests

> **Goal**: the same routing from every connector.

<details>
<summary><strong>Tasks</strong></summary>

1. Add tenant cases to the cross-connector shape contract test of #636 PR 9 (SQL Server `JsonElement`, PostgreSQL and MySQL dictionaries, MongoDB JSON string, Debezium envelope): the same logical row yields the same `TenantId`.
2. Four integration classes (PostgreSQL, MySQL, SQL Server, MongoDB) through the shared `[Collection]` fixtures: two tenants, two file sinks in separate directories, each tenant's changes appear only in its own directory; a row without a tenant and a row of an unconfigured tenant are rejected; a tenant change is rejected where a before image exists (not on SQL Server, where the documented behavior is asserted instead).
3. Benchmark case for extraction plus resolution per event added to the #636 benchmark class; the load justification of #636 PR 9 gets one paragraph.

#### Provider matrix

The stream depends on the source database's connector, not on the application's data access family (as in #636).

| # | Family | Database | Connector (payload shape) | Tenant field read | Before image on update (tenant-change check) |
|---|--------|----------|---------------------------|-------------------|-----------------------------------------------|
| 1 | ADO.NET | SQL Server | Change Tracking (`JsonElement`) | from `After`; from `Before` on delete | no: the check cannot run |
| 2 | ADO.NET | PostgreSQL | logical decoding (dictionary) | as row 1 | only with `REPLICA IDENTITY FULL` |
| 3 | ADO.NET | MySQL | binlog (dictionary) | as row 1 | yes |
| 4 | Dapper | SQL Server | as row 1 | as row 1 | as row 1 |
| 5 | Dapper | PostgreSQL | as row 2 | as row 2 | as row 2 |
| 6 | Dapper | MySQL | as row 3 | as row 3 | as row 3 |
| 7 | EF Core | SQL Server | as row 1 | as row 1 | as row 1 |
| 8 | EF Core | PostgreSQL | as row 2 | as row 2 | as row 2 |
| 9 | EF Core | MySQL | as row 3 | as row 3 | as row 3 |
| 10 | MongoDB | MongoDB | change stream (JSON `string`) | document field | only with pre-images enabled |
| extra | any | Debezium-captured database | envelope (`JsonElement`) | as row 1 | per the source connector configuration |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: Four connectors, four payload shapes; SQL Server updates have no before image.
TASK: Extend the shape contract test and add the four two-tenant isolation integration classes and the benchmark case.
KEY RULES: tests instantiate the real extractor, resolver and observer; shared [Collection] fixtures; FakeTimeProvider;
BenchmarkSwitcher.FromAssembly; no Thread.Sleep; temp directories per tenant removed afterwards.
REFERENCE FILES: docs/plans/audit-log-streaming-cdc-implementation-plan-636.md (Phases 6 and 8), docs/testing/integration-tests.md.
```

</details>

### Phase 6: Documentation and finalization

> **Goal**: operators can configure per-tenant destinations safely.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples on every new public member, naming that the resolver never gates a missing tenant by itself (the observer rejects).
2. `changelog.d/3989-cdc-audit-per-tenant-sinks.added.md`; `src/Encina.Cdc/README.md` and `docs/features/cdc.md`: the tenant field, the three rejections, the before-image limit per connector, offboarding steps (remove the sink, then the table's tenant) and the cardinality note of #906.
3. No ADR: the decisions are recorded here and extend the ADR of #636 (tamper evidence and the observer contract).
4. Verification: build with zero warnings, tests, every flag at its target in the table below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 5.
TASK: Documentation, changelog fragment, verification.
KEY RULES: never type coverage figures by hand; English only; compliance texts cover Spain and the EU only; follow the
encina-docs skill; never edit CHANGELOG.md; never name a private application.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, docs/features/cdc.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Article 5(1)(c), 5(1)(f), 32 | Minimisation and confidentiality: the stream is a second copy of personal data; separation by tenant limits who can read it |
| GDPR Article 28 and 5(2) | Processor separation and accountability when tenants are different controllers |
| SPEC-002 DEC-006, DEC-009 | Fail-closed gates; every capability SPEC-002 touches is tenant-aware |
| ADR-054 D4 | Tenant id as span attribute and metric tag when multi-tenancy is on |
| ADR-062 rules R1, R3, R5, R6 | Categories of the new error codes |
| ADR-050 | The outbox sink per tenant requires the outbox module |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ChangeEvent`, `ChangeMetadata` | `src/Encina.Cdc` | The row and its source |
| Audit stream types of #636 | `src/Encina.Cdc/Auditing` (after #636) | Entry, sinks, observer, options, projector |
| `CdcErrors`, `CdcErrorCodes` | `src/Encina.Cdc/Errors` | Pattern for the new codes |
| `CdcConfiguration`, `ServiceCollectionExtensions` | `src/Encina.Cdc` | Registration |
| `CacheInvalidationMetrics` | `src/Encina.Cdc/Caching/Diagnostics` | Metrics pattern |
| Reserved `default` tenant id | plan of #1257 (Design Choice 2) | Design Choice 5 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (existing range 4800-4899, moving to 11800-11899 in #4007 CM-5) | 4886-4889 used, packed directly after #636's 4880-4885 | `CdcAuditLog` of #636 owns 4880-4885; 4886-4891 are the slice its plan leaves unassigned inside its family block 4854-4891, so this follow-up uses 4886-4889 and 4890-4891 stay spare; 4892-4899 are untouched. `Encina.Cdc` is already mapped in `AssemblyRanges`, so no map change is needed, and the registry document needs no edit |

### Per-file coverage targets (AGENTS.md section 9)

`Encina.Cdc.json` has no per-file targets for `CdcConfiguration.cs` and `ServiceCollectionExtensions.cs` (they use the package targets: unit 50, guard 15, property 10, integration 75), so PR 3 enters U 50 and G 15 for them, the package values; when the measured whole-file value is higher the row takes the measured value, and a target is never lowered afterwards.

Per flag: U unit, G guard, C contract, P property, I integration; each row is entered in `.github/coverage-manifest/Encina.Cdc.json` by the PR that adds the file, with the tests of that PR, and never lowered afterwards. For an existing file the row is the whole-file target (the manifest value when the PR opens, or the value shown if higher).

| File | PR | U | G | C | P | I | Justification |
|------|----|---|---|---|---|---|---------------|
| `CdcAuditEntry.cs` (whole file) | 1 | 85 | 0 | 0 | 0 | 0 | Record with one more property; no parameters to guard beyond the record |
| `CdcAuditOptions.cs` (whole file) | 1 | 90 | 0 | 0 | 0 | 0 | Plain options and defaults reachable; no parameters to guard |
| `CdcAuditOptionsValidator.cs` (whole file) | 1 | 95 | 30 | 0 | 0 | 0 | Pure validation, every branch reachable; guard tests reach argument checks only (evidence on `main`: the package guard target of `Encina.Cdc.json` is 15 and `OutboxCdcHandler.cs` guard measures 15.3 %) |
| `CdcAuditErrors.cs` (whole file) | 1 | 90 | 0 | 0 | 0 | 0 | Factories plus the `GetCategory` test |
| `CdcAuditLog.cs` (whole file) | 1 | 70 | 0 | 0 | 0 | 0 | Generated methods reached by the extractor, resolver and validator tests |
| `AuditTenantExtractor.cs` | 1 | 95 | 30 | 0 | 0 | 0 | Pure extraction over four shapes; guard reaches argument checks only; the per-connector contract case belongs to the shape test of PR 5 |
| `ICdcAuditSinkResolver.cs` | 2 | 0 | 0 | 0 | 0 | 0 | Interface, no executable lines |
| `TenantSinkResolver.cs` | 2 | 95 | 25 | 85 | 85 | 0 | Keyed lookup with a fake scope; contract cases and the isolation property test run it; guard reaches the constructor and argument checks |
| `AuditStreamObserver.cs` (whole file) | 2 | 90 | 25 | 0 | 0 | 0 | Tenant branch, rejections and writes with fake sinks; guard reaches the constructor checks; its integration target is set in PR 5 |
| `CdcConfiguration.cs` (whole file) | 3 | 50 | 15 | 0 | 0 | 0 | The new fluent methods are covered by the DI and validator tests, the rest of the file keeps its own target and is not raised; guard 15 is the share reached by the argument tests of the three new methods (package guard target 15, `OutboxCdcHandler.cs` measured 15.3 %) |
| `ServiceCollectionExtensions.cs` (whole file) | 3 | 50 | 15 | 0 | 0 | 0 | DI proof with `ValidateOnBuild` and `ValidateScopes` covers the new registration; the rest of the file is not touched; guard reaches the argument checks only |
| `CdcAuditStreamingStartupValidator.cs` (whole file) | 3 | 95 | 30 | 0 | 0 | 0 | Every rejection (shared destination, `default`, duplicate, untenanted table) is unit-testable |
| `AuditStreamObserver.cs`, `TenantSinkResolver.cs` | 5 | n/a | n/a | n/a | n/a | 70 | Integration flag only: the connector-driven two-tenant tests run both files; set in PR 5 with its tests and its manifest edit |
| `CdcAuditMetrics.cs` (whole file) | 4 | 90 | 0 | 0 | 0 | 0 | Meter listener asserts every tag |
| `CdcAuditActivitySource.cs` (whole file) | 4 | 85 | 0 | 0 | 0 | 0 | Spans asserted with an activity listener |

CRAP stays at or below 10 on every changed method: the tenant branch of the observer lives in a small private method, and the extractor has one method per connector shape.

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production, new or edited under `src/` | 14 | PR 1: 6; PR 2: 3 (resolver interface, tenant resolver, observer); PR 3: 3; PR 4: 2 |
| PublicAPI and manifests | 9 | PRs 1-4: two each; PR 5: one manifest |
| Tests | 20 | PR 1: 4; PR 2: 5 (observer tenant unit, resolver unit, guard, contract, property); PR 3: 4 (DI proof, validator, configuration guard, extension guard); PR 4: 2; PR 5: 5 (shape contract edit, four integration classes) |
| Benchmark and load justification edits | 2 | PR 5 |
| Docs, changelog | 3 | PR 6 |

PR rows add to 48 distinct touches: 12 + 10 + 9 + 6 + 8 + 3.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are implementing issue #3989:
tenant-aware CDC audit streaming on top of the stream of #636. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds TenantId, the per-table tenant field, error codes, ids 4886-4889 and the tenant extractor.
Phase 2 adds ICdcAuditSinkResolver (optional observer parameter) with the per-tenant resolver and changes AuditStreamObserver. Phase 3 adds the
registration methods and the startup validator. Phase 4 adds tenant tags and the rejection counter. Phase 5 proves the four
connector shapes with two-tenant isolation tests. Phase 6 documents. Deliver as the six pull requests of the Summary, after the
pull requests of #636 each one names.

KEY PATTERNS: fail closed (missing, unconfigured or conflicting tenant is a Left); exact tenant-id lookup; destinations written by
the operator, never derived from row data; resolve from the processor's scope; never log tenant values, row content or
EncinaError.Message; cancellation propagates; TryAdd for the resolver; DI proof with ValidateOnBuild and ValidateScopes.

REFERENCE FILES: docs/plans/audit-log-streaming-cdc-implementation-plan-636.md, src/Encina.Cdc/CdcConfiguration.cs,
src/Encina.Cdc/ChangeEvent.cs, src/Encina.Cdc/Errors/CdcErrors.cs, .github/coverage-manifest/Encina.Cdc.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: a write-only stream |
| 2 | OpenTelemetry | ✅ | Phase 4: tenant tag only for configured tenants, rejection counter, tenant span attribute (ADR-054 D4) |
| 3 | Structured Logging | ✅ | `CdcAuditLog` ids 4886-4889; error code and table only |
| 4 | Health Checks | ⏭️ | Sink reachability per tenant joins the processor liveness work of #2396 (open) |
| 5 | Validation | ✅ | Options validator and startup validator (tenant field on the allow list, no shared destination, no `default`) |
| 6 | Resilience | ✅ | A failed write fails the event and follows the retry and dead-letter path of #2343 (open); sinks add no hidden retry |
| 7 | Distributed Locks | ❌ | Not applicable here; coordination of several CDC consumers is the follow-up named in the plan of #636 |
| 8 | Transactions | ❌ | Not applicable: CDC observes committed transactions |
| 9 | Idempotency | ✅ | The `DeduplicationKey` of #636 is kept; under Design Choice 4 option B it would need a tenant suffix |
| 10 | Multi-Tenancy | ✅ | The feature: tenant field, routing, rejection of missing, unrouted and conflicting tenants |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level |
| 12 | Audit Trail | ✅ | The stream is the external audit trail; this feature separates it per tenant |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #636 (audit stream) | open, plan on `main` | Owns every type extended here; the six pull requests start after the #636 pull requests named in the split |
| #2343 (a `Left` from the dispatcher is not retried or dead-lettered) | open | Needed for the rejection rules to be survivable (Design Choice 6) |
| #1257 (SPEC-002 multi-tenancy) | open, plan on `main` | Owns the reserved `default` id read by Design Choice 5; no code dependency |
| #2676 (tenant tag helper) | open | The span attribute helper of Phase 4; a local constant meanwhile |
| #2396 (processor liveness health checks) | open | Per-tenant sink health |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the Dilemmas (choices 1 to 8) when work on the issue starts; none is decided here.
2. Publish the plan link as a comment on #3989.
3. Begin PR 1 after the pull requests of #636 that it names.
4. Each phase is a self-contained commit; the final commit references `Fixes #3989`.
