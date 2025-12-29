using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying Entity<TId> public API contract.
/// </summary>
public sealed class EntityContracts
{
    private readonly Type _entityType = typeof(Entity<>);

    [Fact]
    public void Entity_MustImplementIEntity()
    {
        _entityType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
    }

    [Fact]
    public void Entity_MustImplementIEquatable()
    {
        _entityType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void Entity_MustHaveIdProperty()
    {
        var idProperty = _entityType.GetProperty("Id");
        idProperty.Should().NotBeNull();
        idProperty!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void Entity_MustOverrideEquals()
    {
        var equalsMethod = _entityType.GetMethod("Equals", [typeof(object)]);
        equalsMethod.Should().NotBeNull();
        equalsMethod!.DeclaringType.Should().Be(_entityType);
    }

    [Fact]
    public void Entity_MustOverrideGetHashCode()
    {
        var hashCodeMethod = _entityType.GetMethod("GetHashCode");
        hashCodeMethod.Should().NotBeNull();
        hashCodeMethod!.DeclaringType.Should().Be(_entityType);
    }

    [Fact]
    public void Entity_MustOverrideToString()
    {
        var toStringMethod = _entityType.GetMethod("ToString");
        toStringMethod.Should().NotBeNull();
        toStringMethod!.DeclaringType.Should().Be(_entityType);
    }

    [Fact]
    public void Entity_MustHaveEqualityOperators()
    {
        var equalityOp = _entityType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Equality");
        equalityOp.Should().NotBeNull();

        var inequalityOp = _entityType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Inequality");
        inequalityOp.Should().NotBeNull();
    }

    [Fact]
    public void Entity_MustBeAbstract()
    {
        _entityType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Entity_MustHaveProtectedConstructor()
    {
        var constructors = _entityType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        constructors.Should().Contain(c => c.IsFamily || c.IsFamilyOrAssembly);
    }
}
