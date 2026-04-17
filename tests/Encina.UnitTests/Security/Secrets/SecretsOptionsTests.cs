using Encina.Security.Secrets;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsOptionsTests
{
    [Fact]
    public void EnableCaching_DefaultsToTrue()
    {
        var options = new SecretsOptions();

        options.EnableCaching.ShouldBeTrue();
    }

    [Fact]
    public void DefaultCacheDuration_DefaultsToFiveMinutes()
    {
        var options = new SecretsOptions();

        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void EnableAutoRotation_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableAutoRotation.ShouldBeFalse();
    }

    [Fact]
    public void RotationCheckInterval_DefaultsToNull()
    {
        var options = new SecretsOptions();

        options.RotationCheckInterval.ShouldBeNull();
    }

    [Fact]
    public void KeyPrefix_DefaultsToNull()
    {
        var options = new SecretsOptions();

        options.KeyPrefix.ShouldBeNull();
    }

    [Fact]
    public void ProviderHealthCheck_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.ProviderHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void EnableAccessAuditing_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableAccessAuditing.ShouldBeFalse();
    }

    [Fact]
    public void EnableFailover_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableFailover.ShouldBeFalse();
    }

    [Fact]
    public void EnableTracing_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableTracing.ShouldBeFalse();
    }

    [Fact]
    public void EnableMetrics_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableMetrics.ShouldBeFalse();
    }

    [Fact]
    public void EnableSecretInjection_DefaultsToFalse()
    {
        var options = new SecretsOptions();

        options.EnableSecretInjection.ShouldBeFalse();
    }

    [Fact]
    public void HealthCheckSecretName_DefaultsToNull()
    {
        var options = new SecretsOptions();

        options.HealthCheckSecretName.ShouldBeNull();
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

        options.EnableCaching.ShouldBeFalse();
        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(30));
        options.EnableAutoRotation.ShouldBeTrue();
        options.RotationCheckInterval.ShouldBe(TimeSpan.FromHours(1));
        options.KeyPrefix.ShouldBe("production/");
        options.ProviderHealthCheck.ShouldBeTrue();
        options.EnableAccessAuditing.ShouldBeTrue();
        options.EnableFailover.ShouldBeTrue();
        options.EnableTracing.ShouldBeTrue();
        options.EnableMetrics.ShouldBeTrue();
        options.EnableSecretInjection.ShouldBeTrue();
        options.HealthCheckSecretName.ShouldBe("health-probe");
    }
}
