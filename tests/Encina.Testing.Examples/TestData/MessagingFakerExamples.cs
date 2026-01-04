using Encina.Testing.Bogus;
using Encina.Testing.Examples.Domain;
using Encina.Testing.Messaging;
using Shouldly;
using FakeOutbox = Encina.Testing.Fakes.Stores.FakeOutboxStore;
using FakeInbox = Encina.Testing.Fakes.Stores.FakeInboxStore;
using FakeOutboxMsg = Encina.Testing.Fakes.Models.FakeOutboxMessage;
using FakeInboxMsg = Encina.Testing.Fakes.Models.FakeInboxMessage;

namespace Encina.Testing.Examples.TestData;

/// <summary>
/// Examples demonstrating messaging fakers and test helpers with Encina.Testing.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 8.2
/// </summary>
public sealed class MessagingFakerExamples
{
    /// <summary>
    /// Pattern: Basic FakeOutboxStore usage.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_BasicUsage()
    {
        // Arrange
        var store = new FakeOutbox();
        var message = new FakeOutboxMsg
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(OrderCreatedEvent).FullName ?? nameof(OrderCreatedEvent),
            Content = """{"orderId":"12345","customerId":"CUST-001"}""",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(message);
        await store.SaveChangesAsync();

        // Assert - Use WasMessageAdded for verification
        store.WasMessageAdded<OrderCreatedEvent>().ShouldBeTrue();
        store.AddedMessages.Count.ShouldBe(1);
    }

    /// <summary>
    /// Pattern: Verify message by type name string.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_VerifyByTypeName()
    {
        // Arrange
        var store = new FakeOutbox();
        var message = new FakeOutboxMsg
        {
            Id = Guid.NewGuid(),
            NotificationType = "MyApp.Events.OrderCreatedEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(message);

        // Assert - Verify by string type name
        store.WasMessageAdded("MyApp.Events.OrderCreatedEvent").ShouldBeTrue();
        store.WasMessageAdded("NonExistent.Event").ShouldBeFalse();
    }

    /// <summary>
    /// Pattern: Access added messages for detailed assertions.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_AccessAddedMessages()
    {
        // Arrange
        var store = new FakeOutbox();
        var orderId = Guid.NewGuid();
        var message = new FakeOutboxMsg
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(OrderCreatedEvent).FullName!,
            Content = $$$"""{"orderId":"{{{orderId}}}"}""",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(message);

        // Assert - Access messages directly for detailed checks
        var addedMessages = store.AddedMessages;
        addedMessages.Count.ShouldBe(1);
        addedMessages[0].Content.ShouldContain(orderId.ToString());
    }

    /// <summary>
    /// Pattern: Track message processing.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_TrackProcessing()
    {
        // Arrange
        var store = new FakeOutbox();
        var messageId = Guid.NewGuid();
        var message = new FakeOutboxMsg
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };
        await store.AddAsync(message);

        // Act
        await store.MarkAsProcessedAsync(messageId);

        // Assert
        store.ProcessedMessageIds.ShouldContain(messageId);
        var processedMessage = store.GetMessage(messageId);
        processedMessage.ShouldNotBeNull();
        processedMessage.IsProcessed.ShouldBeTrue();
    }

    /// <summary>
    /// Pattern: Track message failures.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_TrackFailures()
    {
        // Arrange
        var store = new FakeOutbox();
        var messageId = Guid.NewGuid();
        var message = new FakeOutboxMsg
        {
            Id = messageId,
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };
        await store.AddAsync(message);

        // Act
        await store.MarkAsFailedAsync(
            messageId,
            errorMessage: "Connection timeout",
            nextRetryAt: DateTime.UtcNow.AddMinutes(5));

        // Assert
        store.FailedMessageIds.ShouldContain(messageId);
        var failedMessage = store.GetMessage(messageId);
        failedMessage!.ErrorMessage.ShouldBe("Connection timeout");
    }

