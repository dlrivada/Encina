using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Abstractions;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAIAct_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAISystemRegistry>().ShouldNotBeNull().ShouldBeOfType<InMemoryAISystemRegistry>();
        provider.GetService<IAIActClassifier>().ShouldNotBeNull().ShouldBeOfType<DefaultAIActClassifier>();
        provider.GetService<IHumanOversightEnforcer>().ShouldNotBeNull().ShouldBeOfType<DefaultHumanOversightEnforcer>();
        provider.GetService<IDataQualityValidator>().ShouldNotBeNull();
        provider.GetService<IAIActDocumentation>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAIAct_WithoutConfigure_ShouldRegisterDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAIAct();

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<AIActOptions>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAIAct_NullServices_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ((IServiceCollection)null!).AddEncinaAIAct();

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAIAct_CustomRegistryBeforeCall_ShouldNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customRegistry = NSubstitute.Substitute.For<IAISystemRegistry>();
        services.AddSingleton(customRegistry);

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert — TryAdd should NOT override the custom registration
        provider.GetService<IAISystemRegistry>().ShouldBeSameAs(customRegistry);
    }

    [Fact]
    public void AddEncinaAIAct_CustomValidatorBeforeCall_ShouldNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customValidator = NSubstitute.Substitute.For<IAIActComplianceValidator>();
        services.AddScoped(_ => customValidator);

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IAIActComplianceValidator>().ShouldBeSameAs(customValidator);
    }

    [Fact]
    public void AddEncinaAIAct_WithHealthCheck_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AddHealthCheck = true;
            options.AutoRegisterFromAttributes = false;
        });

        // Assert — health check should be registered
        services.ShouldContain(sd => sd.ImplementationType != null &&
            sd.ImplementationType.Name.Contains("HealthCheckService"));
    }

    [Fact]
    public void AddEncinaAIAct_WithAutoRegistration_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = true;
        });

        // Assert
        services.ShouldContain(sd =>
            sd.ImplementationType == typeof(AIActAutoRegistrationHostedService));
    }

    [Fact]
    public void AddEncinaAIAct_WithAutoRegistrationDisabled_ShouldNotRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        // Assert
        services.ShouldNotContain(sd =>
            sd.ImplementationType == typeof(AIActAutoRegistrationHostedService));
    }

    [Fact]
    public void AddEncinaAIAct_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        // Assert
        returned.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAIAct_ShouldRegisterScopedValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAIAct(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert — scoped service requires a scope
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IAIActComplianceValidator>().ShouldNotBeNull().ShouldBeOfType<DefaultAIActComplianceValidator>();
    }
}
