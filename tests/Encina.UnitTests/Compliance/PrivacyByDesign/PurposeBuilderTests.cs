using Encina.Compliance.PrivacyByDesign;

using Shouldly;

using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PurposeBuilder"/>.
/// </summary>
public class PurposeBuilderTests
{
    [Fact]
    public void Constructor_ShouldSetName()
    {
        // Act
        var builder = new PurposeBuilder("Order Processing");

        // Assert
        builder.Name.ShouldBe("Order Processing");
    }

    [Fact]
    public void Description_ShouldDefaultToEmpty()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.Description.ShouldBeEmpty();
    }

    [Fact]
    public void LegalBasis_ShouldDefaultToEmpty()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.LegalBasis.ShouldBeEmpty();
    }

    [Fact]
    public void AllowedFields_ShouldDefaultToEmpty()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.AllowedFields.ShouldBeEmpty();
    }

    [Fact]
    public void ModuleId_ShouldDefaultToNull()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void ExpiresAtUtc_ShouldDefaultToNull()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.ExpiresAtUtc.ShouldBeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddYears(2);

        // Act
        var builder = new PurposeBuilder("Order Processing")
        {
            Description = "Processing personal data for order fulfillment.",
            LegalBasis = "Contract",
            ModuleId = "orders",
            ExpiresAtUtc = expiresAt
        };
        builder.AllowedFields.AddRange(["ProductId", "Quantity", "ShippingAddress"]);

        // Assert
        builder.Name.ShouldBe("Order Processing");
        builder.Description.ShouldBe("Processing personal data for order fulfillment.");
        builder.LegalBasis.ShouldBe("Contract");
        builder.ModuleId.ShouldBe("orders");
        builder.ExpiresAtUtc.ShouldBe(expiresAt);
        builder.AllowedFields.ShouldBe(["ProductId", "Quantity", "ShippingAddress"]);
    }

    [Fact]
    public void Build_ShouldCreatePurposeDefinitionWithCorrectValues()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
        var expiresAt = new DateTimeOffset(2028, 3, 14, 12, 0, 0, TimeSpan.Zero);

        var builder = new PurposeBuilder("Order Processing")
        {
            Description = "Processing personal data for order fulfillment.",
            LegalBasis = "Contract",
            ModuleId = "orders",
            ExpiresAtUtc = expiresAt
        };
        builder.AllowedFields.AddRange(["ProductId", "Quantity"]);

        // Act
        var definition = builder.Build(fakeTime);

        // Assert
        definition.Name.ShouldBe("Order Processing");
        definition.Description.ShouldBe("Processing personal data for order fulfillment.");
        definition.LegalBasis.ShouldBe("Contract");
        definition.ModuleId.ShouldBe("orders");
        definition.ExpiresAtUtc.ShouldBe(expiresAt);
        definition.AllowedFields.ShouldBe(["ProductId", "Quantity"]);
        definition.CreatedAtUtc.ShouldBe(fakeTime.GetUtcNow());
    }

    [Fact]
    public void Build_ShouldGenerateNonEmptyPurposeId()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var builder = new PurposeBuilder("Test")
        {
            Description = "Test description",
            LegalBasis = "Consent"
        };

        // Act
        var definition = builder.Build(fakeTime);

        // Assert
        definition.PurposeId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Build_ShouldGenerateUniquePurposeIds()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var builder = new PurposeBuilder("Test")
        {
            Description = "Test description",
            LegalBasis = "Consent"
        };

        // Act
        var definition1 = builder.Build(fakeTime);
        var definition2 = builder.Build(fakeTime);

        // Assert
        definition1.PurposeId.ShouldNotBe(definition2.PurposeId);
    }

    [Fact]
    public void Build_WithoutModuleId_ShouldHaveNullModuleId()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var builder = new PurposeBuilder("Global Purpose")
        {
            Description = "A global purpose",
            LegalBasis = "Contract"
        };

        // Act
        var definition = builder.Build(fakeTime);

        // Assert
        definition.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new PurposeBuilder(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .And.ParamName.ShouldBe("name");
    }
}
