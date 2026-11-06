using System.Data;
using Dapper;
using Encina.Dapper.SqlServer.Tenancy;
using Encina.DomainModeling;
using Encina.Tenancy;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.SqlServer.Tenancy;

/// <summary>
/// Integration tests for the update, versioned update, update range, get-by-id and paging paths of the
/// tenant-aware Dapper SQL Server repository, with the tenant filter applied and with it switched off (#3300).
/// </summary>
[Collection("Dapper-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class TenancyDapperWriteAndPagingIntegrationTests : IAsyncLifetime
{
    private const string Tenant1 = "tenant-w01";
    private const string Tenant2 = "tenant-w02";

    private readonly SqlServerFixture _fixture;
    private readonly TestTenantProvider _tenantProvider = new(Tenant1);
    private IDbConnection _connection = null!;
    private ITenantEntityMapping<TenantTestEntity, Guid> _mapping = null!;
    private ITenantEntityMapping<VersionedTenantItem, Guid> _versionedMapping = null!;

    public TenancyDapperWriteAndPagingIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        using var schemaConnection = (SqlConnection)_fixture.CreateConnection();
        await TenancySchema.CreateTenantTestEntitiesSchemaAsync(schemaConnection);
        await schemaConnection.ExecuteAsync(
            """
            IF OBJECT_ID(N'dbo.VersionedTenantItems', N'U') IS NULL
            CREATE TABLE dbo.VersionedTenantItems (
                Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                TenantId NVARCHAR(100) NOT NULL,
                Name NVARCHAR(200) NOT NULL,
                Version INT NOT NULL
            );
            DELETE FROM dbo.VersionedTenantItems;
            """);

        _connection = _fixture.CreateConnection();

        _mapping = new TenantEntityMappingBuilder<TenantTestEntity, Guid>()
            .ToTable("TenantTestEntities")
            .HasId(e => e.Id)
            .HasTenantId(e => e.TenantId)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Description, "Description")
            .MapProperty(e => e.Amount, "Amount")
            .MapProperty(e => e.IsActive, "IsActive")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc")
            .MapProperty(e => e.UpdatedAtUtc, "UpdatedAtUtc")
            .Build()
            .ShouldBeSuccess();

        _versionedMapping = new TenantEntityMappingBuilder<VersionedTenantItem, Guid>()
            .ToTable("VersionedTenantItems")
            .HasId(e => e.Id)
            .HasTenantId(e => e.TenantId)
            .MapProperty(e => e.Name, "Name")
            .MapProperty(e => e.Version, "Version")
            .Build()
            .ShouldBeSuccess();

        await TenancySchema.ClearTenancyDataAsync((SqlConnection)_connection);
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    #region GetById

    [Fact]
    public async Task GetByIdAsync_FindsTheEntityOfTheCurrentTenant()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = NewEntity("found");
        await repository.AddAsync(entity);

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.ShouldBeSuccess().Name.ShouldBe("found");
    }

    [Fact]
    public async Task GetByIdAsync_WithTenantFilterDisabled_SeesEntitiesOfOtherTenants()
    {
        // Arrange
        var entity = NewEntity("other-tenant");
        await CreateRepository().AddAsync(entity);
        _tenantProvider.SetCurrentTenant(Tenant2);
        var repository = CreateRepository(new DapperTenancyOptions { AutoFilterTenantQueries = false });

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        result.ShouldBeSuccess().TenantId.ShouldBe(Tenant1);
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateAsync_OwnEntity_PersistsTheChange()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = NewEntity("before");
        await repository.AddAsync(entity);
        entity.Name = "after";

        // Act
        var result = await repository.UpdateAsync(entity);

        // Assert
        result.ShouldBeSuccess();
        (await repository.GetByIdAsync(entity.Id)).ShouldBeSuccess().Name.ShouldBe("after");
    }

    [Fact]
    public async Task UpdateAsync_EntityThatWasNeverStored_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = NewEntity("ghost");
        entity.TenantId = Tenant1;

        // Act
        var result = await repository.UpdateAsync(entity);

        // Assert
        CodeOf(result).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_WithTenantFilterDisabled_UpdatesWithoutTenantScope()
    {
        // Arrange
        var entity = NewEntity("before");
        await CreateRepository().AddAsync(entity);
        entity.Name = "after";
        var repository = CreateRepository(UnscopedOptions());

        // Act
        var result = await repository.UpdateAsync(entity);
        var ghost = await repository.UpdateAsync(NewEntity("ghost"));

        // Assert
        result.ShouldBeSuccess();
        CodeOf(ghost).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    #endregion

    #region Versioned update

    [Fact]
    public async Task UpdateAsync_Versioned_BumpsTheVersion()
    {
        // Arrange
        var repository = CreateVersionedRepository();
        var item = NewItem("v0");
        await repository.AddAsync(item);
        item.Name = "v1";

        // Act
        var result = await repository.UpdateAsync(item);

        // Assert
        result.ShouldBeSuccess().Version.ShouldBe(1);
        var stored = (await repository.GetByIdAsync(item.Id)).ShouldBeSuccess();
        stored.Name.ShouldBe("v1");
        stored.Version.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_Versioned_StaleVersion_ReturnsConcurrencyConflict()
    {
        // Arrange
        var repository = CreateVersionedRepository();
        var item = NewItem("v0");
        await repository.AddAsync(item);
        var stale = new VersionedTenantItem { Id = item.Id, TenantId = Tenant1, Name = "stale", Version = 0 };
        item.Name = "winner";
        (await repository.UpdateAsync(item)).ShouldBeSuccess();

        // Act
        var result = await repository.UpdateAsync(stale);

        // Assert
        CodeOf(result).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_Versioned_EntityThatWasNeverStored_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateVersionedRepository();
        var ghost = NewItem("ghost");
        ghost.TenantId = Tenant1;

        // Act
        var result = await repository.UpdateAsync(ghost);

        // Assert
        CodeOf(result).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_Versioned_WithTenantFilterDisabled_StillDetectsConflictsAndMissingRows()
    {
        // Arrange
        var item = NewItem("v0");
        await CreateVersionedRepository().AddAsync(item);
        var repository = CreateVersionedRepository(UnscopedOptions());
        var stale = new VersionedTenantItem { Id = item.Id, TenantId = Tenant1, Name = "stale", Version = 7 };

        // Act
        var conflict = await repository.UpdateAsync(stale);
        var missing = await repository.UpdateAsync(NewItem("ghost"));
        item.Name = "ok";
        var updated = await repository.UpdateAsync(item);

        // Assert
        CodeOf(conflict).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
        CodeOf(missing).ShouldBe(RepositoryErrors.NotFoundErrorCode);
        updated.ShouldBeSuccess();
    }

    #endregion

    #region UpdateRange

    [Fact]
    public async Task UpdateRangeAsync_OwnEntities_PersistsEveryChange()
    {
        // Arrange
        var repository = CreateRepository();
        var first = NewEntity("a");
        var second = NewEntity("b");
        await repository.AddRangeAsync([first, second]);
        first.Name = "a2";
        second.Name = "b2";

        // Act
        var result = await repository.UpdateRangeAsync([first, second]);

        // Assert
        result.ShouldBeSuccess();
        (await repository.GetByIdAsync(first.Id)).ShouldBeSuccess().Name.ShouldBe("a2");
        (await repository.GetByIdAsync(second.Id)).ShouldBeSuccess().Name.ShouldBe("b2");
    }

    [Fact]
    public async Task UpdateRangeAsync_WithAnEntityOfAnotherTenant_ReturnsInvalidOperationAndChangesNothing()
    {
        // Arrange
        var repository = CreateRepository();
        var mine = NewEntity("mine");
        await repository.AddAsync(mine);
        var foreign = NewEntity("foreign");
        foreign.TenantId = Tenant2;
        mine.Name = "changed";

        // Act
        var result = await repository.UpdateRangeAsync([mine, foreign]);

        // Assert
        CodeOf(result).ShouldBe(RepositoryErrors.InvalidOperationErrorCode);
        (await repository.GetByIdAsync(mine.Id)).ShouldBeSuccess().Name.ShouldBe("mine");
    }

    [Fact]
    public async Task UpdateRangeAsync_NonVersionedEntityThatWasNeverStored_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateRepository();
        var ghost = NewEntity("ghost");
        ghost.TenantId = Tenant1;

        // Act
        var result = await repository.UpdateRangeAsync([ghost]);

        // Assert
        CodeOf(result).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateRangeAsync_Versioned_BumpsEveryVersion()
    {
        // Arrange
        var repository = CreateVersionedRepository();
        var first = NewItem("a");
        var second = NewItem("b");
        await repository.AddRangeAsync([first, second]);
        first.Name = "a2";
        second.Name = "b2";

        // Act
        var result = await repository.UpdateRangeAsync([first, second]);

        // Assert
        result.ShouldBeSuccess();
        (await repository.GetByIdAsync(first.Id)).ShouldBeSuccess().Version.ShouldBe(1);
        (await repository.GetByIdAsync(second.Id)).ShouldBeSuccess().Name.ShouldBe("b2");
    }

    [Fact]
    public async Task UpdateRangeAsync_Versioned_StaleVersions_ReturnsConcurrencyConflict()
    {
        // Arrange
        var repository = CreateVersionedRepository();
        var item = NewItem("a");
        await repository.AddAsync(item);
        var stale = new VersionedTenantItem { Id = item.Id, TenantId = Tenant1, Name = "stale", Version = 5 };

        // Act
        var result = await repository.UpdateRangeAsync([stale]);

        // Assert
        CodeOf(result).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
    }

    [Fact]
    public async Task UpdateRangeAsync_Versioned_WithTenantFilterDisabled_UpdatesWithoutTenantScope()
    {
        // Arrange
        var item = NewItem("a");
        await CreateVersionedRepository().AddAsync(item);
        item.Name = "a2";
        var repository = CreateVersionedRepository(UnscopedOptions());

        // Act
        var result = await repository.UpdateRangeAsync([item]);

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion

    #region GetPaged

    [Fact]
    public async Task GetPagedAsync_WithoutRows_ReturnsAnEmptyPage()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.GetPagedAsync(new PaginationOptions(1, 2));

        // Assert
        var page = result.ShouldBeSuccess();
        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsOnlyTheCurrentTenantPage()
    {
        // Arrange
        var repository = CreateRepository();
        await repository.AddRangeAsync([NewEntity("a"), NewEntity("b"), NewEntity("c")]);
        _tenantProvider.SetCurrentTenant(Tenant2);
        await CreateRepository().AddAsync(NewEntity("other"));
        _tenantProvider.SetCurrentTenant(Tenant1);

        // Act
        var result = await repository.GetPagedAsync(new PaginationOptions(1, 2));

        // Assert
        var page = result.ShouldBeSuccess();
        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(3);
        page.Items.ShouldAllBe(e => e.TenantId == Tenant1);
    }

    [Fact]
    public async Task GetPagedAsync_WithTenantFilterDisabled_CountsEveryTenant()
    {
        // Arrange
        await CreateRepository().AddAsync(NewEntity("a"));
        _tenantProvider.SetCurrentTenant(Tenant2);
        var repository = CreateRepository(new DapperTenancyOptions { AutoFilterTenantQueries = false });
        await repository.AddAsync(NewEntity("b"));

        // Act
        var result = await repository.GetPagedAsync(new PaginationOptions(1, 10));

        // Assert
        result.ShouldBeSuccess().TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_ReturnsEmptyAndFilledPages()
    {
        // Arrange
        var repository = CreateRepository();
        var spec = new IsActiveTenantSpec();

        // Act
        var empty = await repository.GetPagedAsync(spec, new PaginationOptions(1, 2));
        await repository.AddRangeAsync([NewEntity("a"), NewEntity("b"), NewEntity("c", isActive: false)]);
        var filled = await repository.GetPagedAsync(spec, new PaginationOptions(1, 10));

        // Assert
        empty.ShouldBeSuccess().Items.ShouldBeEmpty();
        var page = filled.ShouldBeSuccess();
        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(2);
    }

    #endregion

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(
            Right: _ => string.Empty,
            Left: error => error.GetCode().IfNone(string.Empty));

    private static DapperTenancyOptions UnscopedOptions()
        => new() { AutoFilterTenantQueries = false, ValidateTenantOnModify = false };

    private static TenantTestEntity NewEntity(string name, bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = string.Empty,
            Name = name,
            Amount = 10m,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static VersionedTenantItem NewItem(string name)
        => new() { Id = Guid.NewGuid(), TenantId = string.Empty, Name = name };

    private TenantAwareFunctionalRepositoryDapper<TenantTestEntity, Guid> CreateRepository(DapperTenancyOptions? options = null)
        => new(_connection, _mapping, _tenantProvider, options ?? new DapperTenancyOptions());

    private TenantAwareFunctionalRepositoryDapper<VersionedTenantItem, Guid> CreateVersionedRepository(DapperTenancyOptions? options = null)
        => new(_connection, _versionedMapping, _tenantProvider, options ?? new DapperTenancyOptions());

    public sealed class VersionedTenantItem : ITenantEntity, IVersionedEntity
    {
        public Guid Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Version { get; set; }

        long IVersioned.Version => Version;
    }
}
