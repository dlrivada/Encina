using System.Data;
using Encina.Messaging;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Messaging;

/// <summary>
/// Behavioral contract tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>.
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
    public async Task Handle_Success_CommitsTransaction()
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        connection.State.Returns(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(connection);
        var context = CreateContext();

        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("ok"));

        var result = await behavior.Handle(new TestTransactionalCommand("v"), context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task Handle_Failure_RollsBack()
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        connection.State.Returns(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(connection);
        var context = CreateContext();

        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(
                Either<EncinaError, string>.Left(EncinaErrors.Create("TEST", "fail")));

        var result = await behavior.Handle(new TestTransactionalCommand("v"), context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        transaction.Received(1).Rollback();
    }

    [Fact]
    public async Task Handle_Exception_RollsBack()
    {
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction().Returns(transaction);
        connection.State.Returns(ConnectionState.Open);

        var behavior = new TransactionPipelineBehavior<TestTransactionalCommand, string>(connection);
        var context = CreateContext();

        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("boom");

        var result = await behavior.Handle(new TestTransactionalCommand("v"), context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        transaction.Received(1).Rollback();
    }

    private static IRequestContext CreateContext()
    {
        var ctx = Substitute.For<IRequestContext>();
        ctx.CorrelationId.Returns("corr-1");
        return ctx;
    }

    public sealed record TestTransactionalCommand(string Value) : ICommand<string>;
}
