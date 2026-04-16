#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Model;

using Shouldly;

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

        terms.IsFullyCompliant.ShouldBeTrue();
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

        terms.IsFullyCompliant.ShouldBeFalse();
    }

    [Fact]
    public void IsFullyCompliant_AllTermsFalse_ReturnsFalse()
    {
        var terms = CreateAllFalseTerms();

        terms.IsFullyCompliant.ShouldBeFalse();
    }

    #endregion

    #region MissingTerms Tests

    [Fact]
    public void MissingTerms_AllTermsTrue_ReturnsEmptyList()
    {
        var terms = CreateFullyCompliantTerms();

        terms.MissingTerms.ShouldBeEmpty();
    }

    [Fact]
    public void MissingTerms_AllTermsFalse_ReturnsAll8Terms()
    {
        var terms = CreateAllFalseTerms();

        terms.MissingTerms.Count.ShouldBe(8);
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.ProcessOnDocumentedInstructions));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.ConfidentialityObligations));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.SecurityMeasures));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.SubProcessorRequirements));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.DataSubjectRightsAssistance));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.ComplianceAssistance));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.DataDeletionOrReturn));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.AuditRights));
    }

    [Fact]
    public void MissingTerms_SingleMissing_ReturnsOnlyThatTerm()
    {
        var terms = CreateFullyCompliantTerms() with { AuditRights = false };

        terms.MissingTerms.Count.ShouldBe(1);
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.AuditRights));
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

        terms.MissingTerms.Count.ShouldBe(3);
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.ProcessOnDocumentedInstructions));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.SecurityMeasures));
        terms.MissingTerms.ShouldContain(nameof(DPAMandatoryTerms.AuditRights));
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var terms1 = CreateFullyCompliantTerms();
        var terms2 = CreateFullyCompliantTerms();

        terms1.ShouldBe(terms2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var terms1 = CreateFullyCompliantTerms();
        var terms2 = CreateFullyCompliantTerms() with { AuditRights = false };

        terms1.ShouldNotBe(terms2);
    }

    [Fact]
    public void WithExpression_CreatesNewInstance()
    {
        var original = CreateFullyCompliantTerms();
        var modified = original with { AuditRights = false };

        original.AuditRights.ShouldBeTrue();
        modified.AuditRights.ShouldBeFalse();
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
