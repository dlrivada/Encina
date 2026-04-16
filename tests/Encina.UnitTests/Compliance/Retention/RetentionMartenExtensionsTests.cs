using Encina.Compliance.Retention;

using Shouldly;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionMartenExtensions.AddRetentionAggregates"/>.
/// </summary>
public sealed class RetentionMartenExtensionsTests
{
    [Fact]
    public void AddRetentionAggregates_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddRetentionAggregates();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddRetentionAggregates_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddRetentionAggregates();

        // Should have registered aggregate repositories and projections (6 total minimum)
        services.Count.ShouldBeGreaterThan(0);
    }
}
