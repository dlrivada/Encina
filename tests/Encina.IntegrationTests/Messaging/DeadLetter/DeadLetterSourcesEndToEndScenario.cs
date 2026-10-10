using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Encina.IntegrationTests.Messaging.DeadLetter;

/// <summary>
/// The terminal failure of each built-in source (recoverability, outbox, inbox, scheduling, sagas) driven through
/// the services a provider registration builds, against a real database: the source's own store and the dead
/// letter store are the provider's, and exactly one dead letter is persisted per source message, also when the
/// terminal event repeats (#1991).
/// </summary>
/// <remarks>
/// Each step saves its store (<c>SaveChangesAsync</c>), so unit-of-work stores (EF Core) persist like the
/// immediate ones. The clock starts one minute in the past: a store that compares retry times with its
/// <see cref="TimeProvider"/> sees the advanced clock, one that compares with the database clock sees retries
/// already due.
/// </remarks>
internal static class DeadLetterSourcesEndToEndScenario
{
    /// <summary>A request the sources dispatch.</summary>
    public sealed record SourceCommand(int Value) : IRequest<int>;

    /// <summary>A notification the outbox publishes.</summary>
    public sealed record SourceNotification(int Value) : INotification;

    /// <summary>Saga data.</summary>
    public sealed record SourceSagaData(int Value);

    /// <summary>A message whose saga is not found.</summary>
    public sealed record OrphanMessage(int Value);

    /// <summary>The saga-not-found handler of the scenario: it moves every orphan to the dead letter queue.</summary>
    public sealed class MoveOrphanToDeadLetter : IHandleSagaNotFound<OrphanMessage>
    {
        public async Task HandleAsync(OrphanMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
            => (await context.MoveToDeadLetterAsync("no saga for this message", cancellationToken)).ShouldBeRight();
    }

    /// <summary>The scenario name of the saga-not-found path (dead letters under source pattern Saga).</summary>
    public const string SagaNotFound = "SagaNotFound";

    /// <summary>The source names a test theory runs over.</summary>
    public static TheoryData<string> Sources =>
    [
        DeadLetterSourcePatterns.Recoverability,
        DeadLetterSourcePatterns.Outbox,
        DeadLetterSourcePatterns.Inbox,
        DeadLetterSourcePatterns.Scheduling,
        DeadLetterSourcePatterns.Saga,
        SagaNotFound
    ];

    private static readonly EncinaError HandlerError = EncinaErrors.Create("e2e.handler_failed", "failure");

