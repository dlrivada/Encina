using Encina.Security.Secrets;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;

namespace Encina.GuardTests.Security.Secrets;

/// <summary>
/// Guard clause tests for <see cref="SecretsOptions"/>.
/// Verifies that default values are sensible and nested option objects are not null.
/// </summary>
public sealed class SecretsOptionsGuardTests
{
    #region Default Values

    [Fact]
    public void Defaults_CachingIsNotNull()
    {
        var options = new SecretsOptions();

        options.Caching.Should().NotBeNull("Caching sub-options must be pre-initialized");
    }

    [Fact]
    public void Defaults_ResilienceIsNotNull()
    {
        var options = new SecretsOptions();

        options.Resilience.Should().NotBeNull("Resilience sub-options must be pre-initialized");
    }

    [Fact]
    public void Defaults_DefaultCacheDuration_IsPositive()
    {
        var options = new SecretsOptions();

        options.DefaultCacheDuration.Should().BeGreaterThan(TimeSpan.Zero,
            "Default cache duration must be positive");
    }

    [Fact]
    public void Defaults_EnableCaching_IsTrue()
    {
        var options = new SecretsOptions();

        options.EnableCaching.Should().BeTrue(
            "Caching should be enabled by default");
    }

    [Fact]
    public void Defaults_EnableResilience_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableResilience.Should().BeFalse(
            "Resilience must be opt-in");
    }

    [Fact]
    public void Defaults_EnableMetrics_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableMetrics.Should().BeFalse(
            "Metrics must be opt-in");
    }

    [Fact]
    public void Defaults_EnableSecretInjection_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableSecretInjection.Should().BeFalse(
            "Secret injection must be opt-in");
    }

    [Fact]
    public void Defaults_EnableAccessAuditing_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableAccessAuditing.Should().BeFalse(
            "Access auditing must be opt-in");
    }

    [Fact]
    public void Defaults_ProviderHealthCheck_IsFalse()
    {
        var options = new SecretsOptions();

        options.ProviderHealthCheck.Should().BeFalse(
            "Provider health check must be opt-in");
    }

    #endregion

    #region Nested Options Defaults

    [Fact]
    public void CachingDefaults_CacheKeyPrefix_IsNotEmpty()
    {
        var caching = new SecretCachingOptions();

        caching.CacheKeyPrefix.Should().NotBeNullOrWhiteSpace(
            "Cache key prefix must have a sensible default");
    }

    [Fact]
    public void CachingDefaults_InvalidationChannel_IsNotEmpty()
    {
        var caching = new SecretCachingOptions();

        caching.InvalidationChannel.Should().NotBeNullOrWhiteSpace(
            "Invalidation channel must have a sensible default");
    }

    [Fact]
    public void ResilienceDefaults_MaxRetryAttempts_IsPositive()
    {
        var resilience = new SecretsResilienceOptions();

        resilience.MaxRetryAttempts.Should().BeGreaterThan(0,
            "Default retry attempts must be positive");
    }

    [Fact]
    public void ResilienceDefaults_OperationTimeout_IsPositive()
    {
        var resilience = new SecretsResilienceOptions();

        resilience.OperationTimeout.Should().BeGreaterThan(TimeSpan.Zero,
            "Default operation timeout must be positive");
    }

    #endregion
}
