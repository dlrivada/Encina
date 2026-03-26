#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern for ValueTask
using Encina.Security.ABAC;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Additional tests for <see cref="ObligationExecutor"/> covering advice execution,
/// missing handlers, and handler failures.
/// </summary>
public sealed class ObligationExecutorAdditionalTests
{
    private readonly ILogger<ObligationExecutor> _logger = NullLogger<ObligationExecutor>.Instance;

    private static PolicyEvaluationContext CreateContext() => new()
    {
        SubjectAttributes = AttributeBag.Empty,
        ResourceAttributes = AttributeBag.Empty,
        EnvironmentAttributes = AttributeBag.Empty,
        ActionAttributes = AttributeBag.Empty,
        RequestType = typeof(object)
    };

    private static Obligation CreateObligation(string id, FulfillOn fulfillOn = FulfillOn.Permit) => new()
    {
        Id = id,
        FulfillOn = fulfillOn,
        AttributeAssignments = []
    };

    private static AdviceExpression CreateAdvice(string id, FulfillOn appliesTo = FulfillOn.Deny) => new()
    {
        Id = id,
        AppliesTo = appliesTo,
        AttributeAssignments = []
    };

    [Fact]
    public async Task ExecuteObligationsAsync_WithEmptyList_ReturnsUnit()
    {
        var sut = new ObligationExecutor([], _logger);
        var context = CreateContext();

        var result = await sut.ExecuteObligationsAsync([], context, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_WithNoMatchingHandler_ReturnsError()
    {
        var sut = new ObligationExecutor([], _logger);
        var context = CreateContext();
        var obligations = new List<Obligation>
        {
            CreateObligation("log-access")
        };

        var result = await sut.ExecuteObligationsAsync(obligations, context, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_WithMatchingHandler_ExecutesSuccessfully()
    {
        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle("log-access").Returns(true);
        handler.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ObligationExecutor([handler], _logger);
        var context = CreateContext();
        var obligations = new List<Obligation>
        {
            CreateObligation("log-access")
        };

        var result = await sut.ExecuteObligationsAsync(obligations, context, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_WhenHandlerFails_ReturnsError()
    {
        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle("audit-log").Returns(true);
        handler.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(EncinaError.New("Handler failed"))));

        var sut = new ObligationExecutor([handler], _logger);
        var context = CreateContext();
        var obligations = new List<Obligation>
        {
            CreateObligation("audit-log")
        };

        var result = await sut.ExecuteObligationsAsync(obligations, context, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_WithMultipleObligations_ExecutesAll()
    {
        var handler1 = Substitute.For<IObligationHandler>();
        handler1.CanHandle("obl-1").Returns(true);
        handler1.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var handler2 = Substitute.For<IObligationHandler>();
        handler2.CanHandle("obl-2").Returns(true);
        handler2.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ObligationExecutor([handler1, handler2], _logger);
        var context = CreateContext();
        var obligations = new List<Obligation>
        {
            CreateObligation("obl-1"),
            CreateObligation("obl-2")
        };

        var result = await sut.ExecuteObligationsAsync(obligations, context, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    // Advice tests

    [Fact]
    public async Task ExecuteAdviceAsync_WithEmptyList_ReturnsImmediately()
    {
        var sut = new ObligationExecutor([], _logger);
        var context = CreateContext();

        // Should not throw
        await sut.ExecuteAdviceAsync([], context, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_WithNoMatchingHandler_LogsAndContinues()
    {
        var sut = new ObligationExecutor([], _logger);
        var context = CreateContext();
        var advice = new List<AdviceExpression>
        {
            CreateAdvice("show-reason")
        };

        // Should not throw - advice is best-effort
        await sut.ExecuteAdviceAsync(advice, context, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_WhenHandlerFails_LogsAndContinues()
    {
        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle("log-advice").Returns(true);
        handler.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(EncinaError.New("Advice handler failed"))));

        var sut = new ObligationExecutor([handler], _logger);
        var context = CreateContext();
        var advice = new List<AdviceExpression>
        {
            CreateAdvice("log-advice")
        };

        // Should not throw - advice failures don't affect decision
        await sut.ExecuteAdviceAsync(advice, context, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_WithMatchingHandler_ExecutesSuccessfully()
    {
        var handler = Substitute.For<IObligationHandler>();
        handler.CanHandle("show-info").Returns(true);
        handler.HandleAsync(Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var sut = new ObligationExecutor([handler], _logger);
        var context = CreateContext();
        var advice = new List<AdviceExpression>
        {
            CreateAdvice("show-info", FulfillOn.Permit)
        };

        await sut.ExecuteAdviceAsync(advice, context, CancellationToken.None);

        await handler.Received(1).HandleAsync(
            Arg.Any<Obligation>(), Arg.Any<PolicyEvaluationContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteObligationsAsync_WithNullObligations_ThrowsArgumentNullException()
    {
        var sut = new ObligationExecutor([], _logger);
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ExecuteObligationsAsync(null!, CreateContext(), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAdviceAsync_WithNullAdvice_ThrowsArgumentNullException()
    {
        var sut = new ObligationExecutor([], _logger);
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ExecuteAdviceAsync(null!, CreateContext(), CancellationToken.None));
    }
}
