using Encina.Testing.Verify;
using Encina.Validation;
using NSubstitute;
using Shouldly;
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.UnitTests.Testing.Verify;

/// <summary>
/// Tests that <see cref="EncinaVerify.PrepareValidationError"/> classifies an error by its
/// <see cref="ErrorCategory"/> and never by a code string (#1922).
/// </summary>
#pragma warning disable CA2012 // NSubstitute Returns setup calls ValueTask-returning method without awaiting
public sealed class EncinaVerifyValidationErrorTests
{
    private sealed record TestRequest(string Name);

    [Fact]
    public async Task PrepareValidationError_ErrorFromRealValidationOrchestrator_IsValidationError()
    {
        // Arrange
        var provider = Substitute.For<IValidationProvider>();
        provider.ValidateAsync<TestRequest>(Arg.Any<TestRequest>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ValidationResult>(ValidationResult.Failure("Name", "Name is required")));
        var orchestrator = new ValidationOrchestrator(provider);
        var result = await orchestrator.ValidateAsync<TestRequest, string>(
            new TestRequest(""), RequestContext.Create(), CancellationToken.None);
        var error = result.Match(Right: _ => throw new InvalidOperationException("Expected a validation failure."), Left: e => e);

        // Act
        var snapshot = (ValidationErrorSnapshot)EncinaVerify.PrepareValidationError(error);

        // Assert
        snapshot.IsValidationError.ShouldBeTrue();
    }

    [Fact]
    public void PrepareValidationError_ValidationCategoryWithPackageSpecificCode_IsValidationError()
    {
        var error = EncinaErrors.Create("orders.invalid_id", ErrorCategory.Validation, "Order id is invalid.");

        var snapshot = (ValidationErrorSnapshot)EncinaVerify.PrepareValidationError(error);

        snapshot.IsValidationError.ShouldBeTrue();
        snapshot.Code.ShouldBe("orders.invalid_id");
    }

    [Fact]
    public void PrepareValidationError_NotFoundCategoryWithPackageSpecificCode_IsNotValidationError()
    {
        var error = EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "Order was not found.");

        var snapshot = (ValidationErrorSnapshot)EncinaVerify.PrepareValidationError(error);

        snapshot.IsValidationError.ShouldBeFalse();
    }

    [Fact]
    public void PrepareValidationError_HandBuiltErrorWithOnlyLegacyValidationCode_IsNotValidationError()
    {
        // The legacy "encina.validation.*" prefix no longer classifies an error: only its category does.
        var error = EncinaErrors.Create("encina.validation.legacy", "Looks like validation by code only.");

        var snapshot = (ValidationErrorSnapshot)EncinaVerify.PrepareValidationError(error);

        snapshot.IsValidationError.ShouldBeFalse();
    }
}
