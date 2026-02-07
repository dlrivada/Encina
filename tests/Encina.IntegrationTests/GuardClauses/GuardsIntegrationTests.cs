using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.GuardClauses;

/// <summary>
/// Integration tests for Guards with Encina.
/// Tests end-to-end scenarios with DI container and real Encina.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GuardsIntegrationTests
{
    private sealed record ValidateUserCommand(string? Email, string? Password, int Age) : ICommand<Guid>;

    private sealed class ValidateUserHandler : ICommandHandler<ValidateUserCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(ValidateUserCommand request, CancellationToken cancellationToken)
        {
            // Use Guards for state validation
            if (!Guards.TryValidateNotEmpty(request.Email, nameof(request.Email), out var emailError))
                return Task.FromResult(Left<EncinaError, Guid>(emailError));

            if (!Guards.TryValidateEmail(request.Email, nameof(request.Email), out var emailFormatError))
                return Task.FromResult(Left<EncinaError, Guid>(emailFormatError));

            if (!Guards.TryValidateNotEmpty(request.Password, nameof(request.Password), out var pwdError))
                return Task.FromResult(Left<EncinaError, Guid>(pwdError));

            if (!Guards.TryValidateInRange(request.Age, nameof(request.Age), 18, 120, out var ageError))
                return Task.FromResult(Left<EncinaError, Guid>(ageError));

            return Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task Integration_GuardsInHandler_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<ValidateUserCommand, Guid>, ValidateUserHandler>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new ValidateUserCommand("user@example.com", "SecurePassword123", 25);

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Integration_GuardsInHandler_InvalidEmail_ShouldReturnError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<ValidateUserCommand, Guid>, ValidateUserHandler>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new ValidateUserCommand("", "SecurePassword123", 25);

        // Act
        var result = await Encina.Send(command);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public async Task Integration_GuardsInHandler_InvalidAge_ShouldReturnError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<ValidateUserCommand, Guid>, ValidateUserHandler>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new ValidateUserCommand("user@example.com", "SecurePassword123", 15); // Age < 18

        // Act
        var result = await Encina.Send(command);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("must be between 18 and 120");
    }
}
