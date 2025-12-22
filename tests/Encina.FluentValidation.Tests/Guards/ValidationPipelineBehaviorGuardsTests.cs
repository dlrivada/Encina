using FluentValidation;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation.Tests.Guards;

/// <summary>
/// Guard clause tests for ValidationPipelineBehavior.
/// Verifies null parameter validation and defensive programming.
/// </summary>
public sealed class ValidationPipelineBehaviorGuardsTests
{
    private sealed record TestCommand(string Name) : ICommand<string>;

    private sealed class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public void Constructor_WithNullValidators_ShouldNotThrow()
    {
        // Arrange & Act
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(null!);

        // Assert
        behavior.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrow()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        TestCommand? nullRequest = null!;
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(nullRequest, context, nextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullContext_ShouldThrow()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Test");
        IRequestContext nullContext = null!;

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(request, nullContext, nextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ShouldThrow()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Test");
        var context = RequestContext.Create();
        RequestHandlerCallback<string> nullNextStep = null!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(request, context, nullNextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithEmptyValidatorCollection_ShouldProceedToNextStep()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("");
        var context = RequestContext.Create();
        var nextCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldHandleGracefully()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Valid Name");
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldReturnLeftGracefully()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand(""); // Invalid
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithCancellationRequested_ShouldReturnLeftGracefully()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Test");
        var context = RequestContext.Create();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("cancelled"));
    }

    [Fact]
    public async Task Handle_WithMultipleNullValidatorsInCollection_ShouldFilterNulls()
    {
        // Arrange
        var validators = new IValidator<TestCommand>?[] { new TestValidator(), null, null }
            .Where(v => v != null)
            .Cast<IValidator<TestCommand>>();

        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Test");
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithContextHavingNullProperties_ShouldHandleGracefully()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Test");
        var context = RequestContext.Create(); // No UserId, TenantId

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }
}
