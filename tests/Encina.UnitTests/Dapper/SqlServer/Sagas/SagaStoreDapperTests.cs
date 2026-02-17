using System.Data;
using Encina.Dapper.SqlServer.Sagas;
using NSubstitute;

namespace Encina.UnitTests.Dapper.SqlServer.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaStoreDapper"/>.
/// </summary>
public sealed class SagaStoreDapperTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SagaStoreDapper(null!));
    }

    [Fact]
    public void Constructor_WithValidConnection_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new SagaStoreDapper(connection);

        // Assert
        store.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomTableName_CreatesInstance()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();

        // Act
        var store = new SagaStoreDapper(connection, "CustomSagas");

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
        var store = new SagaStoreDapper(connection, "SagaStates", timeProvider);

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
            new SagaStoreDapper(connection, tableName!));
    }

    #endregion

    #region GetAsync Validation Tests

    [Fact]
    public async Task GetAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetAsync(Guid.Empty));
    }

    #endregion

    #region AddAsync Validation Tests

    [Fact]
    public async Task AddAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.AddAsync(null!));
    }

    #endregion

    #region UpdateAsync Validation Tests

    [Fact]
    public async Task UpdateAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.UpdateAsync(null!));
    }

    #endregion

    #region GetStuckSagasAsync Validation Tests

    [Fact]
    public async Task GetStuckSagasAsync_ZeroTimeSpan_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetStuckSagasAsync(TimeSpan.Zero, 10));
    }

    [Fact]
    public async Task GetStuckSagasAsync_NegativeTimeSpan_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetStuckSagasAsync(TimeSpan.FromMinutes(-5), 10));
    }

    [Fact]
    public async Task GetStuckSagasAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetStuckSagasAsync(TimeSpan.FromMinutes(10), 0));
    }

    [Fact]
    public async Task GetStuckSagasAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetStuckSagasAsync(TimeSpan.FromMinutes(10), -1));
    }

    #endregion

    #region GetExpiredSagasAsync Validation Tests

    [Fact]
    public async Task GetExpiredSagasAsync_ZeroBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetExpiredSagasAsync(0));
    }

    [Fact]
    public async Task GetExpiredSagasAsync_NegativeBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await store.GetExpiredSagasAsync(-1));
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ReturnsCompletedTask()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act & Assert - should not throw
        await store.SaveChangesAsync();
    }

    #endregion
}
