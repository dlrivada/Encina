using System.Data;
using Encina.Dapper.SqlServer.Scheduling;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="ScheduledMessageStoreDapper"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class ScheduledMessageStoreDapperGuardTests
{
    /// <summary>
    /// Tests that constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        const string tableName = "ScheduledMessages";

        // Act
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        string tableName = null!;

        // Act
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        const string tableName = "";

        // Act
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        const string tableName = "   ";

        // Act
        var act = () => new ScheduledMessageStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act
        var act = async () => await store.AddAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("message");
    }

    /// <summary>
    /// Tests that GetDueMessagesAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetDueMessagesAsync_InvalidBatchSize_ShouldThrowArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act
        var act = async () => await store.GetDueMessagesAsync(batchSize, 3, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(batchSize));
    }

    /// <summary>
    /// Tests that GetDueMessagesAsync throws ArgumentException when maxRetries is negative.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetDueMessagesAsync_NegativeMaxRetries_ShouldThrowArgumentException(int maxRetries)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act
        var act = async () => await store.GetDueMessagesAsync(10, maxRetries, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(maxRetries));
    }

    /// <summary>
    /// Tests that MarkAsProcessedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsProcessedAsync(Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsFailedAsync(
            Guid.Empty,
            "error",
            null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentNullException when errorMessage is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullErrorMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act
        var act = async () => await store.MarkAsFailedAsync(
            messageId,
            null!,
            null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("errorMessage");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentException when errorMessage is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_EmptyErrorMessage_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act
        var act = async () => await store.MarkAsFailedAsync(
            messageId,
            "",
            null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("errorMessage");
    }

    /// <summary>
    /// Tests that RescheduleRecurringMessageAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task RescheduleRecurringMessageAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var nextScheduledAt = DateTime.UtcNow.AddHours(1);

        // Act
        var act = async () => await store.RescheduleRecurringMessageAsync(
            Guid.Empty,
            nextScheduledAt,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that RescheduleRecurringMessageAsync throws ArgumentException when nextScheduledAtUtc is in the past.
    /// </summary>
    [Fact]
    public async Task RescheduleRecurringMessageAsync_PastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();
        var pastDate = DateTime.UtcNow.AddHours(-1);

        // Act
        var act = async () => await store.RescheduleRecurringMessageAsync(
            messageId,
            pastDate,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("nextScheduledAtUtc");
    }

    /// <summary>
    /// Tests that CancelAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task CancelAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act
        var act = async () => await store.CancelAsync(Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }
}
