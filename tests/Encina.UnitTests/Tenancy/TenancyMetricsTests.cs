using Encina.Tenancy.Diagnostics;
using Shouldly;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenancyMetrics"/>.
/// </summary>
public sealed class TenancyMetricsTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        var metrics = new TenancyMetrics();
        metrics.ShouldNotBeNull();
    }

    [Fact]
    public void RecordResolution_WithValidParams_DoesNotThrow()
    {
        // Arrange
        var metrics = new TenancyMetrics();

        // Act & Assert - should not throw
        metrics.RecordResolution("header", "success", 1.5);
    }

    [Fact]
    public void RecordResolution_MultipleRecordings_DoNotThrow()
    {
        // Arrange
        var metrics = new TenancyMetrics();

        // Act & Assert
        metrics.RecordResolution("header", "success", 0.5);
        metrics.RecordResolution("claim", "not_found", 2.0);
        metrics.RecordResolution("subdomain", "error", 15.3);
    }
}
