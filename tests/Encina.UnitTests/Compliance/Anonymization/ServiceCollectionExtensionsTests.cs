using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Techniques;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Compliance.Anonymization;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> verifying correct DI registrations.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAnonymization_NullServices_ShouldThrow()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaAnonymization());
    }

    [Fact]
    public void AddEncinaAnonymization_WithoutConfigure_ShouldRegisterDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IAnonymizer));
        services.ShouldContain(sd => sd.ServiceType == typeof(IPseudonymizer));
        services.ShouldContain(sd => sd.ServiceType == typeof(ITokenizer));
        services.ShouldContain(sd => sd.ServiceType == typeof(IRiskAssessor));
        services.ShouldContain(sd => sd.ServiceType == typeof(IAnonymizationAuditStore));
        services.ShouldContain(sd => sd.ServiceType == typeof(IKeyProvider));
        services.ShouldContain(sd => sd.ServiceType == typeof(ITokenMappingStore));
    }

    [Fact]
    public void AddEncinaAnonymization_WithConfigure_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization(options =>
        {
            options.EnforcementMode = AnonymizationEnforcementMode.Warn;
            options.TrackAuditTrail = false;
        });

        // Assert
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<AnonymizationOptions>));
    }

    [Fact]
    public void AddEncinaAnonymization_WithHealthCheck_ShouldRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization(options =>
        {
            options.AddHealthCheck = true;
        });

        // Assert
        var hasHealthCheck = services.Any(sd =>
            sd.ServiceType.Name.Contains("HealthCheck")
            || (sd.ImplementationType is not null
                && sd.ImplementationType.Name.Contains("AnonymizationHealthCheck")));
        hasHealthCheck.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaAnonymization_WithAutoRegistration_ShouldRegisterDescriptorAndHostedService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization(options =>
        {
            options.AutoRegisterFromAttributes = true;
            options.AssembliesToScan.Add(typeof(ServiceCollectionExtensionsTests).Assembly);
        });

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(AnonymizationAutoRegistrationDescriptor));
    }

    [Fact]
    public void AddEncinaAnonymization_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAnonymization();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersAnonymizationTechniques()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization();

        // Assert
        services.Count(sd => sd.ServiceType == typeof(IAnonymizationTechnique)).ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void AddEncinaAnonymization_RegistersOptionsValidator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaAnonymization();

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IValidateOptions<AnonymizationOptions>));
    }

    [Fact]
    public void AddEncinaAnonymization_TryAdd_ShouldNotOverrideCustomRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var customAnonymizer = NSubstitute.Substitute.For<IAnonymizer>();
        services.AddSingleton(customAnonymizer);

        // Act
        services.AddEncinaAnonymization();

        // Assert — only one registration
        services.Count(sd => sd.ServiceType == typeof(IAnonymizer)).ShouldBe(1);
    }
}
