using Encina.FluentValidation;
using Encina.Testing.Shouldly;
using Encina.Validation;
using FluentValidation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using EncinaTestFixture = Encina.Testing.EncinaTestFixture;

namespace Encina.UnitTests.FluentValidation;

/// <summary>
/// Tests for the validation pipeline behavior with FluentValidation provider.
/// Tests the complete validation pipeline from request validation through Either result handling.
/// </summary>
public sealed class ValidationPipelineBehaviorTests : IDisposable
{
    #region Test Request Types and Validators

    /// <summary>
    /// Command with validation requirements.
    /// </summary>
    public sealed record CreateUserCommand(string Name, string Email, int Age) : ICommand<UserId>;

    /// <summary>
    /// Simple value object for user ID.
    /// </summary>
    public sealed record UserId(Guid Value);

    /// <summary>
    /// Validator for CreateUserCommand.
    /// </summary>
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(x => x.Age)
                .InclusiveBetween(1, 150).WithMessage("Age must be between 1 and 150.");
        }
    }

    /// <summary>
    /// Handler for CreateUserCommand.
    /// </summary>
    public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserId>
    {
        public int InvocationCount { get; private set; }

        public Task<Either<EncinaError, UserId>> Handle(
            CreateUserCommand request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(
                Either<EncinaError, UserId>.Right(new UserId(Guid.NewGuid())));
        }
    }

    /// <summary>
    /// Command without validation requirements.
    /// </summary>
    public sealed record SimpleCommand(string Value) : ICommand<string>;

    /// <summary>
    /// Handler for SimpleCommand.
    /// </summary>
    public sealed class SimpleCommandHandler : ICommandHandler<SimpleCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(
            SimpleCommand request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Either<EncinaError, string>.Right($"Processed: {request.Value}"));
        }
    }

    #endregion

    private EncinaTestFixture? _fixture;
    private CreateUserCommandHandler? _userHandler;

    /// <summary>
    /// Gets the user handler, throwing if CreateFixture() was not called.
    /// </summary>
    private CreateUserCommandHandler UserHandler =>
        _userHandler ?? throw new InvalidOperationException("CreateFixture must be called before accessing UserHandler");

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    private EncinaTestFixture CreateFixture()
    {
        _userHandler = new CreateUserCommandHandler();

        _fixture = new EncinaTestFixture()
            .WithHandler<SimpleCommandHandler>()
            .ConfigureServices(services =>
            {
                // Register handler instance so we can track invocations without cross-test interference
                // Must register as IRequestHandler<,> because that's what Encina resolves
                services.AddSingleton(_userHandler);
                services.AddSingleton<IRequestHandler<CreateUserCommand, UserId>>(_userHandler);
                services.AddEncinaFluentValidation(typeof(CreateUserCommandValidator).Assembly);
            });

        return _fixture;
    }

    #region Successful Validation Tests

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("John Doe", "john@example.com", 30);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using EitherShouldlyExtensions
        var result = context.Result.ShouldBeSuccess();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldInvokeHandler()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("Jane Doe", "jane@example.com", 25);

        // Act
        await fixture.SendAsync(command);

        // Assert
        UserHandler.InvocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldBeRight()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("Test User", "test@example.com", 40);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using ShouldBeRight alias
        var userId = context.Result.ShouldBeRight();
        userId.ShouldNotBeNull();
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldReturnError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "invalid-email", 0);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using ShouldBeError
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Validation failed");
    }

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldNotInvokeHandler()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "invalid", -5);

        // Act
        await fixture.SendAsync(command);

        // Assert - Handler should not be invoked on validation failure
        UserHandler.InvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldBeLeft()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "", 0);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using ShouldBeLeft alias
        context.Result.ShouldBeLeft();
    }

    [Fact]
    public async Task Pipeline_WithEmptyName_ShouldContainNameError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "valid@email.com", 25);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Name");
    }

    [Fact]
    public async Task Pipeline_WithInvalidEmail_ShouldContainEmailError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("John", "not-an-email", 25);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public async Task Pipeline_WithInvalidAge_ShouldContainAgeError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("John", "john@email.com", 200);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Age");
    }

    [Fact]
    public async Task Pipeline_WithMultipleInvalidFields_ShouldContainAllErrors()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "invalid", -10);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Name");
        error.Message.ShouldContain("Email");
        error.Message.ShouldContain("Age");
    }

    #endregion

    #region Value Extraction Pattern Tests

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldExtractValueForFurtherAssertions()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("Extract Test", "extract@test.com", 35);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Value extraction pattern
        var userId = context.Result.ShouldBeSuccess();
        userId.Value.ShouldNotBe(Guid.Empty);
        userId.ShouldBeOfType<UserId>();
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldUseValidatorCallback()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("Callback Test", "callback@test.com", 28);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using validator callback
        context.Result.ShouldBeSuccess(userId =>
        {
            userId.ShouldNotBeNull();
            userId.Value.ShouldNotBe(Guid.Empty);
        });
    }

    #endregion

    #region Error Metadata Tests

    [Fact]
    public async Task Pipeline_WithFieldLevelValidationError_ShouldIncludePropertyNameInMessage()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("ValidName", "invalid-email", 25);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Error message should include field name
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Email:");
    }

    [Fact]
    public async Task Pipeline_ValidationError_ShouldIncludeRequestTypeName()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "", 0);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("CreateUserCommand");
    }

    [Fact]
    public async Task Pipeline_ValidationError_ShouldIncludeErrorCount()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateUserCommand("", "invalid", 0);

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("error(s)");
    }

    #endregion

    #region Request Without Validator Tests

    [Fact]
    public async Task Pipeline_WithRequestWithoutValidator_ShouldPassThrough()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new SimpleCommand("test value");

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Should succeed even without validator
        var result = context.Result.ShouldBeSuccess();
        result.ShouldBe("Processed: test value");
    }

    #endregion
}
