using System.Data;
using Encina.Dapper.PostgreSQL;
using Encina.Dapper.PostgreSQL.Scheduling;
using Encina.Messaging.Scheduling;

namespace Encina.GuardTests.Dapper.PostgreSQL;

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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe(nameof(connection));
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
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe(nameof(tableName));
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
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(tableName));
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
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(tableName));
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
        var act = () => store.AddAsync(message);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe(nameof(message));
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
        var act = () => store.MarkAsProcessedAsync(messageId);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(messageId));
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
        var act = () => store.MarkAsFailedAsync(messageId, "Error", null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(messageId));
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
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe(nameof(errorMessage));
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
        var act = () => store.MarkAsFailedAsync(messageId, errorMessage, null);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(errorMessage));
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
        var act = () => store.RescheduleRecurringMessageAsync(messageId, nextScheduledAt);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(messageId));
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
        var nextScheduledAtUtc = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        var act = () => store.RescheduleRecurringMessageAsync(messageId, nextScheduledAtUtc);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(nextScheduledAtUtc));
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
        var act = () => store.CancelAsync(messageId);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(messageId));
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
        var act = () => store.GetDueMessagesAsync(batchSize, 3);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(batchSize));
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
        var act = () => store.GetDueMessagesAsync(10, maxRetries);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe(nameof(maxRetries));
    }
}
