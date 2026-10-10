using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.Sagas;

/// <summary>
/// Provider-neutral scenarios for the persisted outcome of a low-ceremony saga whose step failed
/// (#2085). Every saga store integration test class runs them against its own real database, so
/// the same two outcomes are proven on all 10 providers: Compensated when every compensation
/// succeeded, Failed (with the compensation error code) when one failed.
/// </summary>
internal static class SagaRunnerOutcomeScenarios
{
    private const string StepErrorCode = "STEP_FAILED";

    /// <summary>Saga data of the scenarios.</summary>
    public sealed class OutcomeData
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// Runs a two-step saga whose second step fails and whose compensation succeeds, then asserts
    /// the persisted saga is Compensated, completed, and no longer reported as stuck.
    /// </summary>
    /// <param name="store">The real saga store under test.</param>
    /// <param name="factory">The provider's saga state factory.</param>
    /// <param name="flush">
    /// Persists pending changes after each write, for stores that rely on an external unit of work
    /// (EF Core); <see langword="null"/> for stores that write immediately.
    /// </param>
    public static async Task AssertStepFailureEndsCompensatedAsync(
        ISagaStore store,
        ISagaStateFactory factory,
        Func<Task>? flush = null)
    {
        var recording = new RecordingSagaStore(store, flush);
        var compensated = false;
        var (runner, definition) = Create(recording, factory, (_, _, _) => { compensated = true; return Task.CompletedTask; });

        var result = await runner.RunAsync(definition, new OutcomeData());

        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe(StepErrorCode);
        compensated.ShouldBeTrue();

        var saga = await recording.LoadAsync();
        saga.Status.ShouldBe(SagaStatus.Compensated);
        saga.CompletedAtUtc.ShouldNotBeNull();
        saga.ErrorMessage.ShouldBe(StepErrorCode);

        await AssertNotStuckAsync(store, saga.SagaId);
    }

    /// <summary>
    /// Runs the same saga with a throwing compensation, then asserts the persisted saga is Failed
    /// with the compensation error code and the exception type, never the exception message.
    /// </summary>
    /// <param name="store">The real saga store under test.</param>
    /// <param name="factory">The provider's saga state factory.</param>
    /// <param name="flush">See <see cref="AssertStepFailureEndsCompensatedAsync"/>.</param>
    public static async Task AssertCompensationFailureEndsFailedAsync(
        ISagaStore store,
        ISagaStateFactory factory,
        Func<Task>? flush = null)
    {
        var recording = new RecordingSagaStore(store, flush);
        var (runner, definition) = Create(
            recording, factory, (_, _, _) => throw new InvalidOperationException("secret compensation detail"));

        var result = await runner.RunAsync(definition, new OutcomeData());

        // The caller still receives the original step error.
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe(StepErrorCode);

        var saga = await recording.LoadAsync();
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.CompletedAtUtc.ShouldNotBeNull();
        var persisted = saga.ErrorMessage.ShouldNotBeNull();
        persisted.ShouldBe($"{SagaErrorCodes.CompensationFailed}:{nameof(InvalidOperationException)}");
        persisted.ShouldNotContain("secret");

        await AssertNotStuckAsync(store, saga.SagaId);
    }

    private static (SagaRunner Runner, BuiltSagaDefinition<OutcomeData> Definition) Create(
        ISagaStore store,
        ISagaStateFactory factory,
        Func<OutcomeData, IRequestContext, CancellationToken, Task> compensateFirstStep)
    {
        var orchestrator = new SagaOrchestrator(
            store,
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            factory,
            new JsonMessageSerializer());

        var runner = new SagaRunner(
            orchestrator,
            new RequestContextAccessor(),
            NullLogger<SagaRunner>.Instance);

        Func<OutcomeData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, OutcomeData>>> succeed =
            (data, _, _) => ValueTask.FromResult(Right<EncinaError, OutcomeData>(data));
        Func<OutcomeData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, OutcomeData>>> fail =
            (_, _, _) => ValueTask.FromResult(
                Left<EncinaError, OutcomeData>(EncinaErrors.Create(StepErrorCode, "step 2 failed")));

        var definition = SagaDefinition.Create<OutcomeData>("OutcomeSaga")
            .Step("Reserve").Execute(succeed).Compensate(compensateFirstStep)
            .Step("Charge").Execute(fail)
            .Build();

        return (runner, definition);
    }

    private static async Task AssertNotStuckAsync(ISagaStore store, Guid sagaId)
    {
        // A minimal threshold (the stores reject zero) after a short wait: a saga left Running or
        // Compensating would be reported as stuck, which is what #2085 fixes for a compensated one.
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        var stuck = await store.GetStuckSagasAsync(TimeSpan.FromMilliseconds(10), 100);
        var sagas = stuck.Match(Right: s => s.ToList(), Left: e => throw new InvalidOperationException(e.GetCode().IfNone("?")));
        sagas.ShouldNotContain(s => s.SagaId == sagaId);
    }

    /// <summary>
    /// Forwards to the real store, remembers the id of the saga the runner added (the runner
    /// returns only the step error) and optionally flushes after each write.
    /// </summary>
    private sealed class RecordingSagaStore(ISagaStore inner, Func<Task>? flush) : ISagaStore
    {
        private Guid _sagaId;

        public async Task<ISagaState> LoadAsync()
        {
            var loaded = await inner.GetAsync(_sagaId);
            var option = loaded.Match(Right: o => o, Left: e => throw new InvalidOperationException(e.GetCode().IfNone("?")));
            return option.Match(Some: s => s, None: () => throw new InvalidOperationException("Saga not found"));
        }

        public Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
            => inner.GetAsync(sagaId, cancellationToken);

        public async Task<Either<EncinaError, LanguageExt.Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
        {
            _sagaId = sagaState.SagaId;
            var result = await inner.AddAsync(sagaState, cancellationToken);
            await FlushAsync();
            return result;
        }

        public async Task<Either<EncinaError, LanguageExt.Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
        {
            var result = await inner.UpdateAsync(sagaState, cancellationToken);
            await FlushAsync();
            return result;
        }

        public Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(TimeSpan olderThan, int batchSize, CancellationToken cancellationToken = default)
            => inner.GetStuckSagasAsync(olderThan, batchSize, cancellationToken);

        public Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(int batchSize, CancellationToken cancellationToken = default)
            => inner.GetExpiredSagasAsync(batchSize, cancellationToken);

        public Task<Either<EncinaError, LanguageExt.Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
            => inner.SaveChangesAsync(cancellationToken);

        private async Task FlushAsync()
        {
            if (flush is not null)
            {
                await flush();
            }
        }
    }
}
