using Encina.Compliance.GDPR;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="ProcessingActivityMapper"/>.
/// </summary>
public class ProcessingActivityMapperTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 2, 24, 12, 0, 0, TimeSpan.Zero);

    private static ProcessingActivity CreateActivity() => new()
    {
        Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        Name = "Order Processing",
        Purpose = "Fulfil customer orders",
        LawfulBasis = LawfulBasis.Contract,
        CategoriesOfDataSubjects = ["Customers", "Suppliers"],
        CategoriesOfPersonalData = ["Name", "Email", "Address"],
        Recipients = ["Shipping Provider", "Payment Gateway"],
        ThirdCountryTransfers = "US data center",
        Safeguards = "Standard contractual clauses",
        RetentionPeriod = TimeSpan.FromDays(730),
        SecurityMeasures = "Encryption at rest and in transit",
        RequestType = typeof(ProcessingActivityMapperTests),
        CreatedAtUtc = FixedTime,
        LastUpdatedAtUtc = FixedTime.AddHours(1)
    };

    // -- ToEntity --

    [Fact]
    public void ToEntity_ValidActivity_ShouldMapAllProperties()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        var entity = ProcessingActivityMapper.ToEntity(activity);

        // Assert
        entity.Id.Should().Be("11111111-2222-3333-4444-555555555555");
        entity.RequestTypeName.Should().Be(typeof(ProcessingActivityMapperTests).AssemblyQualifiedName);
        entity.Name.Should().Be("Order Processing");
        entity.Purpose.Should().Be("Fulfil customer orders");
        entity.LawfulBasisValue.Should().Be((int)LawfulBasis.Contract);
        entity.ThirdCountryTransfers.Should().Be("US data center");
        entity.Safeguards.Should().Be("Standard contractual clauses");
        entity.RetentionPeriodTicks.Should().Be(TimeSpan.FromDays(730).Ticks);
        entity.SecurityMeasures.Should().Be("Encryption at rest and in transit");
        entity.CreatedAtUtc.Should().Be(FixedTime);
        entity.LastUpdatedAtUtc.Should().Be(FixedTime.AddHours(1));
    }

    [Fact]
    public void ToEntity_JsonFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        var entity = ProcessingActivityMapper.ToEntity(activity);

        // Assert
        entity.CategoriesOfDataSubjectsJson.Should().Contain("Customers");
        entity.CategoriesOfDataSubjectsJson.Should().Contain("Suppliers");
        entity.CategoriesOfPersonalDataJson.Should().Contain("Name");
        entity.CategoriesOfPersonalDataJson.Should().Contain("Email");
        entity.CategoriesOfPersonalDataJson.Should().Contain("Address");
        entity.RecipientsJson.Should().Contain("Shipping Provider");
        entity.RecipientsJson.Should().Contain("Payment Gateway");
    }

    [Fact]
    public void ToEntity_NullOptionalFields_ShouldMapToNull()
    {
        // Arrange
        var activity = CreateActivity() with
        {
            ThirdCountryTransfers = null,
            Safeguards = null
        };

        // Act
        var entity = ProcessingActivityMapper.ToEntity(activity);

        // Assert
        entity.ThirdCountryTransfers.Should().BeNull();
        entity.Safeguards.Should().BeNull();
    }

    [Fact]
    public void ToEntity_NullActivity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ProcessingActivityMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("activity");
    }

    // -- ToDomain --

    [Fact]
    public void ToDomain_ValidEntity_ShouldMapAllProperties()
    {
        // Arrange
        var activity = CreateActivity();
        var entity = ProcessingActivityMapper.ToEntity(activity);

        // Act
        var domain = ProcessingActivityMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.Id.Should().Be(activity.Id);
        domain.RequestType.Should().Be(typeof(ProcessingActivityMapperTests));
        domain.Name.Should().Be("Order Processing");
        domain.Purpose.Should().Be("Fulfil customer orders");
        domain.LawfulBasis.Should().Be(LawfulBasis.Contract);
        domain.ThirdCountryTransfers.Should().Be("US data center");
        domain.Safeguards.Should().Be("Standard contractual clauses");
        domain.RetentionPeriod.Should().Be(TimeSpan.FromDays(730));
        domain.SecurityMeasures.Should().Be("Encryption at rest and in transit");
        domain.CreatedAtUtc.Should().Be(FixedTime);
        domain.LastUpdatedAtUtc.Should().Be(FixedTime.AddHours(1));
    }

    [Fact]
    public void ToDomain_JsonFields_ShouldDeserializeCorrectly()
    {
        // Arrange
        var activity = CreateActivity();
        var entity = ProcessingActivityMapper.ToEntity(activity);

        // Act
        var domain = ProcessingActivityMapper.ToDomain(entity);

        // Assert
        domain.Should().NotBeNull();
        domain!.CategoriesOfDataSubjects.Should().BeEquivalentTo(["Customers", "Suppliers"]);
        domain.CategoriesOfPersonalData.Should().BeEquivalentTo(["Name", "Email", "Address"]);
        domain.Recipients.Should().BeEquivalentTo(["Shipping Provider", "Payment Gateway"]);
    }

    [Fact]
    public void ToDomain_UnresolvableRequestType_ShouldReturnNull()
    {
        // Arrange
        var entity = new ProcessingActivityEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            RequestTypeName = "NonExistent.Type, NonExistent.Assembly",
            Name = "Test",
            Purpose = "Test",
            LawfulBasisValue = (int)LawfulBasis.Contract,
            CategoriesOfDataSubjectsJson = "[]",
            CategoriesOfPersonalDataJson = "[]",
            RecipientsJson = "[]",
            RetentionPeriodTicks = TimeSpan.FromDays(365).Ticks,
            SecurityMeasures = "Test",
            CreatedAtUtc = FixedTime,
            LastUpdatedAtUtc = FixedTime
        };

        // Act
        var domain = ProcessingActivityMapper.ToDomain(entity);

        // Assert
        domain.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ProcessingActivityMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }

    // -- Round-trip --

    [Fact]
    public void RoundTrip_ToEntityThenToDomain_ShouldPreserveAllProperties()
    {
        // Arrange
        var original = CreateActivity();

        // Act
        var entity = ProcessingActivityMapper.ToEntity(original);
        var restored = ProcessingActivityMapper.ToDomain(entity);

        // Assert
        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Name.Should().Be(original.Name);
        restored.Purpose.Should().Be(original.Purpose);
        restored.LawfulBasis.Should().Be(original.LawfulBasis);
        restored.CategoriesOfDataSubjects.Should().BeEquivalentTo(original.CategoriesOfDataSubjects);
        restored.CategoriesOfPersonalData.Should().BeEquivalentTo(original.CategoriesOfPersonalData);
        restored.Recipients.Should().BeEquivalentTo(original.Recipients);
        restored.ThirdCountryTransfers.Should().Be(original.ThirdCountryTransfers);
        restored.Safeguards.Should().Be(original.Safeguards);
        restored.RetentionPeriod.Should().Be(original.RetentionPeriod);
        restored.SecurityMeasures.Should().Be(original.SecurityMeasures);
        restored.RequestType.Should().Be(original.RequestType);
    }

    [Fact]
    public void RoundTrip_EmptyCollections_ShouldPreserve()
    {
        // Arrange
        var original = CreateActivity() with
        {
            CategoriesOfDataSubjects = [],
            CategoriesOfPersonalData = [],
            Recipients = []
        };

        // Act
        var entity = ProcessingActivityMapper.ToEntity(original);
        var restored = ProcessingActivityMapper.ToDomain(entity);

        // Assert
        restored.Should().NotBeNull();
        restored!.CategoriesOfDataSubjects.Should().BeEmpty();
        restored.CategoriesOfPersonalData.Should().BeEmpty();
        restored.Recipients.Should().BeEmpty();
    }

    // -- LawfulBasis enum values --

    [Theory]
    [InlineData(LawfulBasis.Consent)]
    [InlineData(LawfulBasis.Contract)]
    [InlineData(LawfulBasis.LegalObligation)]
    [InlineData(LawfulBasis.VitalInterests)]
    [InlineData(LawfulBasis.PublicTask)]
    [InlineData(LawfulBasis.LegitimateInterests)]
    public void RoundTrip_AllLawfulBasisValues_ShouldPreserve(LawfulBasis basis)
    {
        // Arrange
        var original = CreateActivity() with { LawfulBasis = basis };

        // Act
        var entity = ProcessingActivityMapper.ToEntity(original);
        var restored = ProcessingActivityMapper.ToDomain(entity);

        // Assert
        restored.Should().NotBeNull();
        restored!.LawfulBasis.Should().Be(basis);
    }
}
