using Encina.Testing.Bogus;
using Encina.Testing.Examples.Domain;

namespace Encina.Testing.Examples.Unit;

/// <summary>
/// Examples demonstrating handler testing with EncinaTestFixture.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 5.1
/// </summary>
public sealed class HandlerTestExamples
{
    /// <summary>
    /// Pattern: Basic command handler test with EncinaTestFixture.
    /// Demonstrates fluent fixture configuration and Railway-oriented assertions.
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsOrderId()
    {
        // Arrange - Use EncinaTestFixture for clean setup
        var fixture = new EncinaTestFixture()
            .WithHandler<CreateOrderHandler>();

        // Use EncinaFaker for reproducible test data
        var command = new EncinaFaker<CreateOrderCommand>()
            .CustomInstantiator(f => new CreateOrderCommand
            {
                CustomerId = f.Random.UserId(),
                Amount = f.Finance.Amount(1, 1000)
            })
            .Generate();

        // Act - Send command through fixture
        var context = await fixture.SendAsync(command);

        // Assert - Use Railway-oriented assertions
        var orderId = context.Result.ShouldBeSuccess();
        orderId.ShouldNotBe(Guid.Empty);
    }

    /// <summary>
    /// Pattern: Testing validation error scenarios.
    /// Demonstrates domain-specific error assertions.
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithEmptyCustomerId_ReturnsValidationError()
    {
        // Arrange
        var fixture = new EncinaTestFixture()
            .WithHandler<CreateOrderHandler>();

        var command = new CreateOrderCommand
        {
            CustomerId = "",  // Invalid: empty
            Amount = 100.00m
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Use domain-specific error assertions
        context.Result
            .ShouldBeValidationError()
            .Message.ShouldContain("CustomerId");
    }

    /// <summary>
    /// Pattern: Testing with injected dependencies.
    /// Demonstrates WithService for custom dependency injection.
    /// </summary>
    [Fact]
    public async Task GetOrder_WhenOrderExists_ReturnsOrderDto()
    {
        // Arrange
        var repository = new InMemoryOrderRepository();
        var expectedOrder = new OrderDto(
            Id: Guid.NewGuid(),
            CustomerId: "CUST-001",
            Amount: 150.00m,
            CreatedAtUtc: DateTime.UtcNow);

        repository.Seed(expectedOrder);

        var fixture = new EncinaTestFixture()
            .WithHandler<GetOrderHandler>()
            .WithService<IOrderRepository>(repository);

        var query = new GetOrderQuery(expectedOrder.Id);

        // Act
        var context = await fixture.SendAsync(query);

        // Assert
        var result = context.Result.ShouldBeSuccess();
        result.Id.ShouldBe(expectedOrder.Id);
        result.CustomerId.ShouldBe(expectedOrder.CustomerId);
    }

    /// <summary>
    /// Pattern: Testing NotFound error scenarios.
    /// </summary>
    [Fact]
    public async Task GetOrder_WhenOrderDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var repository = new InMemoryOrderRepository();
        var fixture = new EncinaTestFixture()
            .WithHandler<GetOrderHandler>()
            .WithService<IOrderRepository>(repository);

        var query = new GetOrderQuery(Guid.NewGuid());

        // Act
        var context = await fixture.SendAsync(query);

        // Assert
        context.Result.ShouldBeNotFoundError();
    }
}
