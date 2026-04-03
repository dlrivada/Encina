using Encina.Messaging.Inbox;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Messaging;

/// <summary>
/// Behavioral contract tests for <see cref="InboxPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InboxPipelineBehaviorContractTests
{
    [Fact]
    public void ImplementsIPipelineBehavior()
    {
        typeof(IPipelineBehavior<TestIdempotentCommand, string>).IsAssignableFrom(
            typeof(InboxPipelineBehavior<TestIdempotentCommand, string>)).ShouldBeTrue();
    }

    [Fact]
    public void IsSealed()
    {
        typeof(InboxPipelineBehavior<TestIdempotentCommand, string>).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NonIdempotentRequest_SkipsInbox()
    {
        // Arrange — use mock orchestrator since we can't easily construct a real one
        var orchestrator = Substitute.For<InboxOrchestrator>();
        var behavior = new InboxPipelineBehavior<TestNonIdempotentCommand, string>(orchestrator);
        var request = new TestNonIdempotentCommand("value");
        var context = CreateContext();
        var called = false;

        RequestHandlerCallback<string> next = () =>
        {
            called = true;
            return new ValueTask<Either<EncinaError, string>>(
                Either<EncinaError, string>.Right("result"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        called.ShouldBeTrue("Non-idempotent requests should skip inbox and call next directly");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_NullOrchestrator_Throws()
    {
        Should.Throw<ArgumentNullException>(
            () => new InboxPipelineBehavior<TestIdempotentCommand, string>(null!));
    }

    private static IRequestContext CreateContext(string? idempotencyKey = "test-key")
    {
        var ctx = Substitute.For<IRequestContext>();
        ctx.CorrelationId.Returns("corr-1");
        ctx.IdempotencyKey.Returns(idempotencyKey);
        ctx.UserId.Returns("user-1");
        ctx.TenantId.Returns((string?)null);
        ctx.Timestamp.Returns(DateTimeOffset.UtcNow);
        return ctx;
    }

    public sealed record TestIdempotentCommand(string Value) : ICommand<string>, IIdempotentRequest;
    public sealed record TestNonIdempotentCommand(string Value) : ICommand<string>;
}
