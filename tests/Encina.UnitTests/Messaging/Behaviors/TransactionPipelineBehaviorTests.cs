using Encina.Messaging;
using Encina.Messaging.Transactions;
using Encina.Testing.Identity;
using Encina.UnitTests.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Messaging.Behaviors;

/// <summary>
/// Unit tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>: it takes a lease on the scope's
/// transaction, commits it on success, rolls it back on a failure, an exception or a cancellation, and joins a
/// transaction another participant owns instead of beginning a second one.
/// </summary>
public sealed class TransactionPipelineBehaviorTests : IAsyncDisposable
{
    private readonly RecordingTransactionalConnection _connection = new();
    private readonly DbTransactionAccessor _accessor;
    private readonly TransactionPipelineBehavior<TestRequest, string> _behavior;

    public TransactionPipelineBehaviorTests()
    {
        _accessor = new DbTransactionAccessor(_connection, NullLogger<DbTransactionAccessor>.Instance);
        _behavior = new TransactionPipelineBehavior<TestRequest, string>(
            _accessor, NullLogger<TransactionPipelineBehavior<TestRequest, string>>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _accessor.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static RequestHandlerCallback<string> Succeed() =>
        () => ValueTask.FromResult<Either<EncinaError, string>>("Success");

    private ValueTask<Either<EncinaError, string>> Run(RequestHandlerCallback<string> next, CancellationToken ct = default) =>
        _behavior.Handle(new TestRequest(Guid.NewGuid()), CreateTestContext(), next, ct);

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var logger = NullLogger<TransactionPipelineBehavior<TestRequest, string>>.Instance;

        Should.Throw<ArgumentNullException>(() => new TransactionPipelineBehavior<TestRequest, string>(null!, logger))
            .ParamName.ShouldBe("transactionAccessor");
        Should.Throw<ArgumentNullException>(() => new TransactionPipelineBehavior<TestRequest, string>(_accessor, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task Handle_Success_CommitsWithTokenNoneAndDisposesTheTransaction()
    {
        using var cts = new CancellationTokenSource();

        var result = await Run(Succeed(), cts.Token);

        result.IsRight.ShouldBeTrue();
        _connection.BeginAsyncToken.ShouldBe(cts.Token);
        _connection.Transaction.CommitAsyncCalls.ShouldBe(1);
        _connection.Transaction.CommitAsyncToken.ShouldBe(CancellationToken.None);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(0);
        _connection.Transaction.DisposeAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_PublishesTheTransactionWhileRunning_AndClearsItAfterwards()
    {
        System.Data.IDbTransaction? seenByHandler = null;

        await Run(() =>
        {
            seenByHandler = _accessor.Current;
            return ValueTask.FromResult<Either<EncinaError, string>>("Success");
        });

        seenByHandler.ShouldBeSameAs(_connection.Transaction);
        _accessor.Current.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_HandlerReturnsError_RollsBackAndReturnsTheError()
    {
        var error = EncinaErrors.Create("test.error", "Test error");

        var result = await Run(() => ValueTask.FromResult<Either<EncinaError, string>>(error));

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("test.error");
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _connection.Transaction.RollbackAsyncToken.ShouldBe(CancellationToken.None);
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _connection.Transaction.DisposeAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_HandlerThrows_RollsBackAndReturnsTransactionFailed()
    {
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Handler failed");

        var result = await Run(next);

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe("transaction.failed");
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _accessor.Current.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_HandlerThrowsOperationCanceled_RollsBackAndRethrows()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        RequestHandlerCallback<string> next = () => throw new OperationCanceledException(cts.Token);
        _connection.SetState(System.Data.ConnectionState.Open);

        var act = async () => await Run(next, cts.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
        _connection.Transaction.RollbackAsyncToken.ShouldBe(CancellationToken.None);
        _connection.Transaction.DisposeAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_CommitFails_TheCommitErrorReplacesTheResponse()
    {
        _connection.Transaction.ThrowOnCommit = true;

        var result = await Run(Succeed());

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.CommitFailedCode);
        _connection.Transaction.RollbackAsyncCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_BeginFails_ReturnsTheErrorAndNeverRunsTheHandler()
    {
        _connection.ThrowOnBegin = true;
        var ran = false;

        var result = await Run(() =>
        {
            ran = true;
            return ValueTask.FromResult<Either<EncinaError, string>>("Success");
        });

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.BeginFailedCode);
        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ConnectionIsNotADbConnection_FailsClosed()
    {
        var plain = Substitute.For<System.Data.IDbConnection>();
        var accessor = new DbTransactionAccessor(plain, NullLogger<DbTransactionAccessor>.Instance);
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(
            accessor, NullLogger<TransactionPipelineBehavior<TestRequest, string>>.Instance);

        var result = await behavior.Handle(new TestRequest(Guid.NewGuid()), CreateTestContext(), Succeed(), CancellationToken.None);

        ((EncinaError)result).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.NotSupportedCode);
        plain.DidNotReceive().BeginTransaction();
    }

    [Fact]
    public async Task Handle_TransactionAlreadyOwnedByAnotherParticipant_JoinsAndOnlyTheOwnerCommits()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        var result = await Run(Succeed());

        result.IsRight.ShouldBeTrue();
        _connection.BeginAsyncCalls.ShouldBe(1);
        _connection.Transaction.CommitAsyncCalls.ShouldBe(0);
        _accessor.HasActiveTransaction.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_JoinedAndHandlerFails_MarksTheOwnersTransactionRollbackOnly()
    {
        await using var owner = (await _accessor.BeginOrJoinAsync(null)).IfLeft(static _ => throw new InvalidOperationException());

        await Run(() => ValueTask.FromResult<Either<EncinaError, string>>(EncinaErrors.Create("test.error", "x")));
        var committed = await owner.CommitAsync();

        ((EncinaError)committed).GetCode().IfNone(string.Empty).ShouldBe(TransactionErrors.RollbackOnlyCode);
    }

    [Fact]
    public async Task Handle_NullArguments_Throw()
    {
        var context = CreateTestContext();
        var request = new TestRequest(Guid.NewGuid());

        (await Should.ThrowAsync<ArgumentNullException>(async () => await _behavior.Handle(null!, context, Succeed(), CancellationToken.None)))
            .ParamName.ShouldBe("request");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _behavior.Handle(request, null!, Succeed(), CancellationToken.None)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _behavior.Handle(request, context, null!, CancellationToken.None)))
            .ParamName.ShouldBe("nextStep");
    }

    private static IRequestContext CreateTestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns(Guid.NewGuid().ToString());
        context.Identity.Returns(TestIdentity.User("test-user"));
        context.Timestamp.Returns(DateTimeOffset.UtcNow);
        return context;
    }
}

/// <summary>
/// Test request for behavior tests.
/// </summary>
internal sealed record TestRequest(Guid Id) : IRequest<string>;
