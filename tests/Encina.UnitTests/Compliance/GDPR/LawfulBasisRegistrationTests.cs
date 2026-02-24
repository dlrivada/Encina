using Encina.Compliance.GDPR;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

using Encina.UnitTests.Compliance.GDPR.Attributes;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="LawfulBasisRegistration"/>.
/// </summary>
public class LawfulBasisRegistrationTests
{
    // -- FromAttribute --

    [Fact]
    public void FromAttribute_WithLawfulBasisAttribute_CreatesRegistration()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);

        // Act
        var result = LawfulBasisRegistration.FromAttribute(
            typeof(SampleLawfulBasisDecoratedRequest), timeProvider);

        // Assert
        result.Should().NotBeNull();
        result!.RequestType.Should().Be(typeof(SampleLawfulBasisDecoratedRequest));
        result.Basis.Should().Be(LawfulBasis.Consent);
        result.Purpose.Should().Be("Test consent processing");
        result.RegisteredAtUtc.Should().Be(fixedTime);
    }

    [Fact]
    public void FromAttribute_WithAllProperties_MapsCorrectly()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();

        // Act
        var result = LawfulBasisRegistration.FromAttribute(
            typeof(SampleLegitimateInterestsRequest), timeProvider);

        // Assert
        result.Should().NotBeNull();
        result!.Basis.Should().Be(LawfulBasis.LegitimateInterests);
        result.Purpose.Should().Be("Fraud detection");
        result.LIAReference.Should().Be("LIA-2024-FRAUD-001");
    }

    [Fact]
    public void FromAttribute_WithContractBasis_MapsContractReference()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();

        // Act
        var result = LawfulBasisRegistration.FromAttribute(
            typeof(SampleContractRequest), timeProvider);

        // Assert
        result.Should().NotBeNull();
        result!.Basis.Should().Be(LawfulBasis.Contract);
        result.Purpose.Should().Be("Order fulfillment");
        result.ContractReference.Should().Be("Terms of Service v2.1");
    }

    [Fact]
    public void FromAttribute_WithLegalObligation_MapsLegalReference()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();

        // Act
        var result = LawfulBasisRegistration.FromAttribute(
            typeof(SampleLegalObligationRequest), timeProvider);

        // Assert
        result.Should().NotBeNull();
        result!.Basis.Should().Be(LawfulBasis.LegalObligation);
        result.Purpose.Should().Be("Tax reporting");
        result.LegalReference.Should().Be("EU VAT Directive 2006/112/EC");
    }

    [Fact]
    public void FromAttribute_TypeWithoutAttribute_ReturnsNull()
    {
        // Act
        var result = LawfulBasisRegistration.FromAttribute(typeof(SampleNoAttributeRequest));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromAttribute_NullRequestType_ThrowsArgumentNullException()
    {
        // Act
        var act = () => LawfulBasisRegistration.FromAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestType");
    }

    [Fact]
    public void FromAttribute_NullTimeProvider_UsesSystemClock()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var result = LawfulBasisRegistration.FromAttribute(
            typeof(SampleLawfulBasisDecoratedRequest), timeProvider: null);
        var after = DateTimeOffset.UtcNow;

        // Assert
        result.Should().NotBeNull();
        result!.RegisteredAtUtc.Should().BeOnOrAfter(before);
        result.RegisteredAtUtc.Should().BeOnOrBefore(after);
    }

    // -- Record properties --

    [Fact]
    public void Record_WithRequiredProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var registration = new LawfulBasisRegistration
        {
            RequestType = typeof(string),
            Basis = LawfulBasis.Contract,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        // Assert
        registration.RequestType.Should().Be<string>();
        registration.Basis.Should().Be(LawfulBasis.Contract);
        registration.Purpose.Should().BeNull();
        registration.LIAReference.Should().BeNull();
        registration.LegalReference.Should().BeNull();
        registration.ContractReference.Should().BeNull();
    }

    [Fact]
    public void Record_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var registration = new LawfulBasisRegistration
        {
            RequestType = typeof(string),
            Basis = LawfulBasis.LegitimateInterests,
            Purpose = "Test purpose",
            LIAReference = "LIA-001",
            LegalReference = "Legal-001",
            ContractReference = "Contract-001",
            RegisteredAtUtc = now
        };

        // Assert
        registration.Purpose.Should().Be("Test purpose");
        registration.LIAReference.Should().Be("LIA-001");
        registration.LegalReference.Should().Be("Legal-001");
        registration.ContractReference.Should().Be("Contract-001");
        registration.RegisteredAtUtc.Should().Be(now);
    }

    [Fact]
    public void Record_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var r1 = new LawfulBasisRegistration
        {
            RequestType = typeof(string),
            Basis = LawfulBasis.Consent,
            RegisteredAtUtc = now
        };
        var r2 = new LawfulBasisRegistration
        {
            RequestType = typeof(string),
            Basis = LawfulBasis.Consent,
            RegisteredAtUtc = now
        };

        // Assert
        r1.Should().Be(r2);
    }

    [Fact]
    public void Record_WithExpression_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = new LawfulBasisRegistration
        {
            RequestType = typeof(string),
            Basis = LawfulBasis.Contract,
            Purpose = "Original",
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        var modified = original with { Purpose = "Modified" };

        // Assert
        modified.Purpose.Should().Be("Modified");
        modified.RequestType.Should().Be(original.RequestType);
        modified.Basis.Should().Be(original.Basis);
        original.Purpose.Should().Be("Original");
    }
}
