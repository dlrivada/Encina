using System.Data;
using Encina.Dapper.Sqlite.Scheduling;
using NSubstitute;

namespace Encina.Dapper.Sqlite.Tests.Scheduling;

/// <summary>
/// Unit tests for <see cref="ScheduledMessageStoreDapper"/>.
/// </summary>
public sealed class ScheduledMessageStoreDapperTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ScheduledMessageStoreDapper(null!));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new ScheduledMessageStoreDapper(connection);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTableName_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new ScheduledMessageStoreDapper(connection, "CustomScheduled");

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
        var store = new ScheduledMessageStoreDapper(connection, "ScheduledMessages", timeProvider);

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
            new ScheduledMessageStoreDapper(connection, tableName!));
    }

    #endregion

    #region AddAsync Validation Tests

    [Fact]
    public async Task AddAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(null!));
    }

    #endregion

    #region GetDueMessagesAsync Validation Tests

    [Fact]
    public async Task GetDueMessagesAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetDueMessagesAsync(0, 3));
    }

    [Fact]
    public async Task GetDueMessagesAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetDueMessagesAsync(-1, 3));
    }

    [Fact]
    public async Task GetDueMessagesAsync_NegativeMaxRetries_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetDueMessagesAsync(10, -1));
    }

    #endregion

    #region MarkAsProcessedAsync Validation Tests

    [Fact]
    public async Task MarkAsProcessedAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

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
        var store = new ScheduledMessageStoreDapper(connection);

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
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.MarkAsFailedAsync(messageId, errorMessage!, null));
    }

    #endregion

    #region RescheduleRecurringMessageAsync Validation Tests

    [Fact]
    public async Task RescheduleRecurringMessageAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.RescheduleRecurringMessageAsync(Guid.Empty, DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task RescheduleRecurringMessageAsync_PastDate_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);
        var messageId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.RescheduleRecurringMessageAsync(messageId, DateTime.UtcNow.AddHours(-1)));
    }

    #endregion

    #region CancelAsync Validation Tests

    [Fact]
    public async Task CancelAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.CancelAsync(Guid.Empty));
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsCompletedTask()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new ScheduledMessageStoreDapper(connection);

        // Act & Assert - should not throw
        await store.SaveChangesAsync();
    }

    #endregion
}
