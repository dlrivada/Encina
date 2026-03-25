using Encina.OpenTelemetry;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Tests for <see cref="EncinaOpenTelemetryOptions.EnableMessagingEnrichers"/> property.
/// </summary>
public sealed class EncinaOpenTelemetryOptionsEnableMessagingTests
{
    [Fact]
    public void EnableMessagingEnrichers_DefaultsToTrue()
    {
        var options = new EncinaOpenTelemetryOptions();
        options.EnableMessagingEnrichers.ShouldBeTrue();
    }

    [Fact]
    public void EnableMessagingEnrichers_CanBeSetToFalse()
    {
        var options = new EncinaOpenTelemetryOptions { EnableMessagingEnrichers = false };
        options.EnableMessagingEnrichers.ShouldBeFalse();
    }

    [Fact]
    public void EnableMessagingEnrichers_CanBeSetToTrue()
    {
        var options = new EncinaOpenTelemetryOptions { EnableMessagingEnrichers = true };
        options.EnableMessagingEnrichers.ShouldBeTrue();
    }
}
