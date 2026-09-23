using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Testing.Fakes;

/// <summary>
/// Unit tests for <see cref="FakeOutboxStore"/>.
/// </summary>
public sealed class FakeOutboxStoreTests
{
    private readonly FakeOutboxStore _sut = new();

    [Fact]
    public async Task AddAsync_StoresMessage()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{\"test\": true}"
        };

        // Act
        var result = await _sut.AddAsync(message);

        // Assert
        result.IsRight.ShouldBeTrue();
        _sut.GetMessages().Count.ShouldBe(1);
        _sut.GetAddedMessages().Count.ShouldBe(1);
        _sut.GetMessage(message.Id).ShouldNotBeNull();
    }

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _sut.AddAsync(null!));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ReturnsPendingMessages()
    {
        // Arrange
        var pendingMessage = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        var processedMessage = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            ProcessedAtUtc = DateTime.UtcNow
        };

        await _sut.AddAsync(pendingMessage);
        await _sut.AddAsync(processedMessage);

        // Act
        var result = await _sut.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        result.IsRight.ShouldBeTrue();
        var pending = result.Match(Right: r => r, Left: _ => default!);
        pending.Count().ShouldBe(1);
        pending.First().Id.ShouldBe(pendingMessage.Id);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ExcludesDeadLetteredMessages()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = 5
        };

        await _sut.AddAsync(message);

        // Act
        var result = await _sut.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        result.IsRight.ShouldBeTrue();
        var pending = result.Match(Right: r => r, Left: _ => default!);
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_RespectsNextRetryTime()
    {
        // Arrange
        var futureRetryMessage = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        await _sut.AddAsync(futureRetryMessage);

        // Act
        var result = await _sut.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        result.IsRight.ShouldBeTrue();
        var pending = result.Match(Right: r => r, Left: _ => default!);
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_RespectsBatchSize()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await _sut.AddAsync(new FakeOutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestNotification",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var result = await _sut.GetPendingMessagesAsync(batchSize: 3, maxRetries: 3);

        // Assert
        result.IsRight.ShouldBeTrue();
        var pending = result.Match(Right: r => r, Left: _ => default!);
        pending.Count().ShouldBe(3);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_UpdatesMessage()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}"
        };
        await _sut.AddAsync(message);

        // Act
        var result = await _sut.MarkAsProcessedAsync(message.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = _sut.GetMessage(message.Id);
        updated!.IsProcessed.ShouldBeTrue();
        updated.ProcessedAtUtc.ShouldNotBeNull();
        _sut.GetProcessedMessageIds().ShouldContain(message.Id);
    }

    [Fact]
    public async Task MarkAsFailedAsync_UpdatesMessageAndIncrementsRetry()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}"
        };
        await _sut.AddAsync(message);
        var nextRetry = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = await _sut.MarkAsFailedAsync(message.Id, "Test error", nextRetry);

        // Assert
        result.IsRight.ShouldBeTrue();
        var updated = _sut.GetMessage(message.Id);
        updated!.ErrorMessage.ShouldBe("Test error");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldBe(nextRetry);
        _sut.GetFailedMessageIds().ShouldContain(message.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_IncrementsSaveCount()
    {
        // Act
        await _sut.SaveChangesAsync();
        await _sut.SaveChangesAsync();

        // Assert
        _sut.SaveChangesCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Clear_ResetsAllState()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}"
        };
        await _sut.AddAsync(message);
        await _sut.SaveChangesAsync();

        // Act
        _sut.Clear();

        // Assert
        _sut.GetMessages().ShouldBeEmpty();
        _sut.GetAddedMessages().ShouldBeEmpty();
        _sut.GetProcessedMessageIds().ShouldBeEmpty();
        _sut.GetFailedMessageIds().ShouldBeEmpty();
        _sut.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ClearTracking_KeepsMessagesButResetsTracking()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}"
        };
        await _sut.AddAsync(message);
        await _sut.MarkAsProcessedAsync(message.Id);
        await _sut.SaveChangesAsync();

        // Act
        _sut.ClearTracking();

        // Assert
        _sut.GetMessages().Count.ShouldBe(1);
        _sut.GetAddedMessages().ShouldBeEmpty();
        _sut.GetProcessedMessageIds().ShouldBeEmpty();
        _sut.GetFailedMessageIds().ShouldBeEmpty();
        _sut.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task WasMessageAdded_ByTypeName_ReturnsTrue()
    {
        // Arrange
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "MyApp.OrderCreatedNotification",
            Content = "{}"
        };
        await _sut.AddAsync(message);

        // Assert
        _sut.WasMessageAdded("MyApp.OrderCreatedNotification").ShouldBeTrue();
        _sut.WasMessageAdded("NonExistent").ShouldBeFalse();
    }

    [Fact]
    public async Task GetMessage_NonExistentId_ReturnsNull()
    {
        // Act
        var result = _sut.GetMessage(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task MarkAsProcessedAsync_NonExistentId_DoesNotThrow()
    {
        // Act & Assert — should not throw, returns Right(Unit)
        var result = await _sut.MarkAsProcessedAsync(Guid.NewGuid());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsFailedAsync_NonExistentId_DoesNotThrow()
    {
        // Act & Assert — should not throw, returns Right(Unit)
        var result = await _sut.MarkAsFailedAsync(Guid.NewGuid(), "error", DateTime.UtcNow);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithTimeProvider_UsesProvidedProvider()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Act
        var store = new FakeOutboxStore(fakeTime);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPendingAndExhaustedCounts_SplitUnprocessedMessagesByRetryLimit()
    {
        // Arrange
        await AddAsync(retryCount: 0);
        await AddAsync(retryCount: 2);
        await AddAsync(retryCount: 3);
        await AddAsync(retryCount: 3, processed: true);

        // Act
        var pending = await _sut.GetPendingCountAsync(3);
        var exhausted = await _sut.GetExhaustedCountAsync(3);

        // Assert
        pending.Match(Right: c => c, Left: _ => -1).ShouldBe(2);
        exhausted.Match(Right: c => c, Left: _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_All_ResetsExhaustedMessages()
    {
        // Arrange
        var exhaustedId = await AddAsync(retryCount: 5);
        var processedId = await AddAsync(retryCount: 5, processed: true);

        // Act
        var result = await _sut.RequeueExhaustedAsync(3, null);

        // Assert
        result.Match(Right: c => c, Left: _ => -1).ShouldBe(1);
        var message = _sut.GetMessage(exhaustedId)!;
        message.RetryCount.ShouldBe(0);
        message.NextRetryAtUtc.ShouldBeNull();
        message.ErrorMessage.ShouldBeNull();
        _sut.GetMessage(processedId)!.RetryCount.ShouldBe(5);
    }

    [Fact]
    public async Task RequeueExhaustedAsync_ByIds_ResetsOnlyRequestedMessages()
    {
        // Arrange
        var requested = await AddAsync(retryCount: 3);
        var other = await AddAsync(retryCount: 3);

        // Act
        var result = await _sut.RequeueExhaustedAsync(3, [requested]);

        // Assert
        result.Match(Right: c => c, Left: _ => -1).ShouldBe(1);
        _sut.GetMessage(requested)!.RetryCount.ShouldBe(0);
        _sut.GetMessage(other)!.RetryCount.ShouldBe(3);
    }

    private async Task<Guid> AddAsync(int retryCount, bool processed = false)
    {
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestNotification",
            Content = "{}",
            RetryCount = retryCount,
            ErrorMessage = processed ? null : "failed",
            ProcessedAtUtc = processed ? DateTime.UtcNow : null
        };

        (await _sut.AddAsync(message)).IsRight.ShouldBeTrue();
        return message.Id;
    }
}
