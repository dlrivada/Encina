using System.Data;
using System.Data.Common;
using Encina.Messaging.Health;
using NSubstitute;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class DatabaseHealthCheckTests
{
    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestDatabaseHealthCheck("test", null!, null));
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsName()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        Func<IDbConnection> factory = () => connection;

        // Act
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Assert
        healthCheck.Name.ShouldBe("test-db");
    }

    [Fact]
    public void Constructor_WithOptionsName_UsesOptionsName()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        Func<IDbConnection> factory = () => connection;
        var options = new ProviderHealthCheckOptions { Name = "custom-name" };

        // Act
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-name");
    }

    [Fact]
    public void Tags_ContainsDefaultDatabaseTags()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        Func<IDbConnection> factory = () => connection;

        // Act
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionSucceeds_ReturnsHealthy()
    {
        // Arrange
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Closed);

        var command = Substitute.For<DbCommand>();
        command.ExecuteScalarAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        connection.CreateCommand().Returns(command);

        Func<IDbConnection> factory = () => connection;
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionFails_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<DbConnection>();
        connection.OpenAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Connection failed"));

        Func<IDbConnection> factory = () => connection;
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenQueryFails_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);

        var command = Substitute.For<DbCommand>();
        command.ExecuteScalarAsync(Arg.Any<CancellationToken>())
            .Returns<object?>(_ => throw new InvalidOperationException("Query failed"));

        connection.CreateCommand().Returns(command);

        Func<IDbConnection> factory = () => connection;
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAlreadyOpen_DoesNotOpenAgain()
    {
        // Arrange
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);

        var command = Substitute.For<DbCommand>();
        command.ExecuteScalarAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        connection.CreateCommand().Returns(command);

        Func<IDbConnection> factory = () => connection;
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        await connection.DidNotReceive().OpenAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetHealthCheckQuery_ReturnsSelect1ByDefault()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        Func<IDbConnection> factory = () => connection;
        var healthCheck = new TestDatabaseHealthCheck("test-db", factory, null);

        // Act
        var query = healthCheck.GetQueryForTest();

        // Assert
        query.ShouldBe("SELECT 1");
    }

    /// <summary>
    /// Test implementation of DatabaseHealthCheck to expose protected members.
    /// </summary>
    private sealed class TestDatabaseHealthCheck : DatabaseHealthCheck
    {
        public TestDatabaseHealthCheck(
            string name,
            Func<IDbConnection> connectionFactory,
            ProviderHealthCheckOptions? options)
            : base(name, connectionFactory, options)
        {
        }

        public string GetQueryForTest() => GetHealthCheckQuery();
    }
}
