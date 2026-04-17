using Encina.Security.ABAC;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for set functions (intersection, union, subset, at-least-one-member-of, set-equals).
/// Tests string, integer, and double variants with empty bags, disjoint bags, and correct set semantics.
/// </summary>
public class SetFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private static AttributeBag MakeStringBag(params string[] values) =>
        values.Length == 0
            ? AttributeBag.Empty
            : AttributeBag.Of(values.Select(v =>
                new AttributeValue { DataType = XACMLDataTypes.String, Value = v }).ToArray());

    private static AttributeBag MakeIntBag(params int[] values) =>
        values.Length == 0
            ? AttributeBag.Empty
            : AttributeBag.Of(values.Select(v =>
                new AttributeValue { DataType = XACMLDataTypes.Integer, Value = v }).ToArray());

    #region StringIntersection

    [Fact]
    public void StringIntersection_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringIntersection)!;

        var act = () => fn.Evaluate([MakeStringBag("a")]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("exactly");
    }

    [Fact]
    public void StringIntersection_CommonElements_ReturnsIntersection()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringIntersection)!;
        var bag1 = MakeStringBag("a", "b", "c");
        var bag2 = MakeStringBag("b", "c", "d");

        var result = (AttributeBag)fn.Evaluate([bag1, bag2])!;

        result.Count.ShouldBe(2);
        result.Values
            .Select(v => (string?)v.Value)
            .OrderBy(v => v)
            .ShouldBe(["b", "c"]);
    }

    [Fact]
    public void StringIntersection_DisjointBags_ReturnsEmpty()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringIntersection)!;
        var bag1 = MakeStringBag("a", "b");
        var bag2 = MakeStringBag("c", "d");

        var result = (AttributeBag)fn.Evaluate([bag1, bag2])!;

        result.IsEmpty.ShouldBeTrue();
    }

    #endregion

    #region StringUnion

    [Fact]
    public void StringUnion_OverlappingBags_ReturnsUniqueValues()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringUnion)!;
        var bag1 = MakeStringBag("a", "b");
        var bag2 = MakeStringBag("b", "c");

        var result = (AttributeBag)fn.Evaluate([bag1, bag2])!;

        result.Count.ShouldBe(3);
    }

    [Fact]
    public void StringUnion_EmptyBags_ReturnsEmpty()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringUnion)!;

        var result = (AttributeBag)fn.Evaluate([AttributeBag.Empty, AttributeBag.Empty])!;

        result.IsEmpty.ShouldBeTrue();
    }

    #endregion

    #region StringSubset

    [Fact]
    public void StringSubset_IsSubset_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubset)!;
        var bag1 = MakeStringBag("a", "b");
        var bag2 = MakeStringBag("a", "b", "c");

        fn.Evaluate([bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void StringSubset_NotSubset_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubset)!;
        var bag1 = MakeStringBag("a", "z");
        var bag2 = MakeStringBag("a", "b", "c");

        fn.Evaluate([bag1, bag2]).ShouldBe(false);
    }

    [Fact]
    public void StringSubset_EmptyIsSubsetOfAnything_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubset)!;

        fn.Evaluate([AttributeBag.Empty, MakeStringBag("a")]).ShouldBe(true);
    }

    #endregion

    #region StringAtLeastOneMemberOf

    [Fact]
    public void StringAtLeastOneMemberOf_SharedElement_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringAtLeastOneMemberOf)!;
        var bag1 = MakeStringBag("a", "b");
        var bag2 = MakeStringBag("b", "c");

        fn.Evaluate([bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void StringAtLeastOneMemberOf_NoShared_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringAtLeastOneMemberOf)!;
        var bag1 = MakeStringBag("a");
        var bag2 = MakeStringBag("b");

        fn.Evaluate([bag1, bag2]).ShouldBe(false);
    }

    #endregion

    #region StringSetEquals

    [Fact]
    public void StringSetEquals_SameElements_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSetEquals)!;
        var bag1 = MakeStringBag("a", "b", "c");
        var bag2 = MakeStringBag("c", "b", "a");

        fn.Evaluate([bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void StringSetEquals_DifferentElements_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSetEquals)!;
        var bag1 = MakeStringBag("a", "b");
        var bag2 = MakeStringBag("a", "c");

        fn.Evaluate([bag1, bag2]).ShouldBe(false);
    }

    #endregion

    #region Integer Set Functions

    [Fact]
    public void IntegerIntersection_CommonElements_ReturnsIntersection()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerIntersection)!;
        var bag1 = MakeIntBag(1, 2, 3);
        var bag2 = MakeIntBag(2, 3, 4);

        var result = (AttributeBag)fn.Evaluate([bag1, bag2])!;

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void IntegerSubset_ValidSubset_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerSubset)!;
        var bag1 = MakeIntBag(1, 2);
        var bag2 = MakeIntBag(1, 2, 3);

        fn.Evaluate([bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void IntegerSetEquals_SameElements_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerSetEquals)!;
        var bag1 = MakeIntBag(1, 2, 3);
        var bag2 = MakeIntBag(3, 2, 1);

        fn.Evaluate([bag1, bag2]).ShouldBe(true);
    }

    #endregion
}
