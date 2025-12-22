using System.Data;
using Encina.Dapper.SqlServer.Inbox;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="InboxStoreDapper"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class InboxStoreDapperGuardTests
{
    /// <summary>
    /// Tests that constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        const string tableName = "InboxMessages";

        // Act
        var act = () => new InboxStoreDapper(connection, tableName);

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
        var act = () => new InboxStoreDapper(connection, tableName);

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
        var act = () => new InboxStoreDapper(connection, tableName);

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
        var act = () => new InboxStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that GetMessageAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_NullMessageId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.GetMessageAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that GetMessageAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.GetMessageAsync("", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that GetMessageAsync throws ArgumentException when messageId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetMessageAsync_WhitespaceMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.GetMessageAsync("   ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that AddAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.AddAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("message");
    }

    /// <summary>
    /// Tests that MarkAsProcessedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_NullMessageId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsProcessedAsync(null!, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that MarkAsProcessedAsync throws ArgumentException when messageId is empty.
    /// </summary>
    [Fact]
    public async Task MarkAsProcessedAsync_EmptyMessageId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsProcessedAsync("", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    /// <summary>
    /// Tests that MarkAsFailedAsync throws ArgumentNullException when messageId is null.
    /// </summary>
    [Fact]
    public async Task MarkAsFailedAsync_NullMessageId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsFailedAsync(null!, "error", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
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
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsFailedAsync("", "error", null, CancellationToken.None);

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
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsFailedAsync("msg-123", null!, null, CancellationToken.None);

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
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.MarkAsFailedAsync("msg-123", "", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("errorMessage");
    }

    /// <summary>
    /// Tests that GetExpiredMessagesAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetExpiredMessagesAsync_InvalidBatchSize_ShouldThrowArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.GetExpiredMessagesAsync(batchSize, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(batchSize));
    }

    /// <summary>
    /// Tests that RemoveExpiredMessagesAsync throws ArgumentNullException when messageIds is null.
    /// </summary>
    [Fact]
    public async Task RemoveExpiredMessagesAsync_NullMessageIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.RemoveExpiredMessagesAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("messageIds");
    }

    /// <summary>
    /// Tests that RemoveExpiredMessagesAsync throws ArgumentException when messageIds is empty.
    /// </summary>
    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyMessageIds_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act
        var act = async () => await store.RemoveExpiredMessagesAsync(
            Array.Empty<string>(),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageIds");
    }
}
