using FluentAssertions;
using Xunit;

namespace Encina.OpenTelemetry.Tests;

/// <summary>
/// Tests for <see cref="EncinaOpenTelemetryOptions"/>.
/// </summary>
public sealed class EncinaOpenTelemetryOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var options = new EncinaOpenTelemetryOptions();

        // Assert
        options.ServiceName.Should().Be("Encina");
        options.ServiceVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void ServiceName_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaOpenTelemetryOptions();
        const string serviceName = "MyCustomService";

        // Act
        options.ServiceName = serviceName;

        // Assert
        options.ServiceName.Should().Be(serviceName);
    }

    [Fact]
    public void ServiceVersion_ShouldBeSettable()
    {
        // Arrange
        var options = new EncinaOpenTelemetryOptions();
        const string version = "2.0.0";

        // Act
        options.ServiceVersion = version;

        // Assert
        options.ServiceVersion.Should().Be(version);
    }
}
