using System.Data;
using Encina.Dapper.Oracle.Sagas;
using Encina.Messaging.Sagas;

namespace Encina.Dapper.Oracle.GuardTests;

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
}
