using Shouldly;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;
using Xunit;

namespace Encina.EntityFrameworkCore.ContractTests.Inbox;

/// <summary>
/// Contract tests for InboxStoreEF verifying IInboxStore compliance.
/// </summary>
public sealed class InboxStoreEFContractTests : IDisposable
{
    private readonly DbContext _context;
    private readonly InboxStoreEF _store;

    public InboxStoreEFContractTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _store = new InboxStoreEF(_context);
    }

    [Fact]
    public void Contract_MustImplementIInboxStore()
    {
        // Assert
        _store.ShouldBeAssignableTo<IInboxStore>();
    }

    [Fact]
    public async Task Contract_AddAsync_MustAcceptIInboxMessage()
    {
        // Arrange
        IInboxMessage message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        // Act
        var act = async () => await _store.AddAsync(message);

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task Contract_GetMessageAsync_MustReturnIInboxMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var retrieved = await _store.GetMessageAsync(messageId);

        // Assert
        retrieved.ShouldBeAssignableTo<IInboxMessage>();
    }

    [Fact]
    public async Task Contract_MarkAsProcessedAsync_MustAcceptParameters()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.MarkAsProcessedAsync(messageId, "{\"success\":true}");

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task Contract_MarkAsFailedAsync_MustAcceptParameters()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.MarkAsFailedAsync(
            messageId,
            "Error message",
            DateTime.UtcNow.AddMinutes(5));

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task Contract_GetExpiredMessagesAsync_MustReturnIInboxMessage()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var messages = await _store.GetExpiredMessagesAsync(10);

        // Assert
        messages.ShouldAllBe(x => x is IInboxMessage);
    }

    [Fact]
    public async Task Contract_RemoveExpiredMessagesAsync_MustAcceptStringCollection()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.RemoveExpiredMessagesAsync(new[] { messageId });

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task Contract_SaveChangesAsync_MustPersistChanges()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RetryCount = 0
        };

        await _store.AddAsync(message);

        // Act
        await _store.SaveChangesAsync();

        // Assert
        var retrieved = await _store.GetMessageAsync(messageId);
        retrieved.ShouldNotBeNull();
    }

    [Fact]
    public async Task Contract_AddAsync_WithNonEFMessage_MustThrow()
    {
        // Arrange
        var mockMessage = new MockInboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "Test",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var act = async () => await _store.AddAsync(mockMessage);

        // Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await act());
        ex.Message.ShouldContain("InboxMessage");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // Mock implementation for contract testing
    private sealed class MockInboxMessage : IInboxMessage
    {
        public required string MessageId { get; set; }
        public required string RequestType { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ReceivedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAtUtc { get; set; }
        public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;
        public bool IsExpired() => ExpiresAtUtc <= DateTime.UtcNow;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>().HasKey(m => m.MessageId);
        }
    }
}
