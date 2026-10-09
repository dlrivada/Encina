using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Inbox;
using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Messaging;

/// <summary>
/// Runs idempotent requests through the real Encina pipeline (<c>InboxPipelineBehavior</c>, with and without
/// EF Core's <c>TransactionPipelineBehavior</c>) over a real database and one DbContext per request, with no
/// manual SaveChanges, and proves the inbox state (retry count, cached response) survives the rollback of the
/// business transaction (#2084).
/// </summary>
public static class InboxPipelineScenario
{
    /// <summary>What the handler does when it runs.</summary>
    public enum Mode
    {
        /// <summary>Throws an exception (a failed attempt).</summary>
        Throw,

        /// <summary>Returns a business Left.</summary>
        ReturnLeft,

        /// <summary>Returns a successful response.</summary>
        ReturnRight
    }

    /// <summary>Idempotent command that runs inside the EF Core transaction behavior.</summary>
    public sealed record TransactionalCommand(Mode Mode) : IRequest<string>, IIdempotentRequest, ITransactionalCommand;

    /// <summary>Idempotent command without a transaction.</summary>
    public sealed record PlainCommand(Mode Mode) : IRequest<string>, IIdempotentRequest;

    /// <summary>Counts handler runs.</summary>
    public sealed class Counter
    {
        private int _runs;

        /// <summary>Gets the number of handler runs.</summary>
        public int Runs => Volatile.Read(ref _runs);

        internal void Increment() => Interlocked.Increment(ref _runs);
    }

    /// <summary>Handler for <see cref="TransactionalCommand"/>.</summary>
    public sealed class TransactionalHandler(Counter counter) : IRequestHandler<TransactionalCommand, string>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, string>> Handle(TransactionalCommand request, CancellationToken cancellationToken) =>
            Run(counter, request.Mode);
    }

    /// <summary>Handler for <see cref="PlainCommand"/>.</summary>
    public sealed class PlainHandler(Counter counter) : IRequestHandler<PlainCommand, string>
    {
        /// <inheritdoc />
        public Task<Either<EncinaError, string>> Handle(PlainCommand request, CancellationToken cancellationToken) =>
            Run(counter, request.Mode);
    }

    private static Task<Either<EncinaError, string>> Run(Counter counter, Mode mode)
    {
        counter.Increment();
        return mode switch
        {
            Mode.Throw => throw new InvalidOperationException("boom"),
            Mode.ReturnLeft => Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("biz.rule", "business rule"))),
            _ => Task.FromResult(Right<EncinaError, string>("ok"))
        };
    }

    /// <summary>A handler that always throws runs exactly <paramref name="maxRetries"/> times, then is rejected.</summary>
    public static async Task AssertThrowingHandlerRunsMaxRetriesTimesAsync<TContext>(
        Action<DbContextOptionsBuilder> useDatabase,
        Func<TContext> createVerifyContext,
        int maxRetries,
        bool transactional)
        where TContext : DbContext
    {
        await using var provider = BuildProvider<TContext>(useDatabase, maxRetries, out var counter);
        var key = Guid.NewGuid().ToString();

        for (var i = 0; i < maxRetries; i++)
        {
            var failed = await SendAsync(provider, key, Mode.Throw, transactional);
            failed.IsLeft.ShouldBeTrue();
        }

        counter.Runs.ShouldBe(maxRetries);

        var rejected = await SendAsync(provider, key, Mode.Throw, transactional);

        rejected.IsLeft.ShouldBeTrue();
        rejected.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.MaxRetriesExceeded);
        counter.Runs.ShouldBe(maxRetries);

        await using var verify = createVerifyContext();
        var stored = await verify.Set<InboxMessage>().AsNoTracking().SingleAsync(m => m.MessageId == key);
        stored.RetryCount.ShouldBe(maxRetries);
        stored.IsProcessed.ShouldBeFalse();
    }

    /// <summary>A handler Left (rolled back by the transaction) is still cached and not re-run.</summary>
    public static async Task AssertHandlerLeftIsCachedAfterRollbackAsync<TContext>(
        Action<DbContextOptionsBuilder> useDatabase,
        Func<TContext> createVerifyContext,
        bool transactional)
        where TContext : DbContext
    {
        await using var provider = BuildProvider<TContext>(useDatabase, 3, out var counter);
        var key = Guid.NewGuid().ToString();

        var first = await SendAsync(provider, key, Mode.ReturnLeft, transactional);
        var second = await SendAsync(provider, key, Mode.ReturnLeft, transactional);

        first.IsLeft.ShouldBeTrue();
        second.IsLeft.ShouldBeTrue();
        second.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.CachedError);
        counter.Runs.ShouldBe(1);

        await using var verify = createVerifyContext();
        var stored = await verify.Set<InboxMessage>().AsNoTracking().SingleAsync(m => m.MessageId == key);
        stored.RetryCount.ShouldBe(0);
        stored.IsProcessed.ShouldBeTrue();
        stored.Response.ShouldNotBeNull();
    }

    /// <summary>A successful response is persisted and returned on redelivery without re-running the handler.</summary>
    public static async Task AssertSuccessIsCachedAsync<TContext>(
        Action<DbContextOptionsBuilder> useDatabase,
        Func<TContext> createVerifyContext,
        bool transactional)
        where TContext : DbContext
    {
        await using var provider = BuildProvider<TContext>(useDatabase, 3, out var counter);
        var key = Guid.NewGuid().ToString();

        var first = await SendAsync(provider, key, Mode.ReturnRight, transactional);
        var second = await SendAsync(provider, key, Mode.ReturnRight, transactional);

        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();
        counter.Runs.ShouldBe(1);

        await using var verify = createVerifyContext();
        var stored = await verify.Set<InboxMessage>().AsNoTracking().SingleAsync(m => m.MessageId == key);
        stored.IsProcessed.ShouldBeTrue();
    }

    private static ServiceProvider BuildProvider<TContext>(Action<DbContextOptionsBuilder> useDatabase, int maxRetries, out Counter counter)
        where TContext : DbContext
    {
        counter = new Counter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(counter);
        services.AddDbContext<TContext>(o => useDatabase(o));
        services.AddEncina();
        services.AddTransient<IRequestHandler<TransactionalCommand, string>, TransactionalHandler>();
        services.AddTransient<IRequestHandler<PlainCommand, string>, PlainHandler>();
        services.AddEncinaEntityFrameworkCore<TContext>(config =>
        {
            config.UseInbox = true;
            config.UseTransactions = true;
            config.InboxOptions.MaxRetries = maxRetries;
        });
        return services.BuildServiceProvider();
    }

    // One scope (so one DbContext) per delivery, like a real request.
    private static async Task<Either<EncinaError, string>> SendAsync(ServiceProvider provider, string key, Mode mode, bool transactional)
    {
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var context = RequestContext.CreateForTest(idempotencyKey: key);

        return transactional
            ? await encina.Send(new TransactionalCommand(mode), context)
            : await encina.Send(new PlainCommand(mode), context);
    }
}
