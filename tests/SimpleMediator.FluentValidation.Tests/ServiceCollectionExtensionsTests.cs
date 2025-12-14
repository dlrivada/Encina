using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace SimpleMediator.FluentValidation.Tests;

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
    public void AddSimpleMediatorFluentValidation_ShouldRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddSimpleMediatorFluentValidation(assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetService<IPipelineBehavior<TestCommand, string>>();
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<ValidationPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_ShouldRegisterValidatorsFromAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddSimpleMediatorFluentValidation(assembly);

        // Assert
        var provider = services.BuildServiceProvider();

        var testValidators = provider.GetServices<IValidator<TestCommand>>().ToList();
        testValidators.ShouldNotBeEmpty();
        testValidators.ShouldContain(v => v is TestCommandValidator);

        var anotherValidators = provider.GetServices<IValidator<AnotherCommand>>().ToList();
        anotherValidators.ShouldNotBeEmpty();
        anotherValidators.ShouldContain(v => v is AnotherCommandValidator);
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithSingletonLifetime_ShouldRegisterValidatorsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddSimpleMediatorFluentValidation(assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<TestCommand>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithScopedLifetime_ShouldRegisterValidatorsAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddSimpleMediatorFluentValidation(ServiceLifetime.Scoped, assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<TestCommand>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithTransientLifetime_ShouldRegisterValidatorsAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddSimpleMediatorFluentValidation(ServiceLifetime.Transient, assembly);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<TestCommand>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddSimpleMediatorFluentValidation(assembly));
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithNullAssemblies_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddSimpleMediatorFluentValidation(null!));
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithEmptyAssemblies_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => services.AddSimpleMediatorFluentValidation(Array.Empty<System.Reflection.Assembly>()));
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_WithMultipleAssemblies_ShouldRegisterValidatorsFromAll()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = typeof(ServiceCollectionExtensionsTests).Assembly;
        var assembly2 = typeof(ValidationPipelineBehavior<,>).Assembly; // SimpleMediator.FluentValidation assembly (no validators here)

        // Act
        services.AddSimpleMediatorFluentValidation(assembly1, assembly2);

        // Assert
        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidator<TestCommand>>().ToList();
        validators.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_CalledMultipleTimes_ShouldNotDuplicateValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;

        // Act
        services.AddSimpleMediatorFluentValidation(assembly);
        services.AddSimpleMediatorFluentValidation(assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidator<TestCommand>>().ToList();

        // TryAddEnumerable should prevent duplicates
        validators.Count.ShouldBe(1);
    }

    [Fact]
    public void AddSimpleMediatorFluentValidation_ShouldResolveValidatorsInBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ServiceCollectionExtensionsTests).Assembly;
        services.AddSimpleMediatorFluentValidation(assembly);

        // Act
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetService<IPipelineBehavior<TestCommand, string>>();

        // Assert
        behavior.ShouldNotBeNull();
        behavior.ShouldBeOfType<ValidationPipelineBehavior<TestCommand, string>>();
    }
}
