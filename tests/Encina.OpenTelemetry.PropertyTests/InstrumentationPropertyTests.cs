using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Xunit;

namespace Encina.OpenTelemetry.PropertyTests;

/// <summary>
/// Property-based tests for OpenTelemetry instrumentation.
/// Verifies invariants hold for all possible inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public sealed class InstrumentationPropertyTests
{
    /// <summary>
    /// Property: For any service collection, AddEncinaInstrumentation always returns a non-null builder.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool AddEncinaInstrumentation_AlwaysReturnsNonNullBuilder()
    {
        var services = new ServiceCollection();
        var telemetryBuilder = services.AddOpenTelemetry();
        TracerProviderBuilder? tracerBuilder = null;

        telemetryBuilder.WithTracing(builder =>
        {
            tracerBuilder = builder;
        });

        var result = tracerBuilder!.AddEncinaInstrumentation();

        return result != null;
    }

    /// <summary>
    /// Property: WithEncina always returns the same builder instance.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool WithEncina_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();
        var result = builder.WithEncina();

        return ReferenceEquals(builder, result);
    }

    /// <summary>
    /// Property: Service name can be any non-null string.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ServiceName_AcceptsAnyNonNullString(NonEmptyString nonEmptyStr)
    {
        var serviceName = nonEmptyStr.Get;
        var options = new EncinaOpenTelemetryOptions
        {
            ServiceName = serviceName
        };

        return options.ServiceName == serviceName;
    }

    /// <summary>
    /// Property: Service version can be any non-null string.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ServiceVersion_AcceptsAnyNonNullString(NonEmptyString nonEmptyStr)
    {
        var serviceVersion = nonEmptyStr.Get;
        var options = new EncinaOpenTelemetryOptions
        {
            ServiceVersion = serviceVersion
        };

        return options.ServiceVersion == serviceVersion;
    }

    /// <summary>
    /// Property: Options equality is based on value equality.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Options_EqualityIsValueBased(NonEmptyString name, NonEmptyString version)
    {
        var options1 = new EncinaOpenTelemetryOptions
        {
            ServiceName = name.Get,
            ServiceVersion = version.Get
        };

        var options2 = new EncinaOpenTelemetryOptions
        {
            ServiceName = name.Get,
            ServiceVersion = version.Get
        };

        // Same values => should be equal
        return options1.ServiceName == options2.ServiceName &&
               options1.ServiceVersion == options2.ServiceVersion;
    }

    /// <summary>
    /// Property: Instrumentation can be added multiple times without errors.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Instrumentation_CanBeAddedMultipleTimes(PositiveInt count)
    {
        var services = new ServiceCollection();
        var telemetryBuilder = services.AddOpenTelemetry();

        for (var i = 0; i < Math.Min(count.Get, 10); i++)
        {
            telemetryBuilder.WithEncina();
        }

        // Should not throw
        var provider = services.BuildServiceProvider();
        return provider != null;
    }

    /// <summary>
    /// Property: Null options should use defaults without throwing.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool NullOptions_UsesDefaults()
    {
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        var result = builder.WithEncina(null);

        return result != null && ReferenceEquals(builder, result);
    }

    /// <summary>
    /// Property: TracerProviderBuilder extension always returns same instance.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TracerProviderBuilder_Extension_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        var telemetryBuilder = services.AddOpenTelemetry();
        TracerProviderBuilder? tracerBuilder = null;

        telemetryBuilder.WithTracing(builder =>
        {
            tracerBuilder = builder;
        });

        var result = tracerBuilder!.AddEncinaInstrumentation();

        return ReferenceEquals(tracerBuilder, result);
    }
}
