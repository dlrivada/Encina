using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;

namespace Encina.Testing.Fakes.Tests.Stores;

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
        await _sut.AddAsync(message);

        // Assert
        _sut.GetMessages().Count.ShouldBe(1);
        _sut.GetAddedMessages().Count.ShouldBe(1);
        _sut.GetMessage(message.Id).ShouldNotBeNull();
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
        var pending = await _sut.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
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
        var pending = await _sut.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
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
        var pending = await _sut.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);

        // Assert
        pending.ShouldBeEmpty();
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
        await _sut.MarkAsProcessedAsync(message.Id);

        // Assert
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
        await _sut.MarkAsFailedAsync(message.Id, "Test error", nextRetry);

        // Assert
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
}
