using Encina.OpenTelemetry.Behaviors;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Contract tests for <see cref="MessagingEnricherPipelineBehavior{TRequest, TResponse}"/>
/// verifying it correctly implements the <see cref="IPipelineBehavior{TRequest, TResponse}"/> contract.
/// </summary>
public sealed class MessagingEnricherPipelineBehaviorContractTests
{
    private sealed record TestRequest(string Data) : IRequest<string>;

    [Fact]
    public void ImplementsIPipelineBehavior()
    {
        // The behavior must implement the generic IPipelineBehavior interface
        typeof(MessagingEnricherPipelineBehavior<TestRequest, string>)
            .GetInterfaces()
            .ShouldContain(typeof(IPipelineBehavior<TestRequest, string>));
    }

    [Fact]
    public async Task Handle_ReturnsNextStepResult_WhenNoMessagingContext()
    {
        // Arrange
        var behavior = new MessagingEnricherPipelineBehavior<TestRequest, string>();
        var context = Substitute.For<IRequestContext>();
        context.Metadata.Returns(new Dictionary<string, object?>());

        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>("contract-ok");

        // Act
        var result = await behavior.Handle(new TestRequest("test"), context, next, CancellationToken.None);

        // Assert - contract: behavior must forward the next step result unchanged
        result.ShouldBeSuccess().ShouldBe("contract-ok");
    }
}
