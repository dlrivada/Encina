using System.ComponentModel.DataAnnotations;
using Encina.EntityFrameworkCore.Sharding.ReferenceTables;
using Encina.Sharding.ReferenceTables;
using Encina.Testing.Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Sharding;

/// <summary>
/// Unit tests for <see cref="ReferenceTableStoreEF"/> and <see cref="ReferenceTableStoreFactoryEF{TContext}"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReferenceTableStoreEFTests : IDisposable
{
    private readonly ReferenceTableTestDbContext _dbContext;
    private readonly ReferenceTableStoreEF _store;

    public ReferenceTableStoreEFTests()
    {
        var options = new DbContextOptionsBuilder<ReferenceTableTestDbContext>()
            .UseInMemoryDatabase($"RefTableTest-{Guid.NewGuid()}")
            .Options;

        _dbContext = new ReferenceTableTestDbContext(options);
        _store = new ReferenceTableStoreEF(_dbContext);
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ReferenceTableStoreEF(null!));
    }

    #endregion

    #region UpsertAsync

    [Fact]
    public async Task UpsertAsync_NullEntities_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _store.UpsertAsync<CountryRef>(null!));
    }

    [Fact]
    public async Task UpsertAsync_EmptyCollection_ReturnsZero()
    {
        // Act
        var result = await _store.UpsertAsync(Array.Empty<CountryRef>());

        // Assert
        var count = result.ShouldBeRight();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task UpsertAsync_NewEntities_InsertsAndReturnsCount()
    {
        // Arrange
        var entities = new[]
        {
            new CountryRef { Id = 1, Code = "US", Name = "United States" },
            new CountryRef { Id = 2, Code = "ES", Name = "Spain" },
            new CountryRef { Id = 3, Code = "FR", Name = "France" }
        };

        // Act
        var result = await _store.UpsertAsync(entities);

        // Assert
        var count = result.ShouldBeRight();
        count.ShouldBe(3);

        var dbCount = await _dbContext.Countries.CountAsync();
        dbCount.ShouldBe(3);
    }

    [Fact]
    public async Task UpsertAsync_ExistingEntities_UpdatesValues()
    {
        // Arrange - Insert initial data
        var initial = new CountryRef { Id = 1, Code = "US", Name = "United States" };
        _dbContext.Countries.Add(initial);
        await _dbContext.SaveChangesAsync();

        // Act - Upsert with updated name
        var updated = new[] { new CountryRef { Id = 1, Code = "US", Name = "United States of America" } };
        var result = await _store.UpsertAsync(updated);

        // Assert
        result.ShouldBeRight().ShouldBe(1);

        var entity = await _dbContext.Countries.FindAsync(1);
        entity.ShouldNotBeNull();
        entity.Name.ShouldBe("United States of America");
    }

    [Fact]
    public async Task UpsertAsync_MixOfNewAndExisting_InsertsAndUpdates()
    {
        // Arrange - Insert initial data
        _dbContext.Countries.Add(new CountryRef { Id = 1, Code = "US", Name = "United States" });
        await _dbContext.SaveChangesAsync();

        // Act - Upsert with one update and one new
        var entities = new[]
        {
            new CountryRef { Id = 1, Code = "US", Name = "USA" },
            new CountryRef { Id = 2, Code = "ES", Name = "Spain" }
        };
        var result = await _store.UpsertAsync(entities);

        // Assert
        result.ShouldBeRight().ShouldBe(2);

        var count = await _dbContext.Countries.CountAsync();
        count.ShouldBe(2);

        var us = await _dbContext.Countries.FindAsync(1);
        us!.Name.ShouldBe("USA");
    }

    [Fact]
    public async Task UpsertAsync_WithIEnumerable_MaterializesOnce()
    {
        // Arrange - pass as IEnumerable (not IList) to test materialization
        IEnumerable<CountryRef> entities = Enumerable.Range(1, 5).Select(i =>
            new CountryRef { Id = i, Code = $"C{i}", Name = $"Country {i}" });

        // Act
        var result = await _store.UpsertAsync(entities);

        // Assert
        result.ShouldBeRight().ShouldBe(5);
    }

    [Fact]
    public async Task UpsertAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var entities = new[] { new CountryRef { Id = 1, Code = "US", Name = "United States" } };

        // Act & Assert - OperationCanceledException should propagate, not be caught
        await Should.ThrowAsync<OperationCanceledException>(
            () => _store.UpsertAsync(entities, cts.Token));
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_EmptyTable_ReturnsEmptyList()
    {
        // Act
        var result = await _store.GetAllAsync<CountryRef>();

        // Assert
        var entities = result.ShouldBeRight();
        entities.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAllEntities()
    {
        // Arrange
        _dbContext.Countries.AddRange(
            new CountryRef { Id = 1, Code = "US", Name = "United States" },
            new CountryRef { Id = 2, Code = "ES", Name = "Spain" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _store.GetAllAsync<CountryRef>();

        // Assert
        var entities = result.ShouldBeRight();
        entities.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsReadOnlyList()
    {
        // Arrange
        _dbContext.Countries.Add(new CountryRef { Id = 1, Code = "US", Name = "United States" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _store.GetAllAsync<CountryRef>();

        // Assert
        var entities = result.ShouldBeRight();
        entities.ShouldBeAssignableTo<IReadOnlyList<CountryRef>>();
    }

    [Fact]
    public async Task GetAllAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _store.GetAllAsync<CountryRef>(cts.Token));
    }

    #endregion

    #region GetHashAsync

    [Fact]
    public async Task GetHashAsync_EmptyTable_ReturnsHash()
    {
        // Act
        var result = await _store.GetHashAsync<CountryRef>();

        // Assert
        var hash = result.ShouldBeRight();
        hash.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetHashAsync_WithData_ReturnsConsistentHash()
    {
        // Arrange
        _dbContext.Countries.AddRange(
            new CountryRef { Id = 1, Code = "US", Name = "United States" },
            new CountryRef { Id = 2, Code = "ES", Name = "Spain" });
        await _dbContext.SaveChangesAsync();

        // Act
        var hash1 = await _store.GetHashAsync<CountryRef>();
        var hash2 = await _store.GetHashAsync<CountryRef>();

        // Assert
        var h1 = hash1.ShouldBeRight();
        var h2 = hash2.ShouldBeRight();
        h1.ShouldBe(h2);
    }

    [Fact]
    public async Task GetHashAsync_DifferentData_ReturnsDifferentHash()
    {
        // Arrange - first dataset
        _dbContext.Countries.Add(new CountryRef { Id = 1, Code = "US", Name = "United States" });
        await _dbContext.SaveChangesAsync();

        var hash1 = await _store.GetHashAsync<CountryRef>();

        // Arrange - modify data
        var entity = await _dbContext.Countries.FindAsync(1);
        entity!.Name = "USA";
        await _dbContext.SaveChangesAsync();

        // Act
        var hash2 = await _store.GetHashAsync<CountryRef>();

        // Assert
        var h1 = hash1.ShouldBeRight();
        var h2 = hash2.ShouldBeRight();
        h1.ShouldNotBe(h2);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_DisposesUnderlyingDbContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReferenceTableTestDbContext>()
            .UseInMemoryDatabase($"DisposeTest-{Guid.NewGuid()}")
            .Options;
        var context = new ReferenceTableTestDbContext(options);
        var store = new ReferenceTableStoreEF(context);

        // Act
        store.Dispose();

        // Assert - accessing disposed context should throw
        Should.Throw<ObjectDisposedException>(() => context.Countries.ToList());
    }

    #endregion

    #region ReferenceTableStoreFactoryEF

    [Fact]
    public void Factory_Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableStoreFactoryEF<ReferenceTableTestDbContext>(
                null!,
                (builder, cs) => builder.UseInMemoryDatabase(cs)));
    }

    [Fact]
    public void Factory_Constructor_WithNullConfigureProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        using var sp = services.BuildServiceProvider();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableStoreFactoryEF<ReferenceTableTestDbContext>(sp, null!));
    }

    [Fact]
    public void Factory_CreateForShard_ReturnsReferenceTableStoreEF()
    {
        // Arrange
        var services = new ServiceCollection();
        using var sp = services.BuildServiceProvider();

        var factory = new ReferenceTableStoreFactoryEF<ReferenceTableTestDbContext>(
            sp,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act
        var store = factory.CreateForShard("Server=test;Database=shard0;");

        // Assert
        store.ShouldNotBeNull();
        store.ShouldBeOfType<ReferenceTableStoreEF>();

        if (store is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Factory_CreateForShard_NullOrWhitespaceConnectionString_ThrowsArgumentException(
        string? connectionString)
    {
        // Arrange
        var services = new ServiceCollection();
        using var sp = services.BuildServiceProvider();

        var factory = new ReferenceTableStoreFactoryEF<ReferenceTableTestDbContext>(
            sp,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.CreateForShard(connectionString!));
    }

    [Fact]
    public void Factory_CreateForShard_PassesConnectionStringToConfigureProvider()
    {
        // Arrange
        string? capturedConnectionString = null;
        var services = new ServiceCollection();
        using var sp = services.BuildServiceProvider();

        var factory = new ReferenceTableStoreFactoryEF<ReferenceTableTestDbContext>(
            sp,
            (builder, cs) =>
            {
                capturedConnectionString = cs;
                builder.UseInMemoryDatabase(cs);
            });

        // Act
        var store = factory.CreateForShard("Server=shard-1;Database=refdata;");

        // Assert
        capturedConnectionString.ShouldBe("Server=shard-1;Database=refdata;");

        if (store is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public void Factory_CreateForShard_CreatesDifferentInstancesPerCall()
    {
        // Arrange
        var services = new ServiceCollection();
        using var sp = services.BuildServiceProvider();

        var factory = new ReferenceTableStoreFactoryEF<ReferenceTableTestDbContext>(
            sp,
            (builder, cs) => builder.UseInMemoryDatabase(cs));

        // Act
        var store1 = factory.CreateForShard("Server=shard-0;Database=test;");
        var store2 = factory.CreateForShard("Server=shard-1;Database=test;");

        // Assert
        store1.ShouldNotBeSameAs(store2);

        if (store1 is IDisposable d1) d1.Dispose();
        if (store2 is IDisposable d2) d2.Dispose();
    }

    #endregion

    #region Test Entities and DbContext

    public sealed class ReferenceTableTestDbContext : DbContext
    {
        public ReferenceTableTestDbContext(DbContextOptions<ReferenceTableTestDbContext> options)
            : base(options)
        {
        }

        public DbSet<CountryRef> Countries => Set<CountryRef>();
        public DbSet<CurrencyRef> Currencies => Set<CurrencyRef>();
    }

    public sealed class CountryRef
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public sealed class CurrencyRef
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
