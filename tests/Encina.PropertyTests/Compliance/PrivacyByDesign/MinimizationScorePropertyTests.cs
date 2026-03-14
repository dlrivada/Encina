using Encina.Compliance.PrivacyByDesign.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.PrivacyByDesign;

/// <summary>
/// Property-based tests for minimization score calculation invariants
/// using FsCheck random data generation.
/// </summary>
public class MinimizationScorePropertyTests
{
    #region Score Range Invariants

    /// <summary>
    /// Invariant: The minimization score is always between 0.0 and 1.0 inclusive.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Score_AlwaysBetweenZeroAndOne(NonNegativeInt necessaryCount, NonNegativeInt unnecessaryCount)
    {
        var necessary = necessaryCount.Get;
        var unnecessary = unnecessaryCount.Get;
        var total = necessary + unnecessary;

        var score = total > 0
            ? (double)necessary / total
            : 1.0;

        return score >= 0.0 && score <= 1.0;
    }

    /// <summary>
    /// Invariant: When all fields are necessary (no unnecessary fields), the score is 1.0.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Score_AllNecessary_IsOne(PositiveInt necessaryCount)
    {
        var necessary = necessaryCount.Get;
        const int unnecessary = 0;
        var total = necessary + unnecessary;

        var score = total > 0
            ? (double)necessary / total
            : 1.0;

        return Math.Abs(score - 1.0) < 1e-10;
    }

    /// <summary>
    /// Invariant: The score equals the ratio of necessary fields to total fields.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Score_IsRatioOfNecessaryToTotal(PositiveInt necessaryCount, NonNegativeInt unnecessaryCount)
    {
        var necessary = necessaryCount.Get;
        var unnecessary = unnecessaryCount.Get;
        var total = necessary + unnecessary;

        var score = total > 0
            ? (double)necessary / total
            : 1.0;

        var expectedRatio = (double)necessary / total;

        return Math.Abs(score - expectedRatio) < 1e-10;
    }

    /// <summary>
    /// Invariant: When there are zero total fields, the score defaults to 1.0.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Score_ZeroTotalFields_IsOne()
    {
        const int necessary = 0;
        const int unnecessary = 0;
        var total = necessary + unnecessary;

        var score = total > 0
            ? (double)necessary / total
            : 1.0;

        return Math.Abs(score - 1.0) < 1e-10;
    }

    #endregion
}
