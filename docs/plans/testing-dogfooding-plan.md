# Testing Dogfooding Plan - Use Encina.Testing Infrastructure

> **Epic**: #498 - Dogfooding: Refactor Encina tests to use Encina.Testing infrastructure
>
> **Objective**: Refactor Encina tests to use its own testing infrastructure (`Encina.Testing.*`), demonstrating its real utility and serving as implementation examples.

---

## Table of Contents

1. [Dogfooding Objectives](#1-dogfooding-objectives)
2. [Core Migration Principles](#2-core-migration-principles)
3. [Scope and Boundaries](#3-scope-and-boundaries)
4. [Success Criteria](#4-success-criteria)
5. [Test Category Patterns](#5-test-category-patterns)
6. [Dependency Replacement Matrix](#6-dependency-replacement-matrix)
7. [Before/After Migration Examples](#7-beforeafter-migration-examples)
8. [Test Data Generation Patterns](#8-test-data-generation-patterns)
9. [Fixture and Lifecycle Patterns](#9-fixture-and-lifecycle-patterns)
10. [Assertion Patterns](#10-assertion-patterns)
11. [Migration Execution Process](#11-migration-execution-process)
12. [Available Encina.Testing Packages](#12-available-encinatesting-packages)

---

## 1. Dogfooding Objectives

### Primary Goals

1. **Prove the API**: Using Encina.Testing.* packages in real tests validates the API design and uncovers usability issues before external users encounter them.

2. **Create Living Documentation**: Tests become authoritative examples of how to use each testing package correctly.

3. **Identify Gaps**: Dogfooding reveals missing features, awkward patterns, or insufficient abstractions.

4. **Ensure Consistency**: All tests follow the same patterns, making the codebase easier to maintain.

5. **Validate Integration**: Ensure all Encina.Testing.* packages work seamlessly together.

### Secondary Goals

- Reduce test boilerplate across the codebase
- Improve test readability and maintainability
- Establish patterns that external users can follow
- Catch regressions in testing infrastructure early

---

## 2. Core Migration Principles

### 2.1 Gradual Migration

- **Do not break existing tests** - Migration is incremental
- **One test class at a time** - Avoid large PRs that touch many files
- **Green tests first** - Only migrate passing tests
- **Verify coverage remains constant** - Coverage must not decrease

### 2.2 Pattern Over Implementation

- Focus on **what** the test validates, not **how** it's implemented
- Use the highest-level abstraction available
- Prefer `EncinaTestFixture` fluent API over manual `ServiceCollection` setup
- Use domain-specific assertions (`ShouldBeSuccess()`) over generic ones (`Assert.True()`)

### 2.3 Zero Direct Dependencies

After migration, tests should NOT directly reference:
- ❌ `FluentAssertions` (use `Encina.Testing.Shouldly`)
- ❌ `AutoFixture` (use `Encina.Testing.Bogus`)
- ❌ `Testcontainers` directly (use `Encina.Testing.Testcontainers` wrappers)
- ❌ Manual mock factories (use `Encina.Testing.Fakes`)

### 2.4 Test Independence

- Each test must be self-contained
- Use `EncinaTestFixture` to isolate state
- Call `ClearStores()` between tests if using shared fixtures
- Avoid static state in test classes

### 2.5 Explicit Over Implicit

- Configure only what the test needs
- Use `WithHandler<T>()` explicitly instead of assembly scanning
- Make test data visible in the test method, not hidden in base classes
- Prefer inline `EncinaFaker<T>` configuration over shared fakers

---

## 3. Scope and Boundaries

### 3.1 In Scope (Must Migrate)

All test projects for packages that are NOT testing infrastructure themselves:

| Project Category | Example Projects | Priority |
|-----------------|------------------|----------|
| Core | `Encina.Tests` | High |
| Domain Modeling | `Encina.DomainModeling.Tests` | High |
| Messaging | `Encina.Messaging.Tests` | High |
| Database Providers | `Encina.Dapper.*.Tests`, `Encina.ADO.*.Tests` | Medium |
| Caching | `Encina.Caching.*.Tests` | Medium |
| Web Integration | `Encina.AspNetCore.Tests`, `Encina.Refit.Tests` | Medium |
| Resilience | `Encina.Polly.Tests` | Low |
| Validation | `Encina.FluentValidation.Tests` | Low |
| Serverless | `Encina.AwsLambda.Tests`, `Encina.AzureFunctions.Tests` | Low |

### 3.2 Out of Scope (Circular Dependency)

These packages **cannot use their own infrastructure**:

| Package | Reason |
|---------|--------|
| `Encina.Testing` | Self-reference |
| `Encina.Testing.Fakes` | Self-reference |
| `Encina.Testing.Shouldly` | Self-reference |
| `Encina.Testing.Bogus` | Self-reference |
| `Encina.Testing.TUnit` | Self-reference |
| `Encina.Testing.Verify` | Self-reference |
| `Encina.Testing.Architecture` | Self-reference |
| `Encina.Testing.Respawn` | Self-reference |
| `Encina.Testing.Testcontainers` | Self-reference |
| `Encina.Testing.WireMock` | Self-reference |
| `Encina.Testing.FsCheck` | Self-reference |
| `Encina.Testing.Pact` | Self-reference |
| `Encina.Aspire.Testing` | Self-reference |

### 3.3 Partial Migration (Special Cases)

| Package | Notes |
|---------|-------|
| Integration Tests | Keep `Testcontainers` where Aspire is not available (e.g., Oracle) |
| Load Tests | May use NBomber directly, but data generation should use `Encina.Testing.Bogus` |
| Contract Tests | Should use `Encina.Testing.Pact` when testing external APIs |

---

## 4. Success Criteria

### 4.1 Quantitative Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Encina.Testing.* Adoption | 100% | All in-scope tests use at least one Encina.Testing.* package |
| Direct Dependency Removal | 0 | No direct references to FluentAssertions, AutoFixture, etc. |
| Line Coverage | ≥85% | Maintained or improved after migration |
| Branch Coverage | ≥80% | Maintained or improved after migration |
| Method Coverage | ≥90% | Maintained or improved after migration |
| Mutation Score | ≥80% | Per Stryker configuration |

### 4.2 Qualitative Criteria

- [ ] Tests serve as documentation (clear, readable, idiomatic)
- [ ] No test failures introduced during migration
- [ ] CI/CD pipeline remains green
- [ ] Test execution time does not regress significantly (±10%)
- [ ] Each phase has at least one reference implementation

### 4.3 Definition of Done (Per Phase)

- [ ] All tests in the phase pass
- [ ] Coverage targets met
- [ ] No direct dependencies on replaced libraries
- [ ] At least 3 code reviewers approve the migration
- [ ] Documentation updated with examples from migration

---

## 5. Test Category Patterns

### 5.1 Unit Tests

**Purpose**: Test individual methods in isolation with mocked dependencies.

**Primary Tools**:
- `Encina.Testing` - `EncinaTestFixture`, `EncinaTestContext`, `AggregateTestBase<T>`, `HandlerSpecification<TRequest, TResponse>`
- `Encina.Testing.Shouldly` - `EitherShouldlyExtensions`
- `Encina.Testing.Fakes` - `FakeEncina`, `FakeOutboxStore`, etc.
- `Encina.Testing.Bogus` - `EncinaFaker<T>`

#### When to Use Base Classes vs Plain Test Classes

| Base Class | Use When |
|------------|----------|
| `AggregateTestBase<TAggregate, TId>` | Testing event-sourced aggregates with Given/When/Then pattern |
| `HandlerSpecification<TRequest, TResponse>` | Testing request handlers with BDD-style Given/When/Then |
| Plain test class with `EncinaTestFixture` | General handler/service tests without BDD structure |

#### Pattern 1: EncinaTestFixture (General Purpose)

```csharp
[Fact]
public async Task Handler_WithValidInput_ShouldReturnSuccess()
{
    // Arrange
    var fixture = new EncinaTestFixture()
        .WithHandler<CreateOrderHandler>()
        .WithMockedOutbox();

    var command = new EncinaFaker<CreateOrderCommand>()
        .RuleFor(x => x.CustomerId, f => f.Random.UserId())
        .Generate();

    // Act
    var context = await fixture.SendAsync(command);

    // Assert
    context.Result.ShouldBeSuccess();
    fixture.Outbox.GetMessages().ShouldHaveSingleItem();
}
```

#### Pattern 2: AggregateTestBase (Event-Sourced Aggregates)

```csharp
public class OrderAggregateTests : AggregateTestBase<Order, OrderId>
{
    [Fact]
    public void Submit_WhenOrderCreated_ShouldProduceOrderSubmittedEvent()
    {
        Given(
            new OrderCreated(OrderId, CustomerId, Items),
            new PaymentReceived(OrderId, Amount)
        );

        When(order => order.Submit());

        Then<OrderSubmitted>(e =>
        {
            Assert.Equal(OrderId, e.OrderId);
        });
    }

    [Fact]
    public void Cancel_WhenAlreadyShipped_ShouldThrowException()
    {
        Given(new OrderShipped(OrderId));

        When(order => order.Cancel());

        ThenThrows<InvalidOperationException>();
    }
}
```

#### Pattern 3: HandlerSpecification (BDD-Style Handler Tests)

```csharp
public class CreateOrderHandlerSpecs : HandlerSpecification<CreateOrder, OrderId>
{
    private readonly Mock<IOrderRepository> _mockRepo = new();

    protected override CreateOrder CreateRequest() => new()
    {
        CustomerId = "CUST-001",
        Items = [new OrderItem("PROD-001", 1, 99.99m)]
    };

    protected override IRequestHandler<CreateOrder, OrderId> CreateHandler() =>
        new CreateOrderHandler(_mockRepo.Object);

    [Fact]
    public async Task Should_create_order_with_valid_data()
    {
        Given(r => r.CustomerId = "PREMIUM-CUSTOMER");

        await When();

        ThenSuccess(orderId => Assert.NotEqual(Guid.Empty, orderId.Value));
    }

    [Fact]
    public async Task Should_return_validation_error_for_empty_customer()
    {
        Given(r => r.CustomerId = "");

        await When();

        ThenValidationError("CustomerId");
    }
}
```

**Execution Time**: < 1ms per test

### 5.2 Integration Tests

**Purpose**: Test against real infrastructure (databases, queues, etc.).

**Primary Tools**:
- `Encina.Testing` - `ModuleTestFixture<TModule>` for isolated module testing
- `Encina.Aspire.Testing` - For Aspire-compatible resources
- `Encina.Testing.Testcontainers` - For Docker-based fixtures
- `Encina.Testing.Respawn` - Database cleanup between tests
- `Encina.Testing.Fakes` - `FakeOutboxStore`, `FakeInboxStore`, `FakeSagaStore`

#### Pattern 1: ModuleTestFixture for Isolated Module Testing

```csharp
public class OrderModuleTests : IAsyncLifetime
{
    private ModuleTestFixture<OrdersModule> _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new ModuleTestFixture<OrdersModule>()
            .WithMockedModule<IInventoryModuleApi>(mock =>
                mock.ReserveStock = _ => Task.FromResult(
                    Either<EncinaError, ReservationId>.Right(new ReservationId("res-123"))))
            .WithMockedOutbox()
            .WithFakeTimeProvider();

        _fixture.Build();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task PlaceOrder_ValidOrder_Succeeds()
    {
        // Act
        var result = await _fixture.SendAsync(new PlaceOrderCommand(...));

        // Assert
        result.ShouldSucceed();
        _fixture.IntegrationEvents.ShouldContain<OrderPlacedEvent>();
        _fixture.Outbox.GetMessages().ShouldNotBeEmpty();
    }
}
```

#### Pattern 2: Aspire Integration Testing

```csharp
public class OrderIntegrationTests(AspireFixture fixture) : IClassFixture<AspireFixture>
{
    [Fact]
    public async Task CreateOrder_ShouldPersistToDatabase()
    {
        // Arrange
        await using var scope = fixture.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var result = await encina.Send(new CreateOrderCommand { ... });

        // Assert
        result.ShouldBeSuccess();
    }
}
```

#### Pattern 3: Testcontainers with Async Lifecycle

```csharp
public class OrderRepositoryTests : SqlServerIntegrationTestBase, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await StartContainerAsync();
        await RunMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        await StopContainerAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateOrder_ShouldPersistToDatabase()
    {
        // Uses inherited Connection from SqlServerIntegrationTestBase
        var store = new OrderStore(Connection);

        var order = new EncinaFaker<Order>().Generate();
        await store.AddAsync(order);

        var retrieved = await store.GetByIdAsync(order.Id);
        retrieved.ShouldNotBeNull();
    }
}
```

**Traits**: `[Trait("Category", "Integration")]`

### 5.3 Architecture Tests

**Purpose**: Enforce architectural constraints via static analysis.

**Primary Tools**:
- `Encina.Testing.Architecture` - `EncinaArchitectureRulesBuilder`, `EncinaArchitectureTestBase`

#### Standard Rule Bundles

Use `ApplyAllStandardRules()` to apply all pre-built DDD/CQRS rules at once:

```csharp
public class ArchitectureTests : EncinaArchitectureTestBase
{
    [Fact]
    public void AllStandardArchitectureRules_ShouldPass()
    {
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(CreateOrderHandler).Assembly)
            .ApplyAllStandardRules()  // Applies all standard DDD/CQRS rules
            .Build();

        rules.Check();
    }
}
```

#### Custom Rule Composition

```csharp
public class ArchitectureTests : EncinaArchitectureTestBase
{
    [Fact]
    public void Handlers_ShouldNotDependOnInfrastructure()
    {
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(CreateOrderHandler).Assembly)
            .HandlersCannotDependOn("*.Infrastructure")
            .Build();

        rules.Check();
    }

    [Fact]
    public void DomainLayer_ShouldBeIndependent()
    {
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(Order).Assembly)
            .LayerCannotDependOn("Domain", "Infrastructure", "Application")
            .Build();

        rules.Check();
    }

    [Fact]
    public void SagaRules_OptIn()
    {
        // Saga rules are opt-in and not included in ApplyAllStandardRules
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(OrderFulfillmentSaga).Assembly)
            .ApplySagaRules()
            .Build();

        rules.Check();
    }
}
```

### 5.4 Contract Tests (Pact)

**Purpose**: Verify consumer-provider contracts for APIs using Consumer-Driven Contract (CDC) testing.

**Primary Tools**:
- `Encina.Testing.Pact` - `EncinaPactConsumerBuilder`, `EncinaPactProviderVerifier`, `EncinaPactFixture`

#### HTTP Conventions

Encina uses standard HTTP conventions for message types:

| Message Type | HTTP Endpoint | HTTP Method |
|--------------|---------------|-------------|
| Commands | `/api/commands/{TypeName}` | POST |
| Queries | `/api/queries/{TypeName}` | POST |
| Notifications | `/api/notifications/{TypeName}` | POST |

#### Consumer Test Pattern

```csharp
public class OrderServiceConsumerTests : IClassFixture<EncinaPactFixture>
{
    private readonly EncinaPactFixture _fixture;

    public OrderServiceConsumerTests(EncinaPactFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrder()
    {
        // Define consumer expectations
        var consumer = _fixture.CreateConsumer("OrderClient", "OrderService")
            .WithQueryExpectation(
                new GetOrderQuery { OrderId = Guid.Parse("12345678-...") },
                Either<EncinaError, OrderDto>.Right(new OrderDto { ... }))
            .WithProviderState("an order with id 12345678-... exists");

        // Verify consumer behavior against mock server
        await consumer.VerifyAsync(async uri =>
        {
            using var client = uri.CreatePactHttpClient();

            // POST /api/queries/GetOrderQuery
            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(
                new GetOrderQuery { OrderId = Guid.Parse("12345678-...") });

            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.ShouldBeSuccess();
        });
    }

    [Fact]
    public async Task GetOrder_WhenOrderNotFound_ReturnsError()
    {
        var consumer = _fixture.CreateConsumer("OrderClient", "OrderService")
            .WithQueryExpectation(
                new GetOrderQuery { OrderId = Guid.Parse("00000000-...") },
                Either<EncinaError, OrderDto>.Left(EncinaErrors.NotFound("Order", "...")))
            .WithProviderState("no orders exist");

        await consumer.VerifyAsync(async uri =>
        {
            using var client = uri.CreatePactHttpClient();
            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(
                new GetOrderQuery { OrderId = Guid.Parse("00000000-...") });

            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.ShouldBeNotFoundError();
        });
    }
}
```

#### Provider State Management

```csharp
public class OrderServiceProviderTests : IClassFixture<EncinaPactFixture>
{
    private readonly EncinaPactFixture _fixture;
    private readonly IOrderRepository _repository;

    [Fact]
    public async Task VerifyPactWithOrderClient()
    {
        var verifier = _fixture.CreateVerifier("OrderService")
            // Register provider states with setup callbacks
            .WithProviderState("an order with id 12345678-... exists", async () =>
            {
                await _repository.AddAsync(new Order
                {
                    Id = Guid.Parse("12345678-..."),
                    CustomerId = "CUST-001"
                });
            })
            .WithProviderState("no orders exist", async () =>
            {
                await _repository.ClearAsync();
            });

        var result = await verifier.VerifyAsync("pacts/OrderClient-OrderService.json");
        result.Success.ShouldBeTrue();
    }
}
```

#### Either Result Mapping to Pact Responses

Encina automatically maps `Either<EncinaError, T>` to Pact responses:

| Either Result | HTTP Status | Response Body |
|---------------|-------------|---------------|
| `Right(value)` | 200 OK | Serialized value |
| `Left(ValidationError)` | 400 Bad Request | `PactErrorResponse` |
| `Left(NotFoundError)` | 404 Not Found | `PactErrorResponse` |
| `Left(AuthorizationError)` | 403 Forbidden | `PactErrorResponse` |
| `Left(OtherError)` | 500 Internal Server Error | `PactErrorResponse` |

### 5.5 Property-Based Tests

**Purpose**: Test invariants with randomly generated inputs.

**Primary Tools**:
- `Encina.Testing.FsCheck` - `PropertyTestBase`, `EncinaArbitraryProvider`, `EncinaArbitraries`, `EncinaProperties`, `GenExtensions`

#### Custom Attributes for Test Configuration

| Attribute | Max Tests | Use Case |
|-----------|-----------|----------|
| `[EncinaProperty]` | 100 | Default property tests |
| `[QuickProperty]` | 20 | Fast development feedback |
| `[ThoroughProperty]` | 1000 | Comprehensive testing before release |

#### Pattern 1: Using PropertyTestBase with EncinaArbitraryProvider

```csharp
public class EitherPropertyTests : PropertyTestBase
{
    // EncinaArbitraryProvider auto-registers arbitraries for:
    // - EncinaError, Either<EncinaError, T>, OutboxMessage, InboxMessage, etc.

    [EncinaProperty]  // 100 tests with Encina arbitraries
    public Property Either_IsExclusive(Either<EncinaError, int> either)
    {
        return EncinaProperties.EitherIsExclusive(either);
    }

    [QuickProperty]  // 20 tests for fast feedback
    public Property Error_HasNonEmptyMessage(EncinaError error)
    {
        return EncinaProperties.ErrorHasNonEmptyMessage(error);
    }

    [ThoroughProperty]  // 1000 tests for thorough validation
    public Property Handler_IsDeterministic()
    {
        return EncinaProperties.HandlerIsDeterministic<CreateOrderHandler, CreateOrder, OrderId>(
            () => new CreateOrderHandler(),
            () => new CreateOrder { CustomerId = "CUST-001" });
    }
}
```

#### EncinaArbitraries - Pre-built Generators

```csharp
// Built-in arbitraries for Encina types
EncinaArbitraries.EncinaError()           // Generates random EncinaError
EncinaArbitraries.Either<T>(arbT)         // Generates Either<EncinaError, T>
EncinaArbitraries.OutboxMessage()         // Generates OutboxMessage
EncinaArbitraries.InboxMessage()          // Generates InboxMessage
EncinaArbitraries.SagaState()             // Generates SagaState
EncinaArbitraries.ScheduledMessage()      // Generates ScheduledMessage
```

#### EncinaProperties - Reusable Validators

```csharp
// Pre-built property validators
EncinaProperties.EitherIsExclusive(either)     // Either is Left XOR Right, never both
EncinaProperties.ErrorHasNonEmptyMessage(err)  // Error messages are never empty
EncinaProperties.HandlerIsDeterministic(...)   // Same input = same output
```

#### GenExtensions - Fluent Generator Composition

```csharp
public class CustomPropertyTests : PropertyTestBase
{
    [EncinaProperty]
    public Property OrderAmount_AlwaysPositive()
    {
        // ToSuccess<T>() - Always generates Right values
        var orderGen = Gen.Choose(1, 10000)
            .Select(amount => new Order { Amount = amount })
            .ToSuccess<Order>();

        return Prop.ForAll(orderGen.ToArbitrary(), result =>
        {
            var order = result.Match(Right: o => o, Left: _ => null!);
            return order.Amount > 0;
        });
    }

    [EncinaProperty]
    public Property ValidationErrors_AreAlwaysLeft()
    {
        // ToFailure<T>() - Always generates Left values
        var errorGen = EncinaArbitraries.EncinaError().Generator
            .ToFailure<Order>();

        return Prop.ForAll(errorGen.ToArbitrary(), result =>
        {
            return result.IsLeft;
        });
    }

    [EncinaProperty]
    public Property Either_MixedGeneration()
    {
        // ToEither<T>() - Generates both Left and Right values randomly
        var eitherGen = Gen.Choose(1, 100).ToEither<int>();

        return Prop.ForAll(eitherGen.ToArbitrary(), result =>
        {
            return result.IsLeft || result.IsRight;  // Always true
        });
    }
}
```

#### Creating Custom Domain Arbitraries

```csharp
public static class DomainArbitraries
{
    public static Arbitrary<OrderId> OrderId()
    {
        return Gen.Select(ArbMap.Default.GeneratorFor<Guid>(),
            guid => new OrderId(guid)).ToArbitrary();
    }

    public static Arbitrary<Money> Money()
    {
        return Gen.Select2(
            Gen.Choose(1, 1000000),
            Gen.Elements("USD", "EUR", "GBP"),
            (cents, currency) => Domain.Money.From(cents / 100m, currency)
                .Match(Right: m => m, Left: _ => throw new Exception()))
            .ToArbitrary();
    }
}

// Register in tests
[EncinaProperty(Arbitrary = new[] { typeof(DomainArbitraries) })]
public Property Money_AdditionIsCommutative(Money a, Money b)
{
    return (a + b == b + a).ToProperty();
}
```

### 5.6 Snapshot Tests

**Purpose**: Verify output stability through serialized comparisons.

**Primary Tools**:
- `Encina.Testing.Verify` - `EncinaVerify`, `EncinaVerifySettings`

**Pattern**:
```csharp
public class SerializationTests
{
    [Fact]
    public async Task OrderCreatedEvent_Serialization_IsStable()
    {
        var @event = new OrderCreatedEvent
        {
            OrderId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            CustomerId = "CUST-001",
            TotalAmount = 150.00m,
            CreatedAtUtc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        await EncinaVerify.VerifyJson(@event);
    }

    [Fact]
    public async Task ValidationError_Format_IsStable()
    {
        var error = EncinaErrors.Validation("CustomerId", "Customer ID is required");

        await EncinaVerify.VerifyError(error);
    }
}
```

---

## 6. Dependency Replacement Matrix

This matrix shows which Encina.Testing.* package replaces each external testing dependency.

> **Important**: xUnit attributes (`[Fact]`, `[Theory]`, etc.) and NSubstitute for general mocking remain acceptable. Only replace dependencies where Encina.Testing.* provides domain-specific value.

### 6.1 Assertion Libraries

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `Shouldly` (raw) | `Encina.Testing.Shouldly` | Adds Either-specific extensions: `ShouldBeSuccess()`, `ShouldBeError()`, `ShouldBeValidationError()` |
| `FluentAssertions` | `Encina.Testing.Shouldly` | Migrate to Shouldly-based assertions |
| `Assert.True(result.IsRight)` | `result.ShouldBeSuccess()` | Domain-specific, better error messages |
| `Assert.Equal(expected, actual)` | `actual.ShouldBe(expected)` | Standard Shouldly syntax |
| Aggregate event assertions | `ShouldHaveRaisedEvent<T>()`, `ShouldHaveVersion()` | From `Encina.Testing.Shouldly` |
| Streaming assertions | Memory-efficient first-item checks | From `Encina.Testing.Shouldly` |

### 6.2 Test Data Generation

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `Bogus` (raw) | `Encina.Testing.Bogus` | `EncinaFaker<T>` base class with reproducible seeding (seed: 12345) |
| `AutoFixture` | `Encina.Testing.Bogus` | Use `EncinaFaker<T>` with explicit rules |
| `new Fixture().Create<T>()` | `new EncinaFaker<T>().Generate()` | Explicit rules preferred over auto-generation |
| Manual object instantiation | `EncinaFaker<T>` with `RuleFor()` | Reproducible, self-documenting |
| Domain type generators | Extension methods: `EntityId<TId>()`, `StronglyTypedIdValue<T>()`, `QuantityValue()`, `PercentageValue()` | From `Encina.Testing.Bogus` |
| Metadata generators | `CorrelationId()`, `UserId()`, `TenantId()`, `IdempotencyKey()` | From `Encina.Testing.Bogus` |
| Messaging entity builders | `InboxMessageFaker.AsProcessed()`, `OutboxMessageFaker.AsFailed()` | Builder pattern for states |

### 6.3 Mocking and Fakes

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `NSubstitute` for `IOutboxStore` | `FakeOutboxStore` | Behavioral fake with verification methods |
| `NSubstitute` for `IInboxStore` | `FakeInboxStore` | Behavioral fake with verification methods |
| `NSubstitute` for `ISagaStore` | `FakeSagaStore` | Behavioral fake with verification methods |
| `NSubstitute` for `IScheduledMessageStore` | `FakeScheduledMessageStore` | Behavioral fake with verification methods |
| `Mock<IEncina>()` | `FakeEncina` | Full in-memory implementation |
| Manual mock setup | `EncinaTestFixture.WithMockedOutbox()` | Fluent configuration |
| **`NSubstitute` for other interfaces** | **Keep `NSubstitute`** | ✅ Acceptable for non-messaging mocks |

### 6.4 Infrastructure Fixtures

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `Testcontainers.MsSql` | `Encina.Testing.Testcontainers` → `SqlServerContainerFixture` | Wrapped with Encina conventions |
| `Testcontainers.PostgreSql` | `Encina.Testing.Testcontainers` → `PostgreSqlContainerFixture` | Wrapped with Encina conventions |
| `Testcontainers.MySql` | `Encina.Testing.Testcontainers` → `MySqlContainerFixture` | Wrapped with Encina conventions |
| Manual Docker setup | `EncinaContainers` factory | Standardized container creation |
| Aspire `DistributedApplicationTestingBuilder` | `Encina.Aspire.Testing` | When using Aspire (pending #509 investigation) |

### 6.5 Test Framework and Attributes

| Before | After | Notes |
|--------|-------|-------|
| **xUnit `[Fact]`, `[Theory]`** | **Keep xUnit attributes** | ✅ Encina.Testing is an abstraction layer ON TOP of xUnit |
| xUnit raw `Assert.*` | `Encina.Testing.Shouldly` | Enhanced error messages, domain-specific |
| TUnit with custom assertions | `Encina.Testing.TUnit` | TUnit-specific extensions |

### 6.6 Snapshot Testing

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `Verify` (raw) | `Encina.Testing.Verify` | `EncinaVerifySettings` pre-configured for Encina types |
| Custom Verify converters | `EncinaVerify.PrepareEither()` | Automatic scrubbing for dynamic values |
| Aggregate snapshot | `EncinaVerify.PrepareAggregate()` | Pre-configured for aggregate events |
| Outbox snapshot | `EncinaVerify.PrepareOutboxMessages()` | Pre-configured for messaging |

### 6.7 Database Cleanup

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `Respawn` (raw) | `Encina.Testing.Respawn` | `DatabaseRespawner` with Encina-aware configuration |
| Manual cleanup scripts | `DatabaseRespawner.ResetAsync()` | Standardized `InitializeAsync`/`ResetAsync` pattern |

### 6.8 Architecture Testing

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `ArchUnitNET` (raw) | `Encina.Testing.Architecture` | `EncinaArchitectureRulesBuilder` fluent API |
| Manual rule definitions | `ApplyAllStandardRules()` | Pre-built DDD/CQRS rule bundles |
| Custom architecture rules | `EncinaArchitectureRulesBuilder` composition | Fluent rule building |

### 6.9 Property-Based Testing

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `FsCheck` (raw) with manual `Arb.Register` | `Encina.Testing.FsCheck` | `EncinaArbitraryProvider` auto-registers arbitraries |
| `[Property]` attribute | `[EncinaProperty]` (100 tests), `[QuickProperty]` (20), `[ThoroughProperty]` (1000) | Custom attributes with sensible defaults |
| Manual arbitrary creation | `EncinaArbitraries.EncinaError()`, `.Either<T>()`, `.OutboxMessage()`, etc. | Pre-built arbitraries for Encina types |
| Custom property validators | `EncinaProperties.EitherIsExclusive()`, `.ErrorHasNonEmptyMessage()`, `.HandlerIsDeterministic()` | Reusable property validators |
| Generator composition | `GenExtensions.ToEither<T>()`, `.ToSuccess<T>()`, `.ToFailure<T>()` | Fluent generator composition |

### 6.10 Contract Testing (Pact)

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `PactNet` with manual mock server setup | `Encina.Testing.Pact` → `EncinaPactConsumerBuilder` | Fluent API for consumer expectations |
| Manual provider verification | `EncinaPactProviderVerifier` | Handler invocation via reflection |
| Manual xUnit lifecycle | `EncinaPactFixture` | `IAsyncLifetime` integration |
| HTTP response mapping | `PactExtensions.ReadAsEitherAsync<T>()` | Automatic Either conversion |
| Provider state setup | `.WithProviderState(name, callback)` | Structured state management |

### 6.11 HTTP Mocking

| Before (Direct Dependency) | After (Encina.Testing.*) | Notes |
|----------------------------|--------------------------|-------|
| `WireMock.Net` manual setup | `Encina.Testing.WireMock` → `EncinaWireMockFixture` | HTTP mocking with `IAsyncLifetime` |
| Refit client mocking | `EncinaRefitMockFixture` | Automatic Refit client configuration |

---

## 7. Before/After Migration Examples

### Example 1: Basic Unit Test with Either Assertions

**BEFORE** (Manual assertions):
```csharp
[Fact]
public async Task CreateOrder_WithValidData_ReturnsOrderId()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddEncina(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateOrderHandler>());
    services.AddScoped<IOrderRepository, FakeOrderRepository>();

    await using var provider = services.BuildServiceProvider();
    var encina = provider.GetRequiredService<IEncina>();

    var command = new CreateOrderCommand
    {
        CustomerId = "CUST-001",
        Amount = 100.00m
    };

    // Act
    var result = await encina.Send(command);

    // Assert
    Assert.True(result.IsRight);
    var orderId = result.Match(Right: id => id, Left: _ => Guid.Empty);
    Assert.NotEqual(Guid.Empty, orderId);
}
```

**AFTER** (Using Encina.Testing):
```csharp
[Fact]
public async Task CreateOrder_WithValidData_ReturnsOrderId()
{
    // Arrange
    var fixture = new EncinaTestFixture()
        .WithHandler<CreateOrderHandler>()
        .WithService<IOrderRepository, FakeOrderRepository>();

    var command = new EncinaFaker<CreateOrderCommand>()
        .RuleFor(x => x.CustomerId, f => f.Random.UserId())
        .RuleFor(x => x.Amount, f => f.Finance.Amount(1, 1000))
        .Generate();

    // Act
    var context = await fixture.SendAsync(command);

    // Assert
    var orderId = context.Result.ShouldBeSuccess();
    orderId.ShouldNotBe(Guid.Empty);
}
```

---

### Example 2: Test with Outbox Verification

**BEFORE** (Manual mock verification):
```csharp
[Fact]
public async Task CreateOrder_PublishesOrderCreatedEvent()
{
    var mockOutbox = new Mock<IOutboxStore>();
    var capturedMessages = new List<OutboxMessage>();
    mockOutbox
        .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
        .Callback<OutboxMessage, CancellationToken>((m, _) => capturedMessages.Add(m))
        .Returns(Task.CompletedTask);

    var services = new ServiceCollection();
    services.AddEncina(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateOrderHandler>());
    services.AddSingleton(mockOutbox.Object);

    await using var provider = services.BuildServiceProvider();
    var encina = provider.GetRequiredService<IEncina>();

    await encina.Send(new CreateOrderCommand { CustomerId = "CUST-001" });

    Assert.Single(capturedMessages);
    Assert.Contains("OrderCreated", capturedMessages[0].NotificationType);
}
```

**AFTER** (Using EncinaTestFixture with FakeOutboxStore):
```csharp
[Fact]
public async Task CreateOrder_PublishesOrderCreatedEvent()
{
    // Arrange
    var fixture = new EncinaTestFixture()
        .WithHandler<CreateOrderHandler>()
        .WithMockedOutbox();

    var command = new EncinaFaker<CreateOrderCommand>()
        .RuleFor(x => x.CustomerId, f => f.Random.UserId())
        .Generate();

    // Act
    await fixture.SendAsync(command);

    // Assert
    fixture.Outbox.GetMessages().ShouldHaveSingleItem();
    fixture.Outbox.WasMessageAdded<OrderCreatedEvent>().ShouldBeTrue();
}
```

---

### Example 3: Test with Time-Dependent Logic

**BEFORE** (Using DateTime.UtcNow):
```csharp
[Fact]
public async Task ScheduleReminder_SchedulesForCorrectTime()
{
    // Fragile: depends on execution timing
    var before = DateTime.UtcNow;

    var services = new ServiceCollection();
    // ... setup ...

    var result = await encina.Send(new ScheduleReminderCommand { DelayMinutes = 30 });

    var after = DateTime.UtcNow;
    var scheduled = result.Match(Right: x => x, Left: _ => throw new Exception());

    Assert.True(scheduled.ScheduledAtUtc >= before.AddMinutes(30));
    Assert.True(scheduled.ScheduledAtUtc <= after.AddMinutes(30).AddSeconds(1));
}
```

**AFTER** (Using FakeTimeProvider):
```csharp
[Fact]
public async Task ScheduleReminder_SchedulesForCorrectTime()
{
    // Arrange
    var startTime = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
    var fixture = new EncinaTestFixture()
        .WithHandler<ScheduleReminderHandler>()
        .WithFakeTimeProvider(startTime)
        .WithMockedScheduling();

    // Act
    await fixture.SendAsync(new ScheduleReminderCommand { DelayMinutes = 30 });

    // Assert
    var scheduled = fixture.ScheduledMessageStore.GetMessages().ShouldHaveSingleItem();
    scheduled.ScheduledAtUtc.ShouldBe(startTime.AddMinutes(30).UtcDateTime);
}
```

---

### Example 4: Integration Test with Database

**BEFORE** (Raw Testcontainers):
```csharp
public class OrderRepositoryTests : IAsyncLifetime
{
    private MsSqlContainer _container = null!;
    private SqlConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _container.StartAsync();

        _connection = new SqlConnection(_container.GetConnectionString());
        await _connection.OpenAsync();

        // Run migrations manually...
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task AddOrder_PersistsToDatabase()
    {
        var repo = new OrderRepository(_connection);
        var order = new Order { Id = Guid.NewGuid(), CustomerId = "CUST-001" };

        await repo.AddAsync(order);
        var retrieved = await repo.GetByIdAsync(order.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(order.CustomerId, retrieved.CustomerId);
    }
}
```

**AFTER** (Using SqlServerIntegrationTestBase):
```csharp
public class OrderRepositoryTests : SqlServerIntegrationTestBase
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddOrder_PersistsToDatabase()
    {
        // Arrange
        var repo = new OrderRepository(Connection);
        var order = new EncinaFaker<Order>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.CustomerId, f => f.Random.UserId())
            .Generate();

        // Act
        await repo.AddAsync(order);
        var retrieved = await repo.GetByIdAsync(order.Id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.CustomerId.ShouldBe(order.CustomerId);
    }
}
```

---

### Example 5: Snapshot Testing with Verify

**BEFORE** (Raw Verify with custom converters):
```csharp
[Fact]
public async Task OrderCreatedEvent_SerializesCorrectly()
{
    var @event = new OrderCreatedEvent
    {
        OrderId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
        CustomerId = "CUST-001",
        Amount = 150.00m,
        CreatedAtUtc = DateTime.UtcNow  // Dynamic value!
    };

    // Manual scrubbing configuration
    var settings = new VerifySettings();
    settings.ScrubMembers("CreatedAtUtc");
    settings.AddExtraSettings(s =>
    {
        s.Converters.Add(new EncinaErrorConverter());
        s.Converters.Add(new EitherConverter());
    });

    await Verify(@event, settings);
}

// Custom converters needed for each Encina type...
public class EncinaErrorConverter : WriteOnlyJsonConverter<EncinaError> { ... }
public class EitherConverter : WriteOnlyJsonConverter { ... }
```

**AFTER** (Using EncinaVerify with pre-configured settings):
```csharp
[Fact]
public async Task OrderCreatedEvent_SerializesCorrectly()
{
    var @event = new OrderCreatedEvent
    {
        OrderId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
        CustomerId = "CUST-001",
        Amount = 150.00m,
        CreatedAtUtc = DateTime.UtcNow
    };

    // Automatic scrubbing and Encina type converters
    await EncinaVerify.VerifyJson(@event);
}

[Fact]
public async Task Handler_ResultSnapshot()
{
    var fixture = new EncinaTestFixture().WithHandler<CreateOrderHandler>();
    var result = await fixture.SendAsync(new CreateOrderCommand { ... });

    // PrepareEither automatically handles Left/Right serialization
    await EncinaVerify.PrepareEither(result.Result);
}

[Fact]
public async Task Aggregate_EventsSnapshot()
{
    var order = new Order();
    order.Create(orderId, customerId);
    order.AddItem(productId, quantity);

    // PrepareAggregate includes uncommitted events with scrubbing
    await EncinaVerify.PrepareAggregate(order);
}
```

---

### Example 6: Architecture Rule Testing

**BEFORE** (Raw ArchUnitNET with manual rule definition):
```csharp
[Fact]
public void Handlers_ShouldNotDependOnInfrastructure()
{
    var architecture = new ArchLoader()
        .LoadAssemblies(typeof(CreateOrderHandler).Assembly)
        .Build();

    var handlerRule = Types()
        .That()
        .ResideInNamespace("*.Handlers")
        .And()
        .HaveNameEndingWith("Handler")
        .Should()
        .NotDependOnAny(Types().That().ResideInNamespace("*.Infrastructure"))
        .Because("Handlers should not depend on infrastructure");

    handlerRule.Check(architecture);
}

[Fact]
public void Commands_ShouldBeSealed()
{
    var architecture = new ArchLoader()
        .LoadAssemblies(typeof(CreateOrderCommand).Assembly)
        .Build();

    var commandRule = Types()
        .That()
        .ImplementInterface(typeof(ICommand<>))
        .Should()
        .BeSealed()
        .Because("Commands should be immutable");

    commandRule.Check(architecture);
}
// ... many more boilerplate rules
```

**AFTER** (Using EncinaArchitectureRulesBuilder with pre-built bundles):
```csharp
public class ArchitectureTests : EncinaArchitectureTestBase
{
    [Fact]
    public void AllStandardRules_ShouldPass()
    {
        // ApplyAllStandardRules includes:
        // - Handlers cannot depend on Infrastructure
        // - Commands/Queries must be sealed
        // - Domain cannot depend on Application/Infrastructure
        // - And more...
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(CreateOrderHandler).Assembly)
            .ApplyAllStandardRules()
            .Build();

        rules.Check();
    }

    [Fact]
    public void CustomRule_HandlersCannotUseEntityFramework()
    {
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(CreateOrderHandler).Assembly)
            .TypesMatching(".*Handler")
            .ShouldNotDependOn("Microsoft.EntityFrameworkCore")
            .ShouldNotDependOn("System.Data")
            .Build();

        rules.Check();
    }
}
```

---

### Example 7: Property-Based Testing with FsCheck

**BEFORE** (Raw FsCheck with manual Arb.Register):
```csharp
public class EitherPropertyTests
{
    static EitherPropertyTests()
    {
        // Manual arbitrary registration required
        Arb.Register<EncinaErrorArbitrary>();
        Arb.Register<EitherArbitrary>();
    }

    [Property]
    public Property Either_IsExclusive()
    {
        return Prop.ForAll(
            Arb.From<Either<EncinaError, int>>(),
            either => (either.IsLeft && !either.IsRight) || (!either.IsLeft && either.IsRight));
    }

    [Property(MaxTest = 1000)]  // Manual configuration
    public Property Error_HasNonEmptyMessage()
    {
        return Prop.ForAll(
            Arb.From<EncinaError>(),
            error => !string.IsNullOrEmpty(error.Message));
    }
}

// Manual arbitrary classes needed
public class EncinaErrorArbitrary : Arbitrary<EncinaError>
{
    public override Gen<EncinaError> Generator =>
        from code in Gen.Elements("encina.validation", "encina.notfound")
        from message in Arb.Generate<NonEmptyString>()
        select EncinaErrors.Create(code, message.Get);
}
```

**AFTER** (Using PropertyTestBase with EncinaArbitraryProvider):
```csharp
public class EitherPropertyTests : PropertyTestBase
{
    // EncinaArbitraryProvider auto-registers all Encina arbitraries

    [EncinaProperty]  // 100 tests, Encina arbitraries auto-included
    public Property Either_IsExclusive(Either<EncinaError, int> either)
    {
        // Use pre-built property validator
        return EncinaProperties.EitherIsExclusive(either);
    }

    [ThoroughProperty]  // 1000 tests for comprehensive validation
    public Property Error_HasNonEmptyMessage(EncinaError error)
    {
        return EncinaProperties.ErrorHasNonEmptyMessage(error);
    }

    [EncinaProperty]
    public Property Handler_IsDeterministic()
    {
        return EncinaProperties.HandlerIsDeterministic<CreateOrderHandler, CreateOrder, OrderId>(
            () => new CreateOrderHandler(),
            () => new CreateOrder { CustomerId = "CUST-001" });
    }

    [QuickProperty]  // 20 tests for fast feedback
    public Property CustomProperty_OrderAmountIsPositive()
    {
        // Use GenExtensions for fluent generator composition
        var orderGen = Gen.Choose(1, 10000)
            .Select(amount => new Order { Amount = amount })
            .ToSuccess<Order>();

        return Prop.ForAll(orderGen.ToArbitrary(), result =>
        {
            var order = result.Match(Right: o => o, Left: _ => null!);
            return order.Amount > 0;
        });
    }
}
```

---

### Example 8: Contract Testing with Pact

**BEFORE** (Raw PactNet with manual mock server setup):
```csharp
public class OrderServiceConsumerTests : IAsyncLifetime
{
    private IPactBuilderV4? _pactBuilder;
    private int _mockServerPort;

    public async Task InitializeAsync()
    {
        var config = new PactConfig
        {
            PactDir = Path.Combine("..", "..", "..", "pacts"),
            LogLevel = PactLogLevel.Information
        };

        _pactBuilder = Pact.V4("OrderClient", "OrderService", config).WithHttpInteractions();
        _mockServerPort = 9222;
    }

    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrder()
    {
        // Manual interaction setup
        _pactBuilder!
            .UponReceiving("a request for an existing order")
            .Given("an order with id 12345678 exists")
            .WithRequest(HttpMethod.Post, "/api/queries/GetOrderQuery")
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(new { OrderId = "12345678-1234-1234-1234-123456789012" })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json")
            .WithJsonBody(new
            {
                Id = "12345678-1234-1234-1234-123456789012",
                CustomerId = "CUST-001",
                Amount = 150.00m
            });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{ctx.MockServerUri.Port}") };
            var response = await client.PostAsJsonAsync("/api/queries/GetOrderQuery",
                new { OrderId = "12345678-1234-1234-1234-123456789012" });

            Assert.True(response.IsSuccessStatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderDto>();
            Assert.NotNull(order);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
```

**AFTER** (Using EncinaPactConsumerBuilder with fluent API):
```csharp
public class OrderServiceConsumerTests : IClassFixture<EncinaPactFixture>
{
    private readonly EncinaPactFixture _fixture;

    public OrderServiceConsumerTests(EncinaPactFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrder()
    {
        // Fluent consumer expectation builder
        var consumer = _fixture.CreateConsumer("OrderClient", "OrderService")
            .WithQueryExpectation(
                new GetOrderQuery { OrderId = Guid.Parse("12345678-1234-1234-1234-123456789012") },
                Either<EncinaError, OrderDto>.Right(new OrderDto
                {
                    Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
                    CustomerId = "CUST-001",
                    Amount = 150.00m
                }))
            .WithProviderState("an order with id 12345678 exists");

        // Verify with automatic HTTP conventions
        await consumer.VerifyAsync(async uri =>
        {
            using var client = uri.CreatePactHttpClient();

            // POST /api/queries/GetOrderQuery (automatic convention)
            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(
                new GetOrderQuery { OrderId = Guid.Parse("12345678-1234-1234-1234-123456789012") });

            // Automatic Either conversion
            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.ShouldBeSuccess();
        });
    }

    [Fact]
    public async Task GetOrder_WhenNotFound_ReturnsError()
    {
        var consumer = _fixture.CreateConsumer("OrderClient", "OrderService")
            .WithQueryExpectation(
                new GetOrderQuery { OrderId = Guid.Parse("00000000-0000-0000-0000-000000000000") },
                Either<EncinaError, OrderDto>.Left(EncinaErrors.NotFound("Order", "...")))
            .WithProviderState("no orders exist");

        await consumer.VerifyAsync(async uri =>
        {
            using var client = uri.CreatePactHttpClient();
            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(
                new GetOrderQuery { OrderId = Guid.Parse("00000000-0000-0000-0000-000000000000") });

            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.ShouldBeNotFoundError();  // Domain-specific assertion
        });
    }
}
```

---

## 8. Test Data Generation Patterns

### 8.1 Basic EncinaFaker Usage

```csharp
// Simple generation with default seed (reproducible)
var orderFaker = new EncinaFaker<CreateOrderCommand>()
    .RuleFor(x => x.OrderId, f => f.Random.Guid())
    .RuleFor(x => x.CustomerId, f => f.Random.UserId())
    .RuleFor(x => x.Amount, f => f.Finance.Amount(1, 10000))
    .RuleFor(x => x.Currency, f => f.PickRandom("USD", "EUR", "GBP"));

var order = orderFaker.Generate();
var orders = orderFaker.Generate(10);
```

### 8.2 Domain-Specific Extensions

```csharp
// Use Encina-specific extensions
var messageFaker = new EncinaFaker<OutboxMessage>()
    .RuleFor(x => x.Id, f => f.Random.Guid())
    .RuleFor(x => x.CorrelationId, f => f.Random.CorrelationId())  // Extension
    .RuleFor(x => x.NotificationType, f => f.NotificationType())   // Extension
    .RuleFor(x => x.Payload, f => f.JsonContent(5))                // Extension
    .RuleFor(x => x.CreatedAtUtc, f => f.Date.RecentUtc());        // Extension
```

### 8.3 Strongly-Typed IDs

```csharp
// Generate values for strongly-typed IDs from Encina.DomainModeling
var faker = new Faker();
var orderIdValue = faker.Random.GuidStronglyTypedIdValue();
var productIdValue = faker.Random.IntStronglyTypedIdValue(1000, 9999);
var skuValue = faker.Random.StringStronglyTypedIdValue(8, "SKU");

// Use in custom fakers
var productFaker = new EncinaFaker<Product>()
    .RuleFor(x => x.Id, f => new ProductId(f.Random.IntStronglyTypedIdValue()))
    .RuleFor(x => x.Sku, f => new Sku(f.Random.StringStronglyTypedIdValue(8, "SKU")));
```

### 8.4 Value Objects

```csharp
// Generate values for value objects
var orderFaker = new EncinaFaker<Order>()
    .RuleFor(x => x.Quantity, f => Quantity.From(f.Random.QuantityValue(1, 100)))
    .RuleFor(x => x.Discount, f => Percentage.From(f.Random.PercentageValue(0, 50)))
    .RuleFor(x => x.ValidityPeriod, f =>
    {
        var (start, end) = f.Date.DateRangeValue();
        return DateRange.From(start, end);
    });
```

### 8.5 Pre-built Fakers for Messaging

```csharp
// Use pre-built fakers for messaging entities
var outboxFaker = new OutboxMessageFaker();
var inboxFaker = new InboxMessageFaker();
var sagaFaker = new SagaStateFaker();
var scheduledFaker = new ScheduledMessageFaker();

// Generate with defaults
var outboxMessages = outboxFaker.Generate(5);

// Customize with fluent builder pattern
var customOutbox = new OutboxMessageFaker()
    .WithNotificationType("OrderCreated")
    .WithStatus(OutboxMessageStatus.Pending)
    .Generate();
```

#### Builder Pattern for Message States

Use builder methods to create messages in specific states:

```csharp
// OutboxMessage states
var pending = new OutboxMessageFaker().AsPending().Generate();
var processed = new OutboxMessageFaker().AsProcessed().Generate();
var failed = new OutboxMessageFaker().AsFailed("Connection timeout").Generate();

// InboxMessage states
var unprocessed = new InboxMessageFaker().AsUnprocessed().Generate();
var processedInbox = new InboxMessageFaker().AsProcessed().Generate();
var duplicateDetected = new InboxMessageFaker().AsDuplicate().Generate();

// SagaState states
var startedSaga = new SagaStateFaker().AsStarted().Generate();
var completedSaga = new SagaStateFaker().AsCompleted().Generate();
var compensatingSaga = new SagaStateFaker().AsCompensating().Generate();
var failedSaga = new SagaStateFaker().AsFailed("Payment declined").Generate();

// ScheduledMessage states
var scheduledForFuture = new ScheduledMessageFaker().AsScheduledFor(DateTime.UtcNow.AddHours(1)).Generate();
var readyToExecute = new ScheduledMessageFaker().AsReady().Generate();
var executed = new ScheduledMessageFaker().AsExecuted().Generate();
var cancelled = new ScheduledMessageFaker().AsCancelled().Generate();
```

#### Retry Scenario Builders

```csharp
// Build messages with retry history
var messageWithRetries = new OutboxMessageFaker()
    .WithRetryCount(3)
    .WithNextRetryAt(DateTime.UtcNow.AddMinutes(5))
    .WithErrorMessage("Temporary failure")
    .Generate();

// Saga with step history
var sagaWithSteps = new SagaStateFaker()
    .WithCompletedStep("ReserveInventory")
    .WithCompletedStep("ProcessPayment")
    .WithCurrentStep("ShipOrder")
    .Generate();
```

### 8.6 Reproducible Tests

```csharp
// Same seed = same data (default seed is 12345)
var faker1 = new EncinaFaker<Order>();
var faker2 = new EncinaFaker<Order>();

var order1 = faker1.Generate();
var order2 = faker2.Generate();

// order1 and order2 have identical values

// Use different seed for variation
var faker3 = new EncinaFaker<Order>().UseSeed(99999);
var order3 = faker3.Generate(); // Different from order1/order2
```

---

## 9. Fixture and Lifecycle Patterns

### 9.1 EncinaTestFixture (Unit Tests)

```csharp
// Per-test fixture (recommended for unit tests)
[Fact]
public async Task Test1()
{
    var fixture = new EncinaTestFixture()
        .WithHandler<MyHandler>()
        .WithMockedOutbox();

    // Use fixture
    await fixture.SendAsync(command);

    // Fixture is automatically cleaned up
}
```

### 9.2 Shared Fixture with Cleanup

```csharp
// For expensive setup, share fixture with cleanup
public class OrderHandlerTests : IClassFixture<SharedTestFixture>, IDisposable
{
    private readonly SharedTestFixture _fixture;

    public OrderHandlerTests(SharedTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearStores(); // Clean state for each test
    }

    [Fact]
    public async Task Test1() { /* ... */ }

    public void Dispose() => _fixture.ClearStores();
}
```

### 9.3 ModuleTestFixture vs EncinaTestFixture

| Fixture | Use Case | Features |
|---------|----------|----------|
| `EncinaTestFixture` | Unit tests, single handler | Lightweight, per-test isolation |
| `ModuleTestFixture<T>` | Integration tests, full module | Module registration, mocked dependencies, integration events |

```csharp
// EncinaTestFixture: Single handler, unit test
[Fact]
public async Task UnitTest_SingleHandler()
{
    var fixture = new EncinaTestFixture()
        .WithHandler<CreateOrderHandler>()
        .WithMockedOutbox();

    await fixture.SendAsync(command);
}

// ModuleTestFixture: Full module with dependencies
[Fact]
public async Task IntegrationTest_FullModule()
{
    var fixture = new ModuleTestFixture<OrdersModule>()
        .WithMockedModule<IInventoryModuleApi>(mock => { ... })
        .WithMockedModule<IPaymentModuleApi>(mock => { ... })
        .WithMockedOutbox()
        .WithFakeTimeProvider();

    var result = await fixture.SendAsync(new PlaceOrderCommand { ... });

    // Assert integration events were published
    fixture.IntegrationEvents.ShouldContain<OrderPlacedEvent>();
}
```

### 9.4 Database Fixtures with Respawn

```csharp
// Integration test with database and Respawn cleanup
public class OrderRepositoryTests : SqlServerIntegrationTestBase, IAsyncLifetime
{
    private DatabaseRespawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await base.StartContainerAsync();
        await base.RunMigrationsAsync();

        // Configure Respawn with Encina-aware settings
        _respawner = await DatabaseRespawner.CreateAsync(ConnectionString, options =>
        {
            options.TablesToIgnore = ["__EFMigrationsHistory"];
            options.WithReseed = true;
        });
    }

    public async Task DisposeAsync()
    {
        await base.StopContainerAsync();
    }

    // Reset database between tests
    public async Task ResetAsync()
    {
        await _respawner.ResetAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        await ResetAsync();  // Clean slate

        var repo = new OrderRepository(Connection);
        // ...
    }
}
```

#### InitializeAsync/ResetAsync Pattern

```csharp
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected DatabaseRespawner Respawner { get; private set; } = null!;
    protected DbConnection Connection { get; private set; } = null!;

    // Called once before all tests in class
    public virtual async Task InitializeAsync()
    {
        Connection = await CreateConnectionAsync();
        Respawner = await DatabaseRespawner.CreateAsync(Connection);
    }

    // Called once after all tests in class
    public virtual async Task DisposeAsync()
    {
        await Connection.DisposeAsync();
    }

    // Call in each test to reset state
    protected async Task ResetDatabaseAsync()
    {
        await Respawner.ResetAsync();
        await SeedDataAsync();
    }

    protected virtual Task SeedDataAsync() => Task.CompletedTask;
}
```

### 9.5 Collection Fixtures

```csharp
// Share expensive resources across test classes
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public class DatabaseFixture : IAsyncLifetime
{
    public SqlServerContainerFixture Container { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Container = new SqlServerContainerFixture();
        await Container.StartAsync();
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[Collection("Database")]
public class OrderTests
{
    private readonly DatabaseFixture _db;

    public OrderTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task Test1() { /* Use _db.Container.Connection */ }
}
```

### 9.5 Aspire Fixtures

```csharp
// Using Aspire for distributed app testing
public class AspireIntegrationTests : IClassFixture<DistributedApplicationFixture>
{
    private readonly DistributedApplicationFixture _app;

    [Fact]
    public async Task EndToEnd_OrderFlow()
    {
        var client = _app.CreateClient("api-gateway");

        var response = await client.PostAsJsonAsync("/orders", new { ... });

        response.EnsureSuccessStatusCode();
    }
}
```

### 9.6 FakeTimeProvider for Time-Travel

```csharp
[Fact]
public async Task ScheduledMessage_ExecutesAtCorrectTime()
{
    var startTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    var fixture = new EncinaTestFixture()
        .WithFakeTimeProvider(startTime)
        .WithHandler<ScheduledMessageHandler>()
        .WithMockedScheduling();

    // Schedule for 1 hour later
    await fixture.SendAsync(new ScheduleCommand { DelayHours = 1 });

    // Verify scheduled time
    var scheduled = fixture.ScheduledMessageStore.GetMessages().Single();
    scheduled.ScheduledAtUtc.ShouldBe(startTime.AddHours(1).UtcDateTime);

    // Time travel
    fixture.AdvanceTimeByHours(1);

    // Now the message should be due
    fixture.GetCurrentTime().ShouldBe(startTime.AddHours(1));
}
```

---

## 10. Assertion Patterns

### 10.1 Railway-Oriented Assertions

```csharp
// Success assertions
result.ShouldBeSuccess();                           // Assert Right, return value
result.ShouldBeSuccess(expectedValue);              // Assert Right with value
result.ShouldBeSuccess(v => v.Id.ShouldNotBe(Guid.Empty)); // Assert with validator

// Async variants
await resultTask.ShouldBeSuccessAsync();
await resultTask.ShouldBeSuccessAsync(expectedValue);
```

### 10.2 Error Assertions

```csharp
// Generic error assertions
result.ShouldBeError();                             // Assert Left, return error
result.ShouldBeErrorWithCode("encina.notfound");    // Assert specific code
result.ShouldBeErrorContaining("not found");        // Assert message contains

// Domain-specific error assertions
result.ShouldBeValidationError();                   // Code starts with encina.validation
result.ShouldBeNotFoundError();                     // Code starts with encina.notfound
result.ShouldBeAuthorizationError();                // Code starts with encina.authorization
result.ShouldBeConflictError();                     // Code starts with encina.conflict
result.ShouldBeInternalError();                     // Code starts with encina.internal

// Async variants
await resultTask.ShouldBeValidationErrorAsync();
```

### 10.3 Collection Assertions

```csharp
// Either collection assertions
var results = await encina.StreamQuery(query).ToListAsync();

results.ShouldAllBeSuccess();                       // All are Right
results.ShouldContainSuccess(expectedValue);        // At least one Right matches
results.ShouldContainError();                       // At least one Left

var values = results.ShouldAllBeSuccess();          // Returns extracted values
values.ShouldContain(x => x.Id == orderId);
```

### 10.4 Aggregate Event Assertions

```csharp
// Assert aggregate raised specific events
order.ShouldHaveRaisedEvent<OrderCreated>();
order.ShouldHaveRaisedEvent<OrderCreated>(e => e.CustomerId.ShouldBe("CUST-001"));

// Assert multiple events in sequence
order.ShouldHaveRaisedEvents(
    typeof(OrderCreated),
    typeof(OrderItemAdded),
    typeof(OrderSubmitted)
);

// Assert aggregate version
order.ShouldHaveVersion(3);

// Assert no events were raised
order.ShouldNotHaveRaisedAnyEvents();

// Assert event count
order.UncommittedEvents.ShouldHaveCount(3);
```

### 10.5 Outbox/Store Verification

```csharp
// Verify messages were added to outbox
fixture.Outbox.GetMessages().ShouldHaveSingleItem();
fixture.Outbox.WasMessageAdded<OrderCreatedEvent>().ShouldBeTrue();
fixture.Outbox.GetMessagePayload<OrderCreatedEvent>()
    .OrderId.ShouldBe(expectedOrderId);

// Verify saga state
var saga = fixture.SagaStore.GetByCorrelationId(correlationId);
saga.ShouldNotBeNull();
saga.Status.ShouldBe("Completed");

// Verify scheduled messages
fixture.ScheduledMessageStore.GetMessages()
    .ShouldContain(m => m.ScheduledAtUtc > DateTime.UtcNow);
```

### 10.6 Streaming/IAsyncEnumerable Assertions

```csharp
// Memory-efficient first-item check (doesn't enumerate entire stream)
await stream.ShouldHaveFirstItem<OrderDto>(first =>
{
    first.Id.ShouldNotBe(Guid.Empty);
    first.Status.ShouldBe("Active");
});

// Assert stream produces at least N items
await stream.ShouldHaveAtLeast(5);

// Assert all items in stream match predicate (with early exit on failure)
await stream.ShouldAllSatisfy(item => item.IsValid.ShouldBeTrue());

// Assert stream is empty
await stream.ShouldBeEmpty();

// Assert stream has exact count
await stream.ShouldHaveCount(10);

// Collect and assert (only when full enumeration needed)
var items = await stream.ToListAsync();
items.ShouldAllBeSuccess();
```

### 10.7 Snapshot Assertions

```csharp
// Verify serialization is stable
await EncinaVerify.VerifyJson(responseObject);

// Verify error format
await EncinaVerify.VerifyError(error);

// Custom scrubbing for dynamic values
await EncinaVerify.VerifyJson(response, settings =>
{
    settings.ScrubMember("CreatedAtUtc");
    settings.ScrubMember("Id");
});
```

### 10.8 Architecture Assertions

```csharp
// Build and check rules
var result = EncinaArchitectureRulesBuilder
    .ForAssembly(assembly)
    .HandlersCannotDependOn("*.Infrastructure")
    .Build()
    .Check();

// result throws on violation with detailed message
```

---

## 11. Migration Execution Process

### 11.1 Phase-Issue Mapping

| Phase | Issue | Scope | Dependencies |
|-------|-------|-------|--------------|
| Phase 1 | [#499](https://github.com/dlrivada/Encina/issues/499) | `Encina` core package | None |
| Phase 2 | [#500](https://github.com/dlrivada/Encina/issues/500) | `Encina.DomainModeling` | Phase 1 |
| Phase 3 | [#501](https://github.com/dlrivada/Encina/issues/501) | `Encina.Messaging` | Phase 1 |
| Phase 4 | [#502](https://github.com/dlrivada/Encina/issues/502) | Database providers (ADO, Dapper, EF Core) | Phases 1-3 |
| Phase 5 | [#503](https://github.com/dlrivada/Encina/issues/503) | Caching providers | Phases 1-3 |
| Phase 6 | [#504](https://github.com/dlrivada/Encina/issues/504) | Message transports | Phases 1-3 |
| Phase 7 | [#505](https://github.com/dlrivada/Encina/issues/505) | Web Integration (AspNetCore, Refit, gRPC, SignalR) | Phases 1-3 |
| Phase 8 | [#506](https://github.com/dlrivada/Encina/issues/506) | Resilience & Observability | Phases 1-4 |
| Phase 9 | [#507](https://github.com/dlrivada/Encina/issues/507) | Validation providers | Phases 1-2 |
| Phase 10 | [#508](https://github.com/dlrivada/Encina/issues/508) | Serverless & Scheduling | Phases 1-3 |

Supporting issue: [#509](https://github.com/dlrivada/Encina/issues/509) - Evaluate Aspire Testing vs Testcontainers migration strategy

### 11.2 Migration Workflow Per Phase

```
┌─────────────────┐
│  1. Audit       │ ─── Identify tests to migrate, count dependencies
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  2. Plan        │ ─── Create migration checklist, estimate effort
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  3. Migrate     │ ─── One test class at a time, verify green
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  4. Review      │ ─── PR review with focus on patterns
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  5. Document    │ ─── Update examples, note learnings
└─────────────────┘
```

### 11.3 Acceptance Criteria Per Phase

Each phase issue should verify:

- [ ] All tests in scope pass after migration
- [ ] No direct dependencies on replaced libraries remain
- [ ] Coverage did not decrease
- [ ] At least 3 migration examples added to documentation
- [ ] CI pipeline passes
- [ ] Code review approved

### 11.4 Migration Checklist Template

```markdown
## Migration Checklist for [Package Name]

### Pre-Migration
- [ ] Count current tests: ___
- [ ] Current coverage: ___%
- [ ] Direct dependencies to replace:
  - [ ] FluentAssertions → Encina.Testing.Shouldly
  - [ ] AutoFixture → Encina.Testing.Bogus
  - [ ] Manual mocks → Encina.Testing.Fakes
  - [ ] Other: ___

### Migration
- [ ] Add PackageReferences to Encina.Testing.*
- [ ] Migrate test class: [ClassName1]
- [ ] Migrate test class: [ClassName2]
- [ ] ...
- [ ] Remove old PackageReferences

### Post-Migration
- [ ] All tests pass: ✓
- [ ] Coverage maintained: ___%
- [ ] No direct dependencies remain: ✓
- [ ] Documentation updated: ✓
```

### 11.5 Priority and Sequencing

**High Priority (Phases 1-3)**: Foundation packages that establish patterns

| Phase | Package | Why First |
|-------|---------|-----------|
| 1 | Encina | Core patterns, most visible |
| 2 | Encina.DomainModeling | Value objects, domain assertions |
| 3 | Encina.Messaging | Outbox/Inbox testing patterns |

**Medium Priority (Phases 4-7)**: Infrastructure with many tests

| Phase | Package | Notes |
|-------|---------|-------|
| 4 | Database Providers | Large test count, integration tests |
| 5 | Caching | Integration with Redis |
| 6 | Message Transports | Queue testing patterns |
| 7 | Web Integration | HTTP testing patterns |

**Lower Priority (Phases 8-10)**: Specialized packages

| Phase | Package | Notes |
|-------|---------|-------|
| 8 | Resilience & Observability | Polly, OpenTelemetry |
| 9 | Validation Providers | FluentValidation, etc. |
| 10 | Serverless & Scheduling | Lambda, Functions, Hangfire |

---

## 12. Available Encina.Testing Packages

| Package | Purpose | Key Types |
|---------|---------|-----------|
| `Encina.Testing` | Core testing infrastructure | `EncinaTestFixture`, `EncinaTestContext`, `EncinaFixture` |
| `Encina.Testing.Fakes` | In-memory fakes for messaging | `FakeEncina`, `FakeOutboxStore`, `FakeInboxStore`, `FakeSagaStore` |
| `Encina.Testing.Shouldly` | Railway-oriented assertions | `EitherShouldlyExtensions`, `ShouldBeSuccess()`, `ShouldBeError()` |
| `Encina.Testing.TUnit` | TUnit framework support | `TUnitEitherAssertions`, `EncinaTUnitFixture` |
| `Encina.Testing.Bogus` | Test data generation | `EncinaFaker<T>`, `OutboxMessageFaker`, `SagaStateFaker` |
| `Encina.Testing.WireMock` | HTTP API mocking | `EncinaWireMockFixture`, `EncinaRefitMockFixture` |
| `Encina.Testing.Verify` | Snapshot testing | `EncinaVerify`, `EncinaVerifySettings` |
| `Encina.Testing.Architecture` | Architecture testing | `EncinaArchitectureRulesBuilder`, `EncinaArchitectureTestBase` |
| `Encina.Testing.Respawn` | Database cleanup | `DatabaseRespawner`, `SqlServerRespawner`, `PostgreSqlRespawner` |
| `Encina.Testing.Testcontainers` | Docker container fixtures | `SqlServerContainerFixture`, `PostgreSqlContainerFixture`, `RedisContainerFixture` |
| `Encina.Testing.FsCheck` | Property-based testing | `EncinaArbitraries`, `EncinaProperties`, `PropertyTestBase` |
| `Encina.Testing.Pact` | Contract testing | `EncinaPactConsumerBuilder`, `EncinaPactProviderVerifier`, `EncinaPactFixture` |
| `Encina.Aspire.Testing` | Aspire integration testing | Aspire-specific fixtures and extensions |

---

## Additional Notes

1. **Prioritize core packages** that are most used and visible
2. **Don't break existing tests** - gradual migration
3. **Document patterns** in each phase to facilitate the following ones
4. **Update examples** in README of each Testing package
5. **JIT Bug Warning**: Load tests using `IAsyncEnumerable<Either<EncinaError, T>>` may fail in Release builds due to a .NET 10 JIT optimization bug
   - **Workaround**: Set `DOTNET_JitObjectStackAllocationConditionalEscape=0`
   - **Re-evaluate**: Remove workaround when .NET 10.0.x patch addresses the issue

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2024-01-03 | 1.0 | Initial document |
| 2024-01-04 | 2.0 | Complete rewrite per Issue #498 Phase 1 (Tasks 1-8) |
