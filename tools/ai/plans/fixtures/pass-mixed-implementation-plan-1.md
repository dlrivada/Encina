# Implementation Plan: `Encina.Example` — Settled choices and one Dilemma

> **Issue**: self-test fixture
> **Status**: fixture for `tools/ai/plans/check-plan.ps1 -SelfTest`; choices 1, 2 and 4 are Settled or Trivial, choice 3 is a Dilemma answered in `## Maintainer Decisions`

---

## Summary

Minimal plan with one real Dilemma. **Provider category**: none.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package** | Clean separation | One more package |
| **B) Extend core** | No new package | Bloats core |

### Chosen Option: **A — New package**

### Rationale

- Every feature is opt-in.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use)
**Verified:** adversarial-reviewer 2026-10-17

</details>

<details>
<summary><strong>2. Domain Model — Records</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Sealed records** | Immutable | No inheritance |
| **B) Classes** | Flexible | Mutable |

### Chosen Option: **A — Sealed records**

### Rationale

- Entities are named per the naming rules.

Class: Trivial
Settled by: AGENTS.md section 4 (naming)
Verified: adversarial-reviewer 2026-10-17

</details>

<details>
<summary><strong>3. Retention — How long messages are kept</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 30 days** | Small storage | Loses audit history for a late dispute |
| **B) 1 year** | Covers an annual audit | Larger storage |

### Chosen Option: **B — 1 year**

### Rationale

- Each option wins in a different scenario: A for a high-volume tenant, B for a regulated tenant.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Configuration — Options class</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Options class** | Standard | More types |
| **B) Fluent only** | Short | Hard to bind |

### Chosen Option: **A — Options class**

### Rationale

- Binds from configuration.

**Class:** Settled
**Settled by:** ADR-018 (cross-cutting integration)
**Verified:** adversarial-reviewer 2026-10-17

</details>

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

---

## Maintainer Decisions

3. (2026-10-17) Retention: option B, 1 year.
