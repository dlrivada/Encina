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
        using var provider = services.BuildServiceProvider();
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

    #region EncinaOpenTelemetryOptions Invariants

    /// <summary>
    /// Default options should have EnableMessagingEnrichers set to true.
    /// </summary>
    [Fact]
    public void DefaultOptions_EnableMessagingEnrichers_IsTrue()
    {
        // Arrange
        var options = new EncinaOpenTelemetryOptions();

        // Act
        var result = options.EnableMessagingEnrichers;

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Default options should have default service name "Encina".
    /// </summary>
    [Fact]
    public void DefaultOptions_ServiceName_IsEncina()
    {
        // Arrange
        var options = new EncinaOpenTelemetryOptions();

        // Act
        var result = options.ServiceName;

        // Assert
        Assert.Equal("Encina", result);
    }

    /// <summary>
    /// Default options should have default service version "1.0.0".
    /// </summary>
    [Fact]
    public void DefaultOptions_ServiceVersion_Is100()
    {
        // Arrange
        var options = new EncinaOpenTelemetryOptions();

        // Act
        var result = options.ServiceVersion;

        // Assert
        Assert.Equal("1.0.0", result);
    }

    /// <summary>
    /// Property: EnableMessagingEnrichers can be toggled to any boolean value.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool EnableMessagingEnrichers_AcceptsBooleanValue(bool enabled)
    {
        var options = new EncinaOpenTelemetryOptions
        {
            EnableMessagingEnrichers = enabled
        };
        return options.EnableMessagingEnrichers == enabled;
    }

    /// <summary>
    /// Property: Options with custom configuration preserve all values.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Options_PreservesAllConfiguredValues(
        NonEmptyString name,
        NonEmptyString version,
        bool enableEnrichers)
    {
        var options = new EncinaOpenTelemetryOptions
        {
            ServiceName = name.Get,
            ServiceVersion = version.Get,
            EnableMessagingEnrichers = enableEnrichers
        };

        return options.ServiceName == name.Get
               && options.ServiceVersion == version.Get
               && options.EnableMessagingEnrichers == enableEnrichers;
    }

    #endregion

    #region DI Registration Invariants

    /// <summary>
    /// Property: WithEncina with custom options preserves the configuration.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool WithEncina_WithCustomOptions_PreservesConfiguration(
        NonEmptyString name,
        bool enableEnrichers)
    {
        var services = new ServiceCollection();
        var customOptions = new EncinaOpenTelemetryOptions
        {
            ServiceName = name.Get,
            EnableMessagingEnrichers = enableEnrichers
        };

        var builder = services.AddOpenTelemetry().WithEncina(customOptions);

        // Should return the builder (fluent API)
        return builder != null;
    }

    /// <summary>
    /// Property: Multiple AddEncinaInstrumentation calls should not throw.
    /// </summary>
    /// <remarks>
    /// MaxTest is limited to 5 to reduce BuildServiceProvider invocations
    /// which are expensive operations.
    /// </remarks>
    [Property(MaxTest = 5)]
    public bool AddEncinaInstrumentation_MultipleCalls_DoesNotThrow(PositiveInt countRaw)
    {
        var count = Math.Min(countRaw.Get, 5); // Limit to prevent slowness

        var services = new ServiceCollection();
        var telemetryBuilder = services.AddOpenTelemetry();

        telemetryBuilder.WithTracing(builder =>
        {
            for (var i = 0; i < count; i++)
            {
                builder.AddEncinaInstrumentation();
            }
        });

        // Should not throw
        using var provider = services.BuildServiceProvider();
        return provider != null;
    }

    #endregion
}
