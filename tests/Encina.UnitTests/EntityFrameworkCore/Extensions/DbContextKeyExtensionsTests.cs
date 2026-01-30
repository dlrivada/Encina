using Encina.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Extensions;

/// <summary>
/// Tests for DbContextKeyExtensions methods.
/// </summary>
public class DbContextKeyExtensionsTests
{
    #region Test Types

    private sealed class SimpleKeyEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class IntKeyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class StringKeyEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CompositeKeyEntity
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    private sealed class TestKeyDbContext : DbContext
    {
        public TestKeyDbContext(DbContextOptions<TestKeyDbContext> options) : base(options) { }

        public DbSet<SimpleKeyEntity> SimpleEntities => Set<SimpleKeyEntity>();
        public DbSet<IntKeyEntity> IntEntities => Set<IntKeyEntity>();
        public DbSet<StringKeyEntity> StringEntities => Set<StringKeyEntity>();
        public DbSet<CompositeKeyEntity> CompositeEntities => Set<CompositeKeyEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SimpleKeyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<IntKeyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<StringKeyEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<CompositeKeyEntity>(entity =>
            {
                entity.HasKey(e => new { e.OrderId, e.ProductId });
            });
        }
    }

    #endregion

    #region GetPrimaryKeyValue Tests

    [Fact]
    public void GetPrimaryKeyValue_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new SimpleKeyEntity { Id = Guid.NewGuid() };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.GetPrimaryKeyValue(entity));
    }

    [Fact]
    public void GetPrimaryKeyValue_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        SimpleKeyEntity entity = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.GetPrimaryKeyValue(entity));
    }

    [Fact]
    public void GetPrimaryKeyValue_GuidKey_ShouldReturnKeyValue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var expectedId = Guid.NewGuid();
        var entity = new SimpleKeyEntity { Id = expectedId, Name = "Test" };
        context.SimpleEntities.Add(entity);

        // Act
        var keyValue = context.GetPrimaryKeyValue(entity);

        // Assert
        keyValue.ShouldBe(expectedId);
    }

    [Fact]
    public void GetPrimaryKeyValue_IntKey_ShouldReturnKeyValue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var entity = new IntKeyEntity { Id = 42, Name = "Test" };
        context.IntEntities.Add(entity);

        // Act
        var keyValue = context.GetPrimaryKeyValue(entity);

        // Assert
        keyValue.ShouldBe(42);
    }

    [Fact]
    public void GetPrimaryKeyValue_StringKey_ShouldReturnKeyValue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var entity = new StringKeyEntity { Id = "custom-key-123", Name = "Test" };
        context.StringEntities.Add(entity);

        // Act
        var keyValue = context.GetPrimaryKeyValue(entity);

        // Assert
        keyValue.ShouldBe("custom-key-123");
    }

    [Fact]
    public void GetPrimaryKeyValue_CompositeKey_ShouldReturnFirstKeyValue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var entity = new CompositeKeyEntity { OrderId = orderId, ProductId = productId, Quantity = 5 };
        context.CompositeEntities.Add(entity);

        // Act
        var keyValue = context.GetPrimaryKeyValue(entity);

        // Assert
        // For composite keys, GetPrimaryKeyValue returns the first key property
        keyValue.ShouldBe(orderId);
    }

    [Fact]
    public void GetPrimaryKeyValue_UntrackedEntity_ShouldStillReturnKeyValue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var expectedId = Guid.NewGuid();
        var entity = new SimpleKeyEntity { Id = expectedId, Name = "Test" };
        // Note: Entity is NOT added to context

        // Act
        var keyValue = context.GetPrimaryKeyValue(entity);

        // Assert
        keyValue.ShouldBe(expectedId);
    }

    #endregion

    #region GetPrimaryKeyValues Tests

    [Fact]
    public void GetPrimaryKeyValues_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContext context = null!;
        var entity = new SimpleKeyEntity { Id = Guid.NewGuid() };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.GetPrimaryKeyValues(entity));
    }

    [Fact]
    public void GetPrimaryKeyValues_NullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        SimpleKeyEntity entity = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.GetPrimaryKeyValues(entity));
    }

    [Fact]
    public void GetPrimaryKeyValues_SimpleKey_ShouldReturnSingleValueArray()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var expectedId = Guid.NewGuid();
        var entity = new SimpleKeyEntity { Id = expectedId, Name = "Test" };
        context.SimpleEntities.Add(entity);

        // Act
        var keyValues = context.GetPrimaryKeyValues(entity);

        // Assert
        keyValues.ShouldNotBeNull();
        keyValues.Length.ShouldBe(1);
        keyValues[0].ShouldBe(expectedId);
    }

    [Fact]
    public void GetPrimaryKeyValues_CompositeKey_ShouldReturnAllKeyValues()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var entity = new CompositeKeyEntity { OrderId = orderId, ProductId = productId, Quantity = 5 };
        context.CompositeEntities.Add(entity);

        // Act
        var keyValues = context.GetPrimaryKeyValues(entity);

        // Assert
        keyValues.ShouldNotBeNull();
        keyValues.Length.ShouldBe(2);
        keyValues[0].ShouldBe(orderId);
        keyValues[1].ShouldBe(productId);
    }

    #endregion

    #region GetPrimaryKeyPropertyName Tests

    [Fact]
    public void GetPrimaryKeyPropertyName_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        DbContext context = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => context.GetPrimaryKeyPropertyName<SimpleKeyEntity>());
    }

    [Fact]
    public void GetPrimaryKeyPropertyName_SimpleKey_ShouldReturnPropertyName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);

        // Act
        var propertyName = context.GetPrimaryKeyPropertyName<SimpleKeyEntity>();

        // Assert
        propertyName.ShouldBe("Id");
    }

    [Fact]
    public void GetPrimaryKeyPropertyName_CompositeKey_ShouldReturnFirstPropertyName()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);

        // Act
        var propertyName = context.GetPrimaryKeyPropertyName<CompositeKeyEntity>();

        // Assert
        // For composite keys, returns the first key property name
        propertyName.ShouldBe("OrderId");
    }

    #endregion

    #region Error Cases

    private sealed class UnmappedEntity
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public void GetPrimaryKeyValue_UnmappedEntity_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var entity = new UnmappedEntity { Id = Guid.NewGuid() };

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => context.GetPrimaryKeyValue(entity));
        exception.Message.ShouldContain("UnmappedEntity");
        exception.Message.ShouldContain("not part of the model");
    }

    [Fact]
    public void GetPrimaryKeyValues_UnmappedEntity_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);
        var entity = new UnmappedEntity { Id = Guid.NewGuid() };

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => context.GetPrimaryKeyValues(entity));
        exception.Message.ShouldContain("UnmappedEntity");
    }

    [Fact]
    public void GetPrimaryKeyPropertyName_UnmappedEntity_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestKeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestKeyDbContext(options);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            context.GetPrimaryKeyPropertyName<UnmappedEntity>());
        exception.Message.ShouldContain("UnmappedEntity");
    }

    #endregion
}
