using Encina.Messaging.ContentRouter;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.RoutingSlip;
using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.ScatterGather;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// The category of every error the content router, routing slip, saga and scatter-gather producers return (ADR-062,
/// #3723), read through <c>GetCategory()</c> from the real producers.
/// </summary>
/// <remarks>
/// Not reachable through a public result, so the completeness static scan (<c>ErrorCategoryCompletenessStaticScanTests</c>)
/// is their check: the unreachable <c>Right:</c> branches that build <c>routingslip.step_failed</c> and
/// <c>saga.step_failed</c>, the <c>saga.not_found</c> error embedded in the dead letter record of
/// <c>SagaNotFoundDispatcher</c> and the error embedded in the dead letter record of <c>SagaOrchestrator.FailAsync</c>.
/// </remarks>
public sealed class MessagingPatternsErrorCategoryTests
{
    // Public: NSubstitute cannot proxy IHandleSagaNotFound<T> over an inaccessible message type.
    public sealed record TestMessage(string Kind);

    private sealed record TestRequest(string Value);

    private sealed record TestData
    {
        public int Value { get; init; }
    }

    private sealed class SagaPayload
    {
        public int Value { get; set; }
    }

    #region Content router

    [Fact]
    public async Task ContentRouter_NoMatchingRoute_IsInternal()
    {
        var definition = ContentRouterBuilder.Create<TestMessage, string>()
            .When(m => m.Kind == "A").RouteTo(_ => "a")
            .Build();

        var result = await NewRouter(new ContentRouterOptions()).RouteAsync(definition, new TestMessage("B"));

        CodeOf(result).ShouldBe(ContentRouterErrorCodes.NoMatchingRoute);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ContentRouter_RouteHandlerThrows_IsInternal()
    {
        var definition = ContentRouterBuilder.Create<TestMessage, string>()
            .When(_ => true).RouteTo((Func<TestMessage, string>)(_ => throw new InvalidOperationException("boom")))
            .Build();

        var result = await NewRouter(new ContentRouterOptions()).RouteAsync(definition, new TestMessage("A"));

        CodeOf(result).ShouldBe(ContentRouterErrorCodes.RouteExecutionFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ContentRouter_RouteHandlerCancelled_IsCancelled()
    {
        var definition = ContentRouterBuilder.Create<TestMessage, string>()
            .When(_ => true).RouteTo((Func<TestMessage, string>)(_ => throw new OperationCanceledException()))
            .Build();

        var result = await NewRouter(new ContentRouterOptions()).RouteAsync(definition, new TestMessage("A"));

        CodeOf(result).ShouldBe(ContentRouterErrorCodes.RouterCancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task ContentRouter_DefaultRouteThrows_IsInternal()
    {
        var definition = ContentRouterBuilder.Create<TestMessage, string>()
            .When(m => m.Kind == "A").RouteTo(_ => "a")
            .Default((Func<TestMessage, Either<EncinaError, string>>)(_ => throw new InvalidOperationException("boom")))
            .Build();

        var result = await NewRouter(new ContentRouterOptions()).RouteAsync(definition, new TestMessage("B"));

        CodeOf(result).ShouldBe(ContentRouterErrorCodes.RouteExecutionFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ContentRouter_ParallelEvaluationWithACancelledToken_IsCancelled()
    {
        var options = new ContentRouterOptions { AllowMultipleMatches = true, EvaluateInParallel = true };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await NewRouter(options).RouteAsync(TwoRoutes(), new TestMessage("A"), cts.Token);

        CodeOf(result).ShouldBe(ContentRouterErrorCodes.RouterCancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task ContentRouter_ParallelEvaluationWithAnInvalidDegreeOfParallelism_IsInternal()
    {
        var options = new ContentRouterOptions { AllowMultipleMatches = true, EvaluateInParallel = true, MaxDegreeOfParallelism = 0 };

        var result = await NewRouter(options).RouteAsync(TwoRoutes(), new TestMessage("A"));

        CodeOf(result).ShouldBe(ContentRouterErrorCodes.RouteExecutionFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    private static BuiltContentRouterDefinition<TestMessage, string> TwoRoutes()
        => ContentRouterBuilder.Create<TestMessage, string>()
            .When(_ => true).RouteTo(_ => "one")
            .When(_ => true).RouteTo(_ => "two")
            .Build();

    private static global::Encina.Messaging.ContentRouter.ContentRouter NewRouter(ContentRouterOptions options)
        => new(options, NullLogger<global::Encina.Messaging.ContentRouter.ContentRouter>.Instance);

    #endregion

    #region Routing slip

    [Fact]
    public async Task RoutingSlip_StepCancelled_IsCancelled()
    {
        var definition = RoutingSlipBuilder.Create<TestData>("Slip")
            .Step("Cancel").Execute((_, _, _) => throw new OperationCanceledException())
            .Build();

        var result = await NewRoutingSlipRunner().RunAsync(definition, new TestData());

        CodeOf(result).ShouldBe(RoutingSlipErrorCodes.HandlerCancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task RoutingSlip_StepThrows_IsInternal()
    {
        var definition = RoutingSlipBuilder.Create<TestData>("Slip")
            .Step("Crash").Execute((_, _, _) => throw new InvalidOperationException("boom"))
            .Build();

        var result = await NewRoutingSlipRunner().RunAsync(definition, new TestData());

        CodeOf(result).ShouldBe(RoutingSlipErrorCodes.HandlerFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    private static RoutingSlipRunner NewRoutingSlipRunner()
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(Substitute.For<IRequestContext>());
        return new RoutingSlipRunner(accessor, new RoutingSlipOptions(), NullLogger<RoutingSlipRunner>.Instance);
    }

    #endregion

    #region Saga orchestrator

    public static TheoryData<string> NotFoundOperations => new()
    {
        "Advance",
        "Complete",
        "StartCompensation",
        "CompensateStep",
        "Fail",
        "Timeout"
    };

    [Theory]
    [MemberData(nameof(NotFoundOperations))]
    public async Task SagaOrchestrator_UnknownSaga_IsNotFound(string operation)
    {
        var orchestrator = NewOrchestrator(new FakeSagaStore());

        var error = await InvokeAsync(orchestrator, operation, Guid.NewGuid());

        error.GetCode().IfNone(string.Empty).ShouldBe(SagaErrorCodes.NotFound);
        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
    }

    public static TheoryData<string, string> InvalidStatusOperations => new()
    {
        { "Advance", SagaStatus.Completed },
        { "Complete", SagaStatus.Completed },
        { "StartCompensation", SagaStatus.Completed },
        { "CompensateStep", SagaStatus.Running },
        { "Timeout", SagaStatus.Completed }
    };

    [Theory]
    [MemberData(nameof(InvalidStatusOperations))]
    public async Task SagaOrchestrator_SagaInTheWrongStatus_IsConflict(string operation, string status)
    {
        var store = new FakeSagaStore();
        var sagaId = await SeedAsync(store, status, "{}");

        var error = await InvokeAsync(NewOrchestrator(store), operation, sagaId);

        error.GetCode().IfNone(string.Empty).ShouldBe(SagaErrorCodes.InvalidStatus);
        error.GetCategory().ShouldBe(ErrorCategory.Conflict);
    }

    [Fact]
    public async Task SagaOrchestrator_UndeserializableData_IsInternal()
    {
        var store = new FakeSagaStore();
        var sagaId = await SeedAsync(store, SagaStatus.Running, "null");

        var result = await NewOrchestrator(store).AdvanceAsync<SagaPayload>(sagaId);

        CodeOf(result).ShouldBe(SagaErrorCodes.DeserializationFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Theory]
    [InlineData("CompensateStep")]
    [InlineData("Timeout")]
    public async Task SagaOrchestrator_StoreReadFails_KeepsTheStoreError(string operation)
    {
        var store = Substitute.For<ISagaStore>();
        store.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Option<ISagaState>>>(
                EncinaErrors.Create("store.down", ErrorCategory.Unavailable, "store down")));

        var error = await InvokeAsync(NewOrchestrator(store), operation, Guid.NewGuid());

        error.GetCode().IfNone(string.Empty).ShouldBe("store.down");
        error.GetCategory().ShouldBe(ErrorCategory.Unavailable);
    }

    [Fact]
    public async Task SagaOrchestrator_StoreUpdateFailsWhileTimingOut_KeepsTheStoreError()
    {
        var inner = new FakeSagaStore();
        var sagaId = await SeedAsync(inner, SagaStatus.Running, "{}");
        var store = Substitute.For<ISagaStore>();
        store.GetAsync(sagaId, Arg.Any<CancellationToken>()).Returns(call => inner.GetAsync(sagaId));
        store.UpdateAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, LanguageExt.Unit>>(
                EncinaErrors.Create("store.down", ErrorCategory.Unavailable, "store down")));

        var error = await InvokeAsync(NewOrchestrator(store), "Timeout", sagaId);

        error.GetCode().IfNone(string.Empty).ShouldBe("store.down");
        error.GetCategory().ShouldBe(ErrorCategory.Unavailable);
    }

    private static async Task<EncinaError> InvokeAsync(SagaOrchestrator orchestrator, string operation, Guid sagaId)
        => operation switch
        {
            "Advance" => ErrorOf(await orchestrator.AdvanceAsync<SagaPayload>(sagaId)),
            "Complete" => ErrorOf(await orchestrator.CompleteAsync(sagaId)),
            "StartCompensation" => ErrorOf(await orchestrator.StartCompensationAsync(sagaId, "test.failed")),
            "CompensateStep" => ErrorOf(await orchestrator.CompensateStepAsync(sagaId)),
            "Fail" => ErrorOf(await orchestrator.FailAsync(sagaId, "test.failed")),
            "Timeout" => ErrorOf(await orchestrator.TimeoutAsync(sagaId)),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    private static EncinaError ErrorOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e);

    private static async Task<Guid> SeedAsync(FakeSagaStore store, string status, string data)
    {
        var state = new FakeSagaState { SagaType = "TestSaga", Status = status, Data = data };
        (await store.AddAsync(state)).IsRight.ShouldBeTrue();
        return state.SagaId;
    }

    private static SagaOrchestrator NewOrchestrator(ISagaStore store)
        => new(
            store,
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            new StateFactory(),
            new JsonMessageSerializer());

    private sealed class StateFactory : ISagaStateFactory
    {
        public ISagaState Create(
            Guid sagaId,
            string sagaType,
            string data,
            string status,
            int currentStep,
            DateTime startedAtUtc,
            DateTime? timeoutAtUtc = null) => new FakeSagaState
            {
                SagaId = sagaId,
                SagaType = sagaType,
                Data = data,
                Status = status,
                CurrentStep = currentStep,
                StartedAtUtc = startedAtUtc,
                LastUpdatedAtUtc = startedAtUtc,
                TimeoutAtUtc = timeoutAtUtc
            };
    }

    #endregion

    #region Saga runner

    [Fact]
    public async Task SagaRunner_StepCancelled_IsCancelled()
    {
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Cancel").Execute((_, _, _) => throw new OperationCanceledException())
            .Build();

        var result = await NewSagaRunner().RunAsync(definition, new TestData());

        CodeOf(result).ShouldBe(SagaErrorCodes.HandlerCancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task SagaRunner_StepThrows_IsInternal()
    {
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Crash").Execute((_, _, _) => throw new InvalidOperationException("boom"))
            .Build();

        var result = await NewSagaRunner().RunAsync(definition, new TestData());

        CodeOf(result).ShouldBe(SagaErrorCodes.HandlerFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    private static SagaRunner NewSagaRunner()
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(Substitute.For<IRequestContext>());
        return new SagaRunner(NewOrchestrator(new FakeSagaStore()), accessor, NullLogger<SagaRunner>.Instance);
    }

    #endregion

    #region Saga not found: dispatcher and context

    [Fact]
    public async Task SagaNotFoundDispatcher_HandlerThrows_IsInternal()
    {
        var handler = Substitute.For<IHandleSagaNotFound<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<SagaNotFoundContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        var result = await DispatchAsync(handler, CancellationToken.None);

        CodeOf(result).ShouldBe(SagaErrorCodes.HandlerFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task SagaNotFoundDispatcher_HandlerCancelled_IsCancelled()
    {
        using var cts = new CancellationTokenSource();
        var handler = Substitute.For<IHandleSagaNotFound<TestMessage>>();
        handler.HandleAsync(Arg.Any<TestMessage>(), Arg.Any<SagaNotFoundContext>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        var result = await DispatchAsync(handler, cts.Token);

        CodeOf(result).ShouldBe(SagaErrorCodes.HandlerCancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task SagaNotFoundContext_WithoutTheDeadLetterQueue_IsInternal()
    {
        var context = new SagaNotFoundContext(Guid.NewGuid(), "TestSaga", typeof(TestMessage), "message-1");

        var result = await context.MoveToDeadLetterAsync("no saga");

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.NotConfigured);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task SagaNotFoundContext_WithoutTheSourceMessageId_IsInvalidRequest()
    {
        var context = new SagaNotFoundContext(Guid.NewGuid(), "TestSaga", typeof(TestMessage), sourceMessageId: null);
        context.UseDeadLetter(_ => Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));

        var result = await context.MoveToDeadLetterAsync("no saga");

        CodeOf(result).ShouldBe(DeadLetterErrorCodes.SourceMessageIdRequired);
        CategoryOf(result).ShouldBe(ErrorCategory.InvalidRequest);
    }

    private static async Task<Either<EncinaError, LanguageExt.Unit>> DispatchAsync(
        IHandleSagaNotFound<TestMessage> handler,
        CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        using var provider = services.BuildServiceProvider();
        var dispatcher = new SagaNotFoundDispatcher(provider, NullLogger<SagaNotFoundDispatcher>.Instance);
        var context = new SagaNotFoundContext(Guid.NewGuid(), "TestSaga", typeof(TestMessage), "message-1");

        return await dispatcher.DispatchAsync(new TestMessage("A"), context, cancellationToken);
    }

    #endregion

    #region Scatter-gather: fixed categories

    [Fact]
    public async Task ScatterGather_CallerCancelled_IsCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var definition = Scatter(
            [_ => Ok(1), _ => Ok(2)],
            static b => b.GatherAllAllowingPartialFailures(),
            sequential: true);

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"), cts.Token);

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.Cancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task ScatterGather_OperationTimesOut_IsTimeout()
    {
        var definition = Scatter(
            [async ct => await WaitForCancellationAsync(ct), _ => Ok(2)],
            static b => b.GatherAllAllowingPartialFailures(),
            sequential: true,
            timeout: TimeSpan.FromMilliseconds(50));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.Timeout);
        CategoryOf(result).ShouldBe(ErrorCategory.Timeout);
    }

    [Fact]
    public async Task ScatterGather_InvalidDegreeOfParallelism_IsInternal()
    {
        var definition = Scatter([_ => Ok(1)], static b => b.GatherAll());
        var runner = NewScatterRunner(new ScatterGatherOptions { MaxDegreeOfParallelism = -1 });

        var result = await runner.ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.HandlerFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ScatterGather_ScatterHandlerThrows_IsInternalAndCancelledPerScatter()
    {
        var definition = Scatter(
            [_ => throw new InvalidOperationException("boom"), _ => throw new OperationCanceledException()],
            static b => b.GatherAllAllowingPartialFailures(),
            sequential: true);

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        var scatters = ScattersOf(result);
        ErrorOf(scatters[0].Result).GetCode().IfNone(string.Empty).ShouldBe(ScatterGatherErrorCodes.ScatterFailed);
        ErrorOf(scatters[0].Result).GetCategory().ShouldBe(ErrorCategory.Internal);
        ErrorOf(scatters[1].Result).GetCode().IfNone(string.Empty).ShouldBe(ScatterGatherErrorCodes.Cancelled);
        ErrorOf(scatters[1].Result).GetCategory().ShouldBe(ErrorCategory.Cancelled);
    }

    [Fact]
    public async Task ScatterGather_WaitForAllWithAFailedScatter_IsInternal()
    {
        var definition = Scatter(
            [_ => Ok(1), _ => Fail("x.failed", ErrorCategory.NotFound)],
            static b => b.GatherAll());

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.ScatterFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ScatterGather_GatherHandlerThrows_IsInternal()
    {
        var definition = Scatter(
            [_ => Ok(1)],
            static b => b.GatherAllAllowingPartialFailures(),
            aggregate: static g => g.Aggregate((Func<IReadOnlyList<ScatterExecutionResult<int>>, Either<EncinaError, int>>)(
                _ => throw new InvalidOperationException("boom"))));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.GatherFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ScatterGather_GatherHandlerCancelled_IsCancelled()
    {
        var definition = Scatter(
            [_ => Ok(1)],
            static b => b.GatherAllAllowingPartialFailures(),
            aggregate: static g => g.Aggregate((Func<IReadOnlyList<ScatterExecutionResult<int>>, Either<EncinaError, int>>)(
                _ => throw new OperationCanceledException())));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.Cancelled);
        CategoryOf(result).ShouldBe(ErrorCategory.Cancelled);
    }

    #endregion

    #region Scatter-gather: derived category (D23)

    [Fact]
    public async Task ScatterGather_AllScattersFailedWithTheSameCategory_TakesThatCategory()
    {
        var definition = Scatter(
            [_ => Fail("a.gone", ErrorCategory.NotFound), _ => Fail("b.gone", ErrorCategory.NotFound)],
            static b => b.GatherFirst());

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.NotFound);
    }

    [Fact]
    public async Task ScatterGather_AllScattersFailedWithMixedCategories_IsInternal()
    {
        var definition = Scatter(
            [_ => Fail("a.gone", ErrorCategory.NotFound), _ => Fail("b.denied", ErrorCategory.Forbidden)],
            static b => b.GatherFirst());

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ScatterGather_AllScattersFailedWithoutADeclaredCategory_IsInternal()
    {
        var definition = Scatter(
            [_ => ValueTask.FromResult(Left<EncinaError, int>(EncinaError.New("plain failure"))), _ => throw new InvalidOperationException("boom")],
            static b => b.GatherFirst());

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ScatterGather_AllScattersFailedWhenTheTimeBudgetCutTheFanOut_IsTimeout()
    {
        var definition = Scatter(
            [async ct => await WaitForCancellationAsync(ct), async ct => await WaitForCancellationAsync(ct)],
            static b => b.GatherFirst(),
            timeout: TimeSpan.FromMilliseconds(50));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Timeout);
    }

    [Fact]
    public async Task ScatterGather_QuorumNotReachedWithTheSameCategory_TakesThatCategory()
    {
        var definition = Scatter(
            [_ => Ok(1), _ => Fail("a.down", ErrorCategory.Unavailable), _ => Fail("b.down", ErrorCategory.Unavailable)],
            static b => b.GatherQuorum(2));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.QuorumNotReached);
        CategoryOf(result).ShouldBe(ErrorCategory.Unavailable);
    }

    [Fact]
    public async Task ScatterGather_QuorumNotReachedWithMixedCategories_IsInternal()
    {
        var definition = Scatter(
            [_ => Ok(1), _ => Fail("a.down", ErrorCategory.Unavailable), _ => Fail("b.gone", ErrorCategory.NotFound)],
            static b => b.GatherQuorum(2));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.QuorumNotReached);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task ScatterGather_QuorumNotReachedWhenTheTimeBudgetCutTheFanOut_IsTimeout()
    {
        var definition = Scatter(
            [_ => Ok(1), async ct => await WaitForCancellationAsync(ct), async ct => await WaitForCancellationAsync(ct)],
            static b => b.GatherQuorum(2),
            timeout: TimeSpan.FromMilliseconds(50));

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.QuorumNotReached);
        CategoryOf(result).ShouldBe(ErrorCategory.Timeout);
    }

    [Fact]
    public async Task ScatterGather_TakeFirstWithFailedScatters_TakesTheirSharedCategory()
    {
        var definition = Scatter(
            [_ => Fail("a.denied", ErrorCategory.Forbidden), _ => Fail("b.denied", ErrorCategory.Forbidden)],
            static b => b.GatherAllAllowingPartialFailures(),
            aggregate: static g => g.TakeFirst());

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Forbidden);
    }

    [Theory]
    [InlineData("best")]
    [InlineData("max")]
    public async Task ScatterGather_TakeBestAndMaxWithFailedScatters_TakeTheirSharedCategory(string take)
    {
        var definition = TakeString(take, ErrorCategory.Unavailable, ErrorCategory.Unavailable);

        var result = await NewScatterRunner().ExecuteAsync(definition, new TestRequest("x"));

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Unavailable);
    }

    [Theory]
    [InlineData("first")]
    [InlineData("best")]
    [InlineData("max")]
    public async Task ScatterGather_TakeWithoutAnyScatterResult_IsInternal(string take)
    {
        var definition = TakeString(take);

        var result = await definition.GatherHandler([], CancellationToken.None);

        CodeOf(result).ShouldBe(ScatterGatherErrorCodes.AllScattersFailed);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    private static BuiltScatterGatherDefinition<TestRequest, string> TakeString(string take, params ErrorCategory[] failures)
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, string>("Scatter");
        for (var i = 0; i < failures.Length; i++)
        {
            var error = EncinaErrors.Create($"scatter{i}.failed", failures[i], "scatter failed");
            builder.ScatterTo($"Handler{i}", (_, _) => ValueTask.FromResult(Left<EncinaError, string>(error)));
        }

        if (failures.Length == 0)
        {
            builder.ScatterTo("Handler", (_, _) => ValueTask.FromResult(Right<EncinaError, string>("ok")));
        }

        var gather = builder.GatherAllAllowingPartialFailures();
        var parent = take switch
        {
            "first" => gather.TakeFirst(),
            "best" => gather.TakeBest(static v => v),
            _ => gather.TakeMax(static v => v)
        };

        parent.ShouldBeSameAs(builder);
        return builder.Build();
    }

    #endregion

    #region Helpers

    private static ScatterGatherRunner NewScatterRunner(ScatterGatherOptions? options = null)
        => new(options ?? new ScatterGatherOptions(), NullLogger<ScatterGatherRunner>.Instance);

    private static BuiltScatterGatherDefinition<TestRequest, int> Scatter(
        IReadOnlyList<Func<CancellationToken, ValueTask<Either<EncinaError, int>>>> handlers,
        Func<ScatterGatherBuilder<TestRequest, int>, GatherBuilder<TestRequest, int>> gather,
        bool sequential = false,
        TimeSpan? timeout = null,
        Func<GatherBuilder<TestRequest, int>, ScatterGatherBuilder<TestRequest, int>>? aggregate = null)
    {
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Scatter");
        if (timeout is { } budget)
        {
            builder.WithTimeout(budget);
        }

        if (sequential)
        {
            builder.ExecuteSequentially();
        }

        for (var i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            builder.ScatterTo($"Handler{i}", (_, ct) => handler(ct));
        }

        var gatherBuilder = gather(builder);
        (aggregate is null
            ? gatherBuilder.Aggregate((results, _) => ValueTask.FromResult(Right<EncinaError, int>(results.Count(r => r.IsSuccess))))
            : aggregate(gatherBuilder)).ShouldBeSameAs(builder);

        return builder.Build();
    }

    private static ValueTask<Either<EncinaError, int>> Ok(int value)
        => ValueTask.FromResult(Right<EncinaError, int>(value));

    private static ValueTask<Either<EncinaError, int>> Fail(string code, ErrorCategory category)
        => ValueTask.FromResult(Left<EncinaError, int>(EncinaErrors.Create(code, category, "scatter failed")));

    private static async ValueTask<Either<EncinaError, int>> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return Right<EncinaError, int>(0);
    }

    private static IReadOnlyList<ScatterExecutionResult<int>> ScattersOf(Either<EncinaError, ScatterGatherResult<int>> result)
        => result.Match(Right: r => r.ScatterResults, Left: e => throw new InvalidOperationException($"Expected a Right, got {e.GetCode().IfNone("?")}."));

    private static EncinaError ErrorOf(Either<EncinaError, int> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e);

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e.GetCode().IfNone(string.Empty));

    private static ErrorCategory CategoryOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e.GetCategory());

    #endregion
}
