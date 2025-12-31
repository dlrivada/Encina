using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

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
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(typeof(Entity<>));
    }

    [Fact]
    public void AggregateRoot_MustImplementIAggregateRoot()
    {
        _aggregateRootType.GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));
    }

    [Fact]
    public void AggregateRoot_MustHaveDomainEventsProperty()
    {
        var domainEventsProperty = _aggregateRootType.GetProperty("DomainEvents");
        domainEventsProperty.ShouldNotBeNull();
        domainEventsProperty!.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void AggregateRoot_MustHaveClearDomainEventsMethod()
    {
        var clearMethod = _aggregateRootType.GetMethod("ClearDomainEvents");
        clearMethod.ShouldNotBeNull();
        clearMethod!.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public void AggregateRoot_MustHaveProtectedRaiseDomainEventMethod()
    {
        var raiseMethod = _aggregateRootType.GetMethod("RaiseDomainEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        raiseMethod.ShouldNotBeNull();
        raiseMethod!.IsFamily.ShouldBeTrue();
    }

    [Fact]
    public void AggregateRoot_MustBeAbstract()
    {
        _aggregateRootType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region AuditableAggregateRoot Contracts

    [Fact]
    public void AuditableAggregateRoot_MustInheritFromAggregateRoot()
    {
        var baseType = _auditableAggregateRootType.BaseType;
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(_aggregateRootType);
    }

    [Fact]
    public void AuditableAggregateRoot_MustImplementIAuditable()
    {
        _auditableAggregateRootType.GetInterfaces()
            .ShouldContain(typeof(IAuditable));
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveCreatedAtUtcProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("CreatedAtUtc");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTime));
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveCreatedByProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("CreatedBy");
        property.ShouldNotBeNull();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveModifiedAtUtcProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("ModifiedAtUtc");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTime?));
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveModifiedByProperty()
    {
        var property = _auditableAggregateRootType.GetProperty("ModifiedBy");
        property.ShouldNotBeNull();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveSetCreatedByMethod()
    {
        var method = _auditableAggregateRootType.GetMethod("SetCreatedBy");
        method.ShouldNotBeNull();
        method!.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public void AuditableAggregateRoot_MustHaveSetModifiedByMethod()
    {
        var method = _auditableAggregateRootType.GetMethod("SetModifiedBy");
        method.ShouldNotBeNull();
        method!.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public void AuditableAggregateRoot_MustBeAbstract()
    {
        _auditableAggregateRootType.IsAbstract.ShouldBeTrue();
    }

    #endregion

    #region SoftDeletableAggregateRoot Contracts

    [Fact]
    public void SoftDeletableAggregateRoot_MustInheritFromAuditableAggregateRoot()
    {
        var baseType = _softDeletableAggregateRootType.BaseType;
        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().ShouldBe(_auditableAggregateRootType);
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustImplementISoftDeletable()
    {
        _softDeletableAggregateRootType.GetInterfaces()
            .ShouldContain(typeof(ISoftDeletable));
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveIsDeletedProperty()
    {
        var property = _softDeletableAggregateRootType.GetProperty("IsDeleted");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveDeletedAtUtcProperty()
    {
        var property = _softDeletableAggregateRootType.GetProperty("DeletedAtUtc");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTime?));
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveDeletedByProperty()
    {
        var property = _softDeletableAggregateRootType.GetProperty("DeletedBy");
        property.ShouldNotBeNull();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveDeleteMethod()
    {
        var method = _softDeletableAggregateRootType.GetMethod("Delete");
        method.ShouldNotBeNull();
        method!.IsVirtual.ShouldBeTrue();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustHaveRestoreMethod()
    {
        var method = _softDeletableAggregateRootType.GetMethod("Restore");
        method.ShouldNotBeNull();
        method!.IsVirtual.ShouldBeTrue();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_MustBeAbstract()
    {
        _softDeletableAggregateRootType.IsAbstract.ShouldBeTrue();
    }

    #endregion
}
