using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.EntityFrameworkCore.Inbox;

/// <summary>
/// The requirements of the inbox's isolated writes (ADR-048): the context type must be creatable from its own
/// options (checked at registration) and must not share a DbConnection instance (refused at the first write).
/// </summary>
public sealed class InboxStoreEFIsolationTests
{
    [Fact]
    public void ValidateContextType_ContextWithOptionsConstructor_Passes()
    {
        Should.NotThrow(() => InboxStoreEF.ValidateContextType(typeof(WellFormedContext)));
    }

    [Fact]
    public void ValidateContextType_ContextWithoutGenericOptionsConstructor_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => InboxStoreEF.ValidateContextType(typeof(NonGenericOptionsContext)));

        ex.Message.ShouldContain(nameof(NonGenericOptionsContext));
    }

    [Fact]
    public void ValidateContextType_NullType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => InboxStoreEF.ValidateContextType(null!));
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_InboxWithUnusableContextType_FailsAtRegistration()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddEncinaEntityFrameworkCore<NonGenericOptionsContext>(c => c.UseInbox = true));
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_TransactionsAndInbox_RegistersTransactionBehaviorBeforeInboxBehavior()
    {
        var services = new ServiceCollection();
        services.AddEncinaEntityFrameworkCore<WellFormedContext>(c =>
        {
            c.UseTransactions = true;
            c.UseInbox = true;
        });

        var behaviors = services
            .Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(sd => sd.ImplementationType)
            .ToList();

        var transaction = behaviors.IndexOf(typeof(TransactionPipelineBehavior<,>));
        var inbox = behaviors.IndexOf(typeof(global::Encina.Messaging.Inbox.InboxPipelineBehavior<,>));
        transaction.ShouldBeGreaterThanOrEqualTo(0);
        inbox.ShouldBeGreaterThan(transaction);
    }

    [Fact]
    public void AddEncinaEntityFrameworkCore_NoInbox_DoesNotRequireTheConstructor()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() =>
            services.AddEncinaEntityFrameworkCore<NonGenericOptionsContext>(c => c.UseOutbox = true));
    }

    [Fact]
    public async Task AddAsync_ContextConfiguredWithSharedConnectionInstance_IsRefused()
    {
        await using var shared = new SqliteConnection("Data Source=:memory:");
        await shared.OpenAsync();
        var options = new DbContextOptionsBuilder<WellFormedContext>().UseSqlite(shared).Options;
        await using var context = new WellFormedContext(options);
        var store = new InboxStoreEF(context);

        var result = await store.AddAsync(new InboxMessage
        {
            MessageId = "shared-connection",
            RequestType = "Req",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });

        result.IsLeft.ShouldBeTrue();
        result.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe("inbox.add_failed");
        result.LeftToArray()[0].Message.ShouldContain("shared DbConnection");
    }

    [Fact]
    public async Task MarkAsFailedAsync_ContextConfiguredWithSharedConnectionInstance_IsRefused()
    {
        await using var shared = new SqliteConnection("Data Source=:memory:");
        await shared.OpenAsync();
        var options = new DbContextOptionsBuilder<WellFormedContext>().UseSqlite(shared).Options;
        await using var context = new WellFormedContext(options);
        var store = new InboxStoreEF(context);

        var result = await store.MarkAsFailedAsync("any", "error", null);

        result.IsLeft.ShouldBeTrue();
        result.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe("inbox.mark_failed_failed");
    }

    [Fact]
    public async Task IndependentWrites_OnTheInMemoryProvider_PersistOnAnIsolatedContext()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<WellFormedContext>().UseInMemoryDatabase(databaseName).Options;
        await using var context = new WellFormedContext(options);
        var store = new InboxStoreEF(context);

        (await store.AddAsync(new InboxMessage
        {
            MessageId = "iso-1",
            RequestType = "Req",
            ReceivedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        })).IsRight.ShouldBeTrue();
        (await store.MarkAsFailedAsync("iso-1", "boom", null)).IsRight.ShouldBeTrue();
        (await store.CacheHandlerErrorAsync("iso-1", "cached-left")).IsRight.ShouldBeTrue();

        // Nothing was tracked or saved by the injected context: the isolated one wrote everything.
        context.ChangeTracker.Entries().ShouldBeEmpty();
        await using var verify = new WellFormedContext(options);
        var stored = await verify.Set<InboxMessage>().AsNoTracking().SingleAsync(m => m.MessageId == "iso-1");
        stored.RetryCount.ShouldBe(1);
        stored.Response.ShouldBe("cached-left");
        stored.IsProcessed.ShouldBeTrue();
    }

    [Fact]
    public async Task CacheHandlerErrorAsync_NullArguments_Throw()
    {
        var options = new DbContextOptionsBuilder<WellFormedContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new WellFormedContext(options);
        var store = new InboxStoreEF(context);

        await Should.ThrowAsync<ArgumentNullException>(() => store.CacheHandlerErrorAsync(null!, "r"));
        await Should.ThrowAsync<ArgumentNullException>(() => store.CacheHandlerErrorAsync("id", null!));
    }

    private sealed class WellFormedContext(DbContextOptions<WellFormedContext> options) : DbContext(options)
    {
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<InboxMessage>().HasKey(m => m.MessageId);
    }

    // No constructor taking the context's options: the isolated context cannot be created from them.
    private sealed class NonGenericOptionsContext : DbContext
    {
        public NonGenericOptionsContext(string connectionName)
        {
            ConnectionName = connectionName;
        }

        public string ConnectionName { get; }
    }
}
