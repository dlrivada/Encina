using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying StronglyTypedId public API contracts.
/// </summary>
public sealed class StronglyTypedIdContracts
{
    private readonly Type _stronglyTypedIdType = typeof(StronglyTypedId<>);
    private readonly Type _guidStronglyTypedIdType = typeof(GuidStronglyTypedId<>);
    private readonly Type _intStronglyTypedIdType = typeof(IntStronglyTypedId<>);
    private readonly Type _longStronglyTypedIdType = typeof(LongStronglyTypedId<>);
    private readonly Type _stringStronglyTypedIdType = typeof(StringStronglyTypedId<>);

    #region Base StronglyTypedId Contracts

    [Fact]
    public void StronglyTypedId_MustImplementIStronglyTypedId()
    {
        _stronglyTypedIdType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>));
    }

    [Fact]
    public void StronglyTypedId_MustImplementIEquatable()
    {
        _stronglyTypedIdType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void StronglyTypedId_MustImplementIComparable()
    {
        _stronglyTypedIdType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>));
    }

    [Fact]
    public void StronglyTypedId_MustHaveValueProperty()
    {
        var valueProperty = _stronglyTypedIdType.GetProperty("Value");
        valueProperty.ShouldNotBeNull();
        valueProperty!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void StronglyTypedId_MustHaveImplicitConversion()
    {
        var implicitOp = _stronglyTypedIdType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Implicit");
        implicitOp.ShouldNotBeNull();
    }

    [Fact]
    public void StronglyTypedId_MustHaveEqualityOperators()
    {
        var equalityOp = _stronglyTypedIdType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Equality");
        equalityOp.ShouldNotBeNull();

        var inequalityOp = _stronglyTypedIdType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Inequality");
        inequalityOp.ShouldNotBeNull();
    }

    [Fact]
    public void StronglyTypedId_MustBeAbstract()
    {
        _stronglyTypedIdType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region GuidStronglyTypedId Contracts

    [Fact]
    public void GuidStronglyTypedId_MustInheritFromStronglyTypedId()
    {
        var baseType = _guidStronglyTypedIdType.BaseType;
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(_stronglyTypedIdType);
    }

    [Fact]
    public void GuidStronglyTypedId_MustHaveNewMethod()
    {
        var newMethod = _guidStronglyTypedIdType.GetMethod("New", BindingFlags.Static | BindingFlags.Public);
        newMethod.ShouldNotBeNull();
    }

    [Fact]
    public void GuidStronglyTypedId_MustHaveFromMethod()
    {
        var fromMethod = _guidStronglyTypedIdType.GetMethod("From", BindingFlags.Static | BindingFlags.Public);
        fromMethod.ShouldNotBeNull();
    }

    [Fact]
    public void GuidStronglyTypedId_MustHaveTryParseMethod()
    {
        var tryParseMethod = _guidStronglyTypedIdType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public);
        tryParseMethod.ShouldNotBeNull();
    }

    [Fact]
    public void GuidStronglyTypedId_MustHaveEmptyProperty()
    {
        var emptyProperty = _guidStronglyTypedIdType.GetProperty("Empty", BindingFlags.Static | BindingFlags.Public);
        emptyProperty.ShouldNotBeNull();
    }

    [Fact]
    public void GuidStronglyTypedId_MustBeAbstract()
    {
        _guidStronglyTypedIdType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region IntStronglyTypedId Contracts

    [Fact]
    public void IntStronglyTypedId_MustInheritFromStronglyTypedId()
    {
        var baseType = _intStronglyTypedIdType.BaseType;
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(_stronglyTypedIdType);
    }

    [Fact]
    public void IntStronglyTypedId_MustHaveFromMethod()
    {
        var fromMethod = _intStronglyTypedIdType.GetMethod("From", BindingFlags.Static | BindingFlags.Public);
        fromMethod.ShouldNotBeNull();
    }

    [Fact]
    public void IntStronglyTypedId_MustHaveTryParseMethod()
    {
        var tryParseMethod = _intStronglyTypedIdType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public);
        tryParseMethod.ShouldNotBeNull();
    }

    [Fact]
    public void IntStronglyTypedId_MustBeAbstract()
    {
        _intStronglyTypedIdType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region LongStronglyTypedId Contracts

    [Fact]
    public void LongStronglyTypedId_MustInheritFromStronglyTypedId()
    {
        var baseType = _longStronglyTypedIdType.BaseType;
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(_stronglyTypedIdType);
    }

    [Fact]
    public void LongStronglyTypedId_MustHaveFromMethod()
    {
        var fromMethod = _longStronglyTypedIdType.GetMethod("From", BindingFlags.Static | BindingFlags.Public);
        fromMethod.ShouldNotBeNull();
    }

    [Fact]
    public void LongStronglyTypedId_MustHaveTryParseMethod()
    {
        var tryParseMethod = _longStronglyTypedIdType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public);
        tryParseMethod.ShouldNotBeNull();
    }

    [Fact]
    public void LongStronglyTypedId_MustBeAbstract()
    {
        _longStronglyTypedIdType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region StringStronglyTypedId Contracts

    [Fact]
    public void StringStronglyTypedId_MustInheritFromStronglyTypedId()
    {
        var baseType = _stringStronglyTypedIdType.BaseType;
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(_stronglyTypedIdType);
    }

    [Fact]
    public void StringStronglyTypedId_MustHaveFromMethod()
    {
        var fromMethod = _stringStronglyTypedIdType.GetMethod("From", BindingFlags.Static | BindingFlags.Public);
        fromMethod.ShouldNotBeNull();
    }

    [Fact]
    public void StringStronglyTypedId_MustBeAbstract()
    {
        _stringStronglyTypedIdType.IsAbstract.ShouldBeTrue();
    }

    #endregion
}
