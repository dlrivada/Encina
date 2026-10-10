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

    #region Saga not found

    public sealed record NotFoundMessage(int Value);

    private sealed class MovingHandler : IHandleSagaNotFound<NotFoundMessage>
    {
        public List<Either<EncinaError, Unit>> Results { get; } = [];

        public async Task HandleAsync(NotFoundMessage message, SagaNotFoundContext context, CancellationToken cancellationToken)
            => Results.Add(await context.MoveToDeadLetterAsync("no saga for this order", cancellationToken));
    }

    [Fact]
    public async Task SagaNotFound_MovedWithAnId_IsCapturedOnce_AndARedeliveryKeepsOne()
    {
        var handler = new MovingHandler();
        using var host = NotFoundHost(handler, configure: null);
        var dispatcher = Dispatcher(host);

        (await dispatcher.DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"))).IsRight.ShouldBeTrue();
        (await dispatcher.DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"))).IsRight.ShouldBeTrue();

        handler.Results.ShouldAllBe(r => r.IsRight);
        var deadLetter = host.DeadLettersOf(DeadLetterSourcePatterns.Saga).ShouldHaveSingleItem();
        deadLetter.SourceMessageId.ShouldBe("transport-1");
        deadLetter.ErrorCode.ShouldBe(SagaErrorCodes.NotFound);
        deadLetter.RequestType.ShouldContain(nameof(NotFoundMessage));
    }

    [Fact]
    public async Task SagaNotFound_DistinctIds_AreTwoDeadLetters()
    {
        var handler = new MovingHandler();
        using var host = NotFoundHost(handler, configure: null);
        var dispatcher = Dispatcher(host);

        await dispatcher.DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"));
        await dispatcher.DispatchAsync(new NotFoundMessage(1), NotFound("transport-2"));

        host.DeadLettersOf(DeadLetterSourcePatterns.Saga).Count.ShouldBe(2);
    }

    [Fact]
    public async Task SagaNotFound_FlagOff_ReturnsNotConfigured_AndCapturesNothing()
    {
        var handler = new MovingHandler();
        using var host = NotFoundHost(handler, o => o.IntegrateWithSagas = false);

        var dispatched = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"));

        CodeOf(handler.Results.ShouldHaveSingleItem()).ShouldBe(DeadLetterErrorCodes.NotConfigured);
        CodeOf(dispatched).ShouldBe(DeadLetterErrorCodes.NotConfigured);
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task SagaNotFound_NoId_ReturnsSourceMessageIdRequired_AndCapturesNothing()
    {
        var handler = new MovingHandler();
        using var host = NotFoundHost(handler, configure: null);

        var dispatched = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound(sourceMessageId: null));

        CodeOf(handler.Results.ShouldHaveSingleItem()).ShouldBe(DeadLetterErrorCodes.SourceMessageIdRequired);
        CodeOf(dispatched).ShouldBe(DeadLetterErrorCodes.SourceMessageIdRequired);
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task SagaNotFound_IdTheQueueCannotStore_IsRejected()
    {
        var handler = new MovingHandler();
        using var host = NotFoundHost(handler, configure: null);

        var dispatched = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound(" transport-1 "));

        CodeOf(dispatched).ShouldBe(DeadLetterErrorCodes.CaptureRejected);
        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task SagaNotFound_FailedMoveThenIgnored_DispatchSucceeds()
    {
        var handler = Substitute.For<IHandleSagaNotFound<NotFoundMessage>>();
        handler.HandleAsync(Arg.Any<NotFoundMessage>(), Arg.Any<SagaNotFoundContext>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var context = call.Arg<SagaNotFoundContext>();
                (await context.MoveToDeadLetterAsync("try")).IsLeft.ShouldBeTrue();
                context.Ignore();
            });
        using var host = DeadLetterCaptureHost.Create(configureServices: services => services.AddSingleton(handler));

        var dispatched = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound(sourceMessageId: null));

        dispatched.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SagaNotFound_ContextReusedWithoutTheQueue_DoesNotKeepTheEarlierCapture()
    {
        var handler = new MovingHandler();
        using var host = NotFoundHost(handler, configure: null);
        var context = NotFound("transport-1");
        (await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), context)).IsRight.ShouldBeTrue();
        var withoutQueue = new SagaNotFoundDispatcher(host.Provider, NullLogger<SagaNotFoundDispatcher>.Instance);

        var dispatched = await withoutQueue.DispatchAsync(new NotFoundMessage(2), context);

        CodeOf(dispatched).ShouldBe(DeadLetterErrorCodes.NotConfigured);
        host.DeadLettersOf(DeadLetterSourcePatterns.Saga).Count.ShouldBe(1);
    }

    [Fact]
    public async Task SagaNotFoundDispatcher_NullArguments_Throw()
    {
        using var host = DeadLetterCaptureHost.Create();
        var dispatcher = Dispatcher(host);

        (await Should.ThrowAsync<ArgumentNullException>(() => dispatcher.DispatchAsync<NotFoundMessage>(null!, NotFound("t"))))
            .ParamName.ShouldBe("message");
        (await Should.ThrowAsync<ArgumentNullException>(() => dispatcher.DispatchAsync(new NotFoundMessage(1), null!)))
            .ParamName.ShouldBe("context");
        Should.Throw<ArgumentNullException>(() => new SagaNotFoundDispatcher(null!, NullLogger<SagaNotFoundDispatcher>.Instance))
            .ParamName.ShouldBe("serviceProvider");
        Should.Throw<ArgumentNullException>(() => new SagaNotFoundDispatcher(host.Provider, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task SagaNotFound_CaptureFails_TheHandlerGetsTheError()
    {
        var deadLetterStore = Substitute.For<IDeadLetterStore>();
        deadLetterStore.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, bool>>(EncinaErrors.Create(DeadLetterErrorCodes.StoreFailed, "down")));
        var handler = new MovingHandler();
        using var host = DeadLetterCaptureHost.Create(store: deadLetterStore, configureServices: services =>
            services.AddSingleton<IHandleSagaNotFound<NotFoundMessage>>(handler));

        var dispatched = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"));

        CodeOf(handler.Results.ShouldHaveSingleItem()).ShouldBe(DeadLetterErrorCodes.StoreFailed);
        CodeOf(dispatched).ShouldBe(DeadLetterErrorCodes.StoreFailed);
    }

    [Fact]
    public async Task SagaNotFound_WithoutTheDeadLetterQueue_ReturnsNotConfigured()
    {
        var handler = new MovingHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IHandleSagaNotFound<NotFoundMessage>>(handler);
        using var provider = services.BuildServiceProvider();
        var dispatcher = new SagaNotFoundDispatcher(provider, NullLogger<SagaNotFoundDispatcher>.Instance);

        await dispatcher.DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"));

        CodeOf(handler.Results.ShouldHaveSingleItem()).ShouldBe(DeadLetterErrorCodes.NotConfigured);
    }

    [Fact]
    public async Task SagaNotFound_NoHandlerRegistered_PassesThrough()
    {
        using var host = DeadLetterCaptureHost.Create();

        (await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"))).IsRight.ShouldBeTrue();

        host.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task SagaNotFound_HandlerThrows_ReturnsHandlerFailed()
    {
        var handler = Substitute.For<IHandleSagaNotFound<NotFoundMessage>>();
        handler.HandleAsync(Arg.Any<NotFoundMessage>(), Arg.Any<SagaNotFoundContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("handler crashed"));
        using var host = DeadLetterCaptureHost.Create(configureServices: services => services.AddSingleton(handler));

        var result = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"));

        CodeOf(result).ShouldBe(SagaErrorCodes.HandlerFailed);
    }

    [Fact]
    public async Task SagaNotFound_HandlerCancelled_ReturnsHandlerCancelled()
    {
        using var cts = new CancellationTokenSource();
        var handler = Substitute.For<IHandleSagaNotFound<NotFoundMessage>>();
        handler.HandleAsync(Arg.Any<NotFoundMessage>(), Arg.Any<SagaNotFoundContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => { cts.Cancel(); throw new OperationCanceledException(cts.Token); });
        using var host = DeadLetterCaptureHost.Create(configureServices: services => services.AddSingleton(handler));

        var result = await Dispatcher(host).DispatchAsync(new NotFoundMessage(1), NotFound("transport-1"), cts.Token);

        CodeOf(result).ShouldBe(SagaErrorCodes.HandlerCancelled);
    }

    private static DeadLetterCaptureHost NotFoundHost(MovingHandler handler, Action<DeadLetterOptions>? configure)
        => DeadLetterCaptureHost.Create(configure, configureServices: services =>
            services.AddSingleton<IHandleSagaNotFound<NotFoundMessage>>(handler));

    private static SagaNotFoundDispatcher Dispatcher(DeadLetterCaptureHost host)
        => new(host.Provider, NullLogger<SagaNotFoundDispatcher>.Instance, host.Capture, host.Clock);

    private static SagaNotFoundContext NotFound(string? sourceMessageId)
        => new(Guid.NewGuid(), "OrderSaga", typeof(NotFoundMessage), sourceMessageId);

    private static string CodeOf(Either<EncinaError, Unit> result)
        => result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty));

    #endregion

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
