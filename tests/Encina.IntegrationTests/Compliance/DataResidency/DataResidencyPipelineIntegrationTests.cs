#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;
using Encina.Compliance.DataResidency.Model;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.DataResidency;

/// <summary>
/// Integration tests for the full Encina.Compliance.DataResidency pipeline.
/// Tests DI registration, full lifecycle flows (policy creation, region checks,
/// cross-border transfers), options configuration, and concurrent access safety.
/// No Docker containers needed — all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DataResidencyPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaDataResidency_RegistersIResidencyPolicyStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IResidencyPolicyStore>().Should().NotBeNull();
        provider.GetService<IResidencyPolicyStore>().Should().BeOfType<InMemoryResidencyPolicyStore>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIDataLocationStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IDataLocationStore>().Should().NotBeNull();
        provider.GetService<IDataLocationStore>().Should().BeOfType<InMemoryDataLocationStore>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIResidencyAuditStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IResidencyAuditStore>().Should().NotBeNull();
        provider.GetService<IResidencyAuditStore>().Should().BeOfType<InMemoryResidencyAuditStore>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIDataResidencyPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IDataResidencyPolicy>().Should().NotBeNull();
        provider.GetService<IDataResidencyPolicy>().Should().BeOfType<DefaultDataResidencyPolicy>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersICrossBorderTransferValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ICrossBorderTransferValidator>().Should().NotBeNull();
        provider.GetService<ICrossBorderTransferValidator>().Should().BeOfType<DefaultCrossBorderTransferValidator>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIRegionContextProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRegionContextProvider>().Should().NotBeNull();
        provider.GetService<IRegionContextProvider>().Should().BeOfType<DefaultRegionContextProvider>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIAdequacyDecisionProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IAdequacyDecisionProvider>().Should().NotBeNull();
        provider.GetService<IAdequacyDecisionProvider>().Should().BeOfType<DefaultAdequacyDecisionProvider>();
    }

    [Fact]
    public void AddEncinaDataResidency_RegistersIRegionRouter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRegionRouter>().Should().NotBeNull();
        provider.GetService<IRegionRouter>().Should().BeOfType<DefaultRegionRouter>();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaDataResidency_DefaultOptions_HaveCorrectValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DataResidencyOptions>>().Value;
        options.EnforcementMode.Should().Be(DataResidencyEnforcementMode.Warn);
        options.TrackDataLocations.Should().BeTrue();
        options.TrackAuditTrail.Should().BeTrue();
        options.BlockNonCompliantTransfers.Should().BeTrue();
        options.AddHealthCheck.Should().BeFalse();
        options.AutoRegisterFromAttributes.Should().BeTrue();
        options.DefaultRegion.Should().BeNull();
    }

    [Fact]
    public void AddEncinaDataResidency_CustomOptions_AreRespected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDataResidency(options =>
        {
            options.DefaultRegion = RegionRegistry.DE;
            options.EnforcementMode = DataResidencyEnforcementMode.Block;
            options.TrackDataLocations = false;
            options.TrackAuditTrail = false;
            options.AutoRegisterFromAttributes = false;
            options.BlockNonCompliantTransfers = false;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DataResidencyOptions>>().Value;
        options.DefaultRegion.Should().Be(RegionRegistry.DE);
        options.EnforcementMode.Should().Be(DataResidencyEnforcementMode.Block);
        options.TrackDataLocations.Should().BeFalse();
        options.TrackAuditTrail.Should().BeFalse();
        options.AutoRegisterFromAttributes.Should().BeFalse();
        options.BlockNonCompliantTransfers.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaDataResidency_WithConfigure_CallsCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
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
        callbackInvoked.Should().BeTrue();
        options.EnforcementMode.Should().Be(DataResidencyEnforcementMode.Disabled);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public async Task CreatePolicy_CheckRegionAllowed_RegionPasses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataResidency(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        var policyStore = provider.GetRequiredService<IResidencyPolicyStore>();
        var residencyPolicy = provider.GetRequiredService<IDataResidencyPolicy>();

        // Act: Create a policy allowing EU member states only
        var policy = ResidencyPolicyDescriptor.Create(
            dataCategory: "test-category",
            allowedRegions: RegionRegistry.EUMemberStates);

        var createResult = await policyStore.CreateAsync(policy);
        createResult.IsRight.Should().BeTrue("policy creation should succeed");

        // Act & Assert: Germany (DE) is an EU member state — should be allowed
        var deResult = await residencyPolicy.IsAllowedAsync("test-category", RegionRegistry.DE);
        deResult.IsRight.Should().BeTrue("IsAllowedAsync should succeed for a valid category");
        deResult.Match(Right: r => r, Left: _ => false)
            .Should().BeTrue("Germany (DE) is an EU member state and should be allowed");

        // Act & Assert: United States (US) is not an EU member state — should be denied
        var usResult = await residencyPolicy.IsAllowedAsync("test-category", RegionRegistry.US);
        usResult.IsRight.Should().BeTrue("IsAllowedAsync should succeed for a valid category");
        usResult.Match(Right: r => r, Left: _ => true)
            .Should().BeFalse("United States (US) is not an EU member state and should be denied");
    }

    [Fact]
    public async Task RecordDataLocation_RetrieveByEntity_MatchesOriginal()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataResidency();
        var provider = services.BuildServiceProvider();

        var locationStore = provider.GetRequiredService<IDataLocationStore>();

        // Act: Create and record a data location
        var location = DataLocation.Create(
            entityId: "customer-42",
            dataCategory: "personal-data",
            region: RegionRegistry.DE,
            storageType: StorageType.Primary,
            metadata: null);

        var recordResult = await locationStore.RecordAsync(location);
        recordResult.IsRight.Should().BeTrue("recording a data location should succeed");

        // Act: Retrieve by entity
        var retrieveResult = await locationStore.GetByEntityAsync("customer-42");
        retrieveResult.IsRight.Should().BeTrue("retrieving by entity should succeed");

        var locations = retrieveResult.Match(Right: r => r, Left: _ => []);
        locations.Should().HaveCount(1, "exactly one location was recorded for this entity");

        var retrieved = locations[0];
        retrieved.EntityId.Should().Be("customer-42");
        retrieved.DataCategory.Should().Be("personal-data");
        retrieved.Region.Should().Be(RegionRegistry.DE);
        retrieved.StorageType.Should().Be(StorageType.Primary);
        retrieved.Id.Should().Be(location.Id);
    }

    [Fact]
    public async Task CreatePolicy_ValidateTransfer_IntraEEA_Allowed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
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
        result.IsRight.Should().BeTrue("transfer validation should succeed");
        var validation = result.Match(Right: r => r, Left: _ => default!);
        validation.IsAllowed.Should().BeTrue(
            "intra-EEA transfers (DE -> FR) should be allowed under GDPR's single market");
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task ConcurrentPolicyCreation_AllSucceed_NoDataCorruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDataResidency(options =>
        {
            options.AutoRegisterFromAttributes = false;
        });
        var provider = services.BuildServiceProvider();

        var policyStore = provider.GetRequiredService<IResidencyPolicyStore>();
        var policyCount = 50;

        // Act: Create many policies concurrently with unique data categories
        var tasks = Enumerable.Range(0, policyCount).Select(async i =>
        {
            var policy = ResidencyPolicyDescriptor.Create(
                dataCategory: $"concurrent-category-{i}",
                allowedRegions: RegionRegistry.EUMemberStates,
                requireAdequacyDecision: false,
                allowedTransferBases: null);

            var result = await policyStore.CreateAsync(policy);
            return (Category: policy.DataCategory, Result: result);
        });

        var results = await Task.WhenAll(tasks);

        // Assert: All creations succeeded
        results.Should().AllSatisfy(r =>
            r.Result.IsRight.Should().BeTrue($"policy creation for '{r.Category}' should succeed"));

        // Assert: All policies are retrievable with correct data
        foreach (var (category, _) in results)
        {
            var retrieved = await policyStore.GetByCategoryAsync(category);
            retrieved.IsRight.Should().BeTrue($"policy '{category}' should be retrievable");

            var policyOption = retrieved.Match(Right: opt => opt, Left: _ => default);
            policyOption.IsSome.Should().BeTrue($"policy '{category}' should exist in the store");

            var storedPolicy = policyOption.Match(Some: p => p, None: () => null!);
            storedPolicy.DataCategory.Should().Be(category, "stored DataCategory should match what was created");
            storedPolicy.AllowedRegions.Should().BeEquivalentTo(RegionRegistry.EUMemberStates);
        }

        // Assert: All data categories are unique (no overwrite/collision)
        var categories = results.Select(r => r.Category).ToList();
        categories.Should().OnlyHaveUniqueItems("each concurrent write should produce a distinct policy entry");
    }

    #endregion
}
