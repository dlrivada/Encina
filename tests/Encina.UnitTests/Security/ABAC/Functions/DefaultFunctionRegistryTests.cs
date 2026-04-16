using Encina.Security.ABAC;
using Shouldly;
using NSubstitute;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for <see cref="DefaultFunctionRegistry"/>.
/// Verifies registration, retrieval, and standard function pre-loading.
/// </summary>
public sealed class DefaultFunctionRegistryTests
{
    private readonly DefaultFunctionRegistry _sut = new();

    #region Constructor — Pre-registration

    [Fact]
    public void Constructor_RegistersAllStandardFunctions()
    {
        var ids = _sut.GetAllFunctionIds();

        ids.ShouldNotBeEmpty("Standard XACML functions should be pre-registered");
    }

    [Theory]
    [InlineData(XACMLFunctionIds.StringEqual)]
    [InlineData(XACMLFunctionIds.BooleanEqual)]
    [InlineData(XACMLFunctionIds.IntegerEqual)]
    [InlineData(XACMLFunctionIds.DoubleEqual)]
    [InlineData(XACMLFunctionIds.IntegerGreaterThan)]
    [InlineData(XACMLFunctionIds.IntegerAdd)]
    [InlineData(XACMLFunctionIds.StringConcatenate)]
    [InlineData(XACMLFunctionIds.And)]
    [InlineData(XACMLFunctionIds.Or)]
    [InlineData(XACMLFunctionIds.Not)]
    [InlineData(XACMLFunctionIds.NOf)]
    [InlineData(XACMLFunctionIds.StringRegexpMatch)]
    public void Constructor_RegistersKnownFunction(string functionId)
    {
        _sut.GetFunction(functionId).ShouldNotBeNull(
            $"Function '{functionId}' should be pre-registered");
    }

    #endregion

    #region GetFunction

    [Fact]
    public void GetFunction_RegisteredFunction_ReturnsFunction()
    {
        var fn = _sut.GetFunction(XACMLFunctionIds.StringEqual);

        fn.ShouldNotBeNull();
        fn!.ReturnType.ShouldBe(XACMLDataTypes.Boolean);
    }

    [Fact]
    public void GetFunction_UnknownFunction_ReturnsNull()
    {
        var fn = _sut.GetFunction("non-existent-function");

        fn.ShouldBeNull();
    }

    [Fact]
    public void GetFunction_NullFunctionId_ThrowsArgumentException()
    {
        var act = () => _sut.GetFunction(null!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void GetFunction_EmptyFunctionId_ThrowsArgumentException()
    {
        var act = () => _sut.GetFunction("");

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Register

    [Fact]
    public void Register_CustomFunction_CanBeRetrieved()
    {
        var customFn = Substitute.For<IXACMLFunction>();
        customFn.ReturnType.Returns(XACMLDataTypes.String);

        _sut.Register("custom-function", customFn);

        _sut.GetFunction("custom-function").ShouldBeSameAs(customFn);
    }

    [Fact]
    public void Register_OverwritesExistingFunction()
    {
        var original = _sut.GetFunction(XACMLFunctionIds.StringEqual);
        var replacement = Substitute.For<IXACMLFunction>();
        replacement.ReturnType.Returns(XACMLDataTypes.Boolean);

        _sut.Register(XACMLFunctionIds.StringEqual, replacement);

        _sut.GetFunction(XACMLFunctionIds.StringEqual).ShouldBeSameAs(replacement);
        _sut.GetFunction(XACMLFunctionIds.StringEqual).ShouldNotBeSameAs(original);
    }

    [Fact]
    public void Register_NullFunctionId_ThrowsArgumentException()
    {
        var fn = Substitute.For<IXACMLFunction>();

        var act = () => _sut.Register(null!, fn);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Register_NullFunction_ThrowsArgumentNullException()
    {
        var act = () => _sut.Register("custom-fn", null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region GetAllFunctionIds

    [Fact]
    public void GetAllFunctionIds_ReturnsAllRegistered()
    {
        var ids = _sut.GetAllFunctionIds();

        ids.ShouldContain(XACMLFunctionIds.StringEqual);
        ids.ShouldContain(XACMLFunctionIds.IntegerAdd);
        ids.ShouldContain(XACMLFunctionIds.And);
    }

    [Fact]
    public void GetAllFunctionIds_ReturnsSortedList()
    {
        var ids = _sut.GetAllFunctionIds();

        ids.ShouldBe(ids.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void GetAllFunctionIds_IncludesCustomFunctions()
    {
        var fn = Substitute.For<IXACMLFunction>();
        _sut.Register("zzz-custom", fn);

        var ids = _sut.GetAllFunctionIds();

        ids.ShouldContain("zzz-custom");
    }

    #endregion
}
