# Encina.Testing.WireMock

WireMock.NET integration for HTTP API mocking in integration tests. Provides fluent fixtures to stub HTTP responses, simulate faults, and verify API calls.

## Installation

```bash
dotnet add package Encina.Testing.WireMock
```

## Why WireMock?

When testing code that calls external HTTP APIs, you need reliable mocking:
- **Isolation**: Don't depend on external services being available
- **Determinism**: Control exactly what responses your code receives
- **Edge cases**: Simulate errors, timeouts, and malformed responses
- **Verification**: Assert that your code made the expected API calls

**WireMock advantages**:
- **Full HTTP mocking**: Runs a real HTTP server (not just HttpClient mocking)
- **Flexible matching**: Match requests by path, method, headers, body
- **Response simulation**: Delays, faults, sequences
- **Verification**: Record and verify all received requests

## Quick Start

### Basic Usage

```csharp
using System.Net;
using System.Net.Http;
using Encina.Testing.WireMock;
using Shouldly;
using Xunit;

public class PaymentServiceTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task ProcessPayment_ShouldCallPaymentGateway()
    {
        // Arrange - Stub the payment API (StubPost defaults to 201 Created)
        _fixture.StubPost("/api/payments", new { transactionId = "tx-123", status = "approved" });

        // Create HttpClient pointing to mock server
        using var client = _fixture.CreateClient();

        // Act - Your code calls the payment API
        var response = await client.PostAsync("/api/payments",
            new StringContent("{\"amount\": 100}"));

        // Assert - StubPost returns 201 Created by default
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify the call was made
        _fixture.VerifyCallMade("/api/payments", method: "POST");
    }
}
```

### Fluent Stubbing API

```csharp
// Chain multiple stubs
_fixture
    .StubGet("/api/users", new { users = new[] { "Alice", "Bob" } })
    .StubPost("/api/users", new { id = 123 })
    .StubDelete("/api/users/123");

// Custom status codes
_fixture.StubGet("/api/notfound", new { error = "Not found" }, statusCode: 404);

// All HTTP methods
_fixture.StubGet("/api/resource", responseBody);
_fixture.StubPost("/api/resource", responseBody);
_fixture.StubPut("/api/resource/1", responseBody);
_fixture.StubPatch("/api/resource/1", responseBody);
_fixture.StubDelete("/api/resource/1");

// Generic method
_fixture.Stub("OPTIONS", "/api/resource", responseBody);
```

## Fault Simulation

Test your resilience code by simulating failures:

```csharp
using Encina.Testing.WireMock;

// Empty response (connection drops)
_fixture.StubFault("/api/flaky", FaultType.EmptyResponse);

// Malformed response (corrupted data)
_fixture.StubFault("/api/corrupted", FaultType.MalformedResponse);

// Timeout (server doesn't respond)
_fixture.StubFault("/api/slow", FaultType.Timeout);
```

### Available Fault Types

| FaultType | Behavior |
|-----------|----------|
| `EmptyResponse` | Server accepts connection but returns empty body |
| `MalformedResponse` | Server returns corrupted/invalid data |
| `Timeout` | Server never responds (connection hangs) |

## Delayed Responses

Test timeout handling and async behavior:

```csharp
// Simulate slow API (500ms delay)
_fixture.StubDelay("/api/slow-endpoint",
    TimeSpan.FromMilliseconds(500),
    new { data = "delayed" });

// Test your timeout handling
using var client = _fixture.CreateClient();
client.Timeout = TimeSpan.FromSeconds(1); // Should still succeed

var response = await client.GetAsync("/api/slow-endpoint");
```

## Sequential Responses

Different responses for consecutive calls:

```csharp
// First call fails, retry succeeds
_fixture.StubSequence("/api/flaky",
    (new { error = "Temporary failure" }, 503),  // First call
    (new { status = "ok" }, 200));               // Second call (retry)
```

## Request Verification

Assert that your code made the expected API calls:

