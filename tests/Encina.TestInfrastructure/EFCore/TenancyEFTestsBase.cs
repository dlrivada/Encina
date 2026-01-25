using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for multi-tenancy EF Core integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of tenant-aware DbContext to use.</typeparam>
public abstract class TenancyEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Creates a test entity for a specific tenant.
    /// </summary>
    protected abstract object CreateTenantEntity(string tenantId, string name);

    /// <summary>
    /// Adds the tenant entity to the context.
    /// </summary>
    protected abstract void AddTenantEntityToContext(TContext context, object entity);

    /// <summary>
    /// Gets all entities visible to the current tenant context.
    /// </summary>
    protected abstract Task<List<object>> GetAllTenantEntitiesAsync(TContext context);

    /// <summary>
    /// Gets the tenant ID from an entity.
    /// </summary>
    protected abstract string GetEntityTenantId(object entity);

    /// <summary>
    /// Gets the name from an entity.
    /// </summary>
    protected abstract string GetEntityName(object entity);

    /// <summary>
    /// Creates a DbContext for a specific tenant.
    /// </summary>
    protected abstract TContext CreateDbContextForTenant(string tenantId);

    [Fact]
    public async Task TenantFilter_ShouldOnlyReturnCurrentTenantData()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        // Add data for tenant 1
        await using (var context1 = CreateDbContextForTenant(tenant1))
        {
            AddTenantEntityToContext(context1, CreateTenantEntity(tenant1, "Tenant 1 Entity 1"));
            AddTenantEntityToContext(context1, CreateTenantEntity(tenant1, "Tenant 1 Entity 2"));
            await context1.SaveChangesAsync();
        }

        // Add data for tenant 2
        await using (var context2 = CreateDbContextForTenant(tenant2))
        {
            AddTenantEntityToContext(context2, CreateTenantEntity(tenant2, "Tenant 2 Entity"));
            await context2.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        await using var queryContext = CreateDbContextForTenant(tenant1);
        var entities = await GetAllTenantEntitiesAsync(queryContext);

        // Assert
        entities.Count.ShouldBe(2);
        entities.ShouldAllBe(e => GetEntityTenantId(e) == tenant1);
    }

    [Fact]
    public async Task NewEntity_ShouldAutoAssignTenantId()
    {
        // Arrange
        const string tenantId = "auto-assign-tenant";
        await using var context = CreateDbContextForTenant(tenantId);

        var entity = CreateTenantEntity(tenantId, "Auto Assign Test");
        AddTenantEntityToContext(context, entity);

        // Act
        await context.SaveChangesAsync();

        // Assert
        GetEntityTenantId(entity).ShouldBe(tenantId);
    }

    [Fact]
    public async Task QueryWithoutTenant_ShouldReturnEmpty()
    {
        // Arrange
        const string tenantId = "existing-tenant";

        await using (var setupContext = CreateDbContextForTenant(tenantId))
        {
            AddTenantEntityToContext(setupContext, CreateTenantEntity(tenantId, "Test Entity"));
            await setupContext.SaveChangesAsync();
        }

        // Act - Query with different tenant
        await using var queryContext = CreateDbContextForTenant("non-existing-tenant");
        var entities = await GetAllTenantEntitiesAsync(queryContext);

        // Assert
        entities.ShouldBeEmpty();
    }

    [Fact]
    public async Task CrossTenantDataIsolation_ShouldBeEnforced()
    {
        // Arrange
        const string tenant1 = "isolation-tenant-1";
        const string tenant2 = "isolation-tenant-2";

        // Create entities for both tenants
        await using (var context1 = CreateDbContextForTenant(tenant1))
        {
            AddTenantEntityToContext(context1, CreateTenantEntity(tenant1, "Tenant 1 Secret"));
            await context1.SaveChangesAsync();
        }

        await using (var context2 = CreateDbContextForTenant(tenant2))
        {
            AddTenantEntityToContext(context2, CreateTenantEntity(tenant2, "Tenant 2 Secret"));
            await context2.SaveChangesAsync();
        }

        // Act - Tenant 1 tries to access data
        await using var tenant1Context = CreateDbContextForTenant(tenant1);
        var tenant1Entities = await GetAllTenantEntitiesAsync(tenant1Context);

        // Assert - Tenant 1 should only see their own data
        tenant1Entities.Count.ShouldBe(1);
        tenant1Entities.ShouldAllBe(e => GetEntityTenantId(e) == tenant1);
        tenant1Entities.ShouldNotContain(e => GetEntityName(e) == "Tenant 2 Secret");
    }

    [Fact]
    public async Task MultipleTenants_ShouldMaintainSeparateDataSets()
    {
        // Arrange
        var tenants = new[] { "tenant-a", "tenant-b", "tenant-c" };

        foreach (var tenant in tenants)
        {
            await using var context = CreateDbContextForTenant(tenant);
            AddTenantEntityToContext(context, CreateTenantEntity(tenant, $"{tenant} Entity 1"));
            AddTenantEntityToContext(context, CreateTenantEntity(tenant, $"{tenant} Entity 2"));
            await context.SaveChangesAsync();
        }

        // Act & Assert - Each tenant should see only their own data
        foreach (var tenant in tenants)
        {
            await using var context = CreateDbContextForTenant(tenant);
            var entities = await GetAllTenantEntitiesAsync(context);

            entities.Count.ShouldBe(2);
            entities.ShouldAllBe(e => GetEntityTenantId(e) == tenant);
        }
    }
}
