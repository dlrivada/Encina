using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaGDPR_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaGDPR(options =>
        {
            options.ControllerName = "Acme Corp";
            options.ControllerEmail = "privacy@acme.com";
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IProcessingActivityRegistry>().Should().NotBeNull()
            .And.BeOfType<InMemoryProcessingActivityRegistry>();
        provider.GetService<IGDPRComplianceValidator>().Should().NotBeNull()
            .And.BeOfType<DefaultGDPRComplianceValidator>();
        provider.GetService<JsonRoPAExporter>().Should().NotBeNull();
        provider.GetService<CsvRoPAExporter>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaGDPR_WithoutConfigure_ShouldRegisterDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaGDPR();

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IOptions<GDPROptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaGDPR_NullServices_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => ((IServiceCollection)null!).AddEncinaGDPR();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddEncinaGDPR_CustomRegistryBeforeCall_ShouldNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customRegistry = NSubstitute.Substitute.For<IProcessingActivityRegistry>();
        services.AddSingleton(customRegistry);

        // Act
        services.AddEncinaGDPR(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert — TryAdd should NOT override the custom registration
        provider.GetService<IProcessingActivityRegistry>().Should().BeSameAs(customRegistry);
    }

    [Fact]
    public void AddEncinaGDPR_CustomValidatorBeforeCall_ShouldNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customValidator = NSubstitute.Substitute.For<IGDPRComplianceValidator>();
        services.AddScoped(_ => customValidator);

        // Act
        services.AddEncinaGDPR(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IGDPRComplianceValidator>().Should().BeSameAs(customValidator);
    }

    [Fact]
    public void AddEncinaGDPR_WithHealthCheck_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaGDPR(options =>
        {
            options.AddHealthCheck = true;
            options.AutoRegisterFromAttributes = false;
        });

        // Assert — health check should be registered
        services.Should().Contain(sd => sd.ImplementationType != null &&
            sd.ImplementationType.Name.Contains("HealthCheckService"));
    }

    [Fact]
    public void AddEncinaGDPR_WithAutoRegistration_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaGDPR(options =>
        {
            options.AutoRegisterFromAttributes = true;
        });

        // Assert
        services.Should().Contain(sd =>
            sd.ImplementationType == typeof(GDPRAutoRegistrationHostedService));
    }

    [Fact]
    public void AddEncinaGDPR_WithAutoRegistrationDisabled_ShouldNotRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaGDPR(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        // Assert
        services.Should().NotContain(sd =>
            sd.ImplementationType == typeof(GDPRAutoRegistrationHostedService));
    }

    [Fact]
    public void AddEncinaGDPR_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returned = services.AddEncinaGDPR(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });

        // Assert
        returned.Should().BeSameAs(services);
    }
}
