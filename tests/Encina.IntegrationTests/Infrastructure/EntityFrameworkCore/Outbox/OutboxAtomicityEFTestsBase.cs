using System.Diagnostics;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Transactions;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using static Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox.OutboxAtomicityEF;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox;

/// <summary>
/// Proves the transactional outbox of Entity Framework Core (#718) on a real database: the outbox rows of a
/// request's notifications and its business change are saved in the request's single
/// <c>IDbContextTransaction</c> and commit or roll back together; a failed outbox write, a rollback-only
/// participant or a transaction that ended mid-request fails the request. Each provider's
/// <c>OutboxAtomicityTests</c> supplies its shared <c>EFCore-*</c> collection fixture and its context.
/// </summary>
/// <typeparam name="TContext">The provider's test context (outbox, inbox and business tables).</typeparam>
public abstract class OutboxAtomicityEFTestsBase<TContext> : IAsyncLifetime
    where TContext : DbContext
{
    private const string TransactionSourceName = "Encina.Messaging.Transactions";

    /// <summary>Gives every outbox message the same id, so the second one violates the key.</summary>
    private sealed class FixedIdOutboxMessageFactory(IOutboxMessageFactory inner, Guid id) : IOutboxMessageFactory
    {
        public IOutboxMessage Create(Guid _, string notificationType, string content, DateTime createdAtUtc) =>
            inner.Create(id, notificationType, content, createdAtUtc);
    }

    /// <summary>Gets the shared collection fixture of the provider.</summary>
    protected abstract IEFCoreFixture Fixture { get; }

    /// <summary>Configures the provider on <paramref name="options"/>, with a retrying execution strategy when asked.</summary>
    /// <param name="options">The options builder.</param>
    /// <param name="retryOnFailure">Whether to enable the retrying execution strategy.</param>
    protected abstract void UseDatabase(DbContextOptionsBuilder options, bool retryOnFailure);

    /// <summary>Gets why the provider cannot run, or null.</summary>
    protected virtual string? SkipReason => null;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (SkipReason is null)
        {
            await Fixture.ClearAllDataAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Other classes of the shared collection assume empty tables when they start.
        if (SkipReason is null)
        {
            await Fixture.ClearAllDataAsync();
        }
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    public async Task Commit_SavesTheBusinessRowAndOneOutboxRowPerNotification(bool transactional, int notifications)
    {
        Guard();
        await using var provider = Build(transactions: transactional);

        var result = await SendOrderAsync(provider, transactional, notifications, Mode.Commit);

        result.ShouldBeRight();
        (await CountRowsAsync()).ShouldBe((1L, (long)notifications));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandlerLeft_LeavesNoBusinessRowAndNoOutboxRow(bool transactional)
    {
        Guard();
        await using var provider = Build(transactions: transactional);

        var result = await SendOrderAsync(provider, transactional, 3, Mode.ReturnLeft);

        result.ShouldBeErrorWithCode("biz.rule");
        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandlerException_LeavesNoBusinessRowAndNoOutboxRow(bool transactional)
    {
        Guard();
        await using var provider = Build(transactions: transactional);

        // The transaction behavior turns a handler exception into a Left; without it the outbox behavior rolls
        // back and rethrows the original exception.
        if (transactional)
        {
            (await SendOrderAsync(provider, true, 2, Mode.Throw)).ShouldBeErrorWithCode("transaction.failed");
        }
        else
        {
            var thrown = await Should.ThrowAsync<InvalidOperationException>(
                async () => await SendOrderAsync(provider, false, 2, Mode.Throw));
            thrown.Message.ShouldBe("boom");
        }

        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOutboxWrite_FailsTheRequestAndRollsTheBusinessChangeBack(bool transactional)
    {
        Guard();
        var fixedId = Guid.NewGuid();
        await using var provider = Build(
            transactions: transactional,
            customize: services => services.AddScoped<IOutboxMessageFactory>(
                _ => new FixedIdOutboxMessageFactory(new OutboxMessageFactory(), fixedId)));

        var result = await SendOrderAsync(provider, transactional, 2, Mode.Commit);

        result.IsLeft.ShouldBeTrue();
        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task TransactionRequestWithOutbox_BeginsExactlyOneTransaction()
    {
        Guard();
        var owners = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TransactionSourceName || source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.Source.Name == TransactionSourceName)
                {
                    lock (owners) { owners.Add(activity); }
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        await using var provider = Build(transactions: true);
        using var root = Source.StartActivity("scenario");
        root.ShouldNotBeNull();

        var result = await SendOrderAsync(provider, true, 2, Mode.Commit);

        result.ShouldBeRight();
        Activity[] mine;
        lock (owners) { mine = owners.Where(a => a.TraceId == root.TraceId).ToArray(); }
        mine.Length.ShouldBe(1);
        mine[0].GetTagItem("encina.transaction.owner").ShouldBe(true);
        mine[0].GetTagItem("encina.transaction.outcome").ShouldBe("committed");
        (await CountRowsAsync()).ShouldBe((1L, 2L));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnitOfWorkRollbackInsideTheRequest_FailsWithRollbackOnlyAndLeavesNoRow(bool transactional)
    {
        Guard();
        await using var provider = Build(transactions: transactional);

        var result = await SendOrderAsync(provider, transactional, 2, Mode.UnitOfWorkRollback);

        result.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task InboxAndOutbox_ShareTheBusinessTransaction()
    {
        Guard();
        await using var provider = Build(transactions: true, inbox: true);

        var okKey = Guid.NewGuid().ToString();
        (await SendIdempotentAsync(provider, Mode.Commit, okKey)).ShouldBeRight();
        (await CountRowsAsync()).ShouldBe((1L, 2L));
        (await ReadInboxAsync(okKey)).ShouldNotBeNull().IsProcessed.ShouldBeTrue();

        var leftKey = Guid.NewGuid().ToString();
        (await SendIdempotentAsync(provider, Mode.ReturnLeft, leftKey)).IsLeft.ShouldBeTrue();
        var leftRow = (await ReadInboxAsync(leftKey)).ShouldNotBeNull();
        leftRow.IsProcessed.ShouldBeTrue();
        leftRow.Response.ShouldNotBeNull();

        var throwKey = Guid.NewGuid().ToString();
        (await SendIdempotentAsync(provider, Mode.Throw, throwKey)).IsLeft.ShouldBeTrue();
        var thrown = (await ReadInboxAsync(throwKey)).ShouldNotBeNull();
        thrown.RetryCount.ShouldBe(1);
        thrown.IsProcessed.ShouldBeFalse();

        // Only the successful request left business and outbox rows.
        (await CountRowsAsync()).ShouldBe((1L, 2L));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryingExecutionStrategy_RejectsTheTransactionAndWritesNothing(bool transactional)
    {
        Guard();
        await using var provider = Build(transactions: transactional, retryOnFailure: true);

        var result = await SendOrderAsync(provider, transactional, 2, Mode.Commit);

        result.ShouldBeErrorWithCode(TransactionErrors.NotSupportedCode);
        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task ApplicationTransaction_IsJoinedAndNotCommittedByEncina()
    {
        Guard();
        var state = new ApplicationTransactionState();
        await using var provider = Build(transactions: false, customize: services => services.AddSingleton(state));

        var result = await SendOrderAsync(provider, false, 2, Mode.Commit);

        result.ShouldBeRight();
        // Encina neither committed nor rolled back the application's transaction: it was still current when the
        // application's own behavior got control back, and the application committed it.
        state.StillCurrentAfterRequest.ShouldBeTrue();
        (await CountRowsAsync()).ShouldBe((1L, 2L));
    }

    [Fact]
    public async Task ApplicationTransaction_FailedOutboxWriteRollsTheApplicationTransactionBack()
    {
        Guard();
        var fixedId = Guid.NewGuid();
        var state = new ApplicationTransactionState();
        await using var provider = Build(
            transactions: false,
            customize: services =>
            {
                services.AddSingleton(state);
                services.AddScoped<IOutboxMessageFactory>(
                    _ => new FixedIdOutboxMessageFactory(new OutboxMessageFactory(), fixedId));
            });

        var result = await SendOrderAsync(provider, false, 2, Mode.Commit);

        result.IsLeft.ShouldBeTrue();
        // The failed outbox write rolled the application's transaction back (fail closed).
        state.StillCurrentAfterRequest.ShouldBeFalse();
        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    [Fact]
    public async Task TransactionEndedDuringTheHandler_FailsWithRollbackOnlyAndNoOutboxRowAutocommits()
    {
        Guard();
        var state = new ApplicationTransactionState();
        await using var provider = Build(transactions: false, customize: services => services.AddSingleton(state));

        var result = await SendOrderAsync(provider, false, 2, Mode.RollBackAmbientTransaction);

        result.ShouldBeErrorWithCode(TransactionErrors.RollbackOnlyCode);
        (await CountRowsAsync()).ShouldBe((0L, 0L));
    }

    private void Guard() => Assert.SkipWhen(SkipReason is not null, SkipReason ?? string.Empty);

    private ServiceProvider Build(
        bool transactions,
        bool inbox = false,
        bool retryOnFailure = false,
        Action<IServiceCollection>? customize = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<IRequestHandler<PlaceOrder, int>, PlaceOrderHandler>();
        services.AddScoped<IRequestHandler<PlaceTransactionalOrder, int>, PlaceTransactionalOrderHandler>();
        services.AddScoped<IRequestHandler<PlaceIdempotentOrder, int>, PlaceIdempotentOrderHandler>();
        services.AddDbContext<TContext>(o => UseDatabase(o, retryOnFailure));
        // Registered before Encina's behaviors: the application's behavior is the outermost one.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ApplicationTransactionBehavior<,>));
        services.AddEncinaEntityFrameworkCore<TContext>(config =>
        {
            config.UseOutbox = true;
            config.UseTransactions = transactions;
            config.UseInbox = inbox;
        });
        customize?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static async Task<Either<EncinaError, int>> SendOrderAsync(ServiceProvider provider, bool transactional, int notifications, Mode mode)
    {
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var id = Guid.NewGuid();
        return transactional
            ? await encina.Send(new PlaceTransactionalOrder(id, notifications, mode))
            : await encina.Send(new PlaceOrder(id, notifications, mode));
    }

    private static async Task<Either<EncinaError, int>> SendIdempotentAsync(ServiceProvider provider, Mode mode, string idempotencyKey)
    {
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        return await encina.Send(
            new PlaceIdempotentOrder(Guid.NewGuid(), 2, mode),
            RequestContext.CreateForTest(idempotencyKey: idempotencyKey));
    }

    private async Task<(long Business, long Outbox)> CountRowsAsync()
    {
        await using var verify = Fixture.CreateDbContext<TContext>();
        return (await verify.Set<TestRepositoryEntity>().LongCountAsync(), await verify.Set<OutboxMessage>().LongCountAsync());
    }

    private async Task<InboxMessage?> ReadInboxAsync(string messageId)
    {
        await using var verify = Fixture.CreateDbContext<TContext>();
        return await verify.Set<InboxMessage>().AsNoTracking().SingleOrDefaultAsync(m => m.MessageId == messageId);
    }

    private const string SourceName = "Encina.IntegrationTests.OutboxAtomicityEF";

    private static readonly ActivitySource Source = new(SourceName);
}
