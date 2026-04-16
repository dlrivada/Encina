using Encina.Security.Secrets;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Resilience;
using Shouldly;

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

        options.Caching.ShouldNotBeNull("Caching sub-options must be pre-initialized");
    }

    [Fact]
    public void Defaults_ResilienceIsNotNull()
    {
        var options = new SecretsOptions();

        options.Resilience.ShouldNotBeNull("Resilience sub-options must be pre-initialized");
    }

    [Fact]
    public void Defaults_DefaultCacheDuration_IsPositive()
    {
        var options = new SecretsOptions();

        options.DefaultCacheDuration.ShouldBeGreaterThan(TimeSpan.Zero,
            "Default cache duration must be positive");
    }

    [Fact]
    public void Defaults_EnableCaching_IsTrue()
    {
        var options = new SecretsOptions();

        options.EnableCaching.ShouldBeTrue("Caching should be enabled by default");
    }

    [Fact]
    public void Defaults_EnableResilience_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableResilience.ShouldBeFalse("Resilience must be opt-in");
    }

    [Fact]
    public void Defaults_EnableMetrics_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableMetrics.ShouldBeFalse("Metrics must be opt-in");
    }

    [Fact]
    public void Defaults_EnableSecretInjection_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableSecretInjection.ShouldBeFalse("Secret injection must be opt-in");
    }

    [Fact]
    public void Defaults_EnableAccessAuditing_IsFalse()
    {
        var options = new SecretsOptions();

        options.EnableAccessAuditing.ShouldBeFalse("Access auditing must be opt-in");
    }

    [Fact]
    public void Defaults_ProviderHealthCheck_IsFalse()
    {
        var options = new SecretsOptions();

        options.ProviderHealthCheck.ShouldBeFalse("Provider health check must be opt-in");
    }

    #endregion

    #region Nested Options Defaults

    [Fact]
    public void CachingDefaults_CacheKeyPrefix_IsNotEmpty()
    {
        var caching = new SecretCachingOptions();

        caching.CacheKeyPrefix.ShouldNotBeNullOrWhiteSpace("Cache key prefix must have a sensible default");
    }

    [Fact]
    public void CachingDefaults_InvalidationChannel_IsNotEmpty()
    {
        var caching = new SecretCachingOptions();

        caching.InvalidationChannel.ShouldNotBeNullOrWhiteSpace("Invalidation channel must have a sensible default");
    }

    [Fact]
    public void ResilienceDefaults_MaxRetryAttempts_IsPositive()
    {
        var resilience = new SecretsResilienceOptions();

        resilience.MaxRetryAttempts.ShouldBeGreaterThan(0,
            "Default retry attempts must be positive");
    }

    [Fact]
    public void ResilienceDefaults_OperationTimeout_IsPositive()
    {
        var resilience = new SecretsResilienceOptions();

        resilience.OperationTimeout.ShouldBeGreaterThan(TimeSpan.Zero,
            "Default operation timeout must be positive");
    }

    #endregion
}
