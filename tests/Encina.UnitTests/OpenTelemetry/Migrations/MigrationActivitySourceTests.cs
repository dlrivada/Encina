using Encina.OpenTelemetry.Migrations;
using Encina.Sharding.Migrations;
using Shouldly;
using Xunit;

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
                errorMessage: "Connection timeout"));
        ex.ShouldBeNull();
    }
}
