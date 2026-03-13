#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DPAMandatoryTerms"/> — verifying the 8 mandatory Article 28(3) term tracking.
/// </summary>
public sealed class DPAMandatoryTermsTests
{
    #region IsFullyCompliant Tests

    [Fact]
    public void IsFullyCompliant_AllTermsTrue_ReturnsTrue()
    {
        var terms = CreateFullyCompliantTerms();

        terms.IsFullyCompliant.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, true, true, true, true, true, true, true)]
    [InlineData(true, false, true, true, true, true, true, true)]
    [InlineData(true, true, false, true, true, true, true, true)]
    [InlineData(true, true, true, false, true, true, true, true)]
    [InlineData(true, true, true, true, false, true, true, true)]
    [InlineData(true, true, true, true, true, false, true, true)]
    [InlineData(true, true, true, true, true, true, false, true)]
    [InlineData(true, true, true, true, true, true, true, false)]
    public void IsFullyCompliant_OneMissing_ReturnsFalse(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = a,
            ConfidentialityObligations = b,
            SecurityMeasures = c,
            SubProcessorRequirements = d,
            DataSubjectRightsAssistance = e,
            ComplianceAssistance = f,
            DataDeletionOrReturn = g,
            AuditRights = h
        };

        terms.IsFullyCompliant.Should().BeFalse();
    }

    [Fact]
    public void IsFullyCompliant_AllTermsFalse_ReturnsFalse()
    {
        var terms = CreateAllFalseTerms();

        terms.IsFullyCompliant.Should().BeFalse();
    }

    #endregion

    #region MissingTerms Tests

    [Fact]
    public void MissingTerms_AllTermsTrue_ReturnsEmptyList()
    {
        var terms = CreateFullyCompliantTerms();

        terms.MissingTerms.Should().BeEmpty();
    }

    [Fact]
    public void MissingTerms_AllTermsFalse_ReturnsAll8Terms()
    {
        var terms = CreateAllFalseTerms();

        terms.MissingTerms.Should().HaveCount(8);
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.ProcessOnDocumentedInstructions));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.ConfidentialityObligations));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.SecurityMeasures));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.SubProcessorRequirements));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.DataSubjectRightsAssistance));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.ComplianceAssistance));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.DataDeletionOrReturn));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.AuditRights));
    }

    [Fact]
    public void MissingTerms_SingleMissing_ReturnsOnlyThatTerm()
    {
        var terms = CreateFullyCompliantTerms() with { AuditRights = false };

        terms.MissingTerms.Should().HaveCount(1);
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.AuditRights));
    }

    [Fact]
    public void MissingTerms_MultipleMissing_ReturnsAllMissing()
    {
        var terms = CreateFullyCompliantTerms() with
        {
            ProcessOnDocumentedInstructions = false,
            SecurityMeasures = false,
            AuditRights = false
        };

        terms.MissingTerms.Should().HaveCount(3);
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.ProcessOnDocumentedInstructions));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.SecurityMeasures));
        terms.MissingTerms.Should().Contain(nameof(DPAMandatoryTerms.AuditRights));
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var terms1 = CreateFullyCompliantTerms();
        var terms2 = CreateFullyCompliantTerms();

        terms1.Should().Be(terms2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var terms1 = CreateFullyCompliantTerms();
        var terms2 = CreateFullyCompliantTerms() with { AuditRights = false };

        terms1.Should().NotBe(terms2);
    }

    [Fact]
    public void WithExpression_CreatesNewInstance()
    {
        var original = CreateFullyCompliantTerms();
        var modified = original with { AuditRights = false };

        original.AuditRights.Should().BeTrue();
        modified.AuditRights.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private static DPAMandatoryTerms CreateFullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    private static DPAMandatoryTerms CreateAllFalseTerms() => new()
    {
        ProcessOnDocumentedInstructions = false,
        ConfidentialityObligations = false,
        SecurityMeasures = false,
        SubProcessorRequirements = false,
        DataSubjectRightsAssistance = false,
        ComplianceAssistance = false,
        DataDeletionOrReturn = false,
        AuditRights = false
    };

    #endregion
}
