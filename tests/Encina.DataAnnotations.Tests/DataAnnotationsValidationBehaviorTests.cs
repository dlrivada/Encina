using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DataAnnotations.Tests;

public sealed class DataAnnotationsValidationBehaviorTests
{
    private sealed record TestCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; init; } = string.Empty;

        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
        public int Age { get; init; }
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldInvokeNextStep()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John Doe", Email = "john@example.com", Age = 25 };
        var context = RequestContext.Create();
        var nextCalled = false;
        var expectedResponse = "Success";

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>(expectedResponse));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue();
        var value = result.ShouldBeSuccess();
        value.ShouldBe(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldReturnValidationError()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "invalid-email", Age = 15 }; // All invalid
        var context = RequestContext.Create();
        var nextCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeFalse();
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Validation failed");
        error.Message.ShouldContain("TestCommand");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex =>
        {
            ex.ShouldBeOfType<ValidationException>();
            ex.Data["ValidationResults"].ShouldNotBeNull();
            var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
            validationResults.Count.ShouldBeGreaterThanOrEqualTo(3); // Name, Email, Age errors
        });
    }

    [Fact]
    public async Task Handle_WithOnlyNameInvalid_ShouldReturnSingleError()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "valid@example.com", Age = 25 }; // Only name invalid
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex =>
        {
            var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
            validationResults.Count.ShouldBe(1);
            validationResults[0].ErrorMessage.ShouldBe("Name is required");
        });
    }

    [Fact]
    public async Task Handle_WithMinLengthViolation_ShouldReturnError()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Jo", Email = "valid@example.com", Age = 25 }; // Name too short
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Exception.IfSome(ex =>
        {
            var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
            validationResults.ShouldContain(vr => vr.ErrorMessage == "Name must be at least 3 characters");
        });
    }

    [Fact]
    public async Task Handle_WithRequestContextMetadata_ShouldPassContextToValidation()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John", Email = "john@example.com", Age = 25 };
        var correlationId = Guid.NewGuid().ToString();
        var userId = "user-123";
        var tenantId = "tenant-456";
        var context = RequestContext.CreateForTest(userId, tenantId, null, correlationId);

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        // Context enrichment is internal to the behavior, but we verify it doesn't break validation
    }

    [Fact]
    public async Task Handle_WithCustomValidationAttribute_ShouldValidate()
    {
        // Arrange
        var behavior = new DataAnnotationsValidationBehavior<CustomCommand, string>();
        var request = new CustomCommand { Value = "invalid" }; // Will fail custom validation
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Exception.IfSome(ex =>
        {
            var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
            validationResults.ShouldContain(vr => vr.ErrorMessage == "Value must start with 'test-'");
        });
    }

    // Custom validation attribute for testing
    private sealed class StartsWithAttribute : ValidationAttribute
    {
        private readonly string _prefix;

        public StartsWithAttribute(string prefix)
        {
            _prefix = prefix;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string str && str.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? $"Value must start with '{_prefix}'");
        }
    }

    private sealed record CustomCommand : ICommand<string>
    {
        [StartsWith("test-")]
        public string Value { get; init; } = string.Empty;
    }
}
