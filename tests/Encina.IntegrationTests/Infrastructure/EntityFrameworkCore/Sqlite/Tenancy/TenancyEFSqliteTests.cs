using System.Diagnostics.CodeAnalysis;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Sqlite.Tenancy;

/// <summary>
/// SQLite-specific integration tests for EF Core multi-tenancy support.
/// Tests global query filters, automatic tenant assignment, and tenant isolation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
[Collection("EFCore-Sqlite")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Connection is disposed in DisposeAsync")]
public sealed class TenancyEFSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;
    private readonly TestTenantProvider _tenantProvider = new();
    private readonly EfCoreTenancyOptions _tenancyOptions = new()
    {
        AutoAssignTenantId = true,
        ValidateTenantOnSave = true,
        UseQueryFilters = true,
        ThrowOnMissingTenantContext = true
    };
    private readonly TenancyOptions _coreOptions = new()
    {
        RequireTenant = true
    };
    private SqliteConnection? _sharedConnection;

    public TenancyEFSqliteTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create a shared connection for SQLite in-memory
        _sharedConnection = new SqliteConnection(_fixture.ConnectionString);
        await _sharedConnection.OpenAsync();

        // Ensure schema is created
        await using var context = CreateDbContextForTenant("setup");
        await context.Database.EnsureCreatedAsync();

        // Clear any existing data
        await ClearDataAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearDataAsync();
        if (_sharedConnection is not null)
        {
            await _sharedConnection.DisposeAsync();
        }
    }

    private TenantTestDbContext CreateDbContextForTenant(string tenantId)
    {
        _tenantProvider.SetTenant(tenantId);

        var optionsBuilder = new DbContextOptionsBuilder<TenantTestDbContext>();
        optionsBuilder.UseSqlite(_sharedConnection!);

        return new TenantTestDbContext(
            optionsBuilder.Options,
            _tenantProvider,
            Options.Create(_tenancyOptions),
            Options.Create(_coreOptions));
    }

    private async Task ClearDataAsync()
    {
        try
        {
            // Use IgnoreQueryFilters to delete all data
            _tenantProvider.SetTenant("admin");
            await using var context = CreateDbContextForTenant("admin");

            // Delete all entities ignoring tenant filter
            var allEntities = await context.TenantTestEntities.IgnoreQueryFilters().ToListAsync();
            context.TenantTestEntities.RemoveRange(allEntities);
            await context.SaveChangesAsync();
        }
        catch
        {
            // Ignore if table doesn't exist yet
        }
    }

    #region Query Filter Tests

    [Fact]
    public async Task TenantFilter_ShouldOnlyReturnCurrentTenantData()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        // Add data for tenant 1
        await using (var context1 = CreateDbContextForTenant(tenant1))
        {
            context1.TenantTestEntities.AddRange(
                CreateEntity(tenant1, "Tenant 1 Entity 1"),
                CreateEntity(tenant1, "Tenant 1 Entity 2"));
            await context1.SaveChangesAsync();
        }

        // Add data for tenant 2
        await using (var context2 = CreateDbContextForTenant(tenant2))
        {
            context2.TenantTestEntities.Add(CreateEntity(tenant2, "Tenant 2 Entity"));
            await context2.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        await using var queryContext = CreateDbContextForTenant(tenant1);
        var entities = await queryContext.TenantTestEntities.ToListAsync();

        // Assert
        entities.Count.ShouldBe(2);
        entities.ShouldAllBe(e => e.TenantId == tenant1);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnAllTenantData()
    {
        // Arrange
        const string tenant1 = "filter-tenant-1";
        const string tenant2 = "filter-tenant-2";

        await using (var context1 = CreateDbContextForTenant(tenant1))
        {
            context1.TenantTestEntities.Add(CreateEntity(tenant1, "Tenant 1"));
            await context1.SaveChangesAsync();
        }

        await using (var context2 = CreateDbContextForTenant(tenant2))
        {
            context2.TenantTestEntities.Add(CreateEntity(tenant2, "Tenant 2"));
            await context2.SaveChangesAsync();
        }

        // Act - Query ignoring filters
        await using var queryContext = CreateDbContextForTenant(tenant1);
        var allEntities = await queryContext.TenantTestEntities.IgnoreQueryFilters().ToListAsync();

        // Assert - Should see entities from both tenants
        allEntities.ShouldContain(e => e.TenantId == tenant1);
        allEntities.ShouldContain(e => e.TenantId == tenant2);
    }

    #endregion

    #region Auto-Assignment Tests

    [Fact]
    public async Task NewEntity_ShouldAutoAssignTenantId()
    {
        // Arrange
        const string tenantId = "auto-assign-tenant";
        await using var context = CreateDbContextForTenant(tenantId);
        await context.Database.EnsureCreatedAsync();

        var entity = new TenantTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Auto Assign Test",
            Amount = 100m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
            // Note: TenantId is not set - should be auto-assigned
        };
        context.TenantTestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task NewEntity_WithExplicitTenantId_ShouldUseProvidedValue()
    {
        // Arrange - Using options that don't auto-assign
        const string contextTenant = "context-tenant";
        const string explicitTenant = "explicit-tenant";

        _tenantProvider.SetTenant(contextTenant);

        var noAutoAssignOptions = new EfCoreTenancyOptions
        {
            AutoAssignTenantId = false,
            ValidateTenantOnSave = false,
            UseQueryFilters = true,
            ThrowOnMissingTenantContext = false
        };

        var optionsBuilder = new DbContextOptionsBuilder<TenantTestDbContext>();
        optionsBuilder.UseSqlite(_sharedConnection!);

        await using var context = new TenantTestDbContext(
            optionsBuilder.Options,
            _tenantProvider,
            Options.Create(noAutoAssignOptions),
            Options.Create(_coreOptions));

        await context.Database.EnsureCreatedAsync();

        var entity = new TenantTestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = explicitTenant,
            Name = "Explicit Tenant Test",
            Amount = 200m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.TenantTestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert - Should keep the explicit value
        entity.TenantId.ShouldBe(explicitTenant);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task QueryWithDifferentTenant_ShouldReturnEmpty()
    {
        // Arrange
        const string tenantId = "existing-tenant";

        await using (var setupContext = CreateDbContextForTenant(tenantId))
        {
            setupContext.TenantTestEntities.Add(CreateEntity(tenantId, "Test Entity"));
            await setupContext.SaveChangesAsync();
        }

        // Act - Query with different tenant
        await using var queryContext = CreateDbContextForTenant("non-existing-tenant");
        var entities = await queryContext.TenantTestEntities.ToListAsync();

        // Assert
        entities.ShouldBeEmpty();
    }

    [Fact]
    public async Task CrossTenantDataIsolation_ShouldBeEnforced()
    {
        // Arrange
        const string tenant1 = "isolation-tenant-1";
        const string tenant2 = "isolation-tenant-2";

        await using (var context1 = CreateDbContextForTenant(tenant1))
        {
            context1.TenantTestEntities.Add(CreateEntity(tenant1, "Tenant 1 Secret"));
            await context1.SaveChangesAsync();
        }

        await using (var context2 = CreateDbContextForTenant(tenant2))
        {
            context2.TenantTestEntities.Add(CreateEntity(tenant2, "Tenant 2 Secret"));
            await context2.SaveChangesAsync();
        }

        // Act - Tenant 1 tries to access data
        await using var tenant1Context = CreateDbContextForTenant(tenant1);
        var tenant1Entities = await tenant1Context.TenantTestEntities.ToListAsync();

        // Assert
        tenant1Entities.Count.ShouldBe(1);
        tenant1Entities.ShouldAllBe(e => e.TenantId == tenant1);
        tenant1Entities.ShouldNotContain(e => e.Name == "Tenant 2 Secret");
    }

    [Fact]
    public async Task MultipleTenants_ShouldMaintainSeparateDataSets()
    {
        // Arrange
        var tenants = new[] { "tenant-a", "tenant-b", "tenant-c" };

        foreach (var tenant in tenants)
        {
            await using var context = CreateDbContextForTenant(tenant);
            context.TenantTestEntities.AddRange(
                CreateEntity(tenant, $"{tenant} Entity 1"),
                CreateEntity(tenant, $"{tenant} Entity 2"));
            await context.SaveChangesAsync();
        }

        // Act & Assert - Each tenant should see only their own data
        foreach (var tenant in tenants)
        {
            await using var context = CreateDbContextForTenant(tenant);
            var entities = await context.TenantTestEntities.ToListAsync();

            entities.Count.ShouldBe(2);
            entities.ShouldAllBe(e => e.TenantId == tenant);
        }
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task UpdateEntity_WrongTenant_ShouldThrowException()
    {
        // Arrange
        const string tenant1 = "owner-tenant";
        const string tenant2 = "attacker-tenant";

        Guid entityId;
        await using (var setupContext = CreateDbContextForTenant(tenant1))
        {
            var entity = CreateEntity(tenant1, "Protected Entity");
            setupContext.TenantTestEntities.Add(entity);
            await setupContext.SaveChangesAsync();
            entityId = entity.Id;
        }

        // Act & Assert - Trying to modify entity belonging to different tenant
        await using var attackerContext = CreateDbContextForTenant(tenant2);

        // Get the entity ignoring filters (simulating an attack)
        var targetEntity = await attackerContext.TenantTestEntities
            .IgnoreQueryFilters()
            .FirstAsync(e => e.Id == entityId);

        targetEntity.Name = "Hacked!";

        // This should throw because entity belongs to different tenant
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await attackerContext.SaveChangesAsync();
        });
    }

    #endregion

    #region LINQ Query Tests

    [Fact]
    public async Task WhereClause_ShouldWorkWithTenantFilter()
    {
        // Arrange
        const string tenantId = "where-tenant";
        await using (var setupContext = CreateDbContextForTenant(tenantId))
        {
            setupContext.TenantTestEntities.AddRange(
                CreateEntity(tenantId, "Active 1", isActive: true),
                CreateEntity(tenantId, "Active 2", isActive: true),
                CreateEntity(tenantId, "Inactive", isActive: false));
            await setupContext.SaveChangesAsync();
        }

        // Act
        await using var queryContext = CreateDbContextForTenant(tenantId);
        var activeEntities = await queryContext.TenantTestEntities
            .Where(e => e.IsActive)
            .ToListAsync();

        // Assert
        activeEntities.Count.ShouldBe(2);
        activeEntities.ShouldAllBe(e => e.IsActive && e.TenantId == tenantId);
    }

    [Fact]
    public async Task OrderBy_ShouldWorkWithTenantFilter()
    {
        // Arrange
        const string tenantId = "orderby-tenant";
        await using (var setupContext = CreateDbContextForTenant(tenantId))
        {
            setupContext.TenantTestEntities.AddRange(
                CreateEntity(tenantId, "Zebra", amount: 300m),
                CreateEntity(tenantId, "Apple", amount: 100m),
                CreateEntity(tenantId, "Mango", amount: 200m));
            await setupContext.SaveChangesAsync();
        }

        // Act
        await using var queryContext = CreateDbContextForTenant(tenantId);
        var sortedEntities = await queryContext.TenantTestEntities
            .OrderBy(e => e.Name)
            .ToListAsync();

        // Assert
        sortedEntities.Count.ShouldBe(3);
        sortedEntities[0].Name.ShouldBe("Apple");
        sortedEntities[1].Name.ShouldBe("Mango");
        sortedEntities[2].Name.ShouldBe("Zebra");
    }

    [Fact]
    public async Task Aggregate_ShouldWorkWithTenantFilter()
    {
        // Arrange
        const string tenantId = "aggregate-tenant";
        await using (var setupContext = CreateDbContextForTenant(tenantId))
        {
            setupContext.TenantTestEntities.AddRange(
                CreateEntity(tenantId, "Item 1", amount: 100m),
                CreateEntity(tenantId, "Item 2", amount: 200m),
                CreateEntity(tenantId, "Item 3", amount: 300m));
            await setupContext.SaveChangesAsync();
        }

        // Act
        await using var queryContext = CreateDbContextForTenant(tenantId);
        var totalAmount = await queryContext.TenantTestEntities.SumAsync(e => e.Amount);
        var count = await queryContext.TenantTestEntities.CountAsync();

        // Assert
        totalAmount.ShouldBe(600m);
        count.ShouldBe(3);
    }

    #endregion

    #region Helper Methods

    private static TenantTestEntity CreateEntity(
        string tenantId,
        string name,
        decimal amount = 100m,
        bool isActive = true)
    {
        return new TenantTestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Amount = amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    #endregion
}
