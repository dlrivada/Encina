# Testing Dogfooding Migration Priority Guide

This guide provides actionable prioritization for migrating existing tests to use Encina.Testing.* packages.

Related: [Testing Dogfooding Plan](./testing-dogfooding-plan.md) | [Issue #498](https://github.com/dlrivada/Encina/issues/498)

---

## Priority Levels

### P0 - Critical (Do First)
Changes that provide immediate value with minimal risk.

### P1 - High (Core Migration)
Essential migrations that establish patterns for the rest of the codebase.

### P2 - Medium (Comprehensive Coverage)
Migrations that improve consistency across the test suite.

### P3 - Low (Nice to Have)
Optimizations and advanced patterns.

---

## Phase-Based Priority Matrix

### Phase 1: Foundation (P0)

**Goal**: Add Encina.Testing.* package references without breaking existing tests.

| Task | Effort | Impact | Notes |
|------|--------|--------|-------|
| Add `Encina.Testing.Shouldly` reference | Low | High | Enables Either assertions |
| Add `Encina.Testing.Bogus` reference | Low | Medium | Enables seeded fakers |
| Add `Encina.Testing.Fakes` reference | Low | High | Enables messaging test doubles |

**Action**: Update `.csproj` files to include:
```xml
<PackageReference Include="Encina.Testing.Shouldly" />
<PackageReference Include="Encina.Testing.Bogus" />
<PackageReference Include="Encina.Testing.Fakes" />
```

### Phase 2: Either Assertions (P1)

**Goal**: Replace custom Either assertion helpers with standard extensions.

| Before | After | Package |
|--------|-------|---------|
| `result.ExpectSuccess()` | `result.ShouldBeSuccess()` | Encina.Testing.Shouldly |
| `result.ExpectFailure()` | `result.ShouldBeError()` | Encina.Testing.Shouldly |
| `result.IsRight.ShouldBeTrue()` | `result.ShouldBeSuccess()` | Encina.Testing.Shouldly |
| `result.Match(Right: x => x, Left: ...)` | `result.ShouldBeSuccess()` returns value | Encina.Testing.Shouldly |

**Priority Order**:
1. Health check tests (simple, isolated)
2. Handler tests (establish pattern)
3. Integration tests (more complex)
4. Property tests (can use EncinaProperties)

### Phase 3: Test Data Generation (P1)

**Goal**: Replace `new Faker<T>()` with `new EncinaFaker<T>()` for reproducibility.

| Before | After | Benefit |
|--------|-------|---------|
| `new Faker<T>()` | `new EncinaFaker<T>()` | Default seed for reproducibility |
| Random GUIDs | `faker.Random.CorrelationId()` | Domain-specific IDs |
| Manual timestamps | `faker.Date.RecentUtc()` | UTC enforcement |

**Priority Order**:
1. Tests that use domain entities (Orders, Customers, etc.)
2. Tests with complex object graphs
3. Tests with generated messages

### Phase 4: Messaging Test Doubles (P1)

**Goal**: Use FakeOutboxStore/FakeInboxStore for messaging pattern tests.

| Before | After | Benefit |
|--------|-------|---------|
| Custom mock outbox | `FakeOutboxStore` | Pre-built verification |
| Manual message tracking | `store.WasMessageAdded<T>()` | Type-safe checks |
| Mock setup boilerplate | `OutboxTestHelper` | Given/When/Then pattern |

**Priority Order**:
1. Outbox pattern tests
2. Inbox idempotency tests
3. Saga orchestration tests

### Phase 5: Architecture Tests (P2)

**Goal**: Standardize architecture rule tests.

| Before | After | Benefit |
|--------|-------|---------|
| Custom ArchUnitNET rules | `EncinaArchitectureRulesBuilder` | Pre-built Encina rules |
| Manual layer checks | `.VerifyCleanArchitecture()` | Standardized checks |

### Phase 6: Property-Based Tests (P2)

**Goal**: Adopt FsCheck with Encina arbitraries.

| Before | After | Benefit |
|--------|-------|---------|
| Manual edge case tests | `[EncinaProperty]` | Auto-generated inputs |
| Custom generators | `EncinaArbitraries.*` | Pre-built domain types |
| Manual invariant checks | `EncinaProperties.*` | Pre-built validators |

### Phase 7: Contract Tests (P3)

**Goal**: Standardize consumer-driven contracts.

| Before | After | Benefit |
|--------|-------|---------|
| Raw PactNet usage | `EncinaPactConsumerBuilder` | Fluent Encina integration |
| Manual HTTP paths | Extension methods | Standard paths |

### Phase 8: Advanced Patterns (P3)

**Goal**: Adopt fixture patterns for complex tests.

| Before | After | Benefit |
|--------|-------|---------|
| Manual ServiceCollection | `EncinaTestFixture` | Pre-configured DI |
| Manual module setup | `ModuleTestFixture<T>` | Module isolation |
| Manual WireMock | `EncinaWireMockFixture` | Pre-configured HTTP mocks |

---

## Test Project Priority Order

Based on impact and complexity, migrate in this order:

### Tier 1: Core Tests
1. `Encina.Tests` - Core mediator functionality
2. `Encina.Messaging.Tests` - Messaging patterns
3. `Encina.Testing.*.Tests` - Testing library self-tests

### Tier 2: Provider Tests
4. `Encina.EntityFrameworkCore.Tests` - EF Core integration
5. `Encina.Dapper.Tests` - Dapper integration
6. `Encina.Data.Tests` - ADO.NET integration

### Tier 3: Feature Tests
7. `Encina.Validation.*.Tests` - Validation integrations
8. `Encina.Caching.*.Tests` - Caching integrations
9. `Encina.Resilience.Tests` - Resilience patterns

### Tier 4: Integration Tests
10. `*.IntegrationTests` projects - Full integration tests
11. `*.PropertyTests` projects - Property-based tests

---

## Quick Reference: Package Selection

| Need | Package |
|------|---------|
| Either assertions | `Encina.Testing.Shouldly` |
| Seeded test data | `Encina.Testing.Bogus` |
| Messaging fakes | `Encina.Testing.Fakes` |
| Given/When/Then helpers | `Encina.Testing.Messaging` |
| HTTP API mocking | `Encina.Testing.WireMock` |
| Architecture rules | `Encina.Testing.Architecture` |
| Property-based testing | `Encina.Testing.FsCheck` |
| Contract testing | `Encina.Testing.Pact` |
| Snapshot testing | `Encina.Testing.Verify` |
| Database cleanup | `Encina.Testing.Respawn` |
| Handler BDD pattern | `Encina.Testing.Handlers` |

---

## Common Patterns Quick Reference

### Pattern 1: Either Assertion

```csharp
// Before
var result = await handler.Handle(command);
result.IsRight.ShouldBeTrue();
var value = result.Match(Right: v => v, Left: _ => throw new Exception());

// After
var result = await handler.Handle(command);
var value = result.ShouldBeSuccess();  // Returns the value directly
```

### Pattern 2: Error Assertion

```csharp
// Before
var result = await handler.Handle(invalidCommand);
result.IsLeft.ShouldBeTrue();
result.IfLeft(err => err.Code.ShouldStartWith("encina.validation"));

// After
var result = await handler.Handle(invalidCommand);
result.ShouldBeValidationError();  // Or ShouldBeErrorWithCode("encina.validation.*")
```

### Pattern 3: Reproducible Test Data

```csharp
// Before
var faker = new Faker<Order>();
faker.RuleFor(o => o.CustomerId, f => f.Random.Guid().ToString());
var order = faker.Generate();  // Different each run

// After
var faker = new EncinaFaker<Order>();  // Seed 12345 by default
faker.RuleFor(o => o.CustomerId, f => f.Random.UserId("customer"));
var order = faker.Generate();  // Same each run
```

### Pattern 4: Outbox Verification

```csharp
// Before
var mockOutbox = Substitute.For<IOutboxStore>();
// Complex mock setup...

// After
var store = new FakeOutboxStore();
// Execute handler...
store.WasMessageAdded<OrderCreatedEvent>().ShouldBeTrue();
store.AddedMessages[0].Content.ShouldContain("order123");
```

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-04 | 1.0 | Initial guide created |
