using Shouldly;
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Scheduling;
using Xunit;

namespace Encina.EntityFrameworkCore.ContractTests.Scheduling;

/// <summary>
/// Contract tests for ScheduledMessageStoreEF verifying IScheduledMessageStore compliance.
/// </summary>
public sealed class ScheduledMessageStoreEFContractTests : IDisposable
{
    private readonly DbContext _context;
    private readonly ScheduledMessageStoreEF _store;

    public ScheduledMessageStoreEFContractTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _store = new ScheduledMessageStoreEF(_context);
    }

    [Fact]
    public void Contract_MustImplementIScheduledMessageStore()
    {
        // Assert
        _store.ShouldBeAssignableTo<IScheduledMessageStore>();
    }

    [Fact]
    public async Task Contract_AddAsync_MustAcceptIScheduledMessage()
    {
        // Arrange
        IScheduledMessage message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "TestRequest",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        // Act
        var act = async () => await _store.AddAsync(message);

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_GetDueMessagesAsync_MustReturnIScheduledMessage()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddSeconds(-1),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var messages = await _store.GetDueMessagesAsync(10, 3);

        // Assert
        messages.ShouldAllBe(x => x is IScheduledMessage);
    }

    [Fact]
    public async Task Contract_MarkAsProcessedAsync_MustAcceptGuid()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.MarkAsProcessedAsync(messageId);

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_MarkAsFailedAsync_MustAcceptParameters()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.MarkAsFailedAsync(
            messageId,
            "Error message",
            DateTime.UtcNow.AddMinutes(5));

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_RescheduleRecurringMessageAsync_MustWork()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = true,
            CronExpression = "0 0 * * *"
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.RescheduleRecurringMessageAsync(
            messageId,
            DateTime.UtcNow.AddDays(1));

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_CancelAsync_MustAcceptGuid()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ScheduledMessage
        {
            Id = messageId,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0,
            IsRecurring = false
        };

        await _store.AddAsync(message);
        await _store.SaveChangesAsync();

        // Act
        var act = async () => await _store.CancelAsync(messageId);

        // Assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task Contract_SaveChangesAsync_MustPersistChanges()
    {
        // Arrange
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddSeconds(-1), // Make it due
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            RetryCount = 0,
            IsRecurring = false
        };

        await _store.AddAsync(message);

        // Act
        await _store.SaveChangesAsync();

        // Assert
        var retrieved = await _store.GetDueMessagesAsync(100, 3);
        retrieved.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Contract_AddAsync_WithNonEFMessage_MustThrow()
    {
        // Arrange
        var mockMessage = new MockScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        var act = async () => await _store.AddAsync(mockMessage);

        // Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*ScheduledMessage*");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // Mock implementation for contract testing
    private sealed class MockScheduledMessage : IScheduledMessage
    {
        public Guid Id { get; set; }
        public required string RequestType { get; set; }
        public required string Content { get; set; }
        public DateTime ScheduledAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAtUtc { get; set; }
        public bool IsRecurring { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? LastExecutedAtUtc { get; set; }
        public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;
        public bool IsDue() => ScheduledAtUtc <= DateTime.UtcNow && !IsProcessed;
        public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduledMessage>().HasKey(m => m.Id);
        }
    }
}
