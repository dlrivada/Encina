using System.Diagnostics;
using Encina.OpenTelemetry.Migrations;
using Encina.Sharding.Migrations;
using Shouldly;
using Xunit;
using OtelTags = Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationActivitySource"/>.
/// </summary>
public sealed class MigrationActivitySourceTests
{
    [Fact]
    public void SourceName_HasCorrectValue()
    {
        MigrationActivitySource.SourceName.ShouldBe("Encina.Migration");
    }

    [Fact]
    public void StartMigrationCoordination_WithoutListeners_ReturnsNullOrActivity()
    {
        // In parallel test runs, other tests may register global ActivityListeners,
        // causing this to return a non-null activity. Both outcomes are valid.
        var activity = MigrationActivitySource.StartMigrationCoordination(
            Guid.NewGuid(), MigrationStrategy.Sequential, 5);

        activity?.Dispose();
    }

    [Fact]
    public void StartShardMigration_WithoutListeners_ReturnsNullOrActivity()
    {
        // In parallel test runs, other tests may register global ActivityListeners,
        // causing this to return a non-null activity. Both outcomes are valid.
        var activity = MigrationActivitySource.StartShardMigration(
            "shard-1", Guid.NewGuid());

        activity?.Dispose();
    }

    [Fact]
    public void Complete_NullActivity_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            MigrationActivitySource.Complete(null, MigrationOutcome.Succeeded));
        ex.ShouldBeNull();
    }

    [Fact]
    public void Complete_NullActivity_WithDurationAndError_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            MigrationActivitySource.Complete(
                null,
                MigrationOutcome.Failed,
                durationMs: 1234.5,
                errorCode: "migration.connection_timeout"));
        ex.ShouldBeNull();
    }

    [Theory]
    [InlineData(MigrationOutcome.Succeeded, ActivityStatusCode.Ok)]
    [InlineData(MigrationOutcome.Failed, ActivityStatusCode.Error)]
    [InlineData(MigrationOutcome.RolledBack, ActivityStatusCode.Error)]
    [InlineData(MigrationOutcome.Pending, ActivityStatusCode.Unset)]
    [InlineData(MigrationOutcome.InProgress, ActivityStatusCode.Unset)]
    public void Complete_WithListener_SetsStatusFromOutcome(MigrationOutcome outcome, ActivityStatusCode expected)
    {
        using var listener = NewListener();
        using var activity = MigrationActivitySource.StartShardMigration("shard-1", Guid.NewGuid());
        activity.ShouldNotBeNull();

        MigrationActivitySource.Complete(activity, outcome, durationMs: 12.5, errorCode: "migration.failed");

        activity.Status.ShouldBe(expected);
        activity.StatusDescription.ShouldBe(expected == ActivityStatusCode.Error ? "migration.failed" : null);
        activity.TagObjects.ShouldContain(t => t.Key == OtelTags.Migration.Error && (string?)t.Value == "migration.failed");
        activity.TagObjects.ShouldContain(t => t.Key == OtelTags.Migration.DurationMs);
    }

    [Fact]
    public void Complete_WithoutErrorCode_DoesNotSetErrorTag()
    {
        using var listener = NewListener();
        using var activity = MigrationActivitySource.StartShardMigration("shard-1", Guid.NewGuid());
        activity.ShouldNotBeNull();

        MigrationActivitySource.Complete(activity, MigrationOutcome.Succeeded);

        activity.TagObjects.ShouldNotContain(t => t.Key == OtelTags.Migration.Error);
    }

    private static ActivityListener NewListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MigrationActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
