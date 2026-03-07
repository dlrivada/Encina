using Encina.ADO.SqlServer.Inbox;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Xunit;

namespace Encina.IntegrationTests.ADO.SqlServer.Inbox;

/// <summary>
/// Integration tests for <see cref="InboxStoreADO"/>.
/// Tests all public methods with various scenarios using real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class InboxStoreADOTests : IAsyncLifetime
{
    private static readonly string[] s_twoMessageIds = ["msg-1", "msg-2"];
    private static readonly string[] s_oneMessageId = ["msg-1"];

    private readonly SqlServerFixture _fixture;
    private InboxStoreADO _store = null!;

    public InboxStoreADOTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _store = new InboxStoreADO(_fixture.CreateConnection());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidMessage_ShouldInsertToDatabase()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };

        // Act
        (await _store.AddAsync(message)).ShouldBeRight();

        // Assert
        var retrievedOption = (await _store.GetMessageAsync(message.MessageId)).ShouldBeRight();
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = retrievedOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
    }

    [Fact]
    public async Task AddAsync_ValidMessage_ShouldPersistAllProperties()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "ProcessOrderCommand",
            ReceivedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            RetryCount = 0
        };

        // Act
        (await _store.AddAsync(message)).ShouldBeRight();

        // Assert
        var retrievedOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = retrievedOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal(messageId, retrieved.MessageId);
        Assert.Equal("ProcessOrderCommand", retrieved.RequestType);
        Assert.Equal(0, retrieved.RetryCount);
        Assert.Null(retrieved.ProcessedAtUtc);
        Assert.Null(retrieved.Response);
        Assert.Null(retrieved.ErrorMessage);
    }

    [Fact]
    public async Task AddAsync_MessageWithResponse_ShouldPersistResponse()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            Response = "{\"orderId\":12345,\"status\":\"completed\"}",
            RetryCount = 0
        };

        // Act
        (await _store.AddAsync(message)).ShouldBeRight();

        // Assert
        var retrievedOption = (await _store.GetMessageAsync(message.MessageId)).ShouldBeRight();
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = retrievedOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal("{\"orderId\":12345,\"status\":\"completed\"}", retrieved.Response);
        Assert.NotNull(retrieved.ProcessedAtUtc);
    }

    #endregion

    #region GetMessageAsync Tests

    [Fact]
    public async Task GetMessageAsync_ExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();

        // Assert
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal(messageId, result.MessageId);
    }

    [Fact]
    public async Task GetMessageAsync_NonExistentMessage_ShouldReturnNull()
    {
        // Act
        var resultOption = (await _store.GetMessageAsync("non-existent-id")).ShouldBeRight();

        // Assert
        resultOption.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMessageAsync_AfterProcessing_ShouldReturnProcessedMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();
        (await _store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}")).ShouldBeRight();

        // Act
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();

        // Assert
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.NotNull(result.ProcessedAtUtc);
        Assert.Equal("{\"result\":\"success\"}", result.Response);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region MarkAsProcessedAsync Tests

    [Fact]
    public async Task MarkAsProcessedAsync_ValidMessage_ShouldSetProcessedAtUtc()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.MarkAsProcessedAsync(messageId, null!)).ShouldBeRight();

        // Assert
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.NotNull(result.ProcessedAtUtc);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WithResponse_ShouldPersistResponse()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();
        var response = "{\"orderId\":999,\"total\":249.99}";

        // Act
        (await _store.MarkAsProcessedAsync(messageId, response)).ShouldBeRight();

        // Assert
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal(response, result.Response);
        Assert.NotNull(result.ProcessedAtUtc);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_PreviouslyFailed_ShouldClearErrorMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            ErrorMessage = "Previous error",
            RetryCount = 1
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.MarkAsProcessedAsync(messageId, "{\"status\":\"recovered\"}")).ShouldBeRight();

        // Assert
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.ProcessedAtUtc);
    }

    #endregion

    #region MarkAsFailedAsync Tests

    [Fact]
    public async Task MarkAsFailedAsync_ValidMessage_ShouldSetErrorMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.MarkAsFailedAsync(messageId, "Processing failed", null)).ShouldBeRight();

        // Assert
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal("Processing failed", result.ErrorMessage);
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldIncrementRetryCount()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.MarkAsFailedAsync(messageId, "First failure", DateTime.UtcNow.AddMinutes(5))).ShouldBeRight();
        (await _store.MarkAsFailedAsync(messageId, "Second failure", DateTime.UtcNow.AddMinutes(10))).ShouldBeRight();

        // Assert
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.Equal(2, result.RetryCount);
    }

    [Fact]
    public async Task MarkAsFailedAsync_WithNextRetry_ShouldSetNextRetryAtUtc()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();
        var nextRetry = DateTime.UtcNow.AddMinutes(15);

        // Act
        (await _store.MarkAsFailedAsync(messageId, "Temporary failure", nextRetry)).ShouldBeRight();

        // Assert
        var resultOption = (await _store.GetMessageAsync(messageId)).ShouldBeRight();
        resultOption.IsSome.ShouldBeTrue();
        var result = resultOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));
        Assert.NotNull(result.NextRetryAtUtc);
        // Allow small tolerance for DateTime comparison
        Assert.True(Math.Abs((result.NextRetryAtUtc.Value - nextRetry).TotalSeconds) < 2);
    }

    #endregion

    #region GetExpiredMessagesAsync Tests

    [Fact]
    public async Task GetExpiredMessagesAsync_NoExpiredMessages_ShouldReturnEmpty()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30), // Future expiry
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        var results = (await _store.GetExpiredMessagesAsync(10)).ShouldBeRight();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ExpiredAndProcessed_ShouldReturnMessage()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5), // Expired
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        var results = (await _store.GetExpiredMessagesAsync(10)).ShouldBeRight();

        // Assert
        Assert.Single(results);
        Assert.Equal(message.MessageId, results.First().MessageId);
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_ExpiredButNotProcessed_ShouldNotReturn()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = null, // Not processed
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5), // Expired
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        var results = (await _store.GetExpiredMessagesAsync(10)).ShouldBeRight();

        // Assert
        Assert.Empty(results); // Should not return unprocessed messages
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_BatchSize_ShouldLimitResults()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var message = new InboxMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
                ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(-i - 1), // Different expiry times
                RetryCount = 0
            };
            (await _store.AddAsync(message)).ShouldBeRight();
        }

        // Act
        var results = (await _store.GetExpiredMessagesAsync(3)).ShouldBeRight();

        // Assert
        Assert.Equal(3, results.Count());
    }

    #endregion

    #region RemoveExpiredMessagesAsync Tests

    [Fact]
    public async Task RemoveExpiredMessagesAsync_ValidIds_ShouldDeleteMessages()
    {
        // Arrange
        var message1 = new InboxMessage
        {
            MessageId = "msg-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5),
            RetryCount = 0
        };
        var message2 = new InboxMessage
        {
            MessageId = "msg-2",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-40),
            ProcessedAtUtc = DateTime.UtcNow.AddDays(-35),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-5),
            RetryCount = 0
        };
        (await _store.AddAsync(message1)).ShouldBeRight();
        (await _store.AddAsync(message2)).ShouldBeRight();

        // Act
        (await _store.RemoveExpiredMessagesAsync(s_twoMessageIds)).ShouldBeRight();

        // Assert
        var msg1Option = (await _store.GetMessageAsync("msg-1")).ShouldBeRight();
        var msg2Option = (await _store.GetMessageAsync("msg-2")).ShouldBeRight();
        msg1Option.IsNone.ShouldBeTrue();
        msg2Option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_PartialIds_ShouldDeleteOnlySpecified()
    {
        // Arrange
        var message1 = new InboxMessage
        {
            MessageId = "msg-1",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        var message2 = new InboxMessage
        {
            MessageId = "msg-2",
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message1)).ShouldBeRight();
        (await _store.AddAsync(message2)).ShouldBeRight();

        // Act
        (await _store.RemoveExpiredMessagesAsync(s_oneMessageId)).ShouldBeRight();

        // Assert
        var msg1Option = (await _store.GetMessageAsync("msg-1")).ShouldBeRight();
        var msg2Option = (await _store.GetMessageAsync("msg-2")).ShouldBeRight();
        msg1Option.IsNone.ShouldBeTrue(); // Deleted
        msg2Option.IsSome.ShouldBeTrue(); // Still exists
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyList_ShouldNotAffectDatabase()
    {
        // Arrange
        var message = new InboxMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RequestType = "TestRequest",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RetryCount = 0
        };
        (await _store.AddAsync(message)).ShouldBeRight();

        // Act
        (await _store.RemoveExpiredMessagesAsync(Array.Empty<string>())).ShouldBeRight();

        // Assert
        var retrievedOption = (await _store.GetMessageAsync(message.MessageId)).ShouldBeRight();
        retrievedOption.IsSome.ShouldBeTrue();
        var retrieved = retrievedOption.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some")); // Should still exist
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldCompleteSuccessfully()
    {
        // Act & Assert - Should not throw
        (await _store.SaveChangesAsync()).ShouldBeRight();
    }

    #endregion
}
