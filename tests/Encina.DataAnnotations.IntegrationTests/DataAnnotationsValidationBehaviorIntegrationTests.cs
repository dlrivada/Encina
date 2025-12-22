using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DataAnnotations.IntegrationTests;

/// <summary>
/// Integration tests for DataAnnotationsValidationBehavior.
/// Tests end-to-end scenarios with DI container and real Encina.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DataAnnotationsValidationBehaviorIntegrationTests
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
        services.AddTransient<DataAnnotationsValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand { Name = "John", Email = "john@example.com" };

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_InvalidCommand_ShouldReturnValidationError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<DataAnnotationsValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var result = await Encina.Send(command);

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
        services.AddTransient<DataAnnotationsValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand { Name = "", Email = "" };

        // Act
        var result = await Encina.Send(command);

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
        services.AddTransient<DataAnnotationsValidationBehavior<NoValidationCommand, Guid>>();
        services.AddTransient<ICommandHandler<NoValidationCommand, Guid>, NoValidationCommandHandler>();
        services.AddTransient<IPipelineBehavior<NoValidationCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<NoValidationCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new NoValidationCommand { Value = "" };

        // Act
        var result = await Encina.Send(command);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_ScopedBehavior_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<DataAnnotationsValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddScoped<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();

        // Act - Valid in scope 1
        using (var scope1 = provider.CreateScope())
        {
            var Encina1 = scope1.ServiceProvider.GetRequiredService<IEncina>();
            var result1 = await Encina1.Send(new TestCommand { Name = "user1", Email = "user1@example.com" });
            result1.IsRight.ShouldBeTrue();
        }

        // Act - Invalid in scope 2
        using (var scope2 = provider.CreateScope())
        {
            var Encina2 = scope2.ServiceProvider.GetRequiredService<IEncina>();
            var result2 = await Encina2.Send(new TestCommand { Name = "", Email = "" });
            result2.IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Integration_CancellationToken_ShouldPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<DataAnnotationsValidationBehavior<TestCommand, Guid>>();
        services.AddTransient<ICommandHandler<TestCommand, Guid>, TestCommandHandler>();
        services.AddTransient<IPipelineBehavior<TestCommand, Guid>>(sp =>
            sp.GetRequiredService<DataAnnotationsValidationBehavior<TestCommand, Guid>>());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var command = new TestCommand { Name = "John", Email = "john@example.com" };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await Encina.Send(command, cancellationToken: cts.Token);

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
        public Task<Either<EncinaError, Guid>> Handle(NoValidationCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, Guid>(Guid.NewGuid()));
        }
    }
}
