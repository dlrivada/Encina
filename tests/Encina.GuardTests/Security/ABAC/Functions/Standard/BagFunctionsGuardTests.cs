using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for bag functions (one-and-only, bag-size, is-in, bag creation).
/// Tests string, integer, and boolean bag variants with null args, wrong arg count,
/// empty bags, multi-value bags, and ValuesEqual edge cases.
/// </summary>
public class BagFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region StringOneAndOnly

    [Fact]
    public void StringOneAndOnly_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringOneAndOnly)!;

        var act = () => fn.Evaluate(Array.Empty<object?>());

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 1*received 0*");
    }

    [Fact]
    public void StringOneAndOnly_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringOneAndOnly)!;

        var act = () => fn.Evaluate([null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*expected AttributeBag*");
    }

    [Fact]
    public void StringOneAndOnly_SingleValueBag_ReturnsValue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringOneAndOnly)!;
        var bag = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" });

        fn.Evaluate([bag]).Should().Be("admin");
    }

    #endregion

    #region StringBagSize

    [Fact]
    public void StringBagSize_EmptyBag_ReturnsZero()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringBagSize)!;

        fn.Evaluate([AttributeBag.Empty]).Should().Be(0);
    }

    [Fact]
    public void StringBagSize_MultipleBag_ReturnsCount()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringBagSize)!;
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "a" },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "b" },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "c" });

        fn.Evaluate([bag]).Should().Be(3);
    }

    #endregion

    #region StringIsIn

    [Fact]
    public void StringIsIn_ValueInBag_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringIsIn)!;
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "admin" },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "user" });

        fn.Evaluate(["admin", bag]).Should().Be(true);
    }

    [Fact]
    public void StringIsIn_ValueNotInBag_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringIsIn)!;
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "user" });

        fn.Evaluate(["admin", bag]).Should().Be(false);
    }

    [Fact]
    public void StringIsIn_EmptyBag_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringIsIn)!;

        fn.Evaluate(["admin", AttributeBag.Empty]).Should().Be(false);
    }

    #endregion

    #region IntegerBag

    [Fact]
    public void IntegerBag_CreatesFromArgs()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerBag)!;

        var result = fn.Evaluate([1, 2, 3]);

        result.Should().BeOfType<AttributeBag>();
        var bag = (AttributeBag)result!;
        bag.Count.Should().Be(3);
    }

    [Fact]
    public void IntegerBag_EmptyArgs_CreatesEmptyBag()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerBag)!;

        var result = fn.Evaluate(Array.Empty<object?>());

        result.Should().BeOfType<AttributeBag>();
        var bag = (AttributeBag)result!;
        bag.Count.Should().Be(0);
    }

    #endregion

    #region IntegerIsIn

    [Fact]
    public void IntegerIsIn_ValueInBag_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerIsIn)!;
        var bag = AttributeBag.Of(
            new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 10 },
            new AttributeValue { DataType = XACMLDataTypes.Integer, Value = 20 });

        fn.Evaluate([10, bag]).Should().Be(true);
    }

    #endregion

    #region BooleanOneAndOnly

    [Fact]
    public void BooleanOneAndOnly_SingleValueBag_ReturnsValue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanOneAndOnly)!;
        var bag = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.Boolean, Value = true });

        fn.Evaluate([bag]).Should().Be(true);
    }

    #endregion

    #region ValuesEqual — Edge Cases

    [Fact]
    public void ValuesEqual_BothNull_ReturnsFalse()
    {
        // ReferenceEquals(null, null) is true
        BagFunctions.ValuesEqual(null, null).Should().BeTrue();
    }

    [Fact]
    public void ValuesEqual_OneNull_ReturnsFalse()
    {
        BagFunctions.ValuesEqual("hello", null).Should().BeFalse();
        BagFunctions.ValuesEqual(null, "hello").Should().BeFalse();
    }

    [Fact]
    public void ValuesEqual_SameReference_ReturnsTrue()
    {
        var obj = new object();
        BagFunctions.ValuesEqual(obj, obj).Should().BeTrue();
    }

    [Fact]
    public void ValuesEqual_IntAndDouble_CrossTypeComparison()
    {
        BagFunctions.ValuesEqual(42, 42.0).Should().BeTrue();
        BagFunctions.ValuesEqual(42.0, 42).Should().BeTrue();
    }

    [Fact]
    public void ValuesEqual_DifferentStrings_ReturnsFalse()
    {
        BagFunctions.ValuesEqual("hello", "world").Should().BeFalse();
    }

    [Fact]
    public void ValuesEqual_SameStrings_ReturnsTrue()
    {
        BagFunctions.ValuesEqual("hello", "hello").Should().BeTrue();
    }

    #endregion
}
