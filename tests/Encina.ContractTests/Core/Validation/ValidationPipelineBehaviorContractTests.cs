using Encina.Validation;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Core.Validation;

/// <summary>
/// Behavioral contract tests for <see cref="ValidationPipelineBehavior{TRequest, TResponse}"/> that
/// execute real code paths: validation pass-through, validation failure short-circuit,
/// and null argument guards.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ValidationPipeline")]
#pragma warning disable CA2012 // NSubstitute setup calls ValueTask-returning methods without awaiting (expected pattern)
public sealed class ValidationPipelineBehaviorContractTests
{
    // -- Test request types --

    private sealed record TestCommand(string Value) : ICommand<string>;

    // -- Tests --

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNextStep()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync<TestCommand>(Arg.Any<TestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<ValidationResult>(ValidationResult.Success));

        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(orchestrator);
        var command = new TestCommand("valid");
        var context = RequestContext.Create();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v.ShouldBe("success"), Left: _ => throw new InvalidOperationException("Expected Right"));
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldShortCircuitWithLeft()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync<TestCommand>(Arg.Any<TestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<ValidationResult>(ValidationResult.Failure("Value", "Value is required")));

        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(orchestrator);
        var command = new TestCommand("");
        var context = RequestContext.Create();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("should not reach"));
        };

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        nextCalled.ShouldBeFalse();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("Validation failed"));
    }

    [Fact]
    public async Task Handle_WhenValidationFailsWithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var errors = new[]
        {
            new ValidationError("Name", "Name is required"),
            new ValidationError("Email", "Email is invalid"),
        };
        provider.ValidateAsync<TestCommand>(Arg.Any<TestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<ValidationResult>(ValidationResult.Failure(errors)));

        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(orchestrator);
        var command = new TestCommand("");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("no"));

        // Act
        var result = await behavior.Handle(command, context, next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e =>
            {
                e.Message.ShouldContain("2 error(s)");
                e.Message.ShouldContain("Name is required");
                e.Message.ShouldContain("Email is invalid");
            });
    }

    [Fact]
    public void Constructor_WithNullOrchestrator_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ValidationPipelineBehavior<TestCommand, string>(null!));
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrow()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(orchestrator);
        var context = RequestContext.Create();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("no"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(null!, context, next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullContext_ShouldThrow()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(orchestrator);
        var command = new TestCommand("test");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("no"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(command, null!, next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ShouldThrow()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var orchestrator = new ValidationOrchestrator(provider);
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(orchestrator);
        var command = new TestCommand("test");
        var context = RequestContext.Create();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(command, context, null!, CancellationToken.None));
    }
}
#pragma warning restore CA2012
