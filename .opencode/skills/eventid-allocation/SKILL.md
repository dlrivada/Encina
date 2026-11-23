---
name: eventid-allocation
description: Manages EventId allocation for Encina structured logging — registers ranges in EventIdRanges.cs, creates *LogMessages.cs files, prevents sparses and overlaps. Triggers when adding [LoggerMessage] or structured logging to a feature.
---

# EventId Allocation Skill

Manages EventId allocation for Encina's structured logging system. Prevents collisions, sparses, and range violations.

## Central Registry

- **Location**: `src/Encina/Diagnostics/EventIdRanges.cs`
- **Format**: `public static readonly (int Min, int Max) RangeName = (min, max);`
- **Discovery**: `EventIdRanges.GetAllRanges()` returns all registered allocations

## Current Range Map

| Area | Range | Packages |
|------|-------|----------|
| Core | 1-199 | Sanitization (1-99), Encina core: mediator, streaming, sharding (100-199) |
| Web / testing integrations | 200-299 | AspNetCore (200-249), Testing (250-299) |
| DomainModeling | 1100-1699 | Repository, UoW, Bulk, Spec, SoftDelete, Audit |
| Security Audit | 1700-1799 | Read Audit |
| Infrastructure | 1800-1999 | Tenancy, Module Isolation |
| Messaging stores | 2000-2499 | Outbox, Inbox, Saga, Scheduling, QueryCache, Encryption |
| Domain Events / ES | 2500-2799 | DomainEvents, AuditMarten, Marten (2600-2799) |
| Messaging runtime / data access | 2800-3499 | Messaging (2800-2999), EF Core, MongoDB, ADO.NET ×3, Dapper ×3 |
| Caching / locks | 3500-3899 | Caching, Memory, Redis, Hybrid, DistributedLock ×3 |
| Resilience / scheduling adapters | 3900-4099 | Polly, Extensions.Resilience, Hangfire, Quartz |
| Transports / API integrations | 4100-4699 | RabbitMQ, Kafka, NATS, MQTT, AzureServiceBus, AmazonSQS, Redis.PubSub, InMemory, gRPC, GraphQL, SignalR, Refit |
| Serverless | 4700-4799 | AwsLambda, AzureFunctions |
| CDC | 4800-4999 | Cdc, Cdc.SqlServer, Cdc.Debezium |
| Security runtime | 5000-5399 | Security.Audit, Security.Secrets and its four providers |
| Observability | 7000-7099 | OpenTelemetry |
| Security | 8000-8099 | Security (8000-8009), PII (8010-8029), IdGen (8030-8099) |
| Compliance | 8100-8949 | GDPR, Consent, DSR, LawfulBasis, Anonymization, CryptoShredding, Retention, DataResidency, BreachNotification, DPIA, PrivacyByDesign |
| Security Extensions | 9000-9199 | ABAC, AntiTampering |
| Compliance Extensions | 9200-9699 | NIS2, CrossBorderTransfer, ProcessorAgreements, AIAct, Attestation |
| Later additions | 3850-3899, 5400-5499, 9750-9799 and others | Registered after this table was written; verify each against `EventIdRanges.cs` before naming it. [The block registry](../../../docs/engineering/eventid-block-registry.md) lists every range |

The ranges below 10000 are historical: this table is the original allocation, there is no free-range list, free gaps below 10000 are not listed in the registry and are not allocatable, and the registry file is the place to read free space only for the 10000+ zone. New blocks are born in the 10000+ zone (ADR-021 amendment, #4006), one base per area, 1000 ids per block, each area owning ten blocks (base to base+9999):

| Area | Base | Current block |
|------|------|---------------|
| Core and messaging | 10000 | 10000-10999 |
| Database | 20000 | 20000-20999 |
| Caching (and distributed locks) | 30000 | 30000-30999 |
| Transports | 40000 | 40000-40999 |
| Security and compliance | 50000 | 50000-50999 |
| Observability and platform | 60000 | 60000-60999 |
| Testing and tooling | 70000 | 70000-70999 |
| Cloud and integration | 80000 | 80000-80999 |

A new area takes the next base (90000) by amending the ADR table.

## Allocation Workflow

When adding structured logging to a feature:

1. **Pick** the next free 50 or 100 id slot in the area's current 1000 block (open the next block when it is full); read the free space of the 10000+ zone from `docs/engineering/eventid-block-registry.md` (nothing below 10000 is allocatable)
2. **Register** a new `public static readonly (int Min, int Max) RangeName = (min, max);` in `EventIdRanges.cs` and add its row to the registry file (Registered ranges and Blocks tables) in the same pull request
3. **Create** `Diagnostics/*LogMessages.cs` with EventIds **within** the registered range
4. **Update** `PublicAPI.Unshipped.txt` for the new public field
5. **Add** the assembly to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. **Run** architecture tests (`EventIdUniquenessRule` via `EncinaEventIdAllocationTests`); every `[LoggerMessage]` needs an explicit EventId
6. **Run** `dotnet test tests\Encina.UnitTests --filter "FullyQualifiedName~EventIdRegistryAgreementTests"`

## Rules

- ❌ NEVER use an EventId without registering its range first
- ❌ NEVER assign EventIds outside their registered range
- ❌ NEVER create sparses with large gaps (>10) — pack sequentially
- ✅ Use `[LoggerMessage]` source generator (NOT `LoggerMessage.Define`)
- ✅ Group EventIds by functional area within range
- ✅ Add XML doc comments referencing the range

## Architecture Tests

`Encina.Testing.Architecture.EventIdUniquenessRule` provides:

| Method | What It Checks |
|--------|---------------|
| `AssertEventIdsAreGloballyUnique()` | No duplicate EventIds across assemblies |
| `AssertEventIdsWithinRegisteredRanges()` | Every EventId within its registered range |
| `AssertNoRangeOverlaps()` | No two registered ranges overlap |
| `GenerateAllocationReport()` | Creates human-readable table |

## Example

```csharp
// EventIds registered in EventIdRanges.cs
public static readonly (int Min, int Max) OutboxStore = (2000, 2049);

// LogMessages.cs using EventIds in range
public static partial class LogMessages
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information)]
    public static partial void MessageAdded(this Logger logger, Guid messageId);
}
```
