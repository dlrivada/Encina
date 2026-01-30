using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Tests for ImmutableUpdateExtensions methods.
/// </summary>
public class ImmutableUpdateExtensionsTests
{
    #region Test Types

    private sealed record TestNotificationEvent(Guid EntityId) : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        public TestAggregateRoot() : base(Guid.NewGuid()) { }
        public TestAggregateRoot(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    /// <summary>
    /// Aggregate for testing immutable-style updates.
    /// Note: In C#, records cannot inherit from classes, so we use a class with init properties
    /// to simulate immutable record behavior for testing purposes.
    /// </summary>
    private sealed class ImmutableOrder : AggregateRoot<Guid>
    {
        public string CustomerName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;

        public ImmutableOrder() : base(Guid.NewGuid()) { }
        public ImmutableOrder(Guid id) : base(id) { }

        /// <summary>
        /// Simulates immutable update pattern: raises event and creates new instance.
        /// </summary>
        public ImmutableOrder Ship()
        {
            RaiseDomainEvent(new TestNotificationEvent(Id));
            // Simulate with-expression by creating new instance
            return new ImmutableOrder(Id)
            {
                CustomerName = this.CustomerName,
                Status = "Shipped"
            };
        }
    }

    private sealed class SimpleEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ImmutableTestDbContext : DbContext
    {
        public ImmutableTestDbContext(DbContextOptions<ImmutableTestDbContext> options) : base(options) { }

        public DbSet<TestAggregateRoot> Aggregates => Set<TestAggregateRoot>();
        public DbSet<ImmutableOrder> Orders => Set<ImmutableOrder>();
        public DbSet<SimpleEntity> SimpleEntities => Set<SimpleEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregateRoot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Ignore(e => e.DomainEvents);
                entity.Ignore(e => e.RowVersion);
            });

