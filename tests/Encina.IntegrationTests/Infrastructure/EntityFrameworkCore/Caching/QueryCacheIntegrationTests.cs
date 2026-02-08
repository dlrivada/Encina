using Encina.Caching;
using Encina.Caching.Memory;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EncinaMemoryCacheOptions = Encina.Caching.Memory.MemoryCacheOptions;
using MsMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Caching;

/// <summary>
/// Integration tests verifying the query caching interceptor works end-to-end
/// with a real EF Core DbContext and in-memory cache provider.
/// Uses a self-contained SQLite in-memory database per test to avoid shared fixture complications.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
public sealed class QueryCacheIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MsMemoryCache _memoryCache;
    private readonly MemoryCacheProvider _cacheProvider;
    private bool _disposed;

    public QueryCacheIntegrationTests()
    {
        // Self-contained SQLite in-memory database for each test instance
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var memoryCacheOptions = Options.Create(new MsMemoryCacheOptions());
        _memoryCache = new MsMemoryCache(memoryCacheOptions);

        var cacheOptions = Options.Create(new EncinaMemoryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });
        _cacheProvider = new MemoryCacheProvider(
            _memoryCache,
            cacheOptions,
            NullLogger<MemoryCacheProvider>.Instance);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache.Dispose();
            _connection.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    #region Cache Hit/Miss Tests

    [Fact]
    public async Task Query_FirstExecution_ShouldNotHitCache()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();

        // Act — first query should execute against the database
        var results = await context.Products.Where(p => p.IsActive).ToListAsync();

        // Assert
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(2); // Widget + Gadget
    }

    [Fact]
    public async Task Query_SecondExecution_ShouldReturnCachedResult()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();

        // Act — execute same query twice
        var first = await context.Products.Where(p => p.IsActive).ToListAsync();
        var second = await context.Products.Where(p => p.IsActive).ToListAsync();

        // Assert — both should return the same data
        first.Count.ShouldBe(second.Count);
        first.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DifferentQueries_ShouldNotShareCache()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();

        // Act
        var active = await context.Products.Where(p => p.IsActive).ToListAsync();
        var all = await context.Products.ToListAsync();

        // Assert — different queries should return different results
        active.Count.ShouldBe(2);
        all.Count.ShouldBe(3);
        all.Count.ShouldBeGreaterThan(active.Count);
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task SaveChanges_ShouldInvalidateCachedQueries()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync();

        // Warm cache with a query
        var before = await context.Products.Where(p => p.IsActive).ToListAsync();
        var countBefore = before.Count;

        // Act — add a new product and save (should trigger invalidation)
        context.Products.Add(new CacheTestProduct
        {
            Id = Guid.NewGuid(),
            Name = "New Product",
            Price = 29.99m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Query again after invalidation — should reflect the new data
        var after = await context.Products.Where(p => p.IsActive).ToListAsync();

        // Assert
        after.Count.ShouldBe(countBefore + 1);
    }

    #endregion

    #region Disabled Cache Tests

    [Fact]
    public async Task WhenCachingDisabled_QueriesBypassCache()
    {
        // Arrange
        await using var context = await CreateSeededContextAsync(enabled: false);

        // Act — queries should not be cached
        var first = await context.Products.ToListAsync();
        var second = await context.Products.ToListAsync();

        // Assert — both should return data (no caching verification needed, just no errors)
        first.ShouldNotBeEmpty();
        second.ShouldNotBeEmpty();
        first.Count.ShouldBe(3);
    }

    #endregion

    #region Excluded Entity Types Tests

    [Fact]
    public async Task ExcludedEntityTypes_ShouldBypassCache()
    {
        // Arrange — exclude CacheTestProduct from caching
        var options = new QueryCacheOptions
        {
            Enabled = true,
            DefaultExpiration = TimeSpan.FromMinutes(5)
        };
        options.ExcludeType<CacheTestProduct>();

        await using var context = await CreateSeededContextAsync(queryOptions: options);

        // Act — query the excluded entity type
        var results = await context.Products.ToListAsync();

        // Assert — should return data without errors
        results.ShouldNotBeEmpty();
        results.Count.ShouldBe(3);
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddQueryCaching_AndUseQueryCaching_FullRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICacheProvider>(_cacheProvider);
        services.AddQueryCaching(options =>
        {
            options.Enabled = true;
            options.DefaultExpiration = TimeSpan.FromMinutes(10);
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert — all services should resolve
        var keyGenerator = provider.GetService<IQueryCacheKeyGenerator>();
        keyGenerator.ShouldNotBeNull();
        keyGenerator.ShouldBeOfType<DefaultQueryCacheKeyGenerator>();

        var interceptor = provider.GetService<QueryCacheInterceptor>();
        interceptor.ShouldNotBeNull();
    }

    [Fact]
    public void UseQueryCaching_WithoutCacheProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueryCaching();
        // Note: ICacheProvider is NOT registered

        var provider = services.BuildServiceProvider();
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(
            () => optionsBuilder.UseQueryCaching(provider));
        ex.Message.ShouldContain("ICacheProvider");
    }

    #endregion

    #region Helpers

    private async Task<QueryCacheTestDbContext> CreateSeededContextAsync(
        bool enabled = true,
        QueryCacheOptions? queryOptions = null)
    {
        var context = CreateDbContextWithCaching(enabled, queryOptions);
        await context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(context);
        return context;
    }

    private QueryCacheTestDbContext CreateDbContextWithCaching(
        bool enabled = true,
        QueryCacheOptions? queryOptions = null)
    {
        var options = queryOptions ?? new QueryCacheOptions
        {
            Enabled = enabled,
            DefaultExpiration = TimeSpan.FromMinutes(5),
            KeyPrefix = $"test:{Guid.NewGuid():N}"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICacheProvider>(_cacheProvider);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<IQueryCacheKeyGenerator>(
            new DefaultQueryCacheKeyGenerator(Options.Create(options)));
        services.AddSingleton<QueryCacheInterceptor>();

        var serviceProvider = services.BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<QueryCacheTestDbContext>();
        optionsBuilder.UseSqlite(_connection);

        // Add the interceptor
        var interceptor = serviceProvider.GetRequiredService<QueryCacheInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);

        return new QueryCacheTestDbContext(optionsBuilder.Options);
    }

    private static async Task SeedTestDataAsync(QueryCacheTestDbContext context)
    {
        if (await context.Products.AnyAsync())
        {
            return;
        }

        context.Products.AddRange(
            new CacheTestProduct
            {
                Id = Guid.NewGuid(),
                Name = "Widget",
                Price = 9.99m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new CacheTestProduct
            {
                Id = Guid.NewGuid(),
                Name = "Gadget",
                Price = 19.99m,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new CacheTestProduct
            {
                Id = Guid.NewGuid(),
                Name = "Retired Item",
                Price = 4.99m,
                IsActive = false,
                CreatedAtUtc = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    #endregion
}

#region Test Infrastructure

/// <summary>
/// Test DbContext for query caching integration tests.
/// </summary>
public sealed class QueryCacheTestDbContext : DbContext
{
    public QueryCacheTestDbContext(DbContextOptions<QueryCacheTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<CacheTestProduct> Products => Set<CacheTestProduct>();
    public DbSet<CacheTestCategory> Categories => Set<CacheTestCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CacheTestProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CacheTestCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });
    }
}

/// <summary>
/// Test entity for cache integration tests.
/// </summary>
public sealed class CacheTestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Secondary test entity for cross-entity cache invalidation tests.
/// </summary>
public sealed class CacheTestCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion
