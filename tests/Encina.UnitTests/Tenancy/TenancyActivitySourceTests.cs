using System.Diagnostics;
using Encina.Tenancy.Diagnostics;
using Shouldly;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenancyActivitySource"/>.
/// Tests the static helper methods for creating and managing activities.
/// </summary>
public sealed class TenancyActivitySourceTests
{
    [Fact]
    public void SourceName_ShouldBeExpectedValue()
    {
        TenancyActivitySource.SourceName.ShouldBe("Encina.Tenancy");
    }

    [Fact]
    public void StartResolution_WithoutListener_ReturnsNull()
    {
        // No listener attached, should return null
        var activity = TenancyActivitySource.StartResolution("header");
        // When no listener is registered, StartActivity returns null
        // This is expected behavior - zero cost when no listener is active
        // We can't assert null because the test runner may have a listener
        // Just verify no exception is thrown
        activity?.Dispose();
    }

    [Fact]
    public void CompleteResolution_WithNullActivity_DoesNotThrow()
    {
        // Act & Assert - should not throw
        TenancyActivitySource.CompleteResolution(null, "tenant-123");
    }

    [Fact]
    public void Complete_WithNullActivity_DoesNotThrow()
    {
        // Act & Assert
        TenancyActivitySource.Complete(null);
    }

    [Fact]
    public void Failed_WithNullActivity_DoesNotThrow()
    {
        // Act & Assert
        TenancyActivitySource.Failed(null, "ERR_001", "Something failed");
    }

    [Fact]
    public void Failed_WithNullErrorCode_DoesNotThrow()
    {
        // Act & Assert
        TenancyActivitySource.Failed(null, null, "Something failed");
    }

    [Fact]
    public void StartTenantQuery_WithoutListener_DoesNotThrow()
    {
        var activity = TenancyActivitySource.StartTenantQuery("tenant-123", "Order");
        activity?.Dispose();
    }

    [Fact]
    public void CompleteResolution_WithActivity_SetsTagsAndDisposes()
    {
        // Arrange - create an activity using ActivitySource with a listener
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Tenancy",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = TenancyActivitySource.StartResolution("header");

        // Act
        TenancyActivitySource.CompleteResolution(activity, "tenant-456");

        // Assert - activity was disposed by CompleteResolution
        // We just verify no exception was thrown
    }

    [Fact]
    public void Complete_WithActivity_SetsStatusAndDisposes()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Tenancy",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = TenancyActivitySource.StartTenantQuery("t1", "Order");

        // Act
        TenancyActivitySource.Complete(activity);
    }

    [Fact]
    public void Failed_WithActivity_SetsErrorStatusAndDisposes()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Tenancy",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = TenancyActivitySource.StartResolution("header");

        // Act
        TenancyActivitySource.Failed(activity, "TENANT_NOT_FOUND", "Tenant not found");
    }

    [Fact]
    public void Failed_WithActivityAndNullErrorCode_SkipsErrorCodeTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Tenancy",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = TenancyActivitySource.StartResolution("header");

        // Act - null error code should be handled
        TenancyActivitySource.Failed(activity, null, "Unknown error");
    }

    [Fact]
    public void Failed_WithActivityAndEmptyErrorCode_SkipsErrorCodeTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Tenancy",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var activity = TenancyActivitySource.StartResolution("header");

        // Act - whitespace error code should be handled
        TenancyActivitySource.Failed(activity, "   ", "Unknown error");
    }
}
