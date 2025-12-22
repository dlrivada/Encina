using Microsoft.EntityFrameworkCore;
using Encina.Messaging.Scheduling;

namespace Encina.EntityFrameworkCore.Scheduling;

/// <summary>
/// Entity Framework Core implementation of <see cref="IScheduledMessageStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides message scheduling support using EF Core's
/// query capabilities and transaction support for reliable delayed execution.
/// </para>
/// </remarks>
public sealed class ScheduledMessageStoreEF : IScheduledMessageStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public ScheduledMessageStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not ScheduledMessage efMessage)
        {
            throw new InvalidOperationException(
                $"ScheduledMessageStoreEF requires messages of type {nameof(ScheduledMessage)}, " +
                $"but received {message.GetType().Name}");
        }

        await _dbContext.Set<ScheduledMessage>().AddAsync(efMessage, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var messages = await _dbContext.Set<ScheduledMessage>()
            .Where(m =>
                m.ProcessedAtUtc == null &&
                m.ScheduledAtUtc <= now &&
                (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now) &&
                m.RetryCount < maxRetries)
            .OrderBy(m => m.ScheduledAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return messages;
    }

    /// <inheritdoc/>
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<ScheduledMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return;

        message.ProcessedAtUtc = DateTime.UtcNow;
        message.LastExecutedAtUtc = DateTime.UtcNow;
        message.ErrorMessage = null;
    }

    /// <inheritdoc/>
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        var message = await _dbContext.Set<ScheduledMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return;

        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = nextRetryAt;
    }

    /// <inheritdoc/>
    public async Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAt,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<ScheduledMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return;

        message.ScheduledAtUtc = nextScheduledAt;
        message.ProcessedAtUtc = null;
        message.ErrorMessage = null;
        message.RetryCount = 0;
        message.NextRetryAtUtc = null;
    }

    /// <inheritdoc/>
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<ScheduledMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            _dbContext.Set<ScheduledMessage>().Remove(message);
        }
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
