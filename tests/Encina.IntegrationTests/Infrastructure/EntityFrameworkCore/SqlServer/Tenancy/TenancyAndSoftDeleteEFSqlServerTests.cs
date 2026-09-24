using System.Data.Common;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Tenancy;

/// <summary>
/// SQL Server integration tests proving that the tenant isolation filter and the soft-delete
/// filter coexist as two independent named EF Core query filters on an entity that implements
/// both <see cref="ITenantEntity"/> and <see cref="Encina.DomainModeling.ISoftDeletable"/> (#1268).
/// Before the fix, whichever filter was applied last silently replaced the other, so a
/// tenant-scoped soft-deletable entity could leak rows across tenants.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class TenancyAndSoftDeleteEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;
    private readonly TestTenantProvider _tenantProvider = new();
    private readonly EfCoreTenancyOptions _tenancyOptions = new()
    {
        AutoAssignTenantId = false,
        ValidateTenantOnSave = false,
        UseQueryFilters = true,
        ThrowOnMissingTenantContext = false
    };
    private readonly TenancyOptions _coreOptions = new()
    {
        RequireTenant = false
    };

    public TenancyAndSoftDeleteEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        await using var context = CreateContext<TenantThenSoftDeleteSqlServerDbContext>("setup");
        await EnsureSchemaCreatedAsync(context);

        await ClearDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture.IsAvailable)
        {
            await ClearDataAsync();
        }
    }

    [Fact]
    public async Task TenantFilter_AppliedBeforeSoftDelete_OnlyReturnsCurrentTenantNonDeletedRows()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        await SeedTwoTenantsWithSoftDeletedRowsAsync<TenantThenSoftDeleteSqlServerDbContext>();

        await using var queryContext = CreateContext<TenantThenSoftDeleteSqlServerDbContext>("tenant-a");
        var entities = await queryContext.TenantSoftDeleteTestEntities.ToListAsync();

        entities.Count.ShouldBe(1);
        entities.ShouldAllBe(e => e.TenantId == "tenant-a" && !e.IsDeleted);
    }

    [Fact]
    public async Task SoftDeleteFilter_AppliedBeforeTenant_OnlyReturnsCurrentTenantNonDeletedRows()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

        await SeedTwoTenantsWithSoftDeletedRowsAsync<SoftDeleteThenTenantSqlServerDbContext>();

        await using var queryContext = CreateContext<SoftDeleteThenTenantSqlServerDbContext>("tenant-a");
        var entities = await queryContext.TenantSoftDeleteTestEntities.ToListAsync();

        entities.Count.ShouldBe(1);
        entities.ShouldAllBe(e => e.TenantId == "tenant-a" && !e.IsDeleted);
    }

    private async Task SeedTwoTenantsWithSoftDeletedRowsAsync<TContext>()
        where TContext : TenantDbContext
    {
        await using var context = CreateContext<TContext>("seed");
        var set = context.Set<TenantSoftDeleteTestEntity>();

        set.AddRange(
            new TenantSoftDeleteTestEntity { Id = Guid.NewGuid(), TenantId = "tenant-a", Name = "A active", IsDeleted = false },
            new TenantSoftDeleteTestEntity { Id = Guid.NewGuid(), TenantId = "tenant-a", Name = "A deleted", IsDeleted = true, DeletedAtUtc = DateTime.UtcNow },
            new TenantSoftDeleteTestEntity { Id = Guid.NewGuid(), TenantId = "tenant-b", Name = "B active", IsDeleted = false },
            new TenantSoftDeleteTestEntity { Id = Guid.NewGuid(), TenantId = "tenant-b", Name = "B deleted", IsDeleted = true, DeletedAtUtc = DateTime.UtcNow });

        await context.SaveChangesAsync();
    }

    private TContext CreateContext<TContext>(string tenantId)
        where TContext : TenantDbContext
    {
        _tenantProvider.SetTenant(tenantId);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseSqlServer(_fixture.ConnectionString);

        return (TContext)Activator.CreateInstance(
            typeof(TContext),
            optionsBuilder.Options,
            _tenantProvider,
            Options.Create(_tenancyOptions),
            Options.Create(_coreOptions))!;
    }

    private async Task ClearDataAsync()
    {
        try
        {
            await using var context = CreateContext<TenantThenSoftDeleteSqlServerDbContext>("admin");
            var allEntities = await context.TenantSoftDeleteTestEntities.IgnoreQueryFilters().ToListAsync();
            context.TenantSoftDeleteTestEntities.RemoveRange(allEntities);
            await context.SaveChangesAsync();
        }
        catch
        {
            // Ignore if the table doesn't exist yet.
        }
    }

    // Mirrors Encina.TestInfrastructure.Fixtures.EntityFrameworkCore.EFCoreSchema.CreateTablesAsync
    // (internal, not visible here): Database.EnsureCreatedAsync() does nothing once the shared
    // collection database already has any table (#1094), so contexts whose tables are not part of
    // an earlier test class's schema never get created without this.
    private static async Task EnsureSchemaCreatedAsync(TenantDbContext context)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync())
        {
            await creator.CreateAsync();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await creator.CreateTablesAsync();
            await transaction.CommitAsync();
        }
        catch (DbException ex) when (ex is SqlException { Number: 2714 })
        {
            // The table already exists (created by an earlier test class or the base schema).
            await transaction.RollbackAsync();
        }
    }
}
