using System.Reflection;
using Encina.Compliance.GDPR;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR.Attributes;

/// <summary>
/// Unit tests for <see cref="ProcessingActivityAttribute"/>.
/// </summary>
public class ProcessingActivityAttributeTests
{
    [Fact]
    public void Attribute_ShouldSetRequiredProperties()
    {
        // Arrange & Act
        var attr = new ProcessingActivityAttribute
        {
            Purpose = "Order fulfillment",
            LawfulBasis = LawfulBasis.Contract,
            DataCategories = ["Name", "Email"],
            DataSubjects = ["Customers"],
            RetentionDays = 2555
        };

        // Assert
        attr.Purpose.Should().Be("Order fulfillment");
        attr.LawfulBasis.Should().Be(LawfulBasis.Contract);
        attr.DataCategories.Should().BeEquivalentTo(["Name", "Email"]);
        attr.DataSubjects.Should().BeEquivalentTo(["Customers"]);
        attr.RetentionDays.Should().Be(2555);
    }

    [Fact]
    public void Attribute_ShouldHaveDefaultOptionalProperties()
    {
        // Arrange & Act
        var attr = new ProcessingActivityAttribute
        {
            Purpose = "Test",
            LawfulBasis = LawfulBasis.Consent,
            DataCategories = ["Email"],
            DataSubjects = ["Users"],
            RetentionDays = 30
        };

        // Assert
        attr.Recipients.Should().BeEmpty();
        attr.SecurityMeasures.Should().BeEmpty();
        attr.ThirdCountryTransfers.Should().BeNull();
        attr.Safeguards.Should().BeNull();
    }

    [Fact]
    public void Attribute_ShouldSetOptionalProperties()
    {
        // Arrange & Act
        var attr = new ProcessingActivityAttribute
        {
            Purpose = "Analytics",
            LawfulBasis = LawfulBasis.LegitimateInterests,
            DataCategories = ["IP Address"],
            DataSubjects = ["Visitors"],
            RetentionDays = 90,
            Recipients = ["Analytics Provider"],
            SecurityMeasures = "TLS 1.3",
            ThirdCountryTransfers = "US (AWS)",
            Safeguards = "Standard Contractual Clauses"
        };

        // Assert
        attr.Recipients.Should().BeEquivalentTo(["Analytics Provider"]);
        attr.SecurityMeasures.Should().Be("TLS 1.3");
        attr.ThirdCountryTransfers.Should().Be("US (AWS)");
        attr.Safeguards.Should().Be("Standard Contractual Clauses");
    }

    [Fact]
    public void Attribute_ShouldTargetClassOnly()
    {
        // Arrange
        var usage = typeof(ProcessingActivityAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void Attribute_OnDecoratedType_ShouldBeDiscoverable()
    {
        // Act
        var attr = typeof(SampleDecoratedRequest).GetCustomAttribute<ProcessingActivityAttribute>();

        // Assert
        attr.Should().NotBeNull();
        attr!.Purpose.Should().Be("Test processing");
        attr.LawfulBasis.Should().Be(LawfulBasis.Contract);
    }

    [Theory]
    [InlineData(LawfulBasis.Consent)]
    [InlineData(LawfulBasis.Contract)]
    [InlineData(LawfulBasis.LegalObligation)]
    [InlineData(LawfulBasis.VitalInterests)]
    [InlineData(LawfulBasis.PublicTask)]
    [InlineData(LawfulBasis.LegitimateInterests)]
    public void Attribute_AllLawfulBasisValues_ShouldBeAssignable(LawfulBasis basis)
    {
        // Arrange & Act
        var attr = new ProcessingActivityAttribute
        {
            Purpose = "Test",
            LawfulBasis = basis,
            DataCategories = ["Email"],
            DataSubjects = ["Users"],
            RetentionDays = 30
        };

        // Assert
        attr.LawfulBasis.Should().Be(basis);
    }
}
