---
title: "ADR-021: EventId Uniqueness Enforcement — Central Registry + Architecture Test"
layout: default
parent: ADRs
grand_parent: Architecture
---

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
  - `AssertEveryLoggerMessageHasEventId()` — Every `[LoggerMessage]` declares an explicit EventId
  - `AssertEventIdsAreGloballyUnique()` — No two `[LoggerMessage]` methods share an EventId, whether in the same assembly or in different ones
  - `AssertEventIdsWithinRegisteredRanges()` — Every EventId falls within one of the ranges mapped to its assembly (a package may own several)
  - `AssertNoRangeOverlaps()` — No two registered ranges overlap
  - `GenerateAllocationReport()` — Human-readable allocation table with usage statistics
- **Solution-wide test**: `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` loads every shipped `Encina*` assembly from the test output and applies the three validations. Its `AssemblyRanges` field is the assembly → range-name map; a package that starts logging must be added there, and the test fails when a `src/` package with `[LoggerMessage]` methods is not scanned.
- **Scope**: `[LoggerMessage]` attributes, reflected from the compiled assemblies, plus `LoggerMessage.Define` EventIds (#1125): it scans `src/<Package>/**/*.cs` for literal `new EventId(<n>, ...)` allocations (lines starting with `//` are skipped) and requires one per `LoggerMessage.Define` call. Both are checked for uniqueness and for falling inside the ranges registered for their package.

### 3. Range Allocation Policy

| Area | Range | Notes |
|------|-------|-------|
| Core | 1-199 | Sanitization (1-99), Encina core: mediator, streaming, sharding (100-199) |
| Web / testing integrations | 200-299 | AspNetCore (200-249), Testing (250-299) |
| DomainModeling | 1100-1699 | Repository, UoW, Bulk, Spec, SoftDelete, Audit |
| Security Audit | 1700-1799 | Read audit |
| Infrastructure | 1800-1999 | Tenancy, Module Isolation |
| Messaging stores | 2000-2499 | Outbox, Inbox, Saga, Scheduling, QueryCache, Encryption |
| Domain Events / ES | 2500-2799 | DomainEvents, AuditMarten, Marten |
| Messaging runtime and data access | 2800-3499 | Messaging (2800-2999), EF Core, MongoDB, ADO.NET ×3, Dapper ×3 |
| Caching and locks | 3500-3899 | Caching, Caching.Memory, Caching.Redis, Caching.Hybrid, DistributedLock ×3 |
| Resilience and scheduling adapters | 3900-4099 | Polly, Extensions.Resilience, Hangfire, Quartz |
| Transports and API integrations | 4100-4699 | RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub, InMemory, gRPC, GraphQL, SignalR, Refit |
| Serverless | 4700-4799 | AwsLambda, AzureFunctions |
| Change data capture | 4800-4999 | Cdc, Cdc.SqlServer, Cdc.Debezium |
| Security runtime | 5000-5399 | Security.Audit, Security.Secrets and its four providers |
| Observability | 7000-7099 | OpenTelemetry |
| Security | 8000-8099 | Security (8000-8009), PII (8010-8029), IdGen (8030-8099) |
| Compliance | 8100-8949 | GDPR, Consent, DSR, LawfulBasis, Anonymization, CryptoShredding, Retention, DataResidency, BreachNotification, DPIA, PrivacyByDesign |
| Security Extensions | 9000-9199 | ABAC, AntiTampering |
| Compliance Extensions | 9200-9699 | NIS2, CrossBorderTransfer, ProcessorAgreements, AIAct, Attestation |
| Free | 300-1099, 5400-6999, 7100-7999, 8950-8999, 9700-9999 | Future modules |

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
- **Scalable** — supports 40+ packages with room for growth (9700-9999 reserved)
- **Automated** — no manual auditing needed; tests enforce compliance

### Negative

- **Manual registration** — developers must remember to register ranges (mitigated by CLAUDE.md instructions and architecture tests)
- **Range estimation** — choosing range size upfront requires estimation (mitigated by using larger ranges when uncertain)

### Risks

- If architecture tests are not run, collisions can still be introduced (mitigated by CI/CD enforcement)

## Amendment (2026-09-22, #1120)

The rule shipped with its own unit tests but no test applied it to the Encina assemblies, and `AssertEventIdsAreGloballyUnique` ignored duplicates inside one assembly. When the solution-wide test was added, it found that 45 of the 69 packages with `[LoggerMessage]` methods had EventIds outside any registered range: most transports, caching, locking, resilience, scheduling, CDC and provider packages used unregistered ids starting at 1. 115 EventIds were each used by several packages (one id by 33), eight were duplicated only inside one package (seven in `Encina.Cdc`, one in `Encina.Messaging`), and 13 `Encina.Messaging` dead-letter messages declared no EventId, so the source generator derived one from a hash of the method name. The fix:

- `AssertEventIdsAreGloballyUnique` also reports duplicates within one assembly; `AssertEventIdsWithinRegisteredRanges` takes a list of ranges per assembly; the new `AssertEveryLoggerMessageHasEventId` rejects `[LoggerMessage]` methods without an explicit EventId.
- 44 ranges were registered (table above) and 754 EventIds were renumbered into them, one contiguous block per source file, after the ids the package already had in that range; ids already inside a range of their package were kept. The dead-letter messages received 2945-2957. `SecuritySecrets` moved from 8950-8999 to 5100-5199 because the package's 51 ids did not fit in 50 slots.
- `EncinaEventIdAllocationTests` enforces the map on every run of `Encina.UnitTests`.

EventIds are not a stable contract before 1.0; dashboards or alerts that filter on the old numbers must be updated.

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
- #1120 — Solution-wide enforcement and renumbering (amendment above)
- #1050 — OpenTelemetry range
- #1125 — `LoggerMessage.Define` EventIds

## Date

2026-03-20
