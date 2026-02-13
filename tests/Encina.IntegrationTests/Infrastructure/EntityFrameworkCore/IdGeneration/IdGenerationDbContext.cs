using Encina.EntityFrameworkCore.Converters;
using Encina.IdGeneration;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.IdGeneration;

/// <summary>
/// Test entity with all four ID generation types as properties.
/// </summary>
public class IdGenerationEntity
{
    public int Id { get; set; }
    public SnowflakeId? SnowflakeCol { get; set; }
    public UlidId? UlidCol { get; set; }
    public UuidV7Id? UuidV7Col { get; set; }
    public ShardPrefixedId? ShardPrefixedCol { get; set; }
}

/// <summary>
/// Test DbContext for ID generation EF Core integration tests.
/// Configures value converters for all four Encina ID types.
/// </summary>
public sealed class IdGenerationDbContext : DbContext
{
    public IdGenerationDbContext(DbContextOptions<IdGenerationDbContext> options)
        : base(options)
    {
    }

    public DbSet<IdGenerationEntity> IdGenerationEntities => Set<IdGenerationEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureIdGenerationConventions();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdGenerationEntity>(entity =>
        {
            entity.ToTable("IdGenerationEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UlidCol).HasMaxLength(26);
            entity.Property(e => e.ShardPrefixedCol).HasMaxLength(256);
        });
    }
}
