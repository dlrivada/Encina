using System.Data;
using Encina.Dapper.Sqlite.Sagas;

namespace Encina.Dapper.Sqlite.GuardTests;

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
    /// Verifies that the constructor throws ArgumentException when tableName is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        string tableName = null!;

        // Act & Assert
        var act = () => new SagaStoreDapper(connection, tableName);
        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
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
}
