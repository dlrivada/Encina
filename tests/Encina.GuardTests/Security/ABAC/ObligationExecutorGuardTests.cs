using Encina.Security.ABAC;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="ObligationExecutor"/>.
/// </summary>
public class ObligationExecutorGuardTests
{
    private static PolicyEvaluationContext CreateContext() => new()
    {
        SubjectAttributes = AttributeBag.Empty,
        ResourceAttributes = AttributeBag.Empty,
        EnvironmentAttributes = AttributeBag.Empty,
        ActionAttributes = AttributeBag.Empty,
        RequestType = typeof(object)
    };

    #region Constructor Guards

    [Fact]
    public void Constructor_NullHandlers_ThrowsArgumentNullException()
    {
        var act = () => new ObligationExecutor(
            null!,
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("handlers");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    #endregion

    #region ExecuteObligationsAsync Guards

    [Fact]
    public async Task ExecuteObligationsAsync_NullObligations_ThrowsArgumentNullException()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        var act = () => executor.ExecuteObligationsAsync(null!, CreateContext(), CancellationToken.None).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_NullContext_ThrowsArgumentNullException()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        var act = () => executor.ExecuteObligationsAsync(new List<Obligation>(), null!, CancellationToken.None).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_EmptyObligations_ReturnsRight()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        var result = await executor.ExecuteObligationsAsync(new List<Obligation>(), CreateContext(), CancellationToken.None);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteObligationsAsync_NoHandler_ReturnsLeft()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        var obligations = new List<Obligation>
        {
            new() { Id = "audit-log", FulfillOn = FulfillOn.Permit, AttributeAssignments = [] }
        };

        var result = await executor.ExecuteObligationsAsync(obligations, CreateContext(), CancellationToken.None);
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ExecuteAdviceAsync Guards

    [Fact]
    public async Task ExecuteAdviceAsync_NullAdviceExpressions_ThrowsArgumentNullException()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        var act = () => executor.ExecuteAdviceAsync(null!, CreateContext(), CancellationToken.None).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAdviceAsync_NullContext_ThrowsArgumentNullException()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        var act = () => executor.ExecuteAdviceAsync(new List<AdviceExpression>(), null!, CancellationToken.None).AsTask();
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAdviceAsync_EmptyAdvice_ReturnsImmediately()
    {
        var executor = new ObligationExecutor(
            Enumerable.Empty<IObligationHandler>(),
            NullLoggerFactory.Instance.CreateLogger<ObligationExecutor>());

        // Should not throw
        await executor.ExecuteAdviceAsync(new List<AdviceExpression>(), CreateContext(), CancellationToken.None);
    }

    #endregion
}
