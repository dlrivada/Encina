using Microsoft.EntityFrameworkCore;

namespace Encina.Benchmarks.EntityFrameworkCore.Infrastructure;

/// <summary>
/// DbContext for EntityFrameworkCore data access benchmarks.
/// </summary>
/// <remarks>
/// <para>
/// This context is specifically designed for benchmarking repository patterns,
/// specification evaluators, and unit of work operations. It supports both
/// InMemory and SQLite providers for different benchmark scenarios:
/// </para>
/// <list type="bullet">
///   <item><description>InMemory: Pure CPU measurement for isolated benchmarks</description></item>
///   <item><description>SQLite: Operations requiring real SQL behavior</description></item>
/// </list>
/// <para>
/// Includes realistic indexes for query benchmarks to measure actual
/// index utilization patterns.
/// </para>
/// </remarks>
public sealed class EntityFrameworkBenchmarkDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkBenchmarkDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public EntityFrameworkBenchmarkDbContext(DbContextOptions<EntityFrameworkBenchmarkDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the benchmark entities DbSet.
    /// </summary>
    public DbSet<BenchmarkEntity> BenchmarkEntities => Set<BenchmarkEntity>();

    /// <summary>
    /// Creates an InMemory database context for pure CPU benchmarks.
    /// </summary>
    /// <param name="databaseName">Optional database name for isolation.</param>
    /// <returns>A configured DbContext using InMemory provider.</returns>
    public static EntityFrameworkBenchmarkDbContext CreateInMemory(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<EntityFrameworkBenchmarkDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new EntityFrameworkBenchmarkDbContext(options);
    }

    /// <summary>
    /// Creates a SQLite in-memory database context for realistic SQL benchmarks.
    /// </summary>
    /// <param name="connection">The SQLite connection to use.</param>
    /// <returns>A configured DbContext using SQLite provider.</returns>
    public static EntityFrameworkBenchmarkDbContext CreateSqlite(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<EntityFrameworkBenchmarkDbContext>()
            .UseSqlite(connection)
            .Options;

        return new EntityFrameworkBenchmarkDbContext(options);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BenchmarkEntity>(entity =>
        {
            entity.ToTable("BenchmarkEntities");
            entity.HasKey(e => e.Id);

            // Name property with index for text-based queries
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.HasIndex(e => e.Name);

            // Value property for range queries
            entity.Property(e => e.Value)
                .HasPrecision(18, 4);

            // CreatedAtUtc for date-based queries and ordering
            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();
            entity.HasIndex(e => e.CreatedAtUtc);

            // Category for filtering benchmarks
            entity.Property(e => e.Category)
                .HasMaxLength(100);
            entity.HasIndex(e => e.Category);

            // IsActive for boolean filter benchmarks
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);

            // Composite index for common query patterns
            entity.HasIndex(e => new { e.IsActive, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.Category, e.IsActive });
        });
    }
}
