using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.ContractTests.DomainModeling;

/// <summary>
/// Contract tests verifying that all AggregateRoot variants follow consistent contracts
/// for domain events, concurrency, and equality.
/// </summary>
[Trait("Category", "Contract")]
public sealed class AggregateRootContractTests
{
    #region Test Types

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public TestAggregateRoot(Guid id) : base(id) { }
        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed class TestAuditableAggregateRoot : AuditableAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public TestAuditableAggregateRoot(Guid id) : base(id) { }
        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed class TestSoftDeletableAggregateRoot : SoftDeletableAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public TestSoftDeletableAggregateRoot(Guid id) : base(id) { }
        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(Guid EntityId) : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed record AnotherTestEvent(string Data) : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    #endregion

    #region Domain Events Contract Tests

    [Fact]
    public void Contract_AllAggregateVariants_SupportDomainEvents()
    {
        // Contract: All aggregate variants must support domain events
        var baseAggregate = new TestAggregateRoot(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        var event1 = new TestDomainEvent(baseAggregate.Id);
        var event2 = new TestDomainEvent(auditableAggregate.Id);
        var event3 = new TestDomainEvent(softDeletableAggregate.Id);

        // Act
        baseAggregate.RaiseEvent(event1);
        auditableAggregate.RaiseEvent(event2);
        softDeletableAggregate.RaiseEvent(event3);

        // Assert
        baseAggregate.DomainEvents.Count.ShouldBe(1, "AggregateRoot must support domain events");
        auditableAggregate.DomainEvents.Count.ShouldBe(1, "AuditableAggregateRoot must support domain events");
        softDeletableAggregate.DomainEvents.Count.ShouldBe(1, "SoftDeletableAggregateRoot must support domain events");

        baseAggregate.DomainEvents.ShouldContain(event1);
        auditableAggregate.DomainEvents.ShouldContain(event2);
        softDeletableAggregate.DomainEvents.ShouldContain(event3);
    }

    [Fact]
    public void Contract_AllAggregateVariants_ImplementIAggregateRoot()
    {
        // Contract: All aggregate variants must implement IAggregateRoot
        typeof(AggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IAggregateRoot), "AggregateRoot must implement IAggregateRoot");

        typeof(AuditableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IAggregateRoot), "AuditableAggregateRoot must implement IAggregateRoot");

