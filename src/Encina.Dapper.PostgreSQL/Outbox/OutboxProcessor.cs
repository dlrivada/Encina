using Encina.Messaging.Outbox;
using Microsoft.Extensions.Logging;

namespace Encina.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Background service that processes pending outbox messages and publishes them through the Encina.
/// </summary>
/// <remarks>
/// Delivery, retry with exponential backoff and exhaustion handling are implemented once in
/// <see cref="OutboxProcessorBase"/>; this type only binds them to this provider's registration.
/// </remarks>
public sealed class OutboxProcessor : OutboxProcessorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scopes.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="options">Configuration options for outbox processing.</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/>, <paramref name="logger"/> or <paramref name="options"/> is null.</exception>
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        OutboxOptions options,
        TimeProvider? timeProvider = null)
        : base(serviceProvider, logger, options, timeProvider)
    {
    }
}
