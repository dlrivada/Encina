using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="ScheduledMessageStoreEF"/> to verify null parameter handling.
/// </summary>
public class ScheduledMessageStoreEFGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when dbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var act = () => new ScheduledMessageStoreEF(dbContext);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new ScheduledMessageStoreEF(dbContext);
        IScheduledMessage message = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(message);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new ScheduledMessageStoreEF(dbContext);
        var messageId = Guid.NewGuid();
        string errorMessage = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Test DbContext for in-memory database testing.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduledMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RequestType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}
