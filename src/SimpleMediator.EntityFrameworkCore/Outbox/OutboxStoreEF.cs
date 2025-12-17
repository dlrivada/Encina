using Microsoft.EntityFrameworkCore;
using SimpleMediator.Messaging.Outbox;

namespace SimpleMediator.EntityFrameworkCore.Outbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOutboxStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses EF Core's change tracking and query capabilities
/// to manage outbox messages. It provides:
/// <list type="bullet">
/// <item><description>Transactional consistency with domain operations</description></item>
/// <item><description>Optimized queries with proper indexing</description></item>
/// <item><description>Automatic change tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OutboxStoreEF : IOutboxStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public OutboxStoreEF(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        // Cast to concrete type for EF Core
        if (message is not OutboxMessage efMessage)
        {
            throw new InvalidOperationException(
                $"OutboxStoreEF requires messages of type {nameof(OutboxMessage)}, " +
                $"but received {message.GetType().Name}");
        }

        await _dbContext.Set<OutboxMessage>().AddAsync(efMessage, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var messages = await _dbContext.Set<OutboxMessage>()
            .Where(m =>
                m.ProcessedAtUtc == null &&
                (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now) &&
                m.RetryCount < maxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return messages;
    }

    /// <inheritdoc/>
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return;

        message.ProcessedAtUtc = DateTime.UtcNow;
        message.ErrorMessage = null;
    }

    /// <inheritdoc/>
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
            return;

        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = nextRetryAt;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
