using Encina.Security.ABAC;
using Encina.Security.ABAC.Evaluation;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="ObligationExecutor"/>: execution of XACML 3.0
/// obligations (mandatory) and advice (best-effort) expressions.
/// </summary>
public sealed class ObligationExecutorTests
{
    private readonly NullLogger<ObligationExecutor> _logger = new();

    private static PolicyEvaluationContext MakeContext() =>
        new()
        {
            SubjectAttributes = AttributeBag.Empty,
            ResourceAttributes = AttributeBag.Empty,
            EnvironmentAttributes = AttributeBag.Empty,
            ActionAttributes = AttributeBag.Empty,
            RequestType = typeof(object)
        };

    #region ExecuteObligationsAsync — No Obligations

    [Fact]
    public async Task ExecuteObligationsAsync_EmptyList_ReturnsUnit()
    {
        var executor = new ObligationExecutor([], _logger);
        var ctx = MakeContext();

        var result = await executor.ExecuteObligationsAsync([], ctx, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region ExecuteObligationsAsync — Handler Found

    [Fact]
    public async Task ExecuteObligationsAsync_HandlerSucceeds_ReturnsUnit()
    {
        var handler = new TestObligationHandler("log-access", success: true);
        var executor = new ObligationExecutor([handler], _logger);
        var ctx = MakeContext();
        var obligations = new List<Obligation>
        {
            new() { Id = "log-access", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };

        var result = await executor.ExecuteObligationsAsync(obligations, ctx, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        handler.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteObligationsAsync_MultipleHandlers_AllSucceed_ReturnsUnit()
    {
        var handler1 = new TestObligationHandler("audit", success: true);
        var handler2 = new TestObligationHandler("notify", success: true);
        var executor = new ObligationExecutor([handler1, handler2], _logger);
        var ctx = MakeContext();
        var obligations = new List<Obligation>
        {
            new() { Id = "audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] },
            new() { Id = "notify", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };

        var result = await executor.ExecuteObligationsAsync(obligations, ctx, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        handler1.Invocations.ShouldBe(1);
        handler2.Invocations.ShouldBe(1);
    }

    #endregion

    #region ExecuteObligationsAsync — Handler Missing

    [Fact]
    public async Task ExecuteObligationsAsync_NoHandlerRegistered_ReturnsLeft()
    {
        var executor = new ObligationExecutor([], _logger);
        var ctx = MakeContext();
        var obligations = new List<Obligation>
        {
            new() { Id = "missing-handler", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };

        var result = await executor.ExecuteObligationsAsync(obligations, ctx, CancellationToken.None);

        result.IsLeft.ShouldBeTrue("missing handler should fail per XACML §7.18");
    }

    #endregion

    #region ExecuteObligationsAsync — Handler Fails

    [Fact]
    public async Task ExecuteObligationsAsync_HandlerFails_ReturnsLeft()
    {
        var handler = new TestObligationHandler("audit", success: false);
        var executor = new ObligationExecutor([handler], _logger);
        var ctx = MakeContext();
        var obligations = new List<Obligation>
        {
            new() { Id = "audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };

        var result = await executor.ExecuteObligationsAsync(obligations, ctx, CancellationToken.None);

        result.IsLeft.ShouldBeTrue("failed handler should deny access per XACML §7.18");
        handler.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteObligationsAsync_SecondHandlerFails_StopsExecution()
    {
        var handler1 = new TestObligationHandler("audit", success: true);
        var handler2 = new TestObligationHandler("notify", success: false);
        var executor = new ObligationExecutor([handler1, handler2], _logger);
        var ctx = MakeContext();
        var obligations = new List<Obligation>
        {
            new() { Id = "audit", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] },
            new() { Id = "notify", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };

        var result = await executor.ExecuteObligationsAsync(obligations, ctx, CancellationToken.None);

        result.IsLeft.ShouldBeTrue("second handler failure should deny");
        handler1.Invocations.ShouldBe(1, "first handler still executed");
        handler2.Invocations.ShouldBe(1, "second handler was attempted");
    }

    #endregion

    #region ExecuteAdviceAsync — Success

    [Fact]
    public async Task ExecuteAdviceAsync_EmptyList_Completes()
    {
        var executor = new ObligationExecutor([], _logger);
        var ctx = MakeContext();

        // Should not throw
        await executor.ExecuteAdviceAsync([], ctx, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_HandlerSucceeds_ExecutesAdvice()
    {
        var handler = new TestObligationHandler("show-disclaimer", success: true);
        var executor = new ObligationExecutor([handler], _logger);
        var ctx = MakeContext();
        var advice = new List<AdviceExpression>
        {
            new()
            {
                Id = "show-disclaimer",
                AppliesTo = FulfillOn.Permit,
                AttributeAssignments = []
            }
        };

        await executor.ExecuteAdviceAsync(advice, ctx, CancellationToken.None);

        handler.Invocations.ShouldBe(1);
    }

    #endregion

    #region ExecuteAdviceAsync — Handler Missing (Best-Effort)

    [Fact]
    public async Task ExecuteAdviceAsync_NoHandler_SkipsWithoutError()
    {
        var executor = new ObligationExecutor([], _logger);
        var ctx = MakeContext();
        var advice = new List<AdviceExpression>
        {
            new()
            {
                Id = "optional-advice",
                AppliesTo = FulfillOn.Permit,
                AttributeAssignments = []
            }
        };

        // Advice is best-effort; missing handler should not throw
        await executor.ExecuteAdviceAsync(advice, ctx, CancellationToken.None);
    }

    #endregion

    #region ExecuteAdviceAsync — Handler Fails (Best-Effort)

    [Fact]
    public async Task ExecuteAdviceAsync_HandlerFails_ContinuesExecution()
    {
        var handler1 = new TestObligationHandler("advice-1", success: false);
        var handler2 = new TestObligationHandler("advice-2", success: true);
        var executor = new ObligationExecutor([handler1, handler2], _logger);
        var ctx = MakeContext();
        var advice = new List<AdviceExpression>
        {
            new() { Id = "advice-1", AppliesTo = FulfillOn.Permit, AttributeAssignments = [] },
            new() { Id = "advice-2", AppliesTo = FulfillOn.Permit, AttributeAssignments = [] }
        };

        // Advice failures should not throw; second advice should still execute
        await executor.ExecuteAdviceAsync(advice, ctx, CancellationToken.None);

        handler1.Invocations.ShouldBe(1);
        handler2.Invocations.ShouldBe(1, "advice execution continues even after failure");
    }

    #endregion

    #region Guard Clauses

    [Fact]
    public void Constructor_NullHandlers_Throws()
    {
        var act = () => new ObligationExecutor(null!, _logger);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new ObligationExecutor([], null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteObligationsAsync_NullObligations_Throws()
    {
        var executor = new ObligationExecutor([], _logger);
        var ctx = MakeContext();

        var act = () => executor.ExecuteObligationsAsync(null!, ctx, CancellationToken.None).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteObligationsAsync_NullContext_Throws()
    {
        var executor = new ObligationExecutor([], _logger);

        var act = () => executor.ExecuteObligationsAsync([], null!, CancellationToken.None).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_NullAdvice_Throws()
    {
        var executor = new ObligationExecutor([], _logger);
        var ctx = MakeContext();

        var act = () => executor.ExecuteAdviceAsync(null!, ctx, CancellationToken.None).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteAdviceAsync_NullContext_Throws()
    {
        var executor = new ObligationExecutor([], _logger);

        var act = () => executor.ExecuteAdviceAsync([], null!, CancellationToken.None).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region Test Helper — ObligationHandler

    /// <summary>
    /// Simple obligation handler for testing that tracks invocations.
    /// </summary>
    private sealed class TestObligationHandler(string obligationId, bool success) : IObligationHandler
    {
        public int Invocations { get; private set; }

        public bool CanHandle(string id) => id == obligationId;

        public ValueTask<Either<EncinaError, Unit>> HandleAsync(
            Obligation obligation,
            PolicyEvaluationContext context,
            CancellationToken cancellationToken)
        {
            Invocations++;

            return success
                ? new(Either<EncinaError, Unit>.Right(unit))
                : new(Either<EncinaError, Unit>.Left(
                    EncinaError.New($"Handler for '{obligation.Id}' failed.")));
        }
    }

    #endregion
}
