using Encina.Messaging.DeadLetter;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// The capture through the runtime paths the applications use: a provider outbox processor cycle (which
/// resolves the capture from its cycle scope) and the low-ceremony <see cref="SagaRunner"/> (#1991).
/// </summary>
public sealed class DeadLetterRuntimeCaptureTests
{
    public sealed record RuntimeNotification(int Value) : INotification;

    public sealed record RunnerData
    {
        public int Value { get; init; }
    }

    private static readonly EncinaError HandlerError = EncinaErrors.Create("runtime.handler_failed", "failure");

    #region Outbox processor

    [Fact]
    public async Task OutboxProcessor_ExhaustedMessage_IsCapturedThroughTheCycleScope()
    {
        using var host = CreateOutboxHost(configure: null, out var outbox);
        var message = await AddOutboxMessageAsync(outbox, host);

        await RunProcessorUntilAsync(host, () => outbox.GetMessage(message.Id)!.RetryCount >= 1);

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Outbox).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe(message.Id.ToString("D"));
    }

    [Fact]
    public async Task OutboxProcessor_FlagOff_ExhaustsWithoutCapturing()
    {
        using var host = CreateOutboxHost(o => o.IntegrateWithOutbox = false, out var outbox);
        var message = await AddOutboxMessageAsync(outbox, host);

        await RunProcessorUntilAsync(host, () => outbox.GetMessage(message.Id)!.RetryCount >= 1);

        host.Store.GetMessages().ShouldBeEmpty();
    }

    private static DeadLetterCaptureHost CreateOutboxHost(Action<DeadLetterOptions>? configure, out FakeOutboxStore outbox)
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(HandlerError)));
        var store = new FakeOutboxStore();
        outbox = store;
        return DeadLetterCaptureHost.Create(configure, encina: encina, configureServices: services =>
            services.AddSingleton<IOutboxStore>(store));
    }

    private static async Task<IOutboxMessage> AddOutboxMessageAsync(FakeOutboxStore outbox, DeadLetterCaptureHost host)
    {
        var message = new FakeOutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = typeof(RuntimeNotification).AssemblyQualifiedName!,
            Content = host.Serializer.Serialize(new RuntimeNotification(1)),
            CreatedAtUtc = host.Clock.GetUtcNow().UtcDateTime
        };
        await outbox.AddAsync(message);
        return message;
    }

    private static async Task RunProcessorUntilAsync(DeadLetterCaptureHost host, Func<bool> done)
    {
        var processor = new CaptureOutboxProcessor(
            host.Provider,
            new OutboxOptions { MaxRetries = 1, ProcessingInterval = TimeSpan.FromMilliseconds(10) });
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        await processor.StartAsync(cts.Token);
        try
        {
            while (!done())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token);
            }
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    private sealed class CaptureOutboxProcessor(IServiceProvider serviceProvider, OutboxOptions options)
        : OutboxProcessorBase(serviceProvider, NullLogger.Instance, options);

    #endregion

    #region Saga runner

    [Fact]
    public async Task SagaRunner_FailedCompensation_CapturesTheFailedSagaOnce()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, runner) = Runner(host);
        var definition = SagaDefinition.Create<RunnerData>("CaptureSaga")
            .Step("Reserve").Execute((data, _, _) => ValueTask.FromResult(Right<EncinaError, RunnerData>(data)))
            .Compensate((_, _, _) => throw new InvalidOperationException("compensation crashed"))
            .Step("Charge").Execute((_, _, _) => ValueTask.FromResult(Left<EncinaError, RunnerData>(HandlerError)))
            .Build();

        (await runner.RunAsync(definition, new RunnerData())).IsLeft.ShouldBeTrue();

        var saga = store.GetSagas().ShouldHaveSingleItem();
        saga.Status.ShouldBe(SagaStatus.Failed);
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Saga).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe(saga.SagaId.ToString("D"));
        deadLetter.ErrorCode.ShouldStartWith(SagaErrorCodes.CompensationFailed);
    }

    [Fact]
    public async Task SagaRunner_SuccessfulCompensation_CapturesNothing()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, runner) = Runner(host);
        var definition = SagaDefinition.Create<RunnerData>("CaptureSaga")
            .Step("Reserve").Execute((data, _, _) => ValueTask.FromResult(Right<EncinaError, RunnerData>(data)))
            .Compensate((_, _, _) => Task.CompletedTask)
            .Step("Charge").Execute((_, _, _) => ValueTask.FromResult(Left<EncinaError, RunnerData>(HandlerError)))
            .Build();

        (await runner.RunAsync(definition, new RunnerData())).IsLeft.ShouldBeTrue();

        store.GetSagas().ShouldHaveSingleItem().Status.ShouldBe(SagaStatus.Compensated);
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task SagaRunner_UnexpectedException_CapturesTheFailedSaga()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (store, runner) = Runner(host);
        var definition = SagaDefinition.Create<RunnerData>("CaptureSaga")
            .Step("Crash").Execute((_, _, _) => throw new InvalidOperationException("step crashed"))
            .Build();

        (await runner.RunAsync(definition, new RunnerData())).IsLeft.ShouldBeTrue();

        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Saga).ShouldHaveSingleItem();
        deadLetter.ErrorCode.ShouldBe(SagaErrorCodes.HandlerFailed);
        deadLetter.SourceMessageId.ShouldBe(store.GetSagas().Single().SagaId.ToString("D"));
    }

    [Fact]
    public async Task SagaRunner_CancelledRun_CapturesTheFailedSaga()
    {
        using var host = DeadLetterCaptureHost.Create();
        var (_, runner) = Runner(host);
        var definition = SagaDefinition.Create<RunnerData>("CaptureSaga")
            .Step("Cancel").Execute((_, _, _) => throw new OperationCanceledException())
            .Build();

        (await runner.RunAsync(definition, new RunnerData())).IsLeft.ShouldBeTrue();

        host.DeadLettersOf(DeadLetterSourcePatterns.Saga).ShouldHaveSingleItem()
            .ErrorCode.ShouldBe(SagaErrorCodes.HandlerCancelled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SagaRunner_CaptureFails_ReturnsTheCaptureError(bool cancelled)
    {
        var deadLetterStore = Substitute.For<IDeadLetterStore>();
        deadLetterStore.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, bool>>(EncinaErrors.Create(DeadLetterErrorCodes.StoreFailed, "down")));
        using var host = DeadLetterCaptureHost.Create(store: deadLetterStore);
        var (store, runner) = Runner(host);
        Exception thrown = cancelled ? new OperationCanceledException() : new InvalidOperationException("step crashed");
        var definition = SagaDefinition.Create<RunnerData>("CaptureSaga")
            .Step("Crash").Execute((_, _, _) => throw thrown)
            .Build();

        var result = await runner.RunAsync(definition, new RunnerData());

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty)).ShouldBe(DeadLetterErrorCodes.StoreFailed);
        store.GetSagas().ShouldHaveSingleItem().Status.ShouldBe(SagaStatus.Failed);
    }

    private static (FakeSagaStore Store, SagaRunner Runner) Runner(DeadLetterCaptureHost host)
    {
        var store = new FakeSagaStore(host.Clock);
        var orchestrator = new SagaOrchestrator(
            store,
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            new DeadLetterCaptureHost.SagaFactory(),
            host.Serializer,
            host.Clock,
            host.Capture);
        return (store, new SagaRunner(orchestrator, new RequestContextAccessor(), NullLogger<SagaRunner>.Instance));
    }

    #endregion
}
