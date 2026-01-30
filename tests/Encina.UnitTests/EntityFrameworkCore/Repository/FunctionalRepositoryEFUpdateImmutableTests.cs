using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Repository;

/// <summary>
/// Tests for UpdateImmutableAsync method on <see cref="FunctionalRepositoryEF{TEntity, TId}"/>.
/// </summary>
[Trait("Category", "Unit")]
public class FunctionalRepositoryEFUpdateImmutableTests : IDisposable
{
    private readonly RepositoryTestDbContext _dbContext;
    private readonly FunctionalRepositoryEF<TestEntity, Guid> _entityRepository;
    private readonly FunctionalRepositoryEF<TestAggregateRoot, Guid> _aggregateRepository;

    public FunctionalRepositoryEFUpdateImmutableTests()
    {
        var options = new DbContextOptionsBuilder<RepositoryTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RepositoryTestDbContext(options);
        _entityRepository = new FunctionalRepositoryEF<TestEntity, Guid>(_dbContext);
        _aggregateRepository = new FunctionalRepositoryEF<TestAggregateRoot, Guid>(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Null Parameter Tests

    [Fact]
    public async Task UpdateImmutableAsync_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        TestEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            _entityRepository.UpdateImmutableAsync(entity));
        ex.ParamName.ShouldBe("modified");
    }

    #endregion

    #region Successful Update Tests

    [Fact]
    public async Task UpdateImmutableAsync_TrackedEntity_ReturnsRight()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestEntity { Id = id, Name = "Original", Amount = 100m };
        _dbContext.TestEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestEntity { Id = id, Name = "Modified", Amount = 200m };

        // Act
        var result = await _entityRepository.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateImmutableAsync_PersistsChangesImmediately()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestEntity { Id = id, Name = "Original", Amount = 100m };
        _dbContext.TestEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestEntity { Id = id, Name = "Modified", Amount = 200m };

        // Act
        var result = await _entityRepository.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify persistence (FunctionalRepository calls SaveChangesAsync internally)
        var reloaded = await _dbContext.TestEntities.FindAsync(id);
        reloaded.ShouldNotBeNull();
        reloaded!.Name.ShouldBe("Modified");
        reloaded.Amount.ShouldBe(200m);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UpdateImmutableAsync_UntrackedEntity_ReturnsLeftWithError()
    {
        // Arrange - Entity is not tracked (never added to context)
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "NotTracked", Amount = 100m };

        // Act
        var result = await _entityRepository.UpdateImmutableAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
            error.Message.ShouldContain("TestEntity");
        });
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public async Task UpdateImmutableAsync_PreservesDomainEvents()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestAggregateRoot(id) { Name = "Original", Amount = 100m };
        var domainEvent = new TestDomainEvent(id, "Initial event");
        original.RaiseEvent(domainEvent);

        _dbContext.TestAggregates.Add(original);
        await _dbContext.SaveChangesAsync();

        // Create modified version (without events)
        var modified = new TestAggregateRoot(id) { Name = "Modified", Amount = 200m };

        // Act
        var result = await _aggregateRepository.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Events should be copied from original to modified
        modified.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public async Task UpdateImmutableAsync_WithMultipleEvents_PreservesAllEvents()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestAggregateRoot(id) { Name = "Original", Amount = 100m };
        var event1 = new TestDomainEvent(id, "Event 1");
        var event2 = new TestDomainEvent(id, "Event 2");
        var event3 = new TestDomainEvent(id, "Event 3");
        original.RaiseEvent(event1);
        original.RaiseEvent(event2);
        original.RaiseEvent(event3);

        _dbContext.TestAggregates.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestAggregateRoot(id) { Name = "Modified", Amount = 200m };

        // Act
        var result = await _aggregateRepository.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        modified.DomainEvents.Count.ShouldBe(3);
        modified.DomainEvents.ShouldContain(event1);
        modified.DomainEvents.ShouldContain(event2);
        modified.DomainEvents.ShouldContain(event3);
    }

    #endregion

    #region ChangeTracker Behavior Tests

    [Fact]
    public async Task UpdateImmutableAsync_DetachesOriginalEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestEntity { Id = id, Name = "Original", Amount = 100m };
        _dbContext.TestEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestEntity { Id = id, Name = "Modified", Amount = 200m };

        // Act
        var result = await _entityRepository.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        _dbContext.Entry(original).State.ShouldBe(EntityState.Detached);
    }

    [Fact]
    public async Task UpdateImmutableAsync_ModifiedEntityStateAfterSave()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestEntity { Id = id, Name = "Original", Amount = 100m };
        _dbContext.TestEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestEntity { Id = id, Name = "Modified", Amount = 200m };

        // Act
        var result = await _entityRepository.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        // After SaveChanges (which is called internally), state should be Unchanged
        _dbContext.Entry(modified).State.ShouldBe(EntityState.Unchanged);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public async Task UpdateImmutableAsync_TypicalWorkflow_PersistsChangesWithEvents()
    {
        // Arrange - Simulate typical immutable update workflow
        var id = Guid.NewGuid();
        var order = new TestAggregateRoot(id) { Name = "Order", Amount = 100m };
        _dbContext.TestAggregates.Add(order);
        await _dbContext.SaveChangesAsync();

        // Simulate domain operation that raises event and returns new instance
        var updatedOrder = order.WithNewAmount(200m);

        // Act
        var result = await _aggregateRepository.UpdateImmutableAsync(updatedOrder);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify persistence
        var reloaded = await _dbContext.TestAggregates.FindAsync(id);
        reloaded.ShouldNotBeNull();
        reloaded!.Amount.ShouldBe(200m);

        // Verify event was preserved
        updatedOrder.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateImmutableAsync_MultipleSequentialUpdates_AllSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestEntity { Id = id, Name = "Version1", Amount = 100m };
        _dbContext.TestEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        // Act - Multiple sequential updates
        var modified1 = new TestEntity { Id = id, Name = "Version2", Amount = 200m };
        var result1 = await _entityRepository.UpdateImmutableAsync(modified1);
        result1.IsRight.ShouldBeTrue();

        var modified2 = new TestEntity { Id = id, Name = "Version3", Amount = 300m };
        var result2 = await _entityRepository.UpdateImmutableAsync(modified2);
        result2.IsRight.ShouldBeTrue();

        // Assert - Final state persisted
        var final = await _dbContext.TestEntities.FindAsync(id);
        final.ShouldNotBeNull();
        final!.Name.ShouldBe("Version3");
        final.Amount.ShouldBe(300m);
    }

    #endregion
}
