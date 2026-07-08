---
name: cross-cutting-check
description: Checks that new features integrate with ALL 12 cross-cutting functions (Caching, OpenTelemetry, Logging, Health, Validation, Resilience, Distributed Locks, Transactions, Idempotency, Multi-Tenancy, Module Isolation, Audit). Triggers on new store, entity, pipeline behavior, background service, or external integration.
---

# Cross-Cutting Integration Skill

Ensures every new feature in Encina is evaluated against ALL 12 cross-cutting (transversal) functions.

> **CRITICAL**: Missing integrations create invisible gaps that compound over time.

## The 12 Transversal Functions

| # | Function | Key Question | Integration Pattern |
|---|----------|-------------|-------------------|
| 1 | **Caching** | Does this feature read data that benefits from caching? | `ICacheProvider`, decorator, `[Cache]` |
| 2 | **OpenTelemetry** | Does this feature perform operations worth tracing/metering? | `ActivitySource`, `Meter`, semantic attributes |
| 3 | **Structured Logging** | Does this feature need operational visibility? | `Log.cs` with `[LoggerMessage]`, EventId range |
| 4 | **Health Checks** | Does this feature have a health-checkable dependency? | `IEncinaHealthCheck` implementation |
| 5 | **Validation** | Does this feature receive input that needs validation? | `IValidationProvider`, pipeline behavior |
| 6 | **Resilience** | Does this feature call external systems that can fail? | Polly retry, circuit breaker, timeout |
| 7 | **Distributed Locks** | Does this feature have concurrent access to shared state? | `IDistributedLockProvider` |
| 8 | **Transactions** | Does this feature need atomic multi-operation guarantees? | `IUnitOfWork`, `TransactionPipelineBehavior` |
| 9 | **Idempotency** | Can this feature receive duplicate requests? | `InboxPipelineBehavior`, deduplication key |
| 10 | **Multi-Tenancy** | Does this feature store/query data that belongs to a tenant? | `TenantId` field, `ITenantContext` |
| 11 | **Module Isolation** | In modular monolith, does this feature need module scoping? | `ModuleId` field, `IModuleContext` |
| 12 | **Audit Trail** | Does this feature perform operations with compliance/security implications? | `IAuditStore`, audit events |

## Per-Function Outcomes

For each of the 12 functions, determine:

- ✅ **Integrate** — Implement the integration in the current feature
- ⏭️ **Defer** — Create a GitHub Issue for future integration (reference in plan/PR)
- ❌ **Not Applicable** — Document WHY in 1 sentence (plan or PR description)

## Common Misses (What This Skill Prevents)

| New Feature | Commonly Missed Integrations |
|-------------|------------------------------|
| New store/entity | OpenTelemetry, TenantId, ModuleId, Health Check |
| New background service | Distributed Locks, Leader Election, Structured Logging |
| New external integration | Resilience (retry/circuit breaker), Health Check, OpenTelemetry |
| New pipeline behavior | Validation, Idempotency, Audit Trail |
| New messaging pattern | Transactions, Distributed Locks, Multi-Tenancy |

## How to Check

When implementing a new feature:

1. **List** the 12 functions explicitly
2. **Evaluate** each against the feature
3. **Apply** the correct outcome (✅, ⏭️, ❌)
4. **Document** in PR description or plan

Example checklist in PR:
```
## Cross-Cutting Integration (12/12)
- [x] Caching — Implemented via `ICacheProvider`
- [x] OpenTelemetry — `ActivitySource` added on all DB operations
- [x] Logging — `[LoggerMessage]` EventIds in `Diagnostics/LogMessages.cs`
- [x] Health Check — Implements `IEncinaHealthCheck`
- [x] Validation — Pipeline behavior registered
- [x] Resilience — Polly circuit breaker on external calls
- [x] Distributed Locks — Used for concurrent processing
- [x] Transactions — `IUnitOfWork` ensures atomicity
- [x] Idempotency — `InboxPipelineBehavior` enabled
- [x] Multi-Tenancy — `TenantId` field added
- [x] Module Isolation — `ModuleId` field added
- [x] Audit Trail — `IAuditStore` records operations
```

## When to Trigger

- Every new entity or store creation
- New pipeline behavior implementation
- New background service
- New external integration point
- Service registration in `ServiceCollectionExtensions`
