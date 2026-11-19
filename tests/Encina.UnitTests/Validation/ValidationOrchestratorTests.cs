using Encina.Testing;
using Encina.Validation;
using NSubstitute;
using Shouldly;
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.UnitTests.Validation;

#pragma warning disable CA2012 // NSubstitute Returns setup calls ValueTask-returning method without awaiting
public sealed class ValidationOrchestratorTests
{
    private sealed record TestRequest(string Name);

    [Fact]
    public async Task ValidateAsync_WhenValidationPasses_ReturnsRightUnit()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync<TestRequest>(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ValidationResult>(ValidationResult.Success));

        var orchestrator = new ValidationOrchestrator(provider);
        var request = new TestRequest("valid");
        var context = RequestContext.Create();

        // Act
        var result = await orchestrator.ValidateAsync<TestRequest, string>(request, context, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationFails_ReturnsLeftWithValidationFailedCodeAndCategory()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var validationFailure = ValidationResult.Failure("Name", "Name is required");
        provider.ValidateAsync<TestRequest>(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ValidationResult>(validationFailure));

        var orchestrator = new ValidationOrchestrator(provider);
        var request = new TestRequest("");
        var context = RequestContext.Create();

        // Act
        var result = await orchestrator.ValidateAsync<TestRequest, string>(request, context, CancellationToken.None);

        // Assert
        var error = result.ShouldBeValidationError();
        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.ValidationFailed);
        error.GetCategory().ShouldBe(ErrorCategory.Validation);
        error.Message.ShouldContain("Name is required");
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationFails_SatisfiesShouldBeValidationErrorForProperty()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var validationFailure = ValidationResult.Failure("Name", "Name is required");
        provider.ValidateAsync<TestRequest>(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ValidationResult>(validationFailure));

        var orchestrator = new ValidationOrchestrator(provider);
        var request = new TestRequest("");
        var context = RequestContext.Create();

        // Act
        var result = await orchestrator.ValidateAsync<TestRequest, string>(request, context, CancellationToken.None);

        // Assert
        var error = result.ShouldBeValidationErrorForProperty("Name");
        error.GetCategory().ShouldBe(ErrorCategory.Validation);
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenIsAlreadyCancelled_ReturnsLeftWithoutCallingProvider()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        var orchestrator = new ValidationOrchestrator(provider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var result = await orchestrator.ValidateAsync<TestRequest, string>(new TestRequest("valid"), RequestContext.Create(), cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("Operation was cancelled before validation."));
        await provider.DidNotReceiveWithAnyArgs().ValidateAsync<TestRequest>(default!, default!, default);
    }

    [Fact]
    public async Task ValidateAsync_WhenProviderThrowsOperationCanceledWithCancelledToken_ReturnsLeft()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync<TestRequest>(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<ValidationResult>>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        var orchestrator = new ValidationOrchestrator(provider);

        // Act
        var result = await orchestrator.ValidateAsync<TestRequest, string>(new TestRequest("valid"), RequestContext.Create(), cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.Message.ShouldBe($"Validation cancelled for {nameof(TestRequest)}.");
            error.Exception.IsSome.ShouldBeTrue();
        });
    }
}
