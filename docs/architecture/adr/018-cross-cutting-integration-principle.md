---
title: "ADR-018: Cross-Cutting Integration Principle"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-018: Cross-Cutting Integration Principle

**Status:** Accepted
**Date:** 2026-03-10
**Deciders:** David Lozano Rivada
**Technical Story:** [#758 - Cross-Cutting Transversal Function Integration EPIC](https://github.com/dlrivada/Encina/issues/758)

## Context

Encina provides 12 transversal (cross-cutting) functions that infrastructure subsystems can integrate with:

1. Caching (`ICacheProvider`, decorator pattern)
2. OpenTelemetry (`ActivitySource`, `Meter`)
3. Structured Logging (`[LoggerMessage]`, `Log.cs`)
4. Health Checks (`IEncinaHealthCheck`)
5. Validation (`IValidationProvider`, pipeline)
6. Resilience (retry, circuit breaker, timeout)
7. Distributed Locks (`IDistributedLockProvider`)
8. Transactions (`IUnitOfWork`, atomicity)
9. Idempotency (`InboxPipelineBehavior`, dedup)
10. Multi-Tenancy (`TenantId`, `ITenantContext`)
11. Module Isolation (`ModuleId`, `IModuleContext`)
12. Audit Trail (`IAuditStore`, audit events)

During the ABAC feature implementation (v0.12.0), we discovered that the caching infrastructure existed but was not integrated into most subsystems. A comprehensive audit revealed this pattern extended across all 12 transversal functions, resulting in **67 integration gaps** (18 caching + 49 others).

### Root Cause

The gaps were systemic, not accidental. None of our development references — CLAUDE.md, issue templates, plan prompts, PR templates, or code review configuration — included a checkpoint asking: *"Does this feature integrate with existing transversal functions?"*

Each subsystem was developed with its primary concern in focus (messaging stores focus on persistence, saga focuses on orchestration, etc.), but without a systematic reminder to connect with the cross-cutting infrastructure that already existed.

### Examples of Discovered Gaps

| Subsystem | Missing Integration | Risk |
|-----------|-------------------|------|
| Outbox processing | No distributed locks | Duplicate message publishing in multi-instance |
| ADO.NET outbox | No transaction atomicity | Lost/phantom events |
| Saga execution | No distributed locks | Concurrent state corruption |
| ADO.NET/Dapper providers | No OpenTelemetry | Invisible in production |
| Message transports | No resilience | Zero retry on broker failure |
| OutboxMessage/SagaState | No TenantId field | No tenant isolation |
| Background processors | No audit trail | Operations unaudited |

## Decision

Adopt a **mandatory cross-cutting integration evaluation** for every new feature in Encina.

### The Rule

Every new feature, subsystem, or provider implementation MUST be evaluated against ALL 12 transversal functions before the feature is considered complete. This evaluation must be:

1. **Documented** — In the issue (feature request or epic) using the Cross-Cutting Integration matrix
2. **Reviewed** — In pull requests via the cross-cutting checklist
3. **Enforced** — By code review tooling (CodeRabbit) checking for common misses

### Integration Points

The principle is embedded in the following development artifacts:

| Artifact | Integration |
|----------|-------------|
| `CLAUDE.md` | "Cross-Cutting Integration Rule (MANDATORY)" section |
| `.github/ISSUE_TEMPLATE/feature_request.md` | 12-row integration matrix table |
| `.github/ISSUE_TEMPLATE/epic.md` | Cross-cutting coverage table for child issues |
| `.github/pull_request_template.md` | Cross-cutting verification checklist |
| `.coderabbit.yaml` | Path instructions for automated review |
| `CONTRIBUTING.md` | Checklist item for contributors |
| Plan prompts | Research step + plan section for implementation planning |

### Evaluation Outcomes

For each transversal function, the evaluation must result in one of:

- **Included** — Integration implemented in this feature
- **Deferred** — Tracked as a separate GitHub Issue with rationale
- **N/A** — Not applicable, with documented justification

"Not evaluated" is never acceptable.

## Consequences

### Positive

- **Prevents integration gaps at design time** rather than discovering them post-implementation
- **Creates institutional memory** — New contributors automatically learn about transversal functions
- **Provides audit trail** — Every feature has a documented integration evaluation
- **Reduces technical debt accumulation** — Gaps are tracked as issues immediately, not forgotten
- **Improves production readiness** — Features ship with observability, resilience, and proper isolation

### Negative

- **Adds overhead to issue creation** — Every feature request requires filling the 12-row matrix
- **May slow down initial development** — More checkboxes before a feature is "done"
- **Requires maintenance** — If new transversal functions are added (beyond 12), all templates must be updated

### Neutral

- The evaluation doesn't require implementing all 12 integrations — just evaluating and documenting the decision
- Deferred integrations are acceptable as long as they're tracked as issues

## Alternatives Considered

### 1. Automated Static Analysis

Use Roslyn analyzers to detect missing integrations (e.g., a store without `ActivitySource`).

**Rejected because:** Too rigid, high false-positive rate, and cannot detect architectural gaps (like missing `TenantId` fields). Better suited as a complement, not a replacement.

### 2. Post-Implementation Audit Only

Periodically audit the codebase for integration gaps (like we did for this EPIC).

**Rejected because:** Reactive rather than preventive. The ABAC audit found 67 gaps — preventing them is far cheaper than fixing them after the fact.

### 3. No Formal Process

Trust developers to remember cross-cutting concerns.

**Rejected because:** This is exactly what led to 67 gaps. The knowledge exists but wasn't systematically applied.

## Related

- [EPIC #712 — Cross-Cutting Cache Integration](https://github.com/dlrivada/Encina/issues/712) (18 caching issues)
- [EPIC #758 — Cross-Cutting Transversal Function Integration](https://github.com/dlrivada/Encina/issues/758) (49 issues across 11 functions)
- [ADR-003 — Caching Strategy](003-caching-strategy.md)
