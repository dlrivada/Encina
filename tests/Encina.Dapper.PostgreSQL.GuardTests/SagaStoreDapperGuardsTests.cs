using System.Data;
using Encina.Dapper.PostgreSQL.Sagas;
using Encina.Messaging.Sagas;

namespace Encina.Dapper.PostgreSQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="SagaStoreDapper"/> to verify null parameter handling.
/// </summary>
public class SagaStoreDapperGuardsTests
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
        var act = () => new SagaStoreDapper(connection);
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
        var act = () => new SagaStoreDapper(connection, tableName);
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
        var act = () => new SagaStoreDapper(connection, tableName);
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
        var act = () => new SagaStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    /// <summary>
    /// Verifies that AddAsync throws ArgumentNullException when sagaState is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        ISagaState sagaState = null!;

        // Act & Assert
        var act = async () => await store.AddAsync(sagaState);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sagaState");
    }

    /// <summary>
    /// Verifies that UpdateAsync throws ArgumentNullException when sagaState is null.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NullSagaState_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        ISagaState sagaState = null!;

        // Act & Assert
        var act = async () => await store.UpdateAsync(sagaState);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sagaState");
    }

    /// <summary>
    /// Verifies that GetAsync throws ArgumentException when sagaId is empty.
    /// </summary>
    [Fact]
    public async Task GetAsync_EmptySagaId_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        var sagaId = Guid.Empty;

        // Act & Assert
        var act = async () => await store.GetAsync(sagaId);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sagaId");
    }

    /// <summary>
    /// Verifies that GetStuckSagasAsync throws ArgumentException when olderThan is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetStuckSagasAsync_InvalidOlderThan_ThrowsArgumentException(int seconds)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        var olderThan = TimeSpan.FromSeconds(seconds);

        // Act & Assert
        var act = async () => await store.GetStuckSagasAsync(olderThan, 10);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("olderThan");
    }

    /// <summary>
    /// Verifies that GetStuckSagasAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetStuckSagasAsync_InvalidBatchSize_ThrowsArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        var olderThan = TimeSpan.FromMinutes(30);

        // Act & Assert
        var act = async () => await store.GetStuckSagasAsync(olderThan, batchSize);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName(nameof(batchSize));
    }
}
