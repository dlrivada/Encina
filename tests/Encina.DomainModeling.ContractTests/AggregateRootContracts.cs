using System.Reflection;
using Encina.DomainModeling;
using FluentAssertions;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying AggregateRoot public API contracts.
/// </summary>
public sealed class AggregateRootContracts
{
    private readonly Type _aggregateRootType = typeof(AggregateRoot<>);
    private readonly Type _auditableAggregateRootType = typeof(AuditableAggregateRoot<>);
    private readonly Type _softDeletableAggregateRootType = typeof(SoftDeletableAggregateRoot<>);

    #region AggregateRoot Contracts

    [Fact]
    public void AggregateRoot_MustInheritFromEntity()
    {
        var baseType = _aggregateRootType.BaseType;
        baseType.Should().NotBeNull();
        baseType!.IsGenericType.Should().BeTrue();
        baseType.GetGenericTypeDefinition().Should().Be(typeof(Entity<>));
    }

    [Fact]
    public void AggregateRoot_MustImplementIAggregateRoot()
    {
        _aggregateRootType.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));
    }

    [Fact]
    public void AggregateRoot_MustHaveDomainEventsProperty()
    {
        var domainEventsProperty = _aggregateRootType.GetProperty("DomainEvents");
        domainEventsProperty.Should().NotBeNull();
        domainEventsProperty!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void AggregateRoot_MustHaveClearDomainEventsMethod()
    {
        var clearMethod = _aggregateRootType.GetMethod("ClearDomainEvents");
        clearMethod.Should().NotBeNull();
        clearMethod!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void AggregateRoot_MustHaveProtectedRaiseDomainEventMethod()
    {
        var raiseMethod = _aggregateRootType.GetMethod("RaiseDomainEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        raiseMethod.Should().NotBeNull();
        raiseMethod!.IsFamily.Should().BeTrue();
    }

    [Fact]
    public void AggregateRoot_MustBeAbstract()
    {
        _aggregateRootType.IsAbstract.Should().BeTrue();
    }

    #endregion

    #region AuditableAggregateRoot Contracts

    [Fact]
    public void AuditableAggregateRoot_MustInheritFromAggregateRoot()
    {
        var baseType = _auditableAggregateRootType.BaseType;
        baseType.Should().NotBeNull();
        baseType!.IsGenericType.Should().BeTrue();
        baseType.GetGenericTypeDefinition().Should().Be(_aggregateRootType);
    }

    [Fact]
    public void AuditableAggregateRoot_MustImplementIAuditable()
    {
        _auditableAggregateRootType.GetInterfaces()
            .Should().Contain(typeof(IAuditable));
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveCreatedAtUtcProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("CreatedAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime>();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveCreatedByProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("CreatedBy");
        property.Should().NotBeNull();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveModifiedAtUtcProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("ModifiedAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime?>();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveModifiedByProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("ModifiedBy");
        property.Should().NotBeNull();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveSetCreatedByMethod()
    {
        var method = _auditableAggregateRootType.GetMethod("SetCreatedBy");
        method.Should().NotBeNull();
        method!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveSetModifiedByMethod()
    {
        var method = _auditableAggregateRootType.GetMethod("SetModifiedBy");
        method.Should().NotBeNull();
        method!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void AuditableAggregateRoot_MustBeAbstract()
    {
        _auditableAggregateRootType.IsAbstract.Should().BeTrue();
    }

    #endregion

    #region SoftDeletableAggregateRoot Contracts

    [Fact]
    public void SoftDeletableAggregateRoot_MustInheritFromAuditableAggregateRoot()
    {
        var baseType = _softDeletableAggregateRootType.BaseType;
        baseType.Should().NotBeNull();
        baseType!.IsGenericType.Should().BeTrue();
        baseType.GetGenericTypeDefinition().Should().Be(_auditableAggregateRootType);
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustImplementISoftDeletable()
    {
        _softDeletableAggregateRootType.GetInterfaces()
            .Should().Contain(typeof(ISoftDeletable));
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveIsDeletedProperty()
    {
        var property = _softDeletableAggregateRootType.GetProperty("IsDeleted");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<bool>();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveDeletedAtUtcProperty()
    {
        var property = _softDeletableAggregateRootType.GetProperty("DeletedAtUtc");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be<DateTime?>();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveDeletedByProperty()
    {
        var property = _softDeletableAggregateRootType.GetProperty("DeletedBy");
        property.Should().NotBeNull();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveDeleteMethod()
    {
        var method = _softDeletableAggregateRootType.GetMethod("Delete");
        method.Should().NotBeNull();
        method!.IsVirtual.Should().BeTrue();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveRestoreMethod()
    {
        var method = _softDeletableAggregateRootType.GetMethod("Restore");
        method.Should().NotBeNull();
        method!.IsVirtual.Should().BeTrue();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustBeAbstract()
    {
        _softDeletableAggregateRootType.IsAbstract.Should().BeTrue();
    }

    #endregion
}
