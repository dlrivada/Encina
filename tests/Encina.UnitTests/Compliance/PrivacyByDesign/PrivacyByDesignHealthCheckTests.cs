#pragma warning disable CA2012 // Use ValueTasks correctly
#pragma warning disable CA1859 // Use concrete types for improved performance

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

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
        PrivacyByDesignHealthCheck.DefaultName.ShouldBe("encina-privacy-by-design");
    }

    #endregion

    #region Tags

    [Fact]
    public void Tags_ShouldContainExpectedValues()
    {
        PrivacyByDesignHealthCheck.Tags.ShouldContain("encina");
        PrivacyByDesignHealthCheck.Tags.ShouldContain("gdpr");
        PrivacyByDesignHealthCheck.Tags.ShouldContain("privacy-by-design");
        PrivacyByDesignHealthCheck.Tags.ShouldContain("compliance");
        PrivacyByDesignHealthCheck.Tags.ShouldContain("ready");
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
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("fully configured");
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
        result.Data.ShouldContainKey("enforcementMode");
        result.Data.ShouldContainKey("privacyLevel");
        result.Data.ShouldContainKey("minimizationScoreThreshold");
        result.Data.ShouldContainKey("validatorType");
        result.Data.ShouldContainKey("purposeRegistryType");
        result.Data.ShouldContainKey("analyzerType");
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
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("PrivacyByDesignOptions");
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
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IPrivacyByDesignValidator");
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
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IPurposeRegistry");
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
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("IDataMinimizationAnalyzer");
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
        result.Data.ShouldContainKey("warnings");
    }

    #endregion
}
