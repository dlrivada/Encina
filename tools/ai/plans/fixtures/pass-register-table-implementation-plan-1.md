# Implementation Plan: `Encina.Example` — No open design choices

> **Issue**: self-test fixture
> **Status**: fixture for `tools/ai/plans/check-plan.ps1 -SelfTest`; zero open choices, the settled ones are listed in a register table

---

## Summary

Minimal plan with no open design choices. **Provider category**: none.

---

## Design Choices

No open design choices. Every choice below is Settled or Trivial, each with its citation and the adversarial mark; the maintainer can reopen any line, which turns it into a Dilemma.

| Id | Choice | Class | Settled by | Verified |
|----|--------|-------|------------|----------|
| D1 | New package instead of extending core | Settled | AGENTS.md section 1 (pay-for-what-you-use) | adversarial-reviewer 2026-10-17 |
| D2 | Sealed records for the model | Trivial | AGENTS.md section 4 (naming) | adversarial-reviewer 2026-10-17 |
| D3 | Options class for configuration | Settled | ADR-018 | adversarial-reviewer 2026-10-17 |

---

## Implementation Phases

### Phase 1: Core Models

<details>
<summary><strong>Tasks</strong></summary>

1. **`Example.cs`** — `Encina.Example`
   - Sealed record `Example(string Name)`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Example (Issue #1).

CONTEXT:
- New package src/Encina.Example/

TASK:
Create the `Example` record.

KEY RULES:
- XML documentation on every public type

REFERENCE FILES:
- src/Encina/Either.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| Example-1 | Example rule | Fixture only |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `Either<L, R>` | `Encina` core | Return type |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Example` | 9990-9999 | Fixture only |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core | ~1 | One record |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 library.

IMPLEMENTATION OVERVIEW:
Phase 1: the `Example` record.

KEY PATTERNS:
- Either<EncinaError, T> everywhere.

REFERENCE FILES:
- src/Encina/Either.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: no reads |
| 2 | OpenTelemetry | ✅ | Phase 1 |
| 3 | Structured Logging | ✅ | Phase 1 |
| 4 | Health Checks | ❌ | No dependency to check |
| 5 | Validation | ✅ | Phase 1 |
| 6 | Resilience | ❌ | No external calls |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | Single operation |
| 9 | Idempotency | ❌ | No duplicates |
| 10 | Multi-Tenancy | ⏭️ | Deferred to #2 |
| 11 | Module Isolation | ❌ | Not modular |
| 12 | Audit Trail | ❌ | No compliance impact |

---

## Next Steps

1. Review and approve this plan.
