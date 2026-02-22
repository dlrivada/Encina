using System.Diagnostics;
using Encina.Security.Secrets.Diagnostics;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsDiagnosticsTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public SecretsDiagnosticsTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SecretsActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        foreach (var activity in _activities)
        {
            activity.Dispose();
        }
    }

    #region Constants Delegation

    [Fact]
    public void SourceName_DelegatesToSecretsActivitySource()
    {
        SecretsDiagnostics.SourceName.Should().Be(SecretsActivitySource.SourceName);
    }

    [Fact]
    public void SourceVersion_DelegatesToSecretsActivitySource()
    {
        SecretsDiagnostics.SourceVersion.Should().Be(SecretsActivitySource.SourceVersion);
    }

    [Fact]
    public void ActivitySource_IsSameAsSecretsActivitySource()
    {
        SecretsDiagnostics.ActivitySource.Should().BeSameAs(SecretsActivitySource.Source);
    }

    [Fact]
    public void TagConstants_DelegateToSecretsActivitySource()
    {
        SecretsDiagnostics.TagSecretName.Should().Be(SecretsActivitySource.TagSecretName);
        SecretsDiagnostics.TagOperation.Should().Be(SecretsActivitySource.TagOperation);
        SecretsDiagnostics.TagProvider.Should().Be(SecretsActivitySource.TagProviderType);
        SecretsDiagnostics.TagOutcome.Should().Be(SecretsActivitySource.TagOutcome);
        SecretsDiagnostics.TagCached.Should().Be(SecretsActivitySource.TagCacheHit);
        SecretsDiagnostics.TagRequestType.Should().Be(SecretsActivitySource.TagRequestType);
    }

    [Fact]
    public void TagPropertyName_HasCorrectValue()
    {
        SecretsDiagnostics.TagPropertyName.Should().Be("secrets.property_name");
    }

    #endregion

    #region Static Counters

    [Fact]
    public void OperationsTotal_IsNotNull()
    {
        SecretsDiagnostics.OperationsTotal.Should().NotBeNull();
    }

    [Fact]
    public void FailuresTotal_IsNotNull()
    {
        SecretsDiagnostics.FailuresTotal.Should().NotBeNull();
    }

    [Fact]
    public void CacheHitsTotal_IsNotNull()
    {
        SecretsDiagnostics.CacheHitsTotal.Should().NotBeNull();
    }

    [Fact]
    public void CacheMissesTotal_IsNotNull()
    {
        SecretsDiagnostics.CacheMissesTotal.Should().NotBeNull();
    }

    [Fact]
    public void OperationDuration_IsNotNull()
    {
        SecretsDiagnostics.OperationDuration.Should().NotBeNull();
    }

    [Fact]
    public void InjectionsTotal_IsNotNull()
    {
        SecretsDiagnostics.InjectionsTotal.Should().NotBeNull();
    }

    [Fact]
    public void PropertiesInjected_IsNotNull()
    {
        SecretsDiagnostics.PropertiesInjected.Should().NotBeNull();
    }

    #endregion

    #region Activity Delegation

    [Fact]
    public void StartGetSecret_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartGetSecret("test-secret");

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be(SecretsActivitySource.GetSecretActivity);
    }

    [Fact]
    public void StartSetSecret_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartSetSecret("test-secret");

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be(SecretsActivitySource.SetSecretActivity);
    }

    [Fact]
    public void StartRotateSecret_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartRotateSecret("test-secret");

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be(SecretsActivitySource.RotateSecretActivity);
    }

    [Fact]
    public void StartSecretInjection_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartSecretInjection("MyRequest");

        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be(SecretsActivitySource.InjectSecretsActivity);
    }

    #endregion

    #region Record Methods Delegation

    [Fact]
    public void RecordSuccess_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartGetSecret("test-secret");

        var act = () => SecretsDiagnostics.RecordSuccess(activity);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFailure_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartGetSecret("test-secret");

        var act = () => SecretsDiagnostics.RecordFailure(activity, "error message");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordSuccess_NullActivity_DoesNotThrow()
    {
        var act = () => SecretsDiagnostics.RecordSuccess(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFailure_NullActivity_DoesNotThrow()
    {
        var act = () => SecretsDiagnostics.RecordFailure(null, "error");

        act.Should().NotThrow();
    }

    #endregion
}
