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
- `Encina.Testing` - `EncinaTestFixture`, `EncinaTestContext`
- `Encina.Testing.Shouldly` - `EitherShouldlyExtensions`
- `Encina.Testing.Fakes` - `FakeEncina`, `FakeOutboxStore`, etc.
- `Encina.Testing.Bogus` - `EncinaFaker<T>`

**Pattern**:
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

**Execution Time**: < 1ms per test

### 5.2 Integration Tests

**Purpose**: Test against real infrastructure (databases, queues, etc.).

**Primary Tools**:
- `Encina.Aspire.Testing` - For Aspire-compatible resources
- `Encina.Testing.Testcontainers` - For Docker-based fixtures
- `Encina.Testing.Respawn` - Database cleanup between tests

**Pattern with Aspire**:
```csharp
public class OrderIntegrationTests(AspireFixture fixture)
    : IClassFixture<AspireFixture>
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

**Pattern with Testcontainers**:
```csharp
public class OrderIntegrationTests : SqlServerIntegrationTestBase
{
    [Fact]
    public async Task CreateOrder_ShouldPersistToDatabase()
    {
        // Uses inherited connection from SqlServerIntegrationTestBase
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
- `Encina.Testing.Architecture` - `EncinaArchitectureRulesBuilder`

**Pattern**:
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
}
```

### 5.4 Contract Tests (Pact)

**Purpose**: Verify consumer-provider contracts for APIs.

**Primary Tools**:
- `Encina.Testing.Pact` - `EncinaPactConsumerBuilder`, `EncinaPactProviderVerifier`

**Consumer Test Pattern**:
```csharp
public class OrderServiceConsumerTests : IClassFixture<EncinaPactFixture>
{
    private readonly EncinaPactFixture _fixture;

    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrder()
    {
        var consumer = _fixture.CreateConsumer("OrderClient", "OrderService")
            .WithQueryExpectation(
                new GetOrderQuery { OrderId = Guid.Parse("...") },
                Either<EncinaError, OrderDto>.Right(new OrderDto { ... }));

        await _fixture.VerifyAsync(consumer, async uri =>
        {
            using var client = uri.CreatePactHttpClient();
            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(
                new GetOrderQuery { OrderId = Guid.Parse("...") });

            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.ShouldBeSuccess();
        });
    }
}
```

**Provider Verification Pattern**:
```csharp
public class OrderServiceProviderTests : IClassFixture<EncinaPactFixture>
{
    [Fact]
    public async Task VerifyPactWithOrderClient()
    {
        var verifier = _fixture.CreateVerifier("OrderService")
            .WithProviderState("an order exists", async () =>
            {
                await SeedTestOrder();
            });

        var result = await verifier.VerifyAsync("pacts/OrderClient-OrderService.json");
        result.Success.ShouldBeTrue();
    }
}
```

### 5.5 Property-Based Tests

**Purpose**: Test invariants with randomly generated inputs.

**Primary Tools**:
- `Encina.Testing.FsCheck` - `EncinaProperties`, `EncinaArbitraries`
- `Encina.Testing.Bogus` - For simpler random generation

**Pattern with FsCheck**:
```csharp
public class MoneyPropertyTests : PropertyTestBase
{
    [Property]
    public Property Money_Addition_IsCommutative()
    {
        return EncinaProperties.ForAll(
            EncinaArbitraries.Money(),
            EncinaArbitraries.Money(),
            (a, b) => a + b == b + a);
    }

