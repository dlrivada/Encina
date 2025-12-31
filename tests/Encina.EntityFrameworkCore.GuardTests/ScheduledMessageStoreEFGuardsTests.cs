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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dbContext");
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
        Func<Task> act = () => store.AddAsync(message);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("message");
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
        Func<Task> act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("errorMessage");
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
