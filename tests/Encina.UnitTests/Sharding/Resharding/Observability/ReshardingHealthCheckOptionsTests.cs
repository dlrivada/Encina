using Encina.OpenTelemetry.Resharding;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingHealthCheckOptions"/>.
/// Validates default property values and property setters.
/// </summary>
public sealed class ReshardingHealthCheckOptionsTests
{
    #region Default Values

    [Fact]
    public void MaxReshardingDuration_Default_IsTwoHours()
    {
        // Arrange & Act
        var sut = new ReshardingHealthCheckOptions();

        // Assert
        sut.MaxReshardingDuration.ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Timeout_Default_IsThirtySeconds()
    {
        // Arrange & Act
        var sut = new ReshardingHealthCheckOptions();

        // Assert
        sut.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Property Setters

    [Fact]
    public void MaxReshardingDuration_SetCustomValue_RetainsValue()
    {
        // Arrange
        var sut = new ReshardingHealthCheckOptions();
        var customDuration = TimeSpan.FromHours(8);

        // Act
        sut.MaxReshardingDuration = customDuration;

        // Assert
        sut.MaxReshardingDuration.ShouldBe(customDuration);
    }

    [Fact]
    public void Timeout_SetCustomValue_RetainsValue()
    {
        // Arrange
        var sut = new ReshardingHealthCheckOptions();
        var customTimeout = TimeSpan.FromSeconds(5);

        // Act
        sut.Timeout = customTimeout;

        // Assert
        sut.Timeout.ShouldBe(customTimeout);
    }

    #endregion
}
