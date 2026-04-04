using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> verifying correct DI registrations.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaDataResidency_NullServices_ShouldThrow()
    {
        // Act & Assert
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaDataResidency());
    }

    [Fact]
    public void AddEncinaDataResidency_WithoutConfigure_ShouldRegisterDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(ICrossBorderTransferValidator));
        services.ShouldContain(sd => sd.ServiceType == typeof(IRegionContextProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(IAdequacyDecisionProvider));
    }

    [Fact]
    public void AddEncinaDataResidency_WithConfigure_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency(options =>
        {
            options.DefaultRegion = RegionRegistry.DE;
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
        });

        // Assert — options are correctly configured
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<DataResidencyOptions>));
    }

    [Fact]
    public void AddEncinaDataResidency_WithHealthCheck_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency(options =>
        {
            options.AddHealthCheck = true;
        });

        // Assert
        var hasHealthCheck = services.Any(sd =>
            sd.ServiceType.Name.Contains("HealthCheck")
            || (sd.ImplementationType is not null && sd.ImplementationType.Name.Contains("DataResidencyHealthCheck")));
        hasHealthCheck.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaDataResidency_WithFluentPolicies_ShouldRegisterDescriptor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency(options =>
        {
            options.AutoRegisterFromAttributes = false;
            options.AddPolicy("healthcare-data", policy =>
            {
                policy.AllowEU();
                policy.RequireAdequacyDecision();
            });
        });

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(DataResidencyFluentPolicyDescriptor));
    }

    [Fact]
    public void AddEncinaDataResidency_WithAutoRegistration_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency(options =>
        {
            options.AutoRegisterFromAttributes = true;
            options.AssembliesToScan.Add(typeof(ServiceCollectionExtensionsTests).Assembly);
        });

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(DataResidencyAutoRegistrationDescriptor));
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDataResidency();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void AddEncinaDataResidency_TryAdd_ShouldNotOverrideCustomRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var customProvider = NSubstitute.Substitute.For<IRegionContextProvider>();
        services.AddSingleton(customProvider);

        // Act
        services.AddEncinaDataResidency();

        // Assert — only one registration
        services.Count(sd => sd.ServiceType == typeof(IRegionContextProvider)).ShouldBe(1);
    }
}
