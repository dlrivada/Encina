using System.Text.Json;
using Bogus;
using Shouldly;
using Xunit;

namespace Encina.Testing.Bogus.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaFaker{T}"/> and extension methods.
/// </summary>
public sealed class EncinaFakerTests
{
    [Fact]
    public void EncinaFaker_ShouldHaveDefaultSeed()
    {
        // Arrange & Act
        var faker1 = new EncinaFaker<TestClass>();
        var faker2 = new EncinaFaker<TestClass>();

        // Generate with same seed should produce same results
        var result1 = faker1.RuleFor(x => x.Name, f => f.Name.FirstName()).Generate();
        var result2 = faker2.RuleFor(x => x.Name, f => f.Name.FirstName()).Generate();

        // Assert
        result1.Name.ShouldBe(result2.Name);
    }

    [Fact]
    public void EncinaFaker_UseSeed_ShouldMakeResultsReproducible()
    {
        // Arrange
        var faker1 = new EncinaFaker<TestClass>().UseSeed(42);
        var faker2 = new EncinaFaker<TestClass>().UseSeed(42);

        faker1.RuleFor(x => x.Name, f => f.Name.FirstName());
        faker2.RuleFor(x => x.Name, f => f.Name.FirstName());

        // Act
        var result1 = faker1.Generate();
        var result2 = faker2.Generate();

        // Assert
        result1.Name.ShouldBe(result2.Name);
    }

    [Fact]
    public void EncinaFaker_DifferentSeeds_ShouldProduceDifferentResults()
    {
        // Arrange
        var faker1 = new EncinaFaker<TestClass>().UseSeed(1);
        var faker2 = new EncinaFaker<TestClass>().UseSeed(2);

        faker1.RuleFor(x => x.Name, f => f.Name.FirstName());
        faker2.RuleFor(x => x.Name, f => f.Name.FirstName());

        // Act
        var result1 = faker1.Generate();
        var result2 = faker2.Generate();

        // Assert - Different seeds should produce different results (most of the time)
        // Note: There's a tiny chance they could be the same by coincidence
        result1.Name.ShouldNotBe(result2.Name);
    }

    [Fact]
    public void EncinaFaker_WithLocale_ShouldSetLocale()
    {
        // Arrange & Act
        var faker = new EncinaFaker<TestClass>().WithLocale("es");

        // Assert
        faker.Locale.ShouldBe("es");
    }

    [Fact]
    public void EncinaFaker_StrictMode_ShouldEnableStrictMode()
    {
        // Arrange
        var faker = new EncinaFaker<TestClass>()
            .StrictMode(true)
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.Value, f => f.Random.Int());

        // Act - Should not throw because all properties have rules
        var result = faker.Generate();

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CorrelationId_ShouldGenerateGuid()
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act
        var correlationId = randomizer.CorrelationId();

        // Assert
        correlationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void UserId_ShouldGenerateWithDefaultPrefix()
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act
        var userId = randomizer.UserId();

        // Assert
        userId.ShouldStartWith("user_");
        userId.Length.ShouldBeGreaterThan(5);
    }

    [Fact]
    public void UserId_ShouldGenerateWithCustomPrefix()
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act
        var userId = randomizer.UserId("admin");

        // Assert
        userId.ShouldStartWith("admin_");
    }

    [Fact]
    public void TenantId_ShouldGenerateWithDefaultPrefix()
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act
        var tenantId = randomizer.TenantId();

        // Assert
        tenantId.ShouldStartWith("tenant_");
        tenantId.Length.ShouldBeGreaterThan(7);
    }

    [Fact]
    public void TenantId_ShouldGenerateWithCustomPrefix()
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act
        var tenantId = randomizer.TenantId("org");

        // Assert
        tenantId.ShouldStartWith("org_");
    }

    [Fact]
    public void IdempotencyKey_ShouldGenerateGuidString()
    {
        // Arrange
        var randomizer = new Randomizer();

        // Act
        var key = randomizer.IdempotencyKey();

        // Assert
        Guid.TryParse(key, out _).ShouldBeTrue();
    }

    [Fact]
    public void NotificationType_ShouldReturnValidType()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var notificationType = faker.NotificationType();

        // Assert
        notificationType.ShouldNotBeNullOrEmpty();
        notificationType.ShouldBeOneOf(
            "OrderCreated",
            "OrderCompleted",
            "OrderCancelled",
            "PaymentReceived",
            "PaymentFailed",
            "ShipmentDispatched",
            "CustomerRegistered",
            "InventoryUpdated");
    }

    [Fact]
    public void RequestType_ShouldReturnValidType()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var requestType = faker.RequestType();

        // Assert
        requestType.ShouldNotBeNullOrEmpty();
        requestType.ShouldBeOneOf(
            "CreateOrder",
            "UpdateOrder",
            "CancelOrder",
            "ProcessPayment",
            "RefundPayment",
            "RegisterCustomer",
            "UpdateInventory",
            "SendNotification");
    }

    [Fact]
    public void SagaType_ShouldReturnValidType()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var sagaType = faker.SagaType();

        // Assert
        sagaType.ShouldNotBeNullOrEmpty();
        sagaType.ShouldBeOneOf(
            "OrderFulfillmentSaga",
            "PaymentProcessingSaga",
            "CustomerOnboardingSaga",
            "InventoryReservationSaga",
            "ShippingCoordinationSaga");
    }

    [Fact]
    public void SagaStatus_ShouldReturnValidStatus()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var status = faker.SagaStatus();

        // Assert
        status.ShouldBeOneOf("Running", "Completed", "Compensating", "Failed");
    }

    [Fact]
    public void RecentUtc_ShouldReturnUtcDate()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var date = faker.Date.RecentUtc();

        // Assert
        date.Kind.ShouldBe(DateTimeKind.Utc);
        date.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void SoonUtc_ShouldReturnUtcDate()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var date = faker.Date.SoonUtc();

        // Assert
        date.Kind.ShouldBe(DateTimeKind.Utc);
        date.ShouldBeGreaterThanOrEqualTo(DateTime.UtcNow.AddSeconds(-1)); // Allow 1 second tolerance
    }

    [Fact]
    public void JsonContent_ShouldReturnValidJson()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var json = faker.JsonContent();

        // Assert
        json.ShouldStartWith("{");
        json.ShouldEndWith("}");
        json.ShouldContain(":");
    }

    [Fact]
    public void JsonContent_ShouldRespectPropertyCount()
    {
        // Arrange
        var faker = new Faker();

        // Act
        var json = faker.JsonContent(5);

        // Assert - parse JSON properly instead of fragile string splitting
        using var document = JsonDocument.Parse(json);
        var propertyCount = document.RootElement.EnumerateObject().Count();
        propertyCount.ShouldBe(5);
    }

    private sealed class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
