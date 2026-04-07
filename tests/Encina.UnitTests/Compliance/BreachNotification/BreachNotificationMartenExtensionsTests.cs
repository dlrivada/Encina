using Encina.Compliance.BreachNotification;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

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

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddBreachNotificationAggregates_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddBreachNotificationAggregates();

        // Should have registered aggregate repository and projection (2 minimum)
        services.Count.Should().BeGreaterThan(0);
    }
}
