using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

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
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void ValueObject_MustHaveAbstractGetEqualityComponents()
    {
        var method = _valueObjectType.GetMethod("GetEqualityComponents", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method.Should().NotBeNull();
        method!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void ValueObject_MustOverrideEquals()
    {
        var equalsMethod = _valueObjectType.GetMethod("Equals", [typeof(object)]);
        equalsMethod.Should().NotBeNull();
        equalsMethod!.DeclaringType.Should().Be(_valueObjectType);
    }

    [Fact]
    public void ValueObject_MustOverrideGetHashCode()
    {
        var hashCodeMethod = _valueObjectType.GetMethod("GetHashCode");
        hashCodeMethod.Should().NotBeNull();
        hashCodeMethod!.DeclaringType.Should().Be(_valueObjectType);
    }

    [Fact]
    public void ValueObject_MustHaveEqualityOperators()
    {
        var equalityOp = _valueObjectType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Equality");
        equalityOp.Should().NotBeNull();

        var inequalityOp = _valueObjectType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Inequality");
        inequalityOp.Should().NotBeNull();
    }

    [Fact]
    public void ValueObject_MustBeAbstract()
    {
        _valueObjectType.IsAbstract.Should().BeTrue();
    }

    #endregion

    #region SingleValueObject Contracts

    [Fact]
    public void SingleValueObject_MustInheritFromValueObject()
    {
        _singleValueObjectType.BaseType.Should().Be(_valueObjectType);
    }

    [Fact]
    public void SingleValueObject_MustImplementIComparable()
    {
        _singleValueObjectType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>));
    }

    [Fact]
    public void SingleValueObject_MustHaveValueProperty()
    {
        var valueProperty = _singleValueObjectType.GetProperty("Value");
        valueProperty.Should().NotBeNull();
        valueProperty!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void SingleValueObject_MustHaveImplicitConversion()
    {
        var implicitOp = _singleValueObjectType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Implicit");
        implicitOp.Should().NotBeNull();
    }

    [Fact]
    public void SingleValueObject_MustOverrideToString()
    {
        var toStringMethod = _singleValueObjectType.GetMethod("ToString");
        toStringMethod.Should().NotBeNull();
        toStringMethod!.DeclaringType.Should().Be(_singleValueObjectType);
    }

    [Fact]
    public void SingleValueObject_MustBeAbstract()
    {
        _singleValueObjectType.IsAbstract.Should().BeTrue();
    }

    #endregion
}
