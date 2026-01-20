using Encina;
using Encina.DomainModeling;
using Encina.MongoDB;
using Encina.MongoDB.Repository;
using Encina.MongoDB.UnitOfWork;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkMongoDB"/>.
/// </summary>
[Trait("Category", "Unit")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkMongoDBTests : IAsyncLifetime
{
    private readonly IMongoClient _mockMongoClient;
    private readonly IMongoDatabase _mockDatabase;
    private readonly IOptions<EncinaMongoDbOptions> _mockOptions;
    private readonly IServiceProvider _mockServiceProvider;
    private readonly UnitOfWorkMongoDB _unitOfWork;

    public UnitOfWorkMongoDBTests()
    {
        _mockMongoClient = Substitute.For<IMongoClient>();
        _mockDatabase = Substitute.For<IMongoDatabase>();
        _mockOptions = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = "TestDatabase"
        });
        _mockServiceProvider = Substitute.For<IServiceProvider>();

        _mockMongoClient.GetDatabase(_mockOptions.Value.DatabaseName, Arg.Any<MongoDatabaseSettings>())
            .Returns(_mockDatabase);

        _unitOfWork = new UnitOfWorkMongoDB(_mockMongoClient, _mockOptions, _mockServiceProvider);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullMongoClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkMongoDB(null!, _mockOptions, _mockServiceProvider));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkMongoDB(_mockMongoClient, null!, _mockServiceProvider));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkMongoDB(_mockMongoClient, _mockOptions, null!));
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
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    #endregion

    #region Repository Tests

    [Fact]
    public void Repository_WithoutOptions_ThrowsInvalidOperationException()
    {
        // Arrange - No options registered
        _mockServiceProvider.GetService(typeof(MongoDbRepositoryOptions<TestMongoEntity, Guid>)).Returns((object?)null);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _unitOfWork.Repository<TestMongoEntity, Guid>());
    }

    [Fact]
    public void Repository_WithOptions_ReturnsRepositoryInstance()
    {
        // Arrange
        var options = CreateTestRepositoryOptions();
        _mockServiceProvider.GetService(typeof(MongoDbRepositoryOptions<TestMongoEntity, Guid>)).Returns(options);

        var mockCollection = Substitute.For<IMongoCollection<TestMongoEntity>>();
        _mockDatabase.GetCollection<TestMongoEntity>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mockCollection);

        // Act
        var repository = _unitOfWork.Repository<TestMongoEntity, Guid>();

        // Assert
        repository.ShouldNotBeNull();
        repository.ShouldBeAssignableTo<IFunctionalRepository<TestMongoEntity, Guid>>();
    }

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        // Arrange
        var options = CreateTestRepositoryOptions();
        _mockServiceProvider.GetService(typeof(MongoDbRepositoryOptions<TestMongoEntity, Guid>)).Returns(options);

        var mockCollection = Substitute.For<IMongoCollection<TestMongoEntity>>();
        _mockDatabase.GetCollection<TestMongoEntity>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(mockCollection);

        // Act
        var repository1 = _unitOfWork.Repository<TestMongoEntity, Guid>();
        var repository2 = _unitOfWork.Repository<TestMongoEntity, Guid>();

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
            _unitOfWork.Repository<TestMongoEntity, Guid>());
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert - MongoDB doesn't track changes, always returns 0
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
    public async Task BeginTransactionAsync_StartsSession_StartsTransaction()
    {
        // Arrange
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
        mockSession.Received(1).StartTransaction(Arg.Any<TransactionOptions>());
    }

    [Fact]
    public async Task BeginTransactionAsync_TransactionAlreadyActive_ReturnsError()
    {
        // Arrange
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);

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
    public async Task BeginTransactionAsync_SessionStartFails_ReturnsError()
    {
        // Arrange
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns<IClientSessionHandle>(x => throw new MongoException("Cannot start session"));

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
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true, true, false);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        await mockSession.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAsync_CommitThrows_AbortsAndReturnsError()
    {
        // Arrange
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true, true, false);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);
        mockSession.CommitTransactionAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(x => throw new MongoException("Commit failed"));

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
        await mockSession.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
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
    public async Task RollbackAsync_WithActiveTransaction_AbortsAndClearsTransaction()
    {
        // Arrange
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true, true, false);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
        await mockSession.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
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
    public async Task DisposeAsync_WithActiveTransaction_Aborts()
    {
        // Arrange
        var mockSession = Substitute.For<IClientSessionHandle>();
        mockSession.IsInTransaction.Returns(true);
        _mockMongoClient.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>())
            .Returns(mockSession);

        await _unitOfWork.BeginTransactionAsync();

        // Act
        await _unitOfWork.DisposeAsync();

        // Assert
        await mockSession.Received(1).AbortTransactionAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static MongoDbRepositoryOptions<TestMongoEntity, Guid> CreateTestRepositoryOptions()
    {
        return new MongoDbRepositoryOptions<TestMongoEntity, Guid>
        {
            CollectionName = "test_entities",
            IdProperty = e => e.Id
        };
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity for MongoDB UnitOfWork tests.
/// </summary>
public class TestMongoEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion
