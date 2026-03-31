using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Stores;

/// <summary>
/// Guard clause tests for <see cref="InboxStoreEF"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class InboxStoreEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InboxStoreEF(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public void Constructor_NullTimeProvider_DoesNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestInboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new TestInboxDbContext(options);

        // Act & Assert - timeProvider is optional (defaults to TimeProvider.System)
        Should.NotThrow(() =>
            new InboxStoreEF(dbContext, timeProvider: null));
    }

    #endregion

    #region GetMessageAsync Guards

    [Fact]
    public async Task GetMessageAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string messageId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.GetMessageAsync(messageId));
        ex.ParamName.ShouldBe("messageId");
    }

    #endregion

    #region AddAsync Guards

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        IInboxMessage message = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(message));
        ex.ParamName.ShouldBe("message");
    }

    #endregion

    #region MarkAsProcessedAsync Guards

    [Fact]
    public async Task MarkAsProcessedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string messageId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.MarkAsProcessedAsync(messageId, "response"));
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string response = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.MarkAsProcessedAsync("msg-1", response));
        ex.ParamName.ShouldBe("response");
    }

    #endregion

    #region MarkAsFailedAsync Guards

    [Fact]
    public async Task MarkAsFailedAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string messageId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.MarkAsFailedAsync(messageId, "error", null));
        ex.ParamName.ShouldBe("messageId");
    }

    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string errorMessage = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.MarkAsFailedAsync("msg-1", errorMessage, null));
        ex.ParamName.ShouldBe("errorMessage");
    }

    #endregion

    #region IncrementRetryCountAsync Guards

    [Fact]
    public async Task IncrementRetryCountAsync_NullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        string messageId = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.IncrementRetryCountAsync(messageId));
        ex.ParamName.ShouldBe("messageId");
    }

    #endregion

    #region RemoveExpiredMessagesAsync Guards

    [Fact]
    public async Task RemoveExpiredMessagesAsync_NullMessageIds_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        IEnumerable<string> messageIds = null!;

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.RemoveExpiredMessagesAsync(messageIds));
        ex.ParamName.ShouldBe("messageIds");
    }

    #endregion

    #region Test Infrastructure

    private static InboxStoreEF CreateStore()
    {
        var options = new DbContextOptionsBuilder<TestInboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new TestInboxDbContext(options);
        return new InboxStoreEF(dbContext);
    }

    private sealed class TestInboxDbContext : DbContext
    {
        public TestInboxDbContext(DbContextOptions<TestInboxDbContext> options) : base(options)
        {
        }

        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.MessageId);
            });
        }
    }

    #endregion
}
