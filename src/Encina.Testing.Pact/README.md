# Encina.Testing.Pact

Consumer-Driven Contract Testing integration for Encina using [PactNet](https://github.com/pact-foundation/pact-net).

## Overview

This package provides tools for implementing Consumer-Driven Contract (CDC) testing in microservices architectures using Encina. It helps ensure that:

- Consumer expectations match provider implementations
- Breaking changes are detected before deployment
- Contracts are versioned and tracked
- Integration testing is more reliable than full E2E tests

## Installation

```bash
dotnet add package Encina.Testing.Pact
```

## Quick Start

### Consumer Side: Defining Expectations

```csharp
using Encina.Testing.Pact;
using LanguageExt;

public class OrderServiceConsumerTests : IClassFixture<EncinaPactFixture>
{
    private readonly EncinaPactFixture _fixture;

    public OrderServiceConsumerTests(EncinaPactFixture fixture)
    {
        _fixture = fixture;
        // PactDirectory defaults to "./pacts"
        // To customize, create a derived fixture class or instantiate directly
    }

    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrder()
    {
        var orderId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Define what we expect from the Orders API
        var consumer = _fixture.CreateConsumer("WebApp", "OrdersAPI")
            .WithQueryExpectation(
                new GetOrderByIdQuery(orderId),
                Either<EncinaError, OrderDto>.Right(new OrderDto
                {
                    Id = orderId,
                    Status = "Created"
                }),
                description: "Get existing order by ID",
                providerState: "an order with ID 12345678 exists");

        await _fixture.VerifyAsync(consumer, async mockServerUri =>
        {
            // Test your consumer code against the mock
            using var client = mockServerUri.CreatePactHttpClient();
            var response = await client.SendQueryAsync<GetOrderByIdQuery, OrderDto>(
                new GetOrderByIdQuery(orderId));

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.IsRight.Should().BeTrue();
        });
    }

    [Fact]
    public async Task CreateOrder_InvalidData_ReturnsValidationError()
    {
        var consumer = _fixture.CreateConsumer("WebApp", "OrdersAPI")
            .WithCommandFailureExpectation<CreateOrderCommand, OrderDto>(
                new CreateOrderCommand(Guid.Empty, ""),
                EncinaErrors.Create("encina.validation.failed", "Order ID is required"),
                description: "Create order with invalid data");

        await consumer.VerifyAsync(async uri =>
        {
            // Your client code testing here
        });
    }
}
```

### Provider Side: Verifying Contracts

```csharp
public class OrdersApiProviderTests
{
    [Fact]
    public async Task Verify_WebApp_Contract()
    {
        // Set up Encina with actual handlers
        var services = new ServiceCollection();
        services.AddEncina(cfg => cfg.AddHandlersFromAssemblyContaining<CreateOrderHandler>());
        var serviceProvider = services.BuildServiceProvider();
        var encina = serviceProvider.GetRequiredService<IEncina>();

        var verifier = new EncinaPactProviderVerifier(encina, serviceProvider)
            .WithProviderName("OrdersAPI")
            .WithProviderState("an order with ID 12345678 exists", async () =>
            {
                // Set up test data
                await SetupOrderInDatabase();
            })
            .WithProviderState("no orders exist", async () =>
            {
                await ClearOrdersFromDatabase();
            });

        var result = await verifier.VerifyAsync("./pacts/webapp-ordersapi.json");

        result.Success.Should().BeTrue();
    }
}
```

## Key Components

### EncinaPactConsumerBuilder

Fluent builder for defining consumer-side expectations:

```csharp
var consumer = new EncinaPactConsumerBuilder("ConsumerName", "ProviderName")
    // Commands
    .WithCommandExpectation(command, expectedResponse, description, providerState)
    .WithCommandFailureExpectation<TCmd, TResp>(command, expectedError)

    // Queries
    .WithQueryExpectation(query, expectedResponse, description, providerState)
    .WithQueryFailureExpectation<TQuery, TResp>(query, expectedError)

    // Notifications
    .WithNotificationExpectation(notification, description, providerState);

await consumer.VerifyAsync(async uri =>
{
    // Test your client code using the mock server at 'uri'
});
```

### EncinaPactProviderVerifier

Verifies Pact contracts against provider implementation:

```csharp
var verifier = new EncinaPactProviderVerifier(encina, serviceProvider)
    .WithProviderName("MyService")
    .WithProviderState("state name", async () => { /* setup */ })
    .WithProviderState("state with params", async (params) => { /* setup with params */ });

var result = await verifier.VerifyAsync("./pacts/consumer-provider.json");
```

### EncinaPactFixture

xUnit fixture for simplified test setup:

```csharp
// Option 1: Use default directory with IClassFixture
public class MyTests : IClassFixture<EncinaPactFixture>
{
    private readonly EncinaPactFixture _fixture;

    public MyTests(EncinaPactFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _fixture.OutputHelper = outputHelper; // Optional xUnit logging
    }

    [Fact]
    public async Task MyContractTest()
    {
        var consumer = _fixture.CreateConsumer("Consumer", "Provider");
        // ... define expectations
        await consumer.VerifyAsync(async uri => { /* test */ });
    }
}

// Option 2: Custom directory without IClassFixture
public class CustomDirectoryTests : IAsyncLifetime
{
    private readonly EncinaPactFixture _fixture = new() { PactDirectory = "./custom-pacts" };

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();
}
```

### Extension Methods

Helper methods for HTTP testing:

```csharp
// Create configured HttpClient
using var client = mockServerUri.CreatePactHttpClient();

// Send Encina requests
var cmdResponse = await client.SendCommandAsync<MyCommand, MyResponse>(command);
var queryResponse = await client.SendQueryAsync<MyQuery, MyResponse>(query);
var notifResponse = await client.PublishNotificationAsync(notification);

// Read responses as Either
var result = await response.ReadAsEitherAsync<MyResponse>();
```

## Error Code Mapping

Encina error codes are automatically mapped to HTTP status codes:

| Error Code Prefix | HTTP Status |
|-------------------|-------------|
| `encina.validation` | 400 Bad Request |
| `encina.authorization` | 403 Forbidden |
| `encina.authentication` | 401 Unauthorized |
| `encina.notfound` | 404 Not Found |
| `encina.conflict` | 409 Conflict |
| `encina.timeout` | 408 Request Timeout |
| `encina.ratelimit` | 429 Too Many Requests |
| Other | 500 Internal Server Error |

## Provider States

Provider states allow you to set up test conditions:

```csharp
// Simple state
verifier.WithProviderState("user is logged in", async () =>
{
    await SetupUserSession();
});

// State with parameters
verifier.WithProviderState("order exists", async (params) =>
{
    var orderId = params["orderId"]?.ToString();
    await CreateOrder(orderId);
});

// Synchronous state
verifier.WithProviderState("database is empty", () =>
{
    ClearDatabase();
});
```

## Best Practices

1. **Keep contracts focused**: One Pact file per consumer-provider pair
2. **Use meaningful descriptions**: Help identify failing tests
3. **Define clear provider states**: Make test setup reproducible
4. **Version your contracts**: Store in version control
5. **Verify in CI/CD**: Run provider verification on every deployment
6. **Use Pact Broker**: Consider using a Pact Broker for larger systems

## Integration with Pact Broker

For production usage, consider using the Pact Broker:

```csharp
// Future: Publish to broker
// await consumer.PublishToBrokerAsync("https://broker.pact.io", "1.0.0");

// Future: Verify from broker
var result = await verifier.VerifyFromBrokerAsync(
    brokerUrl: "https://broker.pact.io",
    providerName: "OrdersAPI",
    consumerVersionTag: "main");
```

## Dependencies

- [PactNet](https://www.nuget.org/packages/PactNet/) - Pact implementation for .NET
- [Encina](https://www.nuget.org/packages/Encina/) - Core mediator library
- [Encina.Messaging](https://www.nuget.org/packages/Encina.Messaging/) - Messaging abstractions

## References

- [PactNet GitHub](https://github.com/pact-foundation/pact-net)
- [Pact Documentation](https://docs.pact.io/)
- [Consumer-Driven Contract Testing](https://martinfowler.com/articles/consumerDrivenContracts.html)
- [Microsoft CDC Testing Guide](https://microsoft.github.io/code-with-engineering-playbook/automated-testing/cdc-testing/)
