using Encina.Messaging.Health;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.Messaging.PropertyTests.Health;

/// <summary>
/// Property-based tests for health check invariants.
/// </summary>
public sealed class HealthCheckPropertyTests
{
    /// <summary>
    /// HealthCheckResult.Healthy should always have Healthy status.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool HealthCheckResult_Healthy_AlwaysReturnsHealthyStatus(NonEmptyString description)
    {
        var result = HealthCheckResult.Healthy(description.Get);
        return result.Status == HealthStatus.Healthy;
    }

    /// <summary>
    /// HealthCheckResult.Unhealthy should always have Unhealthy status.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool HealthCheckResult_Unhealthy_AlwaysReturnsUnhealthyStatus(NonEmptyString description)
    {
        var result = HealthCheckResult.Unhealthy(description.Get);
        return result.Status == HealthStatus.Unhealthy;
    }

    /// <summary>
    /// HealthCheckResult.Degraded should always have Degraded status.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool HealthCheckResult_Degraded_AlwaysReturnsDegradedStatus(NonEmptyString description)
    {
        var result = HealthCheckResult.Degraded(description.Get);
        return result.Status == HealthStatus.Degraded;
    }

    /// <summary>
    /// HealthCheckResult should preserve the description provided.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool HealthCheckResult_PreservesDescription(NonEmptyString description, int statusIndex)
    {
        var status = (HealthStatus)(Math.Abs(statusIndex) % 3);
        var result = status switch
        {
            HealthStatus.Healthy => HealthCheckResult.Healthy(description.Get),
            HealthStatus.Unhealthy => HealthCheckResult.Unhealthy(description.Get),
            HealthStatus.Degraded => HealthCheckResult.Degraded(description.Get),
            _ => HealthCheckResult.Healthy(description.Get)
        };

        return result.Description == description.Get;
    }

    /// <summary>
    /// ProviderHealthCheckOptions should preserve Name when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ProviderHealthCheckOptions_PreservesName(NonEmptyString name)
    {
        var options = new ProviderHealthCheckOptions { Name = name.Get };
        return options.Name == name.Get;
    }

    /// <summary>
    /// ProviderHealthCheckOptions should preserve Tags when set.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ProviderHealthCheckOptions_PreservesTags(List<string> tags)
    {
        tags ??= [];
        var validTags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

        var options = new ProviderHealthCheckOptions { Tags = validTags };
        return options.Tags.SequenceEqual(validTags);
    }

    /// <summary>
    /// ProviderHealthCheckOptions.Timeout should always be non-negative when set to valid values.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ProviderHealthCheckOptions_TimeoutIsNonNegative(PositiveInt milliseconds)
    {
        var timeout = TimeSpan.FromMilliseconds(milliseconds.Get);
        var options = new ProviderHealthCheckOptions { Timeout = timeout };
        return options.Timeout >= TimeSpan.Zero;
    }

    /// <summary>
    /// ProviderHealthCheckOptions default Enabled should be true.
    /// </summary>
    [Fact]
    public void ProviderHealthCheckOptions_DefaultEnabled_IsTrue()
    {
        var options = new ProviderHealthCheckOptions();
        Assert.True(options.Enabled);
    }

    /// <summary>
    /// ProviderHealthCheckOptions.Enabled should toggle correctly.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ProviderHealthCheckOptions_EnabledToggle(bool enabled)
    {
        var options = new ProviderHealthCheckOptions { Enabled = enabled };
        return options.Enabled == enabled;
    }

    /// <summary>
    /// HealthStatus enum should have exactly 3 values.
    /// </summary>
    [Fact]
    public void HealthStatus_HasExactly3Values()
    {
        var values = Enum.GetValues<HealthStatus>();
        Assert.Equal(3, values.Length);
        Assert.Contains(HealthStatus.Healthy, values);
        Assert.Contains(HealthStatus.Unhealthy, values);
        Assert.Contains(HealthStatus.Degraded, values);
    }

    /// <summary>
    /// HealthCheckResult with null description should not throw.
    /// </summary>
    [Fact]
    public void HealthCheckResult_NullDescription_DoesNotThrow()
    {
        var result = HealthCheckResult.Healthy(null);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
    }

    /// <summary>
    /// HealthCheckResult.Exception should be preserved when provided.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool HealthCheckResult_PreservesException(NonEmptyString message)
    {
        var exception = new InvalidOperationException(message.Get);
        var result = HealthCheckResult.Unhealthy("Test failure", exception);

        return result.Exception is not null
               && result.Exception.Message == message.Get;
    }
}
