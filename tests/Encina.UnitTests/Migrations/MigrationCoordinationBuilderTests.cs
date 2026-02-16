using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationCoordinationBuilder"/>.
/// Verifies the fluent builder correctly configures <see cref="MigrationCoordinationOptions"/>.
/// </summary>
public sealed class MigrationCoordinationBuilderTests
{
    #region UseStrategy

    [Fact]
    public void UseStrategy_SetsDefaultStrategy()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.UseStrategy(MigrationStrategy.CanaryFirst);
        var options = builder.Build();

        // Assert
        options.DefaultStrategy.ShouldBe(MigrationStrategy.CanaryFirst);
    }

    #endregion

    #region WithMaxParallelism

    [Fact]
    public void WithMaxParallelism_SetsMaxParallelism()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.WithMaxParallelism(16);
        var options = builder.Build();

        // Assert
        options.MaxParallelism.ShouldBe(16);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithMaxParallelism_ZeroOrNegative_ThrowsArgumentOutOfRangeException(int invalidValue)
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.WithMaxParallelism(invalidValue));
    }

    #endregion

    #region StopOnFirstFailure

    [Fact]
    public void StopOnFirstFailure_SetsStopOnFirstFailure()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.StopOnFirstFailure();
        var options = builder.Build();

        // Assert
        options.StopOnFirstFailure.ShouldBeTrue();
    }

    [Fact]
    public void StopOnFirstFailure_WithFalse_SetsStopOnFirstFailureToFalse()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.StopOnFirstFailure(false);
        var options = builder.Build();

        // Assert
        options.StopOnFirstFailure.ShouldBeFalse();
    }

    #endregion

    #region WithPerShardTimeout

    [Fact]
    public void WithPerShardTimeout_SetsTimeout()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();
        var timeout = TimeSpan.FromMinutes(10);

        // Act
        builder.WithPerShardTimeout(timeout);
        var options = builder.Build();

        // Assert
        options.PerShardTimeout.ShouldBe(timeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithPerShardTimeout_ZeroOrNegative_ThrowsArgumentOutOfRangeException(int seconds)
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();
        var invalidTimeout = TimeSpan.FromSeconds(seconds);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.WithPerShardTimeout(invalidTimeout));
    }

    #endregion

    #region ValidateBeforeApply

    [Fact]
    public void ValidateBeforeApply_SetsValidateBeforeApply()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.ValidateBeforeApply();
        var options = builder.Build();

        // Assert
        options.ValidateBeforeApply.ShouldBeTrue();
    }

    [Fact]
    public void ValidateBeforeApply_WithFalse_SetsValidateBeforeApplyToFalse()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.ValidateBeforeApply(false);
        var options = builder.Build();

        // Assert
        options.ValidateBeforeApply.ShouldBeFalse();
    }

    #endregion

    #region OnShardMigrated

    [Fact]
    public void OnShardMigrated_SetsCallback()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();
        var callbackCalled = false;
        Action<string, MigrationOutcome> callback = (_, _) => callbackCalled = true;

        // Act
        builder.OnShardMigrated(callback);
        var options = builder.Build();

        // Assert
        options.OnShardMigrated.ShouldNotBeNull();
        options.OnShardMigrated!("shard-0", MigrationOutcome.Succeeded);
        callbackCalled.ShouldBeTrue();
    }

    [Fact]
    public void OnShardMigrated_NullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.OnShardMigrated(null!));
    }

    #endregion

    #region WithDriftDetection

    [Fact]
    public void WithDriftDetection_ConfiguresOptions()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder.WithDriftDetection(drift =>
        {
            drift.ComparisonDepth = SchemaComparisonDepth.Full;
            drift.IncludeColumnDiffs = true;
            drift.IncludeIndexes = true;
            drift.IncludeConstraints = true;
            drift.CriticalTables = ["orders", "payments"];
            drift.BaselineShardId = "shard-0";
        });
        var options = builder.Build();

        // Assert
        options.DriftDetection.ComparisonDepth.ShouldBe(SchemaComparisonDepth.Full);
        options.DriftDetection.IncludeColumnDiffs.ShouldBeTrue();
        options.DriftDetection.IncludeIndexes.ShouldBeTrue();
        options.DriftDetection.IncludeConstraints.ShouldBeTrue();
        options.DriftDetection.CriticalTables.ShouldContain("orders");
        options.DriftDetection.CriticalTables.ShouldContain("payments");
        options.DriftDetection.BaselineShardId.ShouldBe("shard-0");
    }

    [Fact]
    public void WithDriftDetection_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.WithDriftDetection(null!));
    }

    #endregion

    #region Build

    [Fact]
    public void Build_ReturnsConfiguredOptions()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        builder
            .UseStrategy(MigrationStrategy.RollingUpdate)
            .WithMaxParallelism(8)
            .StopOnFirstFailure()
            .WithPerShardTimeout(TimeSpan.FromMinutes(10))
            .ValidateBeforeApply();

        var options = builder.Build();

        // Assert
        options.DefaultStrategy.ShouldBe(MigrationStrategy.RollingUpdate);
        options.MaxParallelism.ShouldBe(8);
        options.StopOnFirstFailure.ShouldBeTrue();
        options.PerShardTimeout.ShouldBe(TimeSpan.FromMinutes(10));
        options.ValidateBeforeApply.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithDefaults_ReturnsDefaultOptions()
    {
        // Arrange
        var builder = new MigrationCoordinationBuilder();

        // Act
        var options = builder.Build();

        // Assert
        options.DefaultStrategy.ShouldBe(MigrationStrategy.Sequential);
        options.MaxParallelism.ShouldBe(4);
        options.StopOnFirstFailure.ShouldBeTrue();
        options.PerShardTimeout.ShouldBe(TimeSpan.FromMinutes(5));
        options.ValidateBeforeApply.ShouldBeTrue();
        options.OnShardMigrated.ShouldBeNull();
    }

    #endregion
}
