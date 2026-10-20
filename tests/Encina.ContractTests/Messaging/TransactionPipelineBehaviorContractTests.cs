#pragma warning disable CA2012 // NSubstitute configures a ValueTask-returning member by calling it inside Returns(...).

using Encina.Messaging;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Messaging;

/// <summary>
/// Behavioral contract tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>: it takes a
/// lease from the accessor, commits it on success, rolls it back on a failure or an exception, and never begins a
/// transaction of its own.
/// </summary>
[Trait("Category", "Contract")]
public sealed class TransactionPipelineBehaviorContractTests
{
    [Fact]
    public void ImplementsIPipelineBehavior()
    {
        typeof(IPipelineBehavior<TestTransactionalCommand, string>).IsAssignableFrom(
            typeof(TransactionPipelineBehavior<TestTransactionalCommand, string>)).ShouldBeTrue();
    }

    [Fact]
    public void IsSealed()
    {
        typeof(TransactionPipelineBehavior<TestTransactionalCommand, string>).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Success_CommitsLease()
    {
        var (behavior, lease) = Create();

        var result = await behavior.Handle(
            new TestTransactionalCommand("v"), CreateContext(), () => Ok("ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await lease.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await lease.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Failure_RollsBackLease()
    {
        var (behavior, lease) = Create();

        var result = await behavior.Handle(
            new TestTransactionalCommand("v"), CreateContext(), () => Fail(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await lease.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await lease.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Exception_RollsBackLease()
    {
        var (behavior, lease) = Create();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("boom");

        var result = await behavior.Handle(
            new TestTransactionalCommand("v"), CreateContext(), next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await lease.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BeginFails_ReturnsTheErrorWithoutRunningTheHandler()
    {
        var accessor = Substitute.For<IDbTransactionAccessor>();
        accessor.BeginOrJoinAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<Either<EncinaError, ITransactionLease>>(TransactionErrors.NotSupported(typeof(object))));
        var behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(
            accessor, NullLogger<TransactionPipelineBehavior<TestTransactionalCommand, string>>.Instance);
        var handlerRan = false;
        RequestHandlerCallback<string> next = () =>
        {
            handlerRan = true;
            return Ok("ok");
        };

        var result = await behavior.Handle(
            new TestTransactionalCommand("v"), CreateContext(), next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        handlerRan.ShouldBeFalse();
    }

    private static (TransactionPipelineBehavior<TestTransactionalCommand, string> Behavior, ITransactionLease Lease) Create()
    {
        var lease = Substitute.For<ITransactionLease>();
        lease.IsOwner.Returns(true);
        lease.CommitAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var accessor = Substitute.For<IDbTransactionAccessor>();
        accessor.BeginOrJoinAsync(Arg.Any<CancellationToken>()).Returns(
            new ValueTask<Either<EncinaError, ITransactionLease>>(Either<EncinaError, ITransactionLease>.Right(lease)));

        return (
            new TransactionPipelineBehavior<TestTransactionalCommand, string>(
                accessor, NullLogger<TransactionPipelineBehavior<TestTransactionalCommand, string>>.Instance),
            lease);
    }

    private static ValueTask<Either<EncinaError, string>> Ok(string value)
        => new(Either<EncinaError, string>.Right(value));

    private static ValueTask<Either<EncinaError, string>> Fail()
        => new(Either<EncinaError, string>.Left(EncinaErrors.Create("TEST", "fail")));

    private static IRequestContext CreateContext()
    {
        var ctx = Substitute.For<IRequestContext>();
        ctx.CorrelationId.Returns("corr-1");
        return ctx;
    }

    public sealed record TestTransactionalCommand(string Value) : ICommand<string>;
}
