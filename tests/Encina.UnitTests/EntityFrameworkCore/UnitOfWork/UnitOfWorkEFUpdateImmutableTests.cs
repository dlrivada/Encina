using Encina.DomainModeling;
using Encina.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Encina.UnitTests.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Tests for UpdateImmutable and UpdateImmutableAsync methods on <see cref="UnitOfWorkEF"/>.
/// </summary>
[Trait("Category", "Unit")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkEFUpdateImmutableTests : IAsyncLifetime
{
    private readonly TestDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly UnitOfWorkEF _unitOfWork;

    public UnitOfWorkEFUpdateImmutableTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _unitOfWork = new UnitOfWorkEF(_dbContext, _serviceProvider);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    #region Test Types

    private sealed record TestDomainEvent(Guid EntityId) : IDomainEvent, INotification
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }

    #endregion

    #region UpdateImmutable Synchronous Tests

    [Fact]
    public void UpdateImmutable_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        TestUoWEntity entity = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            _unitOfWork.UpdateImmutable(entity));
        ex.ParamName.ShouldBe("modified");
    }

    [Fact]
    public void UpdateImmutable_TrackedEntity_ReturnsRight()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        _dbContext.TestUoWEntities.Add(original);
        _dbContext.SaveChanges();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act
        var result = _unitOfWork.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void UpdateImmutable_UntrackedEntity_ReturnsLeft()
    {
        // Arrange - Entity is not tracked (never added to context)
        var entity = new TestUoWEntity { Id = Guid.NewGuid(), Name = "NotTracked" };

        // Act
        var result = _unitOfWork.UpdateImmutable(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
            error.Message.ShouldContain("TestUoWEntity");
        });
    }

    [Fact]
    public void UpdateImmutable_PreservesDomainEvents()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWAggregateRoot(id) { Name = "Original" };
        var domainEvent = new TestDomainEvent(id);
        original.RaiseTestEvent(domainEvent);

        _dbContext.TestUoWAggregates.Add(original);
        _dbContext.SaveChanges();

        // Create modified version (without events)
        var modified = new TestUoWAggregateRoot(id) { Name = "Modified" };

        // Act
        var result = _unitOfWork.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Events should be copied from original to modified
        modified.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public void UpdateImmutable_DetachesOriginalEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        _dbContext.TestUoWEntities.Add(original);
        _dbContext.SaveChanges();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act
        var result = _unitOfWork.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        _dbContext.Entry(original).State.ShouldBe(EntityState.Detached);
    }

    [Fact]
    public void UpdateImmutable_MarksModifiedEntityAsModified()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        _dbContext.TestUoWEntities.Add(original);
        _dbContext.SaveChanges();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act
        var result = _unitOfWork.UpdateImmutable(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        _dbContext.Entry(modified).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public async Task UpdateImmutable_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestDbContext(options);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);

        // Add and save entity before disposal
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        dbContext.TestUoWEntities.Add(original);
        await dbContext.SaveChangesAsync();

        // Dispose
        await unitOfWork.DisposeAsync();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() =>
            unitOfWork.UpdateImmutable(modified));
    }

    #endregion

    #region UpdateImmutableAsync Tests

    [Fact]
    public async Task UpdateImmutableAsync_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        TestUoWEntity entity = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            _unitOfWork.UpdateImmutableAsync(entity).AsTask());
        ex.ParamName.ShouldBe("modified");
    }

    [Fact]
    public async Task UpdateImmutableAsync_TrackedEntity_ReturnsRight()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        _dbContext.TestUoWEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act
        var result = await _unitOfWork.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateImmutableAsync_UntrackedEntity_ReturnsLeft()
    {
        // Arrange - Entity is not tracked
        var entity = new TestUoWEntity { Id = Guid.NewGuid(), Name = "NotTracked" };

        // Act
        var result = await _unitOfWork.UpdateImmutableAsync(entity);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(RepositoryErrors.EntityNotTrackedErrorCode));
        });
    }

    [Fact]
    public async Task UpdateImmutableAsync_PreservesDomainEvents()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWAggregateRoot(id) { Name = "Original" };
        var domainEvent = new TestDomainEvent(id);
        original.RaiseTestEvent(domainEvent);

        _dbContext.TestUoWAggregates.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestUoWAggregateRoot(id) { Name = "Modified" };

        // Act
        var result = await _unitOfWork.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        modified.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact]
    public async Task UpdateImmutableAsync_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var entity = new TestUoWEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _unitOfWork.UpdateImmutableAsync(entity, cts.Token).AsTask());
    }

    [Fact]
    public async Task UpdateImmutableAsync_DetachesOriginalAndTracksModified()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        _dbContext.TestUoWEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act
        var result = await _unitOfWork.UpdateImmutableAsync(modified);

        // Assert
        result.IsRight.ShouldBeTrue();
        _dbContext.Entry(original).State.ShouldBe(EntityState.Detached);
        _dbContext.Entry(modified).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public async Task UpdateImmutableAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestDbContext(options);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var unitOfWork = new UnitOfWorkEF(dbContext, serviceProvider);

        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        dbContext.TestUoWEntities.Add(original);
        await dbContext.SaveChangesAsync();

        await unitOfWork.DisposeAsync();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() =>
            unitOfWork.UpdateImmutableAsync(modified).AsTask());
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public async Task UpdateImmutable_ThenSaveChanges_PersistsChanges()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = new TestUoWEntity { Id = id, Name = "Original" };
        _dbContext.TestUoWEntities.Add(original);
        await _dbContext.SaveChangesAsync();

        var modified = new TestUoWEntity { Id = id, Name = "Modified" };

        // Act
        var updateResult = _unitOfWork.UpdateImmutable(modified);
        updateResult.IsRight.ShouldBeTrue();

        var saveResult = await _unitOfWork.SaveChangesAsync();

        // Assert
        saveResult.IsRight.ShouldBeTrue();
        saveResult.IfRight(count => count.ShouldBe(1));

        // Verify persistence
        var reloaded = await _dbContext.TestUoWEntities.FindAsync(id);
        reloaded.ShouldNotBeNull();
        reloaded!.Name.ShouldBe("Modified");
    }

    [Fact]
    public async Task UpdateImmutableAsync_MultipleUpdates_AllSucceed()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var original1 = new TestUoWEntity { Id = id1, Name = "Original1" };
        var original2 = new TestUoWEntity { Id = id2, Name = "Original2" };
        _dbContext.TestUoWEntities.AddRange(original1, original2);
        await _dbContext.SaveChangesAsync();

        var modified1 = new TestUoWEntity { Id = id1, Name = "Modified1" };
        var modified2 = new TestUoWEntity { Id = id2, Name = "Modified2" };

        // Act
        var result1 = await _unitOfWork.UpdateImmutableAsync(modified1);
        var result2 = await _unitOfWork.UpdateImmutableAsync(modified2);
        var saveResult = await _unitOfWork.SaveChangesAsync();

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();
        saveResult.IsRight.ShouldBeTrue();
        saveResult.IfRight(count => count.ShouldBe(2));
    }

    #endregion
}
