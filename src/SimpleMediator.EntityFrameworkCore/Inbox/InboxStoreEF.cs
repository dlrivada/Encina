using Microsoft.EntityFrameworkCore;
using SimpleMediator.Messaging.Inbox;

namespace SimpleMediator.EntityFrameworkCore.Inbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides idempotent message processing using EF Core's
/// change tracking and transaction support.
/// </para>
/// </remarks>
public sealed class InboxStoreEF : IInboxStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public InboxStoreEF(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<IInboxMessage?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        if (message is not InboxMessage efMessage)
        {
            throw new InvalidOperationException(
                $"InboxStoreEF requires messages of type {nameof(InboxMessage)}, " +
                $"but received {message.GetType().Name}");
        }

        await _dbContext.Set<InboxMessage>().AddAsync(efMessage, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

        if (message == null)
            return;

        message.Response = response;
        message.ProcessedAtUtc = DateTime.UtcNow;
        message.ErrorMessage = null;
    }

    /// <inheritdoc/>
    public async Task MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

        if (message == null)
            return;

        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = nextRetryAt;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var messages = await _dbContext.Set<InboxMessage>()
            .Where(m => m.ExpiresAtUtc <= now)
            .OrderBy(m => m.ExpiresAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return messages;
    }

    /// <inheritdoc/>
    public async Task RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        var messages = await _dbContext.Set<InboxMessage>()
            .Where(m => messageIds.Contains(m.MessageId))
            .ToListAsync(cancellationToken);

        _dbContext.Set<InboxMessage>().RemoveRange(messages);
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
