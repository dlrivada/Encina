using Encina.DomainModeling;
using Encina.MongoDB;
using Encina.MongoDB.Repository;
using Encina.MongoDB.UnitOfWork;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.UnitOfWork;

/// <summary>
/// Integration tests for <see cref="UnitOfWorkMongoDB"/> using real MongoDB.
/// </summary>
/// <remarks>
/// <para>
/// <strong>IMPORTANT:</strong> MongoDB transactions require a replica set configuration.
/// The default MongoDB container does not support transactions. These tests will be
/// skipped if the MongoDB instance does not support transactions.
/// </para>
/// <para>
/// To run these tests locally, use MongoDB with replica set enabled:
/// <code>
/// docker run -d -p 27017:27017 --name mongodb-replica mongo:7 --replSet rs0
/// docker exec -it mongodb-replica mongosh --eval "rs.initiate()"
/// </code>
/// </para>
/// </remarks>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal handled by IAsyncLifetime.DisposeAsync")]
public class UnitOfWorkMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private IMongoCollection<TestUoWDocument>? _collection;
    private UnitOfWorkMongoDB? _unitOfWork;
    private IServiceProvider? _serviceProvider;

    public UnitOfWorkMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        _collection = _fixture.Database!.GetCollection<TestUoWDocument>("test_uow_documents");

        // Register repository options
        var services = new ServiceCollection();
        var repositoryOptions = new MongoDbRepositoryOptions<TestUoWDocument, Guid>
        {
            CollectionName = "test_uow_documents",
            IdProperty = doc => doc.Id
        };
        services.AddSingleton(repositoryOptions);

        var mongoOptions = Options.Create(new EncinaMongoDbOptions { DatabaseName = MongoDbFixture.DatabaseName });
        services.AddSingleton(mongoOptions);
        _serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWorkMongoDB(_fixture.Client!, mongoOptions, _serviceProvider);
    }

    public async Task DisposeAsync()
    {
        if (_unitOfWork != null)
        {
            await _unitOfWork.DisposeAsync();
        }

        if (_collection != null)
        {
            await _collection.DeleteManyAsync(Builders<TestUoWDocument>.Filter.Empty);
        }
    }

    private async Task ClearDataAsync()
    {
        if (_collection != null)
        {
            await _collection.DeleteManyAsync(Builders<TestUoWDocument>.Filter.Empty);
        }
    }

    private static void SkipIfNotAvailable(bool isAvailable, UnitOfWorkMongoDB? unitOfWork)
    {
        if (!isAvailable || unitOfWork == null)
        {
            throw new Xunit.SkipException("MongoDB container is not available");
        }
    }

    private void SkipIfNotAvailable()
    {
        SkipIfNotAvailable(_fixture.IsAvailable, _unitOfWork);
    }

    #region Transaction Commit Tests

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Transaction_CommitMultipleEntities_AllPersisted()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var repository = _unitOfWork!.Repository<TestUoWDocument, Guid>();
        var entity1 = CreateTestDocument("Entity 1");
        var entity2 = CreateTestDocument("Entity 2");
        var entity3 = CreateTestDocument("Entity 3");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);
        await repository.AddAsync(entity3);

        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert
        var count = await _collection!.CountDocumentsAsync(Builders<TestUoWDocument>.Filter.Empty);
        count.ShouldBe(3);
    }

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Transaction_ModifyEntities_ChangesPersisted()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Create initial entity
        var entity = CreateTestDocument("Original Name");
        await _collection!.InsertOneAsync(entity);

        // Act - Modify in transaction
        var beginResult = await _unitOfWork!.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var repository = _unitOfWork.Repository<TestUoWDocument, Guid>();
        entity.Name = "Updated Name";
        await repository.UpdateAsync(entity);

        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert
        var filter = Builders<TestUoWDocument>.Filter.Eq(d => d.Id, entity.Id);
        var updated = await _collection.Find(filter).FirstOrDefaultAsync();
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Name");
    }

    #endregion

    #region Transaction Rollback Tests

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Transaction_Rollback_NoChangesPersisted()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var repository = _unitOfWork!.Repository<TestUoWDocument, Guid>();
        var entity = CreateTestDocument("Should Not Persist");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);

        // Rollback instead of commit
        await _unitOfWork.RollbackAsync();

        // Assert
        var count = await _collection!.CountDocumentsAsync(Builders<TestUoWDocument>.Filter.Empty);
        count.ShouldBe(0);
    }

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Transaction_RollbackAfterModify_OriginalValuePreserved()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Create initial entity
        var entity = CreateTestDocument("Original Name");
        await _collection!.InsertOneAsync(entity);

        // Act - Modify in transaction then rollback
        var beginResult = await _unitOfWork!.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        var repository = _unitOfWork.Repository<TestUoWDocument, Guid>();
        entity.Name = "Modified Name";
        await repository.UpdateAsync(entity);

        await _unitOfWork.RollbackAsync();

        // Assert - Original value preserved
        var filter = Builders<TestUoWDocument>.Filter.Eq(d => d.Id, entity.Id);
        var persisted = await _collection.Find(filter).FirstOrDefaultAsync();
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("Original Name");
    }

    #endregion

    #region Auto-Rollback on Dispose Tests

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Dispose_WithUncommittedTransaction_AutoRollback()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var repository = _unitOfWork!.Repository<TestUoWDocument, Guid>();
        var entity = CreateTestDocument("Uncommitted Entity");

        // Act
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        await repository.AddAsync(entity);

        // Don't commit - just dispose
        await _unitOfWork.DisposeAsync();

        // Assert - Changes should be rolled back
        var count = await _collection!.CountDocumentsAsync(Builders<TestUoWDocument>.Filter.Empty);
        count.ShouldBe(0);
    }

    #endregion

    #region Repository Caching Tests

    [Fact]
    public void Repository_SameEntityType_ReturnsSameInstance()
    {
        SkipIfNotAvailable();

        // Act
        var repository1 = _unitOfWork!.Repository<TestUoWDocument, Guid>();
        var repository2 = _unitOfWork.Repository<TestUoWDocument, Guid>();

        // Assert
        repository1.ShouldBeSameAs(repository2);
    }

    #endregion

    #region Transaction State Tests

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task BeginTransaction_SetsHasActiveTransactionTrue()
    {
        SkipIfNotAvailable();

        // Arrange
        _unitOfWork!.HasActiveTransaction.ShouldBeFalse();

        // Act
        var result = await _unitOfWork.BeginTransactionAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Commit_ClearsHasActiveTransaction()
    {
        SkipIfNotAvailable();

        // Arrange
        await _unitOfWork!.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.CommitAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task Rollback_ClearsHasActiveTransaction()
    {
        SkipIfNotAvailable();

        // Arrange
        await _unitOfWork!.BeginTransactionAsync();
        _unitOfWork.HasActiveTransaction.ShouldBeTrue();

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        _unitOfWork.HasActiveTransaction.ShouldBeFalse();
    }

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task BeginTransaction_WhenAlreadyActive_ReturnsError()
    {
        SkipIfNotAvailable();

        // Arrange
        await _unitOfWork!.BeginTransactionAsync();

        // Act
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
    public async Task Commit_WhenNoTransaction_ReturnsError()
    {
        SkipIfNotAvailable();

        // Act
        var result = await _unitOfWork!.CommitAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(UnitOfWorkErrors.NoActiveTransactionErrorCode));
        });
    }

    #endregion

    #region Non-Transaction Operations Tests

    [Fact]
    public async Task Repository_WithoutTransaction_OperationsStillWork()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var repository = _unitOfWork!.Repository<TestUoWDocument, Guid>();
        var entity = CreateTestDocument("No Transaction");

        // Act - Add without transaction
        var addResult = await repository.AddAsync(entity);
        addResult.IsRight.ShouldBeTrue();

        // Assert
        var count = await _collection!.CountDocumentsAsync(Builders<TestUoWDocument>.Filter.Empty);
        count.ShouldBe(1);

        var getResult = await repository.GetByIdAsync(entity.Id);
        getResult.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Complex Workflow Tests

    [Fact(Skip = "Requires MongoDB replica set for transactions")]
    public async Task ComplexWorkflow_MultipleOperations_CommitPreservesAll()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var repository = _unitOfWork!.Repository<TestUoWDocument, Guid>();

        // Act - Begin transaction
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        beginResult.IsRight.ShouldBeTrue();

        // Add entities
        var entity1 = CreateTestDocument("First");
        var entity2 = CreateTestDocument("Second");
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Modify one entity
        entity1.Name = "First Modified";
        await repository.UpdateAsync(entity1);

        // Add another entity
        var entity3 = CreateTestDocument("Third");
        await repository.AddAsync(entity3);

        // Commit
        var commitResult = await _unitOfWork.CommitAsync();
        commitResult.IsRight.ShouldBeTrue();

        // Assert - All changes persisted
        var entities = await _collection!.Find(Builders<TestUoWDocument>.Filter.Empty).ToListAsync();
        entities.Count.ShouldBe(3);
        entities.ShouldContain(e => e.Name == "First Modified");
        entities.ShouldContain(e => e.Name == "Second");
        entities.ShouldContain(e => e.Name == "Third");
    }

    #endregion

    #region Helper Methods

    private static TestUoWDocument CreateTestDocument(
        string name = "Test Document",
        bool isActive = true,
        decimal amount = 100m)
    {
        return new TestUoWDocument
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test document for MongoDB UnitOfWork integration tests.
/// </summary>
public class TestUoWDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

#endregion
