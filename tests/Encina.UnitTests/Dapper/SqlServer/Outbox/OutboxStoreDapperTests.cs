using System.Data;
using Encina.Dapper.SqlServer.Outbox;
using NSubstitute;

namespace Encina.UnitTests.Dapper.SqlServer.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxStoreDapper"/>.
/// </summary>
public sealed class OutboxStoreDapperTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxStoreDapper(null!));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new OutboxStoreDapper(connection);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTableName_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new OutboxStoreDapper(connection, "CustomOutbox");

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
            new OutboxStoreDapper(connection, tableName!));
    }

    #endregion

    #region AddAsync Validation Tests

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(null!));
    }

    #endregion

    #region GetPendingMessagesAsync Validation Tests

    [Fact]
    public async Task GetPendingMessagesAsync_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await store.GetPendingMessagesAsync(0, 3));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await store.GetPendingMessagesAsync(-1, 3));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_NegativeMaxRetries_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await store.GetPendingMessagesAsync(10, -1));
    }

    #endregion

    #region MarkAsProcessedAsync Validation Tests

    [Fact]
    public async Task MarkAsProcessedAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsProcessedAsync(Guid.Empty));
    }

    #endregion

    #region MarkAsFailedAsync Validation Tests

    [Fact]
    public async Task MarkAsFailedAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsFailedAsync(Guid.Empty, "Error", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MarkAsFailedAsync_InvalidErrorMessage_ThrowsArgumentException(string? errorMessage)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsFailedAsync(messageId, errorMessage!, null));
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsCompletedTask()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new OutboxStoreDapper(connection);

        // Act & Assert - should not throw
        await store.SaveChangesAsync();
    }

    #endregion
}
