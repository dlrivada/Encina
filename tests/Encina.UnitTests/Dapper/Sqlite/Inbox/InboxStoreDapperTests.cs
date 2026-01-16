using System.Data;
using Encina.Dapper.Sqlite;
using Encina.Dapper.Sqlite.Inbox;
using NSubstitute;

namespace Encina.UnitTests.Dapper.Sqlite.Inbox;

/// <summary>
/// Unit tests for <see cref="InboxStoreDapper"/>.
/// </summary>
public sealed class InboxStoreDapperTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new InboxStoreDapper(null!));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new InboxStoreDapper(connection);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTableName_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new InboxStoreDapper(connection, "CustomInbox");

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithTimeProvider_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var timeProvider = TimeProvider.System;

        // Act
        var store = new InboxStoreDapper(connection, "InboxMessages", timeProvider);

        // Assert
        store.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTableName_ThrowsArgumentException(string? tableName)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new InboxStoreDapper(connection, tableName!));
    }

    #endregion

    #region GetMessageAsync Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMessageAsync_InvalidMessageId_ThrowsArgumentException(string? messageId)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetMessageAsync(messageId!));
    }

    #endregion

    #region AddAsync Validation Tests

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(null!));
    }

    #endregion

    #region MarkAsProcessedAsync Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MarkAsProcessedAsync_InvalidMessageId_ThrowsArgumentException(string? messageId)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsProcessedAsync(messageId!, null));
    }

    #endregion

    #region MarkAsFailedAsync Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MarkAsFailedAsync_InvalidMessageId_ThrowsArgumentException(string? messageId)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsFailedAsync(messageId!, "Error", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MarkAsFailedAsync_InvalidErrorMessage_ThrowsArgumentException(string? errorMessage)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsFailedAsync("msg-123", errorMessage!, null));
    }

    #endregion

    #region GetExpiredMessagesAsync Validation Tests

    [Fact]
    public async Task GetExpiredMessagesAsync_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await store.GetExpiredMessagesAsync(0));
    }

    [Fact]
    public async Task GetExpiredMessagesAsync_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await store.GetExpiredMessagesAsync(-1));
    }

    #endregion

    #region RemoveExpiredMessagesAsync Validation Tests

    [Fact]
    public async Task RemoveExpiredMessagesAsync_NullMessageIds_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.RemoveExpiredMessagesAsync(null!));
    }

    [Fact]
    public async Task RemoveExpiredMessagesAsync_EmptyMessageIds_ReturnsWithoutException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert - should not throw, just return
        await store.RemoveExpiredMessagesAsync([]);
    }

    #endregion

    #region IncrementRetryCountAsync Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IncrementRetryCountAsync_InvalidMessageId_ThrowsArgumentException(string? messageId)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.IncrementRetryCountAsync(messageId!));
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsCompletedTask()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new InboxStoreDapper(connection);

        // Act & Assert - should not throw
        await store.SaveChangesAsync();
    }

    #endregion
}
