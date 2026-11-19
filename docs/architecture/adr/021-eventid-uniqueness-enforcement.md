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
| Later additions | 3850-3899, 5400-5499, 9750-9799 and others | Registered after this table was written; `EventIdRanges.cs` and [the block registry](../../engineering/eventid-block-registry.md) list every range |

The table above is the original 2026-03 allocation and its "Free" row is withdrawn: the free space it named has been reserved by plans or registered. New blocks follow the 10000+ zone of the amendment below.

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
- **Scalable** — supports 40+ packages; room for growth is the 10000+ zone (see the 2026-11-20 amendment)
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

## Amendment (2026-11-20, #4006): the 10000+ zone

The ids 1-9999 are nearly exhausted: 91 ranges are registered in `EventIdRanges.cs`, the five free blocks below 10000 that are not in the registry (5970-5999, 6380-6399, 7950-7999, 8950-8999, 9950-9999) sum to 200 ids, and the estimate that the plans written for the 1.0 scope need about 2500-3000 comes from the #3387 plan batch, not from the registry. The registry covers reserved and registered blocks, so free space below 10000 is not read from it. The maintainer chose on 2026-11-18 (option D) to open a second id space instead of squeezing blocks into the gaps. From this amendment on, every new EventId block is born in a 10000+ zone, one base per area, in blocks of 1000 ids, in the style of EF Core's [`CoreEventId`](https://github.com/dotnet/efcore/blob/main/src/EFCore/Diagnostics/CoreEventId.cs).

### Areas

Each area owns ten 1000-id blocks (`base` to `base + 9999`). The "Current block" column is where the next package range of the area is taken.

| Area | Base | Covers | Current block |
| --- | --- | --- | --- |
| Core and messaging | 10000 | Mediator, messaging patterns, domain modeling, tenancy, modules, event sourcing, CDC | 10000-10999 |
| Database | 20000 | ADO.NET, Dapper, EF Core and MongoDB providers | 20000-20999 |
| Caching | 30000 | Cache providers and patterns, distributed locks | 30000-30999 |
| Transports | 40000 | Message transports and their API integrations | 40000-40999 |
| Security and compliance | 50000 | Security, secrets, audit, compliance modules | 50000-50999 |
| Observability and platform | 60000 | OpenTelemetry, ASP.NET Core, resilience, scheduling adapters, health checks, platform features | 60000-60999 |
| Testing and tooling | 70000 | Testing packages and developer tooling | 70000-70999 |
| Cloud and integration | 80000 | Serverless and cloud service integrations | 80000-80999 |

### Rules

- **Pick rule.** Take the next free 50 or 100 id slot (the size a package range usually needs) in the area's current 1000 block. When that block is full, open the next 1000 block of the area (for example 11000-11999) and update the "Current block" column in the same pull request.
- **One pull request.** Registering the range in `EventIdRanges.cs` and adding the row to [the block registry](../../engineering/eventid-block-registry.md) happen together.
- **New area.** A new area takes the next free base (90000) by amending the area table above.
- **Historical ranges stay.** The ranges registered below 10000 stay where they are, and so do the blocks that plans reserved there before this amendment (status `reserved-pre-zone` in the registry); none is rewritten. Moving them into the scheme is the separate refactoring issue #4007, which this amendment blocks. After this amendment no new block may be created below 10000.
- **Enforcement.** `EventIdUniquenessRule` has no upper bound: the rule has no upper bound, and `EventIdRegistryAgreementTests` includes a test that shows an id such as 10001 is extracted and is not flagged by the uniqueness rule. The assembly map of `EncinaEventIdAllocationTests` works unchanged.

### The block registry

[`docs/engineering/eventid-block-registry.md`](../../engineering/eventid-block-registry.md) is a tracked file with two machine-read tables: the registered ranges (name, bounds, target area, zone) and the blocks plans have reserved (bounds, area, owner, status). `tests/Encina.UnitTests/Testing/Architecture/EventIdRegistryAgreementTests.cs` keeps it in agreement with `src/Encina/Diagnostics/EventIdRanges.cs` through five rules:

1. Every range in `EventIdRanges.cs` appears in the registered table with the same bounds.
2. Every registry row with status `registered` matches a range in `EventIdRanges.cs` with the same bounds.
3. Reserved blocks (`reserved` and `reserved-pre-zone`) do not overlap any registered range.
4. Reserved blocks do not overlap each other.
5. Nothing is below 10000 unless it is a historical registered range or a block with status `reserved-pre-zone`.

The registry replaces the git-ignored working snapshot the plan batch used, and the free-range list that `AGENTS.md` section 7 carried: free space is read from the registry, never from a list typed into a rule file.

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
- #4006 — The 10000+ zone and the block registry (amendment above)
- #4007 — Migration of the historical ranges into the zone scheme

## Date

2026-03-20
