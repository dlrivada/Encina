using Encina.Security.ABAC;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML bag functions (one-and-only, bag-size, is-in, bag)
/// across all 8 data types: string, boolean, integer, double, date, dateTime, time, anyURI.
/// </summary>
public sealed class BagFunctionsTests
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

    #region one-and-only

    [Fact]
    public void StringOneAndOnly_SingleValue_ReturnsValue()
    {
        var bag = MakeBag(XACMLDataTypes.String, "hello");
        Eval(XACMLFunctionIds.StringOneAndOnly, bag).ShouldBe("hello");
    }

    [Fact]
    public void IntegerOneAndOnly_SingleValue_ReturnsValue()
    {
        var bag = MakeBag(XACMLDataTypes.Integer, 42);
        Eval(XACMLFunctionIds.IntegerOneAndOnly, bag).ShouldBe(42);
    }

    [Fact]
    public void BooleanOneAndOnly_SingleValue_ReturnsValue()
    {
        var bag = MakeBag(XACMLDataTypes.Boolean, true);
        Eval(XACMLFunctionIds.BooleanOneAndOnly, bag).ShouldBe(true);
    }

    [Fact]
    public void DoubleOneAndOnly_SingleValue_ReturnsValue()
    {
        var bag = MakeBag(XACMLDataTypes.Double, 3.14);
        Eval(XACMLFunctionIds.DoubleOneAndOnly, bag).ShouldBe(3.14);
    }

    [Fact]
    public void StringOneAndOnly_EmptyBag_Throws()
    {
        var bag = AttributeBag.Empty;
        var fn = _registry.GetFunction(XACMLFunctionIds.StringOneAndOnly)!;
        var act = () => fn.Evaluate([bag]);
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StringOneAndOnly_MultipleBagValues_Throws()
    {
        var bag = MakeBag(XACMLDataTypes.String, "a", "b");
        var fn = _registry.GetFunction(XACMLFunctionIds.StringOneAndOnly)!;
        var act = () => fn.Evaluate([bag]);
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StringOneAndOnly_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringOneAndOnly)!;
        var act = () => fn.Evaluate([]);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region bag-size

    [Fact]
    public void StringBagSize_EmptyBag_ReturnsZero()
    {
        var bag = AttributeBag.Empty;
        Eval(XACMLFunctionIds.StringBagSize, bag).ShouldBe(0);
    }

    [Fact]
    public void StringBagSize_SingleElement_ReturnsOne()
    {
        var bag = MakeBag(XACMLDataTypes.String, "test");
        Eval(XACMLFunctionIds.StringBagSize, bag).ShouldBe(1);
    }

    [Fact]
    public void IntegerBagSize_MultipleElements_ReturnsCount()
    {
        var bag = MakeBag(XACMLDataTypes.Integer, 1, 2, 3, 4, 5);
        Eval(XACMLFunctionIds.IntegerBagSize, bag).ShouldBe(5);
    }

    [Fact]
    public void DoubleBagSize_MultipleElements_ReturnsCount()
    {
        var bag = MakeBag(XACMLDataTypes.Double, 1.0, 2.0, 3.0);
        Eval(XACMLFunctionIds.DoubleBagSize, bag).ShouldBe(3);
    }

    #endregion

    #region is-in

    [Fact]
    public void StringIsIn_ValuePresent_ReturnsTrue()
    {
        var bag = MakeBag(XACMLDataTypes.String, "admin", "user", "manager");
        Eval(XACMLFunctionIds.StringIsIn, "admin", bag).ShouldBe(true);
    }

    [Fact]
    public void StringIsIn_ValueNotPresent_ReturnsFalse()
    {
        var bag = MakeBag(XACMLDataTypes.String, "admin", "user");
        Eval(XACMLFunctionIds.StringIsIn, "manager", bag).ShouldBe(false);
    }

    [Fact]
    public void IntegerIsIn_ValuePresent_ReturnsTrue()
    {
        var bag = MakeBag(XACMLDataTypes.Integer, 10, 20, 30);
        Eval(XACMLFunctionIds.IntegerIsIn, 20, bag).ShouldBe(true);
    }

    [Fact]
    public void IntegerIsIn_ValueNotPresent_ReturnsFalse()
    {
        var bag = MakeBag(XACMLDataTypes.Integer, 10, 20, 30);
        Eval(XACMLFunctionIds.IntegerIsIn, 99, bag).ShouldBe(false);
    }

    [Fact]
    public void BooleanIsIn_ValuePresent_ReturnsTrue()
    {
        var bag = MakeBag(XACMLDataTypes.Boolean, true);
        Eval(XACMLFunctionIds.BooleanIsIn, true, bag).ShouldBe(true);
    }

    [Fact]
    public void StringIsIn_EmptyBag_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.StringIsIn, "any", AttributeBag.Empty).ShouldBe(false);
    }

    [Fact]
    public void StringIsIn_CaseSensitive_ReturnsFalse()
    {
        var bag = MakeBag(XACMLDataTypes.String, "Admin");
        Eval(XACMLFunctionIds.StringIsIn, "admin", bag).ShouldBe(false);
    }

    #endregion

    #region bag (creation)

    [Fact]
    public void StringBag_CreatesWithValues()
    {
        var result = Eval(XACMLFunctionIds.StringBag, "a", "b", "c");

        result.ShouldBeOfType<AttributeBag>();
        var bag = (AttributeBag)result!;
        bag.Count.ShouldBe(3);
        bag.Values[0].Value.ShouldBe("a");
        bag.Values[1].Value.ShouldBe("b");
        bag.Values[2].Value.ShouldBe("c");
    }

    [Fact]
    public void StringBag_EmptyArgs_CreatesEmptyBag()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringBag)!;
        var result = fn.Evaluate([]);

        result.ShouldBeOfType<AttributeBag>();
        ((AttributeBag)result!).Count.ShouldBe(0);
    }

    [Fact]
    public void IntegerBag_CreatesWithValues()
    {
        var result = Eval(XACMLFunctionIds.IntegerBag, 1, 2, 3);

        result.ShouldBeOfType<AttributeBag>();
        var bag = (AttributeBag)result!;
        bag.Count.ShouldBe(3);
        bag.Values.Select(v => v.DataType).ShouldAllBe(dt => dt == XACMLDataTypes.Integer);
    }

    [Fact]
    public void DoubleBag_SetsCorrectDataType()
    {
        var result = Eval(XACMLFunctionIds.DoubleBag, 1.5);

        var bag = (AttributeBag)result!;
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.Double);
    }

    [Fact]
    public void BooleanBag_SetsCorrectDataType()
    {
        var result = Eval(XACMLFunctionIds.BooleanBag, true, false);

        var bag = (AttributeBag)result!;
        bag.Count.ShouldBe(2);
        bag.Values.Select(v => v.DataType).ShouldAllBe(dt => dt == XACMLDataTypes.Boolean);
    }

    #endregion

    #region Date/Time/AnyURI types

    [Fact]
    public void DateOneAndOnly_SingleValue_ReturnsValue()
    {
        var date = new DateOnly(2026, 3, 8);
        var bag = MakeBag(XACMLDataTypes.Date, date);
        Eval(XACMLFunctionIds.DateOneAndOnly, bag).ShouldBe(date);
    }

    [Fact]
    public void DateTimeBagSize_ReturnsCount()
    {
        var now = DateTime.UtcNow;
        var bag = MakeBag(XACMLDataTypes.DateTime, now, now.AddHours(1));
        Eval(XACMLFunctionIds.DateTimeBagSize, bag).ShouldBe(2);
    }

    [Fact]
    public void TimeIsIn_ValuePresent_ReturnsTrue()
    {
        var time = new TimeOnly(14, 30);
        var bag = MakeBag(XACMLDataTypes.Time, time, new TimeOnly(8, 0));
        Eval(XACMLFunctionIds.TimeIsIn, time, bag).ShouldBe(true);
    }

    [Fact]
    public void AnyURIBag_CreatesWithValues()
    {
        var result = Eval(XACMLFunctionIds.AnyURIBag, "https://example.com", "https://other.com");

        result.ShouldBeOfType<AttributeBag>();
        var bag = (AttributeBag)result!;
        bag.Count.ShouldBe(2);
        bag.Values.Select(v => v.DataType).ShouldAllBe(dt => dt == XACMLDataTypes.AnyURI);
    }

    [Fact]
    public void AnyURIOneAndOnly_SingleValue_ReturnsValue()
    {
        var bag = MakeBag(XACMLDataTypes.AnyURI, "https://example.com");
        Eval(XACMLFunctionIds.AnyURIOneAndOnly, bag).ShouldBe("https://example.com");
    }

    #endregion
}
