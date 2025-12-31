using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

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
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
    }

    [Fact]
    public void Entity_MustImplementIEquatable()
    {
        _entityType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void Entity_MustHaveIdProperty()
    {
        var idProperty = _entityType.GetProperty("Id");
        idProperty.ShouldNotBeNull();
        idProperty!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void Entity_MustOverrideEquals()
    {
        var equalsMethod = _entityType.GetMethod("Equals", [typeof(object)]);
        equalsMethod.ShouldNotBeNull();
        equalsMethod!.DeclaringType.ShouldBe(_entityType);
    }

    [Fact]
    public void Entity_MustOverrideGetHashCode()
    {
        var hashCodeMethod = _entityType.GetMethod("GetHashCode");
        hashCodeMethod.ShouldNotBeNull();
        hashCodeMethod!.DeclaringType.ShouldBe(_entityType);
    }

    [Fact]
    public void Entity_MustOverrideToString()
    {
        var toStringMethod = _entityType.GetMethod("ToString");
        toStringMethod.ShouldNotBeNull();
        toStringMethod!.DeclaringType.ShouldBe(_entityType);
    }

    [Fact]
    public void Entity_MustHaveEqualityOperators()
    {
        var equalityOp = _entityType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Equality");
        equalityOp.ShouldNotBeNull();

        var inequalityOp = _entityType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "op_Inequality");
        inequalityOp.ShouldNotBeNull();
    }

    [Fact]
    public void Entity_MustBeAbstract()
    {
        _entityType.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    public void Entity_MustHaveProtectedConstructor()
    {
        var constructors = _entityType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        constructors.ShouldContain(c => c.IsFamily || c.IsFamilyOrAssembly);
    }
}
