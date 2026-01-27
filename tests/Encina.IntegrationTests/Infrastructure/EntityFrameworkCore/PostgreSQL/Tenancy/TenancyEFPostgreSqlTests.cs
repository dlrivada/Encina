using Encina.EntityFrameworkCore.Tenancy;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Tenancy;
using Encina.Tenancy;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Tenancy;

/// <summary>
/// PostgreSQL-specific integration tests for EF Core multi-tenancy support.
/// Tests global query filters, automatic tenant assignment, and tenant isolation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class TenancyEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;
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

    public TenancyEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        // Ensure schema is created
        await using var context = CreateDbContextForTenant("setup");
        await context.Database.EnsureCreatedAsync();

        // Clear any existing data
        await ClearDataAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fixture.IsAvailable)
        {
            await ClearDataAsync();
        }
    }

    private TenantTestDbContext CreateDbContextForTenant(string tenantId)
    {
        _tenantProvider.SetTenant(tenantId);

        var optionsBuilder = new DbContextOptionsBuilder<TenantTestDbContext>();
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);

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
            _tenantProvider.SetTenant("admin");
            await using var context = CreateDbContextForTenant("admin");
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

    [SkippableFact]
    public async Task TenantFilter_ShouldOnlyReturnCurrentTenantData()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        await using (var context1 = CreateDbContextForTenant(tenant1))
        {
            context1.TenantTestEntities.AddRange(
                CreateEntity(tenant1, "Tenant 1 Entity 1"),
                CreateEntity(tenant1, "Tenant 1 Entity 2"));
            await context1.SaveChangesAsync();
        }

        await using (var context2 = CreateDbContextForTenant(tenant2))
        {
            context2.TenantTestEntities.Add(CreateEntity(tenant2, "Tenant 2 Entity"));
            await context2.SaveChangesAsync();
        }

        // Act
        await using var queryContext = CreateDbContextForTenant(tenant1);
        var entities = await queryContext.TenantTestEntities.ToListAsync();

        // Assert
        entities.Count.ShouldBe(2);
        entities.ShouldAllBe(e => e.TenantId == tenant1);
    }

    [SkippableFact]
    public async Task IgnoreQueryFilters_ShouldReturnAllTenantData()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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

        // Act
        await using var queryContext = CreateDbContextForTenant(tenant1);
        var allEntities = await queryContext.TenantTestEntities.IgnoreQueryFilters().ToListAsync();

        // Assert
        allEntities.ShouldContain(e => e.TenantId == tenant1);
        allEntities.ShouldContain(e => e.TenantId == tenant2);
    }

    #endregion

    #region Auto-Assignment Tests

    [SkippableFact]
    public async Task NewEntity_ShouldAutoAssignTenantId()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        const string tenantId = "auto-assign-tenant";
        await using var context = CreateDbContextForTenant(tenantId);

        var entity = new TenantTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Auto Assign Test",
            Amount = 100m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.TenantTestEntities.Add(entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        entity.TenantId.ShouldBe(tenantId);
    }

    #endregion

    #region Tenant Isolation Tests

    [SkippableFact]
    public async Task QueryWithDifferentTenant_ShouldReturnEmpty()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

        // Arrange
        const string tenantId = "existing-tenant";

        await using (var setupContext = CreateDbContextForTenant(tenantId))
        {
            setupContext.TenantTestEntities.Add(CreateEntity(tenantId, "Test Entity"));
            await setupContext.SaveChangesAsync();
        }

        // Act
        await using var queryContext = CreateDbContextForTenant("non-existing-tenant");
        var entities = await queryContext.TenantTestEntities.ToListAsync();

        // Assert
        entities.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task CrossTenantDataIsolation_ShouldBeEnforced()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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

        // Act
        await using var tenant1Context = CreateDbContextForTenant(tenant1);
        var tenant1Entities = await tenant1Context.TenantTestEntities.ToListAsync();

        // Assert
        tenant1Entities.Count.ShouldBe(1);
        tenant1Entities.ShouldAllBe(e => e.TenantId == tenant1);
        tenant1Entities.ShouldNotContain(e => e.Name == "Tenant 2 Secret");
    }

    [SkippableFact]
    public async Task MultipleTenants_ShouldMaintainSeparateDataSets()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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

        // Act & Assert
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

    [SkippableFact]
    public async Task UpdateEntity_WrongTenant_ShouldThrowException()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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

        // Act & Assert
        await using var attackerContext = CreateDbContextForTenant(tenant2);
        var targetEntity = await attackerContext.TenantTestEntities
            .IgnoreQueryFilters()
            .FirstAsync(e => e.Id == entityId);

        targetEntity.Name = "Hacked!";

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await attackerContext.SaveChangesAsync();
        });
    }

    #endregion

    #region LINQ Query Tests

    [SkippableFact]
    public async Task WhereClause_ShouldWorkWithTenantFilter()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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

    [SkippableFact]
    public async Task Aggregate_ShouldWorkWithTenantFilter()
    {
        Skip.IfNot(_fixture.IsAvailable, "PostgreSQL container not available");

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
