using System.Reflection;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    #region AddEncina(IServiceCollection, Assembly[])

    /// <summary>
    /// Verifies that AddEncina throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncina_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncina(typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncina with configure delegate throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncina_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<EncinaConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncina(configure, typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncina allows null configure delegate (optional parameter).
    /// </summary>
    [Fact]
    public void AddEncina_NullConfigure_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<EncinaConfiguration>? configure = null;

        // Act & Assert
        var act = () => services.AddEncina(configure, typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that AddEncina registers IEncina in the service collection.
    /// </summary>
    [Fact]
    public void AddEncina_ValidServices_RegistersIEncina()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncina(typeof(ServiceCollectionExtensionsGuardTests).Assembly);

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IEncina));
    }

    /// <summary>
    /// Verifies that AddEncina returns the same IServiceCollection for chaining.
    /// </summary>
    [Fact]
    public void AddEncina_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncina(typeof(ServiceCollectionExtensionsGuardTests).Assembly);

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region AddEncina with empty assemblies

    /// <summary>
    /// Verifies that AddEncina with no assemblies falls back to its own assembly.
    /// </summary>
    [Fact]
    public void AddEncina_NoAssemblies_FallsBackToDefaultAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncina();

        // Assert
        result.ShouldBeSameAs(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IEncina));
    }

    #endregion

    #region AddApplicationMessaging (legacy alias)

    /// <summary>
    /// Verifies that AddApplicationMessaging throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddApplicationMessaging_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddApplicationMessaging(typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddApplicationMessaging with configure throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddApplicationMessaging_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<EncinaConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddApplicationMessaging(configure, typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    #endregion

    #region AddEncina configuration callback

    /// <summary>
    /// Verifies that the configuration callback is invoked when provided.
    /// </summary>
    [Fact]
    public void AddEncina_ConfigureCallback_IsInvoked()
    {
        // Arrange
        var services = new ServiceCollection();
        var callbackInvoked = false;

        // Act
        services.AddEncina(config =>
        {
            callbackInvoked = true;
            config.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensionsGuardTests).Assembly);
        });

        // Assert
        callbackInvoked.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that AddEncina registers IEncinaMetrics as singleton.
    /// </summary>
    [Fact]
    public void AddEncina_RegistersEncinaMetricsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncina(typeof(ServiceCollectionExtensionsGuardTests).Assembly);

        // Assert
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IEncinaMetrics) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    #endregion
}
