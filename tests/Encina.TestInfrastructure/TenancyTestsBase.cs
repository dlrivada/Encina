using System.Data;
using Encina.TestInfrastructure.Entities;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure;

/// <summary>
/// Abstract base class for multi-tenancy integration tests.
/// Contains standard test methods for tenant isolation verification that work
/// across ADO.NET, Dapper, and direct database access.
/// </summary>
/// <typeparam name="TFixture">The type of database fixture to use.</typeparam>
/// <remarks>
/// This base class provides:
/// <list type="bullet">
/// <item><description>Tenant filter injection verification</description></item>
/// <item><description>Cross-tenant data isolation tests</description></item>
/// <item><description>Tenant context switching tests</description></item>
/// <item><description>Tenant-aware bulk operation tests</description></item>
/// </list>
/// <para>
/// Derived classes must implement methods to create connections for specific tenants
/// and to execute queries against the underlying database.
/// </para>
/// </remarks>
public abstract class TenancyTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    /// <summary>
    /// Gets the database fixture instance.
    /// </summary>
    protected abstract TFixture Fixture { get; }

    /// <summary>
    /// Creates a database connection for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>A database connection configured for the specified tenant.</returns>
    protected abstract IDbConnection CreateConnectionForTenant(string tenantId);

    /// <summary>
    /// Inserts a tenant test entity into the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="tenantId">The tenant ID to associate with the entity.</param>
    protected abstract Task InsertTenantEntityAsync(IDbConnection connection, TenantTestEntity entity, string tenantId);

    /// <summary>
    /// Queries all tenant test entities visible to the current connection's tenant context.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <returns>A list of entities visible to the tenant.</returns>
    protected abstract Task<List<TenantTestEntity>> QueryTenantEntitiesAsync(IDbConnection connection);

    /// <summary>
    /// Queries a specific entity by ID.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>The entity if found and visible; otherwise, null.</returns>
    protected abstract Task<TenantTestEntity?> QueryTenantEntityByIdAsync(IDbConnection connection, Guid entityId);

    /// <summary>
    /// Gets the provider-specific name for display in test output.
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Clears all test data before each test.
    /// </summary>
    protected abstract Task ClearTestDataAsync();

    #region IAsyncLifetime

    /// <inheritdoc />
    public virtual async ValueTask InitializeAsync()
    {
        await ClearTestDataAsync();
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Tenant Filter Injection Tests

    [Fact]
    public async Task TenantFilter_ShouldOnlyReturnCurrentTenantData()
    {
        // Arrange
        const string tenant1 = "tenant-1";
        const string tenant2 = "tenant-2";

        // Insert data for tenant 1
        using (var conn1 = CreateConnectionForTenant(tenant1))
        {
            await InsertTenantEntityAsync(conn1, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant1,
                Name = "Tenant 1 Entity 1",
                Amount = 100m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant1);

            await InsertTenantEntityAsync(conn1, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant1,
                Name = "Tenant 1 Entity 2",
                Amount = 200m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant1);
        }

        // Insert data for tenant 2
        using (var conn2 = CreateConnectionForTenant(tenant2))
        {
            await InsertTenantEntityAsync(conn2, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant2,
                Name = "Tenant 2 Entity",
                Amount = 300m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant2);
        }

        // Act - Query as tenant 1
        using var queryConn = CreateConnectionForTenant(tenant1);
        var entities = await QueryTenantEntitiesAsync(queryConn);

        // Assert
        entities.Count.ShouldBe(2, $"[{ProviderName}] Expected 2 entities for tenant 1");
        entities.ShouldAllBe(e => e.TenantId == tenant1, $"[{ProviderName}] All entities should belong to tenant 1");
    }

    [Fact]
    public async Task TenantFilter_QueryWithNonExistingTenant_ShouldReturnEmpty()
    {
        // Arrange
        const string existingTenant = "existing-tenant";

        using (var conn = CreateConnectionForTenant(existingTenant))
        {
            await InsertTenantEntityAsync(conn, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = existingTenant,
                Name = "Existing Entity",
                Amount = 100m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, existingTenant);
        }

        // Act - Query with different tenant
        using var queryConn = CreateConnectionForTenant("non-existing-tenant");
        var entities = await QueryTenantEntitiesAsync(queryConn);

        // Assert
        entities.ShouldBeEmpty($"[{ProviderName}] No entities should be visible to non-existing tenant");
    }

    #endregion

    #region Cross-Tenant Isolation Tests

    [Fact]
    public async Task CrossTenantIsolation_TenantCannotAccessOtherTenantData()
    {
        // Arrange
        const string tenant1 = "isolation-tenant-1";
        const string tenant2 = "isolation-tenant-2";
        var tenant1EntityId = Guid.NewGuid();
        var tenant2EntityId = Guid.NewGuid();

        // Create entity for tenant 1
        using (var conn1 = CreateConnectionForTenant(tenant1))
        {
            await InsertTenantEntityAsync(conn1, new TenantTestEntity
            {
                Id = tenant1EntityId,
                TenantId = tenant1,
                Name = "Tenant 1 Secret Data",
                Amount = 1000m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant1);
        }

        // Create entity for tenant 2
        using (var conn2 = CreateConnectionForTenant(tenant2))
        {
            await InsertTenantEntityAsync(conn2, new TenantTestEntity
            {
                Id = tenant2EntityId,
                TenantId = tenant2,
                Name = "Tenant 2 Secret Data",
                Amount = 2000m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant2);
        }

        // Act - Tenant 1 tries to access tenant 2's entity by ID
        using var queryConn = CreateConnectionForTenant(tenant1);
        var tenant2Entity = await QueryTenantEntityByIdAsync(queryConn, tenant2EntityId);
        var allEntities = await QueryTenantEntitiesAsync(queryConn);

        // Assert
        tenant2Entity.ShouldBeNull($"[{ProviderName}] Tenant 1 should not be able to access tenant 2's entity by ID");
        allEntities.ShouldAllBe(e => e.TenantId == tenant1, $"[{ProviderName}] Tenant 1 should only see their own data");
        allEntities.ShouldNotContain(e => e.Name == "Tenant 2 Secret Data",
            $"[{ProviderName}] Tenant 2's data should not be visible to tenant 1");
    }

    [Fact]
    public async Task CrossTenantIsolation_MultipleTenantsMaintainSeparation()
    {
        // Arrange
        var tenants = new[] { "tenant-a", "tenant-b", "tenant-c" };

        foreach (var tenant in tenants)
        {
            using var conn = CreateConnectionForTenant(tenant);
            await InsertTenantEntityAsync(conn, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                Name = $"{tenant} Entity 1",
                Amount = 100m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant);

            await InsertTenantEntityAsync(conn, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                Name = $"{tenant} Entity 2",
                Amount = 200m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant);
        }

        // Act & Assert - Each tenant should see only their own data
        foreach (var tenant in tenants)
        {
            using var queryConn = CreateConnectionForTenant(tenant);
            var entities = await QueryTenantEntitiesAsync(queryConn);

            entities.Count.ShouldBe(2, $"[{ProviderName}] {tenant} should see exactly 2 entities");
            entities.ShouldAllBe(e => e.TenantId == tenant, $"[{ProviderName}] {tenant} should only see their own data");
        }
    }

    #endregion

    #region Tenant Context Switching Tests

    [Fact]
    public async Task TenantContextSwitching_ChangingTenantChangesVisibleData()
    {
        // Arrange
        const string tenant1 = "switch-tenant-1";
        const string tenant2 = "switch-tenant-2";

        using (var conn1 = CreateConnectionForTenant(tenant1))
        {
            await InsertTenantEntityAsync(conn1, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant1,
                Name = "Tenant 1 Data",
                Amount = 100m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant1);
        }

        using (var conn2 = CreateConnectionForTenant(tenant2))
        {
            await InsertTenantEntityAsync(conn2, new TenantTestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant2,
                Name = "Tenant 2 Data",
                Amount = 200m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }, tenant2);
        }

        // Act - Query as tenant 1
        using (var queryConn1 = CreateConnectionForTenant(tenant1))
        {
            var tenant1Entities = await QueryTenantEntitiesAsync(queryConn1);
            tenant1Entities.Count.ShouldBe(1, $"[{ProviderName}] Should see 1 entity as tenant 1");
            tenant1Entities[0].Name.ShouldBe("Tenant 1 Data");
        }

        // Act - Switch to tenant 2
        using (var queryConn2 = CreateConnectionForTenant(tenant2))
        {
            var tenant2Entities = await QueryTenantEntitiesAsync(queryConn2);
            tenant2Entities.Count.ShouldBe(1, $"[{ProviderName}] Should see 1 entity as tenant 2");
            tenant2Entities[0].Name.ShouldBe("Tenant 2 Data");
        }
    }

    #endregion
}
