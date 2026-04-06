using Encina.Compliance.BreachNotification;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachNotificationMartenExtensions"/>
/// null parameter handling on extension methods.
/// </summary>
public sealed class BreachNotificationMartenExtensionsGuardTests
{
    [Fact]
    public void AddBreachNotificationAggregates_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBreachNotificationAggregates();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }
}
