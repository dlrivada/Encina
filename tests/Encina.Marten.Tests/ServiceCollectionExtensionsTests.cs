using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Encina.Marten;

namespace Encina.Marten.Tests;

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
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
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
        options.Value.AutoPublishDomainEvents.Should().BeFalse();
        options.Value.UseOptimisticConcurrency.Should().BeFalse();
        options.Value.ThrowOnConcurrencyConflict.Should().BeTrue();
        options.Value.StreamPrefix.Should().Be("test-prefix");
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
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(MartenAggregateRepository<>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaMarten_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaMarten();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaMarten_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaMarten());
    }

    [Fact]
    public void AddEncinaMarten_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddEncinaMarten(null!));
    }

    [Fact]
    public void EncinaMartenOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new EncinaMartenOptions();

        // Assert
        options.AutoPublishDomainEvents.Should().BeTrue();
        options.UseOptimisticConcurrency.Should().BeTrue();
        options.ThrowOnConcurrencyConflict.Should().BeFalse();
        options.StreamPrefix.Should().BeEmpty();
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
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(MartenAggregateRepository<TestAggregate>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
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
