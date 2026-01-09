using System.ComponentModel.DataAnnotations;
using Encina.Validation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.MiniValidator.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed record TestCommand : ICommand<string>
    {
        [Required] public string Name { get; init; } = string.Empty;
    }

    [Fact]
    public void AddMiniValidation_ShouldRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMiniValidation();

        // Assert
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetService<IPipelineBehavior<TestCommand, string>>();
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<ValidationPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddMiniValidation_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(services.AddMiniValidation);
    }

    [Fact]
    public void AddMiniValidation_CalledMultipleTimes_ShouldNotDuplicateBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Call multiple times
        services.AddMiniValidation();
        services.AddMiniValidation();
        services.AddMiniValidation();

        // Assert - TryAddTransient prevents duplicate behavior registration
        var provider = services.BuildServiceProvider();
        var behaviors = provider.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();
        behaviors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddMiniValidation_ShouldResolveAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMiniValidation();

        // Act
        var provider = services.BuildServiceProvider();
        var behavior1 = provider.GetService<IPipelineBehavior<TestCommand, string>>();
        var behavior2 = provider.GetService<IPipelineBehavior<TestCommand, string>>();

        // Assert
        behavior1.ShouldNotBeNull();
        behavior2.ShouldNotBeNull();
        behavior1.ShouldNotBeSameAs(behavior2); // Transient = new instance each time
    }

    [Fact]
    public void AddMiniValidation_ShouldRegisterValidationOrchestrator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMiniValidation();

        // Assert
        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetService<ValidationOrchestrator>();
        orchestrator.ShouldNotBeNull();
    }

    [Fact]
    public void AddMiniValidation_ShouldRegisterMiniValidationProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMiniValidation();

        // Assert
        var provider = services.BuildServiceProvider();
        var validationProvider = provider.GetService<IValidationProvider>();
        validationProvider.ShouldNotBeNull();
        validationProvider.ShouldBeOfType<MiniValidationProvider>();
    }

    [Fact]
    public void AddMiniValidation_MultipleCallsWithDifferentScopes_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMiniValidation();

        // Assert
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var behavior1 = scope1.ServiceProvider.GetService<IPipelineBehavior<TestCommand, string>>();
        var behavior2 = scope2.ServiceProvider.GetService<IPipelineBehavior<TestCommand, string>>();

        // Transient means different instances in different scopes
        behavior1.ShouldNotBeNull();
        behavior2.ShouldNotBeNull();
        behavior1.ShouldNotBeSameAs(behavior2);
    }
}
