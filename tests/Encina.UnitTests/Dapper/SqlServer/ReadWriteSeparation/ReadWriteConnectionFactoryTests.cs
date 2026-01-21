using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Microsoft.Data.SqlClient;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteConnectionFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReadWriteConnectionFactoryTests
{
    private const string WriteConnectionString = "Server=primary;Database=test;";
    private const string ReadConnectionString = "Server=replica;Database=test;";

    private readonly IReadWriteConnectionSelector _connectionSelector;

    public ReadWriteConnectionFactoryTests()
    {
        _connectionSelector = Substitute.For<IReadWriteConnectionSelector>();
        _connectionSelector.GetWriteConnectionString().Returns(WriteConnectionString);
        _connectionSelector.GetReadConnectionString().Returns(ReadConnectionString);
        _connectionSelector.GetConnectionString().Returns(WriteConnectionString);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnectionSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteConnectionFactory(null!));
    }

    [Fact]
    public void Constructor_WithValidConnectionSelector_DoesNotThrow()
    {
        // Act
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Assert
        factory.ShouldNotBeNull();
    }

    #endregion

    #region CreateWriteConnection Tests

    [Fact]
    public void CreateWriteConnection_CallsGetWriteConnectionString()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection = factory.CreateWriteConnection();

        // Assert
        _connectionSelector.Received(1).GetWriteConnectionString();
        connection.ShouldNotBeNull();
        connection.ConnectionString.ShouldBe(WriteConnectionString);
        connection.Dispose();
    }

    [Fact]
    public void CreateWriteConnection_ReturnsSqlConnection()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection = factory.CreateWriteConnection();

        // Assert
        connection.ShouldBeOfType<SqlConnection>();
        connection.Dispose();
    }

    [Fact]
    public void CreateWriteConnection_CreatesNewConnectionEachTime()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection1 = factory.CreateWriteConnection();
        var connection2 = factory.CreateWriteConnection();

        // Assert
        connection1.ShouldNotBeSameAs(connection2);
        connection1.Dispose();
        connection2.Dispose();
    }

    #endregion

    #region CreateReadConnection Tests

    [Fact]
    public void CreateReadConnection_CallsGetReadConnectionString()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection = factory.CreateReadConnection();

        // Assert
        _connectionSelector.Received(1).GetReadConnectionString();
        connection.ShouldNotBeNull();
        connection.ConnectionString.ShouldBe(ReadConnectionString);
        connection.Dispose();
    }

    [Fact]
    public void CreateReadConnection_ReturnsSqlConnection()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection = factory.CreateReadConnection();

        // Assert
        connection.ShouldBeOfType<SqlConnection>();
        connection.Dispose();
    }

    #endregion

    #region CreateConnection Tests

    [Fact]
    public void CreateConnection_CallsGetConnectionString()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        _connectionSelector.Received(1).GetConnectionString();
        connection.ShouldNotBeNull();
        connection.Dispose();
    }

    [Fact]
    public void CreateConnection_ReturnsSqlConnection()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.ShouldBeOfType<SqlConnection>();
        connection.Dispose();
    }

    #endregion

    #region Async Methods - Cancellation Tests

    [Fact]
    public async Task CreateWriteConnectionAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await factory.CreateWriteConnectionAsync(cts.Token));
    }

    [Fact]
    public async Task CreateReadConnectionAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await factory.CreateReadConnectionAsync(cts.Token));
    }

    [Fact]
    public async Task CreateConnectionAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await factory.CreateConnectionAsync(cts.Token));
    }

    #endregion

    #region GetWriteConnectionString Tests

    [Fact]
    public void GetWriteConnectionString_ReturnsConnectionStringFromSelector()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connectionString = factory.GetWriteConnectionString();

        // Assert
        connectionString.ShouldBe(WriteConnectionString);
        _connectionSelector.Received(1).GetWriteConnectionString();
    }

    #endregion

    #region GetReadConnectionString Tests

    [Fact]
    public void GetReadConnectionString_ReturnsConnectionStringFromSelector()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Act
        var connectionString = factory.GetReadConnectionString();

        // Assert
        connectionString.ShouldBe(ReadConnectionString);
        _connectionSelector.Received(1).GetReadConnectionString();
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIReadWriteConnectionFactory()
    {
        // Arrange
        var factory = new ReadWriteConnectionFactory(_connectionSelector);

        // Assert
        (factory is IReadWriteConnectionFactory).ShouldBeTrue();
    }

    #endregion
}
