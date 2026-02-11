using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Tenancy;

/// <summary>
/// Integration tests for <see cref="TenantAwareFunctionalRepositoryMongoDB{TEntity, TId}"/> using real MongoDB.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify tenant isolation, automatic tenant assignment, and cross-tenant
/// access prevention using a real MongoDB instance.
/// </para>
/// <list type="bullet">
/// <item><description>Tenant filter injection ensures queries only return tenant-scoped data</description></item>
/// <item><description>Automatic tenant ID assignment on insert operations</description></item>
/// <item><description>Cross-tenant access prevention on update and delete operations</description></item>
/// <item><description>Tenant switching correctly changes data visibility</description></item>
/// </list>
/// </remarks>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class TenantAwareRepositoryMongoDBIntegrationTests : IAsyncLifetime
{
    private const string Tenant1 = "tenant-1";
    private const string Tenant2 = "tenant-2";
    private const string CollectionName = "tenant_test_documents";

    private readonly MongoDbFixture _fixture;
    private IMongoCollection<TenantTestDocument>? _collection;
    private TestTenantProvider? _tenantProvider;
    private ITenantEntityMapping<TenantTestDocument, Guid>? _mapping;
    private MongoDbTenancyOptions? _options;

    public TenantAwareRepositoryMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return ValueTask.CompletedTask;
        }

        _collection = _fixture.Database!.GetCollection<TenantTestDocument>(CollectionName);

        // Create tenant provider
        _tenantProvider = new TestTenantProvider();
        _tenantProvider.SetTenant(Tenant1);

        // Create mapping
        _mapping = new TenantEntityMappingBuilder<TenantTestDocument, Guid>()
            .ToCollection(CollectionName)
            .HasId(d => d.Id)
            .HasTenantId(d => d.TenantId)
            .MapField(d => d.Name)
            .MapField(d => d.Amount)
            .Build();

        // Create options
        _options = new MongoDbTenancyOptions
        {
            AutoFilterTenantQueries = true,
            AutoAssignTenantId = true,
            ValidateTenantOnModify = true,
            ThrowOnMissingTenantContext = true
        };

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_collection != null)
        {
            await _collection.DeleteManyAsync(Builders<TenantTestDocument>.Filter.Empty);
        }
    }

    private async Task ClearDataAsync()
    {
        if (_collection != null)
        {
            await _collection.DeleteManyAsync(Builders<TenantTestDocument>.Filter.Empty);
        }
    }

    private void SkipIfNotAvailable()
    {
        if (!_fixture.IsAvailable || _collection == null)
        {
            Assert.Skip("MongoDB container is not available");
        }
    }

    private TenantAwareFunctionalRepositoryMongoDB<TenantTestDocument, Guid> CreateRepository()
    {
        return new TenantAwareFunctionalRepositoryMongoDB<TenantTestDocument, Guid>(
            _collection!,
            _mapping!,
            _tenantProvider!,
            _options!,
            d => d.Id);
    }

    #region Tenant Isolation Tests

    [Fact]
    public async Task ListAsync_WithTenantContext_ReturnsOnlyTenantData()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert documents for both tenants directly
        var tenant1Doc = CreateDocument("Tenant 1 Doc", Tenant1);
        var tenant2Doc = CreateDocument("Tenant 2 Doc", Tenant2);
        await _collection!.InsertManyAsync([tenant1Doc, tenant2Doc]);

        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act
        var result = await repository.ListAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("Tenant 1 Doc");
            list[0].TenantId.ShouldBe(Tenant1);
        });
    }

    [Fact]
    public async Task GetByIdAsync_DifferentTenant_ReturnsNotFound()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert document for tenant 2
        var tenant2Doc = CreateDocument("Tenant 2 Doc", Tenant2);
        await _collection!.InsertOneAsync(tenant2Doc);

        // Set context to tenant 1
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act - Try to get tenant 2's document as tenant 1
        var result = await repository.GetByIdAsync(tenant2Doc.Id);

        // Assert - Should not find due to tenant filter
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task CountAsync_WithTenantContext_CountsOnlyTenantData()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert documents for both tenants
        await _collection!.InsertManyAsync([
            CreateDocument("T1 Doc 1", Tenant1),
            CreateDocument("T1 Doc 2", Tenant1),
            CreateDocument("T2 Doc 1", Tenant2),
            CreateDocument("T2 Doc 2", Tenant2),
            CreateDocument("T2 Doc 3", Tenant2)
        ]);

        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act
        var result = await repository.CountAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2)); // Only tenant 1's 2 documents
    }

    [Fact]
    public async Task AnyAsync_WithMatchingTenantData_ReturnsTrue()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert document for tenant 1
        await _collection!.InsertOneAsync(CreateDocument("T1 Doc", Tenant1));

        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act
        var result = await repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeTrue());
    }

    [Fact]
    public async Task AnyAsync_WithNoMatchingTenantData_ReturnsFalse()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert document for tenant 2 only
        await _collection!.InsertOneAsync(CreateDocument("T2 Doc", Tenant2));

        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act
        var result = await repository.AnyAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(any => any.ShouldBeFalse());
    }

    #endregion

    #region Auto-Assignment Tests

    [Fact]
    public async Task AddAsync_AutoAssignsTenantId()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        var document = new TenantTestDocument
        {
            Id = Guid.NewGuid(),
            Name = "Auto Assign Test",
            Amount = 100m,
            TenantId = string.Empty // Empty - should be auto-assigned
        };

        // Act
        var result = await repository.AddAsync(document);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(d =>
        {
            d.TenantId.ShouldBe(Tenant1);
        });

        // Verify in database
        var filter = Builders<TenantTestDocument>.Filter.Eq(d => d.Id, document.Id);
        var stored = await _collection!.Find(filter).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.TenantId.ShouldBe(Tenant1);
    }

    [Fact]
    public async Task AddRangeAsync_AutoAssignsTenantIdToAll()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        var documents = new[]
        {
            new TenantTestDocument { Id = Guid.NewGuid(), Name = "Batch 1", Amount = 100m, TenantId = string.Empty },
            new TenantTestDocument { Id = Guid.NewGuid(), Name = "Batch 2", Amount = 200m, TenantId = string.Empty }
        };

        // Act
        var result = await repository.AddRangeAsync(documents);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.ShouldAllBe(d => d.TenantId == Tenant1);
        });
    }

    #endregion

    #region Cross-Tenant Access Prevention Tests

    [Fact]
    public async Task UpdateAsync_DifferentTenant_ReturnsNotFound()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert document for tenant 2
        var tenant2Doc = CreateDocument("Tenant 2 Original", Tenant2);
        await _collection!.InsertOneAsync(tenant2Doc);

        // Set context to tenant 1
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act - Try to update tenant 2's document as tenant 1
        tenant2Doc.Name = "Attempted Update";
        var result = await repository.UpdateAsync(tenant2Doc);

        // Assert - Should fail due to tenant validation
        result.IsLeft.ShouldBeTrue();

        // Verify original is unchanged
        var filter = Builders<TenantTestDocument>.Filter.Eq(d => d.Id, tenant2Doc.Id);
        var stored = await _collection!.Find(filter).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.Name.ShouldBe("Tenant 2 Original");
    }

    [Fact]
    public async Task DeleteAsync_DifferentTenant_ReturnsNotFound()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert document for tenant 2
        var tenant2Doc = CreateDocument("Tenant 2 Doc", Tenant2);
        await _collection!.InsertOneAsync(tenant2Doc);

        // Set context to tenant 1
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        // Act - Try to delete tenant 2's document as tenant 1
        var result = await repository.DeleteAsync(tenant2Doc.Id);

        // Assert - Should fail due to tenant filter
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldContain("not found"));

        // Verify document still exists
        var count = await _collection!.CountDocumentsAsync(Builders<TenantTestDocument>.Filter.Empty);
        count.ShouldBe(1);
    }

    #endregion

    #region Tenant Switching Tests

    [Fact]
    public async Task TenantSwitch_ChangesDataVisibility()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert documents for both tenants
        await _collection!.InsertManyAsync([
            CreateDocument("T1 Doc 1", Tenant1),
            CreateDocument("T1 Doc 2", Tenant1),
            CreateDocument("T2 Doc 1", Tenant2)
        ]);

        // Act & Assert - As Tenant 1
        _tenantProvider!.SetTenant(Tenant1);
        var repo1 = CreateRepository();
        var result1 = await repo1.CountAsync();
        result1.IsRight.ShouldBeTrue();
        result1.IfRight(count => count.ShouldBe(2));

        // Act & Assert - Switch to Tenant 2
        _tenantProvider.SetTenant(Tenant2);
        var repo2 = CreateRepository();
        var result2 = await repo2.CountAsync();
        result2.IsRight.ShouldBeTrue();
        result2.IfRight(count => count.ShouldBe(1));
    }

    [Fact]
    public async Task TenantSwitch_AllowsAccessToNewTenantData()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert document for tenant 2
        var tenant2Doc = CreateDocument("Tenant 2 Doc", Tenant2);
        await _collection!.InsertOneAsync(tenant2Doc);

        // Initially as tenant 1 - should not find
        _tenantProvider!.SetTenant(Tenant1);
        var repo1 = CreateRepository();
        var result1 = await repo1.GetByIdAsync(tenant2Doc.Id);
        result1.IsLeft.ShouldBeTrue();

        // Switch to tenant 2 - should find
        _tenantProvider.SetTenant(Tenant2);
        var repo2 = CreateRepository();
        var result2 = await repo2.GetByIdAsync(tenant2Doc.Id);
        result2.IsRight.ShouldBeTrue();
        result2.IfRight(d => d.Name.ShouldBe("Tenant 2 Doc"));
    }

    #endregion

    #region Specification with Tenant Filter Tests

    [Fact]
    public async Task ListAsync_WithSpecification_CombinesTenantAndSpecificationFilters()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange - Insert documents for both tenants with various amounts
        await _collection!.InsertManyAsync([
            CreateDocument("T1 Low", Tenant1, 50m),
            CreateDocument("T1 High", Tenant1, 200m),
            CreateDocument("T2 Low", Tenant2, 50m),
            CreateDocument("T2 High", Tenant2, 200m)
        ]);

        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();
        var spec = new HighValueDocumentSpec();

        // Act
        var result = await repository.ListAsync(spec);

        // Assert - Should only find tenant 1's high-value document
        result.IsRight.ShouldBeTrue();
        result.IfRight(list =>
        {
            list.Count.ShouldBe(1);
            list[0].Name.ShouldBe("T1 High");
            list[0].TenantId.ShouldBe(Tenant1);
        });
    }

    [Fact]
    public async Task CountAsync_WithSpecification_AppliesBothFilters()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        await _collection!.InsertManyAsync([
            CreateDocument("T1 Low", Tenant1, 50m),
            CreateDocument("T1 High 1", Tenant1, 200m),
            CreateDocument("T1 High 2", Tenant1, 300m),
            CreateDocument("T2 High", Tenant2, 200m)
        ]);

        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();
        var spec = new HighValueDocumentSpec();

        // Act
        var result = await repository.CountAsync(spec);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2)); // Only T1's 2 high-value docs
    }

    #endregion

    #region Same Tenant CRUD Tests

    [Fact]
    public async Task UpdateAsync_SameTenant_Succeeds()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        var document = CreateDocument("Original", Tenant1);
        await _collection!.InsertOneAsync(document);

        // Act
        document.Name = "Updated";
        var result = await repository.UpdateAsync(document);

        // Assert
        result.IsRight.ShouldBeTrue();

        var filter = Builders<TenantTestDocument>.Filter.Eq(d => d.Id, document.Id);
        var stored = await _collection!.Find(filter).FirstOrDefaultAsync();
        stored!.Name.ShouldBe("Updated");
    }

    [Fact]
    public async Task DeleteAsync_SameTenant_Succeeds()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        _tenantProvider!.SetTenant(Tenant1);
        var repository = CreateRepository();

        var document = CreateDocument("To Delete", Tenant1);
        await _collection!.InsertOneAsync(document);

        // Act
        var result = await repository.DeleteAsync(document.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        var filter = Builders<TenantTestDocument>.Filter.Eq(d => d.Id, document.Id);
        var stored = await _collection!.Find(filter).FirstOrDefaultAsync();
        stored.ShouldBeNull();
    }

    #endregion

    #region Helper Methods

    private static TenantTestDocument CreateDocument(
        string name,
        string tenantId,
        decimal amount = 100m)
    {
        return new TenantTestDocument
        {
            Id = Guid.NewGuid(),
            Name = name,
            Amount = amount,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}

#region Test Entity and Specifications

/// <summary>
/// Test document for MongoDB tenancy integration tests.
/// </summary>
public class TenantTestDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Specification for high-value documents (Amount > 100).
/// </summary>
public class HighValueDocumentSpec : Specification<TenantTestDocument>
{
    public override Expression<Func<TenantTestDocument, bool>> ToExpression()
        => d => d.Amount > 100;
}

/// <summary>
/// Test tenant provider for integration tests.
/// </summary>
public sealed class TestTenantProvider : ITenantProvider
{
    private string? _currentTenantId;

    public void SetTenant(string tenantId)
    {
        _currentTenantId = tenantId;
    }

    public string? GetCurrentTenantId() => _currentTenantId;

    public ValueTask<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_currentTenantId))
        {
            return ValueTask.FromResult<TenantInfo?>(null);
        }

        return ValueTask.FromResult<TenantInfo?>(new TenantInfo(
            TenantId: _currentTenantId,
            Name: $"Test Tenant {_currentTenantId}",
            Strategy: TenantIsolationStrategy.SharedSchema));
    }
}

#endregion