    [Property]
    public Property Money_FromPositiveAmount_NeverFails()
    {
        return Prop.ForAll(
            Gen.Choose(1, 1_000_000).ToArbitrary(),
            amount =>
            {
                var result = Money.From(amount, Currency.USD);
                return result.IsRight;
            });
    }
}
```

**Pattern with Bogus for simpler cases**:
```csharp
[Theory]
[MemberData(nameof(RandomOrders))]
public async Task CreateOrder_WithAnyValidOrder_ShouldSucceed(CreateOrderCommand command)
{
    var result = await _fixture.SendAsync(command);
    result.Result.ShouldBeSuccess();
}

public static IEnumerable<object[]> RandomOrders()
{
    var faker = new EncinaFaker<CreateOrderCommand>()
        .RuleFor(x => x.CustomerId, f => f.Random.UserId())
        .RuleFor(x => x.Amount, f => f.Finance.Amount(1, 10000));

    return faker.Generate(10).Select(x => new object[] { x });
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

### 6.1 Assertion Libraries

| Before | After | Notes |
|--------|-------|-------|
| `FluentAssertions` | `Encina.Testing.Shouldly` | Use `ShouldBeSuccess()`, `ShouldBeError()` |
| `Assert.True(result.IsRight)` | `result.ShouldBeSuccess()` | Domain-specific assertions |
| `Assert.Equal(expected, actual)` | `actual.ShouldBe(expected)` | Shouldly syntax |
| `result.Should().BeRight()` | `result.ShouldBeSuccess()` | Railway-oriented |

### 6.2 Test Data Generation

| Before | After | Notes |
|--------|-------|-------|
| `AutoFixture` | `Encina.Testing.Bogus` | Use `EncinaFaker<T>` |
| `new Fixture().Create<T>()` | `new EncinaFaker<T>().Generate()` | Explicit rules preferred |
| Manual object instantiation | `EncinaFaker<T>` with rules | Reproducible with seed |
| `Builder<T>` pattern | `EncinaFaker<T>.RuleFor()` | Fluent configuration |

### 6.3 Mocking and Fakes

| Before | After | Notes |
|--------|-------|-------|
| `new Mock<IOutboxStore>()` | `new FakeOutboxStore()` | Behavioral fake with verification |
| `Mock<IEncina>()` | `FakeEncina` | Full in-memory implementation |
| Manual mock setup | `EncinaTestFixture.WithMockedOutbox()` | Fluent configuration |
| Custom test doubles | `Encina.Testing.Fakes` | Pre-built fakes for messaging |

### 6.4 Infrastructure Fixtures

| Before | After | Notes |
|--------|-------|-------|
| `Testcontainers.MsSql` | `SqlServerContainerFixture` | Wrapped with Encina conventions |
| `Testcontainers.PostgreSql` | `PostgreSqlContainerFixture` | Wrapped with Encina conventions |
| Manual Docker setup | `EncinaContainers` factory | Standardized container creation |
| Aspire `DistributedApplicationTestingBuilder` | `Encina.Aspire.Testing` | When using Aspire |

### 6.5 Test Framework Extensions

| Before | After | Notes |
|--------|-------|-------|
| xUnit raw assertions | `Encina.Testing.Shouldly` | Enhanced error messages |
| TUnit with custom assertions | `Encina.Testing.TUnit` | TUnit-specific extensions |
| Verify with custom converters | `Encina.Testing.Verify` | Pre-configured for Encina types |

### 6.6 Specialized Testing

| Before | After | Notes |
|--------|-------|-------|
| WireMock manual setup | `EncinaWireMockFixture` | HTTP mocking |
| Respawn manual config | `DatabaseRespawner` | Database cleanup |
| ArchUnitNET manual rules | `EncinaArchitectureRulesBuilder` | Architecture testing |
| PactNet raw usage | `EncinaPactConsumerBuilder` | Contract testing |
| FsCheck raw arbitraries | `EncinaArbitraries` | Property testing |

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

### Example 5: Error Handling Test

**BEFORE** (Manual error extraction):
```csharp
[Fact]
public async Task CreateOrder_WithInvalidCustomer_ReturnsValidationError()
{
    // ... setup ...

    var result = await encina.Send(new CreateOrderCommand { CustomerId = "" });

    Assert.True(result.IsLeft);
    var error = result.Match(Right: _ => null!, Left: e => e);
    Assert.NotNull(error);
    Assert.StartsWith("encina.validation", error.GetCode().IfNone(""));
    Assert.Contains("CustomerId", error.Message);
}
```

**AFTER** (Using ShouldBeValidationError):
```csharp
[Fact]
public async Task CreateOrder_WithInvalidCustomer_ReturnsValidationError()
{
    // Arrange
    var fixture = new EncinaTestFixture()
        .WithHandler<CreateOrderHandler>()
        .WithValidator<CreateOrderValidator>();

    // Act
    var context = await fixture.SendAsync(new CreateOrderCommand { CustomerId = "" });

    // Assert
    context.Result
        .ShouldBeValidationError()
        .Message.ShouldContain("CustomerId");
}
```

---

### Example 6: Saga Orchestration Test

**BEFORE** (Manual saga tracking):
```csharp
[Fact]
public async Task OrderFulfillmentSaga_CompletesAllSteps()
{
    var sagaStore = new InMemorySagaStore();
    // ... complex setup ...

    await encina.Send(new StartOrderFulfillmentCommand { OrderId = orderId });

    var saga = sagaStore.GetByCorrelationId(orderId);
    Assert.NotNull(saga);
    Assert.Equal("Completed", saga.Status);
    Assert.Equal(3, saga.CompletedSteps.Count);
}
```

**AFTER** (Using EncinaTestFixture with FakeSagaStore):
```csharp
[Fact]
public async Task OrderFulfillmentSaga_CompletesAllSteps()
{
    // Arrange
    var fixture = new EncinaTestFixture()
        .WithHandler<OrderFulfillmentSagaHandler>()
        .WithMockedSaga()
        .WithMockedOutbox();

    var orderId = Guid.NewGuid();

    // Act
    await fixture.SendAsync(new StartOrderFulfillmentCommand { OrderId = orderId });

    // Assert
    var saga = fixture.SagaStore.GetByCorrelationId(orderId);
    saga.ShouldNotBeNull();
    saga.Status.ShouldBe("Completed");
    saga.CompletedSteps.Count.ShouldBe(3);
}
```

---

### Example 7: HTTP API Mock Test

**BEFORE** (Manual WireMock setup):
```csharp
[Fact]
public async Task PaymentGateway_ReturnsSuccess()
{
    using var server = WireMockServer.Start();
    server.Given(Request.Create().WithPath("/payments").UsingPost())
          .RespondWith(Response.Create()
              .WithStatusCode(200)
              .WithBody("{\"transactionId\":\"TXN-001\",\"status\":\"approved\"}"));

    var client = new PaymentGatewayClient(new HttpClient { BaseAddress = new Uri(server.Url!) });

    var result = await client.ProcessPaymentAsync(new PaymentRequest { Amount = 100 });

    Assert.True(result.IsRight);
}
```

**AFTER** (Using EncinaWireMockFixture):
```csharp
public class PaymentGatewayTests : IClassFixture<EncinaWireMockFixture>
{
    private readonly EncinaWireMockFixture _wireMock;

    [Fact]
    public async Task PaymentGateway_ReturnsSuccess()
    {
        // Arrange
        _wireMock.SetupPostEndpoint("/payments", new
        {
            transactionId = "TXN-001",
            status = "approved"
        });

        var client = new PaymentGatewayClient(_wireMock.CreateClient());

        // Act
        var result = await client.ProcessPaymentAsync(
            new EncinaFaker<PaymentRequest>()
                .RuleFor(x => x.Amount, f => f.Finance.Amount())
                .Generate());

        // Assert
        result.ShouldBeSuccess();
    }
}
```

---

### Example 8: Architecture Constraint Test

**BEFORE** (Manual reflection checks):
```csharp
[Fact]
public void Handlers_ShouldNotReferenceDataAccessDirectly()
{
    var handlerAssembly = typeof(CreateOrderHandler).Assembly;
    var handlerTypes = handlerAssembly.GetTypes()
        .Where(t => t.Name.EndsWith("Handler"));

    foreach (var handler in handlerTypes)
    {
        var fields = handler.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            Assert.False(
                field.FieldType.Namespace?.StartsWith("System.Data") ?? false,
                $"Handler {handler.Name} should not depend on System.Data");
        }
    }
}
```

**AFTER** (Using EncinaArchitectureRulesBuilder):
```csharp
public class ArchitectureTests : EncinaArchitectureTestBase
{
    [Fact]
    public void Handlers_ShouldNotReferenceDataAccessDirectly()
    {
        var rules = EncinaArchitectureRulesBuilder
            .ForAssembly(typeof(CreateOrderHandler).Assembly)
            .TypesMatching(".*Handler")
            .ShouldNotDependOn("System.Data")
            .ShouldNotDependOn("Microsoft.EntityFrameworkCore")
            .Build();

        rules.Check();
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

// Customize
var customOutbox = new OutboxMessageFaker()
    .WithNotificationType("OrderCreated")
    .WithStatus(OutboxMessageStatus.Pending)
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

### 9.3 Database Fixtures

```csharp
// Integration test with database
public class OrderRepositoryTests : SqlServerIntegrationTestBase
{
    // Base class handles:
    // - Container lifecycle (StartAsync/StopAsync)
    // - Connection management
    // - Schema creation

    protected override async Task SeedDataAsync()
    {
        // Optional: seed test data
        await Connection.ExecuteAsync("INSERT INTO ...");
    }

    [Fact]
    public async Task MyTest()
    {
        // Connection is available and ready
        var repo = new OrderRepository(Connection);
        // ...
    }
}
```

### 9.4 Collection Fixtures

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

### 10.4 Outbox/Store Verification

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
```

### 10.5 Snapshot Assertions

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

### 10.6 Architecture Assertions

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
| 2024-01-04 | 2.0 | Complete rewrite per Issue #498 Task 1 requirements |
