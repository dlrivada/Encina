using System.ComponentModel.DataAnnotations;
using Encina.DataAnnotations;
using Encina.Validation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.DataAnnotations;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed record TestCommand : ICommand<string>
    {
        [Required] public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Creates a configured ServiceProvider with DataAnnotations validation registered.
    /// </summary>
    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddDataAnnotationsValidation();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddDataAnnotationsValidation_ShouldRegisterValidationBehavior()
    {
        // Arrange & Act
        using var provider = CreateProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<TestCommand, string>>();

        // Assert
        behavior.ShouldBeOfType<ValidationPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddDataAnnotationsValidation_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(services.AddDataAnnotationsValidation);
    }

    [Fact]
    public void AddDataAnnotationsValidation_CalledMultipleTimes_ShouldNotDuplicateBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Call multiple times
        services.AddDataAnnotationsValidation();
        services.AddDataAnnotationsValidation();
        services.AddDataAnnotationsValidation();

        // Assert - Duplicate behavior registration is prevented
        using var provider = services.BuildServiceProvider();
        var behaviors = provider.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();
        behaviors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddDataAnnotationsValidation_ShouldResolveAsTransient()
    {
        // Arrange & Act
        using var provider = CreateProvider();
        var behavior1 = provider.GetRequiredService<IPipelineBehavior<TestCommand, string>>();
        var behavior2 = provider.GetRequiredService<IPipelineBehavior<TestCommand, string>>();

        // Assert
        behavior1.ShouldNotBeSameAs(behavior2); // Transient = new instance each time
    }

    #region ValidationPipelineBehavior with Either Type Verification

    [Fact]
    public void AddDataAnnotationsValidation_ShouldResolveValidationPipelineBehaviorWithEitherType()
    {
        // Arrange & Act
        using var provider = CreateProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<TestCommand, string>>();

        // Assert
        behavior.ShouldBeOfType<ValidationPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddDataAnnotationsValidation_ShouldRegisterValidationOrchestrator()
    {
        // Arrange & Act
        using var provider = CreateProvider();
        var orchestrator = provider.GetRequiredService<ValidationOrchestrator>();

        // Assert
        orchestrator.ShouldBeOfType<ValidationOrchestrator>();
    }

    [Fact]
    public void AddDataAnnotationsValidation_ShouldRegisterDataAnnotationsValidationProvider()
    {
        // Arrange & Act
        using var provider = CreateProvider();
        var validationProvider = provider.GetRequiredService<IValidationProvider>();

        // Assert
        validationProvider.ShouldBeOfType<DataAnnotationsValidationProvider>();
    }

    #endregion
}
