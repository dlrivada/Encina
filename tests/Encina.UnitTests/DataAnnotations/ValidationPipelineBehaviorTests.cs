using System.ComponentModel.DataAnnotations;
using Encina.DataAnnotations;
using Encina.Testing.Shouldly;
using Encina.Validation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using EncinaTestFixture = Encina.Testing.EncinaTestFixture;

namespace Encina.UnitTests.DataAnnotations;

/// <summary>
/// Tests for the validation pipeline behavior with DataAnnotations provider.
/// Tests the complete validation pipeline from request validation through Either result handling.
/// </summary>
public sealed class ValidationPipelineBehaviorTests : IDisposable
{
    #region Test Request Types

    /// <summary>
    /// Command with DataAnnotations validation attributes.
    /// </summary>
    public sealed record RegisterUserCommand : ICommand<RegistrationResult>
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        public string Email { get; init; } = string.Empty;

        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
        public int Age { get; init; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; init; } = string.Empty;
    }

    /// <summary>
    /// Result type for registration.
    /// </summary>
    public sealed record RegistrationResult(Guid UserId, string Username);

    /// <summary>
    /// Handler for RegisterUserCommand.
    /// </summary>
    public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegistrationResult>
    {
        public int InvocationCount { get; private set; }

        public Task<Either<EncinaError, RegistrationResult>> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(
                Either<EncinaError, RegistrationResult>.Right(
                    new RegistrationResult(Guid.NewGuid(), request.Username)));
        }
    }

    /// <summary>
    /// Command without validation attributes.
    /// </summary>
    public sealed record PingCommand(string Message) : ICommand<string>;

    /// <summary>
    /// Handler for PingCommand.
    /// </summary>
    public sealed class PingCommandHandler : ICommandHandler<PingCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(
            PingCommand request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Either<EncinaError, string>.Right($"Pong: {request.Message}"));
        }
    }

    #endregion

    private EncinaTestFixture? _fixture;
    private RegisterUserCommandHandler? _registerHandler;

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    private EncinaTestFixture CreateFixture()
    {
        // Dispose any existing fixture to prevent resource leaks
        if (_fixture is not null)
        {
            _fixture.Dispose();
            _fixture = null;
        }

        _registerHandler = new RegisterUserCommandHandler();

        _fixture = new EncinaTestFixture()
            .WithHandler<PingCommandHandler>()
            .ConfigureServices(services =>
            {
                // Register handler instance so we can track invocations without cross-test interference
                // Must register as IRequestHandler<,> because that's what Encina resolves
                services.AddSingleton(_registerHandler);
                services.AddSingleton<IRequestHandler<RegisterUserCommand, RegistrationResult>>(_registerHandler);
                services.AddDataAnnotationsValidation();
            });

        return _fixture;
    }

    #region Successful Validation Tests

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "johndoe",
            Email = "john@example.com",
            Age = 30,
            Password = "securepassword123"
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using EitherShouldlyExtensions
        var result = context.Result.ShouldBeSuccess();
        result.UserId.ShouldNotBe(Guid.Empty);
        result.Username.ShouldBe("johndoe");
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldInvokeHandler()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "janedoe",
            Email = "jane@example.com",
            Age = 25,
            Password = "password123"
        };

        // Act
        await fixture.SendAsync(command);

        // Assert
        _registerHandler!.InvocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ValueExtractionPattern()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "testuser",
            Email = "test@test.com",
            Age = 40,
            Password = "mypassword123"
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Value extraction pattern: var value = result.ShouldBeSuccess(); value.ShouldBe(expected);
        var registration = context.Result.ShouldBeSuccess();
        registration.Username.ShouldBe("testuser");
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldReturnError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "",
            Email = "invalid",
            Age = 10,
            Password = "short"
        };

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
        var command = new RegisterUserCommand
        {
            Username = "ab",
            Email = "not-an-email",
            Age = 0,
            Password = ""
        };

        // Act
        await fixture.SendAsync(command);

        // Assert - Handler should not be invoked on validation failure
        _registerHandler!.InvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task Pipeline_WithEmptyRequiredFields_ShouldReturnError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "",
            Email = "",
            Age = 25,
            Password = ""
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeLeft();
        error.Message.ShouldContain("required");
    }

    [Fact]
    public async Task Pipeline_WithInvalidEmail_ShouldContainEmailError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "validuser",
            Email = "invalid-email",
            Age = 25,
            Password = "validpassword123"
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public async Task Pipeline_WithAgeOutOfRange_ShouldContainAgeError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "validuser",
            Email = "valid@email.com",
            Age = 15, // Below minimum of 18
            Password = "validpassword123"
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Age");
    }

    [Fact]
    public async Task Pipeline_WithShortPassword_ShouldContainPasswordError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "validuser",
            Email = "valid@email.com",
            Age = 25,
            Password = "short" // Less than 8 characters
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Password");
    }

    [Fact]
    public async Task Pipeline_WithUsernameOutOfRange_ShouldContainUsernameError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "ab", // Less than 3 characters
            Email = "valid@email.com",
            Age = 25,
            Password = "validpassword123"
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Username");
    }

    #endregion

    #region DataAnnotations Attribute Mapping Tests

    [Fact]
    public async Task Pipeline_DataAnnotationsErrors_ShouldMapToEitherEncinaError()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "",
            Email = "",
            Age = 0,
            Password = ""
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Verify Either<EncinaError, TResponse> mapping
        context.Result.IsLeft.ShouldBeTrue();
        var error = context.Result.ShouldBeError();
        error.ShouldBeOfType<EncinaError>();
    }

    [Fact]
    public async Task Pipeline_ValidationError_ShouldIncludeRequestTypeName()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "",
            Email = "",
            Age = 0,
            Password = ""
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("RegisterUserCommand");
    }

    #endregion

    #region Pipeline Stops on Failure Tests

    [Fact]
    public async Task Pipeline_OnValidationFailure_ShouldStopExecution()
    {
        // Arrange
        var fixture = CreateFixture();

        // Invalid request
        var invalidCommand = new RegisterUserCommand
        {
            Username = "",
            Email = "invalid",
            Age = 0,
            Password = ""
        };
        await fixture.SendAsync(invalidCommand);

        // Assert - Handler should not be invoked
        _registerHandler!.InvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task Pipeline_ValidAfterInvalid_ShouldProcessValidRequest()
    {
        // Arrange
        var fixture = CreateFixture();

        // First - invalid
        var invalidCommand = new RegisterUserCommand
        {
            Username = "",
            Email = "",
            Age = 0,
            Password = ""
        };
        await fixture.SendAsync(invalidCommand);

        // Second - valid
        var validCommand = new RegisterUserCommand
        {
            Username = "validuser",
            Email = "valid@email.com",
            Age = 25,
            Password = "validpassword123"
        };
        await fixture.SendAsync(validCommand);

        // Assert
        _registerHandler!.InvocationCount.ShouldBe(1);
    }

    #endregion

    #region Request Without Validation Tests

    [Fact]
    public async Task Pipeline_WithRequestWithoutValidation_ShouldPassThrough()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new PingCommand("Hello");

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Should succeed even without validation attributes
        var result = context.Result.ShouldBeSuccess();
        result.ShouldBe("Pong: Hello");
    }

    #endregion

    #region Custom Validator Callback Tests

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldSupportCustomValidatorCallback()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new RegisterUserCommand
        {
            Username = "callbacktest",
            Email = "callback@test.com",
            Age = 30,
            Password = "securepass123"
        };

        // Act
        var context = await fixture.SendAsync(command);

        // Assert - Using validator callback
        context.Result.ShouldBeSuccess(result =>
        {
            result.ShouldNotBeNull();
            result.UserId.ShouldNotBe(Guid.Empty);
            result.Username.ShouldBe("callbacktest");
        });
    }

    #endregion
}
