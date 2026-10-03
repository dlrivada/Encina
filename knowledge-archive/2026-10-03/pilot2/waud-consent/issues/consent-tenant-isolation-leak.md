<!-- issue
title: [BUG] Encina.Compliance.Consent read queries ignore TenantId and can leak consent across tenants
labels: bug, area-compliance, area-multitenancy, ai:claude-required
milestone: v0.14.0 — Hardening
-->

## Description

`ConsentAggregate` and `ConsentReadModel` both carry a `TenantId` (and `ModuleId`) for multi-tenancy scoping, but `DefaultConsentService`'s query methods never filter by it. In a multi-tenant host, `GetConsentBySubjectAndPurposeAsync`, `GetAllConsentsAsync` and `HasValidConsentAsync` can return, or report as valid, a consent record that belongs to a different tenant sharing the same `DataSubjectId`/`Purpose` pair.

## Steps to Reproduce

1. Enable multi-tenancy and grant consent for `dataSubjectId="user-1"`, `purpose="marketing"` under `tenantId="tenant-a"` via `ConsentAggregate.Grant(..., tenantId: "tenant-a")`.
2. Grant a different consent for the same `dataSubjectId`/`purpose` under `tenantId="tenant-b"`.
3. Call `IConsentService.GetConsentBySubjectAndPurposeAsync("user-1", "marketing")` (or `HasValidConsentAsync`) from a request scoped to `tenant-b`.
4. Observe that the query in `Services/DefaultConsentService.cs:323-325` (`q.Where(c => c.DataSubjectId == dataSubjectId && c.Purpose == purpose)`) has no `TenantId` predicate, so either tenant's record can be returned depending on read-model ordering.

## Expected Behavior

Every query in `IConsentService` that reads consent state is scoped to the caller's current tenant (and module, where module isolation is enabled), consistent with `CLAUDE.md`'s cross-cutting multi-tenancy rule and SPEC-002 REQ-061 ("every store and provider it ships on" is tenant-aware; queries filter by tenant; the lifecycle and legal model, explicitly naming consents, are per tenant).

## Actual Behavior

- `GetConsentBySubjectAndPurposeAsync` (`Services/DefaultConsentService.cs:323-337`) filters only by `DataSubjectId` and `Purpose`.
- `GetAllConsentsAsync` (`Services/DefaultConsentService.cs:357-359`) filters only by `DataSubjectId`.
- `HasValidConsentAsync` (`Services/DefaultConsentService.cs:380-385`) filters only by `DataSubjectId`, `Purpose` and `Status`.
- None of the three accepts or applies a tenant/module filter, even though `ConsentReadModel.TenantId`/`ModuleId` (`ReadModels/ConsentReadModel.cs:111-118`) are populated by `ConsentProjection.Create` (`ReadModels/ConsentProjection.cs:67-68`) from the write side.

## Environment

- **Encina Version**: pre-1.0 (unreleased)
- **.NET Version**: .NET 10.0
- **OS**: N/A — found during a static code audit
- **Package(s) Affected**: Encina.Compliance.Consent

## Code Sample

```csharp
// src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
var result = await _readModelRepository.QueryAsync(
    q => q.Where(c => c.DataSubjectId == dataSubjectId && c.Purpose == purpose),
    cancellationToken);
// no TenantId / ModuleId predicate
```

## Stack Trace

```
N/A — this is a static-analysis finding (SPEC-003 audit AUD-02), not an exception.
```

## Additional Context

Found during the SPEC-003 deep quality audit of `Encina.Compliance.Consent` (2026-09-24), checklist item AUD-02 (multi-tenancy function). Listed as finding F2 in `artifacts/audit/findings.csv` and `artifacts/audit/encina-compliance-consent.md` in the audit worktree. The fix needs `IConsentService`'s query methods to take the current tenant (and module) from `IRequestContext`/`ITenantContext` and apply it as a query predicate, plus a two-tenant isolation test analogous to the pattern SPEC-002 REQ-061/P-54 describes for the rest of the SPEC-002 scope.
