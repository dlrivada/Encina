using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Core.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationOptions"/> and <see cref="MigrationErrorCodes"/>.
/// </summary>
public sealed class MigrationOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        // Act
        var options = new MigrationOptions();

        // Assert
        options.Strategy.ShouldBe(MigrationStrategy.Sequential);
        options.MaxParallelism.ShouldBe(4);
        options.StopOnFirstFailure.ShouldBeTrue();
        options.PerShardTimeout.ShouldBe(TimeSpan.FromMinutes(5));
        options.ValidateBeforeApply.ShouldBeTrue();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Act
        var options = new MigrationOptions
        {
            Strategy = MigrationStrategy.CanaryFirst,
            MaxParallelism = 8,
            StopOnFirstFailure = false,
            PerShardTimeout = TimeSpan.FromMinutes(10),
            ValidateBeforeApply = false
        };

        // Assert
        options.Strategy.ShouldBe(MigrationStrategy.CanaryFirst);
        options.MaxParallelism.ShouldBe(8);
        options.StopOnFirstFailure.ShouldBeFalse();
        options.PerShardTimeout.ShouldBe(TimeSpan.FromMinutes(10));
        options.ValidateBeforeApply.ShouldBeFalse();
    }
}

/// <summary>
/// Unit tests for <see cref="MigrationErrorCodes"/> constants.
/// </summary>
public sealed class MigrationErrorCodesTests
{
    [Fact]
    public void MigrationFailed_HasExpectedValue()
    {
        MigrationErrorCodes.MigrationFailed.ShouldBe("encina.sharding.migration.migration_failed");
    }

    [Fact]
    public void MigrationTimeout_HasExpectedValue()
    {
        MigrationErrorCodes.MigrationTimeout.ShouldBe("encina.sharding.migration.migration_timeout");
    }

    [Fact]
    public void RollbackFailed_HasExpectedValue()
    {
        MigrationErrorCodes.RollbackFailed.ShouldBe("encina.sharding.migration.rollback_failed");
    }

    [Fact]
    public void DriftDetected_HasExpectedValue()
    {
        MigrationErrorCodes.DriftDetected.ShouldBe("encina.sharding.migration.drift_detected");
    }

    [Fact]
    public void InvalidScript_HasExpectedValue()
    {
        MigrationErrorCodes.InvalidScript.ShouldBe("encina.sharding.migration.invalid_script");
    }

    [Fact]
    public void SchemaComparisonFailed_HasExpectedValue()
    {
        MigrationErrorCodes.SchemaComparisonFailed.ShouldBe("encina.sharding.migration.schema_comparison_failed");
    }

    [Fact]
    public void MigrationNotFound_HasExpectedValue()
    {
        MigrationErrorCodes.MigrationNotFound.ShouldBe("encina.sharding.migration.migration_not_found");
    }
}
