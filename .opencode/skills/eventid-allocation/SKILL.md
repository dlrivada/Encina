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
| Core | 1-99 | Sanitization |
| DomainModeling | 1100-1699 | Repository, UoW, Bulk, Spec, SoftDelete, Audit |
| Security Audit | 1700-1799 | Read Audit |
| Infrastructure | 1800-1999 | Tenancy, Module Isolation |
| Messaging | 2000-2499 | Outbox, Inbox, Saga, Scheduling, QueryCache, Encryption |
| Domain Events / ES | 2500-2699 | DomainEvents, AuditMarten |
| Security | 8000-8099 | Security (8000-8009), PII (8010-8029), IdGen (8030-8099) |
| Compliance | 8100-8949 | GDPR, Consent, DSR, LawfulBasis, Anonymization, CryptoShredding, Retention, DataResidency, BreachNotification, DPIA, PrivacyByDesign |
| Security Extensions | 9000-9199 | ABAC, AntiTampering |
| Compliance Extensions | 9200-9499 | NIS2, CrossBorderTransfer, ProcessorAgreements |
| Reserved | 9500-9999 | Future modules |

## Allocation Workflow

When adding structured logging to a feature:

1. **Check** `EventIdRanges.cs` for next free range in the appropriate area
2. **Register** a new `public static readonly (int Min, int Max) RangeName = (min, max);` with appropriate size (typically 50-100 slots)
3. **Create** `Diagnostics/*LogMessages.cs` with EventIds **within** the registered range
4. **Update** `PublicAPI.Unshipped.txt` for the new public field
5. **Run** architecture tests (EventIdUniquenessRule)

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
