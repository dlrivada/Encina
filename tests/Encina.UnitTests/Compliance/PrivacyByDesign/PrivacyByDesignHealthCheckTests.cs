#pragma warning disable CA2012 // Use ValueTasks correctly
#pragma warning disable CA1859 // Use concrete types for improved performance

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Health;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PrivacyByDesignHealthCheck"/>.
/// </summary>
public class PrivacyByDesignHealthCheckTests
{
    private static ServiceProvider BuildServiceProvider(
        bool includeOptions = true,
        bool includeValidator = true,
        bool includeRegistry = true,
        bool includeAnalyzer = true)
    {
        var services = new ServiceCollection();
        if (includeOptions)
            services.Configure<PrivacyByDesignOptions>(_ => { });
        if (includeValidator)
            services.AddSingleton(Substitute.For<IPrivacyByDesignValidator>());
        if (includeRegistry)
            services.AddSingleton(Substitute.For<IPurposeRegistry>());
        if (includeAnalyzer)
            services.AddSingleton(Substitute.For<IDataMinimizationAnalyzer>());
        return services.BuildServiceProvider();
    }

    private static PrivacyByDesignHealthCheck CreateHealthCheck(IServiceProvider serviceProvider) =>
        new(serviceProvider, NullLogger<PrivacyByDesignHealthCheck>.Instance);

    #region DefaultName

    [Fact]
    public void DefaultName_ShouldBeEncinaPrivacyByDesign()
    {
        PrivacyByDesignHealthCheck.DefaultName.Should().Be("encina-privacy-by-design");
    }

    #endregion

    #region Tags

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        PrivacyByDesignHealthCheck.Tags.Should().Contain("encina");
        PrivacyByDesignHealthCheck.Tags.Should().Contain("gdpr");
        PrivacyByDesignHealthCheck.Tags.Should().Contain("privacy-by-design");
        PrivacyByDesignHealthCheck.Tags.Should().Contain("compliance");
        PrivacyByDesignHealthCheck.Tags.Should().Contain("ready");
    }

    #endregion

    #region Healthy — All Services Registered

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ShouldReturnHealthy()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("fully configured");
    }

    [Fact]
    public async Task CheckHealthAsync_AllServicesRegistered_ShouldIncludeExpectedData()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("enforcementMode");
        result.Data.Should().ContainKey("privacyLevel");
        result.Data.Should().ContainKey("minimizationScoreThreshold");
        result.Data.Should().ContainKey("validatorType");
        result.Data.Should().ContainKey("purposeRegistryType");
        result.Data.Should().ContainKey("analyzerType");
    }

    #endregion

    #region Unhealthy — Missing Options

    [Fact]
    public async Task CheckHealthAsync_MissingOptions_ShouldReturnUnhealthy()
    {
        // Arrange
        var provider = BuildServiceProvider(includeOptions: false);
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("PrivacyByDesignOptions");
    }

    #endregion

    #region Unhealthy — Missing Validator

    [Fact]
    public async Task CheckHealthAsync_MissingValidator_ShouldReturnUnhealthy()
    {
        // Arrange
        var provider = BuildServiceProvider(includeValidator: false);
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IPrivacyByDesignValidator");
    }

    #endregion

    #region Unhealthy — Missing Registry

    [Fact]
    public async Task CheckHealthAsync_MissingRegistry_ShouldReturnUnhealthy()
    {
        // Arrange
        var provider = BuildServiceProvider(includeRegistry: false);
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("IPurposeRegistry");
    }

    #endregion

    #region Degraded — Missing Analyzer

    [Fact]
    public async Task CheckHealthAsync_MissingAnalyzer_ShouldReturnDegraded()
    {
        // Arrange
        var provider = BuildServiceProvider(includeAnalyzer: false);
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("IDataMinimizationAnalyzer");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingAnalyzer_ShouldIncludeWarningInData()
    {
        // Arrange
        var provider = BuildServiceProvider(includeAnalyzer: false);
        var healthCheck = CreateHealthCheck(provider);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Data.Should().ContainKey("warnings");
    }

    #endregion
}
