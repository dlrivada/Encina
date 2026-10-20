# Implementation Plan: `Encina.Example` — Minimal conforming plan

> **Issue**: self-test fixture
> **Status**: fixture for `tools/ai/plans/check-plan.ps1 -SelfTest`; it satisfies every check

---

## Summary

Minimal plan that follows the structure of the implementation plan prompt. **Provider category**: none.

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

- Keeps the core small.

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

- Immutability fits the domain.

</details>

<details>
<summary><strong>3. Store Pattern — In-memory default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory default** | Zero setup | Not durable |
| **B) Database only** | Durable | Setup cost |

### Chosen Option: **A — In-memory default**

### Rationale

- Pay for what you use.

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

1. (2026-10-06) Package Placement: option A, a new package.
2. (2026-10-06) Domain Model: option A, sealed records.
3. (2026-10-06) Store Pattern: option A, in-memory default.
4. Configuration: option A, an options class, after the maintainer
   weighed it against the fluent-only option in the chat session.
   2026-10-06 is the date of that decision.
5. Not a Design Choice number of this plan, only a sixth line
   2026-10-07 that must not hide entry 4's date.
