using Encina.Security.ABAC;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML higher-order functions: any-of, all-of, any-of-any,
/// all-of-any, all-of-all, map.
/// </summary>
public sealed class HigherOrderFunctionsTests
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

    #region any-of

    [Fact]
    public void AnyOf_MatchFound_ReturnsTrue()
    {
        // any-of(string-equal, "admin", bag("admin", "user")) → true
        var bag = MakeBag(XACMLDataTypes.String, "admin", "user");

        Eval(XACMLFunctionIds.AnyOfFunc, XACMLFunctionIds.StringEqual, "admin", bag)
            .ShouldBe(true);
    }

    [Fact]
    public void AnyOf_NoMatch_ReturnsFalse()
    {
        // any-of(string-equal, "manager", bag("admin", "user")) → false
        var bag = MakeBag(XACMLDataTypes.String, "admin", "user");

        Eval(XACMLFunctionIds.AnyOfFunc, XACMLFunctionIds.StringEqual, "manager", bag)
            .ShouldBe(false);
    }

    [Fact]
    public void AnyOf_EmptyBag_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.AnyOfFunc, XACMLFunctionIds.StringEqual, "admin", AttributeBag.Empty)
            .ShouldBe(false);
    }

    [Fact]
    public void AnyOf_UnregisteredFunction_Throws()
    {
        var bag = MakeBag(XACMLDataTypes.String, "a");
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;
        var act = () => fn.Evaluate(["nonexistent-function", "a", bag]);
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("nonexistent-function");
    }

    [Fact]
    public void AnyOf_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;
        var act = () => fn.Evaluate(["string-equal", "a"]);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region all-of

    [Fact]
    public void AllOf_AllMatch_ReturnsTrue()
    {
        // all-of(integer-greater-than, 0, bag(1, 2, 3)) → true (0 < 1, 0 < 2, 0 < 3)
        var bag = MakeBag(XACMLDataTypes.Integer, 1, 2, 3);

        Eval(XACMLFunctionIds.AllOfFunc, XACMLFunctionIds.IntegerGreaterThan, 10, bag)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOf_SomeNotMatch_ReturnsFalse()
    {
        // all-of(string-equal, "admin", bag("admin", "user")) → false
        var bag = MakeBag(XACMLDataTypes.String, "admin", "user");

        Eval(XACMLFunctionIds.AllOfFunc, XACMLFunctionIds.StringEqual, "admin", bag)
            .ShouldBe(false);
    }

    [Fact]
    public void AllOf_EmptyBag_ReturnsTrue()
    {
        // Vacuous truth: all elements match when there are no elements
        Eval(XACMLFunctionIds.AllOfFunc, XACMLFunctionIds.StringEqual, "admin", AttributeBag.Empty)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOf_SingleMatchingElement_ReturnsTrue()
    {
        var bag = MakeBag(XACMLDataTypes.String, "admin");

        Eval(XACMLFunctionIds.AllOfFunc, XACMLFunctionIds.StringEqual, "admin", bag)
            .ShouldBe(true);
    }

    #endregion

    #region any-of-any

    [Fact]
    public void AnyOfAny_CommonElement_ReturnsTrue()
    {
        // any-of-any(string-equal, bag("a", "b"), bag("b", "c")) → true
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "b", "c");

        Eval(XACMLFunctionIds.AnyOfAny, XACMLFunctionIds.StringEqual, bag1, bag2)
            .ShouldBe(true);
    }

    [Fact]
    public void AnyOfAny_NoCommon_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "c", "d");

        Eval(XACMLFunctionIds.AnyOfAny, XACMLFunctionIds.StringEqual, bag1, bag2)
            .ShouldBe(false);
    }

    [Fact]
    public void AnyOfAny_EmptyBag1_ReturnsFalse()
    {
        var bag2 = MakeBag(XACMLDataTypes.String, "a");

        Eval(XACMLFunctionIds.AnyOfAny, XACMLFunctionIds.StringEqual, AttributeBag.Empty, bag2)
            .ShouldBe(false);
    }

    [Fact]
    public void AnyOfAny_BothEmpty_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.AnyOfAny, XACMLFunctionIds.StringEqual, AttributeBag.Empty, AttributeBag.Empty)
            .ShouldBe(false);
    }

    #endregion

    #region all-of-any

    [Fact]
    public void AllOfAny_AllHaveMatch_ReturnsTrue()
    {
        // all-of-any(string-equal, bag("a", "b"), bag("a", "b", "c")) → true
        // For "a": exists "a" in bag2 ✓, For "b": exists "b" in bag2 ✓
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "b");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b", "c");

        Eval(XACMLFunctionIds.AllOfAny, XACMLFunctionIds.StringEqual, bag1, bag2)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOfAny_SomeMissing_ReturnsFalse()
    {
        // all-of-any(string-equal, bag("a", "x"), bag("a", "b")) → false
        // For "a": exists "a" ✓, For "x": no match ✗
        var bag1 = MakeBag(XACMLDataTypes.String, "a", "x");
        var bag2 = MakeBag(XACMLDataTypes.String, "a", "b");

        Eval(XACMLFunctionIds.AllOfAny, XACMLFunctionIds.StringEqual, bag1, bag2)
            .ShouldBe(false);
    }

    [Fact]
    public void AllOfAny_EmptyBag1_ReturnsTrue()
    {
        // Vacuous truth: all (zero) elements in bag1 have a match
        var bag2 = MakeBag(XACMLDataTypes.String, "a");

        Eval(XACMLFunctionIds.AllOfAny, XACMLFunctionIds.StringEqual, AttributeBag.Empty, bag2)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOfAny_EmptyBag2_NonEmptyBag1_ReturnsFalse()
    {
        var bag1 = MakeBag(XACMLDataTypes.String, "a");

        Eval(XACMLFunctionIds.AllOfAny, XACMLFunctionIds.StringEqual, bag1, AttributeBag.Empty)
            .ShouldBe(false);
    }

    #endregion

    #region all-of-all

    [Fact]
    public void AllOfAll_AllPairsMatch_ReturnsTrue()
    {
        // all-of-all(integer-greater-than, bag(10, 20), bag(1, 2, 3)) → true
        // 10>1✓, 10>2✓, 10>3✓, 20>1✓, 20>2✓, 20>3✓
        var bag1 = MakeBag(XACMLDataTypes.Integer, 10, 20);
        var bag2 = MakeBag(XACMLDataTypes.Integer, 1, 2, 3);

        Eval(XACMLFunctionIds.AllOfAll, XACMLFunctionIds.IntegerGreaterThan, bag1, bag2)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOfAll_SomePairsFail_ReturnsFalse()
    {
        // all-of-all(integer-greater-than, bag(10, 2), bag(1, 5)) → false
        // 10>1✓, 10>5✓, 2>1✓, 2>5✗
        var bag1 = MakeBag(XACMLDataTypes.Integer, 10, 2);
        var bag2 = MakeBag(XACMLDataTypes.Integer, 1, 5);

        Eval(XACMLFunctionIds.AllOfAll, XACMLFunctionIds.IntegerGreaterThan, bag1, bag2)
            .ShouldBe(false);
    }

    [Fact]
    public void AllOfAll_EmptyBag1_ReturnsTrue()
    {
        var bag2 = MakeBag(XACMLDataTypes.Integer, 1, 2);

        Eval(XACMLFunctionIds.AllOfAll, XACMLFunctionIds.IntegerGreaterThan, AttributeBag.Empty, bag2)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOfAll_EmptyBag2_ReturnsTrue()
    {
        var bag1 = MakeBag(XACMLDataTypes.Integer, 1, 2);

        Eval(XACMLFunctionIds.AllOfAll, XACMLFunctionIds.IntegerGreaterThan, bag1, AttributeBag.Empty)
            .ShouldBe(true);
    }

    [Fact]
    public void AllOfAll_BothEmpty_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.AllOfAll, XACMLFunctionIds.IntegerGreaterThan, AttributeBag.Empty, AttributeBag.Empty)
            .ShouldBe(true);
    }

    #endregion

    #region map

    [Fact]
    public void Map_AppliesFunctionToEachElement()
    {
        // map(string-from-integer, bag(1, 2, 3)) → bag("1", "2", "3")
        var bag = MakeBag(XACMLDataTypes.Integer, 1, 2, 3);

        var result = (AttributeBag)Eval(XACMLFunctionIds.Map, XACMLFunctionIds.StringFromInteger, bag)!;

        result.Count.ShouldBe(3);
        result.Values[0].Value.ShouldBe("1");
        result.Values[1].Value.ShouldBe("2");
        result.Values[2].Value.ShouldBe("3");
    }

    [Fact]
    public void Map_EmptyBag_ReturnsEmptyBag()
    {
        var result = (AttributeBag)Eval(XACMLFunctionIds.Map, XACMLFunctionIds.StringFromInteger, AttributeBag.Empty)!;

        result.Count.ShouldBe(0);
    }

    [Fact]
    public void Map_SetsReturnTypeFromFunction()
    {
        var bag = MakeBag(XACMLDataTypes.Integer, 42);

        var result = (AttributeBag)Eval(XACMLFunctionIds.Map, XACMLFunctionIds.StringFromInteger, bag)!;

        result.Values[0].DataType.ShouldBe(XACMLDataTypes.String);
    }

    [Fact]
    public void Map_UnregisteredFunction_Throws()
    {
        var bag = MakeBag(XACMLDataTypes.String, "a");
        var fn = _registry.GetFunction(XACMLFunctionIds.Map)!;
        var act = () => fn.Evaluate(["nonexistent-function", bag]);
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("nonexistent-function");
    }

    [Fact]
    public void Map_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Map)!;
        var act = () => fn.Evaluate(["string-from-integer"]);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion
}
