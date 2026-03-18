using Encina.Caching;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Marten;
using Encina.Marten.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DataResidency;

/// <summary>
/// Integration tests for the Encina.Compliance.DataResidency DI pipeline.
/// Tests service registration, options configuration, and cross-border transfer validation.
/// Event-sourced aggregates require Marten — these tests mock aggregate/read model repos.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DataResidencyPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaDataResidency_RegistersIResidencyPolicyService()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IResidencyPolicyService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIDataLocationService()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IDataLocationService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersICrossBorderTransferValidator()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ICrossBorderTransferValidator>().ShouldNotBeNull();
        provider.GetService<ICrossBorderTransferValidator>().ShouldBeOfType<DefaultCrossBorderTransferValidator>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIRegionContextProvider()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRegionContextProvider>().ShouldNotBeNull();
        provider.GetService<IRegionContextProvider>().ShouldBeOfType<DefaultRegionContextProvider>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIAdequacyDecisionProvider()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAdequacyDecisionProvider>().ShouldNotBeNull();
        provider.GetService<IAdequacyDecisionProvider>().ShouldBeOfType<DefaultAdequacyDecisionProvider>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIRegionRouter()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IRegionRouter>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<IRegionRouter>().ShouldBeOfType<DefaultRegionRouter>();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaDataResidency_DefaultOptions_HaveCorrectValues()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DataResidencyOptions>>().Value;
        options.EnforcementMode.ShouldBe(DataResidencyEnforcementMode.Warn);
        options.TrackDataLocations.ShouldBeTrue();
        options.BlockNonCompliantTransfers.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
        options.AutoRegisterFromAttributes.ShouldBeTrue();
        options.DefaultRegion.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaDataResidency_CustomOptions_AreRespected()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();

        // Act
        services.AddEncinaDataResidency(options =>
        {
            options.DefaultRegion = RegionRegistry.DE;
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
            options.TrackDataLocations = false;
            options.AutoRegisterFromAttributes = false;
            options.BlockNonCompliantTransfers = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DataResidencyOptions>>().Value;
        options.DefaultRegion.ShouldBe(RegionRegistry.DE);
        options.EnforcementMode.ShouldBe(DataResidencyEnforcementMode.Block);
        options.TrackDataLocations.ShouldBeFalse();
        options.AutoRegisterFromAttributes.ShouldBeFalse();
        options.BlockNonCompliantTransfers.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDataResidency_WithConfigure_CallsCallback()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();
        var callbackInvoked = false;

        // Act
        services.AddEncinaDataResidency(options =>
        {
            callbackInvoked = true;
            options.EnforcementMode = DataResidencyEnforcementMode.Disabled;
        });
        var provider = services.BuildServiceProvider();

        // Force options resolution to trigger the configure callback
        var options = provider.GetRequiredService<IOptions<DataResidencyOptions>>().Value;

        // Assert
        callbackInvoked.ShouldBeTrue();
        options.EnforcementMode.ShouldBe(DataResidencyEnforcementMode.Disabled);
    }

    #endregion

    #region Cross-Border Transfer Validation

    [Fact]
    public async Task ValidateTransfer_IntraEEA_Allowed()
    {
        // Arrange
        var services = CreateServicesWithMockedRepos();
        services.AddEncinaDataResidency(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        var transferValidator = provider.GetRequiredService<ICrossBorderTransferValidator>();

        // Act: Validate a transfer from Germany (DE) to France (FR) — both EEA members
        var result = await transferValidator.ValidateTransferAsync(
            RegionRegistry.DE,
            RegionRegistry.FR,
            "test-data");

        // Assert: Intra-EEA transfers should be allowed
        result.IsRight.ShouldBeTrue("transfer validation should succeed");
        var validation = result.Match(Right: r => r, Left: _ => default!);
        validation.IsAllowed.ShouldBeTrue(
            "intra-EEA transfers (DE -> FR) should be allowed under GDPR's single market");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a service collection with logging and mocked Marten repositories.
    /// The new ES services (IResidencyPolicyService, IDataLocationService) need
    /// IAggregateRepository and IReadModelRepository from Marten. For DI pipeline tests,
    /// we mock these so the services can be resolved without Marten infrastructure.
    /// </summary>
    private static ServiceCollection CreateServicesWithMockedRepos()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Mock Marten repositories that the default services depend on
        services.AddScoped(_ => Substitute.For<IAggregateRepository<ResidencyPolicyAggregate>>());
        services.AddScoped(_ => Substitute.For<IAggregateRepository<DataLocationAggregate>>());
        services.AddScoped(_ => Substitute.For<IReadModelRepository<ResidencyPolicyReadModel>>());
        services.AddScoped(_ => Substitute.For<IReadModelRepository<DataLocationReadModel>>());

        // Mock ICacheProvider (used by services for cache-aside)
        services.AddSingleton(_ => Substitute.For<ICacheProvider>());

        return services;
    }

    #endregion
}
