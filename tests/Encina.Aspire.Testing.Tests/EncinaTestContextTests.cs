using Encina.Aspire.Testing;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Shouldly;

namespace Encina.Aspire.Testing.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaTestContext"/>.
/// </summary>
public sealed class EncinaTestContextTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new EncinaTestContext(null!));
    }

    [Fact]
    public void Options_ReturnsProvidedOptions()
    {
        // Arrange
        var options = new EncinaTestSupportOptions
        {
            DefaultWaitTimeout = TimeSpan.FromMinutes(2)
        };

        var context = new EncinaTestContext(options);

        // Act & Assert
        context.Options.ShouldBeSameAs(options);
    }

    [Fact]
    public void OutboxStore_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new EncinaTestContext(new EncinaTestSupportOptions());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = context.OutboxStore);
    }

    [Fact]
    public void OutboxStore_WhenConfigured_ReturnsStore()
    {
        // Arrange
        var store = new FakeOutboxStore();
        var context = new EncinaTestContext(new EncinaTestSupportOptions(), outboxStore: store);

        // Act & Assert
        context.OutboxStore.ShouldBeSameAs(store);
    }

    [Fact]
    public void InboxStore_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new EncinaTestContext(new EncinaTestSupportOptions());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = context.InboxStore);
    }

    [Fact]
    public void InboxStore_WhenConfigured_ReturnsStore()
    {
        // Arrange
        var store = new FakeInboxStore();
        var context = new EncinaTestContext(new EncinaTestSupportOptions(), inboxStore: store);

        // Act & Assert
        context.InboxStore.ShouldBeSameAs(store);
    }

    [Fact]
    public void SagaStore_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new EncinaTestContext(new EncinaTestSupportOptions());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = context.SagaStore);
    }

    [Fact]
    public void SagaStore_WhenConfigured_ReturnsStore()
    {
        // Arrange
        var store = new FakeSagaStore();
        var context = new EncinaTestContext(new EncinaTestSupportOptions(), sagaStore: store);

        // Act & Assert
        context.SagaStore.ShouldBeSameAs(store);
    }

    [Fact]
    public void ScheduledMessageStore_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new EncinaTestContext(new EncinaTestSupportOptions());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = context.ScheduledMessageStore);
    }

    [Fact]
    public void ScheduledMessageStore_WhenConfigured_ReturnsStore()
    {
        // Arrange
        var store = new FakeScheduledMessageStore();
        var context = new EncinaTestContext(new EncinaTestSupportOptions(), scheduledMessageStore: store);

        // Act & Assert
        context.ScheduledMessageStore.ShouldBeSameAs(store);
    }

    [Fact]
    public void DeadLetterStore_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new EncinaTestContext(new EncinaTestSupportOptions());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = context.DeadLetterStore);
    }

    [Fact]
    public void DeadLetterStore_WhenConfigured_ReturnsStore()
    {
        // Arrange
        var store = new FakeDeadLetterStore();
        var context = new EncinaTestContext(new EncinaTestSupportOptions(), deadLetterStore: store);

        // Act & Assert
        context.DeadLetterStore.ShouldBeSameAs(store);
    }

    [Fact]
    public void ClearAll_ClearsAllStores_WhenOptionsEnabled()
    {
        // Arrange
        var outboxStore = new FakeOutboxStore();
        var inboxStore = new FakeInboxStore();
        var sagaStore = new FakeSagaStore();
        var scheduledStore = new FakeScheduledMessageStore();
        var deadLetterStore = new FakeDeadLetterStore();

        // Add some data (synchronous wait since ClearAll is synchronous)
        outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" }).GetAwaiter().GetResult();
        inboxStore.AddAsync(new FakeInboxMessage { MessageId = "test-1" }).GetAwaiter().GetResult();
        sagaStore.AddAsync(new FakeSagaState { SagaId = Guid.NewGuid(), SagaType = "Test" }).GetAwaiter().GetResult();
        scheduledStore.AddAsync(new FakeScheduledMessage { Id = Guid.NewGuid() }).GetAwaiter().GetResult();
        deadLetterStore.AddAsync(new FakeDeadLetterMessage { Id = Guid.NewGuid() }).GetAwaiter().GetResult();

        var context = new EncinaTestContext(
            new EncinaTestSupportOptions(),
            outboxStore,
            inboxStore,
            sagaStore,
            scheduledStore,
            deadLetterStore);

        // Act
        context.ClearAll();

        // Assert
        outboxStore.Messages.ShouldBeEmpty();
        inboxStore.Messages.ShouldBeEmpty();
        sagaStore.Sagas.ShouldBeEmpty();
        scheduledStore.Messages.ShouldBeEmpty();
        deadLetterStore.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearAll_RespectsOptions()
    {
        // Arrange
        var outboxStore = new FakeOutboxStore();
        var inboxStore = new FakeInboxStore();

        await outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" });
        await inboxStore.AddAsync(new FakeInboxMessage { MessageId = "test-1" });

        var options = new EncinaTestSupportOptions
        {
            ClearOutboxBeforeTest = false,
            ClearInboxBeforeTest = true
        };

        var context = new EncinaTestContext(options, outboxStore, inboxStore);

        // Act
        context.ClearAll();

        // Assert
        outboxStore.Messages.ShouldNotBeEmpty(); // Should NOT be cleared
        inboxStore.Messages.ShouldBeEmpty(); // Should be cleared
    }

    [Fact]
    public async Task ClearOutbox_ClearsOnlyOutbox()
    {
        // Arrange
        var outboxStore = new FakeOutboxStore();
        var inboxStore = new FakeInboxStore();

        await outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" });
        await inboxStore.AddAsync(new FakeInboxMessage { MessageId = "test-1" });

        var context = new EncinaTestContext(new EncinaTestSupportOptions(), outboxStore, inboxStore);

        // Act
        context.ClearOutbox();

        // Assert
        outboxStore.Messages.ShouldBeEmpty();
        inboxStore.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ClearInbox_ClearsOnlyInbox()
    {
        // Arrange
        var outboxStore = new FakeOutboxStore();
        var inboxStore = new FakeInboxStore();

        await outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" });
        await inboxStore.AddAsync(new FakeInboxMessage { MessageId = "test-1" });

        var context = new EncinaTestContext(new EncinaTestSupportOptions(), outboxStore, inboxStore);

        // Act
        context.ClearInbox();

        // Assert
        outboxStore.Messages.ShouldNotBeEmpty();
        inboxStore.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearSagas_ClearsOnlySagas()
    {
        // Arrange
        var sagaStore = new FakeSagaStore();
        var outboxStore = new FakeOutboxStore();

        await sagaStore.AddAsync(new FakeSagaState { SagaId = Guid.NewGuid(), SagaType = "Test" });
        await outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" });

        var context = new EncinaTestContext(new EncinaTestSupportOptions(), outboxStore, sagaStore: sagaStore);

        // Act
        context.ClearSagas();

        // Assert
        sagaStore.Sagas.ShouldBeEmpty();
        outboxStore.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ClearScheduledMessages_ClearsOnlyScheduled()
    {
        // Arrange
        var scheduledStore = new FakeScheduledMessageStore();
        var outboxStore = new FakeOutboxStore();

        await scheduledStore.AddAsync(new FakeScheduledMessage { Id = Guid.NewGuid() });
        await outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" });

        var context = new EncinaTestContext(new EncinaTestSupportOptions(), outboxStore, scheduledMessageStore: scheduledStore);

        // Act
        context.ClearScheduledMessages();

        // Assert
        scheduledStore.Messages.ShouldBeEmpty();
        outboxStore.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ClearDeadLetter_ClearsOnlyDeadLetter()
    {
        // Arrange
        var deadLetterStore = new FakeDeadLetterStore();
        var outboxStore = new FakeOutboxStore();

        await deadLetterStore.AddAsync(new FakeDeadLetterMessage { Id = Guid.NewGuid() });
        await outboxStore.AddAsync(new FakeOutboxMessage { Id = Guid.NewGuid(), NotificationType = "Test" });

        var context = new EncinaTestContext(new EncinaTestSupportOptions(), outboxStore, deadLetterStore: deadLetterStore);

        // Act
        context.ClearDeadLetter();

        // Assert
        deadLetterStore.Messages.ShouldBeEmpty();
        outboxStore.Messages.ShouldNotBeEmpty();
    }
}
