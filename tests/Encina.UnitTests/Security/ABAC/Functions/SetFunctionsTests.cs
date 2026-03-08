using Encina.Security.ABAC;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML set functions (intersection, union, subset,
/// at-least-one-member-of, set-equals) for string, integer, and double types.
/// </summary>
public sealed class SetFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    private static AttributeBag MakeBag(string dataType, params object?[] values)
    {
        var attrValues = values.Select(v => new AttributeValue
        {
            DataType = dataType,
            Value = v
        }).ToArray();

        return attrValues.Length == 0 ? AttributeBag.Empty : AttributeBag.Of(attrValues);
    }

    private static List<object?> ExtractValues(AttributeBag bag) =>
        bag.Values.Select(v => v.Value).ToList();

    #region Intersection

    [Fact]
    public void StringIntersection_CommonElements_ReturnsIntersection()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b", "c");
        var bag2 = MakeBag(XACMLDataTypes.String, "b", "c", "d");

        var result = (AttributeBag)Eval(XACMLFunctionIds.StringIntersection, bag1, bag2)!;

        var values = ExtractValues(result);
        values.Should().HaveCount(2);
        values.Should().Contain("b");
        values.Should().Contain("c");
    }

    [Fact]
    public void StringIntersection_DisjointBags_ReturnsEmpty()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "c", "d");

        var result = (AttributeBag)Eval(XACMLFunctionIds.StringIntersection, bag1, bag2)!;

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void StringIntersection_EmptyBag_ReturnsEmpty()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");

        var result = (AttributeBag)Eval(XACMLFunctionIds.StringIntersection, bag1, AttributeBag.Empty)!;

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IntegerIntersection_WithDuplicates_RemovesDuplicates()
    {
        var bag1 = MakeBag(XACMLDataTypes.Integer, 1, 1, 2);
        var bag2 = MakeBag(XACMLDataTypes.Integer, 1, 2, 3);

        var result = (AttributeBag)Eval(XACMLFunctionIds.IntegerIntersection, bag1, bag2)!;

        var values = ExtractValues(result);
        values.Should().HaveCount(2);
        values.Should().Contain(1);
        values.Should().Contain(2);
    }

    #endregion

    #region Union

    [Fact]
    public void StringUnion_DistinctElements_ReturnsCombined()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "c", "d");

        var result = (AttributeBag)Eval(XACMLFunctionIds.StringUnion, bag1, bag2)!;

        var values = ExtractValues(result);
        values.Should().HaveCount(4);
        values.Should().Contain("a");
        values.Should().Contain("b");
        values.Should().Contain("c");
        values.Should().Contain("d");
    }

    [Fact]
    public void StringUnion_OverlappingElements_RemovesDuplicates()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b", "c");
        var bag2 = MakeBag(XACMLDataTypes.String, "b", "c", "d");

        var result = (AttributeBag)Eval(XACMLFunctionIds.StringUnion, bag1, bag2)!;

        var values = ExtractValues(result);
        values.Should().HaveCount(4);
    }

    [Fact]
    public void IntegerUnion_IdenticalBags_ReturnsSameSet()
    {
        var bag1 = MakeBag(XACMLDataTypes.Integer, 1, 2, 3);
        var bag2 = MakeBag(XACMLDataTypes.Integer, 1, 2, 3);

        var result = (AttributeBag)Eval(XACMLFunctionIds.IntegerUnion, bag1, bag2)!;

        ExtractValues(result).Should().HaveCount(3);
    }

    [Fact]
    public void StringUnion_EmptyBags_ReturnsEmpty()
    {
        var result = (AttributeBag)Eval(XACMLFunctionIds.StringUnion, AttributeBag.Empty, AttributeBag.Empty)!;

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void DoubleUnion_MixedElements_CombinesAll()
    {
        var bag1 = MakeBag(XACMLDataTypes.Double, 1.0, 2.0);
        var bag2 = MakeBag(XACMLDataTypes.Double, 3.0);

        var result = (AttributeBag)Eval(XACMLFunctionIds.DoubleUnion, bag1, bag2)!;

        ExtractValues(result).Should().HaveCount(3);
    }

    #endregion

    #region Subset

    [Fact]
    public void StringSubset_ProperSubset_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b", "c");

        Eval(XACMLFunctionIds.StringSubset, bag1, bag2).Should().Be(true);
    }

    [Fact]
    public void StringSubset_EqualSets_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b");

        Eval(XACMLFunctionIds.StringSubset, bag1, bag2).Should().Be(true);
    }

    [Fact]
    public void StringSubset_NotSubset_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b", "x");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b", "c");

        Eval(XACMLFunctionIds.StringSubset, bag1, bag2).Should().Be(false);
    }

    [Fact]
    public void StringSubset_EmptyBag1_ReturnsTrue()
    {
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b");

        Eval(XACMLFunctionIds.StringSubset, AttributeBag.Empty, bag2).Should().Be(true);
    }

    [Fact]
    public void IntegerSubset_EmptyBag2_NonEmptyBag1_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.Integer, 1);

        Eval(XACMLFunctionIds.IntegerSubset, bag1, AttributeBag.Empty).Should().Be(false);
    }

    [Fact]
    public void IntegerSubset_BothEmpty_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.IntegerSubset, AttributeBag.Empty, AttributeBag.Empty)
            .Should().Be(true);
    }

    #endregion

    #region AtLeastOneMemberOf

    [Fact]
    public void StringAtLeastOneMemberOf_CommonElement_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "b", "c");

        Eval(XACMLFunctionIds.StringAtLeastOneMemberOf, bag1, bag2).Should().Be(true);
    }

    [Fact]
    public void StringAtLeastOneMemberOf_NoCommon_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "c", "d");

        Eval(XACMLFunctionIds.StringAtLeastOneMemberOf, bag1, bag2).Should().Be(false);
    }

    [Fact]
    public void IntegerAtLeastOneMemberOf_EmptyBag_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.Integer, 1, 2);

        Eval(XACMLFunctionIds.IntegerAtLeastOneMemberOf, bag1, AttributeBag.Empty)
            .Should().Be(false);
    }

    [Fact]
    public void DoubleAtLeastOneMemberOf_FullOverlap_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.Double, 1.0, 2.0);
        var bag2 = MakeBag(XACMLDataTypes.Double, 1.0, 2.0, 3.0);

        Eval(XACMLFunctionIds.DoubleAtLeastOneMemberOf, bag1, bag2).Should().Be(true);
    }

    #endregion

    #region SetEquals

    [Fact]
    public void StringSetEquals_SameSets_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b", "c");
        var bag2 = MakeBag(XACMLDataTypes.String, "c", "b", "a");

        Eval(XACMLFunctionIds.StringSetEquals, bag1, bag2).Should().Be(true);
    }

    [Fact]
    public void StringSetEquals_DifferentSets_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "c");

        Eval(XACMLFunctionIds.StringSetEquals, bag1, bag2).Should().Be(false);
    }

    [Fact]
    public void StringSetEquals_SubsetButNotEqual_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b", "c");

        Eval(XACMLFunctionIds.StringSetEquals, bag1, bag2).Should().Be(false);
    }

    [Fact]
    public void IntegerSetEquals_WithDuplicates_TreatsAsSet()
    {
        var bag1 = MakeBag(XACMLDataTypes.Integer, 1, 1, 2);
        var bag2 = MakeBag(XACMLDataTypes.Integer, 1, 2, 2);

        Eval(XACMLFunctionIds.IntegerSetEquals, bag1, bag2).Should().Be(true);
    }

    [Fact]
    public void StringSetEquals_BothEmpty_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.StringSetEquals, AttributeBag.Empty, AttributeBag.Empty)
            .Should().Be(true);
    }

    [Fact]
    public void DoubleSetEquals_SameValues_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.Double, 1.5, 2.5);
        var bag2 = MakeBag(XACMLDataTypes.Double, 2.5, 1.5);

        Eval(XACMLFunctionIds.DoubleSetEquals, bag1, bag2).Should().Be(true);
    }

    #endregion
}
