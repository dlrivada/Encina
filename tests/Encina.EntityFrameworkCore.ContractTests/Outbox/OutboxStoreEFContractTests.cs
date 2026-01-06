using Shouldly;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Xunit;

namespace Encina.EntityFrameworkCore.ContractTests.Outbox;

/// <summary>
/// Contract tests for OutboxStoreEF verifying IOutboxStore compliance.
/// </summary>
public sealed class OutboxStoreEFContractTests : IDisposable
{
    private readonly DbContext _context;
    private readonly OutboxStoreEF _store;

    public OutboxStoreEFContractTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _store = new OutboxStoreEF(_context);
    }

    [Fact]
    public void Contract_MustImplementIOutboxStore()
    {
        // Assert
        _store.ShouldBeAssignableTo<IOutboxStore>();
    }

    [Fact]
    public async Task Contract_AddAsync_MustAcceptIOutboxMessage()
    {
        // Arrange
        IOutboxMessage message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        // Act
        Func<Task> act = () => _store.AddAsync(message);

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_GetPendingMessagesAsync_MustReturnIOutboxMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var messages = await _store.GetPendingMessagesAsync(10, 3);

        // Assert
        messages.ShouldAllBe(x => x is IOutboxMessage);
    }

    [Fact]
    public async Task Contract_MarkAsProcessedAsync_MustAcceptGuid()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        Func<Task> act = () => _store.MarkAsProcessedAsync(messageId);

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_MarkAsFailedAsync_MustAcceptParameters()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        Func<Task> act = () => _store.MarkAsFailedAsync(
            messageId,
            "Error message",
            DateTime.UtcNow.AddMinutes(5));

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_SaveChangesAsync_MustPersistChanges()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _store.AddAsync(message);

        // Act
        await _store.SaveChangesAsync();

        // Assert
        var retrieved = await _store.GetPendingMessagesAsync(10, 3);
        retrieved.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Contract_AddAsync_WithNonEFMessage_MustThrow()
    {
        // Arrange
        var mockMessage = new MockOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        Func<Task> act = () => _store.AddAsync(mockMessage);

        // Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(act);
        ex.Message.ShouldContain("OutboxMessage");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // Mock implementation for contract testing
    private sealed class MockOutboxMessage : IOutboxMessage
    {
        public Guid Id { get; set; }
        public required string NotificationType { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAtUtc { get; set; }
        public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;
        public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    }
}
