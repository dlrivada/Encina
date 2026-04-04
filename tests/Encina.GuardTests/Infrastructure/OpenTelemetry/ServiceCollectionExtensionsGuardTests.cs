using Encina.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> covering null argument validation
/// and basic DI registration verification.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    #region AddEncinaOpenTelemetry

    [Fact]
    public void AddEncinaOpenTelemetry_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaOpenTelemetry();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_ValidServices_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaOpenTelemetry();

        result.ShouldBe(services);
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WithNullConfigure_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() => services.AddEncinaOpenTelemetry(configure: null));
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WithConfigure_InvokesDelegate()
    {
        var services = new ServiceCollection();
        var invoked = false;

        services.AddEncinaOpenTelemetry(options =>
        {
            invoked = true;
            options.ServiceName = "TestService";
            options.ServiceVersion = "2.0.0";
        });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaOpenTelemetry_RegistersOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaOpenTelemetry(options =>
        {
            options.ServiceName = "MyService";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetService<EncinaOpenTelemetryOptions>();
        options.ShouldNotBeNull();
        options.ServiceName.ShouldBe("MyService");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_CalledTwice_DoesNotDuplicateOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaOpenTelemetry(o => o.ServiceName = "First");
        services.AddEncinaOpenTelemetry(o => o.ServiceName = "Second");

        var sp = services.BuildServiceProvider();
        // TryAddSingleton means first registration wins
        var options = sp.GetRequiredService<EncinaOpenTelemetryOptions>();
        options.ServiceName.ShouldBe("First");
    }

    #endregion

    #region WithEncina (OpenTelemetryBuilder extension)

    [Fact]
    public void WithEncina_NullBuilder_ThrowsArgumentNullException()
    {
        OpenTelemetryBuilder builder = null!;

        var act = () => builder.WithEncina();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }

    #endregion

    #region AddEncinaInstrumentation (TracerProviderBuilder)

    [Fact]
    public void AddEncinaInstrumentation_NullTracerBuilder_ThrowsArgumentNullException()
    {
        TracerProviderBuilder builder = null!;

        var act = () => builder.AddEncinaInstrumentation();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }

    #endregion

    #region AddEncinaInstrumentation (MeterProviderBuilder)

    [Fact]
    public void AddEncinaInstrumentation_NullMeterBuilder_ThrowsArgumentNullException()
    {
        MeterProviderBuilder builder = null!;

        var act = () => builder.AddEncinaInstrumentation();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }

    #endregion
}
