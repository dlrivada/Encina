using Encina.DomainModeling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

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

    // Test aggregate for registration tests
    private sealed class TestAggregate : AggregateBase
    {
        protected override void Apply(object domainEvent)
        {
            // No-op
        }
    }
}
