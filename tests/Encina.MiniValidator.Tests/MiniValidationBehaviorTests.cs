using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.MiniValidator.Tests;

public sealed class MiniValidationBehaviorTests
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
        var behavior = new MiniValidationBehavior<TestCommand, string>();
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
        var behavior = new MiniValidationBehavior<TestCommand, string>();
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
        error.Message.ShouldContain("error(s)");
    }

    [Fact]
    public async Task Handle_WithOnlyNameInvalid_ShouldReturnSingleError()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "valid@example.com", Age = 25 }; // Only name invalid
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Name");
        error.Message.ShouldContain("required");
    }

    [Fact]
    public async Task Handle_WithMinLengthViolation_ShouldReturnError()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Jo", Email = "valid@example.com", Age = 25 }; // Name too short
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Name");
        error.Message.ShouldContain("at least 3 characters");
    }

    [Fact]
    public async Task Handle_WithEmailValidation_ShouldValidateEmailFormat()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John", Email = "not-an-email", Age = 25 };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public async Task Handle_WithRangeValidation_ShouldValidateRange()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John", Email = "john@example.com", Age = 150 }; // Age out of range
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Age");
    }
}
