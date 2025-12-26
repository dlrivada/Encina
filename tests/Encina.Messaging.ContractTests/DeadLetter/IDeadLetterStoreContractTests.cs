using Encina.Messaging.DeadLetter;

namespace Encina.Messaging.ContractTests.DeadLetter;

/// <summary>
/// Contract tests that verify all IDeadLetterStore implementations follow the same behavioral contract.
/// These tests ensure consistency across all dead letter store implementations (EF Core, Dapper, ADO.NET, etc.).
/// </summary>
public abstract class IDeadLetterStoreContractTests
{
    protected abstract IDeadLetterStore CreateStore();
    protected abstract IDeadLetterMessage CreateMessage(Guid id, string sourcePattern);
    protected abstract Task CleanupAsync();

    #region AddAsync Contract

    [Fact]
    public async Task AddAsync_ValidMessage_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        var message = CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability);

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(message.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(message.Id);

        // Cleanup
        await CleanupAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveAllProperties()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        var message = CreateMessage(id, DeadLetterSourcePatterns.Outbox);

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(id);
        retrieved.Should().NotBeNull();
        retrieved!.RequestType.Should().Be(message.RequestType);
        retrieved.SourcePattern.Should().Be(message.SourcePattern);
        retrieved.ErrorMessage.Should().Be(message.ErrorMessage);

        // Cleanup
        await CleanupAsync();
    }

    #endregion

    #region GetAsync Contract

    [Fact]
    public async Task GetAsync_ExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        var message = CreateMessage(id, DeadLetterSourcePatterns.Recoverability);
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Act
        var retrieved = await store.GetAsync(id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(id);

        // Cleanup
        await CleanupAsync();
    }

    [Fact]
    public async Task GetAsync_NonExistentMessage_ShouldReturnNull()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var retrieved = await store.GetAsync(nonExistentId);

        // Assert
        retrieved.Should().BeNull();
    }

    #endregion

    #region GetMessagesAsync Contract

    [Fact]
    public async Task GetMessagesAsync_NoFilter_ShouldReturnAllMessages()
    {
        // Arrange
        var store = CreateStore();
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Outbox));
        await store.SaveChangesAsync();

        // Act
        var messages = await store.GetMessagesAsync();

        // Assert
        messages.Should().HaveCount(2);

        // Cleanup
        await CleanupAsync();
    }

    [Fact]
    public async Task GetMessagesAsync_WithSourceFilter_ShouldReturnFilteredMessages()
    {
        // Arrange
        var store = CreateStore();
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Outbox));
        await store.SaveChangesAsync();

        var filter = DeadLetterFilter.FromSource(DeadLetterSourcePatterns.Recoverability);

        // Act
        var messages = await store.GetMessagesAsync(filter);

        // Assert
        messages.Should().HaveCount(2);
        messages.Should().AllSatisfy(m => m.SourcePattern.Should().Be(DeadLetterSourcePatterns.Recoverability));

        // Cleanup
        await CleanupAsync();
    }

    [Fact]
    public async Task GetMessagesAsync_WithPagination_ShouldRespectSkipAndTake()
    {
        // Arrange
        var store = CreateStore();
        for (int i = 0; i < 10; i++)
        {
            await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        }
        await store.SaveChangesAsync();

        // Act
        var page1 = await store.GetMessagesAsync(skip: 0, take: 5);
        var page2 = await store.GetMessagesAsync(skip: 5, take: 5);

        // Assert
        page1.Should().HaveCount(5);
        page2.Should().HaveCount(5);

        // Cleanup
        await CleanupAsync();
    }

    #endregion

    #region GetCountAsync Contract

    [Fact]
    public async Task GetCountAsync_NoFilter_ShouldReturnTotalCount()
    {
        // Arrange
        var store = CreateStore();
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Outbox));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Inbox));
        await store.SaveChangesAsync();

        // Act
        var count = await store.GetCountAsync();

        // Assert
        count.Should().Be(3);

        // Cleanup
        await CleanupAsync();
    }

    [Fact]
    public async Task GetCountAsync_WithFilter_ShouldReturnFilteredCount()
    {
        // Arrange
        var store = CreateStore();
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Recoverability));
        await store.AddAsync(CreateMessage(Guid.NewGuid(), DeadLetterSourcePatterns.Outbox));
        await store.SaveChangesAsync();

        var filter = DeadLetterFilter.FromSource(DeadLetterSourcePatterns.Recoverability);

        // Act
        var count = await store.GetCountAsync(filter);

        // Assert
        count.Should().Be(2);

        // Cleanup
        await CleanupAsync();
    }

    #endregion

    #region DeleteAsync Contract

    [Fact]
    public async Task DeleteAsync_ExistingMessage_ShouldReturnTrueAndRemoveMessage()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        await store.AddAsync(CreateMessage(id, DeadLetterSourcePatterns.Recoverability));
        await store.SaveChangesAsync();

        // Act
        var deleted = await store.DeleteAsync(id);
        await store.SaveChangesAsync();

        // Assert
        deleted.Should().BeTrue();
        var retrieved = await store.GetAsync(id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentMessage_ShouldReturnFalse()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var deleted = await store.DeleteAsync(nonExistentId);

        // Assert
        deleted.Should().BeFalse();
    }

    #endregion

    #region MarkAsReplayedAsync Contract

    [Fact]
    public async Task MarkAsReplayedAsync_ExistingMessage_ShouldUpdateReplayedFields()
    {
        // Arrange
        var store = CreateStore();
        var id = Guid.NewGuid();
        var message = CreateMessage(id, DeadLetterSourcePatterns.Recoverability);
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Act
        await store.MarkAsReplayedAsync(id, "Success");
        await store.SaveChangesAsync();

        // Assert
        var retrieved = await store.GetAsync(id);
        retrieved.Should().NotBeNull();
        retrieved!.IsReplayed.Should().BeTrue();
        retrieved.ReplayResult.Should().Be("Success");
        retrieved.ReplayedAtUtc.Should().NotBeNull();

        // Cleanup
        await CleanupAsync();
    }

    #endregion
}

