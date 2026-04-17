using Encina.Compliance.GDPR;
using Shouldly;
using LawfulBasis = Encina.Compliance.GDPR.LawfulBasis;

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
        entity.Id.ShouldBe("11111111-2222-3333-4444-555555555555");
        entity.RequestTypeName.ShouldBe(typeof(ProcessingActivityMapperTests).AssemblyQualifiedName);
        entity.Name.ShouldBe("Order Processing");
        entity.Purpose.ShouldBe("Fulfil customer orders");
        entity.LawfulBasisValue.ShouldBe((int)LawfulBasis.Contract);
        entity.ThirdCountryTransfers.ShouldBe("US data center");
        entity.Safeguards.ShouldBe("Standard contractual clauses");
        entity.RetentionPeriodTicks.ShouldBe(TimeSpan.FromDays(730).Ticks);
        entity.SecurityMeasures.ShouldBe("Encryption at rest and in transit");
        entity.CreatedAtUtc.ShouldBe(FixedTime);
        entity.LastUpdatedAtUtc.ShouldBe(FixedTime.AddHours(1));
    }

    [Fact]
    public void ToEntity_JsonFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var activity = CreateActivity();

        // Act
        var entity = ProcessingActivityMapper.ToEntity(activity);

        // Assert
        entity.CategoriesOfDataSubjectsJson.ShouldContain("Customers");
        entity.CategoriesOfDataSubjectsJson.ShouldContain("Suppliers");
        entity.CategoriesOfPersonalDataJson.ShouldContain("Name");
        entity.CategoriesOfPersonalDataJson.ShouldContain("Email");
        entity.CategoriesOfPersonalDataJson.ShouldContain("Address");
        entity.RecipientsJson.ShouldContain("Shipping Provider");
        entity.RecipientsJson.ShouldContain("Payment Gateway");
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
        entity.ThirdCountryTransfers.ShouldBeNull();
        entity.Safeguards.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_NullActivity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ProcessingActivityMapper.ToEntity(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("activity");
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
        domain.ShouldNotBeNull();
        domain!.Id.ShouldBe(activity.Id);
        domain.RequestType.ShouldBe(typeof(ProcessingActivityMapperTests));
        domain.Name.ShouldBe("Order Processing");
        domain.Purpose.ShouldBe("Fulfil customer orders");
        domain.LawfulBasis.ShouldBe(LawfulBasis.Contract);
        domain.ThirdCountryTransfers.ShouldBe("US data center");
        domain.Safeguards.ShouldBe("Standard contractual clauses");
        domain.RetentionPeriod.ShouldBe(TimeSpan.FromDays(730));
        domain.SecurityMeasures.ShouldBe("Encryption at rest and in transit");
        domain.CreatedAtUtc.ShouldBe(FixedTime);
        domain.LastUpdatedAtUtc.ShouldBe(FixedTime.AddHours(1));
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
        domain.ShouldNotBeNull();
        domain!.CategoriesOfDataSubjects.ShouldBe(["Customers", "Suppliers"]);
        domain.CategoriesOfPersonalData.ShouldBe(["Name", "Email", "Address"]);
        domain.Recipients.ShouldBe(["Shipping Provider", "Payment Gateway"]);
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
        domain.ShouldBeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ProcessingActivityMapper.ToDomain(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("entity");
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
        restored.ShouldNotBeNull();
        restored!.Id.ShouldBe(original.Id);
        restored.Name.ShouldBe(original.Name);
        restored.Purpose.ShouldBe(original.Purpose);
        restored.LawfulBasis.ShouldBe(original.LawfulBasis);
        restored.CategoriesOfDataSubjects.ShouldBe(original.CategoriesOfDataSubjects);
        restored.CategoriesOfPersonalData.ShouldBe(original.CategoriesOfPersonalData);
        restored.Recipients.ShouldBe(original.Recipients);
        restored.ThirdCountryTransfers.ShouldBe(original.ThirdCountryTransfers);
        restored.Safeguards.ShouldBe(original.Safeguards);
        restored.RetentionPeriod.ShouldBe(original.RetentionPeriod);
        restored.SecurityMeasures.ShouldBe(original.SecurityMeasures);
        restored.RequestType.ShouldBe(original.RequestType);
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
        restored.ShouldNotBeNull();
        restored!.CategoriesOfDataSubjects.ShouldBeEmpty();
        restored.CategoriesOfPersonalData.ShouldBeEmpty();
        restored.Recipients.ShouldBeEmpty();
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
        restored.ShouldNotBeNull();
        restored!.LawfulBasis.ShouldBe(basis);
    }
}
