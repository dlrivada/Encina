using System.ComponentModel.DataAnnotations;
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
        behavior.ShouldBeOfType<MiniValidationBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddMiniValidation_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddMiniValidation());
    }

    [Fact]
    public void AddMiniValidation_CalledMultipleTimes_ShouldNotDuplicateBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMiniValidation();
        services.AddMiniValidation();

        // Assert
        var provider = services.BuildServiceProvider();
        var behaviors = provider.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();

        // TryAddTransient should prevent duplicates
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
}
