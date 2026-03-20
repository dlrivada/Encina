# ADR-021: EventId Uniqueness Enforcement — Central Registry + Architecture Test

## Status

**Accepted** (March 2026)

## Context

.NET's `[LoggerMessage]` source generator assigns `EventId` values to structured log messages. The compiler warning `SYSLIB1006` only detects duplicate EventIds **within a single class**, not across classes in the same assembly or across assemblies. In a framework with 40+ packages, each with its own log messages, undetected EventId collisions are inevitable without a centralized system.

**Problems discovered (EPIC #668, Phase 4d):**

- Multiple packages independently chose the same EventId ranges (e.g., Security, SecurityPII, and IdGeneration all started at 8000)
- Adjacent packages drifted into each other's ranges (e.g., Anonymization overflowed from 8400-8449 into CryptoShredding's 8450-8499)
- Internal cross-class duplicates within the same assembly (e.g., GDPR's `GDPRLogMessages` and `LawfulBasisLogMessages` both used 8200+)
- No tooling existed to catch these at build or test time

**Impact of collisions:** When two log entries share the same EventId, log aggregation tools (ELK, Azure Monitor, Datadog) cannot distinguish between them, making filtering, alerting, and root-cause analysis unreliable.

## Decision

### 1. Central Registry (`EventIdRanges.cs`)

A single source of truth for all EventId range allocations:

- **Location**: `src/Encina/Diagnostics/EventIdRanges.cs`
- **Format**: `public static readonly (int Min, int Max) RangeName = (min, max);`
- **Discovery**: `GetAllRanges()` method returns all registered ranges via reflection
- **Rule**: Every package MUST register its range here before using EventIds

### 2. Architecture Test (`EventIdUniquenessRule`)

Automated enforcement via architecture tests:

- **Location**: `src/Encina.Testing.Architecture/EventIdUniquenessRule.cs`
- **Validations**:
  - `AssertEventIdsAreGloballyUnique()` — No two `[LoggerMessage]` methods across all assemblies share the same EventId
  - `AssertEventIdsWithinRegisteredRanges()` — Every EventId falls within its assembly's registered range
  - `AssertNoRangeOverlaps()` — No two registered ranges overlap
  - `GenerateAllocationReport()` — Human-readable allocation table with usage statistics

### 3. Range Allocation Policy

| Area | Range | Notes |
|------|-------|-------|
| Core | 1-99 | Sanitization |
| DomainModeling | 1100-1699 | Repository, UoW, Bulk, Spec, SoftDelete, Audit |
| Security Audit | 1700-1799 | Read audit |
| Infrastructure | 1800-1999 | Tenancy, Module Isolation |
| Messaging | 2000-2499 | Outbox, Inbox, Saga, Scheduling, QueryCache, Encryption |
| Domain Events / ES | 2500-2699 | DomainEvents, AuditMarten |
| Security | 8000-8099 | Security (8000-8009), PII (8010-8029), IdGen (8030-8099) |
| Compliance | 8100-8949 | GDPR, Consent, DSR, LawfulBasis, Anonymization, CryptoShredding, Retention, DataResidency, BreachNotification, DPIA, PrivacyByDesign |
| Security Extensions | 9000-9199 | ABAC, AntiTampering |
| Compliance Extensions | 9200-9499 | NIS2, CrossBorderTransfer, ProcessorAgreements |
| Reserved | 9500-9999 | Future modules |

### 4. Allocation Workflow for New Features

1. **Check** `EventIdRanges.cs` for the next free range in the appropriate area
2. **Register** a new field with an appropriate size (typically 50 or 100 slots)
3. **Create** your `*LogMessages.cs` file with EventIds within the registered range
4. **Update** `PublicAPI.Unshipped.txt` if the range field is public
5. **Run** architecture tests to verify no collisions or range violations

## Consequences

### Positive

- **Collisions are impossible** when the workflow is followed — architecture tests catch violations
- **Self-documenting** — `EventIdRanges.cs` is the single source of truth, readable by humans and code
- **Scalable** — supports 40+ packages with room for growth (9500-9999 reserved)
- **Automated** — no manual auditing needed; tests enforce compliance

### Negative

- **Manual registration** — developers must remember to register ranges (mitigated by CLAUDE.md instructions and architecture tests)
- **Range estimation** — choosing range size upfront requires estimation (mitigated by using larger ranges when uncertain)

### Risks

- If architecture tests are not run, collisions can still be introduced (mitigated by CI/CD enforcement)

## Related Issues

- #828 — Initial EventIdRanges.cs creation
- #829 — EventIdUniquenessRule + architecture test
- #830 — Security/PII/IdGeneration collision fix
- #831 — QueryCache/MessagingEncryption boundary fix
- #832 — DomainEvents/AuditMarten boundary fix
- #833 — GDPR/Consent boundary fix
- #834 — Anonymization/CryptoShredding boundary fix
- #835 — GDPR internal duplicates (resolved by #833)
- EPIC #668 — Phase 4d: EventId Uniqueness Enforcement

## Date

2026-03-20
