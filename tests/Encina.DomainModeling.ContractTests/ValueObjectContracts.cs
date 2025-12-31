using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying ValueObject public API contract.
/// </summary>
public sealed class ValueObjectContracts
{
    private readonly Type _valueObjectType = typeof(ValueObject);
    private readonly Type _singleValueObjectType = typeof(SingleValueObject<>);

    #region ValueObject Contracts

    [Fact]
    public void ValueObject_MustImplementIEquatable()
    {
        _valueObjectType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void ValueObject_MustHaveAbstractGetEqualityComponents()
    {
        var method = _valueObjectType.GetMethod("GetEqualityComponents", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method.ShouldNotBeNull();
        method!.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    public void ValueObject_MustOverrideEquals()
    {
        var equalsMethod = _valueObjectType.GetMethod("Equals", [typeof(object)]);
        equalsMethod.ShouldNotBeNull();
        equalsMethod!.DeclaringType.ShouldBe(_valueObjectType);
    }

    [Fact]
    public void ValueObject_MustOverrideGetHashCode()
    {
        var hashCodeMethod = _valueObjectType.GetMethod("GetHashCode");
        hashCodeMethod.ShouldNotBeNull();
        hashCodeMethod!.DeclaringType.ShouldBe(_valueObjectType);
    }

    [Fact]
    public void ValueObject_MustHaveEqualityOperators()
    {
        var equalityOp = _valueObjectType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Equality");
        equalityOp.ShouldNotBeNull();

        var inequalityOp = _valueObjectType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Inequality");
        inequalityOp.ShouldNotBeNull();
    }

    [Fact]
    public void ValueObject_MustBeAbstract()
    {
        _valueObjectType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region SingleValueObject Contracts

    [Fact]
    public void SingleValueObject_MustInheritFromValueObject()
    {
        _singleValueObjectType.BaseType.ShouldBe(_valueObjectType);
    }

    [Fact]
    public void SingleValueObject_MustImplementIComparable()
    {
        _singleValueObjectType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>));
    }

    [Fact]
    public void SingleValueObject_MustHaveValueProperty()
    {
        var valueProperty = _singleValueObjectType.GetProperty("Value");
        valueProperty.ShouldNotBeNull();
        valueProperty!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void SingleValueObject_MustHaveImplicitConversion()
    {
        var implicitOp = _singleValueObjectType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Implicit");
        implicitOp.ShouldNotBeNull();
    }

    [Fact]
    public void SingleValueObject_MustOverrideToString()
    {
        var toStringMethod = _singleValueObjectType.GetMethod("ToString");
        toStringMethod.ShouldNotBeNull();
        toStringMethod!.DeclaringType.ShouldBe(_singleValueObjectType);
    }

    [Fact]
    public void SingleValueObject_MustBeAbstract()
    {
        _singleValueObjectType.IsAbstract.ShouldBeTrue();
    }

    #endregion
}
