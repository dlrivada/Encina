using Bogus;
using Encina.Testing.Bogus;
using Encina.Testing.Examples.Domain;

namespace Encina.Testing.Examples.TestData;

/// <summary>
/// Examples demonstrating Bogus test data generation with EncinaFaker.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 8.1
/// </summary>
public sealed class BogusExamples
{
    /// <summary>
    /// Pattern: Basic EncinaFaker with default seed for reproducibility.
    /// </summary>
    [Fact]
    public void EncinaFaker_GeneratesReproducibleData()
    {
        // Arrange - Default seed is 12345
        var faker1 = new EncinaFaker<CreateOrderCommand>()
            .RuleFor(o => o.CustomerId, f => f.Random.AlphaNumeric(10))
            .RuleFor(o => o.Amount, f => f.Finance.Amount(10, 1000));

        var faker2 = new EncinaFaker<CreateOrderCommand>()
            .RuleFor(o => o.CustomerId, f => f.Random.AlphaNumeric(10))
            .RuleFor(o => o.Amount, f => f.Finance.Amount(10, 1000));

        // Act
        var order1 = faker1.Generate();
        var order2 = faker2.Generate();

        // Assert - Same seed produces same data
        order1.CustomerId.ShouldBe(order2.CustomerId);
        order1.Amount.ShouldBe(order2.Amount);
    }

    /// <summary>
    /// Pattern: Custom seed for different test scenarios.
    /// </summary>
    [Fact]
    public void EncinaFaker_CustomSeed()
    {
        // Arrange
        var faker = new EncinaFaker<CreateOrderCommand>()
            .UseSeed(42)
            .RuleFor(o => o.CustomerId, f => f.Random.AlphaNumeric(10))
            .RuleFor(o => o.Amount, f => f.Finance.Amount());

        // Act
        var order = faker.Generate();

        // Assert
        order.ShouldNotBeNull();
        order.CustomerId.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Pattern: Generate multiple test items.
    /// </summary>
    [Fact]
    public void EncinaFaker_GenerateMultiple()
    {
        // Arrange
        var faker = new EncinaFaker<CreateOrderCommand>()
            .RuleFor(o => o.CustomerId, f => $"CUST-{f.Random.Number(1000, 9999)}")
            .RuleFor(o => o.Amount, f => f.Finance.Amount(1, 500));

        // Act
        var orders = faker.Generate(5);

        // Assert
        orders.Count.ShouldBe(5);
        orders.Select(o => o.CustomerId).Distinct().Count().ShouldBe(5);
    }

    /// <summary>
    /// Pattern: Encina-specific extensions for domain data.
    /// </summary>
    [Fact]
    public void EncinaExtensions_GenerateDomainData()
    {
        // Arrange
        var faker = new Faker();

        // Act - Use Encina-specific extensions
        var correlationId = faker.Random.CorrelationId();
        var userId = faker.Random.UserId("user");
        var tenantId = faker.Random.TenantId("tenant");
        var idempotencyKey = faker.Random.IdempotencyKey();

        // Assert
        correlationId.ShouldNotBe(Guid.Empty);
        userId.ShouldStartWith("user_");
        tenantId.ShouldStartWith("tenant_");
        idempotencyKey.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Pattern: Entity ID generation.
    /// </summary>
    [Fact]
    public void EncinaExtensions_EntityIdGeneration()
    {
        // Arrange
        var faker = new Faker();

        // Act - Generate different ID types
        var guidId = faker.Random.GuidEntityId();
        var intId = faker.Random.IntEntityId();
        var longId = faker.Random.LongEntityId();
        var stringId = faker.Random.StringEntityId(8, "ORD");

        // Assert
        guidId.ShouldNotBe(Guid.Empty);
        intId.ShouldBeGreaterThan(0);
        longId.ShouldBeGreaterThan(0);
        stringId.ShouldStartWith("ORD_");
    }

    /// <summary>
    /// Pattern: Strongly-typed ID value generation.
    /// </summary>
    [Fact]
    public void EncinaExtensions_StronglyTypedIdValues()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var guidValue = faker.Random.StronglyTypedIdValue<Guid>();
        var intValue = faker.Random.StronglyTypedIdValue<int>();
        var stringValue = faker.Random.StringStronglyTypedIdValue(12, "SKU");

        // Assert
        guidValue.ShouldNotBe(Guid.Empty);
        intValue.ShouldBeGreaterThan(0);
        stringValue.ShouldStartWith("SKU_");
    }

    /// <summary>
    /// Pattern: Notification and request type generation.
    /// </summary>
    [Fact]
    public void EncinaExtensions_MessageTypes()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var notificationType = faker.NotificationType();
        var requestType = faker.RequestType();
        var sagaType = faker.SagaType();
        var sagaStatus = faker.SagaStatus();

        // Assert
        notificationType.ShouldNotBeNullOrWhiteSpace();
        requestType.ShouldNotBeNullOrWhiteSpace();
        sagaType.ShouldEndWith("Saga");
        sagaStatus.ShouldBeOneOf("Running", "Completed", "Compensating", "Failed");
    }

    /// <summary>
    /// Pattern: Date/time generation with UTC.
    /// </summary>
    [Fact]
    public void EncinaExtensions_UtcDates()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var recentUtc = faker.Date.RecentUtc(7);
        var soonUtc = faker.Date.SoonUtc(7);

        // Assert
        recentUtc.Kind.ShouldBe(DateTimeKind.Utc);
        soonUtc.Kind.ShouldBe(DateTimeKind.Utc);
        recentUtc.ShouldBeLessThan(DateTime.UtcNow);
        soonUtc.ShouldBeGreaterThan(DateTime.UtcNow);
    }

    /// <summary>
    /// Pattern: JSON content generation.
    /// </summary>
    [Fact]
    public void EncinaExtensions_JsonContent()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var json = faker.JsonContent(3);

        // Assert
        json.ShouldStartWith("{");
        json.ShouldEndWith("}");
    }

    /// <summary>
    /// Pattern: Value object generation (DateRange, TimeRange).
    /// </summary>
    [Fact]
    public void EncinaExtensions_ValueObjects()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var (startDate, endDate) = faker.Date.DateRangeValue(30, 30);
        var (startTime, endTime) = faker.Date.TimeRangeValue(1, 8);

        // Assert
        startDate.ShouldBeLessThan(endDate);
        startTime.ShouldBeLessThan(endTime);
    }
}
