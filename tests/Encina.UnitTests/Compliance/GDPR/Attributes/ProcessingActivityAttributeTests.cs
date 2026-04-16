using System.Reflection;
using Encina.Compliance.GDPR;
using Shouldly;
using LawfulBasis = Encina.Compliance.GDPR.LawfulBasis;

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
        attr.Purpose.ShouldBe("Order fulfillment");
        attr.LawfulBasis.ShouldBe(LawfulBasis.Contract);
        attr.DataCategories.ShouldBe(["Name", "Email"]);
        attr.DataSubjects.ShouldBe(["Customers"]);
        attr.RetentionDays.ShouldBe(2555);
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
        attr.Recipients.ShouldBeEmpty();
        attr.SecurityMeasures.ShouldBeEmpty();
        attr.ThirdCountryTransfers.ShouldBeNull();
        attr.Safeguards.ShouldBeNull();
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
        attr.Recipients.ShouldBe(["Analytics Provider"]);
        attr.SecurityMeasures.ShouldBe("TLS 1.3");
        attr.ThirdCountryTransfers.ShouldBe("US (AWS)");
        attr.Safeguards.ShouldBe("Standard Contractual Clauses");
    }

    [Fact]
    public void Attribute_ShouldTargetClassOnly()
    {
        // Arrange
        var usage = typeof(ProcessingActivityAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.ShouldNotBeNull();
        usage!.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_OnDecoratedType_ShouldBeDiscoverable()
    {
        // Act
        var attr = typeof(SampleDecoratedRequest).GetCustomAttribute<ProcessingActivityAttribute>();

        // Assert
        attr.ShouldNotBeNull();
        attr!.Purpose.ShouldBe("Test processing");
        attr.LawfulBasis.ShouldBe(LawfulBasis.Contract);
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
        attr.LawfulBasis.ShouldBe(basis);
    }
}