    /// <summary>Creates the services every family shares: logging, the fake clock and the saga-not-found handler.</summary>
    public static (ServiceCollection Services, FakeTimeProvider Clock) NewServices()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddMinutes(-1));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(clock);
        services.AddScoped<IHandleSagaNotFound<OrphanMessage>, MoveOrphanToDeadLetter>();
        return (services, clock);
    }

    /// <summary>Turns on the four stored patterns and the dead letter queue, with two retries and no backoff.</summary>
    public static void Configure(MessagingConfiguration config)
    {
        config.UseOutbox = true;
        config.UseInbox = true;
        config.UseSagas = true;
        config.UseScheduling = true;
        config.UseDeadLetterQueue = true;
        Tune(config.OutboxOptions, config.InboxOptions, config.SchedulingOptions);
    }

    /// <summary>Two retries and a one-millisecond backoff for the three retrying sources.</summary>
    public static void Tune(OutboxOptions outbox, InboxOptions inbox, SchedulingOptions scheduling)
    {
        outbox.MaxRetries = 2;
        outbox.BaseRetryDelay = TimeSpan.FromMilliseconds(1);
        outbox.MaxRetryDelay = TimeSpan.FromMilliseconds(1);
        outbox.RetryJitterRatio = 0;
        outbox.EnableProcessor = false;
        inbox.MaxRetries = 2;
        scheduling.MaxRetries = 2;
        scheduling.BaseRetryDelay = TimeSpan.FromMilliseconds(1);
        scheduling.EnableProcessor = false;
    }

    /// <summary>
    /// Runs the scenario of <paramref name="source"/>, on an empty dead letter queue (a fixture's data clean-up
    /// may not cover the dead letter table, and other tests of the collection write to it).
    /// </summary>
    public static async Task RunAsync(string source, IServiceProvider provider, FakeTimeProvider clock)
    {
        await using (var scope = provider.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IDeadLetterStore>();
            (await store.DeleteManyAsync(DeadLetterFilter.All)).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();
        }

        await RunSourceAsync(source, provider, clock);
    }

    private static Task RunSourceAsync(string source, IServiceProvider provider, FakeTimeProvider clock) => source switch
    {
        DeadLetterSourcePatterns.Recoverability => RecoverabilityAsync(provider, clock),
        DeadLetterSourcePatterns.Outbox => OutboxAsync(provider, clock),
        DeadLetterSourcePatterns.Inbox => InboxAsync(provider),
        DeadLetterSourcePatterns.Scheduling => SchedulingAsync(provider, clock),
        DeadLetterSourcePatterns.Saga => SagaAsync(provider),
        SagaNotFound => SagaNotFoundAsync(provider),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown source")
    };

    private static async Task RecoverabilityAsync(IServiceProvider provider, FakeTimeProvider clock)
    {
        var behavior = new RecoverabilityPipelineBehavior<SourceCommand, int>(
            new RecoverabilityOptions { ImmediateRetries = 1, ImmediateRetryDelay = TimeSpan.Zero, UseJitter = false, EnableDelayedRetries = false },
            NullLogger<RecoverabilityPipelineBehavior<SourceCommand, int>>.Instance,
            delayedRetryScheduler: null,
            timeProvider: clock,
            deadLetterCapture: provider.GetRequiredService<DeadLetterSourceCapture>());

        var result = await behavior.Handle(new SourceCommand(1), RequestContext.Create(), async () =>
        {
            (await CountAsync(provider, DeadLetterSourcePatterns.Recoverability)).ShouldBe(0);
            return Either<EncinaError, int>.Left(HandlerError);
        }, CancellationToken.None);

        result.ShouldBeLeft();
        (await CountAsync(provider, DeadLetterSourcePatterns.Recoverability)).ShouldBe(1);
    }

    private static async Task OutboxAsync(IServiceProvider provider, FakeTimeProvider clock)
    {
        // The fixture may hold outbox rows of other tests, so this message is found by a unique value.
        var marker = Random.Shared.Next(100_000_000, int.MaxValue);
        var messageId = string.Empty;
        await InScopeAsync<OutboxOrchestrator, IOutboxStore>(provider, async (orchestrator, store) =>
        {
            (await orchestrator.AddAsync(new SourceNotification(marker))).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();
            var pending = (await store.GetPendingMessagesAsync(10_000, 100)).ShouldBeRight();
            messageId = pending.Single(m => m.Content.Contains(marker.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal))
                .Id.ToString("D");
        });

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await InScopeAsync<OutboxOrchestrator, IOutboxStore>(provider, async (orchestrator, store) =>
            {
                (await orchestrator.ProcessPendingMessagesAsync((_, _, _) => ValueTask.FromResult(Either<EncinaError, Unit>.Left(HandlerError))))
                    .ShouldBeRight();
                (await store.SaveChangesAsync()).ShouldBeRight();
            });
            (await CountAsync(provider, DeadLetterSourcePatterns.Outbox, messageId)).ShouldBe(cycle == 1 ? 0 : 1);
            clock.Advance(TimeSpan.FromHours(1));
        }

        // Requeued and exhausted again: still one dead letter for the outbox message.
        await InScopeAsync<OutboxOrchestrator, IOutboxStore>(provider, async (orchestrator, _) =>
            (await orchestrator.RequeueExhaustedAsync()).ShouldBeRight().ShouldBeGreaterThanOrEqualTo(1));
        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await InScopeAsync<OutboxOrchestrator, IOutboxStore>(provider, async (orchestrator, store) =>
            {
                await orchestrator.ProcessPendingMessagesAsync((_, _, _) => ValueTask.FromResult(Either<EncinaError, Unit>.Left(HandlerError)));
                (await store.SaveChangesAsync()).ShouldBeRight();
            });
            clock.Advance(TimeSpan.FromHours(1));
        }

        (await CountAsync(provider, DeadLetterSourcePatterns.Outbox, messageId)).ShouldBe(1);
    }

    private static async Task InboxAsync(IServiceProvider provider)
    {
        var messageId = Guid.NewGuid().ToString("D");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await InScopeAsync<InboxOrchestrator, IInboxStore>(provider, async (orchestrator, store) =>
            {
                var result = await orchestrator.ProcessAsync<int>(
                    new SourceCommand(attempt),
                    messageId,
                    typeof(SourceCommand).AssemblyQualifiedName!,
                    "e2e-corr",
                    metadata: null,
                    () => throw new InvalidOperationException("handler crashed"));
                result.ShouldBeLeft();
                (await store.SaveChangesAsync()).ShouldBeRight();
            });

            // The second attempt uses up MaxRetries; the third is a rejected redelivery.
            (await CountAsync(provider, DeadLetterSourcePatterns.Inbox, messageId)).ShouldBe(attempt == 1 ? 0 : 1);
        }
    }

    private static async Task SchedulingAsync(IServiceProvider provider, FakeTimeProvider clock)
    {
        // Counted by the message id: the fixture may hold scheduled rows of other tests.
        var messageId = string.Empty;
        await InScopeAsync<SchedulerOrchestrator, IScheduledMessageStore>(provider, async (orchestrator, store) =>
        {
            messageId = (await orchestrator.ScheduleAsync(new SourceCommand(1), TimeSpan.FromSeconds(1))).ShouldBeRight().ToString("D");
            (await store.SaveChangesAsync()).ShouldBeRight();
        });

        for (var cycle = 1; cycle <= 3; cycle++)
        {
            clock.Advance(TimeSpan.FromHours(1));
            await InScopeAsync<SchedulerOrchestrator, IScheduledMessageStore>(provider, async (orchestrator, _) =>
                (await orchestrator.ProcessDueMessagesAsync((_, _, _, _) => ValueTask.FromResult(Either<EncinaError, Unit>.Left(HandlerError))))
                    .ShouldBeRight());

            // The second failure is the one the retry policy dead-letters; a third cycle finds nothing due.
            (await CountAsync(provider, DeadLetterSourcePatterns.Scheduling, messageId)).ShouldBe(cycle == 1 ? 0 : 1);
        }
    }

    private static async Task SagaAsync(IServiceProvider provider)
    {
        var sagaId = Guid.Empty;
        await InScopeAsync<SagaOrchestrator, ISagaStore>(provider, async (orchestrator, store) =>
        {
            sagaId = (await orchestrator.StartAsync("E2ESaga", new SourceSagaData(1))).ShouldBeRight();
            (await store.SaveChangesAsync()).ShouldBeRight();
        });
        (await CountAsync(provider, DeadLetterSourcePatterns.Saga)).ShouldBe(0);

        for (var call = 0; call < 2; call++)
        {
            await InScopeAsync<SagaOrchestrator, ISagaStore>(provider, async (orchestrator, store) =>
            {
                (await orchestrator.FailAsync(sagaId, "saga.compensation_failed")).ShouldBeRight();
                (await store.SaveChangesAsync()).ShouldBeRight();
            });
        }

        (await CountAsync(provider, DeadLetterSourcePatterns.Saga, sagaId.ToString("D"))).ShouldBe(1);
    }

    // The caller supplies the message identity (the transport id): a redelivery keeps one dead letter, a different
    // message gets its own.
    private static async Task SagaNotFoundAsync(IServiceProvider provider)
    {
        var first = $"transport-{Guid.NewGuid():N}";
        var second = $"transport-{Guid.NewGuid():N}";

        foreach (var sourceMessageId in new[] { first, first, second })
        {
            await using var scope = provider.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<ISagaNotFoundDispatcher>();
            (await dispatcher.DispatchAsync(
                new OrphanMessage(1),
                new SagaNotFoundContext(Guid.NewGuid(), "E2ESaga", typeof(OrphanMessage), sourceMessageId))).ShouldBeRight();
        }

        (await CountAsync(provider, DeadLetterSourcePatterns.Saga, first)).ShouldBe(1);
        (await CountAsync(provider, DeadLetterSourcePatterns.Saga, second)).ShouldBe(1);
    }

    private static async Task InScopeAsync<TOrchestrator, TStore>(
        IServiceProvider provider,
        Func<TOrchestrator, TStore, Task> step)
        where TOrchestrator : notnull
        where TStore : notnull
    {
        await using var scope = provider.CreateAsyncScope();
        await step(
            scope.ServiceProvider.GetRequiredService<TOrchestrator>(),
            scope.ServiceProvider.GetRequiredService<TStore>());
    }

    private static async Task<int> CountAsync(IServiceProvider provider, string sourcePattern, string? sourceMessageId = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IDeadLetterStore>();
        return (await store.GetCountAsync(new DeadLetterFilter { SourcePattern = sourcePattern, SourceMessageId = sourceMessageId }))
            .ShouldBeRight();
    }
}
