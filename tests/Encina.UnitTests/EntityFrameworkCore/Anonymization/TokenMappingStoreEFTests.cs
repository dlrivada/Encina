using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Encina.EntityFrameworkCore.Anonymization;
using Encina.Messaging.Health;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Anonymization;

/// <summary>
/// Unit tests for <see cref="TokenMappingStoreEF"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class TokenMappingStoreEFTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TokenMappingStoreEF(null!));
    }

    [Fact]
    public void Constructor_WithValidContext_DoesNotThrow()
    {
        // Arrange
        var context = CreateInMemoryContext();

        // Act
        var store = new TokenMappingStoreEF(context);

        // Assert
        store.ShouldNotBeNull();
    }

    #endregion

    #region StoreAsync

    [Fact]
    public async Task StoreAsync_WithNullMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => store.StoreAsync(null!).AsTask());
    }

    [Fact]
    public async Task StoreAsync_WithValidMapping_ReturnsRight()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);
        var mapping = CreateTestMapping();

        // Act
        var result = await store.StoreAsync(mapping);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task StoreAsync_WhenDbUpdateExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var mockContext = Substitute.For<DbContext>();
        var mockSet = Substitute.For<DbSet<TokenMappingEntity>>();
        mockContext.Set<TokenMappingEntity>().Returns(mockSet);
        mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException("Duplicate key"));

        var store = new TokenMappingStoreEF(mockContext);
        var mapping = CreateTestMapping();

        // Act
        var result = await store.StoreAsync(mapping);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task StoreAsync_WhenOperationCancelled_RethrowsOperationCanceledException()
    {
        // Arrange
        var mockContext = Substitute.For<DbContext>();
        var mockSet = Substitute.For<DbSet<TokenMappingEntity>>();
        mockContext.Set<TokenMappingEntity>().Returns(mockSet);
        mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var store = new TokenMappingStoreEF(mockContext);
        var mapping = CreateTestMapping();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => store.StoreAsync(mapping).AsTask());
    }

    [Fact]
    public async Task StoreAsync_WhenGenericExceptionThrown_ReturnsLeft()
    {
        // Arrange
        var mockContext = Substitute.For<DbContext>();
        var mockSet = Substitute.For<DbSet<TokenMappingEntity>>();
        mockContext.Set<TokenMappingEntity>().Returns(mockSet);
        mockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var store = new TokenMappingStoreEF(mockContext);
        var mapping = CreateTestMapping();

        // Act
        var result = await store.StoreAsync(mapping);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetByTokenAsync

    [Fact]
    public async Task GetByTokenAsync_WithNullToken_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetByTokenAsync(null!).AsTask());
    }

    [Fact]
    public async Task GetByTokenAsync_WithWhitespaceToken_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetByTokenAsync("  ").AsTask());
    }

    [Fact]
    public async Task GetByTokenAsync_WithEmptyToken_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetByTokenAsync(string.Empty).AsTask());
    }

    #endregion

    #region GetByOriginalValueHashAsync

    [Fact]
    public async Task GetByOriginalValueHashAsync_WithNullHash_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetByOriginalValueHashAsync(null!).AsTask());
    }

    [Fact]
    public async Task GetByOriginalValueHashAsync_WithWhitespaceHash_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.GetByOriginalValueHashAsync("   ").AsTask());
    }

    #endregion

    #region DeleteByKeyIdAsync

    [Fact]
    public async Task DeleteByKeyIdAsync_WithNullKeyId_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.DeleteByKeyIdAsync(null!).AsTask());
    }

    [Fact]
    public async Task DeleteByKeyIdAsync_WithWhitespaceKeyId_ThrowsArgumentException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => store.DeleteByKeyIdAsync("  ").AsTask());
    }

    #endregion

    #region Interface Compliance

    [Fact]
    public void ImplementsITokenMappingStore()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var store = new TokenMappingStoreEF(context);

        // Assert
        (store is ITokenMappingStore).ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static TokenMappingTestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TokenMappingTestDbContext>()
            .UseInMemoryDatabase($"token-mapping-test-{Guid.NewGuid()}")
            .Options;
        return new TokenMappingTestDbContext(options);
    }

    private static TokenMapping CreateTestMapping()
    {
        return TokenMapping.Create(
            token: $"tok_{Guid.NewGuid():N}",
            originalValueHash: $"hash_{Guid.NewGuid():N}",
            encryptedOriginalValue: new byte[] { 1, 2, 3, 4 },
            keyId: "key-2025-01");
    }

    private sealed class TokenMappingTestDbContext : DbContext
    {
        public TokenMappingTestDbContext(DbContextOptions<TokenMappingTestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TokenMappingEntity> TokenMappings => Set<TokenMappingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TokenMappingEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.OriginalValueHash);
            });
        }
    }

    #endregion
}