    /// <summary>
    /// Pattern: Get pending messages.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_GetPendingMessages()
    {
        // Arrange
        var store = new FakeOutbox();
        for (int i = 0; i < 5; i++)
        {
            await store.AddAsync(new FakeOutboxMsg
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestEvent",
                Content = $"{{\"index\":{i}}}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // Act
        var pending = await store.GetPendingMessagesAsync(batchSize: 3, maxRetries: 5);

        // Assert
        pending.Count().ShouldBe(3); // Respects batch size
    }

    /// <summary>
    /// Pattern: Clear store for test isolation.
    /// </summary>
    [Fact]
    public async Task FakeOutboxStore_ClearForIsolation()
    {
        // Arrange
        var store = new FakeOutbox();
        await store.AddAsync(new FakeOutboxMsg
        {
            Id = Guid.NewGuid(),
            NotificationType = "TestEvent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Act
        store.Clear();

        // Assert
        store.Messages.Count.ShouldBe(0);
        store.AddedMessages.Count.ShouldBe(0);
    }

    /// <summary>
    /// Pattern: FakeInboxStore for idempotency testing.
    /// </summary>
    [Fact]
    public async Task FakeInboxStore_IdempotencyTesting()
    {
        // Arrange
        var store = new FakeInbox();
        var messageId = "msg-12345";

        // First processing
        await store.AddAsync(new FakeInboxMsg
        {
            MessageId = messageId,
            RequestType = "CreateOrder",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await store.MarkAsProcessedAsync(messageId, response: """{"orderId":"ORD-001"}""");

        // Assert - Check for duplicate processing
        store.IsMessageProcessed(messageId).ShouldBeTrue();

        // Second request with same ID should return cached response
        var cachedMessage = await store.GetMessageAsync(messageId);
        cachedMessage.ShouldNotBeNull();
        cachedMessage.Response.ShouldNotBeNull();
        cachedMessage.Response.ShouldContain("ORD-001");
    }

    /// <summary>
    /// Pattern: OutboxMessageFaker for generating test data.
    /// </summary>
    [Fact]
    public void OutboxMessageFaker_GenerateTestData()
    {
        // Arrange & Act
        var faker = new OutboxMessageFaker();
        var message = faker.Generate();

        // Assert
        message.Id.ShouldNotBe(Guid.Empty);
        message.NotificationType.ShouldNotBeNullOrWhiteSpace();
        message.Content.ShouldNotBeNullOrWhiteSpace();
        message.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    /// <summary>
    /// Pattern: Generate processed outbox messages.
    /// </summary>
    [Fact]
    public void OutboxMessageFaker_GenerateProcessed()
    {
        // Arrange & Act
        var faker = new OutboxMessageFaker().AsProcessed();
        var message = faker.Generate();

        // Assert
        message.IsProcessed.ShouldBeTrue();
        message.ProcessedAtUtc.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: Generate failed outbox messages.
    /// </summary>
    [Fact]
    public void OutboxMessageFaker_GenerateFailed()
    {
        // Arrange & Act
        var faker = new OutboxMessageFaker().AsFailed(retryCount: 3);
        var message = faker.Generate();

        // Assert
        message.RetryCount.ShouldBe(3);
        message.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        message.NextRetryAtUtc.ShouldNotBeNull();
    }

    /// <summary>
    /// Pattern: InboxMessageFaker with states.
    /// </summary>
    [Fact]
    public void InboxMessageFaker_GenerateVariousStates()
    {
        // Pending message
        var pending = new InboxMessageFaker().Generate();
        pending.IsProcessed.ShouldBeFalse();

        // Processed message
        var processed = new InboxMessageFaker().AsProcessed().Generate();
        processed.IsProcessed.ShouldBeTrue();

        // Expired message
        var expired = new InboxMessageFaker().AsExpired().Generate();
        expired.IsExpired().ShouldBeTrue();

        // Failed message
        var failed = new InboxMessageFaker().AsFailed(retryCount: 2).Generate();
        failed.RetryCount.ShouldBe(2);
        failed.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Pattern: OutboxTestHelper Given/When/Then pattern.
    /// </summary>
    [Fact]
    public void OutboxTestHelper_GivenWhenThen()
    {
        // Arrange - Create helper
        var helper = new OutboxTestHelper();

        // Given - Setup initial state
        helper.GivenEmptyOutbox();

        // When - Perform action
        helper.WhenMessageAdded(new OrderCreatedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: "CUST-001",
            Amount: 100m,
            CreatedAtUtc: DateTime.UtcNow));

        // Then - Assert result
        helper.ThenOutboxContains<OrderCreatedEvent>();
        helper.ThenOutboxHasCount(1);
    }

    /// <summary>
    /// Pattern: OutboxTestHelper with pre-existing messages.
    /// </summary>
    [Fact]
    public void OutboxTestHelper_WithPreExistingMessages()
    {
        // Arrange
        var helper = new OutboxTestHelper();

        // Given - Setup with existing messages
        var existingMessage = new FakeOutboxMsg
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(OrderCreatedEvent).FullName!,
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        helper.GivenMessages(existingMessage);

        // When
        helper.WhenMessageProcessed(existingMessage.Id);

        // Then
        helper.ThenMessageWasProcessed(existingMessage.Id);
    }

    /// <summary>
    /// Pattern: OutboxTestHelper time manipulation.
    /// </summary>
    [Fact]
    public void OutboxTestHelper_TimeManipulation()
    {
        // Arrange
        var helper = new OutboxTestHelper();

        // Given
        helper.GivenEmptyOutbox();
        helper.WhenMessageAdded(new OrderCreatedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: "CUST-001",
            Amount: 50m,
            CreatedAtUtc: DateTime.UtcNow));

        // When - Advance time
        helper.AdvanceTimeByMinutes(30);

        // Then - Message should still exist after time advance
        helper.ThenOutboxHasCount(1);
    }

    /// <summary>
    /// Pattern: InboxTestHelper for duplicate detection.
    /// </summary>
    [Fact]
    public void InboxTestHelper_DuplicateDetection()
    {
        // Arrange
        var helper = new InboxTestHelper();
        var messageId = "msg-duplicate-test";

        // Given - Message already processed
        helper.GivenProcessedMessage<string>(
            messageId,
            cachedResponse: "Already processed result",
            requestType: "CreateOrder");

        // Then - Should detect duplicate
        helper.ThenMessageWasAlreadyProcessed(messageId);

        // And can retrieve cached response
        var cached = helper.GetCachedResponse<string>(messageId);
        cached.ShouldBe("Already processed result");
    }

    /// <summary>
    /// Pattern: InboxTestHelper for new message handling.
    /// </summary>
    [Fact]
    public void InboxTestHelper_NewMessageHandling()
    {
        // Arrange
        var helper = new InboxTestHelper();
        var messageId = "msg-new-test";

        // Given - Empty inbox
        helper.GivenEmptyInbox();

        // When - Receive new message
        helper.WhenMessageReceived(messageId);

        // Then - Should be a new message
        helper.ThenMessageIsNew(messageId);
    }
}
