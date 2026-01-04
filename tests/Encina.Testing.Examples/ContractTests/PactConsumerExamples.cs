using Encina.Testing.Examples.Domain;
using Encina.Testing.Pact;
using Shouldly;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Encina.Testing.Examples.ContractTests;

/// <summary>
/// Examples demonstrating consumer-driven contract testing with Encina.Testing.Pact.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 6.1
/// </summary>
/// <remarks>
/// These examples show the EncinaPactConsumerBuilder API patterns.
/// Note: Actual Pact tests require proper consumer/provider setup.
/// </remarks>
public sealed class PactConsumerExamples : IDisposable
{
    private readonly EncinaPactConsumerBuilder _consumer;

    public PactConsumerExamples(ITestOutputHelper output)
    {
        // Create consumer builder with consumer/provider names and pact output directory
        _consumer = new EncinaPactConsumerBuilder(
            consumerName: "OrderService",
            providerName: "InventoryService",
            pactDirectory: "./pacts",
            outputWriter: output);
    }

    /// <summary>
    /// Pattern: Basic query expectation with success response.
    /// Uses GetOrderQuery which implements IQuery&lt;OrderDto&gt;.
    /// </summary>
    [Fact]
    public async Task QueryExpectation_SuccessResponse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var query = new GetOrderQuery(orderId);

        var expectedResponse = Either<EncinaError, OrderDto>.Right(
            new OrderDto(
                Id: orderId,
                CustomerId: "CUST-001",
                Amount: 150.00m,
                CreatedAtUtc: DateTime.UtcNow));

        _consumer.WithQueryExpectation(
            query,
            expectedResponse,
            description: "Get order by ID",
            providerState: $"Order {orderId} exists");

        // Act & Assert
        await _consumer.VerifyAsync(async mockServerUri =>
        {
            using var client = mockServerUri.CreatePactHttpClient();

            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(query);

            response.EnsureSuccessStatusCode();
            var result = await response.ReadAsEitherAsync<OrderDto>();
            result.IsRight.ShouldBeTrue();
        });
    }

    /// <summary>
    /// Pattern: Query with not found response.
    /// </summary>
    [Fact]
    public async Task QueryExpectation_NotFoundResponse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var query = new GetOrderQuery(nonExistentId);

        var notFoundError = EncinaErrors.Create(
            "encina.notfound.order",
            $"Order {nonExistentId} not found");

        _consumer.WithQueryFailureExpectation<GetOrderQuery, OrderDto>(
            query,
            notFoundError,
            description: "Order not found returns 404",
            providerState: "Order does not exist");

        // Act & Assert
        await _consumer.VerifyAsync(async mockServerUri =>
        {
            using var client = mockServerUri.CreatePactHttpClient();

            var response = await client.SendQueryAsync<GetOrderQuery, OrderDto>(query);

            // Not found errors return 404
            ((int)response.StatusCode).ShouldBe(404);
        });
    }

    /// <summary>
    /// Pattern: Notification expectation.
    /// </summary>
    [Fact]
    public async Task NotificationExpectation()
    {
        // Arrange
        var notification = new OrderCreatedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: "CUST-001",
            Amount: 99.99m,
            CreatedAtUtc: DateTime.UtcNow);

        _consumer.WithNotificationExpectation(
            notification,
            description: "Order created notification",
            providerState: "Event publishing is enabled");

        // Act & Assert
        await _consumer.VerifyAsync(async mockServerUri =>
        {
            using var client = mockServerUri.CreatePactHttpClient();

            var response = await client.PublishNotificationAsync(notification);

            response.EnsureSuccessStatusCode();
        });
    }

    /// <summary>
    /// Pattern: Getting mock server URI for custom HTTP calls.
    /// </summary>
    [Fact]
    public async Task GetMockServerUri_ForCustomHttpCalls()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var query = new GetOrderQuery(orderId);

        _consumer.WithQueryExpectation(
            query,
            Either<EncinaError, OrderDto>.Right(
                new OrderDto(orderId, "CUST-001", 100m, DateTime.UtcNow)),
            description: "Get order for custom HTTP test");

        Uri? capturedUri = null;

        // Act
        await _consumer.VerifyAsync(async uri =>
        {
            capturedUri = uri;

            // Can use standard HttpClient with the mock server URI
            using var client = new HttpClient { BaseAddress = uri };
            var response = await client.PostAsJsonAsync(
                "/api/queries/GetOrderQuery",
                query);
            response.EnsureSuccessStatusCode();
        });

        // Assert
        capturedUri.ShouldNotBeNull();
        _consumer.GetMockServerUri().ShouldBe(capturedUri);
    }

    /// <summary>
    /// Pattern: Using synchronous verification.
    /// </summary>
    [Fact]
    public void SynchronousVerification()
    {
        // Arrange
        var query = new GetOrderQuery(Guid.NewGuid());

        _consumer.WithQueryExpectation(
            query,
            Either<EncinaError, OrderDto>.Right(
                new OrderDto(query.OrderId, "SYNC-TEST", 50m, DateTime.UtcNow)),
            description: "Sync verification test");

        // Act & Assert - Use synchronous Verify
        _consumer.Verify(mockServerUri =>
        {
            using var client = new HttpClient { BaseAddress = mockServerUri };
            var response = client.PostAsJsonAsync(
                "/api/queries/GetOrderQuery",
                query).Result;
            response.EnsureSuccessStatusCode();
        });
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }
}
