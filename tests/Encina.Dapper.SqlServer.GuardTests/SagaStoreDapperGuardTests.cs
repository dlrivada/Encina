using System.Data;
using Encina.Dapper.SqlServer.Sagas;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="SagaStoreDapper"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class SagaStoreDapperGuardTests
{
    /// <summary>
    /// Tests that constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;
        const string tableName = "SagaStates";

        // Act
        var act = () => new SagaStoreDapper(connection, tableName);

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
        var act = () => new SagaStoreDapper(connection, tableName);

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
        var act = () => new SagaStoreDapper(connection, tableName);

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
        var act = () => new SagaStoreDapper(connection, tableName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    /// <summary>
    /// Tests that GetAsync throws ArgumentException when sagaId is empty.
    /// </summary>
    [Fact]
    public async Task GetAsync_EmptySagaId_ShouldThrowArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act
        var act = async () => await store.GetAsync(Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sagaId");
    }

    /// <summary>
    /// Tests that AddAsync throws ArgumentNullException when sagaState is null.
    /// </summary>
    [Fact]
    public async Task AddAsync_NullSagaState_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act
        var act = async () => await store.AddAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sagaState");
    }

    /// <summary>
    /// Tests that UpdateAsync throws ArgumentNullException when sagaState is null.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NullSagaState_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);

        // Act
        var act = async () => await store.UpdateAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sagaState");
    }

    /// <summary>
    /// Tests that GetStuckSagasAsync throws ArgumentException when olderThan is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public async Task GetStuckSagasAsync_InvalidOlderThan_ShouldThrowArgumentException(int seconds)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        var olderThan = TimeSpan.FromSeconds(seconds);

        // Act
        var act = async () => await store.GetStuckSagasAsync(olderThan, 10, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("olderThan");
    }

    /// <summary>
    /// Tests that GetStuckSagasAsync throws ArgumentException when batchSize is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetStuckSagasAsync_InvalidBatchSize_ShouldThrowArgumentException(int batchSize)
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var store = new SagaStoreDapper(connection);
        var olderThan = TimeSpan.FromMinutes(30);

        // Act
        var act = async () => await store.GetStuckSagasAsync(olderThan, batchSize, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName(nameof(batchSize));
    }
}
