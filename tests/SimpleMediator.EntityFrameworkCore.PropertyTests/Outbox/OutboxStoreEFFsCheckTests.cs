using System.Diagnostics.CodeAnalysis;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using SimpleMediator.EntityFrameworkCore.Outbox;
using SimpleMediator.Messaging.Outbox;

namespace SimpleMediator.EntityFrameworkCore.PropertyTests.Outbox;

/// <summary>
/// FsCheck property-based tests for <see cref="OutboxStoreEF"/>.
/// Uses FsCheck to generate thousands of random test cases automatically.
/// Verifies invariants hold for ALL possible inputs.
/// </summary>
[Trait("Category", "Property")]
[Trait("Category", "FsCheck")]
public sealed class OutboxStoreEFFsCheckTests
{
    /// <summary>
    /// Property: Adding a pending message always makes it retrievable in GetPendingMessages.
    /// Invariant: AddAsync(msg) AND msg.ProcessedAtUtc == null => GetPending contains msg
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property AddPendingMessage_AlwaysRetrievable()
    {
        return Prop.ForAll(
            Generators.PendingOutboxMessageArbitrary(),
            message =>
            {
                // Use a dedicated in-memory database per test
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxAdd_{message.Id}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Act: Add message
                store.AddAsync(message).Wait();
                store.SaveChangesAsync().Wait();

                // Assert: Must be in pending
                var pending = store.GetPendingMessagesAsync(100, 10).Result;
                var found = pending.Any(m => m.Id == message.Id);

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return found.ToProperty();
            });
    }

