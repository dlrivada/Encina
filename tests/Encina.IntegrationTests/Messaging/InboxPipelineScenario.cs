using Encina.EntityFrameworkCore;
using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Messaging;

/// <summary>
/// Runs idempotent requests through the real Encina pipeline (the provider's transaction behavior, then
/// <c>InboxPipelineBehavior</c>) over a real database and one scope per delivery, with no manual SaveChanges,
/// and proves the inbox boundary of ADR-048 on every provider family (#2084):
/// the failure records (retry count, cached handler Left) survive the rollback of the business transaction,
/// the processed mark commits atomically with the business effect, and a failed business commit leaves the
/// message unprocessed so the redelivery runs the handler again.
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

    /// <summary>The persisted state of an inbox row.</summary>
    /// <param name="RetryCount">The stored retry count.</param>
    /// <param name="IsProcessed">Whether the row is marked processed.</param>
    /// <param name="Response">The cached response, if any.</param>
    public sealed record InboxRow(int RetryCount, bool IsProcessed, string? Response);

    /// <summary>How a scenario wires the provider under test.</summary>
    /// <param name="MaxRetries">The configured <c>InboxOptions.MaxRetries</c>.</param>
    /// <param name="Transactions">Whether the provider's transaction behavior wraps the request.</param>
    /// <param name="Sabotage">The switch that breaks the business commit after the handler succeeded.</param>
    public sealed record Setup(int MaxRetries, bool Transactions, CommitSabotage Sabotage);

    /// <summary>A provider family under test.</summary>
    public sealed class Harness
    {
        /// <summary>Registers database, Encina, the inbox and (when requested) the transaction behavior followed by the sabotage behavior.</summary>
        public required Action<IServiceCollection, Setup> Register { get; init; }

        /// <summary>Reads the inbox row of a message on a fresh connection, or null when there is none.</summary>
        public required Func<string, Task<InboxRow?>> ReadRow { get; init; }

        /// <summary>Gets a value indicating whether the provider has a business transaction in the pipeline.</summary>
        public bool HasBusinessTransaction { get; init; } = true;

        /// <summary>Breaks the business transaction of the request scope so that its commit fails.</summary>
        public Func<IServiceProvider, Task> BreakTransaction { get; init; } = _ => Task.CompletedTask;
    }

    /// <summary>Switch armed by a scenario to break the business commit after a successful handler.</summary>
    public sealed class CommitSabotage
    {
        /// <summary>Gets or sets a value indicating whether the next successful request must lose its commit.</summary>
        public bool Armed { get; set; }

        /// <summary>Gets or sets the action that breaks the request scope's transaction.</summary>
        public Func<IServiceProvider, Task> Break { get; set; } = _ => Task.CompletedTask;
    }

    /// <summary>
    /// Pipeline behavior registered between the transaction behavior and the inbox behavior: after the inbox
    /// has marked the message processed inside the business transaction it breaks the transaction, so the
    /// commit that follows fails.
    /// </summary>
    public sealed class CommitSabotageBehavior<TRequest, TResponse>(CommitSabotage sabotage, IServiceProvider scope)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <inheritdoc />
        public async ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request,
            IRequestContext context,
            RequestHandlerCallback<TResponse> nextStep,
            CancellationToken cancellationToken)
        {
            var result = await nextStep();

            if (sabotage.Armed && result.IsRight)
            {
                await sabotage.Break(scope);
            }

            return result;
        }
    }

    /// <summary>Idempotent command that runs inside EF Core's transaction behavior.</summary>
    public sealed record TransactionalCommand(Mode Mode) : IRequest<string>, IIdempotentRequest, ITransactionalCommand;

    /// <summary>Idempotent command without a transaction marker.</summary>
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

    /// <summary>A handler that always throws runs exactly <paramref name="maxRetries"/> times, then is rejected; the count is persisted.</summary>
    public static async Task AssertThrowingHandlerRunsMaxRetriesTimesAsync(Harness harness, int maxRetries, bool transactional)
    {
        await using var provider = BuildProvider(harness, maxRetries, transactional, out var counter, out _);
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

        var row = await harness.ReadRow(key);
        row.ShouldNotBeNull();
        row.RetryCount.ShouldBe(maxRetries);
        row.IsProcessed.ShouldBeFalse();
    }

    /// <summary>A handler Left (rolled back by the transaction) is still cached and not re-run.</summary>
    public static async Task AssertHandlerLeftIsCachedAfterRollbackAsync(Harness harness, bool transactional)
    {
        await using var provider = BuildProvider(harness, 3, transactional, out var counter, out _);
        var key = Guid.NewGuid().ToString();

        var first = await SendAsync(provider, key, Mode.ReturnLeft, transactional);
        var second = await SendAsync(provider, key, Mode.ReturnLeft, transactional);

        first.IsLeft.ShouldBeTrue();
        second.IsLeft.ShouldBeTrue();
        second.LeftToArray()[0].GetCode().IfNone(string.Empty).ShouldBe(InboxErrorCodes.CachedError);
        counter.Runs.ShouldBe(1);

        var row = await harness.ReadRow(key);
        row.ShouldNotBeNull();
        row.RetryCount.ShouldBe(0);
        row.IsProcessed.ShouldBeTrue();
        row.Response.ShouldNotBeNull();
    }

    /// <summary>A successful response is committed with the business transaction and returned on redelivery without re-running the handler.</summary>
    public static async Task AssertSuccessIsCachedAsync(Harness harness, bool transactional)
    {
        await using var provider = BuildProvider(harness, 3, transactional, out var counter, out _);
        var key = Guid.NewGuid().ToString();

        var first = await SendAsync(provider, key, Mode.ReturnRight, transactional);
        var second = await SendAsync(provider, key, Mode.ReturnRight, transactional);

        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();
        counter.Runs.ShouldBe(1);

        var row = await harness.ReadRow(key);
        row.ShouldNotBeNull();
        row.IsProcessed.ShouldBeTrue();
    }

    /// <summary>
    /// The business commit fails after the handler succeeded: the processed mark rolls back with it, so the message
    /// stays unprocessed and the redelivery runs the handler again (exactly-once, not lost).
    /// </summary>
    public static async Task AssertFailedBusinessCommitLeavesMessageUnprocessedAsync(Harness harness)
    {
        await using var provider = BuildProvider(harness, 3, transactional: true, out var counter, out var sabotage);
        var key = Guid.NewGuid().ToString();

        sabotage.Armed = true;
        try
        {
            var lost = await SendAsync(provider, key, Mode.ReturnRight, transactional: true);
            lost.IsLeft.ShouldBeTrue();
        }
        catch (Exception)
        {
            // A provider may surface the broken transaction as an exception instead of a Left: either way
            // the request did not succeed, which is what matters here.
        }

        counter.Runs.ShouldBe(1);

        var afterLostCommit = await harness.ReadRow(key);
        afterLostCommit.ShouldNotBeNull();
        afterLostCommit.IsProcessed.ShouldBeFalse();

        sabotage.Armed = false;
        var redelivered = await SendAsync(provider, key, Mode.ReturnRight, transactional: true);

        redelivered.IsRight.ShouldBeTrue();
        counter.Runs.ShouldBe(2);

        var final = await harness.ReadRow(key);
        final.ShouldNotBeNull();
        final.IsProcessed.ShouldBeTrue();
    }

    private static ServiceProvider BuildProvider(
        Harness harness, int maxRetries, bool transactional, out Counter counter, out CommitSabotage sabotage)
    {
        counter = new Counter();
        sabotage = new CommitSabotage { Break = harness.BreakTransaction };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(counter);
        services.AddSingleton(sabotage);
        services.AddEncina();
        services.AddTransient<IRequestHandler<TransactionalCommand, string>, TransactionalHandler>();
        services.AddTransient<IRequestHandler<PlainCommand, string>, PlainHandler>();

        harness.Register(services, new Setup(maxRetries, transactional && harness.HasBusinessTransaction, sabotage));
        return services.BuildServiceProvider();
    }

    // One scope (so one connection / DbContext) per delivery, like a real request.
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
