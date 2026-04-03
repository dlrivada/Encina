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
    public void Constructor_NullOrchestrator_Throws()
    {
        Should.Throw<ArgumentNullException>(
            () => new InboxPipelineBehavior<TestIdempotentCommand, string>(null!));
    }

    [Fact]
    public void HasCorrectGenericConstraint()
    {
        // TRequest must implement IRequest<TResponse>
        var type = typeof(InboxPipelineBehavior<,>);
        var constraints = type.GetGenericArguments()[0].GetGenericParameterConstraints();
        constraints.Length.ShouldBeGreaterThan(0);
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
