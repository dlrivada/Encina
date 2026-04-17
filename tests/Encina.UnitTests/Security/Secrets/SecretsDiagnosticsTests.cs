using System.Diagnostics;
using Encina.Security.Secrets.Diagnostics;
using Shouldly;

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
        SecretsDiagnostics.SourceName.ShouldBe(SecretsActivitySource.SourceName);
    }

    [Fact]
    public void SourceVersion_DelegatesToSecretsActivitySource()
    {
        SecretsDiagnostics.SourceVersion.ShouldBe(SecretsActivitySource.SourceVersion);
    }

    [Fact]
    public void ActivitySource_IsSameAsSecretsActivitySource()
    {
        SecretsDiagnostics.ActivitySource.ShouldBeSameAs(SecretsActivitySource.Source);
    }

    [Fact]
    public void TagConstants_DelegateToSecretsActivitySource()
    {
        SecretsDiagnostics.TagSecretName.ShouldBe(SecretsActivitySource.TagSecretName);
        SecretsDiagnostics.TagOperation.ShouldBe(SecretsActivitySource.TagOperation);
        SecretsDiagnostics.TagProvider.ShouldBe(SecretsActivitySource.TagProviderType);
        SecretsDiagnostics.TagOutcome.ShouldBe(SecretsActivitySource.TagOutcome);
        SecretsDiagnostics.TagCached.ShouldBe(SecretsActivitySource.TagCacheHit);
        SecretsDiagnostics.TagRequestType.ShouldBe(SecretsActivitySource.TagRequestType);
    }

    [Fact]
    public void TagPropertyName_HasCorrectValue()
    {
        SecretsDiagnostics.TagPropertyName.ShouldBe("secrets.property_name");
    }

    #endregion

    #region Static Counters

    [Fact]
    public void OperationsTotal_IsNotNull()
    {
        SecretsDiagnostics.OperationsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void FailuresTotal_IsNotNull()
    {
        SecretsDiagnostics.FailuresTotal.ShouldNotBeNull();
    }

    [Fact]
    public void CacheHitsTotal_IsNotNull()
    {
        SecretsDiagnostics.CacheHitsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void CacheMissesTotal_IsNotNull()
    {
        SecretsDiagnostics.CacheMissesTotal.ShouldNotBeNull();
    }

    [Fact]
    public void OperationDuration_IsNotNull()
    {
        SecretsDiagnostics.OperationDuration.ShouldNotBeNull();
    }

    [Fact]
    public void InjectionsTotal_IsNotNull()
    {
        SecretsDiagnostics.InjectionsTotal.ShouldNotBeNull();
    }

    [Fact]
    public void PropertiesInjected_IsNotNull()
    {
        SecretsDiagnostics.PropertiesInjected.ShouldNotBeNull();
    }

    #endregion

    #region Activity Delegation

    [Fact]
    public void StartGetSecret_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartGetSecret("test-secret");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe(SecretsActivitySource.GetSecretActivity);
    }

    [Fact]
    public void StartSetSecret_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartSetSecret("test-secret");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe(SecretsActivitySource.SetSecretActivity);
    }

    [Fact]
    public void StartRotateSecret_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartRotateSecret("test-secret");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe(SecretsActivitySource.RotateSecretActivity);
    }

    [Fact]
    public void StartSecretInjection_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartSecretInjection("MyRequest");

        activity.ShouldNotBeNull();
        activity!.OperationName.ShouldBe(SecretsActivitySource.InjectSecretsActivity);
    }

    #endregion

    #region Record Methods Delegation

    [Fact]
    public void RecordSuccess_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartGetSecret("test-secret");

        var act = () => SecretsDiagnostics.RecordSuccess(activity);

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordFailure_DelegatesToSecretsActivitySource()
    {
        using var activity = SecretsDiagnostics.StartGetSecret("test-secret");

        var act = () => SecretsDiagnostics.RecordFailure(activity, "error message");

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordSuccess_NullActivity_DoesNotThrow()
    {
        var act = () => SecretsDiagnostics.RecordSuccess(null);

        Should.NotThrow(act);
    }

    [Fact]
    public void RecordFailure_NullActivity_DoesNotThrow()
    {
        var act = () => SecretsDiagnostics.RecordFailure(null, "error");

        Should.NotThrow(act);
    }

    #endregion
}