        typeof(SoftDeletableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IAggregateRoot), "SoftDeletableAggregateRoot must implement IAggregateRoot");
    }

    [Fact]
    public void Contract_AllAggregateVariants_DomainEventsReturnIReadOnlyCollection()
    {
        // Contract: DomainEvents property must return IReadOnlyCollection<IDomainEvent>
        var baseAggregate = new TestAggregateRoot(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        baseAggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
        auditableAggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
        softDeletableAggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void Contract_AllAggregateVariants_ClearDomainEventsWorks()
    {
        // Contract: ClearDomainEvents must clear all events for all variants
        var baseAggregate = new TestAggregateRoot(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        baseAggregate.RaiseEvent(new TestDomainEvent(baseAggregate.Id));
        auditableAggregate.RaiseEvent(new TestDomainEvent(auditableAggregate.Id));
        softDeletableAggregate.RaiseEvent(new TestDomainEvent(softDeletableAggregate.Id));

        // Act
        baseAggregate.ClearDomainEvents();
        auditableAggregate.ClearDomainEvents();
        softDeletableAggregate.ClearDomainEvents();

        // Assert
        baseAggregate.DomainEvents.ShouldBeEmpty("AggregateRoot ClearDomainEvents must clear all events");
        auditableAggregate.DomainEvents.ShouldBeEmpty("AuditableAggregateRoot ClearDomainEvents must clear all events");
        softDeletableAggregate.DomainEvents.ShouldBeEmpty("SoftDeletableAggregateRoot ClearDomainEvents must clear all events");
    }

    [Fact]
    public void Contract_AllAggregateVariants_CanRaiseMultipleEventTypes()
    {
        // Contract: All variants must be able to raise different event types
        var baseAggregate = new TestAggregateRoot(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        // Act - Raise different event types
        baseAggregate.RaiseEvent(new TestDomainEvent(baseAggregate.Id));
        baseAggregate.RaiseEvent(new AnotherTestEvent("data"));

        auditableAggregate.RaiseEvent(new TestDomainEvent(auditableAggregate.Id));
        auditableAggregate.RaiseEvent(new AnotherTestEvent("data"));

        softDeletableAggregate.RaiseEvent(new TestDomainEvent(softDeletableAggregate.Id));
        softDeletableAggregate.RaiseEvent(new AnotherTestEvent("data"));

        // Assert
        baseAggregate.DomainEvents.Count.ShouldBe(2);
        auditableAggregate.DomainEvents.Count.ShouldBe(2);
        softDeletableAggregate.DomainEvents.Count.ShouldBe(2);

        baseAggregate.DomainEvents.OfType<TestDomainEvent>().Count().ShouldBe(1);
        baseAggregate.DomainEvents.OfType<AnotherTestEvent>().Count().ShouldBe(1);
    }

    #endregion

    #region IConcurrencyAware Contract Tests

    [Fact]
    public void Contract_AllAggregateVariants_ImplementIConcurrencyAware()
    {
        // Contract: All aggregate variants must implement IConcurrencyAware
        typeof(AggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IConcurrencyAware), "AggregateRoot must implement IConcurrencyAware");

        typeof(AuditableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IConcurrencyAware), "AuditableAggregateRoot must implement IConcurrencyAware");

        typeof(SoftDeletableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IConcurrencyAware), "SoftDeletableAggregateRoot must implement IConcurrencyAware");
    }

    [Fact]
    public void Contract_AllAggregateVariants_HaveRowVersionProperty()
    {
        // Contract: All aggregate variants must have RowVersion property
        var baseAggregate = new TestAggregateRoot(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        // All should have RowVersion property accessible
        var rowVersion = new byte[] { 0x01, 0x02, 0x03 };

        baseAggregate.RowVersion = rowVersion;
        auditableAggregate.RowVersion = rowVersion;
        softDeletableAggregate.RowVersion = rowVersion;

        baseAggregate.RowVersion.ShouldBe(rowVersion, "AggregateRoot must have settable RowVersion");
        auditableAggregate.RowVersion.ShouldBe(rowVersion, "AuditableAggregateRoot must have settable RowVersion");
        softDeletableAggregate.RowVersion.ShouldBe(rowVersion, "SoftDeletableAggregateRoot must have settable RowVersion");
    }

    [Fact]
    public void Contract_AllAggregateVariants_RowVersionDefaultsToNull()
    {
        // Contract: RowVersion must default to null for all variants
        var baseAggregate = new TestAggregateRoot(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        baseAggregate.RowVersion.ShouldBeNull("AggregateRoot RowVersion must default to null");
        auditableAggregate.RowVersion.ShouldBeNull("AuditableAggregateRoot RowVersion must default to null");
        softDeletableAggregate.RowVersion.ShouldBeNull("SoftDeletableAggregateRoot RowVersion must default to null");
    }

    [Fact]
    public void Contract_RowVersionProperty_HasCorrectSignature()
    {
        // Contract: RowVersion property must be byte[]? with get and set
        var rowVersionProperty = typeof(AggregateRoot<Guid>).GetProperty("RowVersion");

        rowVersionProperty.ShouldNotBeNull("AggregateRoot must have RowVersion property");
        rowVersionProperty!.PropertyType.ShouldBe(typeof(byte[]), "RowVersion must be byte[]");
        rowVersionProperty.CanRead.ShouldBeTrue("RowVersion must have getter");
        rowVersionProperty.CanWrite.ShouldBeTrue("RowVersion must have setter");
    }

    #endregion

    #region Equality Contract Tests

    [Fact]
    public void Contract_AllAggregateVariants_EqualityByIdOnly()
    {
        // Contract: Equality must be determined by Id only, not by other properties
        var id = Guid.NewGuid();

        var aggregate1 = new TestAggregateRoot(id) { Name = "Name1" };
        var aggregate2 = new TestAggregateRoot(id) { Name = "Name2" };

        // Despite different names, should be equal
        aggregate1.ShouldBe(aggregate2, "Aggregates with same Id must be equal");
        (aggregate1 == aggregate2).ShouldBeTrue();
    }

    [Fact]
    public void Contract_AllAggregateVariants_EqualityIgnoresDomainEvents()
    {
        // Contract: Domain events should not affect equality
        var id = Guid.NewGuid();

        var aggregate1 = new TestAggregateRoot(id);
        var aggregate2 = new TestAggregateRoot(id);

        aggregate1.RaiseEvent(new TestDomainEvent(aggregate1.Id));
        // aggregate2 has no events

        aggregate1.ShouldBe(aggregate2, "Domain events must not affect equality");
    }

    [Fact]
    public void Contract_AllAggregateVariants_EqualityIgnoresRowVersion()
    {
        // Contract: RowVersion should not affect equality
        var id = Guid.NewGuid();

        var aggregate1 = new TestAggregateRoot(id) { RowVersion = [0x01] };
        var aggregate2 = new TestAggregateRoot(id) { RowVersion = [0x02] };

        aggregate1.ShouldBe(aggregate2, "RowVersion must not affect equality");
    }

    [Fact]
    public void Contract_DifferentAggregateTypes_NotEqualEvenWithSameId()
    {
        // Contract: Different aggregate types should not be equal even with same Id
        var id = Guid.NewGuid();

        var baseAggregate = new TestAggregateRoot(id);
        var auditableAggregate = new TestAuditableAggregateRoot(id);

        baseAggregate.Equals(auditableAggregate).ShouldBeFalse(
            "Different aggregate types must not be equal even with same Id");
    }

    #endregion

    #region IAggregateRoot<TId> Contract Tests

    [Fact]
    public void Contract_AllAggregateVariants_ImplementGenericInterface()
    {
        // Contract: All aggregate variants must implement IAggregateRoot<TId>
        typeof(AggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>),
            "AggregateRoot must implement IAggregateRoot<TId>");

        typeof(AuditableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>),
            "AuditableAggregateRoot must implement IAggregateRoot<TId>");

        typeof(SoftDeletableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>),
            "SoftDeletableAggregateRoot must implement IAggregateRoot<TId>");
    }

    #endregion

    #region Inheritance Hierarchy Contract Tests

    [Fact]
    public void Contract_InheritanceHierarchy_IsCorrect()
    {
        // Contract: Verify the inheritance hierarchy
        typeof(AggregateRoot<Guid>).BaseType.ShouldBe(typeof(Entity<Guid>),
            "AggregateRoot must inherit from Entity");

        typeof(AuditableAggregateRoot<Guid>).BaseType.ShouldBe(typeof(AggregateRoot<Guid>),
            "AuditableAggregateRoot must inherit from AggregateRoot");

        typeof(SoftDeletableAggregateRoot<Guid>).BaseType.ShouldBe(typeof(AuditableAggregateRoot<Guid>),
            "SoftDeletableAggregateRoot must inherit from AuditableAggregateRoot");
    }

    [Fact]
    public void Contract_Entity_HasDomainEventsCapability()
    {
        // Contract: Entity<TId> base class must provide domain events capability
        var entityType = typeof(Entity<Guid>);

        // Must have DomainEvents property
        var domainEventsProperty = entityType.GetProperty("DomainEvents");
        domainEventsProperty.ShouldNotBeNull("Entity must have DomainEvents property");

        // Must have AddDomainEvent method (protected)
        var addMethod = entityType.GetMethod("AddDomainEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        addMethod.ShouldNotBeNull("Entity must have AddDomainEvent method");

        // Must have RemoveDomainEvent method
        var removeMethod = entityType.GetMethod("RemoveDomainEvent");
        removeMethod.ShouldNotBeNull("Entity must have RemoveDomainEvent method");

        // Must have ClearDomainEvents method
        var clearMethod = entityType.GetMethod("ClearDomainEvents");
        clearMethod.ShouldNotBeNull("Entity must have ClearDomainEvents method");
    }

    #endregion

    #region Special-Purpose Aggregate Contract Tests

    [Fact]
    public void Contract_AuditableAggregateRoot_HasAuditProperties()
    {
        // Contract: AuditableAggregateRoot must have audit properties
        var auditableAggregate = new TestAuditableAggregateRoot(Guid.NewGuid());

        // Verify properties exist and are accessible
        auditableAggregate.CreatedAtUtc.ShouldBeGreaterThan(DateTime.MinValue);
        auditableAggregate.CreatedBy.ShouldBeNull(); // Default
        auditableAggregate.ModifiedAtUtc.ShouldBeNull(); // Default
        auditableAggregate.ModifiedBy.ShouldBeNull(); // Default

        // Must implement IAuditable
        typeof(AuditableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(IAuditable), "AuditableAggregateRoot must implement IAuditable");
    }

    [Fact]
    public void Contract_SoftDeletableAggregateRoot_HasSoftDeleteProperties()
    {
        // Contract: SoftDeletableAggregateRoot must have soft delete properties
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        // Verify properties exist and have correct defaults
        softDeletableAggregate.IsDeleted.ShouldBeFalse();
        softDeletableAggregate.DeletedAtUtc.ShouldBeNull();
        softDeletableAggregate.DeletedBy.ShouldBeNull();

        // Must implement ISoftDeletable
        typeof(SoftDeletableAggregateRoot<Guid>).GetInterfaces()
            .ShouldContain(typeof(ISoftDeletable), "SoftDeletableAggregateRoot must implement ISoftDeletable");
    }

    [Fact]
    public void Contract_SoftDeletableAggregateRoot_HasDeleteAndRestoreMethods()
    {
        // Contract: SoftDeletableAggregateRoot must have Delete and Restore methods
        var softDeletableAggregate = new TestSoftDeletableAggregateRoot(Guid.NewGuid());

        // Delete
        softDeletableAggregate.Delete("user1");
        softDeletableAggregate.IsDeleted.ShouldBeTrue();
        softDeletableAggregate.DeletedBy.ShouldBe("user1");
        softDeletableAggregate.DeletedAtUtc.ShouldNotBeNull();

        // Restore
        softDeletableAggregate.Restore();
        softDeletableAggregate.IsDeleted.ShouldBeFalse();
        softDeletableAggregate.DeletedBy.ShouldBeNull();
        softDeletableAggregate.DeletedAtUtc.ShouldBeNull();
    }

    #endregion

    #region CopyEventsFrom Contract Tests

    [Fact]
    public void Contract_AllAggregateVariants_SupportCopyEventsFrom()
    {
        // Contract: All aggregate variants must support CopyEventsFrom via IAggregateRoot
        var event1 = new TestDomainEvent(Guid.NewGuid());
        var event2 = new AnotherTestEvent("test");

        // Test base AggregateRoot
        var sourceBase = new TestAggregateRoot(Guid.NewGuid());
        sourceBase.RaiseEvent(event1);
        sourceBase.RaiseEvent(event2);

        var targetBase = new TestAggregateRoot(Guid.NewGuid());
        ((IAggregateRoot)targetBase).CopyEventsFrom(sourceBase);
        targetBase.DomainEvents.Count.ShouldBe(2, "AggregateRoot must support CopyEventsFrom");

        // Test AuditableAggregateRoot
        var sourceAuditable = new TestAuditableAggregateRoot(Guid.NewGuid());
        sourceAuditable.RaiseEvent(event1);
        sourceAuditable.RaiseEvent(event2);

        var targetAuditable = new TestAuditableAggregateRoot(Guid.NewGuid());
        ((IAggregateRoot)targetAuditable).CopyEventsFrom(sourceAuditable);
        targetAuditable.DomainEvents.Count.ShouldBe(2, "AuditableAggregateRoot must support CopyEventsFrom");

        // Test SoftDeletableAggregateRoot
        var sourceSoftDeletable = new TestSoftDeletableAggregateRoot(Guid.NewGuid());
        sourceSoftDeletable.RaiseEvent(event1);
        sourceSoftDeletable.RaiseEvent(event2);

        var targetSoftDeletable = new TestSoftDeletableAggregateRoot(Guid.NewGuid());
        ((IAggregateRoot)targetSoftDeletable).CopyEventsFrom(sourceSoftDeletable);
        targetSoftDeletable.DomainEvents.Count.ShouldBe(2, "SoftDeletableAggregateRoot must support CopyEventsFrom");
    }

    [Fact]
    public void Contract_CopyEventsFrom_AppendsToExistingEvents()
    {
        // Contract: CopyEventsFrom must append events, not replace them
        var event1 = new TestDomainEvent(Guid.NewGuid());
        var event2 = new AnotherTestEvent("source event");
        var existingEvent = new AnotherTestEvent("existing event");

        var source = new TestAggregateRoot(Guid.NewGuid());
        source.RaiseEvent(event1);
        source.RaiseEvent(event2);

        var target = new TestAggregateRoot(Guid.NewGuid());
        target.RaiseEvent(existingEvent); // Existing event

        ((IAggregateRoot)target).CopyEventsFrom(source);

        target.DomainEvents.Count.ShouldBe(3, "CopyEventsFrom must append to existing events");
        target.DomainEvents.ShouldContain(existingEvent, "Existing events must be preserved");
        target.DomainEvents.ShouldContain(event1, "Copied events must be present");
        target.DomainEvents.ShouldContain(event2, "Copied events must be present");
    }

    [Fact]
    public void Contract_CopyEventsFrom_PreservesEventOrder()
    {
        // Contract: CopyEventsFrom must preserve event order
        var events = Enumerable.Range(0, 5)
            .Select(i => new TestDomainEvent(Guid.NewGuid()))
            .ToList();

        var source = new TestAggregateRoot(Guid.NewGuid());
        foreach (var e in events)
        {
            source.RaiseEvent(e);
        }

        var target = new TestAggregateRoot(Guid.NewGuid());
        ((IAggregateRoot)target).CopyEventsFrom(source);

        var targetEvents = target.DomainEvents.ToList();
        for (int i = 0; i < events.Count; i++)
        {
            targetEvents[i].ShouldBe(events[i], $"Event at index {i} must maintain order");
        }
    }

    [Fact]
    public void Contract_CopyEventsFrom_DoesNotAffectSource()
    {
        // Contract: CopyEventsFrom must not modify source aggregate events
        var event1 = new TestDomainEvent(Guid.NewGuid());
        var event2 = new AnotherTestEvent("test");

        var source = new TestAggregateRoot(Guid.NewGuid());
        source.RaiseEvent(event1);
        source.RaiseEvent(event2);
        var originalCount = source.DomainEvents.Count;

        var target = new TestAggregateRoot(Guid.NewGuid());
        ((IAggregateRoot)target).CopyEventsFrom(source);

        // Clear target events
        target.ClearDomainEvents();

        // Source should still have its events
        source.DomainEvents.Count.ShouldBe(originalCount, "Source events must not be affected by target operations");
    }

    [Fact]
    public void Contract_IAggregateRoot_HasCopyEventsFromMethod()
    {
        // Contract: IAggregateRoot interface must define CopyEventsFrom method
        var interfaceType = typeof(IAggregateRoot);

        var copyEventsFromMethod = interfaceType.GetMethod("CopyEventsFrom");
        copyEventsFromMethod.ShouldNotBeNull("IAggregateRoot must have CopyEventsFrom method");

        var parameters = copyEventsFromMethod!.GetParameters();
        parameters.Length.ShouldBe(1, "CopyEventsFrom must have exactly one parameter");
        parameters[0].ParameterType.ShouldBe(typeof(IAggregateRoot), "CopyEventsFrom parameter must be IAggregateRoot");
    }

    #endregion

    #region Thread Safety Contract Tests

    [Fact]
    public void Contract_DomainEvents_ThreadSafeAccess()
    {
        // Contract: DomainEvents should be accessible safely (though not necessarily thread-safe writes)
        var aggregate = new TestAggregateRoot(Guid.NewGuid());

        // Pre-populate events
        for (int i = 0; i < 10; i++)
        {
            aggregate.RaiseEvent(new TestDomainEvent(aggregate.Id));
        }

        // Multiple reads should be safe
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => aggregate.DomainEvents.Count))
            .ToArray();

        Task.WaitAll(tasks);

        foreach (var task in tasks)
        {
            task.Result.ShouldBe(10, "Concurrent reads should return consistent count");
        }
    }

    #endregion
}
