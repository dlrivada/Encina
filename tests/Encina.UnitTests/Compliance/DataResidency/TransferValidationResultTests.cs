using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataResidency;

public class TransferValidationResultTests
{
    [Fact]
    public void Allow_WithLegalBasis_ShouldSetIsAllowedTrue()
    {
        // Act
        var result = TransferValidationResult.Allow(TransferLegalBasis.AdequacyDecision);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.LegalBasis.Should().Be(TransferLegalBasis.AdequacyDecision);
        result.DenialReason.Should().BeNull();
        result.RequiredSafeguards.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Allow_WithSafeguardsAndWarnings_ShouldPreserveAll()
    {
        // Arrange
        var safeguards = new List<string> { "SCCs must be in place", "TIA recommended" };
        var warnings = new List<string> { "No adequacy decision" };

        // Act
        var result = TransferValidationResult.Allow(
            TransferLegalBasis.StandardContractualClauses,
            requiredSafeguards: safeguards,
            warnings: warnings);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.LegalBasis.Should().Be(TransferLegalBasis.StandardContractualClauses);
        result.RequiredSafeguards.Should().HaveCount(2);
        result.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public void Deny_ShouldSetIsAllowedFalse()
    {
        // Act
        var result = TransferValidationResult.Deny("No adequacy decision and no safeguards");

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.DenialReason.Should().Be("No adequacy decision and no safeguards");
        result.LegalBasis.Should().BeNull();
        result.RequiredSafeguards.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(TransferLegalBasis.AdequacyDecision)]
    [InlineData(TransferLegalBasis.StandardContractualClauses)]
    [InlineData(TransferLegalBasis.BindingCorporateRules)]
    [InlineData(TransferLegalBasis.ExplicitConsent)]
    [InlineData(TransferLegalBasis.PublicInterest)]
    [InlineData(TransferLegalBasis.LegalClaims)]
    [InlineData(TransferLegalBasis.VitalInterests)]
    [InlineData(TransferLegalBasis.Derogation)]
    public void Allow_WithAnyLegalBasis_ShouldPreserveBasis(TransferLegalBasis basis)
    {
        // Act
        var result = TransferValidationResult.Allow(basis);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.LegalBasis.Should().Be(basis);
    }

    [Fact]
    public void Allow_WithNullSafeguards_ShouldDefaultToEmpty()
    {
        // Act
        var result = TransferValidationResult.Allow(
            TransferLegalBasis.AdequacyDecision,
            requiredSafeguards: null);

        // Assert
        result.RequiredSafeguards.Should().BeEmpty();
    }

    [Fact]
    public void Allow_WithNullWarnings_ShouldDefaultToEmpty()
    {
        // Act
        var result = TransferValidationResult.Allow(
            TransferLegalBasis.AdequacyDecision,
            warnings: null);

        // Assert
        result.Warnings.Should().BeEmpty();
    }
}
