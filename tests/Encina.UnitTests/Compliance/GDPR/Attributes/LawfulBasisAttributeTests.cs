using System.Reflection;
using Encina.Compliance.GDPR;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR.Attributes;

/// <summary>
/// Unit tests for <see cref="LawfulBasisAttribute"/>.
/// </summary>
public class LawfulBasisAttributeTests
{
    [Fact]
    public void Constructor_WithRequiredBasis_SetsProperty()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.Consent);

        // Assert
        attr.Basis.Should().Be(LawfulBasis.Consent);
    }

    [Theory]
    [InlineData(LawfulBasis.Consent)]
    [InlineData(LawfulBasis.Contract)]
    [InlineData(LawfulBasis.LegalObligation)]
    [InlineData(LawfulBasis.VitalInterests)]
    [InlineData(LawfulBasis.PublicTask)]
    [InlineData(LawfulBasis.LegitimateInterests)]
    public void Constructor_AllLawfulBasisValues_ShouldBeAssignable(LawfulBasis basis)
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(basis);

        // Assert
        attr.Basis.Should().Be(basis);
    }

    [Fact]
    public void AttributeUsage_TargetsClass_InheritedTrue_AllowMultipleFalse()
    {
        // Arrange
        var usage = typeof(LawfulBasisAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Class);
        usage.Inherited.Should().BeTrue();
        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void OptionalProperties_DefaultToNull()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.Contract);

        // Assert
        attr.Purpose.Should().BeNull();
        attr.LIAReference.Should().BeNull();
        attr.LegalReference.Should().BeNull();
        attr.ContractReference.Should().BeNull();
    }

    [Fact]
    public void Purpose_CanBeSet()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.Consent) { Purpose = "Marketing newsletters" };

        // Assert
        attr.Purpose.Should().Be("Marketing newsletters");
    }

    [Fact]
    public void LIAReference_CanBeSet()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.LegitimateInterests)
        {
            LIAReference = "LIA-2024-FRAUD-001"
        };

        // Assert
        attr.LIAReference.Should().Be("LIA-2024-FRAUD-001");
    }

    [Fact]
    public void LegalReference_CanBeSet()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.LegalObligation)
        {
            LegalReference = "EU VAT Directive 2006/112/EC"
        };

        // Assert
        attr.LegalReference.Should().Be("EU VAT Directive 2006/112/EC");
    }

    [Fact]
    public void ContractReference_CanBeSet()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.Contract)
        {
            ContractReference = "Terms of Service v2.1"
        };

        // Assert
        attr.ContractReference.Should().Be("Terms of Service v2.1");
    }

    [Fact]
    public void AllProperties_SetSimultaneously_ShouldWork()
    {
        // Arrange & Act
        var attr = new LawfulBasisAttribute(LawfulBasis.LegitimateInterests)
        {
            Purpose = "Fraud detection",
            LIAReference = "LIA-2024-FRAUD-001",
            LegalReference = "Some legal ref",
            ContractReference = "Some contract ref"
        };

        // Assert
        attr.Basis.Should().Be(LawfulBasis.LegitimateInterests);
        attr.Purpose.Should().Be("Fraud detection");
        attr.LIAReference.Should().Be("LIA-2024-FRAUD-001");
        attr.LegalReference.Should().Be("Some legal ref");
        attr.ContractReference.Should().Be("Some contract ref");
    }

    [Fact]
    public void Attribute_OnDecoratedType_ShouldBeDiscoverable()
    {
        // Act
        var attr = typeof(SampleLawfulBasisDecoratedRequest)
            .GetCustomAttribute<LawfulBasisAttribute>();

        // Assert
        attr.Should().NotBeNull();
        attr!.Basis.Should().Be(LawfulBasis.Consent);
        attr.Purpose.Should().Be("Test consent processing");
    }

    [Fact]
    public void Attribute_OnTypeWithAllProperties_ShouldBeDiscoverable()
    {
        // Act
        var attr = typeof(SampleLegitimateInterestsRequest)
            .GetCustomAttribute<LawfulBasisAttribute>();

        // Assert
        attr.Should().NotBeNull();
        attr!.Basis.Should().Be(LawfulBasis.LegitimateInterests);
        attr.Purpose.Should().Be("Fraud detection");
        attr.LIAReference.Should().Be("LIA-2024-FRAUD-001");
    }

    [Fact]
    public void Attribute_OnInheritedType_ShouldBeInherited()
    {
        // Act — DerivedLawfulBasisRequest inherits from BaseLawfulBasisRequest
        var attr = typeof(DerivedLawfulBasisRequest)
            .GetCustomAttribute<LawfulBasisAttribute>(inherit: true);

        // Assert
        attr.Should().NotBeNull();
        attr!.Basis.Should().Be(LawfulBasis.VitalInterests);
    }

    [Fact]
    public void Attribute_OnInheritedType_WithoutInheritFlag_ShouldNotBeFound()
    {
        // Act — without inherit: true, base attributes are not found on derived classes
        var attr = typeof(DerivedLawfulBasisRequest)
            .GetCustomAttribute<LawfulBasisAttribute>(inherit: false);

        // Assert — DerivedLawfulBasisRequest does not have its own attribute
        attr.Should().BeNull();
    }
}

// Test stub types for LawfulBasis attribute tests

[LawfulBasis(LawfulBasis.Consent, Purpose = "Test consent processing")]
public sealed record SampleLawfulBasisDecoratedRequest : ICommand<LanguageExt.Unit>;

[LawfulBasis(LawfulBasis.LegitimateInterests,
    Purpose = "Fraud detection",
    LIAReference = "LIA-2024-FRAUD-001")]
public sealed record SampleLegitimateInterestsRequest : ICommand<LanguageExt.Unit>;

[LawfulBasis(LawfulBasis.Contract,
    Purpose = "Order fulfillment",
    ContractReference = "Terms of Service v2.1")]
public sealed record SampleContractRequest : ICommand<LanguageExt.Unit>;

[LawfulBasis(LawfulBasis.LegalObligation,
    Purpose = "Tax reporting",
    LegalReference = "EU VAT Directive 2006/112/EC")]
public sealed record SampleLegalObligationRequest : ICommand<LanguageExt.Unit>;

// Non-sealed base for inheritance tests
[LawfulBasis(LawfulBasis.VitalInterests, Purpose = "Inherited basis")]
public record BaseLawfulBasisRequest : ICommand<LanguageExt.Unit>;

public sealed record DerivedLawfulBasisRequest : BaseLawfulBasisRequest;
