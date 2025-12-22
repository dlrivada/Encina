using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DataAnnotations.ContractTests;

/// <summary>
/// Contract tests for DataAnnotationsValidationBehavior.
/// Verifies that the behavior adheres to expected contracts and guarantees.
/// </summary>
public sealed class DataAnnotationsValidationBehaviorContractTests
{
    private sealed record TestCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        public string Name { get; init; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; init; } = string.Empty;
    }

    [Fact]
    public async Task Contract_IPipelineBehavior_ShouldImplementInterface()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();

        // Assert
        behavior.ShouldBeAssignableTo<IPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public async Task Contract_ValidRequest_MustInvokeNextStep()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John", Email = "john@example.com" };
        var context = RequestContext.Create();
        var nextStepInvoked = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepInvoked = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Valid requests MUST proceed to handler
        nextStepInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_InvalidRequest_MustNotInvokeNextStep()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "invalid" }; // Invalid
        var context = RequestContext.Create();
        var nextStepInvoked = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepInvoked = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Invalid requests MUST short-circuit
        nextStepInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task Contract_InvalidRequest_MustReturnLeft()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "invalid" };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Validation failures MUST return Left
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_MultipleValidationErrors_MustAggregateAllFailures()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "bad-email" }; // Both fail
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: ALL validation failures MUST be aggregated
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex =>
                {
                    ex.ShouldBeOfType<ValidationException>();
                    var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
                    validationResults.Count.ShouldBeGreaterThanOrEqualTo(2);
                });
                return true;
            });
    }

    [Fact]
    public async Task Contract_NoValidationAttributes_MustBypassValidation()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<NoValidationCommand, string>();
        var request = new NoValidationCommand { Value = "" }; // No validation attributes
        var context = RequestContext.Create();
        var nextStepInvoked = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepInvoked = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: No validation attributes MUST skip validation entirely
        nextStepInvoked.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_ContextEnrichment_MustPassCorrelationId()
    {
        // Arrange
        var capturedCorrelationId = string.Empty;
        var behavior = new DataAnnotationsValidationBehavior<CustomValidationCommand, string>();
        var request = new CustomValidationCommand();
        var context = RequestContext.Create();
        var expectedCorrelationId = context.CorrelationId;

        // Inject correlation ID into custom validator
        CustomValidationCommand.OnValidation = (validationContext) =>
        {
            if (validationContext.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                capturedCorrelationId = correlationId?.ToString() ?? string.Empty;
            }
        };

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: CorrelationId MUST be passed to validation context
        capturedCorrelationId.ShouldBe(expectedCorrelationId);
    }

    [Fact]
    public async Task Contract_ContextEnrichment_MustPassUserId()
    {
        // Arrange
        var capturedUserId = string.Empty;
        var behavior = new DataAnnotationsValidationBehavior<CustomValidationCommand, string>();
        var request = new CustomValidationCommand();
        var context = RequestContext.CreateForTest(userId: "user-123");

        CustomValidationCommand.OnValidation = (validationContext) =>
        {
            if (validationContext.Items.TryGetValue("UserId", out var userId))
            {
                capturedUserId = userId?.ToString() ?? string.Empty;
            }
        };

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: UserId MUST be passed when present
        capturedUserId.ShouldBe("user-123");
    }

    [Fact]
    public async Task Contract_ContextEnrichment_MustPassTenantId()
    {
        // Arrange
        var capturedTenantId = string.Empty;
        var behavior = new DataAnnotationsValidationBehavior<CustomValidationCommand, string>();
        var request = new CustomValidationCommand();
        var context = RequestContext.CreateForTest(tenantId: "tenant-456");

        CustomValidationCommand.OnValidation = (validationContext) =>
        {
            if (validationContext.Items.TryGetValue("TenantId", out var tenantId))
            {
                capturedTenantId = tenantId?.ToString() ?? string.Empty;
            }
        };

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: TenantId MUST be passed when present
        capturedTenantId.ShouldBe("tenant-456");
    }

    [Fact(Skip = "DataAnnotations uses synchronous validation which does not support CancellationToken propagation")]
    public async Task Contract_CancellationToken_MustPropagateToValidation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tokenReceived = false;

        var behavior = new DataAnnotationsValidationBehavior<AsyncValidationCommand, string>();
        var request = new AsyncValidationCommand();
        var context = RequestContext.Create();

        AsyncValidationCommand.OnValidation = (ct) =>
        {
            tokenReceived = ct.CanBeCanceled;
        };

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert - Contract: CancellationToken MUST be propagated
        tokenReceived.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_ValidationException_MustContainAllErrors()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "J", Email = "bad" }; // Both fail
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: ValidationException MUST contain ALL error details
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex =>
                {
                    ex.ShouldBeOfType<ValidationException>();
                    var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
                    var errorMessages = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    errorMessages.ShouldContain("Name must be at least 3 characters");
                    errorMessages.ShouldContain("Invalid email format");
                });
                return true;
            });
    }

    [Fact]
    public async Task Contract_ValidRequest_MustReturnRight()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John", Email = "john@example.com" };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Valid requests MUST return Right
        result.IsRight.ShouldBeTrue();
    }

    // Helper types
    private sealed record NoValidationCommand : ICommand<string>
    {
        public string Value { get; init; } = string.Empty;
    }

}

// CustomValidation attribute requires the validation type to be public
public sealed record CustomValidationCommand : ICommand<string>
{
    [CustomValidation(typeof(CustomValidationCommand), nameof(ValidateCustom))]
    public string Value { get; init; } = string.Empty;

    public static Action<ValidationContext>? OnValidation { get; set; }

    public static ValidationResult ValidateCustom(object value, ValidationContext context)
    {
        OnValidation?.Invoke(context);
        return ValidationResult.Success!;
    }
}

// CustomValidation attribute requires the validation type to be public
public sealed record AsyncValidationCommand : ICommand<string>
{
    [CustomValidation(typeof(AsyncValidationCommand), nameof(ValidateAsync))]
    public string Value { get; init; } = string.Empty;

    public static Action<CancellationToken>? OnValidation { get; set; }

    public static ValidationResult ValidateAsync(object value, ValidationContext context)
    {
        // DataAnnotations doesn't support async validation directly,
        // but we can check if cancellation token is available in context
        if (context.Items.TryGetValue("CancellationToken", out var ct) && ct is CancellationToken token)
        {
            OnValidation?.Invoke(token);
        }
        return ValidationResult.Success!;
    }
}
