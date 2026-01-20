using System.Data;
using Encina;
using Encina.ADO.SqlServer.Repository;
using Encina.ADO.SqlServer.UnitOfWork;
using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.ADO.SqlServer.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkADO"/>.
/// </summary>
[Trait("Category", "Unit")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkADOTests : IAsyncLifetime
{
    private readonly IDbConnection _mockConnection;
    private readonly IServiceProvider _mockServiceProvider;
    private readonly UnitOfWorkADO _unitOfWork;

    public UnitOfWorkADOTests()
    {
        _mockConnection = Substitute.For<IDbConnection>();
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _unitOfWork = new UnitOfWorkADO(_mockConnection, _mockServiceProvider);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkADO(null!, _mockServiceProvider));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkADO(_mockConnection, null!));
    }

    #endregion

    #region HasActiveTransaction Tests

    [Fact]
    public void HasActiveTransaction_NoTransaction_ReturnsFalse()
    {
        // Act & Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact]
    public async Task HasActiveTransaction_AfterSuccessfulBeginTransaction_ReturnsTrue()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    #endregion

    #region Repository Tests

    [Fact]
    public void Repository_WithoutMapping_ThrowsInvalidOperationException()
    {
        // Arrange - No mapping registered
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestADOEntity, Guid>)).Returns((object?)null);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _unitOfWork.Repository<TestADOEntity, Guid>());
    }

    [Fact]
    public void Repository_WithMapping_ReturnsRepositoryInstance()
    {
        // Arrange
        var mapping = CreateTestMapping();
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestADOEntity, Guid>)).Returns(mapping);

        // Act
        var repository = _unitOfWork.Repository<TestADOEntity, Guid>();

        // Assert
        repository.ShouldNotBeNull();
        repository.ShouldBeAssignableTo<IFunctionalRepository<TestADOEntity, Guid>>();
    }

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        // Arrange
        var mapping = CreateTestMapping();
        _mockServiceProvider.GetService(typeof(IEntityMapping<TestADOEntity, Guid>)).Returns(mapping);

        // Act
        var repository1 = _unitOfWork.Repository<TestADOEntity, Guid>();
        var repository2 = _unitOfWork.Repository<TestADOEntity, Guid>();

        // Assert
        repository1.ShouldBeSameAs(repository2);
    }

    [Fact]
    public async Task Repository_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _unitOfWork.DisposeAsync();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() =>
            _unitOfWork.Repository<TestADOEntity, Guid>());
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert - ADO.NET doesn't track changes, always returns 0
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(0));
    }

    [Fact]
    public async Task SaveChangesAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _unitOfWork.DisposeAsync();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await _unitOfWork.SaveChangesAsync());
    }

    #endregion

    #region BeginTransactionAsync Tests

    [Fact]
    public async Task BeginTransactionAsync_ConnectionOpen_StartsTransaction()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
        _mockConnection.Received(1).BeginTransaction();
    }

    [Fact]
    public async Task BeginTransactionAsync_TransactionAlreadyActive_ReturnsError()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        // First transaction
        await _unitOfWork.BeginTransactionAsync();

        // Act - Try to start second transaction
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionAlreadyActiveErrorCode));
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_ConnectionThrows_ReturnsError()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        _mockConnection.BeginTransaction().Returns(x => throw new InvalidOperationException("Cannot start transaction"));

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.TransactionStartFailedErrorCode));
        });
    }

    [Fact]
    public async Task BeginTransactionAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _unitOfWork.DisposeAsync();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await _unitOfWork.BeginTransactionAsync());
    }

    #endregion

    #region CommitAsync Tests

    [Fact]
    public async Task CommitAsync_NoActiveTransaction_ReturnsError()
    {
        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
        });
    }

    [Fact]
    public async Task CommitAsync_WithActiveTransaction_CommitsAndClearsTransaction()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        mockTransaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAsync_CommitThrows_RollsBackAndReturnsError()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);
        mockTransaction.When(t => t.Commit()).Do(x => throw new InvalidOperationException("Commit failed"));

        await _unitOfWork.BeginTransactionAsync();

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.CommitFailedErrorCode));
        });
        mockTransaction.Received(1).Rollback();
    }

    [Fact]
    public async Task CommitAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _unitOfWork.DisposeAsync();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await _unitOfWork.CommitAsync());
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_NoActiveTransaction_DoesNotThrow()
    {
        // Act & Assert - Should not throw even when no transaction
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithActiveTransaction_RollsBackAndClearsTransaction()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        mockTransaction.Received(1).Rollback();
    }

    [Fact]
    public async Task RollbackAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        await _unitOfWork.DisposeAsync();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await _unitOfWork.RollbackAsync());
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _unitOfWork.DisposeAsync();
        await _unitOfWork.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WithActiveTransaction_RollsBack()
    {
        // Arrange
        _mockConnection.State.Returns(ConnectionState.Open);
        var mockTransaction = Substitute.For<IDbTransaction>();
        _mockConnection.BeginTransaction().Returns(mockTransaction);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        await _unitOfWork.DisposeAsync();

        // Assert
        mockTransaction.Received(1).Rollback();
    }

    #endregion

    #region Helper Methods

    private static IEntityMapping<TestADOEntity, Guid> CreateTestMapping()
    {
        var builder = new EntityMappingBuilder<TestADOEntity, Guid>();
        builder.ToTable("TestEntities")
            .HasId(e => e.Id)
            .MapProperty(e => e.Name);
        return builder.Build();
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity for ADO.NET UnitOfWork tests.
/// </summary>
public class TestADOEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion
