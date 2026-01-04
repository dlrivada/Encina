# Phase 3: Test Data Generation Checklist

**Goal**: Replace `new Faker<T>()` with `new EncinaFaker<T>()` for reproducible test data.

**Estimated Effort**: 1-3 hours per project

---

## Pre-Migration Checklist

- [ ] Phase 2 completed
- [ ] Run `dotnet test` to establish baseline
- [ ] Create branch: `feature/testing-dogfooding-phase3`

## Find and Replace Patterns

### Pattern 1: Basic Faker Replacement

**Search for**:
```csharp
var faker = new Faker<Order>();
faker.RuleFor(o => o.CustomerId, f => f.Random.Guid().ToString());
```

**Replace with**:
```csharp
var faker = new EncinaFaker<Order>();
faker.RuleFor(o => o.CustomerId, f => f.Random.UserId("customer"));
```

### Pattern 2: Domain-Specific IDs

**Search for**:
```csharp
f.Random.Guid().ToString()
f.Random.AlphaNumeric(10)
```

**Replace with**:
```csharp
f.Random.CorrelationId()     // For correlation IDs
f.Random.UserId("prefix")     // For user IDs
f.Random.TenantId("prefix")   // For tenant IDs
f.Random.IdempotencyKey()     // For idempotency keys
```

### Pattern 3: Entity IDs

**Search for**:
```csharp
Guid.NewGuid()
f.Random.Int(1, 10000)
```

**Replace with**:
```csharp
f.Random.GuidEntityId()       // For GUID-based IDs
f.Random.IntEntityId()        // For int-based IDs
f.Random.StringEntityId(8, "ORD")  // For string IDs with prefix
```

### Pattern 4: UTC Dates

**Search for**:
```csharp
f.Date.Recent()
f.Date.Soon()
DateTime.Now
```

**Replace with**:
```csharp
f.Date.RecentUtc()            // Past UTC dates
f.Date.SoonUtc()              // Future UTC dates
```

### Pattern 5: Message Types

**Search for**:
```csharp
$"Order.{f.Random.Word()}"
```

**Replace with**:
```csharp
f.NotificationType()          // Generates realistic notification types
f.RequestType()               // Generates realistic request types
```

## Add Required Usings

```csharp
using Encina.Testing.Bogus;
```

## Grep Patterns to Find Candidates

```bash
grep -r "new Faker<" tests/
grep -r "\.RuleFor(" tests/
grep -r "f\.Random\.Guid()" tests/
grep -r "DateTime\.Now" tests/
grep -r "DateTime\.UtcNow" tests/  # In Faker context
```

## Verification

- [ ] Run `dotnet test` - all tests still pass
- [ ] Tests are now reproducible (run twice, same results)
- [ ] Random data uses domain-appropriate formats

## Post-Migration

- [ ] Commit: `refactor(tests): migrate to EncinaFaker for reproducible test data`
- [ ] Update tracking issue #498

---

## EncinaFaker Features Reference

### Seeding
```csharp
// Default seed: 12345 (reproducible)
var faker = new EncinaFaker<Order>();

// Custom seed
var faker = new EncinaFaker<Order>().UseSeed(42);
```

### Bogus Extensions
```csharp
// IDs
f.Random.CorrelationId()
f.Random.UserId("user")
f.Random.TenantId("tenant")
f.Random.IdempotencyKey()
f.Random.GuidEntityId()
f.Random.IntEntityId()
f.Random.LongEntityId()
f.Random.StringEntityId(8, "ORD")
f.Random.StronglyTypedIdValue<Guid>()
f.Random.StringStronglyTypedIdValue(12, "SKU")

// Messages
f.NotificationType()
f.RequestType()
f.SagaType()
f.SagaStatus()

// Dates
f.Date.RecentUtc(days: 7)
f.Date.SoonUtc(days: 7)
f.Date.DateRangeValue(pastDays: 30, futureDays: 30)
f.Date.TimeRangeValue(minHours: 1, maxHours: 8)

// Content
f.JsonContent(propertyCount: 3)
```
