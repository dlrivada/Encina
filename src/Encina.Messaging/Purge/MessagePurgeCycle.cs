using Encina.Messaging.Diagnostics;
using Encina.Messaging.Inbox;
using LanguageExt;

namespace Encina.Messaging.Purge;

/// <summary>
/// One purge cycle of one messaging pattern: removes expired messages in batches until a batch comes back
/// shorter than the batch size.
/// </summary>
/// <remarks>
/// The first <c>Left</c> of a store stops the cycle and is returned, so a failed purge is never reported as
/// success. Messages removed by the batches before the failure are already counted in the metrics.
/// </remarks>
internal static class MessagePurgeCycle
{
    /// <summary>The pattern label of the inbox purge in logs and metrics.</summary>
    internal const string InboxPattern = "inbox";

    /// <summary>
    /// Removes the expired inbox messages in batches of <paramref name="batchSize"/> through the store's
    /// <see cref="IInboxStore.GetExpiredMessagesAsync"/>, <see cref="IInboxStore.RemoveExpiredMessagesAsync"/>
    /// and <see cref="IInboxStore.SaveChangesAsync"/>.
    /// </summary>
    /// <param name="store">The inbox store.</param>
    /// <param name="batchSize">The maximum number of messages one batch removes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Right(total removed) when the cycle finished; Left(error) with the first store error.</returns>
    internal static async Task<Either<EncinaError, int>> PurgeInboxAsync(
        IInboxStore store,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var total = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await PurgeInboxBatchAsync(store, batchSize, cancellationToken).ConfigureAwait(false);
            if (batch.IsLeft)
            {
                return (EncinaError)batch;
            }

            var removed = (int)batch;
            MessagePurgeMetrics.RecordDeleted(InboxPattern, removed);
            total += removed;

            if (removed < batchSize)
            {
                return total;
            }
        }
    }

    private static async Task<Either<EncinaError, int>> PurgeInboxBatchAsync(
        IInboxStore store,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var expired = await store.GetExpiredMessagesAsync(batchSize, cancellationToken).ConfigureAwait(false);
        if (expired.IsLeft)
        {
            return (EncinaError)expired;
        }

        var ids = expired.Match(
            Right: static messages => messages.Select(static m => m.MessageId).ToList(),
            Left: static _ => []);
        if (ids.Count == 0)
        {
            return 0;
        }

        var removed = await store.RemoveExpiredMessagesAsync(ids, cancellationToken).ConfigureAwait(false);
        if (removed.IsLeft)
        {
            return (EncinaError)removed;
        }

        // The EF Core store only stages the delete; the other stores make this a no-op.
        var saved = await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsLeft)
        {
            return (EncinaError)saved;
        }

        return ids.Count;
    }
}
