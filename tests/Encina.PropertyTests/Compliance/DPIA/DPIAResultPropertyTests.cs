using Encina.Compliance.DPIA.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.DPIA;

/// <summary>
/// Property-based tests for <see cref="DPIAResult"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class DPIAResultPropertyTests
{
    #region IsAcceptable Invariants

    /// <summary>
    /// Invariant: A result with Low risk is always acceptable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsAcceptable_LowRisk_AlwaysTrue()
    {
        var result = CreateResult(RiskLevel.Low);
        return result.IsAcceptable;
    }

    /// <summary>
    /// Invariant: A result with Medium risk is always acceptable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsAcceptable_MediumRisk_AlwaysTrue()
    {
        var result = CreateResult(RiskLevel.Medium);
        return result.IsAcceptable;
    }

    /// <summary>
    /// Invariant: A result with High risk is never acceptable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsAcceptable_HighRisk_AlwaysFalse()
    {
        var result = CreateResult(RiskLevel.High);
        return !result.IsAcceptable;
    }

    /// <summary>
    /// Invariant: A result with VeryHigh risk is never acceptable.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool IsAcceptable_VeryHighRisk_AlwaysFalse()
    {
        var result = CreateResult(RiskLevel.VeryHigh);
        return !result.IsAcceptable;
    }

    /// <summary>
    /// Invariant: IsAcceptable is equivalent to OverallRisk &lt;= Medium for any risk level.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool IsAcceptable_EquivalentToRiskLessThanOrEqualMedium()
    {
        var riskLevels = Enum.GetValues<RiskLevel>();
        return riskLevels.All(level =>
        {
            var result = CreateResult(level);
            return result.IsAcceptable == (level <= RiskLevel.Medium);
        });
    }

    #endregion

    #region RiskItem Invariants

    /// <summary>
    /// Invariant: RiskItem is a value type — two items with same values are equal.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RiskItem_ValueEquality_SameValuesAreEqual(NonEmptyString category, NonEmptyString description)
    {
        var item1 = new RiskItem(category.Get, RiskLevel.High, description.Get, null);
        var item2 = new RiskItem(category.Get, RiskLevel.High, description.Get, null);

        return item1 == item2;
    }

    /// <summary>
    /// Invariant: RiskItem with different levels are never equal.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RiskItem_DifferentLevels_NeverEqual(NonEmptyString category, NonEmptyString description)
    {
        var item1 = new RiskItem(category.Get, RiskLevel.Low, description.Get, null);
        var item2 = new RiskItem(category.Get, RiskLevel.High, description.Get, null);

        return item1 != item2;
    }

    #endregion

    #region Mitigation Invariants

    /// <summary>
    /// Invariant: Mitigation value equality holds for identical values.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Mitigation_ValueEquality(NonEmptyString desc, NonEmptyString cat)
    {
        var now = DateTimeOffset.UtcNow;
        var m1 = new Mitigation(desc.Get, cat.Get, true, now);
        var m2 = new Mitigation(desc.Get, cat.Get, true, now);

        return m1 == m2;
    }

    #endregion

    #region Helpers

    private static DPIAResult CreateResult(RiskLevel level) => new()
    {
        OverallRisk = level,
        IdentifiedRisks = [],
        ProposedMitigations = [],
        RequiresPriorConsultation = level >= RiskLevel.VeryHigh,
        AssessedAtUtc = DateTimeOffset.UtcNow
    };

    #endregion
}
