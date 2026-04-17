using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class TransferValidationResultTests
{
    [Fact]
    public void Allow_WithLegalBasis_ShouldSetIsAllowedTrue()
    {
        // Act
        var result = TransferValidationResult.Allow(TransferLegalBasis.AdequacyDecision);

        // Assert
        result.IsAllowed.ShouldBeTrue();
        result.LegalBasis.ShouldBe(TransferLegalBasis.AdequacyDecision);
        result.DenialReason.ShouldBeNull();
        result.RequiredSafeguards.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
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
        result.IsAllowed.ShouldBeTrue();
        result.LegalBasis.ShouldBe(TransferLegalBasis.StandardContractualClauses);
        result.RequiredSafeguards.Count.ShouldBe(2);
        result.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void Deny_ShouldSetIsAllowedFalse()
    {
        // Act
        var result = TransferValidationResult.Deny("No adequacy decision and no safeguards");

        // Assert
        result.IsAllowed.ShouldBeFalse();
        result.DenialReason.ShouldBe("No adequacy decision and no safeguards");
        result.LegalBasis.ShouldBeNull();
        result.RequiredSafeguards.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
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
        result.IsAllowed.ShouldBeTrue();
        result.LegalBasis.ShouldBe(basis);
    }

    [Fact]
    public void Allow_WithNullSafeguards_ShouldDefaultToEmpty()
    {
        // Act
        var result = TransferValidationResult.Allow(
            TransferLegalBasis.AdequacyDecision,
            requiredSafeguards: null);

        // Assert
        result.RequiredSafeguards.ShouldBeEmpty();
    }

    [Fact]
    public void Allow_WithNullWarnings_ShouldDefaultToEmpty()
    {
        // Act
        var result = TransferValidationResult.Allow(
            TransferLegalBasis.AdequacyDecision,
            warnings: null);

        // Assert
        result.Warnings.ShouldBeEmpty();
    }
}
