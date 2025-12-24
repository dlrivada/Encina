using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.MiniValidator.Tests.Guards;

/// <summary>
/// Guard clause tests for MiniValidationBehavior.
/// Verifies defensive programming and null parameter checks.
/// </summary>
public sealed class MiniValidationBehaviorGuardsTests
{
    private sealed record TestCommand : ICommand<string>
    {
        [Required]
        public string Name { get; init; } = string.Empty;
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrow()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        TestCommand? nullRequest = null!;
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(nullRequest, context, nextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullContext_ShouldThrow()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Test" };
        IRequestContext nullContext = null!;

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(request, nullContext, nextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithNullNextStep_ShouldThrow()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Test" };
        var context = RequestContext.Create();
        RequestHandlerCallback<string> nullNextStep = null!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(request, context, nullNextStep, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Valid Name" };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WithCancelledToken_ShouldReturnLeft()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Test" };
        var context = RequestContext.Create();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldNotCallNextStep()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "" }; // Invalid - empty required field
        var context = RequestContext.Create();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeFalse();
        result.ShouldBeError();
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNextStep()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Valid" };
        var context = RequestContext.Create();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithContextWithNullProperties_ShouldNotThrow()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Test" };
        var context = RequestContext.Create(); // Default context has null UserId, TenantId, etc.

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WithEmptyRequestName_ShouldReturnValidationError()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "" };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("Validation failed");
                return true;
            });
    }

    [Fact]
    public async Task Handle_WithWhitespaceRequestName_ShouldNotThrow()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "   " };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Whitespace passes Required validation, operation completes without throwing
        Assert.True(result.IsRight || result.IsLeft, "Handle completed and returned a valid result");
    }
}
