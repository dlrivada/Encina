using Encina.DomainModeling;
using Encina.Marten;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Marten;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMarten_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<EncinaMartenOptions>>();
        options.ShouldNotBeNull();
        options!.Value.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMarten_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.AutoPublishDomainEvents = false;
            options.UseOptimisticConcurrency = false;
            options.ThrowOnConcurrencyConflict = true;
            options.StreamPrefix = "test-prefix";
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<EncinaMartenOptions>>();
        options.Value.AutoPublishDomainEvents.ShouldBeFalse();
        options.Value.UseOptimisticConcurrency.ShouldBeFalse();
        options.Value.ThrowOnConcurrencyConflict.ShouldBeTrue();
        options.Value.StreamPrefix.ShouldBe("test-prefix");
    }

    [Fact]
    public void AddEncinaMarten_RegistersGenericAggregateRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAggregateRepository<>));

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(MartenAggregateRepository<>));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaMarten();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaMarten_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange

        // Act
        var act = () => ((IServiceCollection)null!).AddEncinaMarten();

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddEncinaMarten_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaMarten(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void EncinaMartenOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaMartenOptions();

        // Assert
        options.AutoPublishDomainEvents.ShouldBeTrue();
        options.UseOptimisticConcurrency.ShouldBeTrue();
        options.ThrowOnConcurrencyConflict.ShouldBeFalse();
        options.StreamPrefix.ShouldBeEmpty();
    }

    [Fact]
    public void AddAggregateRepository_RegistersSpecificRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAggregateRepository<TestAggregate>();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IAggregateRepository<TestAggregate>));

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(MartenAggregateRepository<TestAggregate>));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_WithDefaultMetadata_RegistersMetadataConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<StoreOptions>) &&
            d.ImplementationType == typeof(ConfigureMartenEventMetadata));

        // Assert - Default metadata options have features enabled
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMarten_WithMetadataDisabled_DoesNotRegisterMetadataConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            // Disable all metadata features
            options.Metadata.CorrelationIdEnabled = false;
            options.Metadata.CausationIdEnabled = false;
            options.Metadata.CaptureUserId = false;
            options.Metadata.CaptureTenantId = false;
            options.Metadata.CaptureTimestamp = false;
            options.Metadata.CaptureCommitSha = false;
            options.Metadata.CaptureSemanticVersion = false;
            options.Metadata.HeadersEnabled = false;
        });

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<StoreOptions>) &&
            d.ImplementationType == typeof(ConfigureMartenEventMetadata));

        // Assert - No metadata configuration when all features are disabled
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaMarten_WithOnlyCorrelationId_RegistersMetadataConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Metadata.CorrelationIdEnabled = true;
            options.Metadata.CausationIdEnabled = false;
            options.Metadata.CaptureUserId = false;
            options.Metadata.CaptureTenantId = false;
            options.Metadata.CaptureTimestamp = false;
            options.Metadata.HeadersEnabled = false;
        });

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<StoreOptions>) &&
            d.ImplementationType == typeof(ConfigureMartenEventMetadata));

        // Assert - CorrelationId alone should trigger registration
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMarten_MetadataOptions_CanBeConfigured()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            options.Metadata.CaptureCommitSha = true;
            options.Metadata.CommitSha = "abc123";
            options.Metadata.CaptureSemanticVersion = true;
            options.Metadata.SemanticVersion = "1.2.3";
            options.Metadata.CustomHeaders["Environment"] = "Production";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var martenOptions = provider.GetRequiredService<IOptions<EncinaMartenOptions>>();
        martenOptions.Value.Metadata.CaptureCommitSha.ShouldBeTrue();
        martenOptions.Value.Metadata.CommitSha.ShouldBe("abc123");
        martenOptions.Value.Metadata.CaptureSemanticVersion.ShouldBeTrue();
        martenOptions.Value.Metadata.SemanticVersion.ShouldBe("1.2.3");
        martenOptions.Value.Metadata.CustomHeaders["Environment"].ShouldBe("Production");
    }

    [Fact]
    public void AddEncinaMarten_WithDefaultMetadata_RegistersEventMetadataQuery()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEventMetadataQuery));

        // Assert - Event metadata query should be registered when metadata is enabled
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(MartenEventMetadataQuery));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_WithMetadataDisabled_DoesNotRegisterEventMetadataQuery()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaMarten(options =>
        {
            // Disable all metadata features
            options.Metadata.CorrelationIdEnabled = false;
            options.Metadata.CausationIdEnabled = false;
            options.Metadata.CaptureUserId = false;
            options.Metadata.CaptureTenantId = false;
            options.Metadata.CaptureTimestamp = false;
            options.Metadata.CaptureCommitSha = false;
            options.Metadata.CaptureSemanticVersion = false;
            options.Metadata.HeadersEnabled = false;
        });

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEventMetadataQuery));

        // Assert - No query service when metadata is disabled
        descriptor.ShouldBeNull();
    }

    // Test aggregate for registration tests
    private sealed class TestAggregate : AggregateBase
    {
        protected override void Apply(object domainEvent)
        {
            // No-op
        }
    }
}
