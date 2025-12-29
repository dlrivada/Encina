using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying Anti-Corruption Layer public API contract.
/// </summary>
public sealed class AntiCorruptionLayerContracts
{
    private readonly Type _interfaceType = typeof(IAntiCorruptionLayer<,>);
    private readonly Type _asyncInterfaceType = typeof(IAsyncAntiCorruptionLayer<,>);
    private readonly Type _baseType = typeof(AntiCorruptionLayerBase<,>);
    private readonly Type _errorType = typeof(TranslationError);

    #region IAntiCorruptionLayer Contracts

    [Fact]
    public void IAntiCorruptionLayer_MustBeInterface()
    {
        _interfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IAntiCorruptionLayer_MustHaveTwoTypeParameters()
    {
        _interfaceType.GetGenericArguments().Should().HaveCount(2);
    }

    [Fact]
    public void IAntiCorruptionLayer_MustHaveTranslateToInternalMethod()
    {
        var method = _interfaceType.GetMethod("TranslateToInternal");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IAntiCorruptionLayer_MustHaveTranslateToExternalMethod()
    {
        var method = _interfaceType.GetMethod("TranslateToExternal");
        method.Should().NotBeNull();
    }

    #endregion

    #region IAsyncAntiCorruptionLayer Contracts

    [Fact]
    public void IAsyncAntiCorruptionLayer_MustBeInterface()
    {
        _asyncInterfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IAsyncAntiCorruptionLayer_MustHaveTwoTypeParameters()
    {
        _asyncInterfaceType.GetGenericArguments().Should().HaveCount(2);
    }

    [Fact]
    public void IAsyncAntiCorruptionLayer_MustHaveTranslateToInternalAsyncMethod()
    {
        var method = _asyncInterfaceType.GetMethod("TranslateToInternalAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void IAsyncAntiCorruptionLayer_MustHaveTranslateToExternalAsyncMethod()
    {
        var method = _asyncInterfaceType.GetMethod("TranslateToExternalAsync");
        method.Should().NotBeNull();
    }

    #endregion

    #region AntiCorruptionLayerBase Contracts

    [Fact]
    public void AntiCorruptionLayerBase_MustBeAbstract()
    {
        _baseType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustImplementIAntiCorruptionLayer()
    {
        _baseType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == _interfaceType);
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveExternalSystemIdProperty()
    {
        var property = _baseType.GetProperty("ExternalSystemId", BindingFlags.Instance | BindingFlags.NonPublic);
        property.Should().NotBeNull();
        property!.GetMethod!.IsVirtual.Should().BeTrue();
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveAbstractTranslateToInternal()
    {
        var method = _baseType.GetMethod("TranslateToInternal");
        method.Should().NotBeNull();
        method!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveAbstractTranslateToExternal()
    {
        var method = _baseType.GetMethod("TranslateToExternal");
        method.Should().NotBeNull();
        method!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveProtectedErrorMethod()
    {
        var methods = _baseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "Error" && m.IsFamily);
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveProtectedUnsupportedTypeMethod()
    {
        var methods = _baseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "UnsupportedType" && m.IsFamily);
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveProtectedMissingFieldMethod()
    {
        var methods = _baseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "MissingField" && m.IsFamily);
    }

    [Fact]
    public void AntiCorruptionLayerBase_MustHaveProtectedInvalidFormatMethod()
    {
        var methods = _baseType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        methods.Should().Contain(m => m.Name == "InvalidFormat" && m.IsFamily);
    }

    #endregion

    #region TranslationError Contracts

    [Fact]
    public void TranslationError_MustBeRecord()
    {
        _errorType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void TranslationError_MustBeSealed()
    {
        _errorType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void TranslationError_MustHaveErrorCodeProperty()
    {
        var property = _errorType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void TranslationError_MustHaveErrorMessageProperty()
    {
        var property = _errorType.GetProperty("ErrorMessage");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void TranslationError_MustHaveExternalSystemIdProperty()
    {
        var property = _errorType.GetProperty("ExternalSystemId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void TranslationError_MustHaveUnsupportedTypeFactory()
    {
        var method = _errorType.GetMethod("UnsupportedType", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<TranslationError>();
    }

    [Fact]
    public void TranslationError_MustHaveMissingRequiredFieldFactory()
    {
        var method = _errorType.GetMethod("MissingRequiredField", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<TranslationError>();
    }

    [Fact]
    public void TranslationError_MustHaveInvalidFormatFactory()
    {
        var method = _errorType.GetMethod("InvalidFormat", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<TranslationError>();
    }

    #endregion
}
