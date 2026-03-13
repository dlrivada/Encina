using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="DPAMandatoryTerms"/> verifying GDPR Article 28(3)
/// compliance invariants using FsCheck random data generation.
/// </summary>
public class DPAMandatoryTermsPropertyTests
{
    #region IsFullyCompliant Invariants

    /// <summary>
    /// Invariant: IsFullyCompliant is true when all eight mandatory terms are true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsFullyCompliant_AllTrue_AlwaysTrue()
    {
        var terms = CreateTerms(true, true, true, true, true, true, true, true);
        return terms.IsFullyCompliant;
    }

    /// <summary>
    /// Invariant: IsFullyCompliant is true if and only if all eight bools are true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsFullyCompliant_TrueIffAllTrue(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms = CreateTerms(a, b, c, d, e, f, g, h);
        var allTrue = a && b && c && d && e && f && g && h;
        return terms.IsFullyCompliant == allTrue;
    }

    #endregion

    #region MissingTerms Invariants

    /// <summary>
    /// Invariant: MissingTerms count equals the number of false properties.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MissingTerms_CountEqualsNumberOfFalse(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms = CreateTerms(a, b, c, d, e, f, g, h);
        var falseCount = new[] { a, b, c, d, e, f, g, h }.Count(x => !x);
        return terms.MissingTerms.Count == falseCount;
    }

    /// <summary>
    /// Invariant: MissingTerms is empty if and only if IsFullyCompliant is true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MissingTerms_EmptyIffFullyCompliant(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms = CreateTerms(a, b, c, d, e, f, g, h);
        return (terms.MissingTerms.Count == 0) == terms.IsFullyCompliant;
    }

    /// <summary>
    /// Invariant: MissingTerms contains only valid property names from the record.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool MissingTerms_OnlyContainsValidPropertyNames(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var validNames = new System.Collections.Generic.HashSet<string>
        {
            nameof(DPAMandatoryTerms.ProcessOnDocumentedInstructions),
            nameof(DPAMandatoryTerms.ConfidentialityObligations),
            nameof(DPAMandatoryTerms.SecurityMeasures),
            nameof(DPAMandatoryTerms.SubProcessorRequirements),
            nameof(DPAMandatoryTerms.DataSubjectRightsAssistance),
            nameof(DPAMandatoryTerms.ComplianceAssistance),
            nameof(DPAMandatoryTerms.DataDeletionOrReturn),
            nameof(DPAMandatoryTerms.AuditRights)
        };

        var terms = CreateTerms(a, b, c, d, e, f, g, h);
        return terms.MissingTerms.All(name => validNames.Contains(name));
    }

    #endregion

    #region Record Equality Invariants

    /// <summary>
    /// Invariant: Two instances with the same values are equal (record semantics).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RecordEquality_SameValues_AreEqual(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms1 = CreateTerms(a, b, c, d, e, f, g, h);
        var terms2 = CreateTerms(a, b, c, d, e, f, g, h);
        return terms1 == terms2 && terms1.Equals(terms2);
    }

    /// <summary>
    /// Invariant: GetHashCode is consistent with equality.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RecordEquality_EqualInstances_SameHashCode(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h)
    {
        var terms1 = CreateTerms(a, b, c, d, e, f, g, h);
        var terms2 = CreateTerms(a, b, c, d, e, f, g, h);
        return terms1.GetHashCode() == terms2.GetHashCode();
    }

    #endregion

    #region Helpers

    private static DPAMandatoryTerms CreateTerms(
        bool a, bool b, bool c, bool d, bool e, bool f, bool g, bool h) => new()
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

    #endregion
}
