using Encina.FluentValidation;
﻿using Encina.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.FluentValidation;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed record TestCommand(string Name) : ICommand<string>;
    private sealed record AnotherCommand(int Value) : ICommand<int>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    private sealed class AnotherCommandValidator : AbstractValidator<AnotherCommand>
    {
        public AnotherCommandValidator()
        {
            RuleFor(x => x.Value).GreaterThan(0);
        }
    }

    [Fact]
    public void AddEncinaFluentValidation_ShouldRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(assembly);

        // Assert
        using var provider = services.BuildServiceProvider();
        var behavior = provider.GetService<IPipelineBehavior<TestCommand, string>>();
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<global::Encina.Validation.ValidationPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddEncinaFluentValidation_ShouldRegisterValidatorsFromAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(assembly);

        // Assert
        using var provider = services.BuildServiceProvider();

        var testValidators = provider.GetServices<IValidator<TestCommand>>().ToList();
        testValidators.ShouldNotBeEmpty();
        testValidators.ShouldContain(v => v is TestCommandValidator);

        var anotherValidators = provider.GetServices<IValidator<AnotherCommand>>().ToList();
        anotherValidators.ShouldNotBeEmpty();
        anotherValidators.ShouldContain(v => v is AnotherCommandValidator);
    }

    [Fact]
    public void AddEncinaFluentValidation_WithSingletonLifetime_ShouldRegisterValidatorsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<TestCommand>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaFluentValidation_WithScopedLifetime_ShouldRegisterValidatorsAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(ServiceLifetime.Scoped, assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<TestCommand>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaFluentValidation_WithTransientLifetime_ShouldRegisterValidatorsAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(ServiceLifetime.Transient, assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<TestCommand>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddEncinaFluentValidation_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddEncinaFluentValidation(assembly));
    }

    [Fact]
    public void AddEncinaFluentValidation_WithNullAssemblies_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddEncinaFluentValidation(null!));
    }

    [Fact]
    public void AddEncinaFluentValidation_WithEmptyAssemblies_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddEncinaFluentValidation(Array.Empty<System.Reflection.Assembly>()));
    }

    [Fact]
    public void AddEncinaFluentValidation_WithMultipleAssemblies_ShouldRegisterValidatorsFromAll()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = typeof(ServiceCollectionExtensionsTests).Assembly;
        var assembly2 = typeof(FluentValidationProvider).Assembly; // Encina.FluentValidation assembly (no validators here)

        // Act
        services.AddEncinaFluentValidation(assembly1, assembly2);

        // Assert
        using var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidator<TestCommand>>().ToList();
        validators.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEncinaFluentValidation_CalledMultipleTimes_ShouldNotDuplicateValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act - Call multiple times
        services.AddEncinaFluentValidation(assembly);
        services.AddEncinaFluentValidation(assembly);
        services.AddEncinaFluentValidation(assembly);

        // Assert
        using var provider = services.BuildServiceProvider();

        // TryAddEnumerable should prevent duplicate validators
        var validators = provider.GetServices<IValidator<TestCommand>>().ToList();
        validators.Count.ShouldBe(1);

        // TryAddTransient should prevent duplicate behaviors
        var behaviors = provider.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();
        behaviors.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaFluentValidation_ShouldResolveValidatorsInBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;
        services.AddEncinaFluentValidation(assembly);

        // Act
        using var provider = services.BuildServiceProvider();
        var behavior = provider.GetService<IPipelineBehavior<TestCommand, string>>();

        // Assert
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<global::Encina.Validation.ValidationPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddEncinaFluentValidation_ShouldRegisterValidationOrchestrator()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(assembly);

        // Assert
        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetService<ValidationOrchestrator>();
        orchestrator.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaFluentValidation_ShouldRegisterFluentValidationProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(assembly);

        // Assert
        using var provider = services.BuildServiceProvider();
        var validationProvider = provider.GetService<IValidationProvider>();
        validationProvider.ShouldNotBeNull();
        validationProvider.ShouldBeOfType<FluentValidationProvider>();
    }

    [Fact]
    public void AddEncinaFluentValidation_LifetimeConfiguration_ShouldWorkWithValidationPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddEncinaFluentValidation(ServiceLifetime.Scoped, assembly);

        // Assert
        using var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var validator1 = scope1.ServiceProvider.GetService<IValidator<TestCommand>>();
        var validator2 = scope2.ServiceProvider.GetService<IValidator<TestCommand>>();

        // Different scopes should have different instances for scoped lifetime
        validator1.ShouldNotBeNull();
        validator2.ShouldNotBeNull();
        ReferenceEquals(validator1, validator2).ShouldBeFalse();
    }
}
