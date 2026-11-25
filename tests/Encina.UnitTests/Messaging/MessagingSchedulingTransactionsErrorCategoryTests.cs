using System.Data;
using Encina.Messaging;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using Encina.Messaging.Transactions;
using Encina.UnitTests.Messaging.Transactions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Messaging;

/// <summary>
/// The category of every error the scheduler orchestrator, the compiled expression dispatcher, the transaction
/// pipeline behavior and <see cref="TransactionErrors"/> return (ADR-062, #4198), read through <c>GetCategory()</c>
/// from the real producers.
/// </summary>
/// <remarks>
/// Not reachable through a public result, so the completeness static scan (<c>ErrorCategoryCompletenessStaticScanTests</c>)
/// is their check: the variable-code error that <c>SchedulerOrchestrator</c> embeds in the dead letter record of a
/// scheduled message that used up its retries.
/// </remarks>
public sealed class MessagingSchedulingTransactionsErrorCategoryTests
{
    private sealed record TestRequest(string Value);

    private sealed record TransactionRequest(Guid Id) : IRequest<string>;

    private sealed record UnknownShape(string Data);

    #region Scheduler orchestrator

    [Fact]
    public async Task Scheduler_ScheduleInThePast_IsValidation()
    {
        var result = await NewScheduler().ScheduleAsync(new TestRequest("a"), DateTime.UtcNow.AddHours(-1));

        CodeOf(result).ShouldBe(SchedulingErrorCodes.InvalidScheduleTime);
        CategoryOf(result).ShouldBe(ErrorCategory.Validation);
    }

    [Fact]
    public async Task Scheduler_ScheduleWithNonPositiveDelay_IsValidation()
    {
        var result = await NewScheduler().ScheduleAsync(new TestRequest("a"), TimeSpan.Zero);

        CodeOf(result).ShouldBe(SchedulingErrorCodes.InvalidDelay);
        CategoryOf(result).ShouldBe(ErrorCategory.Validation);
    }

    [Fact]
    public async Task Scheduler_RecurringDisabled_IsInternal()
    {
        var scheduler = NewScheduler(new SchedulingOptions { EnableRecurringMessages = false });

        var result = await scheduler.ScheduleRecurringAsync(new TestRequest("a"), "* * * * *");

        CodeOf(result).ShouldBe(SchedulingErrorCodes.RecurringDisabled);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public async Task Scheduler_NoCronParser_IsInternal()
    {
        var result = await NewScheduler().ScheduleRecurringAsync(new TestRequest("a"), "* * * * *");

        CodeOf(result).ShouldBe(SchedulingErrorCodes.NoCronParser);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Dispatcher

    [Fact]
    public async Task Dispatcher_UnknownRequestShape_IsInternal()
    {
        var dispatcher = new CompiledExpressionScheduledMessageDispatcher(Substitute.For<IEncina>());

        var result = await dispatcher.DispatchAsync(typeof(UnknownShape), new UnknownShape("x"), CancellationToken.None);

        CodeOf(result).ShouldBe(SchedulingErrorCodes.UnknownRequestShape);
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Transaction pipeline behavior

    [Fact]
    public async Task TransactionBehavior_HandlerThrows_TransactionFailedIsInternal()
    {
        await using var connection = new RecordingTransactionalConnection();
        await using var accessor = new DbTransactionAccessor(connection, NullLogger<DbTransactionAccessor>.Instance);
        var behavior = new TransactionPipelineBehavior<TransactionRequest, string>(
            accessor, NullLogger<TransactionPipelineBehavior<TransactionRequest, string>>.Instance);
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("handler failed");

        var result = await behavior.Handle(
            new TransactionRequest(Guid.NewGuid()), Substitute.For<IRequestContext>(), next, CancellationToken.None);

        CodeOf(result).ShouldBe("transaction.failed");
        CategoryOf(result).ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Transaction errors

    [Fact]
    public void TransactionErrors_BeginFailed_IsInternal()
    {
        var error = TransactionErrors.BeginFailed(new InvalidOperationException("open failed"));

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.BeginFailedCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void TransactionErrors_CommitFailed_IsInternal()
    {
        var error = TransactionErrors.CommitFailed(new InvalidOperationException("commit failed"));

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.CommitFailedCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("TransientTransactionError")]
    public void TransactionErrors_CommitFailedWithLabel_IsInternal(string? label)
    {
        var error = TransactionErrors.CommitFailed(new InvalidOperationException("commit failed"), label);

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.CommitFailedCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void TransactionErrors_RollbackOnly_IsInternal()
    {
        var error = TransactionErrors.RollbackOnly();

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.RollbackOnlyCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void TransactionErrors_IsolationLevelMismatch_IsInternal()
    {
        var error = TransactionErrors.IsolationLevelMismatch(IsolationLevel.Serializable, IsolationLevel.ReadCommitted);

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.IsolationLevelMismatchCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void TransactionErrors_LeaseDisposed_IsInternal()
    {
        var error = TransactionErrors.LeaseDisposed();

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.LeaseDisposedCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void TransactionErrors_NotSupported_IsInternal()
    {
        var error = TransactionErrors.NotSupported(typeof(TestRequest));

        error.GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.NotSupportedCode);
        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    #endregion

    #region Helpers

    private static SchedulerOrchestrator NewScheduler(SchedulingOptions? options = null)
    {
        options ??= new SchedulingOptions();
        return new SchedulerOrchestrator(
            Substitute.For<IScheduledMessageStore>(),
            options,
            NullLogger<SchedulerOrchestrator>.Instance,
            Substitute.For<IScheduledMessageFactory>(),
            new ExponentialBackoffRetryPolicy(options),
            new JsonMessageSerializer());
    }

    private static string CodeOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e.GetCode().IfNone(string.Empty));

    private static ErrorCategory CategoryOf<T>(Either<EncinaError, T> result)
        => result.Match(Right: _ => throw new InvalidOperationException("Expected a Left."), Left: e => e.GetCategory());

    #endregion
}
