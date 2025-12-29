using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying Business Rule public API contract.
/// </summary>
public sealed class BusinessRuleContracts
{
    private readonly Type _interfaceType = typeof(IBusinessRule);
    private readonly Type _abstractType = typeof(BusinessRule);
    private readonly Type _exceptionType = typeof(BusinessRuleViolationException);
    private readonly Type _errorType = typeof(BusinessRuleError);
    private readonly Type _aggregateErrorType = typeof(AggregateBusinessRuleError);

    #region IBusinessRule Contracts

    [Fact]
    public void IBusinessRule_MustBeInterface()
    {
        _interfaceType.IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IBusinessRule_MustHaveErrorCodeProperty()
    {
        var property = _interfaceType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void IBusinessRule_MustHaveErrorMessageProperty()
    {
        var property = _interfaceType.GetProperty("ErrorMessage");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void IBusinessRule_MustHaveIsSatisfiedMethod()
    {
        var method = _interfaceType.GetMethod("IsSatisfied");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<bool>();
    }

    #endregion

    #region BusinessRule Abstract Class Contracts

    [Fact]
    public void BusinessRule_MustBeAbstract()
    {
        _abstractType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void BusinessRule_MustImplementIBusinessRule()
    {
        _abstractType.GetInterfaces().Should().Contain(_interfaceType);
    }

    [Fact]
    public void BusinessRule_MustHaveAbstractErrorCodeProperty()
    {
        var property = _abstractType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.GetMethod!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void BusinessRule_MustHaveAbstractErrorMessageProperty()
    {
        var property = _abstractType.GetProperty("ErrorMessage");
        property.Should().NotBeNull();
        property!.GetMethod!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void BusinessRule_MustHaveAbstractIsSatisfiedMethod()
    {
        var method = _abstractType.GetMethod("IsSatisfied");
        method.Should().NotBeNull();
        method!.IsAbstract.Should().BeTrue();
    }

    #endregion

    #region BusinessRuleViolationException Contracts

    [Fact]
    public void BusinessRuleViolationException_MustExtendException()
    {
        _exceptionType.BaseType.Should().Be<Exception>();
    }

    [Fact]
    public void BusinessRuleViolationException_MustHaveBrokenRuleProperty()
    {
        var property = _exceptionType.GetProperty("BrokenRule");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<IBusinessRule>();
    }

    [Fact]
    public void BusinessRuleViolationException_MustHaveConstructorWithRule()
    {
        var ctor = _exceptionType.GetConstructor([_interfaceType]);
        ctor.Should().NotBeNull();
    }

    [Fact]
    public void BusinessRuleViolationException_MustHaveConstructorWithRuleAndInner()
    {
        var ctor = _exceptionType.GetConstructor([_interfaceType, typeof(Exception)]);
        ctor.Should().NotBeNull();
    }

    #endregion

    #region BusinessRuleError Contracts

    [Fact]
    public void BusinessRuleError_MustBeRecord()
    {
        // Records implement IEquatable<T>
        _errorType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void BusinessRuleError_MustHaveErrorCodeProperty()
    {
        var property = _errorType.GetProperty("ErrorCode");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void BusinessRuleError_MustHaveErrorMessageProperty()
    {
        var property = _errorType.GetProperty("ErrorMessage");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<string>();
    }

    [Fact]
    public void BusinessRuleError_MustHaveFromFactoryMethod()
    {
        var method = _errorType.GetMethod("From", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<BusinessRuleError>();
    }

    #endregion

    #region AggregateBusinessRuleError Contracts

    [Fact]
    public void AggregateBusinessRuleError_MustBeRecord()
    {
        _aggregateErrorType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void AggregateBusinessRuleError_MustHaveErrorsProperty()
    {
        var property = _aggregateErrorType.GetProperty("Errors");
        property.Should().NotBeNull();
    }

    [Fact]
    public void AggregateBusinessRuleError_MustHaveFromRulesFactoryMethod()
    {
        var method = _aggregateErrorType.GetMethod("FromRules", BindingFlags.Static | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<AggregateBusinessRuleError>();
    }

    [Fact]
    public void AggregateBusinessRuleError_MustHaveErrorsCollectionOfCorrectType()
    {
        var property = _aggregateErrorType.GetProperty("Errors");
        property.Should().NotBeNull();
        // Errors should be a collection of BusinessRuleError
        var returnType = property!.PropertyType;
        returnType.IsAssignableTo(typeof(System.Collections.IEnumerable)).Should().BeTrue();
    }

    #endregion

    #region BusinessRuleExtensions Contracts

    [Fact]
    public void BusinessRuleExtensions_MustExist()
    {
        var extensionsType = typeof(BusinessRuleExtensions);
        extensionsType.Should().NotBeNull();
        extensionsType.IsAbstract.Should().BeTrue();
        extensionsType.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void BusinessRuleExtensions_MustHaveCheckMethod()
    {
        var method = typeof(BusinessRuleExtensions).GetMethod("Check");
        method.Should().NotBeNull();
    }

    [Fact]
    public void BusinessRuleExtensions_MustHaveCheckFirstMethod()
    {
        var method = typeof(BusinessRuleExtensions).GetMethod("CheckFirst");
        method.Should().NotBeNull();
    }

    [Fact]
    public void BusinessRuleExtensions_MustHaveCheckAllMethod()
    {
        var method = typeof(BusinessRuleExtensions).GetMethod("CheckAll");
        method.Should().NotBeNull();
    }

    [Fact]
    public void BusinessRuleExtensions_MustHaveThrowIfNotSatisfiedMethod()
    {
        var method = typeof(BusinessRuleExtensions).GetMethod("ThrowIfNotSatisfied");
        method.Should().NotBeNull();
    }

    [Fact]
    public void BusinessRuleExtensions_MustHaveThrowIfAnyNotSatisfiedMethod()
    {
        var method = typeof(BusinessRuleExtensions).GetMethod("ThrowIfAnyNotSatisfied");
        method.Should().NotBeNull();
    }

    #endregion
}
