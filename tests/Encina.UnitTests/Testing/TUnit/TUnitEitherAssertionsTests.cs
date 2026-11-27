using Encina.Testing.TUnit;
using Encina.Validation;
using LanguageExt;
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.UnitTests.Testing.TUnit;

/// <summary>
/// Unit tests for the category-based error assertions of <see cref="TUnitEitherAssertions"/> (ADR-062):
/// the category is the contract, never a code string or the message.
/// </summary>
public sealed class TUnitEitherAssertionsTests
{
    private sealed record RejectedCommand : ICommand<string>;

    private sealed class RejectingValidationProvider : IValidationProvider
    {
        public ValueTask<ValidationResult> ValidateAsync<TRequest>(
            TRequest request,
            IRequestContext context,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ValidationResult.Failure("Name", "Name is required"));
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_WhenRealValidationPipelineRejects_ReturnsError()
    {
        // Arrange: the real behavior and orchestrator produce the error, not a hand-built one
        var orchestrator = new ValidationOrchestrator(new RejectingValidationProvider());
        var behavior = new ValidationPipelineBehavior<RejectedCommand, string>(orchestrator);

        // Act
        var result = await behavior.Handle(
            new RejectedCommand(),
            RequestContext.Create(),
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("unreachable")),
            CancellationToken.None);

        // Assert
        var error = await TUnitEitherAssertions.ShouldBeValidationErrorAsync(result);
        Assert.Equal(ErrorCategory.Validation, error.GetCategory());
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_WhenValidationCategoryWithPackageSpecificCode_ReturnsError()
    {
        // Arrange: the category is the contract, not the code (ADR-062)
        Either<EncinaError, int> result = EncinaErrors.Create("consent.invalid_purpose", ErrorCategory.Validation, "invalid");

        // Act
        var error = await TUnitEitherAssertions.ShouldBeValidationErrorAsync(result);

        // Assert
        Assert.Equal(ErrorCategory.Validation, error.GetCategory());
    }

    [Fact]
    public async Task ShouldBeNotFoundErrorAsync_WhenNotFoundCategoryWithPackageSpecificCode_ReturnsError()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("saga.not_found", ErrorCategory.NotFound, "missing");

        // Act
        var error = await TUnitEitherAssertions.ShouldBeNotFoundErrorAsync(result);

        // Assert
        Assert.Equal(ErrorCategory.NotFound, error.GetCategory());
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_Task_WhenValidationCategory_ReturnsError()
    {
        // Arrange
        var task = Task.FromResult<Either<EncinaError, int>>(EncinaErrors.Validation("invalid"));

        // Act
        var error = await TUnitEitherAssertions.ShouldBeValidationErrorAsync(task);

        // Assert
        Assert.Equal(ErrorCategory.Validation, error.GetCategory());
    }

    [Fact]
    public async Task ShouldBeNotFoundErrorAsync_Task_WhenNotFoundCategory_ReturnsError()
    {
        // Arrange
        var task = Task.FromResult<Either<EncinaError, int>>(
            EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "missing"));

        // Act
        var error = await TUnitEitherAssertions.ShouldBeNotFoundErrorAsync(task);

        // Assert
        Assert.Equal(ErrorCategory.NotFound, error.GetCategory());
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_WhenOtherCategory_FailsNamingTheCategoryNotTheMessage()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "secret detail");

        // Act
        var exception = await Record.ExceptionAsync(() => TUnitEitherAssertions.ShouldBeValidationErrorAsync(result));

        // Assert
        Assert.NotNull(exception);
        Assert.Contains(nameof(ErrorCategory.NotFound), exception.Message);
        Assert.DoesNotContain("secret detail", exception.Message);
    }

    [Fact]
    public async Task ShouldBeNotFoundErrorAsync_WhenOtherCategory_FailsNamingTheCategoryNotTheMessage()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("orders.invalid", ErrorCategory.Validation, "secret detail");

        // Act
        var exception = await Record.ExceptionAsync(() => TUnitEitherAssertions.ShouldBeNotFoundErrorAsync(result));

        // Assert
        Assert.NotNull(exception);
        Assert.Contains(nameof(ErrorCategory.Validation), exception.Message);
        Assert.DoesNotContain("secret detail", exception.Message);
    }

    [Fact]
    public async Task ShouldBeValidationErrorAsync_WhenOnlyEncinaValidationCodeWithoutCategory_Throws()
    {
        // Arrange: a hand-built error with the legacy code prefix and no category no longer satisfies the assertion
        Either<EncinaError, int> result = EncinaErrors.Create("encina.validation.failed", "legacy");

        // Act
        var exception = await Record.ExceptionAsync(() => TUnitEitherAssertions.ShouldBeValidationErrorAsync(result));

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ShouldBeNotFoundErrorAsync_WhenOnlyEncinaNotFoundCodeWithoutCategory_Throws()
    {
        // Arrange
        Either<EncinaError, int> result = EncinaErrors.Create("encina.notfound.order", "legacy");

        // Act
        var exception = await Record.ExceptionAsync(() => TUnitEitherAssertions.ShouldBeNotFoundErrorAsync(result));

        // Assert
        Assert.NotNull(exception);
    }
}
