using Encina.Messaging.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Messaging;

/// <summary>
/// Behavioral contract tests for <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class RecoverabilityPipelineBehaviorContractTests
{
    [Fact]
    public void ImplementsIPipelineBehavior()
    {
        typeof(IPipelineBehavior<TestRecoverableCommand, string>).IsAssignableFrom(
            typeof(RecoverabilityPipelineBehavior<TestRecoverableCommand, string>)).ShouldBeTrue();
    }

    [Fact]
    public void IsSealed()
    {
        typeof(RecoverabilityPipelineBehavior<TestRecoverableCommand, string>).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_SuccessfulRequest_ReturnsRight()
    {
        var behavior = CreateBehavior();
        var context = CreateContext();

        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("ok"));

        var result = await behavior.Handle(new TestRecoverableCommand("v"), context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_TransientFailureThenSuccess_Retries()
    {
        var behavior = CreateBehavior(immediateRetries: 3);
        var context = CreateContext();
        var callCount = 0;

        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            if (callCount < 3)
                throw new InvalidOperationException("Transient error");
            return new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right("recovered"));
        };

        var result = await behavior.Handle(new TestRecoverableCommand("v"), context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        callCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_AllRetriesExhausted_ReturnsLeft()
    {
        var behavior = CreateBehavior(immediateRetries: 2);
        var context = CreateContext();

        RequestHandlerCallback<string> next = () =>
            throw new InvalidOperationException("Always fails");

        var result = await behavior.Handle(new TestRecoverableCommand("v"), context, next, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    private static RecoverabilityPipelineBehavior<TestRecoverableCommand, string> CreateBehavior(int immediateRetries = 3)
    {
        var options = new RecoverabilityOptions { ImmediateRetries = immediateRetries };
        var logger = NullLogger<RecoverabilityPipelineBehavior<TestRecoverableCommand, string>>.Instance;
        return new RecoverabilityPipelineBehavior<TestRecoverableCommand, string>(options, logger);
    }

    private static IRequestContext CreateContext()
    {
        var ctx = Substitute.For<IRequestContext>();
        ctx.CorrelationId.Returns("corr-1");
        return ctx;
    }

    public sealed record TestRecoverableCommand(string Value) : ICommand<string>;
}
