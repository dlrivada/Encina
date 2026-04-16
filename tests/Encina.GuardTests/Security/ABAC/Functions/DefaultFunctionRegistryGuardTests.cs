using Encina.Security.ABAC;

using Shouldly;

using NSubstitute;

namespace Encina.GuardTests.Security.ABAC.Functions;

/// <summary>
/// Guard clause tests for <see cref="DefaultFunctionRegistry"/>.
/// Covers constructor initialization, GetFunction/Register/GetAllFunctionIds guards,
/// and custom function override behavior.
/// </summary>
public class DefaultFunctionRegistryGuardTests
{
    #region Constructor

    [Fact]
    public void Constructor_PreRegistersStandardFunctions()
    {
        var sut = new DefaultFunctionRegistry();

        var allIds = sut.GetAllFunctionIds();

        allIds.ShouldNotBeEmpty();
        allIds.ShouldContain(XACMLFunctionIds.StringEqual);
        allIds.ShouldContain(XACMLFunctionIds.IntegerAdd);
        allIds.ShouldContain(XACMLFunctionIds.And);
        allIds.ShouldContain(XACMLFunctionIds.StringRegexpMatch);
    }

    #endregion

    #region GetFunction — Guards

    [Fact]
    public void GetFunction_NullFunctionId_ThrowsArgumentException()
    {
        var sut = new DefaultFunctionRegistry();

        var act = () => sut.GetFunction(null!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void GetFunction_EmptyFunctionId_ThrowsArgumentException()
    {
        var sut = new DefaultFunctionRegistry();

        var act = () => sut.GetFunction("");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void GetFunction_WhitespaceFunctionId_ThrowsArgumentException()
    {
        var sut = new DefaultFunctionRegistry();

        var act = () => sut.GetFunction("   ");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void GetFunction_UnregisteredId_ReturnsNull()
    {
        var sut = new DefaultFunctionRegistry();

        var result = sut.GetFunction("nonexistent-function");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFunction_RegisteredStandardFunction_ReturnsFunction()
    {
        var sut = new DefaultFunctionRegistry();

        var result = sut.GetFunction(XACMLFunctionIds.StringEqual);

        result.ShouldNotBeNull();
        result!.ReturnType.ShouldBe(XACMLDataTypes.Boolean);
    }

    #endregion

    #region Register — Guards

    [Fact]
    public void Register_NullFunctionId_ThrowsArgumentException()
    {
        var sut = new DefaultFunctionRegistry();
        var fn = Substitute.For<IXACMLFunction>();

        var act = () => sut.Register(null!, fn);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Register_EmptyFunctionId_ThrowsArgumentException()
    {
        var sut = new DefaultFunctionRegistry();
        var fn = Substitute.For<IXACMLFunction>();

        var act = () => sut.Register("", fn);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Register_WhitespaceFunctionId_ThrowsArgumentException()
    {
        var sut = new DefaultFunctionRegistry();
        var fn = Substitute.For<IXACMLFunction>();

        var act = () => sut.Register("   ", fn);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Register_NullFunction_ThrowsArgumentNullException()
    {
        var sut = new DefaultFunctionRegistry();

        var act = () => sut.Register("custom-fn", null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Register_ValidCustomFunction_CanBeRetrieved()
    {
        var sut = new DefaultFunctionRegistry();
        var fn = Substitute.For<IXACMLFunction>();
        fn.ReturnType.Returns(XACMLDataTypes.Boolean);

        sut.Register("custom-geo-within", fn);

        var result = sut.GetFunction("custom-geo-within");
        result.ShouldBeSameAs(fn);
    }

    [Fact]
    public void Register_OverridesExistingFunction()
    {
        var sut = new DefaultFunctionRegistry();
        var customFn = Substitute.For<IXACMLFunction>();
        customFn.ReturnType.Returns(XACMLDataTypes.Boolean);

        sut.Register(XACMLFunctionIds.StringEqual, customFn);

        var result = sut.GetFunction(XACMLFunctionIds.StringEqual);
        result.ShouldBeSameAs(customFn);
    }

    #endregion

    #region GetAllFunctionIds

    [Fact]
    public void GetAllFunctionIds_ReturnsSortedList()
    {
        var sut = new DefaultFunctionRegistry();

        var ids = sut.GetAllFunctionIds();

        ids.ShouldBeInOrder(SortDirection.Ascending);
    }

    [Fact]
    public void GetAllFunctionIds_IncludesCustomFunctions()
    {
        var sut = new DefaultFunctionRegistry();
        var fn = Substitute.For<IXACMLFunction>();
        sut.Register("zzz-custom", fn);

        var ids = sut.GetAllFunctionIds();

        ids.ShouldContain("zzz-custom");
    }

    #endregion
}