/// <summary>
/// In-memory implementation of IDeadLetterStore for contract testing.
/// </summary>
internal sealed class InMemoryDeadLetterStoreForContract : IDeadLetterStore
{
    private readonly List<InMemoryDeadLetterMessageForContract> _messages = [];

    public Task AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        if (message is InMemoryDeadLetterMessageForContract inMemoryMessage)
        {
            _messages.Add(inMemoryMessage);
        }
        return Task.CompletedTask;
    }

    public Task<IDeadLetterMessage?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);
        return Task.FromResult<IDeadLetterMessage?>(message);
    }

    public Task<IEnumerable<IDeadLetterMessage>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<IDeadLetterMessage> query = _messages;

        if (filter != null)
        {
            if (filter.SourcePattern != null)
                query = query.Where(m => m.SourcePattern == filter.SourcePattern);

            if (filter.ExcludeReplayed == true)
                query = query.Where(m => !m.IsReplayed);
        }

        return Task.FromResult(query.OrderBy(m => m.DeadLetteredAtUtc).Skip(skip).Take(take));
    }

    public Task<int> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<IDeadLetterMessage> query = _messages;

        if (filter != null)
        {
            if (filter.SourcePattern != null)
                query = query.Where(m => m.SourcePattern == filter.SourcePattern);

            if (filter.ExcludeReplayed == true)
                query = query.Where(m => !m.IsReplayed);
        }

        return Task.FromResult(query.Count());
    }

    public Task MarkAsReplayedAsync(Guid messageId, string result, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            message.ReplayedAtUtc = DateTime.UtcNow;
            message.ReplayResult = result;
        }
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            _messages.Remove(message);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = _messages.Where(m => m.ExpiresAtUtc.HasValue && m.ExpiresAtUtc <= now).ToList();
        foreach (var message in expired)
        {
            _messages.Remove(message);
        }
        return Task.FromResult(expired.Count);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear() => _messages.Clear();
}

/// <summary>
/// In-memory implementation of IDeadLetterMessage for contract testing.
/// </summary>
internal sealed class InMemoryDeadLetterMessageForContract : IDeadLetterMessage
{
    public Guid Id { get; set; }
    public string RequestType { get; set; } = "";
    public string RequestContent { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionStackTrace { get; set; }
    public string? CorrelationId { get; set; }
    public string SourcePattern { get; set; } = "";
    public int TotalRetryAttempts { get; set; }
    public DateTime FirstFailedAtUtc { get; set; }
    public DateTime DeadLetteredAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReplayedAtUtc { get; set; }
    public string? ReplayResult { get; set; }

    public bool IsReplayed => ReplayedAtUtc.HasValue;
    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTime.UtcNow;
}

/// <summary>
/// Concrete implementation of contract tests for in-memory dead letter store.
/// </summary>
public sealed class InMemoryDeadLetterStoreContractTests : IDeadLetterStoreContractTests
{
    private readonly InMemoryDeadLetterStoreForContract _store = new();

    protected override IDeadLetterStore CreateStore() => _store;

    protected override IDeadLetterMessage CreateMessage(Guid id, string sourcePattern)
    {
        return new InMemoryDeadLetterMessageForContract
        {
            Id = id,
            RequestType = "TestRequest",
            RequestContent = "{}",
            ErrorMessage = "Test error",
            SourcePattern = sourcePattern,
            TotalRetryAttempts = 3,
            FirstFailedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            DeadLetteredAtUtc = DateTime.UtcNow
        };
    }

    protected override Task CleanupAsync()
    {
        _store.Clear();
        return Task.CompletedTask;
    }
}