            modelBuilder.Entity<ImmutableOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CustomerName).HasMaxLength(200);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Ignore(e => e.DomainEvents);
                entity.Ignore(e => e.RowVersion);
            });

            modelBuilder.Entity<SimpleEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });
        }
    }

    #endregion

    #region WithPreservedEvents Tests

    [Fact]
    public void WithPreservedEvents_NullNewInstance_ShouldThrowArgumentNullException()
    {
        // Arrange
        var original = new TestAggregateRoot();
        TestAggregateRoot newInstance = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => newInstance.WithPreservedEvents(original));
    }

    [Fact]
    public void WithPreservedEvents_NullOriginalInstance_ShouldThrowArgumentNullException()
    {
        // Arrange
        var newInstance = new TestAggregateRoot();
        TestAggregateRoot original = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => newInstance.WithPreservedEvents(original));
    }

    [Fact]
    public void WithPreservedEvents_ShouldCopyEventsFromOriginal()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var event1 = new TestNotificationEvent(original.Id);
        var event2 = new TestNotificationEvent(original.Id);
        original.RaiseEvent(event1);
        original.RaiseEvent(event2);

        var newInstance = new TestAggregateRoot(original.Id);

        // Act
        var result = newInstance.WithPreservedEvents(original);

        // Assert
        result.ShouldBeSameAs(newInstance);
        result.DomainEvents.Count.ShouldBe(2);
        result.DomainEvents.ShouldContain(event1);
        result.DomainEvents.ShouldContain(event2);
    }

    [Fact]
    public void WithPreservedEvents_WithEmptyOriginal_ShouldNotAddEvents()
    {
        // Arrange
        var original = new TestAggregateRoot(); // No events
        var newInstance = new TestAggregateRoot(original.Id);

        // Act
        var result = newInstance.WithPreservedEvents(original);

        // Assert
        result.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void WithPreservedEvents_ShouldReturnSameInstanceForFluentChaining()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var newInstance = new TestAggregateRoot(original.Id);

        // Act
        var result = newInstance.WithPreservedEvents(original);

        // Assert
        result.ShouldBeSameAs(newInstance);
    }

    [Fact]
    public void WithPreservedEvents_NewInstanceAlreadyHasEvents_ShouldMergeBoth()
    {
        // Arrange
        var original = new TestAggregateRoot();
        var event1 = new TestNotificationEvent(original.Id);
        original.RaiseEvent(event1);

        var newInstance = new TestAggregateRoot(original.Id);
        var event2 = new TestNotificationEvent(newInstance.Id);
        newInstance.RaiseEvent(event2);

        // Act
        var result = newInstance.WithPreservedEvents(original);

        // Assert
        result.DomainEvents.Count.ShouldBe(2);
        result.DomainEvents.ShouldContain(event1);
        result.DomainEvents.ShouldContain(event2);
    }

    #endregion

    #region UpdateImmutable Tests

    [Fact]
    public void UpdateImmutable_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new TestAggregateRoot();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.UpdateImmutable(entity));
    }

    [Fact]
    public void UpdateImmutable_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        TestAggregateRoot entity = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.UpdateImmutable(entity));
    }

    [Fact]
    public void UpdateImmutable_EntityNotTracked_ShouldReturnError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        var entity = new TestAggregateRoot { Name = "Test", Value = 42 };
        // Entity is NOT tracked

        // Act
        var result = context.UpdateImmutable(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
            error.Message.ShouldContain("TestAggregateRoot");
        });
    }

    [Fact]
    public void UpdateImmutable_TrackedEntity_ShouldSucceed()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var original = new TestAggregateRoot(id) { Name = "Original", Value = 10 };
        context.Aggregates.Add(original);
        context.SaveChanges();

        // Now the entity is tracked - create modified version
        var modified = new TestAggregateRoot(id) { Name = "Modified", Value = 20 };

        // Act
        var result = context.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        var entry = context.Entry(modified);
        entry.State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public void UpdateImmutable_ShouldPreserveDomainEvents()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var original = new TestAggregateRoot(id) { Name = "Original", Value = 10 };
        var domainEvent = new TestNotificationEvent(id);
        original.RaiseEvent(domainEvent);

        context.Aggregates.Add(original);
        context.SaveChanges();

        // Create modified version (normally without events)
        var modified = new TestAggregateRoot(id) { Name = "Modified", Value = 20 };

        // Act
        var result = context.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Events should be copied from original to modified
        modified.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void UpdateImmutable_NonAggregateRoot_ShouldSucceedWithoutEventCopying()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var original = new SimpleEntity { Id = id, Name = "Original" };
        context.SimpleEntities.Add(original);
        context.SaveChanges();

        var modified = new SimpleEntity { Id = id, Name = "Modified" };

        // Act
        var result = context.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        context.Entry(modified).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public void UpdateImmutable_ShouldDetachOriginalAndAttachModified()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var original = new TestAggregateRoot(id) { Name = "Original", Value = 10 };
        context.Aggregates.Add(original);
        context.SaveChanges();

        var modified = new TestAggregateRoot(id) { Name = "Modified", Value = 20 };

        // Act
        var result = context.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Original should be detached
        context.Entry(original).State.ShouldBe(EntityState.Detached);

        // Modified should be tracked as modified
        context.Entry(modified).State.ShouldBe(EntityState.Modified);
    }

    #endregion

    #region UpdateImmutableAsync Tests

    [Fact]
    public async Task UpdateImmutableAsync_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new TestAggregateRoot();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            context.UpdateImmutableAsync(entity).AsTask());
    }

    [Fact]
    public async Task UpdateImmutableAsync_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ImmutableTestDbContext(options);
        TestAggregateRoot entity = null!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            context.UpdateImmutableAsync(entity).AsTask());
    }

    [Fact]
    public async Task UpdateImmutableAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ImmutableTestDbContext(options);
        var entity = new TestAggregateRoot();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            context.UpdateImmutableAsync(entity, cts.Token).AsTask());
    }

    [Fact]
    public async Task UpdateImmutableAsync_TrackedEntity_ShouldSucceed()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var original = new TestAggregateRoot(id) { Name = "Original", Value = 10 };
        context.Aggregates.Add(original);
        await context.SaveChangesAsync();

        var modified = new TestAggregateRoot(id) { Name = "Modified", Value = 20 };

        // Act
        var result = await context.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        context.Entry(modified).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public async Task UpdateImmutableAsync_EntityNotTracked_ShouldReturnError()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ImmutableTestDbContext(options);
        var entity = new TestAggregateRoot { Name = "Test", Value = 42 };

        // Act
        var result = await context.UpdateImmutableAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
        });
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void ImmutableUpdateWorkflow_UpdateImmutableAutomaticallyPreservesEvents()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var order = new ImmutableOrder(id) { CustomerName = "John", Status = "Pending" };
        context.Orders.Add(order);
        context.SaveChanges();

        // Act - Ship the order (raises event on original and returns new instance)
        // Note: UpdateImmutable automatically copies events from tracked original,
        // so WithPreservedEvents is NOT needed when using UpdateImmutable
        var shippedOrder = order.Ship();

        // Update the change tracker - this automatically copies events from original
        var result = context.UpdateImmutable(shippedOrder);

        // Assert
        result.IsRight.ShouldBeTrue();
        shippedOrder.Status.ShouldBe("Shipped");
        // Event was copied from original by UpdateImmutable
        shippedOrder.DomainEvents.Count.ShouldBe(1);
        shippedOrder.DomainEvents.First().ShouldBeOfType<TestNotificationEvent>();

        context.Entry(shippedOrder).State.ShouldBe(EntityState.Modified);
        context.Entry(order).State.ShouldBe(EntityState.Detached);
    }

    [Fact]
    public void ImmutableUpdateWorkflow_WithPreservedEventsUsedWithoutUpdateImmutable()
    {
        // Arrange - This shows when to use WithPreservedEvents: when NOT using UpdateImmutable
        var original = new TestAggregateRoot();
        var event1 = new TestNotificationEvent(original.Id);
        original.RaiseEvent(event1);

        // Create a "modified" instance (simulating with-expression result)
        var modified = new TestAggregateRoot(original.Id) { Name = "Modified", Value = 100 };

        // Act - WithPreservedEvents is useful when you need events on the new instance
        // without going through UpdateImmutable (e.g., for manual change tracking)
        var result = modified.WithPreservedEvents(original);

        // Assert
        result.ShouldBeSameAs(modified);
        result.DomainEvents.Count.ShouldBe(1);
        result.DomainEvents.ShouldContain(event1);
    }

    [Fact]
    public async Task ImmutableUpdateWorkflow_FullSaveCycle_ShouldPersistChanges()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ImmutableTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ImmutableTestDbContext(options);
        var id = Guid.NewGuid();
        var order = new ImmutableOrder(id) { CustomerName = "John", Status = "Pending" };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act - Ship and update (UpdateImmutable handles event copying automatically)
        var shippedOrder = order.Ship();
        var updateResult = context.UpdateImmutable(shippedOrder);
        updateResult.IsRight.ShouldBeTrue();

        await context.SaveChangesAsync();

        // Assert - Reload and verify
        await using var verifyContext = new ImmutableTestDbContext(options);
        var reloaded = await verifyContext.Orders.FindAsync(id);

        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe("Shipped");
        reloaded.CustomerName.ShouldBe("John");
    }

    #endregion
}
