using Encina.OpenTelemetry;
using Encina.Testing;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Tests for <see cref="EncinaOpenTelemetryOptions"/>.
/// </summary>
public sealed class EncinaOpenTelemetryOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange
        // No setup required - testing default constructor

        // Act
        var options = new EncinaOpenTelemetryOptions();

        // Assert
        options.ServiceName.ShouldBe("Encina");
        options.ServiceVersion.ShouldBe("1.0.0");
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
        options.ServiceName.ShouldBe(serviceName);
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
        options.ServiceVersion.ShouldBe(version);
    }
}
