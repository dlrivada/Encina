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
        _sut.Messages.Should().HaveCount(1);
        _sut.AddedMessages.Should().HaveCount(1);
        _sut.GetMessage(message.Id).Should().NotBeNull();
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
        pending.Should().HaveCount(1);
        pending.First().Id.Should().Be(pendingMessage.Id);
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
        pending.Should().BeEmpty();
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
        pending.Should().BeEmpty();
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
        updated!.IsProcessed.Should().BeTrue();
        updated.ProcessedAtUtc.Should().NotBeNull();
        _sut.ProcessedMessageIds.Should().Contain(message.Id);
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
        updated!.ErrorMessage.Should().Be("Test error");
        updated.RetryCount.Should().Be(1);
        updated.NextRetryAtUtc.Should().Be(nextRetry);
        _sut.FailedMessageIds.Should().Contain(message.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_IncrementsSaveCount()
    {
        // Act
        await _sut.SaveChangesAsync();
        await _sut.SaveChangesAsync();

        // Assert
        _sut.SaveChangesCallCount.Should().Be(2);
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
        _sut.Messages.Should().BeEmpty();
        _sut.AddedMessages.Should().BeEmpty();
        _sut.ProcessedMessageIds.Should().BeEmpty();
        _sut.FailedMessageIds.Should().BeEmpty();
        _sut.SaveChangesCallCount.Should().Be(0);
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
        _sut.WasMessageAdded("MyApp.OrderCreatedNotification").Should().BeTrue();
        _sut.WasMessageAdded("NonExistent").Should().BeFalse();
    }
}
