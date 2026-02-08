using System.Data.Common;
using Encina.Database;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Resilience;

/// <summary>
/// EF Core interceptor that monitors database connection lifecycle events
/// for pool health tracking and circuit breaker integration.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor hooks into EF Core's connection open/close lifecycle to track
/// connection pool behavior. It does not perform any blocking operations and
/// delegates monitoring to the registered <see cref="IDatabaseHealthMonitor"/>.
/// </para>
/// <para>
/// <b>Connection Events Monitored</b>:
/// <list type="bullet">
/// <item><description><c>ConnectionOpened</c>: Tracks successful connection acquisitions</description></item>
/// <item><description><c>ConnectionFailed</c>: Tracks connection failures for circuit breaker state</description></item>
/// </list>
/// </para>
/// <para>
/// Register this interceptor by enabling resilience in the EF Core configuration:
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
/// {
///     options.UseSqlServer(connectionString);
///     var interceptor = sp.GetService&lt;ConnectionPoolMonitoringInterceptor&gt;();
///     if (interceptor is not null)
///     {
///         options.AddInterceptors(interceptor);
///     }
/// });
/// </code>
/// </example>
public sealed partial class ConnectionPoolMonitoringInterceptor : DbConnectionInterceptor
{
    private readonly ILogger<ConnectionPoolMonitoringInterceptor> _logger;
    private long _totalConnectionsOpened;
    private long _totalConnectionsFailed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPoolMonitoringInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ConnectionPoolMonitoringInterceptor(
        ILogger<ConnectionPoolMonitoringInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the total number of connections successfully opened through this interceptor.
    /// </summary>
    /// <remarks>
    /// This counter is thread-safe and monotonically increasing. It can be used
    /// for metrics and monitoring purposes.
    /// </remarks>
    public long TotalConnectionsOpened => Interlocked.Read(ref _totalConnectionsOpened);

    /// <summary>
    /// Gets the total number of connection failures observed through this interceptor.
    /// </summary>
    /// <remarks>
    /// This counter is thread-safe and monotonically increasing. A high failure
    /// count may indicate network issues or database unavailability.
    /// </remarks>
    public long TotalConnectionsFailed => Interlocked.Read(ref _totalConnectionsFailed);

    /// <inheritdoc />
    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        Interlocked.Increment(ref _totalConnectionsOpened);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalConnectionsOpened);
        return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    /// <inheritdoc />
    public override void ConnectionFailed(
        DbConnection connection,
        ConnectionErrorEventData eventData)
    {
        Interlocked.Increment(ref _totalConnectionsFailed);
        Log.ConnectionFailed(_logger, eventData.Exception);
        base.ConnectionFailed(connection, eventData);
    }

    /// <inheritdoc />
    public override Task ConnectionFailedAsync(
        DbConnection connection,
        ConnectionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalConnectionsFailed);
        Log.ConnectionFailed(_logger, eventData.Exception);
        return base.ConnectionFailedAsync(connection, eventData, cancellationToken);
    }

    /// <summary>
    /// High-performance logging methods using <see cref="LoggerMessage"/>.
    /// </summary>
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "Database connection failed.")]
        public static partial void ConnectionFailed(
            ILogger logger,
            Exception exception);
    }
}
