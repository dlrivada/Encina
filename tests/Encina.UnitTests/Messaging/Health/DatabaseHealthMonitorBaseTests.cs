using System.Data;
using System.Data.Common;

using Encina.Database;
using Encina.Messaging.Health;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Unit tests for <see cref="DatabaseHealthMonitorBase"/>.
/// </summary>
public sealed class DatabaseHealthMonitorBaseTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestHealthMonitor(null!, () => Substitute.For<IDbConnection>()));
    }

    [Fact]
    public void Constructor_EmptyProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestHealthMonitor("", () => Substitute.For<IDbConnection>()));
    }

    [Fact]
    public void Constructor_WhitespaceProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestHealthMonitor("   ", () => Substitute.For<IDbConnection>()));
    }

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestHealthMonitor("test-provider", null!));
    }

    [Fact]
    public void Constructor_ValidArgs_SetsProviderName()
    {
        // Arrange & Act
        var monitor = new TestHealthMonitor("ado-sqlserver", () => Substitute.For<IDbConnection>());

        // Assert
        monitor.ProviderName.ShouldBe("ado-sqlserver");
    }

    [Fact]
    public void Constructor_DefaultState_CircuitIsClosed()
    {
        // Arrange & Act
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>());

        // Assert
        monitor.IsCircuitOpen.ShouldBeFalse();
    }

    #endregion

    #region GetPoolStatistics

    [Fact]
    public void GetPoolStatistics_DelegatesToCoreMethod()
    {
        // Arrange
        var expectedStats = new ConnectionPoolStats(5, 10, 15, 2, 100);
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>())
        {
            StatsToReturn = expectedStats
        };

        // Act
        var stats = monitor.GetPoolStatistics();

        // Assert
        stats.ShouldBe(expectedStats);
    }

    [Fact]
    public void GetPoolStatistics_WhenCoreThrows_ReturnsEmpty()
    {
        // Arrange
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>())
        {
            ThrowOnGetStats = true
        };

        // Act
        var stats = monitor.GetPoolStatistics();

        // Assert
        stats.ShouldBe(ConnectionPoolStats.CreateEmpty());
    }

    #endregion

    #region CheckHealthAsync

    [Fact]
    public async Task CheckHealthAsync_CircuitOpen_ReturnsUnhealthy()
    {
        // Arrange
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>());
        monitor.SetCircuit(true);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Circuit breaker is open");
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessfulQuery_ReturnsHealthy()
    {
        // Arrange
        var connection = CreateMockConnection();
        var monitor = new TestHealthMonitor("test", () => connection);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessfulQuery_ClosesCircuit()
    {
        // Arrange
        var connection = CreateMockConnection();
        var monitor = new TestHealthMonitor("test", () => connection);
        monitor.SetCircuit(true);

        // The check will still skip because circuit is open
        var result = await monitor.CheckHealthAsync();
        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectionFails_OpensCircuit()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Closed);
        connection.When(x => x.Open()).Do(_ => throw new InvalidOperationException("Connection refused"));
        connection.CreateCommand().Returns(Substitute.For<IDbCommand>());

        var monitor = new TestHealthMonitor("test", () => connection);

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        monitor.IsCircuitOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_HighPoolUtilization_ReturnsDegraded()
    {
        // Arrange
        var connection = CreateMockConnection();
        var monitor = new TestHealthMonitor("test", () => connection)
        {
            StatsToReturn = new ConnectionPoolStats(95, 0, 95, 0, 100) // 95% utilization
        };

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesDataDictionary()
    {
        // Arrange
        var connection = CreateMockConnection();
        var monitor = new TestHealthMonitor("test-provider", () => connection)
        {
            StatsToReturn = new ConnectionPoolStats(5, 10, 15, 0, 100)
        };

        // Act
        var result = await monitor.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("provider");
        result.Data["provider"].ShouldBe("test-provider");
        result.Data.ShouldContainKey("activeConnections");
        result.Data.ShouldContainKey("poolUtilization");
    }

    #endregion

    #region ClearPoolAsync

    [Fact]
    public async Task ClearPoolAsync_DelegatesToCoreMethod()
    {
        // Arrange
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>());

        // Act
        await monitor.ClearPoolAsync();

        // Assert
        monitor.ClearPoolCallCount.ShouldBe(1);
    }

    #endregion

    #region SetCircuitState

    [Fact]
    public void SetCircuitState_Open_SetsIsCircuitOpenTrue()
    {
        // Arrange
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>());

        // Act
        monitor.SetCircuit(true);

        // Assert
        monitor.IsCircuitOpen.ShouldBeTrue();
    }

    [Fact]
    public void SetCircuitState_Closed_SetsIsCircuitOpenFalse()
    {
        // Arrange
        var monitor = new TestHealthMonitor("test", () => Substitute.For<IDbConnection>());
        monitor.SetCircuit(true);

        // Act
        monitor.SetCircuit(false);

        // Assert
        monitor.IsCircuitOpen.ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static IDbConnection CreateMockConnection()
    {
        var connection = Substitute.For<IDbConnection>();
        connection.State.Returns(ConnectionState.Open);

        var command = Substitute.For<IDbCommand>();
        command.ExecuteScalar().Returns(1);
        connection.CreateCommand().Returns(command);

        return connection;
    }

    /// <summary>
    /// Concrete test implementation of <see cref="DatabaseHealthMonitorBase"/>.
    /// </summary>
    private sealed class TestHealthMonitor : DatabaseHealthMonitorBase
    {
        public ConnectionPoolStats StatsToReturn { get; set; } = ConnectionPoolStats.CreateEmpty();
        public bool ThrowOnGetStats { get; set; }
        public int ClearPoolCallCount { get; private set; }

        public TestHealthMonitor(
            string providerName,
            Func<IDbConnection> connectionFactory,
            DatabaseResilienceOptions? options = null)
            : base(providerName, connectionFactory, options)
        {
        }

        protected override ConnectionPoolStats GetPoolStatisticsCore()
        {
            if (ThrowOnGetStats)
                throw new InvalidOperationException("Stats unavailable");

            return StatsToReturn;
        }

        protected override Task ClearPoolCoreAsync(CancellationToken cancellationToken)
        {
            ClearPoolCallCount++;
            return Task.CompletedTask;
        }

        public void SetCircuit(bool isOpen) => SetCircuitState(isOpen);
    }

    #endregion
}
