using Encina.EntityFrameworkCore.ABAC;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.ABAC;

/// <summary>
/// Guard clause tests for <see cref="PolicyStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class PolicyStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;
        var serializer = Substitute.For<IPolicySerializer>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new PolicyStoreEF(dbContext, serializer));
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public void Constructor_NullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestPolicyStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestPolicyStoreDbContext(options);
        IPolicySerializer serializer = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new PolicyStoreEF(dbContext, serializer));
        ex.ParamName.ShouldBe("serializer");
    }

    [Fact]
    public void Constructor_NullTimeProvider_DoesNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestPolicyStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestPolicyStoreDbContext(options);
        var serializer = Substitute.For<IPolicySerializer>();

        // Act & Assert - timeProvider is optional (defaults to TimeProvider.System)
        Should.NotThrow(() =>
            new PolicyStoreEF(dbContext, serializer, timeProvider: null));
    }

    #endregion

    #region GetPolicySetAsync Guards

    [Fact]
    public async Task GetPolicySetAsync_NullPolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string policySetId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetPolicySetAsync(policySetId));
        ex.ParamName.ShouldBe("policySetId");
    }

    [Fact]
    public async Task GetPolicySetAsync_WhitespacePolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetPolicySetAsync("  "));
        ex.ParamName.ShouldBe("policySetId");
    }

    #endregion

    #region SavePolicySetAsync Guards

    [Fact]
    public async Task SavePolicySetAsync_NullPolicySet_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        PolicySet policySet = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.SavePolicySetAsync(policySet));
        ex.ParamName.ShouldBe("policySet");
    }

    #endregion

    #region DeletePolicySetAsync Guards

    [Fact]
    public async Task DeletePolicySetAsync_NullPolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string policySetId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeletePolicySetAsync(policySetId));
        ex.ParamName.ShouldBe("policySetId");
    }

    [Fact]
    public async Task DeletePolicySetAsync_WhitespacePolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeletePolicySetAsync("  "));
        ex.ParamName.ShouldBe("policySetId");
    }

    #endregion

    #region ExistsPolicySetAsync Guards

    [Fact]
    public async Task ExistsPolicySetAsync_NullPolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string policySetId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.ExistsPolicySetAsync(policySetId));
        ex.ParamName.ShouldBe("policySetId");
    }

    [Fact]
    public async Task ExistsPolicySetAsync_WhitespacePolicySetId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.ExistsPolicySetAsync("  "));
        ex.ParamName.ShouldBe("policySetId");
    }

    #endregion

    #region GetPolicyAsync Guards

    [Fact]
    public async Task GetPolicyAsync_NullPolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string policyId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetPolicyAsync(policyId));
        ex.ParamName.ShouldBe("policyId");
    }

    [Fact]
    public async Task GetPolicyAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetPolicyAsync("  "));
        ex.ParamName.ShouldBe("policyId");
    }

    #endregion

    #region SavePolicyAsync Guards

    [Fact]
    public async Task SavePolicyAsync_NullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        Policy policy = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.SavePolicyAsync(policy));
        ex.ParamName.ShouldBe("policy");
    }

    #endregion

    #region DeletePolicyAsync Guards

    [Fact]
    public async Task DeletePolicyAsync_NullPolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string policyId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeletePolicyAsync(policyId));
        ex.ParamName.ShouldBe("policyId");
    }

    [Fact]
    public async Task DeletePolicyAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeletePolicyAsync("  "));
        ex.ParamName.ShouldBe("policyId");
    }

    #endregion

    #region ExistsPolicyAsync Guards

    [Fact]
    public async Task ExistsPolicyAsync_NullPolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string policyId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.ExistsPolicyAsync(policyId));
        ex.ParamName.ShouldBe("policyId");
    }

    [Fact]
    public async Task ExistsPolicyAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.ExistsPolicyAsync("  "));
        ex.ParamName.ShouldBe("policyId");
    }

    #endregion

    #region Test Infrastructure

    private static PolicyStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestPolicyStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestPolicyStoreDbContext(options);
        var serializer = Substitute.For<IPolicySerializer>();
        return new PolicyStoreEF(dbContext, serializer);
    }

    private sealed class TestPolicyStoreDbContext : DbContext
    {
        public TestPolicyStoreDbContext(DbContextOptions<TestPolicyStoreDbContext> options) : base(options)
        {
        }

        public DbSet<PolicySetEntity> PolicySets => Set<PolicySetEntity>();
        public DbSet<PolicyEntity> Policies => Set<PolicyEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PolicySetEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<PolicyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
