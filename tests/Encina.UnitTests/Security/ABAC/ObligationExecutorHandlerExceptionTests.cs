#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Security.ABAC;

using LanguageExt;

using Microsoft.Extensions.Logging.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// A handler that throws is a handler that failed (#1634): mandatory obligations deny, advice
/// continues, cancellation propagates, and no exception message reaches a log or an error.
/// </summary>
public sealed class ObligationExecutorHandlerExceptionTests
{
    private const string Sentinel = "SENTINEL-obligation-secret";

    private static readonly PolicyEvaluationContext Context = new()
    {
        SubjectAttributes = AttributeBag.Empty,
        ResourceAttributes = AttributeBag.Empty,
        EnvironmentAttributes = AttributeBag.Empty,
        ActionAttributes = AttributeBag.Empty,
        RequestType = typeof(object)
    };

    private readonly FakeLogger<ObligationExecutor> _logger = new();

    private static Obligation Obligation(string id) =>
        new() { Id = id, FulfillOn = FulfillOn.Permit, AttributeAssignments = [] };

    private static AdviceExpression Advice(string id) =>
        new() { Id = id, AppliesTo = FulfillOn.Permit, AttributeAssignments = [] };

    private static IObligationHandler Handler(string id, Func<CancellationToken, Either<EncinaError, Unit>> behavior)
    {
        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle(id).Returns(true);
        handler.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(behavior(call.ArgAt<CancellationToken>(2))));
        return handler;
    }

    private static Either<EncinaError, Unit> Throw(Exception exception) => throw exception;

    [Fact]
    public async Task ExecuteObligationsAsync_HandlerThrows_ReturnsObligationFailedWithoutTheMessage()
    {
        var sut = new ObligationExecutor([Handler("ob-1", _ => Throw(new InvalidOperationException(Sentinel)))], _logger);

        var result = await sut.ExecuteObligationsAsync([Obligation("ob-1")], Context, CancellationToken.None);

        result.IsLeft.ShouldBeTrue("a throwing mandatory obligation must deny (fail closed)");
        result.IfLeft(error =>
        {
            error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.ObligationFailedCode);
            error.Message.ShouldNotContain(Sentinel);
        });

        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9078);
        records.ShouldContain(r => r.Id.Id == 9011 && r.Message.Contains(ABACErrors.ObligationHandlerExceptionCode));
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        records.Where(r => r.Exception != null)
            .ShouldAllBe(r => !r.Exception!.ToString().Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteObligationsAsync_HandlerThrows_StopsBeforeTheNextObligation()
    {
        var second = Handler("ob-2", _ => Right<EncinaError, Unit>(unit));
        var sut = new ObligationExecutor(
            [Handler("ob-1", _ => Throw(new InvalidOperationException(Sentinel))), second], _logger);

        await sut.ExecuteObligationsAsync([Obligation("ob-1"), Obligation("ob-2")], Context, CancellationToken.None);

        await second.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_HandlerThrows_ContinuesWithTheNextAdvice()
    {
        var second = Handler("advice-2", _ => Right<EncinaError, Unit>(unit));
        var sut = new ObligationExecutor(
            [Handler("advice-1", _ => Throw(new InvalidOperationException(Sentinel))), second], _logger);

        await sut.ExecuteAdviceAsync([Advice("advice-1"), Advice("advice-2")], Context, CancellationToken.None);

        await second.Received(1).HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9021 && r.Message.Contains(ABACErrors.ObligationHandlerExceptionCode));
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteObligationsAsync_TokenCancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = new ObligationExecutor(
            [Handler("ob-1", token => Throw(new OperationCanceledException(token)))], _logger);

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.ExecuteObligationsAsync([Obligation("ob-1")], Context, cts.Token));
    }

    [Fact]
    public async Task ExecuteAdviceAsync_TokenCancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = new ObligationExecutor(
            [Handler("advice-1", token => Throw(new OperationCanceledException(token)))], _logger);

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.ExecuteAdviceAsync([Advice("advice-1")], Context, cts.Token));
    }

    [Fact]
    public async Task ExecuteObligationsAsync_CanHandleThrows_ReturnsObligationFailedWithoutTheMessage()
    {
        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle(Arg.Any<string>()).Returns(_ => throw new InvalidOperationException(Sentinel));
        var sut = new ObligationExecutor([handler], _logger);

        var result = await sut.ExecuteObligationsAsync([Obligation("ob-1")], Context, CancellationToken.None);

        result.IsLeft.ShouldBeTrue("a handler whose CanHandle throws fails its mandatory obligation (fail closed)");
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(ABACErrors.ObligationFailedCode));
        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9011 && r.Message.Contains(ABACErrors.ObligationHandlerExceptionCode));
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
        records.Where(r => r.Exception != null)
            .ShouldAllBe(r => !r.Exception!.ToString().Contains(Sentinel, StringComparison.Ordinal));
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_CanHandleThrows_SkipsTheAdviceAndContinues()
    {
        var throwing = Substitute.For<IObligationHandler>();
        throwing.CanHandle("advice-1").Returns(_ => throw new InvalidOperationException(Sentinel));
        var second = Handler("advice-2", _ => Right<EncinaError, Unit>(unit));
        var sut = new ObligationExecutor([throwing, second], _logger);

        await sut.ExecuteAdviceAsync([Advice("advice-1"), Advice("advice-2")], Context, CancellationToken.None);

        await second.Received(1).HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
        var records = _logger.Collector.GetSnapshot();
        records.ShouldContain(r => r.Id.Id == 9021 && r.Message.Contains(ABACErrors.ObligationHandlerExceptionCode));
        records.ShouldAllBe(r => !r.Message.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteObligationsAsync_CancellationWithoutCancelledToken_IsAFailedObligation()
    {
        var sut = new ObligationExecutor(
            [Handler("ob-1", _ => Throw(new OperationCanceledException()))], _logger);

        var result = await sut.ExecuteObligationsAsync([Obligation("ob-1")], Context, CancellationToken.None);

        result.IsLeft.ShouldBeTrue("an internal timeout of the handler is a failure, not a request cancellation");
    }
}
