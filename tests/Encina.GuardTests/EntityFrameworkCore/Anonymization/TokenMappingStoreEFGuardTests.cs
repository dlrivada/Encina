using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.EntityFrameworkCore.Anonymization;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Anonymization;

/// <summary>
/// Guard clause tests for <see cref="TokenMappingStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class TokenMappingStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext context = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new TokenMappingStoreEF(context));
        ex.ParamName.ShouldBe("context");
    }

    #endregion

    #region StoreAsync Guards

    [Fact]
    public async Task StoreAsync_NullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        TokenMapping mapping = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.StoreAsync(mapping));
        ex.ParamName.ShouldBe("mapping");
    }

    #endregion

    #region GetByTokenAsync Guards

    [Fact]
    public async Task GetByTokenAsync_NullToken_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string token = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByTokenAsync(token));
        ex.ParamName.ShouldBe("token");
    }

    [Fact]
    public async Task GetByTokenAsync_WhitespaceToken_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByTokenAsync("  "));
        ex.ParamName.ShouldBe("token");
    }

    #endregion

    #region GetByOriginalValueHashAsync Guards

    [Fact]
    public async Task GetByOriginalValueHashAsync_NullHash_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string hash = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByOriginalValueHashAsync(hash));
        ex.ParamName.ShouldBe("hash");
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_WhitespaceHash_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetByOriginalValueHashAsync("  "));
        ex.ParamName.ShouldBe("hash");
    }

    #endregion

    #region DeleteByKeyIdAsync Guards

    [Fact]
    public async Task DeleteByKeyIdAsync_NullKeyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();
        string keyId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeleteByKeyIdAsync(keyId));
        ex.ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_WhitespaceKeyId_ThrowsArgumentException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.DeleteByKeyIdAsync("  "));
        ex.ParamName.ShouldBe("keyId");
    }

    #endregion

    #region Test Infrastructure

    private static TokenMappingStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestTokenMappingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestTokenMappingDbContext(options);
        return new TokenMappingStoreEF(dbContext);
    }

    private sealed class TestTokenMappingDbContext : DbContext
    {
        public TestTokenMappingDbContext(DbContextOptions<TestTokenMappingDbContext> options) : base(options)
        {
        }

        public DbSet<TokenMappingEntity> TokenMappings => Set<TokenMappingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TokenMappingEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    #endregion
}
