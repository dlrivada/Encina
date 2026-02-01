using Encina;
using Encina.DomainModeling;
using Encina.MongoDB.Repository;
using Encina.TestInfrastructure.Fixtures;
using Encina.Testing.Time;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Auditing;

/// <summary>
/// Integration tests for audit field auto-population in MongoDB repository.
/// Tests verify that IAuditableEntity fields are correctly populated during CRUD operations.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
[Collection(MongoDbCollection.Name)]
public class AuditFieldPopulationMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private IMongoCollection<AuditableDocument>? _auditableCollection;
    private IMongoCollection<NonAuditableDocument>? _nonAuditableCollection;
    private IMongoCollection<PartialAuditableDocument>? _partialAuditableCollection;
    private FunctionalRepositoryMongoDB<AuditableDocument, Guid>? _repository;
    private FunctionalRepositoryMongoDB<AuditableDocument, Guid>? _repositoryWithAudit;
    private FunctionalRepositoryMongoDB<NonAuditableDocument, Guid>? _nonAuditableRepository;
    private FunctionalRepositoryMongoDB<PartialAuditableDocument, Guid>? _partialAuditableRepository;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IRequestContext _mockRequestContext;
    private static readonly DateTimeOffset FixedTime = new(2024, 8, 10, 16, 30, 0, TimeSpan.Zero);
    private const string TestUserId = "mongo-test-user-456";

    public AuditFieldPopulationMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _fakeTimeProvider = new FakeTimeProvider(FixedTime);
        _mockRequestContext = Substitute.For<IRequestContext>();
        _mockRequestContext.UserId.Returns(TestUserId);
    }

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return Task.CompletedTask;
        }

        var database = _fixture.Database!;

        // Get or create collections
        _auditableCollection = database.GetCollection<AuditableDocument>("AuditableDocuments");
        _nonAuditableCollection = database.GetCollection<NonAuditableDocument>("NonAuditableDocuments");
        _partialAuditableCollection = database.GetCollection<PartialAuditableDocument>("PartialAuditableDocuments");

        // Repository without audit context
        _repository = new FunctionalRepositoryMongoDB<AuditableDocument, Guid>(
            _auditableCollection,
            d => d.Id);

        // Repository with audit context
        _repositoryWithAudit = new FunctionalRepositoryMongoDB<AuditableDocument, Guid>(
            _auditableCollection,
            d => d.Id,
            _mockRequestContext,
            _fakeTimeProvider);

        // Non-auditable repository
        _nonAuditableRepository = new FunctionalRepositoryMongoDB<NonAuditableDocument, Guid>(
            _nonAuditableCollection,
            d => d.Id,
            _mockRequestContext,
            _fakeTimeProvider);

        // Partial auditable repository
        _partialAuditableRepository = new FunctionalRepositoryMongoDB<PartialAuditableDocument, Guid>(
            _partialAuditableCollection,
            d => d.Id,
            _mockRequestContext,
            _fakeTimeProvider);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_fixture.IsAvailable && _fixture.Database is not null)
        {
            // Clean up test collections
            await _fixture.Database.DropCollectionAsync("AuditableDocuments");
            await _fixture.Database.DropCollectionAsync("NonAuditableDocuments");
            await _fixture.Database.DropCollectionAsync("PartialAuditableDocuments");
        }
    }

    private void SkipIfMongoNotAvailable()
    {
        if (!_fixture.IsAvailable)
        {
            throw new SkipException("MongoDB container is not available. Skipping test.");
        }
    }

    private async Task ClearDataAsync()
    {
        if (_auditableCollection is not null)
        {
            await _auditableCollection.DeleteManyAsync(FilterDefinition<AuditableDocument>.Empty);
        }
        if (_nonAuditableCollection is not null)
        {
            await _nonAuditableCollection.DeleteManyAsync(FilterDefinition<NonAuditableDocument>.Empty);
        }
        if (_partialAuditableCollection is not null)
        {
            await _partialAuditableCollection.DeleteManyAsync(FilterDefinition<PartialAuditableDocument>.Empty);
        }
    }

    #region Create Operations Tests

    [Fact]
    public async Task AddAsync_WithAuditContext_PopulatesCreatedFields()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = new AuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "MongoDB Test Document",
            Value = 199.99m
        };

        // Act
        var result = await _repositoryWithAudit!.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            e.CreatedBy.ShouldBe(TestUserId);
            e.ModifiedAtUtc.ShouldBeNull();
            e.ModifiedBy.ShouldBeNull();
        });
    }

    [Fact]
    public async Task AddAsync_WithDefaultTimeProvider_PopulatesTimestampWithSystemTime()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        // Use AddSeconds(-1) to avoid timing issues due to MongoDB's millisecond precision
        var beforeAdd = DateTime.UtcNow.AddSeconds(-1);
        var entity = new AuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "MongoDB Test Document",
            Value = 199.99m
        };

        // Act - Repository without explicit audit context uses TimeProvider.System by default
        var result = await _repository!.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            // With default TimeProvider.System, CreatedAtUtc should be populated
            e.CreatedAtUtc.ShouldBeGreaterThan(beforeAdd);
            e.CreatedAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
            // No RequestContext provided, so CreatedBy should be null
            e.CreatedBy.ShouldBeNull();
        });
    }

    [Fact]
    public async Task AddRangeAsync_WithAuditContext_PopulatesCreatedFieldsForAllEntities()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entities = new[]
        {
            new AuditableDocument { Id = Guid.NewGuid(), Name = "Mongo Doc 1", Value = 10m },
            new AuditableDocument { Id = Guid.NewGuid(), Name = "Mongo Doc 2", Value = 20m },
            new AuditableDocument { Id = Guid.NewGuid(), Name = "Mongo Doc 3", Value = 30m }
        };

        // Act
        var result = await _repositoryWithAudit!.AddRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        foreach (var entity in entities)
        {
            var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
            stored.IsRight.ShouldBeTrue();
            stored.IfRight(e =>
            {
                e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
                e.CreatedBy.ShouldBe(TestUserId);
                e.ModifiedAtUtc.ShouldBeNull();
                e.ModifiedBy.ShouldBeNull();
            });
        }
    }

    #endregion

    #region Update Operations Tests

    [Fact]
    public async Task UpdateAsync_WithAuditContext_PopulatesModifiedFields()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = new AuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "Original MongoDB Name",
            Value = 99.99m
        };
        await _repositoryWithAudit!.AddAsync(entity);

        // Advance time for update
        _fakeTimeProvider.Advance(TimeSpan.FromHours(4));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        // Update with different user
        var updateContext = Substitute.For<IRequestContext>();
        updateContext.UserId.Returns("mongo-update-user");
        var updateRepository = new FunctionalRepositoryMongoDB<AuditableDocument, Guid>(
            _auditableCollection!,
            d => d.Id,
            updateContext,
            _fakeTimeProvider);

        // Modify entity
        entity.Name = "Updated MongoDB Name";

        // Act
        var result = await updateRepository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            // Created fields should remain unchanged
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            e.CreatedBy.ShouldBe(TestUserId);

            // Modified fields should be updated
            e.ModifiedAtUtc.ShouldBe(updateTime);
            e.ModifiedBy.ShouldBe("mongo-update-user");
        });
    }

    [Fact]
    public async Task UpdateRangeAsync_WithAuditContext_PopulatesModifiedFieldsForAllEntities()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entities = new[]
        {
            new AuditableDocument { Id = Guid.NewGuid(), Name = "Mongo Doc 1", Value = 10m },
            new AuditableDocument { Id = Guid.NewGuid(), Name = "Mongo Doc 2", Value = 20m }
        };
        await _repositoryWithAudit!.AddRangeAsync(entities);

        // Advance time for update
        _fakeTimeProvider.Advance(TimeSpan.FromHours(5));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        // Modify entities
        entities[0].Name = "Updated Mongo 1";
        entities[1].Name = "Updated Mongo 2";

        // Act
        var result = await _repositoryWithAudit.UpdateRangeAsync(entities);

        // Assert
        result.IsRight.ShouldBeTrue();

        foreach (var entity in entities)
        {
            var stored = await _repositoryWithAudit.GetByIdAsync(entity.Id);
            stored.IsRight.ShouldBeTrue();
            stored.IfRight(e =>
            {
                e.ModifiedAtUtc.ShouldBe(updateTime);
                e.ModifiedBy.ShouldBe(TestUserId);
            });
        }
    }

    #endregion

    #region Non-Auditable Entity Tests

    [Fact]
    public async Task AddAsync_NonAuditableEntity_WorksWithoutErrors()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = new NonAuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "Non-Auditable MongoDB Document",
            Value = 69.99m
        };

        // Act
        var result = await _nonAuditableRepository!.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _nonAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.Name.ShouldBe("Non-Auditable MongoDB Document");
            e.Value.ShouldBe(69.99m);
        });
    }

    [Fact]
    public async Task UpdateAsync_NonAuditableEntity_WorksWithoutErrors()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = new NonAuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "Original MongoDB",
            Value = 69.99m
        };
        await _nonAuditableRepository!.AddAsync(entity);

        // Modify entity
        entity.Name = "Updated MongoDB";

        // Act
        var result = await _nonAuditableRepository.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _nonAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e => e.Name.ShouldBe("Updated MongoDB"));
    }

    #endregion

    #region Null RequestContext Tests

    [Fact]
    public async Task AddAsync_NullRequestContext_PopulatesTimestampButNotUserId()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Repository with TimeProvider but null RequestContext
        var repositoryNullContext = new FunctionalRepositoryMongoDB<AuditableDocument, Guid>(
            _auditableCollection!,
            d => d.Id,
            requestContext: null,
            _fakeTimeProvider);

        var entity = new AuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "MongoDB Test Document",
            Value = 99.99m
        };

        // Act
        var result = await repositoryNullContext.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await repositoryNullContext.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            e.CreatedBy.ShouldBeNull(); // No user context, should remain null
        });
    }

    [Fact]
    public async Task UpdateAsync_NullRequestContext_PopulatesTimestampButNotUserId()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Repository with TimeProvider but null RequestContext
        var repositoryNullContext = new FunctionalRepositoryMongoDB<AuditableDocument, Guid>(
            _auditableCollection!,
            d => d.Id,
            requestContext: null,
            _fakeTimeProvider);

        var entity = new AuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "Original MongoDB",
            Value = 99.99m
        };
        await repositoryNullContext.AddAsync(entity);

        // Advance time
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(60));
        var updateTime = _fakeTimeProvider.GetUtcNow().UtcDateTime;

        entity.Name = "Updated MongoDB";

        // Act
        var result = await repositoryNullContext.UpdateAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await repositoryNullContext.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.ModifiedAtUtc.ShouldBe(updateTime);
            e.ModifiedBy.ShouldBeNull(); // No user context
        });
    }

    #endregion

    #region Partial Interface Implementation Tests

    [Fact]
    public async Task AddAsync_PartialAuditableEntity_PopulatesOnlyImplementedFields()
    {
        SkipIfMongoNotAvailable();
        await ClearDataAsync();

        // Arrange
        var entity = new PartialAuditableDocument
        {
            Id = Guid.NewGuid(),
            Name = "Partial Auditable MongoDB Document"
        };

        // Act
        var result = await _partialAuditableRepository!.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _partialAuditableRepository.GetByIdAsync(entity.Id);
        stored.IsRight.ShouldBeTrue();
        stored.IfRight(e =>
        {
            e.CreatedAtUtc.ShouldBe(FixedTime.UtcDateTime);
            // No CreatedBy field on this entity
        });
    }

    #endregion
}

#region Test Documents

/// <summary>
/// MongoDB document implementing full IAuditableEntity for audit field population tests.
/// </summary>
public class AuditableDocument : IAuditableEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// MongoDB document that does NOT implement any audit interfaces.
/// </summary>
public class NonAuditableDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>
/// MongoDB document implementing only ICreatedAtUtc (partial audit tracking).
/// </summary>
public class PartialAuditableDocument : ICreatedAtUtc
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

#endregion

#region Skip Exception

/// <summary>
/// Exception to skip tests when infrastructure is not available.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}

#endregion
