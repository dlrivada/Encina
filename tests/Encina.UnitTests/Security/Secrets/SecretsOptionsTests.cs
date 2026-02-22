using Encina.Security.Secrets;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsOptionsTests
{
    [Fact]
    public void EnableCaching_DefaultsToTrue()
    {
        var options = new SecretsOptions();

        options.EnableCaching.Should().BeTrue();
    }

    [Fact]
    public void DefaultCacheDuration_DefaultsToFiveMinutes()
    {
        var options = new SecretsOptions();

        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void EnableAutoRotation_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableAutoRotation.Should().BeFalse();
    }

    [Fact]
    public void RotationCheckInterval_DefaultsToNull()
    {
        var options = new SecretsOptions();

        options.RotationCheckInterval.Should().BeNull();
    }

    [Fact]
    public void KeyPrefix_DefaultsToNull()
    {
        var options = new SecretsOptions();

        options.KeyPrefix.Should().BeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.ProviderHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void EnableAccessAuditing_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableAccessAuditing.Should().BeFalse();
    }

    [Fact]
    public void EnableFailover_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableFailover.Should().BeFalse();
    }

    [Fact]
    public void EnableTracing_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableTracing.Should().BeFalse();
    }

    [Fact]
    public void EnableMetrics_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void EnableSecretInjection_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableSecretInjection.Should().BeFalse();
    }

    [Fact]
    public void HealthCheckSecretName_DefaultsToNull()
    {
        var options = new SecretsOptions();

        options.HealthCheckSecretName.Should().BeNull();
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var options = new SecretsOptions
        {
            EnableCaching = false,
            DefaultCacheDuration = TimeSpan.FromMinutes(30),
            EnableAutoRotation = true,
            RotationCheckInterval = TimeSpan.FromHours(1),
            KeyPrefix = "production/",
            ProviderHealthCheck = true,
            EnableAccessAuditing = true,
            EnableFailover = true,
            EnableTracing = true,
            EnableMetrics = true,
            EnableSecretInjection = true,
            HealthCheckSecretName = "health-probe"
        };

        options.EnableCaching.Should().BeFalse();
        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(30));
        options.EnableAutoRotation.Should().BeTrue();
        options.RotationCheckInterval.Should().Be(TimeSpan.FromHours(1));
        options.KeyPrefix.Should().Be("production/");
        options.ProviderHealthCheck.Should().BeTrue();
        options.EnableAccessAuditing.Should().BeTrue();
        options.EnableFailover.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.EnableSecretInjection.Should().BeTrue();
        options.HealthCheckSecretName.Should().Be("health-probe");
    }
}
