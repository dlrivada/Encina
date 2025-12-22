using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.MiniValidator.IntegrationTests;

/// <summary>
/// Integration tests for MiniValidationBehavior.
/// Tests end-to-end scenarios with DI container and real mediator.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MiniValidationBehaviorIntegrationTests
{
    private sealed record TestCommand : ICommand<Guid>
    {
        [Required]
        [MinLength(3)]
        public string Name { get; init; } = string.Empty;

        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, Guid>
    {
        public Task<Either<MediatorError, Guid>> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<MediatorError, Guid>(Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task Integration_ValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<MiniValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "John", Email = "john@example.com" };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_InvalidCommand_ShouldReturnValidationError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<MiniValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex => ex.ShouldBeOfType<ValidationException>());
                return true;
            });
    }

    [Fact]
    public async Task Integration_MultipleErrors_ShouldAggregateErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<MiniValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "", Email = "" };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsLeft.ShouldBeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex =>
                {
                    var validationResults = (List<ValidationResult>)ex.Data["ValidationResults"]!;
                    validationResults.Count.ShouldBeGreaterThan(1);
                });
                return true;
            });
    }

    [Fact]
    public async Task Integration_NoValidationAttributes_ShouldBypassValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<MiniValidationBehavior<NoValidationCommand, Guid>>();
        services.AddTransient<ICommandHandler<NoValidationCommand, Guid>, NoValidationCommandHandler>();
        services.AddTransient<IPipelineBehavior<NoValidationCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<NoValidationCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new NoValidationCommand { Value = "" };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_ScopedBehavior_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<MiniValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddScoped<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act - Valid in scope 1
        using (var scope1 = provider.CreateScope())
        {
            var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
            var result1 = await mediator1.Send(new TestCommand { Name = "user1", Email = "user1@example.com" });
            result1.IsRight.ShouldBeTrue();
        }

        // Act - Invalid in scope 2
        using (var scope2 = provider.CreateScope())
        {
            var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
            var result2 = await mediator2.Send(new TestCommand { Name = "", Email = "" });
            result2.IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Integration_CancellationToken_ShouldPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<MiniValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<MiniValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "John", Email = "john@example.com" };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await mediator.Send(command, cancellationToken: cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // Helper types
    private sealed record NoValidationCommand : ICommand<Guid>
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class NoValidationCommandHandler : ICommandHandler<NoValidationCommand, Guid>
    {
        public Task<Either<MediatorError, Guid>> Handle(NoValidationCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<MediatorError, Guid>(Guid.NewGuid()));
        }
    }
}
