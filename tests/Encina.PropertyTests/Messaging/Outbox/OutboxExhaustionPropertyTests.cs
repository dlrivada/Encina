using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using Encina.Testing.Fakes.Stores;
using FsCheck;
using FsCheck.Xunit;
using EfOutboxMessage = Encina.EntityFrameworkCore.Outbox.OutboxMessage;
using FakesOutboxMessage = Encina.Testing.Fakes.Models.FakeOutboxMessage;

namespace Encina.PropertyTests.Messaging.Outbox;

/// <summary>
/// Property-based tests for the exhausted-message invariants of <see cref="IOutboxStore"/> (#1150), run against
/// the EF Core store (in-memory provider) and the fake store for arbitrary sets of messages and retry limits.
/// </summary>
[Trait("Category", "Property")]
public sealed class OutboxExhaustionPropertyTests
{
    [Property(MaxTest = 50)]
    public bool Property_EfStore_PendingPlusExhaustedEqualsUnprocessed(byte[] retryCounts, bool[] processedFlags, byte maxRetriesSeed)
    {
        var messages = Messages(retryCounts, processedFlags);
        var maxRetries = MaxRetries(maxRetriesSeed);
        using var context = CreateContext(messages);
        var store = new OutboxStoreEF(context);

        return CountsPartitionUnprocessed(store, messages, maxRetries);
    }

    [Property(MaxTest = 50)]
    public bool Property_EfStore_RequeueAllLeavesNothingExhausted(byte[] retryCounts, bool[] processedFlags, byte maxRetriesSeed)
    {
        var messages = Messages(retryCounts, processedFlags);
        var maxRetries = MaxRetries(maxRetriesSeed);
        using var context = CreateContext(messages);
        var store = new OutboxStoreEF(context);

        return RequeueAllRestoresPending(store, messages, maxRetries);
    }

    [Property(MaxTest = 50)]
    public bool Property_FakeStore_PendingPlusExhaustedEqualsUnprocessed(byte[] retryCounts, bool[] processedFlags, byte maxRetriesSeed)
    {
        var messages = Messages(retryCounts, processedFlags);
        var store = CreateFakeStore(messages);

        return CountsPartitionUnprocessed(store, messages, MaxRetries(maxRetriesSeed));
    }

    [Property(MaxTest = 50)]
    public bool Property_FakeStore_RequeueAllLeavesNothingExhausted(byte[] retryCounts, bool[] processedFlags, byte maxRetriesSeed)
    {
        var messages = Messages(retryCounts, processedFlags);
        var store = CreateFakeStore(messages);

        return RequeueAllRestoresPending(store, messages, MaxRetries(maxRetriesSeed));
    }

    // OutboxOptions.MaxRetries is the number of delivery attempts and is at least 1, so the stores are
    // only ever queried with limits from 1 upwards.
    private static int MaxRetries(byte seed) => (seed % 5) + 1;

    private static bool CountsPartitionUnprocessed(IOutboxStore store, IReadOnlyList<(int RetryCount, bool Processed)> messages, int maxRetries)
    {
        var pending = Count(store.GetPendingCountAsync(maxRetries));
        var exhausted = Count(store.GetExhaustedCountAsync(maxRetries));

        var unprocessed = messages.Count(m => !m.Processed);
        var expectedExhausted = messages.Count(m => !m.Processed && m.RetryCount >= maxRetries);

        return pending + exhausted == unprocessed && exhausted == expectedExhausted;
    }

    private static bool RequeueAllRestoresPending(IOutboxStore store, IReadOnlyList<(int RetryCount, bool Processed)> messages, int maxRetries)
    {
        var exhaustedBefore = Count(store.GetExhaustedCountAsync(maxRetries));

        var requeued = Count(store.RequeueExhaustedAsync(maxRetries, null));
        store.SaveChangesAsync().GetAwaiter().GetResult().IsRight.ShouldBeTrue();

        var exhaustedAfter = Count(store.GetExhaustedCountAsync(maxRetries));
        var pendingAfter = Count(store.GetPendingCountAsync(maxRetries));

        return requeued == exhaustedBefore
            && exhaustedAfter == 0
            && pendingAfter == messages.Count(m => !m.Processed);
    }

    private static int Count(Task<Either<EncinaError, int>> task)
        => task.GetAwaiter().GetResult().Match(Right: count => count, Left: error => throw new InvalidOperationException(error.Message));

    private static List<(int RetryCount, bool Processed)> Messages(byte[]? retryCounts, bool[]? processedFlags)
    {
        retryCounts ??= [];
        processedFlags ??= [];
        return retryCounts
            .Take(30)
            .Select((retry, index) => (retry % 8, index < processedFlags.Length && processedFlags[index]))
            .ToList();
    }

    private static PropertyDbContext CreateContext(IEnumerable<(int RetryCount, bool Processed)> messages)
    {
        var context = new PropertyDbContext(
            new DbContextOptionsBuilder<PropertyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var now = DateTime.UtcNow;
        foreach (var (retryCount, processed) in messages)
        {
            context.Set<EfOutboxMessage>().Add(new EfOutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "PropertyNotification",
                Content = "{}",
                CreatedAtUtc = now,
                RetryCount = retryCount,
                ProcessedAtUtc = processed ? now : null,
                ErrorMessage = retryCount > 0 && !processed ? "failed" : null
            });
        }

        context.SaveChanges();
        context.ChangeTracker.Clear();
        return context;
    }

    private static FakeOutboxStore CreateFakeStore(IEnumerable<(int RetryCount, bool Processed)> messages)
    {
        var store = new FakeOutboxStore();
        var now = DateTime.UtcNow;
        foreach (var (retryCount, processed) in messages)
        {
            store.AddAsync(new FakesOutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "PropertyNotification",
                Content = "{}",
                CreatedAtUtc = now,
                RetryCount = retryCount,
                ProcessedAtUtc = processed ? now : null,
                ErrorMessage = retryCount > 0 && !processed ? "failed" : null
            }).GetAwaiter().GetResult();
        }

        return store;
    }

    private sealed class PropertyDbContext(DbContextOptions<PropertyDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<EfOutboxMessage>(entity => entity.HasKey(e => e.Id));
    }
}
