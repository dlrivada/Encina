using System.Data;
using System.Data.Common;

using Encina.Messaging.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Messaging.Services;

/// <summary>
/// Unit tests for <see cref="ConnectionWarmupHostedService"/>.
/// </summary>
public sealed class ConnectionWarmupHostedServiceTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConnectionWarmupHostedService(null!, 5, NullLogger<ConnectionWarmupHostedService>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConnectionWarmupHostedService(() => Substitute.For<IDbConnection>(), 5, null!));
    }

    [Fact]
    public void Constructor_ZeroWarmUpCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ConnectionWarmupHostedService(
                () => Substitute.For<IDbConnection>(),
                0,
                NullLogger<ConnectionWarmupHostedService>.Instance));
    }

    [Fact]
    public void Constructor_NegativeWarmUpCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ConnectionWarmupHostedService(
                () => Substitute.For<IDbConnection>(),
                -1,
                NullLogger<ConnectionWarmupHostedService>.Instance));
    }

    #endregion

    #region StartAsync

    [Fact]
    public async Task StartAsync_OpensAndClosesRequestedNumberOfConnections()
    {
        // Arrange
        var connectionsOpened = 0;
        Func<IDbConnection> factory = () =>
        {
            var conn = Substitute.For<IDbConnection>();
            conn.State.Returns(ConnectionState.Closed);
            conn.When(x => x.Open()).Do(_ => connectionsOpened++);
            return conn;
        };

        var service = new ConnectionWarmupHostedService(
            factory, 5, NullLogger<ConnectionWarmupHostedService>.Instance);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        connectionsOpened.ShouldBe(5);
    }

    [Fact]
    public async Task StartAsync_SkipsAlreadyOpenConnections()
    {
        // Arrange
        var openCallCount = 0;
        Func<IDbConnection> factory = () =>
        {
            var conn = Substitute.For<IDbConnection>();
            conn.State.Returns(ConnectionState.Open);
            conn.When(x => x.Open()).Do(_ => openCallCount++);
            return conn;
        };

        var service = new ConnectionWarmupHostedService(
            factory, 3, NullLogger<ConnectionWarmupHostedService>.Instance);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert — Connection.Open() should not be called since connections are already open
        openCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task StartAsync_ContinuesOnIndividualConnectionFailure()
    {
        // Arrange
        var callCount = 0;
        Func<IDbConnection> factory = () =>
        {
            callCount++;
            var conn = Substitute.For<IDbConnection>();
            conn.State.Returns(ConnectionState.Closed);

            if (callCount == 2)
            {
                conn.When(x => x.Open()).Do(_ => throw new InvalidOperationException("Connection refused"));
            }
            else
            {
                conn.When(x => x.Open()).Do(_ => { });
            }

            return conn;
        };

        var service = new ConnectionWarmupHostedService(
            factory, 3, NullLogger<ConnectionWarmupHostedService>.Instance);

        // Act — Should not throw even though connection 2 fails
        await service.StartAsync(CancellationToken.None);

        // Assert — All 3 connections should have been attempted
        callCount.ShouldBe(3);
    }

    [Fact]
    public async Task StartAsync_RespectsCanclellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var service = new ConnectionWarmupHostedService(
            () => Substitute.For<IDbConnection>(),
            10,
            NullLogger<ConnectionWarmupHostedService>.Instance);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => service.StartAsync(cts.Token));
    }

    #endregion

    #region StopAsync

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        // Arrange
        var service = new ConnectionWarmupHostedService(
            () => Substitute.For<IDbConnection>(),
            1,
            NullLogger<ConnectionWarmupHostedService>.Instance);

        // Act & Assert — Should complete without issue
        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region Logging

    [Fact]
    public async Task StartAsync_LogsWarmupStartAndCompletion()
    {
        // Arrange
        var logger = new FakeLogger<ConnectionWarmupHostedService>();
        var conn = Substitute.For<IDbConnection>();
        conn.State.Returns(ConnectionState.Open);

        var service = new ConnectionWarmupHostedService(
            () => conn, 2, logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        logger.Collector.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    #endregion
}
