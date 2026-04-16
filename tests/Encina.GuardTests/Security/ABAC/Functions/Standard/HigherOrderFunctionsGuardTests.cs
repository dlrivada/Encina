using Encina.Security.ABAC;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for higher-order functions (any-of, all-of, any-of-any, all-of-any, all-of-all, map).
/// Covers wrong arg count, null args, unregistered function references, and evaluation logic.
/// </summary>
public class HigherOrderFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private static AttributeBag MakeStringBag(params string[] values) =>
        AttributeBag.Of(values.Select(v =>
            new AttributeValue { DataType = XACMLDataTypes.String, Value = v }).ToArray());

    #region AnyOf

    [Fact]
    public void AnyOf_TooFewArgs_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;

        var act = () => fn.Evaluate([XACMLFunctionIds.StringEqual, "admin"]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("at");
    }

    [Fact]
    public void AnyOf_NullFunctionId_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;
        var bag = MakeStringBag("admin");

        var act = () => fn.Evaluate([null, "admin", bag]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("must");
    }

    [Fact]
    public void AnyOf_UnregisteredFunction_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;
        var bag = MakeStringBag("admin");

        var act = () => fn.Evaluate(["nonexistent-function", "admin", bag]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("not");
    }

    [Fact]
    public void AnyOf_MatchFound_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;
        var bag = MakeStringBag("admin", "user", "manager");

        fn.Evaluate([XACMLFunctionIds.StringEqual, "admin", bag]).ShouldBe(true);
    }

    [Fact]
    public void AnyOf_NoMatch_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfFunc)!;
        var bag = MakeStringBag("user", "manager");

        fn.Evaluate([XACMLFunctionIds.StringEqual, "admin", bag]).ShouldBe(false);
    }

    #endregion

    #region AllOf

    [Fact]
    public void AllOf_AllMatch_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfFunc)!;
        var bag = MakeStringBag("admin", "admin", "admin");

        fn.Evaluate([XACMLFunctionIds.StringEqual, "admin", bag]).ShouldBe(true);
    }

    [Fact]
    public void AllOf_OneMismatch_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfFunc)!;
        var bag = MakeStringBag("admin", "user", "admin");

        fn.Evaluate([XACMLFunctionIds.StringEqual, "admin", bag]).ShouldBe(false);
    }

    [Fact]
    public void AllOf_UnregisteredFunction_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfFunc)!;
        var bag = MakeStringBag("admin");

        var act = () => fn.Evaluate(["nonexistent-function", "admin", bag]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("not");
    }

    #endregion

    #region AnyOfAny

    [Fact]
    public void AnyOfAny_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfAny)!;

        var act = () => fn.Evaluate([XACMLFunctionIds.StringEqual, MakeStringBag("a")]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("exactly");
    }

    [Fact]
    public void AnyOfAny_CommonElement_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfAny)!;
        var bag1 = MakeStringBag("admin", "user");
        var bag2 = MakeStringBag("manager", "admin");

        fn.Evaluate([XACMLFunctionIds.StringEqual, bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void AnyOfAny_NoCommonElement_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AnyOfAny)!;
        var bag1 = MakeStringBag("admin", "user");
        var bag2 = MakeStringBag("manager", "viewer");

        fn.Evaluate([XACMLFunctionIds.StringEqual, bag1, bag2]).ShouldBe(false);
    }

    #endregion

    #region AllOfAny

    [Fact]
    public void AllOfAny_AllHaveMatch_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfAny)!;
        var bag1 = MakeStringBag("a", "b");
        var bag2 = MakeStringBag("a", "b", "c");

        fn.Evaluate([XACMLFunctionIds.StringEqual, bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void AllOfAny_OneHasNoMatch_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfAny)!;
        var bag1 = MakeStringBag("a", "z");
        var bag2 = MakeStringBag("a", "b", "c");

        fn.Evaluate([XACMLFunctionIds.StringEqual, bag1, bag2]).ShouldBe(false);
    }

    #endregion

    #region AllOfAll

    [Fact]
    public void AllOfAll_AllPairsMatch_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfAll)!;
        var bag1 = MakeStringBag("admin");
        var bag2 = MakeStringBag("admin");

        fn.Evaluate([XACMLFunctionIds.StringEqual, bag1, bag2]).ShouldBe(true);
    }

    [Fact]
    public void AllOfAll_SomePairsFail_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.AllOfAll)!;
        var bag1 = MakeStringBag("admin");
        var bag2 = MakeStringBag("admin", "user");

        fn.Evaluate([XACMLFunctionIds.StringEqual, bag1, bag2]).ShouldBe(false);
    }

    #endregion

    #region Map

    [Fact]
    public void Map_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Map)!;

        var act = () => fn.Evaluate([XACMLFunctionIds.StringNormalizeToLowerCase]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("exactly");
    }

    [Fact]
    public void Map_TransformsBagElements()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Map)!;
        var bag = MakeStringBag("HELLO", "WORLD");

        var result = fn.Evaluate([XACMLFunctionIds.StringNormalizeToLowerCase, bag]);

        result.ShouldBeOfType<AttributeBag>();
        var resultBag = (AttributeBag)result!;
        resultBag.Count.ShouldBe(2);
        resultBag.Values[0].Value.ShouldBe("hello");
        resultBag.Values[1].Value.ShouldBe("world");
    }

    [Fact]
    public void Map_UnregisteredFunction_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Map)!;
        var bag = MakeStringBag("hello");

        var act = () => fn.Evaluate(["nonexistent-function", bag]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("not");
    }

    #endregion
}
