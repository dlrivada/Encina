using System.Reflection;
using Encina.Sharding.Resharding;
using Shouldly;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingErrorCodes"/>.
/// Verifies constant values, uniqueness, naming convention, and completeness.
/// </summary>
public sealed class ReshardingErrorCodesTests
{
    #region Test Helpers

    private static FieldInfo[] GetAllConstantFields()
    {
        return typeof(ReshardingErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToArray();
    }

    #endregion

    #region Constant Count

    [Fact]
    public void ErrorCodes_Has16Constants()
    {
        var fields = GetAllConstantFields();

        fields.Length.ShouldBe(16);
    }

    #endregion

    #region Naming Convention

    [Fact]
    public void ErrorCodes_AllStartWithEncinaShardingResharding()
    {
        var fields = GetAllConstantFields();

        foreach (var field in fields)
        {
            var value = (string)field.GetRawConstantValue()!;
            value.ShouldStartWith("encina.sharding.resharding.");
        }

    }

    #endregion

    #region Uniqueness

    [Fact]
    public void ErrorCodes_AllValuesAreUnique()
    {
        var fields = GetAllConstantFields();
        var values = fields.Select(f => (string)f.GetRawConstantValue()!).ToList();
        var distinctValues = values.Distinct().ToList();

        distinctValues.Count.ShouldBe(values.Count,
            $"Duplicate error codes found: {string.Join(", ", values.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key))}");
    }

    #endregion

    #region Specific Values

    [Fact]
    public void TopologiesIdentical_HasExpectedValue()
    {
        ReshardingErrorCodes.TopologiesIdentical
            .ShouldBe("encina.sharding.resharding.topologies_identical");
    }

    [Fact]
    public void ConcurrentReshardingNotAllowed_HasExpectedValue()
    {
        ReshardingErrorCodes.ConcurrentReshardingNotAllowed
            .ShouldBe("encina.sharding.resharding.concurrent_resharding_not_allowed");
    }

    [Fact]
    public void CutoverTimeout_HasExpectedValue()
    {
        ReshardingErrorCodes.CutoverTimeout
            .ShouldBe("encina.sharding.resharding.cutover_timeout");
    }

    [Fact]
    public void StateStoreFailed_HasExpectedValue()
    {
        ReshardingErrorCodes.StateStoreFailed
            .ShouldBe("encina.sharding.resharding.state_store_failed");
    }

    #endregion
}
