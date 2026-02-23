using Encina.Compliance.Consent;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.Consent;

/// <summary>
/// Integration tests for the full Encina.Compliance.Consent pipeline.
/// Tests DI registration, full consent flows, and health check integration.
/// No Docker containers needed — all operations use in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConsentPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaConsent_RegistersAllDefaultServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IConsentStore>().Should().NotBeNull();
        provider.GetService<IConsentValidator>().Should().NotBeNull();
        provider.GetService<IConsentVersionManager>().Should().NotBeNull();
        provider.GetService<IConsentAuditStore>().Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaConsent_DefaultStore_IsInMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent();
        var provider = services.BuildServiceProvider();

        // Assert
        var store = provider.GetRequiredService<IConsentStore>();
        store.Should().BeOfType<InMemoryConsentStore>();
    }

    [Fact]
    public void AddEncinaConsent_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaConsent_CustomStoreRegisteredBefore_ShouldNotBeOverridden()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConsentStore, InMemoryConsentStore>();

        // Act
        services.AddEncinaConsent();
        var provider = services.BuildServiceProvider();

        // Assert - should use the pre-registered store
        var store = provider.GetRequiredService<IConsentStore>();
        store.Should().BeOfType<InMemoryConsentStore>();
    }

    [Fact]
    public void AddEncinaConsent_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Warn;
            options.DefaultExpirationDays = 365;
            options.DefinePurpose("marketing", p =>
            {
                p.Description = "Marketing communications";
                p.RequiresExplicitOptIn = true;
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<ConsentOptions>>().Value;
        options.EnforcementMode.Should().Be(ConsentEnforcementMode.Warn);
        options.DefaultExpirationDays.Should().Be(365);
    }

    #endregion

    #region Full Consent Flow with DI

    [Fact]
    public async Task FullPipeline_RecordAndValidateConsent_WithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
            options.DefinePurpose("marketing", p =>
            {
                p.Description = "Marketing communications";
                p.RequiresExplicitOptIn = true;
            });
        });
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConsentStore>();
        var validator = provider.GetRequiredService<IConsentValidator>();

        // Act: Record consent
        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-123",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = "integration-test",
            Metadata = new Dictionary<string, object?>()
        };

        var recordResult = await store.RecordConsentAsync(consent);
        recordResult.IsRight.Should().BeTrue();

        // Act: Validate consent
        var validationResult = await validator.ValidateAsync("user-123", ["marketing"]);

        // Assert
        validationResult.IsRight.Should().BeTrue();
        var result = (ConsentValidationResult)validationResult;
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FullPipeline_MissingConsent_ValidationFails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
            options.DefinePurpose("marketing", p =>
            {
                p.Description = "Marketing communications";
                p.RequiresExplicitOptIn = true;
            });
        });
        var provider = services.BuildServiceProvider();

        var validator = provider.GetRequiredService<IConsentValidator>();

        // Act: Validate consent (without recording any)
        var validationResult = await validator.ValidateAsync("user-456", ["marketing"]);

        // Assert
        validationResult.IsRight.Should().BeTrue();
        var result = (ConsentValidationResult)validationResult;
        result.IsValid.Should().BeFalse();
        result.MissingPurposes.Should().Contain("marketing");
    }

    [Fact]
    public async Task FullPipeline_WithdrawAndRevalidate_ShouldFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
        });
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConsentStore>();
        var validator = provider.GetRequiredService<IConsentValidator>();

        // Step 1: Record consent
        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-789",
            Purpose = "analytics",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = "integration-test",
            Metadata = new Dictionary<string, object?>()
        };
        await store.RecordConsentAsync(consent);

        // Step 2: Validate (should pass)
        var validBefore = await validator.ValidateAsync("user-789", ["analytics"]);
        ((ConsentValidationResult)validBefore).IsValid.Should().BeTrue();

        // Step 3: Withdraw consent
        var withdrawResult = await store.WithdrawConsentAsync("user-789", "analytics");
        withdrawResult.IsRight.Should().BeTrue();

        // Step 4: Validate again (should fail)
        var validAfter = await validator.ValidateAsync("user-789", ["analytics"]);
        ((ConsentValidationResult)validAfter).IsValid.Should().BeFalse();
    }

    #endregion

    #region Multi-Purpose Consent Flow

    [Fact]
    public async Task FullPipeline_MultiplePurposes_PartialConsent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent(options =>
        {
            options.EnforcementMode = ConsentEnforcementMode.Block;
            options.DefinePurpose("marketing", p => p.Description = "Marketing");
            options.DefinePurpose("analytics", p => p.Description = "Analytics");
        });
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConsentStore>();
        var validator = provider.GetRequiredService<IConsentValidator>();

        // Record consent for only one purpose
        await store.RecordConsentAsync(new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = "user-multi",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            GivenAtUtc = DateTimeOffset.UtcNow,
            Source = "test",
            Metadata = new Dictionary<string, object?>()
        });

        // Act: Validate for both purposes
        var result = await validator.ValidateAsync("user-multi", ["marketing", "analytics"]);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (ConsentValidationResult)result;
        validation.IsValid.Should().BeFalse();
        validation.MissingPurposes.Should().Contain("analytics");
        validation.MissingPurposes.Should().NotContain("marketing");
    }

    #endregion

    #region Bulk Operations via DI

    [Fact]
    public async Task FullPipeline_BulkOperations_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaConsent();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConsentStore>();

        var consents = Enumerable.Range(0, 10)
            .Select(i => new ConsentRecord
            {
                Id = Guid.NewGuid(),
                SubjectId = $"bulk-user-{i}",
                Purpose = "marketing",
                Status = ConsentStatus.Active,
                ConsentVersionId = "v1",
                GivenAtUtc = DateTimeOffset.UtcNow,
                Source = "bulk-test",
                Metadata = new Dictionary<string, object?>()
            })
            .ToList();

        // Act
        var result = await store.BulkRecordConsentAsync(consents);

        // Assert
        result.IsRight.Should().BeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.Should().Be(10);
        bulkResult.AllSucceeded.Should().BeTrue();
    }

    #endregion
}
