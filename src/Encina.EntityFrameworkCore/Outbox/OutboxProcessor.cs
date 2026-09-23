using Encina.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Outbox;

/// <summary>
/// Background service that processes pending outbox messages.
/// </summary>
/// <remarks>
/// <para>
/// This service runs periodically to publish notifications that were stored in the outbox.
/// Delivery, retry with exponential backoff and exhaustion handling are implemented once in
/// <see cref="OutboxProcessorBase"/>.
/// </para>
/// <para>
/// Each cycle works on the scoped <see cref="DbContext"/> through an <see cref="OutboxStoreEF"/>,
/// so the processor only needs the <see cref="DbContext"/> and <see cref="IEncina"/> registrations.
/// </para>
/// </remarks>
public sealed class OutboxProcessor : OutboxProcessorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scopes.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/>, <paramref name="options"/>, or <paramref name="logger"/> is null.</exception>
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        OutboxOptions options,
        ILogger<OutboxProcessor> logger,
        TimeProvider? timeProvider = null)
        : base(serviceProvider, logger, options, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override IOutboxStore ResolveOutboxStore(IServiceProvider scopedServices)
        => new OutboxStoreEF(scopedServices.GetRequiredService<DbContext>(), TimeProvider);
}
