using Encina.Messaging.Inbox;
using Encina.TestInfrastructure.Extensions;

namespace Encina.IntegrationTests.Messaging;

/// <summary>
/// Provider-neutral scenario for #3010: only a processed row whose <c>ExpiresAtUtc</c> has passed is
/// expired. An unprocessed row, a dead-lettered row (RetryCount at the maximum, never processed) and a
/// non-expired processed row are neither returned by <c>GetExpiredMessagesAsync</c> nor removed by
/// <c>RemoveExpiredMessagesAsync</c>, even when their ids are passed to it explicitly.
/// </summary>
public static class InboxExpiryScenario
{
    /// <summary>The RetryCount used for the dead-lettered row.</summary>
    public const int DeadLetterRetryCount = 3;

    /// <summary>One row to seed: <paramref name="ProcessedAtUtc"/> null means never processed.</summary>
    public sealed record Row(string MessageId, DateTime ExpiresAtUtc, DateTime? ProcessedAtUtc, int RetryCount);

    /// <summary>Runs the scenario against one provider store.</summary>
    /// <param name="store">The provider store under test.</param>
    /// <param name="seed">Persists the rows through the provider's own storage (not through the store).</param>
    /// <param name="remainingIds">Reads the ids still stored, through a fresh connection.</param>
    /// <param name="saveChanges">Flushes pending changes for unit-of-work stores (EF Core); null for immediate stores.</param>
    public static async Task AssertOnlyProcessedExpiredRowsArePurgedAsync(
        IInboxStore store,
        Func<IReadOnlyList<Row>, Task> seed,
        Func<Task<IReadOnlyCollection<string>>> remainingIds,
        Func<Task>? saveChanges = null)
    {
        var now = DateTime.UtcNow;
        var processedExpired = new Row(Guid.NewGuid().ToString(), now.AddDays(-1), now.AddDays(-2), 0);
        var unprocessedExpired = new Row(Guid.NewGuid().ToString(), now.AddDays(-1), null, 0);
        var deadLetteredExpired = new Row(Guid.NewGuid().ToString(), now.AddDays(-1), null, DeadLetterRetryCount);
        var processedNotExpired = new Row(Guid.NewGuid().ToString(), now.AddDays(7), now.AddMinutes(-1), 0);
        await seed([processedExpired, unprocessedExpired, deadLetteredExpired, processedNotExpired]);

        var expired = (await store.GetExpiredMessagesAsync(batchSize: 10)).ShouldBeRight().ToList();

        expired.Count.ShouldBe(1);
        expired[0].MessageId.ShouldBe(processedExpired.MessageId);

        // Explicitly pass every id: the remove itself must refuse the rows that are not expired and processed.
        string[] allIds =
        [
            processedExpired.MessageId,
            unprocessedExpired.MessageId,
            deadLetteredExpired.MessageId,
            processedNotExpired.MessageId
        ];
        (await store.RemoveExpiredMessagesAsync(allIds)).ShouldBeRight();
        if (saveChanges is not null)
        {
            await saveChanges();
        }

        var remaining = await remainingIds();
        remaining.Count.ShouldBe(3);
        remaining.ShouldNotContain(processedExpired.MessageId);
        remaining.ShouldContain(unprocessedExpired.MessageId);
        remaining.ShouldContain(deadLetteredExpired.MessageId);
        remaining.ShouldContain(processedNotExpired.MessageId);
    }
}
