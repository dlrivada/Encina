using Encina.DomainModeling;
using Encina.MongoDB.Tenancy;
using Encina.Tenancy;
using Encina.Testing.Shouldly;
using LanguageExt;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Tenancy;

/// <summary>
/// Unit tests for the missing tenant context behaviour of
/// <see cref="TenantAwareFunctionalRepositoryMongoDB{TEntity, TId}"/> (#2938): every operation returns
/// <c>Repository.TenantRequired</c> as a <c>Left</c>, never throws, and never reaches the collection.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TenantAwareFunctionalRepositoryMongoDBTenantRequiredTests
{
    public static TheoryData<string, string?> OperationsAndMissingTenantIds()
    {
        var data = new TheoryData<string, string?>();
        foreach (var tenantId in new string?[] { null, string.Empty })
        {
            foreach (var operation in AllOperations)
            {
                data.Add(operation, tenantId);
            }
        }

        return data;
    }

    private static readonly string[] AllOperations =
    [
        "GetById", "List", "ListSpec", "FirstOrDefault", "Count", "CountSpec", "Any", "AnySpec",
        "GetPaged", "GetPagedSpec", "GetPagedPagedSpec", "Add", "AddRange", "AddRangeEmpty", "Update",
        "UpdateNoOwnershipCheck", "UpdateRange", "UpdateRangeNoOwnershipCheck", "DeleteById", "DeleteEntity", "DeleteRange"
    ];

    [Theory]
    [MemberData(nameof(OperationsAndMissingTenantIds))]
    public async Task Operation_WithoutTenantContext_ReturnsTenantRequiredLeft_WithoutTouchingTheCollection(
        string operation,
        string? tenantId)
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        var validateOnModify = !operation.EndsWith("NoOwnershipCheck", StringComparison.Ordinal);
        var repository = CreateRepository(collection, tenantId, new MongoDbTenancyOptions
        {
            ThrowOnMissingTenantContext = true,
            ValidateTenantOnModify = validateOnModify
        });
        var order = new MongoTenantTestOrder { Id = Guid.NewGuid(), IsActive = true };

        // Act
        var error = await RunAsync(repository, operation, order);

        // Assert
        error.ShouldNotBeNull();
        error.Value.GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.TenantRequiredErrorCode);
        error.Value.Message.ShouldNotContain("tenant-");
        collection.ReceivedCalls().ShouldBeEmpty();
        order.TenantId.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task AddAsync_WithoutTenantContext_AndOptOut_StoresEntityWithoutAssigningTenant()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        var repository = CreateRepository(collection, null, new MongoDbTenancyOptions { ThrowOnMissingTenantContext = false });
        var order = new MongoTenantTestOrder { Id = Guid.NewGuid() };

        // Act
        var result = await repository.AddAsync(order);

        // Assert
        result.ShouldBeSuccess();
        order.TenantId.ShouldBe(string.Empty);
        await collection.Received(1).InsertOneAsync(order, Arg.Any<InsertOneOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_WithTenantContext_AssignsTenant()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        var repository = CreateRepository(collection, "tenant-1", new MongoDbTenancyOptions());
        var order = new MongoTenantTestOrder { Id = Guid.NewGuid() };

        // Act
        var result = await repository.AddAsync(order);

        // Assert
        result.ShouldBeSuccess();
        order.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public async Task CountAsync_WithoutTenantContext_AndOptOut_ReachesTheCollection()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        collection.CountDocumentsAsync(
                Arg.Any<FilterDefinition<MongoTenantTestOrder>>(),
                Arg.Any<CountOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(3L));
        var repository = CreateRepository(collection, null, new MongoDbTenancyOptions { ThrowOnMissingTenantContext = false });

        // Act
        var result = await repository.CountAsync();

        // Assert
        result.ShouldBeSuccess().ShouldBe(3);
    }

    [Fact]
    public async Task DeleteAsync_WithoutTenantContext_AndOptOut_ReachesTheCollection()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        collection.DeleteOneAsync(
                Arg.Any<FilterDefinition<MongoTenantTestOrder>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));
        var repository = CreateRepository(collection, null, new MongoDbTenancyOptions { ThrowOnMissingTenantContext = false });

        // Act
        var result = await repository.DeleteAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task UpdateAsync_WithoutTenantContext_AndOptOut_IsNotRejected()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<MongoTenantTestOrder>>(),
                Arg.Any<MongoTenantTestOrder>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));
        var repository = CreateRepository(collection, null, new MongoDbTenancyOptions { ThrowOnMissingTenantContext = false });

        // Act
        var result = await repository.UpdateAsync(new MongoTenantTestOrder { Id = Guid.NewGuid() });

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task UpdateAsync_WithDifferentTenant_StillReturnsInvalidOperation()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        var repository = CreateRepository(collection, "tenant-1", new MongoDbTenancyOptions());

        // Act
        var result = await repository.UpdateAsync(new MongoTenantTestOrder { Id = Guid.NewGuid(), TenantId = "tenant-2" });

        // Assert
        var error = result.ShouldBeError();
        error.GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.InvalidOperationErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_WithMatchingDocument_Succeeds()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<MongoTenantTestOrder>>(),
                Arg.Any<MongoTenantTestOrder>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));
        var repository = CreateRepository(collection, "tenant-1", new MongoDbTenancyOptions());
        var order = new MongoTenantTestOrder { Id = Guid.NewGuid(), TenantId = "tenant-1" };

        // Act
        var result = await repository.UpdateAsync(order);

        // Assert
        result.ShouldBeSuccess().ShouldBeSameAs(order);
    }

    [Fact]
    public async Task UpdateAsync_WithoutMatchingDocument_ReturnsNotFound()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<MongoTenantTestOrder>>(),
                Arg.Any<MongoTenantTestOrder>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(0, 0, null)));
        var repository = CreateRepository(collection, "tenant-1", new MongoDbTenancyOptions());

        // Act
        var result = await repository.UpdateAsync(new MongoTenantTestOrder { Id = Guid.NewGuid(), TenantId = "tenant-1" });

        // Assert
        result.ShouldBeError().GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_VersionedEntity_BumpsVersionAndReportsConflictOnNoMatch()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<VersionedTenantOrder>>();
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<VersionedTenantOrder>>(),
                Arg.Any<VersionedTenantOrder>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(0, 0, null)));
        var repository = CreateVersionedRepository(collection, "tenant-1");
        var order = new VersionedTenantOrder { Id = Guid.NewGuid(), TenantId = "tenant-1", Version = 4 };

        // Act
        var result = await repository.UpdateAsync(order);

        // Assert
        result.ShouldBeError().GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
        order.Version.ShouldBe(5);
    }

    [Fact]
    public async Task UpdateAsync_ReadOnlyVersionedEntity_ReportsConflictOnNoMatchWithoutBumpingVersion()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<ReadOnlyVersionedTenantOrder>>();
        collection.ReplaceOneAsync(
                Arg.Any<FilterDefinition<ReadOnlyVersionedTenantOrder>>(),
                Arg.Any<ReadOnlyVersionedTenantOrder>(),
                Arg.Any<ReplaceOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(0, 0, null)));
        var repository = CreateRepositoryFor(collection, "tenant-1");
        var order = new ReadOnlyVersionedTenantOrder { Id = Guid.NewGuid(), TenantId = "tenant-1", Version = 7 };

        // Act
        var result = await repository.UpdateAsync(order);

        // Assert
        result.ShouldBeError().GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
        order.Version.ShouldBe(7);
    }

    [Fact]
    public async Task UpdateRangeAsync_NonVersionedEntities_Succeeds()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        collection.BulkWriteAsync(
                Arg.Any<IEnumerable<WriteModel<MongoTenantTestOrder>>>(),
                Arg.Any<BulkWriteOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<BulkWriteResult<MongoTenantTestOrder>>(
                new BulkWriteResult<MongoTenantTestOrder>.Acknowledged(
                    1, 0, 0, 0, 0, call.Arg<IEnumerable<WriteModel<MongoTenantTestOrder>>>().ToList(), [])));
        var repository = CreateRepository(collection, "tenant-1", new MongoDbTenancyOptions());

        // Act
        var result = await repository.UpdateRangeAsync([new MongoTenantTestOrder { Id = Guid.NewGuid(), TenantId = "tenant-1" }]);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task UpdateRangeAsync_EmptyList_SucceedsWithoutBulkWrite()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<MongoTenantTestOrder>>();
        var repository = CreateRepository(collection, "tenant-1", new MongoDbTenancyOptions());

        // Act
        var result = await repository.UpdateRangeAsync([]);

        // Assert
        result.ShouldBeSuccess();
        collection.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateRangeAsync_VersionedEntities_ReportsConflictWhenFewerDocumentsMatch()
    {
        // Arrange
        var collection = Substitute.For<IMongoCollection<VersionedTenantOrder>>();
        collection.BulkWriteAsync(
                Arg.Any<IEnumerable<WriteModel<VersionedTenantOrder>>>(),
                Arg.Any<BulkWriteOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<BulkWriteResult<VersionedTenantOrder>>(
                new BulkWriteResult<VersionedTenantOrder>.Acknowledged(
                    2, 1, 0, 0, 1, call.Arg<IEnumerable<WriteModel<VersionedTenantOrder>>>().ToList(), [])));
        var repository = CreateVersionedRepository(collection, "tenant-1");
        var first = new VersionedTenantOrder { Id = Guid.NewGuid(), TenantId = "tenant-1", Version = 1 };
        var second = new VersionedTenantOrder { Id = Guid.NewGuid(), TenantId = "tenant-1", Version = 2 };

        // Act
        var result = await repository.UpdateRangeAsync([first, second]);

        // Assert
        result.ShouldBeError().GetCode().IfNone(string.Empty).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
        first.Version.ShouldBe(2);
        second.Version.ShouldBe(3);
    }

    [Fact]
    public void TenantRequired_Error_HasStableCodeDetailsAndNoTenantId()
    {
        // Act
        var error = RepositoryErrors.TenantRequired<MongoTenantTestOrder>("Add");

        // Assert
        RepositoryErrors.TenantRequiredErrorCode.ShouldBe("Repository.TenantRequired");
        error.GetCode().IfNone(string.Empty).ShouldBe("Repository.TenantRequired");
        error.Message.ShouldContain("'Add'");
        error.Message.ShouldContain(nameof(MongoTenantTestOrder));
        var details = error.GetDetails();
        details["EntityType"].ShouldBe(nameof(MongoTenantTestOrder));
        details["Operation"].ShouldBe("Add");
    }

    private static TenantAwareFunctionalRepositoryMongoDB<MongoTenantTestOrder, Guid> CreateRepository(
        IMongoCollection<MongoTenantTestOrder> collection,
        string? tenantId,
        MongoDbTenancyOptions options)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var mapping = new TenantEntityMappingBuilder<MongoTenantTestOrder, Guid>()
            .ToCollection("orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .MapField(o => o.Total)
            .MapField(o => o.IsActive)
            .Build()
            .ShouldBeSuccess();

        return new TenantAwareFunctionalRepositoryMongoDB<MongoTenantTestOrder, Guid>(
            collection, mapping, tenantProvider, options, o => o.Id);
    }

    private static TenantAwareFunctionalRepositoryMongoDB<VersionedTenantOrder, Guid> CreateVersionedRepository(
        IMongoCollection<VersionedTenantOrder> collection,
        string tenantId)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var mapping = new TenantEntityMappingBuilder<VersionedTenantOrder, Guid>()
            .ToCollection("versioned_orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeSuccess();

        return new TenantAwareFunctionalRepositoryMongoDB<VersionedTenantOrder, Guid>(
            collection, mapping, tenantProvider, new MongoDbTenancyOptions(), o => o.Id);
    }

    private static TenantAwareFunctionalRepositoryMongoDB<ReadOnlyVersionedTenantOrder, Guid> CreateRepositoryFor(
        IMongoCollection<ReadOnlyVersionedTenantOrder> collection,
        string tenantId)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var mapping = new TenantEntityMappingBuilder<ReadOnlyVersionedTenantOrder, Guid>()
            .ToCollection("versioned_orders")
            .HasId(o => o.Id)
            .HasTenantId(o => o.TenantId)
            .Build()
            .ShouldBeSuccess();

        return new TenantAwareFunctionalRepositoryMongoDB<ReadOnlyVersionedTenantOrder, Guid>(
            collection, mapping, tenantProvider, new MongoDbTenancyOptions(), o => o.Id);
    }

    private static async Task<EncinaError?> RunAsync(
        TenantAwareFunctionalRepositoryMongoDB<MongoTenantTestOrder, Guid> repository,
        string operation,
        MongoTenantTestOrder order)
    {
        var spec = new MongoActiveOrdersSpec();
        var page = new PaginationOptions(1, 10);

        return operation switch
        {
            "GetById" => ErrorOf(await repository.GetByIdAsync(order.Id)),
            "List" => ErrorOf(await repository.ListAsync()),
            "ListSpec" => ErrorOf(await repository.ListAsync(spec)),
            "FirstOrDefault" => ErrorOf(await repository.FirstOrDefaultAsync(spec)),
            "Count" => ErrorOf(await repository.CountAsync()),
            "CountSpec" => ErrorOf(await repository.CountAsync(spec)),
            "Any" => ErrorOf(await repository.AnyAsync()),
            "AnySpec" => ErrorOf(await repository.AnyAsync(spec)),
            "GetPaged" => ErrorOf(await repository.GetPagedAsync(page)),
            "GetPagedSpec" => ErrorOf(await repository.GetPagedAsync(spec, page)),
            "GetPagedPagedSpec" => ErrorOf(await repository.GetPagedAsync(new PagedActiveOrdersSpec(page))),
            "Add" => ErrorOf(await repository.AddAsync(order)),
            "AddRange" => ErrorOf(await repository.AddRangeAsync([order])),
            "AddRangeEmpty" => ErrorOf(await repository.AddRangeAsync([])),
            "Update" or "UpdateNoOwnershipCheck" => ErrorOf(await repository.UpdateAsync(order)),
            "UpdateRange" or "UpdateRangeNoOwnershipCheck" => ErrorOf(await repository.UpdateRangeAsync([order])),
            "DeleteById" => ErrorOf(await repository.DeleteAsync(order.Id)),
            "DeleteEntity" => ErrorOf(await repository.DeleteAsync(order)),
            "DeleteRange" => ErrorOf(await repository.DeleteRangeAsync(spec)),
            _ => throw new InvalidOperationException($"Unknown operation {operation}")
        };
    }

    private static EncinaError? ErrorOf<T>(Either<EncinaError, T> either)
        => either.Match<EncinaError?>(Right: _ => null, Left: error => error);

    public sealed class VersionedTenantOrder : IVersionedEntity
    {
        public Guid Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public int Version { get; set; }

        long IVersioned.Version => Version;
    }

    public sealed class ReadOnlyVersionedTenantOrder : IVersioned
    {
        public Guid Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public long Version { get; set; }
    }

    private sealed class PagedActiveOrdersSpec : Specification<MongoTenantTestOrder>, IPagedSpecification<MongoTenantTestOrder>
    {
        public PagedActiveOrdersSpec(PaginationOptions pagination) => Pagination = pagination;

        public PaginationOptions Pagination { get; }

        public override System.Linq.Expressions.Expression<Func<MongoTenantTestOrder, bool>> ToExpression()
            => o => o.IsActive;
    }
}
