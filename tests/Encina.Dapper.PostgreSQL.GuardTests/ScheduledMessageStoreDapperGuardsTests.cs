using System.Data;
using Encina.Dapper.PostgreSQL.Scheduling;
using Encina.Messaging.Scheduling;

namespace Encina.Dapper.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="ScheduledMessageStoreDapper"/> to verify null parameter handling.
/// </summary>
public class ScheduledMessageStoreDapperGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;

        // Act & Assert
        var act = () => new ScheduledMessageStoreDapper(connection);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        string tableName = null!;

        // Act & Assert
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var tableName = string.Empty;

        // Act & Assert
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var tableName = "   ";

        // Act & Assert
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        IScheduledMessage message = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(message);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Verifies that MarkAsProcessedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsProcessedAsync(messageId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, "Error", null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();
        string errorMessage = null!;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that MarkAsFailedAsync throws ArgumentException when errorMessage is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyErrorMessage_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();
        var errorMessage = string.Empty;

        // Act & Assert
        var act = async () => await store.MarkAsFailedAsync(messageId, errorMessage, null);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
    }

    /// <summary>
    /// Verifies that RescheduleRecurringMessageAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task RescheduleRecurringMessageAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.Empty;
        var nextScheduledAt = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        var act = async () => await store.RescheduleRecurringMessageAsync(messageId, nextScheduledAt);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that RescheduleRecurringMessageAsync throws ArgumentException when nextScheduledAtUtc is in the past.
    /// </summary>
    [Fact]
    public async Task RescheduleRecurringMessageAsync_PastScheduledTime_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();
        var nextScheduledAt = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        var act = async () => await store.RescheduleRecurringMessageAsync(messageId, nextScheduledAt);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("nextScheduledAtUtc");
    }

    /// <summary>
    /// Verifies that CancelAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task CancelAsync_EmptyMessageId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.Empty;

        // Act & Assert
        var act = async () => await store.CancelAsync(messageId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("messageId");
    }

    /// <summary>
    /// Verifies that GetDueMessagesAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetDueMessagesAsync_InvalidBatchSize_ThrowsArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetDueMessagesAsync(batchSize, 3);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName(nameof(batchSize));
    }

    /// <summary>
    /// Verifies that GetDueMessagesAsync throws ArgumentException when maxRetries is negative.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetDueMessagesAsync_NegativeMaxRetries_ThrowsArgumentException(int maxRetries)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        var act = async () => await store.GetDueMessagesAsync(10, maxRetries);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName(nameof(maxRetries));
    }
}
