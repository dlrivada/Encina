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

    private sealed class WellFormedContext(DbContextOptions<WellFormedContext> options) : DbContext(options)
    {
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
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
