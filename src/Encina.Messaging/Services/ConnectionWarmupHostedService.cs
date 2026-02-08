using System.Data;
using System.Data.Common;
using Encina.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Services;

/// <summary>
/// A hosted service that warms up database connections during application startup.
/// </summary>
/// <remarks>
/// <para>
/// Connection warm-up reduces the latency of early requests by ensuring that a minimum
/// number of connections are already established and pooled before the application starts
/// accepting traffic.
/// </para>
/// <para>
/// The service opens connections sequentially up to the configured
/// <see cref="DatabaseResilienceOptions.WarmUpConnections"/> count. Each connection is
/// opened and then immediately closed, returning it to the pool for reuse.
/// </para>
/// <para>
/// This service is only registered when <see cref="DatabaseResilienceOptions.WarmUpConnections"/>
/// is greater than zero. It runs once during <see cref="IHostedService.StartAsync"/> and
/// performs no work during the application's lifetime.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseResilience(options =>
///     {
///         options.WarmUpConnections = 5; // Warm up 5 connections on startup
///     });
/// });
/// </code>
/// </example>
public sealed partial class ConnectionWarmupHostedService : IHostedService
{
    private readonly Func<IDbConnection> _connectionFactory;
    private readonly int _warmUpCount;
    private readonly ILogger<ConnectionWarmupHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionWarmupHostedService"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory function to create database connections.</param>
    /// <param name="warmUpCount">The number of connections to warm up.</param>
    /// <param name="logger">The logger instance.</param>
    public ConnectionWarmupHostedService(
        Func<IDbConnection> connectionFactory,
        int warmUpCount,
        ILogger<ConnectionWarmupHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(warmUpCount);

        _connectionFactory = connectionFactory;
        _warmUpCount = warmUpCount;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.WarmupStarting(_logger, _warmUpCount);

        var warmedUp = 0;

        for (var i = 0; i < _warmUpCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var connection = _connectionFactory();

                if (connection.State != ConnectionState.Open)
                {
                    if (connection is DbConnection dbConnection)
                    {
                        await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        connection.Open();
                    }
                }

                warmedUp++;

                // Connection is disposed here, returning it to the pool
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.WarmupConnectionFailed(_logger, i + 1, ex);
                // Continue warming up remaining connections; a partial warm-up is still beneficial
            }
        }

        Log.WarmupCompleted(_logger, warmedUp, _warmUpCount);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Information,
            Message = "Warming up {Count} database connections.")]
        public static partial void WarmupStarting(ILogger logger, int count);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "Failed to warm up connection {Number}.")]
        public static partial void WarmupConnectionFailed(ILogger logger, int number, Exception exception);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Information,
            Message = "Connection warm-up completed: {WarmedUp}/{Total} connections established.")]
        public static partial void WarmupCompleted(ILogger logger, int warmedUp, int total);
    }
}