    /// <summary>
    /// Property: Marking a message as processed removes it from pending.
    /// Invariant: MarkAsProcessed(id) => GetPending does NOT contain id
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property MarkAsProcessed_RemovesFromPending()
    {
        return Prop.ForAll(
            Generators.PendingOutboxMessageArbitrary(),
            message =>
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxProcessed_{message.Id}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange: Add message
                store.AddAsync(message).Wait();
                store.SaveChangesAsync().Wait();

                // Act: Mark as processed
                store.MarkAsProcessedAsync(message.Id).Wait();
                store.SaveChangesAsync().Wait();

                // Assert: NOT in pending
                var pending = store.GetPendingMessagesAsync(100, 10).Result;
                var notFound = !pending.Any(m => m.Id == message.Id);

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return notFound.ToProperty();
            });
    }

    /// <summary>
    /// Property: GetPendingMessages never returns more than batchSize messages.
    /// Invariant: GetPending(batchSize, _).Count() <= batchSize
    /// </summary>
    [Property(MaxTest = 50, QuietOnSuccess = true)]
    public Property GetPending_RespectsBatchSize()
    {
        return Prop.ForAll(
            Gen.Choose(1, 20).ToArbitrary(), // batchSize
            Gen.NonEmptyListOf(Generators.PendingOutboxMessageArbitrary().Generator).ToArbitrary(),
            (batchSize, messages) =>
            {
                var dbId = Guid.NewGuid();
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxBatch_{dbId}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange: Add messages
                foreach (var msg in messages)
                {
                    store.AddAsync(msg).Wait();
                }
                store.SaveChangesAsync().Wait();

                // Act: Get pending with batch size
                var pending = store.GetPendingMessagesAsync(batchSize, 10).Result.ToList();

                // Assert: Count <= batchSize
                var respectsLimit = pending.Count <= batchSize;

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return respectsLimit.ToProperty();
            });
    }

    /// <summary>
    /// Property: Pending messages are ordered by CreatedAtUtc ascending.
    /// Invariant: GetPending()[i].CreatedAtUtc <= GetPending()[i+1].CreatedAtUtc
    /// </summary>
    [Property(MaxTest = 50, QuietOnSuccess = true)]
    public Property GetPending_OrderedByCreatedAtUtc()
    {
        return Prop.ForAll(
            Gen.NonEmptyListOf(Generators.PendingOutboxMessageArbitrary().Generator).ToArbitrary(),
            messages =>
            {
                var dbId = Guid.NewGuid();
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxOrder_{dbId}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange: Add messages in random order
                foreach (var msg in messages.OrderBy(_ => Guid.NewGuid()))
                {
                    store.AddAsync(msg).Wait();
                }
                store.SaveChangesAsync().Wait();

                // Act
                var pending = store.GetPendingMessagesAsync(100, 10).Result.ToList();

                // Assert: Ordered
                bool isOrdered = true;
                for (int i = 1; i < pending.Count; i++)
                {
                    if (pending[i].CreatedAtUtc < pending[i - 1].CreatedAtUtc)
                    {
                        isOrdered = false;
                        break;
                    }
                }

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return isOrdered.ToProperty();
            });
    }

    /// <summary>
    /// Property: Messages with RetryCount >= maxRetries are excluded from pending.
    /// Invariant: msg.RetryCount >= maxRetries => GetPending does NOT contain msg
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property GetPending_ExcludesExhaustedRetries()
    {
        return Prop.ForAll(
            Generators.OutboxMessageArbitrary(),
            Gen.Choose(0, 5).ToArbitrary(), // maxRetries
            (message, maxRetries) =>
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxRetries_{message.Id}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange: Set message as not processed
                message.ProcessedAtUtc = null;
                store.AddAsync(message).Wait();
                store.SaveChangesAsync().Wait();

                // Act
                var pending = store.GetPendingMessagesAsync(100, maxRetries).Result;

                // Assert: If exhausted, not in pending
                bool correctlyExcluded = true;
                if (message.RetryCount >= maxRetries)
                {
                    correctlyExcluded = !pending.Any(m => m.Id == message.Id);
                }

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return correctlyExcluded.ToProperty();
            });
    }

    /// <summary>
    /// Property: Messages with future NextRetryAtUtc are excluded from pending.
    /// Invariant: msg.NextRetryAtUtc > NOW => GetPending does NOT contain msg
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property GetPending_ExcludesFutureRetries()
    {
        return Prop.ForAll(
            Generators.OutboxMessageArbitrary(),
            message =>
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxFuture_{message.Id}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange: Not processed
                message.ProcessedAtUtc = null;
                message.RetryCount = 0; // Within limits
                store.AddAsync(message).Wait();
                store.SaveChangesAsync().Wait();

                // Act
                var pending = store.GetPendingMessagesAsync(100, 10).Result;

                // Assert: If NextRetryAtUtc is future, not in pending
                bool correctlyExcluded = true;
                if (message.NextRetryAtUtc.HasValue && message.NextRetryAtUtc.Value > DateTime.UtcNow)
                {
                    correctlyExcluded = !pending.Any(m => m.Id == message.Id);
                }

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return correctlyExcluded.ToProperty();
            });
    }

    /// <summary>
    /// Property: MarkAsFailed always increments RetryCount by exactly 1.
    /// Invariant: initialRetryCount + 1 == finalRetryCount after MarkAsFailed
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property MarkAsFailed_IncrementsRetryCount()
    {
        return Prop.ForAll(
            Generators.OutboxMessageArbitrary(),
            Gen.NonEmptyString.ToArbitrary(), // error message
            message, errorMessage =>
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxFail_{message.Id}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange
                var initialRetryCount = message.RetryCount;
                store.AddAsync(message).Wait();
                store.SaveChangesAsync().Wait();

                // Act
                store.MarkAsFailedAsync(message.Id, errorMessage.Item, DateTime.UtcNow.AddMinutes(5)).Wait();
                store.SaveChangesAsync().Wait();

                // Assert
                var updated = dbContext.Set<OutboxMessage>().First(m => m.Id == message.Id);
                var correctIncrement = updated.RetryCount == initialRetryCount + 1;

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return correctIncrement.ToProperty();
            });
    }

    /// <summary>
    /// Property: MarkAsProcessed always clears ErrorMessage.
    /// Invariant: MarkAsProcessed => ErrorMessage == null
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property MarkAsProcessed_ClearsError()
    {
        return Prop.ForAll(
            Generators.OutboxMessageArbitrary(),
            message =>
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxClearError_{message.Id}")
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Arrange: May have error
                store.AddAsync(message).Wait();
                store.SaveChangesAsync().Wait();

                // Act
                store.MarkAsProcessedAsync(message.Id).Wait();
                store.SaveChangesAsync().Wait();

                // Assert
                var updated = dbContext.Set<OutboxMessage>().First(m => m.Id == message.Id);
                var errorCleared = updated.ErrorMessage == null;

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return errorCleared.ToProperty();
            });
    }

    /// <summary>
    /// Property: IsProcessed reflects state correctly.
    /// Invariant: (ProcessedAtUtc != null AND ErrorMessage == null) <=> IsProcessed == true
    /// </summary>
    [Property(MaxTest = 100, QuietOnSuccess = true)]
    public Property IsProcessed_ReflectsState()
    {
        return Prop.ForAll(
            Generators.OutboxMessageArbitrary(),
            message =>
            {
                // Calculate expected
                var expectedIsProcessed = message.ProcessedAtUtc.HasValue &&
                                          string.IsNullOrEmpty(message.ErrorMessage);

                // Assert
                var actualIsProcessed = message.IsProcessed;

                return (actualIsProcessed == expectedIsProcessed).ToProperty();
            });
    }

    /// <summary>
    /// Property: Concurrent adds never result in lost messages.
    /// Invariant: Add(msgs) concurrently => ALL messages retrievable
    /// </summary>
    [Property(MaxTest = 20, QuietOnSuccess = true)]
    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code")]
    public Property ConcurrentAdds_NoMessageLoss()
    {
        return Prop.ForAll(
            Gen.ListOf(10, 30, Generators.PendingOutboxMessageArbitrary().Generator).ToArbitrary(),
            messages =>
            {
                var dbId = Guid.NewGuid();
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase($"OutboxConcurrent_{dbId}")
                    .EnableSensitiveDataLogging()
                    .Options;

                using var dbContext = new TestDbContext(options);
                dbContext.Database.EnsureCreated();
                var store = new OutboxStoreEF(dbContext);

                // Act: Concurrent adds
                var tasks = messages.Select(async msg =>
                {
                    await store.AddAsync(msg);
                    await store.SaveChangesAsync();
                });
                Task.WhenAll(tasks).Wait();

                // Assert: All messages present
                var allMessages = dbContext.Set<OutboxMessage>().ToList();
                var allPresent = messages.Count == allMessages.Count;

                // Cleanup
                dbContext.Database.EnsureDeleted();

                return allPresent.ToProperty();
            });
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NotificationType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}
