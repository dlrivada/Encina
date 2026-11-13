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

    public TenancyDapperWriteAndPagingIntegrationTests(SqlServerFixture fixture) => _fixture = fixture;

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
            .ToTable("TenantTestEntities").HasId(e => e.Id).HasTenantId(e => e.TenantId)
            .MapProperty(e => e.Name, "Name").MapProperty(e => e.Description, "Description")
            .MapProperty(e => e.Amount, "Amount").MapProperty(e => e.IsActive, "IsActive")
            .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc").MapProperty(e => e.UpdatedAtUtc, "UpdatedAtUtc")
            .Build().ShouldBeSuccess();
        _versionedMapping = new TenantEntityMappingBuilder<VersionedTenantItem, Guid>()
            .ToTable("VersionedTenantItems").HasId(e => e.Id).HasTenantId(e => e.TenantId)
            .MapProperty(e => e.Name, "Name").MapProperty(e => e.Version, "Version")
            .Build().ShouldBeSuccess();
        await TenancySchema.ClearTenancyDataAsync((SqlConnection)_connection);
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task GetByIdAsync_FindsOwnEntity_AndSeesOtherTenantsOnlyWithTheFilterDisabled()
    {
        var repository = CreateRepository();
        var entity = await SeedAsync(repository, "found");
        (await repository.GetByIdAsync(entity.Id)).ShouldBeSuccess().Name.ShouldBe("found");

        _tenantProvider.SetCurrentTenant(Tenant2);
        CodeOf(await CreateRepository().GetByIdAsync(entity.Id)).ShouldBe(RepositoryErrors.NotFoundErrorCode);
        var unfiltered = CreateRepository(new DapperTenancyOptions { AutoFilterTenantQueries = false });
        (await unfiltered.GetByIdAsync(entity.Id)).ShouldBeSuccess().TenantId.ShouldBe(Tenant1);
    }

    [Fact]
    public async Task UpdateAsync_PersistsOwnEntity_AndReturnsNotFoundForOneNeverStored()
    {
        var repository = CreateRepository();
        var entity = await SeedAsync(repository, "before");
        entity.Name = "after";

        (await repository.UpdateAsync(entity)).ShouldBeSuccess();

        (await repository.GetByIdAsync(entity.Id)).ShouldBeSuccess().Name.ShouldBe("after");
        var ghost = NewEntity("ghost");
        ghost.TenantId = Tenant1;
        CodeOf(await repository.UpdateAsync(ghost)).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_WithTenantFilterDisabled_UpdatesWithoutTenantScope()
    {
        var entity = await SeedAsync(CreateRepository(), "before");
        entity.Name = "after";
        var repository = CreateRepository(UnscopedOptions());

        (await repository.UpdateAsync(entity)).ShouldBeSuccess();
        CodeOf(await repository.UpdateAsync(NewEntity("ghost"))).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_Versioned_BumpsTheVersion_AndRejectsStaleAndMissingRows()
    {
        var repository = CreateVersionedRepository();
        var item = NewItem("v0");
        await repository.AddAsync(item);
        var stale = new VersionedTenantItem { Id = item.Id, TenantId = Tenant1, Name = "stale", Version = 0 };
        item.Name = "v1";

        (await repository.UpdateAsync(item)).ShouldBeSuccess().Version.ShouldBe(1);

        var stored = (await repository.GetByIdAsync(item.Id)).ShouldBeSuccess();
        stored.Name.ShouldBe("v1");
        stored.Version.ShouldBe(1);
        CodeOf(await repository.UpdateAsync(stale)).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
        var ghost = NewItem("ghost");
        ghost.TenantId = Tenant1;
        CodeOf(await repository.UpdateAsync(ghost)).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_Versioned_WithTenantFilterDisabled_StillDetectsConflictsAndMissingRows()
    {
        var item = NewItem("v0");
        await CreateVersionedRepository().AddAsync(item);
        var repository = CreateVersionedRepository(UnscopedOptions());
        var stale = new VersionedTenantItem { Id = item.Id, TenantId = Tenant1, Name = "stale", Version = 7 };
        item.Name = "ok";

        CodeOf(await repository.UpdateAsync(stale)).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
        CodeOf(await repository.UpdateAsync(NewItem("ghost"))).ShouldBe(RepositoryErrors.NotFoundErrorCode);
        (await repository.UpdateAsync(item)).ShouldBeSuccess();
    }

    [Fact]
    public async Task UpdateRangeAsync_PersistsOwnEntities_RejectsForeignOnesAndReturnsNotFoundForMissingOnes()
    {
        var repository = CreateRepository();
        var first = await SeedAsync(repository, "a");
        var second = await SeedAsync(repository, "b");
        first.Name = "a2";
        second.Name = "b2";

        (await repository.UpdateRangeAsync([first, second])).ShouldBeSuccess();

        (await repository.GetByIdAsync(first.Id)).ShouldBeSuccess().Name.ShouldBe("a2");
        (await repository.GetByIdAsync(second.Id)).ShouldBeSuccess().Name.ShouldBe("b2");
        var foreign = NewEntity("foreign");
        foreign.TenantId = Tenant2;
        first.Name = "changed";
        CodeOf(await repository.UpdateRangeAsync([first, foreign])).ShouldBe(RepositoryErrors.InvalidOperationErrorCode);
        (await repository.GetByIdAsync(first.Id)).ShouldBeSuccess().Name.ShouldBe("a2");
        var ghost = NewEntity("ghost");
        ghost.TenantId = Tenant1;
        CodeOf(await repository.UpdateRangeAsync([ghost])).ShouldBe(RepositoryErrors.NotFoundErrorCode);
    }

    [Fact]
    public async Task UpdateRangeAsync_Versioned_BumpsEveryVersion_AndRejectsStaleVersions()
    {
        var repository = CreateVersionedRepository();
        var first = NewItem("a");
        var second = NewItem("b");
        await repository.AddRangeAsync([first, second]);
        var stale = new VersionedTenantItem { Id = first.Id, TenantId = Tenant1, Name = "stale", Version = 5 };
        first.Name = "a2";
        second.Name = "b2";

        (await repository.UpdateRangeAsync([first, second])).ShouldBeSuccess();

        (await repository.GetByIdAsync(first.Id)).ShouldBeSuccess().Version.ShouldBe(1);
        (await repository.GetByIdAsync(second.Id)).ShouldBeSuccess().Name.ShouldBe("b2");
        CodeOf(await repository.UpdateRangeAsync([stale])).ShouldBe(RepositoryErrors.ConcurrencyConflictErrorCode);
    }

    [Fact]
    public async Task UpdateRangeAsync_Versioned_WithTenantFilterDisabled_UpdatesWithoutTenantScope()
    {
        var item = NewItem("a");
        await CreateVersionedRepository().AddAsync(item);
        item.Name = "a2";

        (await CreateVersionedRepository(UnscopedOptions()).UpdateRangeAsync([item])).ShouldBeSuccess();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsEmptyPage_ThenOnlyTheCurrentTenantPage()
    {
        var repository = CreateRepository();
        var empty = (await repository.GetPagedAsync(new PaginationOptions(1, 2))).ShouldBeSuccess();
        empty.Items.ShouldBeEmpty();
        empty.TotalCount.ShouldBe(0);

        await repository.AddRangeAsync([NewEntity("a"), NewEntity("b"), NewEntity("c")]);
        _tenantProvider.SetCurrentTenant(Tenant2);
        await CreateRepository().AddAsync(NewEntity("other"));
        _tenantProvider.SetCurrentTenant(Tenant1);

        var page = (await repository.GetPagedAsync(new PaginationOptions(1, 2))).ShouldBeSuccess();
        page.Items.Count.ShouldBe(2);
        page.TotalCount.ShouldBe(3);
        page.Items.ShouldAllBe(e => e.TenantId == Tenant1);
        var unfiltered = CreateRepository(new DapperTenancyOptions { AutoFilterTenantQueries = false });
        (await unfiltered.GetPagedAsync(new PaginationOptions(1, 10))).ShouldBeSuccess().TotalCount.ShouldBe(4);
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_ReturnsEmptyAndFilledPages()
    {
        var repository = CreateRepository();
        var spec = new IsActiveTenantSpec();

        var empty = await repository.GetPagedAsync(spec, new PaginationOptions(1, 2));
        await repository.AddRangeAsync([NewEntity("a"), NewEntity("b"), NewEntity("c", isActive: false)]);
        var filled = (await repository.GetPagedAsync(spec, new PaginationOptions(1, 10))).ShouldBeSuccess();

        empty.ShouldBeSuccess().Items.ShouldBeEmpty();
        filled.Items.Count.ShouldBe(2);
        filled.TotalCount.ShouldBe(2);
    }

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => string.Empty, Left: error => error.GetCode().IfNone(string.Empty));

    private static DapperTenancyOptions UnscopedOptions()
        => new() { AutoFilterTenantQueries = false, ValidateTenantOnModify = false };

    private static async Task<TenantTestEntity> SeedAsync(
        TenantAwareFunctionalRepositoryDapper<TenantTestEntity, Guid> repository, string name)
    {
        var entity = NewEntity(name);
        await repository.AddAsync(entity);
        return entity;
    }

    private static TenantTestEntity NewEntity(string name, bool isActive = true)
        => new() { Id = Guid.NewGuid(), TenantId = string.Empty, Name = name, Amount = 10m, IsActive = isActive, CreatedAtUtc = DateTime.UtcNow };

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
