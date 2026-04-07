using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    #region AddEncinaBreachNotification Guards

    /// <summary>
    /// Verifies that AddEncinaBreachNotification throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaBreachNotification_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaBreachNotification();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaBreachNotification throws ArgumentNullException when services
    /// is null, even when a configure action is provided.
    /// </summary>
    [Fact]
    public void AddEncinaBreachNotification_NullServicesWithConfigure_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaBreachNotification(options =>
        {
            options.EnforcementMode = BreachDetectionEnforcementMode.Block;
        });

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    #endregion
}
