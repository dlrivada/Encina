using Encina.Compliance.BreachNotification;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions.AddEncinaBreachNotification"/>
/// null parameter handling and happy path registration.
/// </summary>
public sealed class BreachNotificationServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaBreachNotification_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaBreachNotification();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaBreachNotification_WithoutConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaBreachNotification();

        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaBreachNotification_WithConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaBreachNotification(options =>
        {
            options.NotificationDeadlineHours = 48;
        });

        Should.NotThrow(act);
    }
}
