using FluentValidation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation.IntegrationTests;

/// <summary>
/// Integration tests for ValidationPipelineBehavior.
/// Tests end-to-end scenarios with DI container and real Encina.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ValidationPipelineBehaviorIntegrationTests
{
    private sealed record TestCommand(string Name, string Email) : ICommand<Guid>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
        }
    }

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, Guid>
    {
        public Task<Either<EncinaError, Guid>> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task Integration_ValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand("John", "john@example.com");

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Integration_InvalidCommand_ShouldReturnValidationError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand("", "invalid-email");

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.ShouldBeOfType<ValidationException>();
                return true;
            });
    }

    [Fact]
    public async Task Integration_MultipleValidators_ShouldAggregateErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<IValidator<TestCommand>, TestCommandValidator>();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand("", "");

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task Integration_NoValidators_ShouldBypassValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand("", "");

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Integration_CancellationToken_ShouldPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        services.AddTransient<IValidator<TestCommand>>(sp =>
        {
            var validator = new InlineValidator<TestCommand>();
            validator.RuleFor(x => x.Name).MustAsync(async (name, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
                return true;
            });
            return validator;
        });

        services.AddTransient<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand("John", "john@example.com");

        // Act
        var result = await Encina.Send(command, cancellationToken: cts.Token);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task Integration_ScopedValidators_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        services.AddScoped<ValidationPipelineBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddScoped<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<ValidationPipelineBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act
        using (var scope1 = provider.CreateScope())
        {
            var Encina1 = scope1.ServiceProvider.GetRequiredService<IEncina>();
            var result1 = await Encina1.Send(new TestCommand("user1", "user1@example.com"));
            result1.ShouldBeSuccess();
        }

        using (var scope2 = provider.CreateScope())
        {
            var Encina2 = scope2.ServiceProvider.GetRequiredService<IEncina>();
            var result2 = await Encina2.Send(new TestCommand("", ""));
            result2.ShouldBeError();
        }
    }
}
