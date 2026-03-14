using Encina.Compliance.PrivacyByDesign;

using FluentAssertions;

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
        builder.Name.Should().Be("Order Processing");
    }

    [Fact]
    public void Description_ShouldDefaultToEmpty()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.Description.Should().BeEmpty();
    }

    [Fact]
    public void LegalBasis_ShouldDefaultToEmpty()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.LegalBasis.Should().BeEmpty();
    }

    [Fact]
    public void AllowedFields_ShouldDefaultToEmpty()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.AllowedFields.Should().BeEmpty();
    }

    [Fact]
    public void ModuleId_ShouldDefaultToNull()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.ModuleId.Should().BeNull();
    }

    [Fact]
    public void ExpiresAtUtc_ShouldDefaultToNull()
    {
        // Act
        var builder = new PurposeBuilder("Test");

        // Assert
        builder.ExpiresAtUtc.Should().BeNull();
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
        builder.Name.Should().Be("Order Processing");
        builder.Description.Should().Be("Processing personal data for order fulfillment.");
        builder.LegalBasis.Should().Be("Contract");
        builder.ModuleId.Should().Be("orders");
        builder.ExpiresAtUtc.Should().Be(expiresAt);
        builder.AllowedFields.Should().BeEquivalentTo(["ProductId", "Quantity", "ShippingAddress"]);
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
        definition.Name.Should().Be("Order Processing");
        definition.Description.Should().Be("Processing personal data for order fulfillment.");
        definition.LegalBasis.Should().Be("Contract");
        definition.ModuleId.Should().Be("orders");
        definition.ExpiresAtUtc.Should().Be(expiresAt);
        definition.AllowedFields.Should().BeEquivalentTo(["ProductId", "Quantity"]);
        definition.CreatedAtUtc.Should().Be(fakeTime.GetUtcNow());
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
        definition.PurposeId.Should().NotBeNullOrEmpty();
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
        definition1.PurposeId.Should().NotBe(definition2.PurposeId);
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
        definition.ModuleId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new PurposeBuilder(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("name");
    }
}
