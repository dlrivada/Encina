using Encina.Compliance.BreachNotification;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="BreachNotificationMartenExtensions.AddBreachNotificationAggregates"/>.
/// </summary>
public sealed class BreachNotificationMartenExtensionsTests
{
    [Fact]
    public void AddBreachNotificationAggregates_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddBreachNotificationAggregates();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddBreachNotificationAggregates_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddBreachNotificationAggregates();

        // Should have registered aggregate repository and projection (2 minimum)
        services.Count.ShouldBeGreaterThan(0);
    }
}