```csharp
// Verify a call was made
_fixture.VerifyCallMade("/api/users");

// Verify specific HTTP method
_fixture.VerifyCallMade("/api/users", method: "POST");

// Verify exact call count
_fixture.VerifyCallMade("/api/users", times: 3);

// Verify no calls were made
_fixture.VerifyNoCallsMade("/api/unused-endpoint");
```

### Inspecting Requests

```csharp
// Get all recorded requests
var requests = _fixture.GetReceivedRequests();

foreach (var request in requests)
{
    Console.WriteLine($"{request.Method} {request.Path}");
    Console.WriteLine($"Headers: {request.Headers}");
    Console.WriteLine($"Body: {request.Body}");
}
```

## Reset Between Tests

```csharp
// Clear all stubs and request history
_fixture.Reset();

// Clear only request history (keep stubs)
_fixture.ResetRequestHistory();
```

## Docker Container Fixture

For CI/CD or when you need a real WireMock container:

```csharp
using Encina.Testing.WireMock;

public class IntegrationTests : IAsyncLifetime
{
    private readonly WireMockContainerFixture _containerFixture = new();

    public Task InitializeAsync() => _containerFixture.InitializeAsync();
    public Task DisposeAsync() => _containerFixture.DisposeAsync();

    [Fact]
    public async Task Test_WithRealContainer()
    {
        var baseUrl = _containerFixture.BaseUrl;
        var adminClient = _containerFixture.CreateAdminClient();

        // Use the WireMock Admin API to configure stubs
        // ...
    }
}
```

## xUnit Integration

### Class Fixture (Shared across tests in a class)

```csharp
public class PaymentApiTests : IClassFixture<EncinaWireMockFixture>, IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture;

    public PaymentApiTests(EncinaWireMockFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.Reset(); // Clean slate for each test
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test1() { /* ... */ }

    [Fact]
    public async Task Test2() { /* ... */ }
}
```

### Collection Fixture (Shared across test classes)

```csharp
[CollectionDefinition("WireMock")]
public class WireMockCollection : ICollectionFixture<EncinaWireMockFixture> { }

[Collection("WireMock")]
public class PaymentTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _fixture;

    public PaymentTests(EncinaWireMockFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[Collection("WireMock")]
public class NotificationTests : IAsyncLifetime
{
    // Same fixture, different test class
}
```

## Integration with Dependency Injection

Configure your services to use the mock server:

```csharp
public class ServiceTests : IAsyncLifetime
{
    private readonly EncinaWireMockFixture _wireMock = new();
    private ServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        await _wireMock.InitializeAsync();

        var services = new ServiceCollection();
        services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
        {
            client.BaseAddress = new Uri(_wireMock.BaseUrl);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _wireMock.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task PaymentService_ShouldHandleApproval()
    {
        _wireMock.StubPost("/api/charge", new { approved = true });

        var paymentClient = _serviceProvider.GetRequiredService<IPaymentClient>();
        var result = await paymentClient.ChargeAsync(100);

        result.IsApproved.ShouldBeTrue();
    }
}
```

## Advanced: Direct WireMock Access

For advanced scenarios, access the underlying WireMock server:

```csharp
// Direct access to WireMock server
var server = _fixture.Server;

// Use WireMock.Net fluent API directly
server
    .Given(Request.Create()
        .WithPath("/api/*")
        .UsingPost()
        .WithHeader("Authorization", "Bearer *"))
    .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithBody(@"{ ""status"": ""ok"" }"));
```

## Performance Tips

1. **Reuse fixtures** - Use `IClassFixture<T>` or `ICollectionFixture<T>` to avoid starting a new server per test
2. **Reset, don't recreate** - Call `Reset()` between tests instead of recreating the fixture
3. **Use container fixture in CI** - The `WireMockContainerFixture` is more reliable in containerized CI environments

## Related Packages

- **Encina.Testing.Fakes** - In-memory fakes for IEncina and stores
- **Encina.Testing.Shouldly** - Shouldly assertions for Either and Aggregates
- **Encina.Testing.Respawn** - Database reset for integration tests

## License

MIT License - see LICENSE file for details.
