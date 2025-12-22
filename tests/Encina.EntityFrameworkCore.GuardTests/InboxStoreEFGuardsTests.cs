using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="InboxStoreEF"/> to verify null parameter handling.
/// </summary>
public class InboxStoreEFGuardsTests
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
        var act = () => new InboxStoreEF(dbContext);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    /// <summary>
    /// Verifies that GetMessageAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new InboxStoreEF(dbContext);
        string messageId = null!;

        // Act & Assert
        var act = async () => await store.GetMessageAsync(messageId);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
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
        var store = new InboxStoreEF(dbContext);
        IInboxMessage message = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(message);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new InboxStoreEF(dbContext);
        string messageId = null!;
        var response = "test response";

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, response);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentNullException when response is null.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new InboxStoreEF(dbContext);
        var messageId = "test-message-id";
        string response = null!;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId, response);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("response");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new InboxStoreEF(dbContext);
        string messageId = null!;
        var errorMessage = "test error";

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageId");
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
        var store = new InboxStoreEF(dbContext);
        var messageId = "test-message-id";
        string errorMessage = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that RemoveExpiredMessagesAsync throws ArgumentNullException when messageIds is null.
    /// </summary>
    [Fact]
    public async Task RemoveExpiredMessagesAsync_NullMessageIds_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestDbContext(options);
        var store = new InboxStoreEF(dbContext);
        IEnumerable<string> messageIds = null!;

        // Act & Assert
        var act = async () => await store.RemoveExpiredMessagesAsync(messageIds);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("messageIds");
    }

    /// <summary>
    /// Test DbContext for in-memory database testing.
    /// </summary>
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.RequestType).IsRequired();
                entity.Property(e => e.ReceivedAtUtc).IsRequired();
            });
        }
    }
}
