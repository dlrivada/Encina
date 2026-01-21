using Encina.Tenancy;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="InMemoryTenantStore"/>.
/// </summary>
public class InMemoryTenantStoreTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyStore()
    {
        // Arrange & Act
        var store = new InMemoryTenantStore();

        // Assert
        store.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithTenants_PopulatesStore()
    {
        // Arrange
        var tenants = new[]
        {
            new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema),
            new TenantInfo("t2", "Tenant 2", TenantIsolationStrategy.DatabasePerTenant)
        };

        // Act
        var store = new InMemoryTenantStore(tenants);

        // Assert
        store.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithNullTenants_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => new InMemoryTenantStore(null!));
    }

    #endregion

    #region GetTenantAsync Tests

    [Fact]
    public async Task GetTenantAsync_ExistingTenant_ReturnsTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema);
        store.RegisterTenant(tenant);

        // Act
        var result = await store.GetTenantAsync("t1");

        // Assert
        result.ShouldNotBeNull();
        result.TenantId.ShouldBe("t1");
        result.Name.ShouldBe("Tenant 1");
    }

    [Fact]
    public async Task GetTenantAsync_NonExistingTenant_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = await store.GetTenantAsync("non-existent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetTenantAsync_NullTenantId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetTenantAsync(null!));
    }

    [Fact]
    public async Task GetTenantAsync_IsCaseInsensitive()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.RegisterTenant(new TenantInfo("Tenant-ABC", "Test", TenantIsolationStrategy.SharedSchema));

        // Act
        var result1 = await store.GetTenantAsync("tenant-abc");
        var result2 = await store.GetTenantAsync("TENANT-ABC");
        var result3 = await store.GetTenantAsync("Tenant-ABC");

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result3.ShouldNotBeNull();
    }

    #endregion

    #region GetAllTenantsAsync Tests

    [Fact]
    public async Task GetAllTenantsAsync_EmptyStore_ReturnsEmptyList()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = await store.GetAllTenantsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllTenantsAsync_WithTenants_ReturnsAllTenants()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.RegisterTenant(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema));
        store.RegisterTenant(new TenantInfo("t2", "Tenant 2", TenantIsolationStrategy.SharedSchema));
        store.RegisterTenant(new TenantInfo("t3", "Tenant 3", TenantIsolationStrategy.SharedSchema));

        // Act
        var result = await store.GetAllTenantsAsync();

        // Assert
        result.Count.ShouldBe(3);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ExistingTenant_ReturnsTrue()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.RegisterTenant(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema));

        // Act
        var result = await store.ExistsAsync("t1");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingTenant_ReturnsFalse()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = await store.ExistsAsync("non-existent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_NullTenantId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.ExistsAsync(null!));
    }

    #endregion

    #region RegisterTenant Tests

    [Fact]
    public void RegisterTenant_NewTenant_AddsTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema);

        // Act
        store.RegisterTenant(tenant);

        // Assert
        store.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RegisterTenant_ExistingTenant_ReplacesTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant1 = new TenantInfo("t1", "Old Name", TenantIsolationStrategy.SharedSchema);
        var tenant2 = new TenantInfo("t1", "New Name", TenantIsolationStrategy.DatabasePerTenant);

        // Act
        store.RegisterTenant(tenant1);
        store.RegisterTenant(tenant2);

        // Assert
        store.Count.ShouldBe(1);
        var result = await store.GetTenantAsync("t1");
        result!.Name.ShouldBe("New Name");
        result.Strategy.ShouldBe(TenantIsolationStrategy.DatabasePerTenant);
    }

    [Fact]
    public void RegisterTenant_NullTenant_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => store.RegisterTenant(null!));
    }

    #endregion

    #region RemoveTenant Tests

    [Fact]
    public void RemoveTenant_ExistingTenant_ReturnsTrue()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.RegisterTenant(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema));

        // Act
        var result = store.RemoveTenant("t1");

        // Assert
        result.ShouldBeTrue();
        store.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveTenant_NonExistingTenant_ReturnsFalse()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = store.RemoveTenant("non-existent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void RemoveTenant_NullTenantId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => store.RemoveTenant(null!));
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_WithTenants_RemovesAllTenants()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.RegisterTenant(new TenantInfo("t1", "Tenant 1", TenantIsolationStrategy.SharedSchema));
        store.RegisterTenant(new TenantInfo("t2", "Tenant 2", TenantIsolationStrategy.SharedSchema));

        // Act
        store.Clear();

        // Assert
        store.Count.ShouldBe(0);
    }

    [Fact]
    public void Clear_EmptyStore_DoesNotThrow()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act & Assert
        Should.NotThrow(() => store.Clear());
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tasks = new List<Task>();
        const int operationsPerThread = 100;

        // Act - Run concurrent registrations and lookups
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var tenantId = $"t-{threadId}-{j}";
                    store.RegisterTenant(new TenantInfo(tenantId, $"Tenant {tenantId}", TenantIsolationStrategy.SharedSchema));
                    await store.GetTenantAsync(tenantId);
                    await store.ExistsAsync(tenantId);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All operations completed without exception
        store.Count.ShouldBe(10 * operationsPerThread);
    }

    #endregion
}
