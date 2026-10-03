<!-- issue
title: [DEBT] Encina.ADO.PostgreSQL: evaluate OpenTelemetry, resilience and caching cross-cutting integration
labels: technical-debt, area-database
milestone: v0.21.0 — Documentation
-->

## Type

- [ ] Failing tests
- [x] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [x] Incorrect implementation
- [ ] Other

## Description

The SPEC-003 pilot-2 deep quality audit of `Encina.ADO.PostgreSQL` ran the ADR-018 twelve-function
cross-cutting check (AUD-02) and found several functions with neither an integration nor a
recorded deferral:

- **OpenTelemetry**: no `ActivitySource`/`Meter` usage anywhere in the package. Database calls,
  bulk operations and repository queries are currently untraced.
- **Resilience**: no Polly/`ResiliencePipeline` usage. Transient PostgreSQL connection failures
  are not retried at this layer.
- **Caching**: no `ICacheProvider`/query-result caching integration, even though
  `Encina.EntityFrameworkCore` ships one for the same database (`Caching/CachedQueryResult.cs`,
  `QueryCacheInterceptor.cs`, etc. — 6 files) and `Encina.EntityFrameworkCore/DomainEvents/*`
  dispatches domain events on save, which ADO.PostgreSQL also has no equivalent of.
- **Structured logging**: present, but only 5 `[LoggerMessage]` call sites across roughly 25
  store/factory classes — Outbox, Inbox, Sagas, all three Auditing stores, ABAC, Anonymization,
  Scheduling and BulkOperations currently log nothing on failure paths.

Distributed locks are correctly out of scope for this package (PostgreSQL advisory locks belong
in a would-be `Encina.DistributedLock.PostgreSQL`, which does not exist yet under `src/` — see
Related Issues). Health checks and transactions are already integrated and are not part of this
item.

## Location

- **File(s)**: `src/Encina.ADO.PostgreSQL/**/*.cs` (whole package)
- **Package(s)**: Encina.ADO.PostgreSQL

## Current Behavior

No tracing, no resilience policies, no caching hook, and log coverage limited to 2 of roughly 20
feature areas.

## Expected Behavior

Per CLAUDE.md's Cross-Cutting Integration Rule, each of the twelve functions should end in one
of: integrated, deferred to an open issue with a citation, or documented as not applicable with a
one-sentence reason. Today these four sit in none of those three states for this package.

## Root Cause

The package was built feature-by-feature (Outbox, Inbox, Sagas, ...) without a pass that
evaluates it as a whole against the cross-cutting checklist; ADR-018 postdates most of this
package's original implementation.

## Proposed Fix

For each of OpenTelemetry, resilience and caching: either scope a follow-up implementation issue
(citing this audit) or record an explicit "not applicable for a raw-ADO.NET provider" decision
(e.g. resilience might be judged the caller's responsibility at this layer, unlike EF Core's
interceptor model) in this package's README or an ADR addendum. For structured logging, add
`[LoggerMessage]` calls to the currently-silent store classes' failure paths, reusing the
package's registered EventId range `ADOPostgreSQL = (3250, 3299)` (`EventIdRanges.cs:138`) —
there is ample room left in the range for the roughly 20 additional call sites this would need.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [x] Large (> 4 hours)

## Related Issues

- ADR-018 - Cross-cutting integration principle (the rule this item enforces)
- #207 - Encina.DistributedLock.PostgreSQL (pg_advisory_lock), the package that legitimately keeps distributed locks out of ADO.PostgreSQL
- Sibling audit of Encina.EntityFrameworkCore (whichever package owns `Caching/*` and `DomainEvents/*`) — cite when scoping the caching/domain-events follow-up so the two audits agree on scope
