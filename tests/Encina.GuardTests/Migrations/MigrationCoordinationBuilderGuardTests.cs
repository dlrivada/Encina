using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="MigrationCoordinationBuilder"/> fluent builder methods.
/// Validates that invalid arguments are rejected with the appropriate exceptions.
/// </summary>
public sealed class MigrationCoordinationBuilderGuardTests
{
    #region WithMaxParallelism Guards

    [Fact]
    public void WithMaxParallelism_Zero_ThrowsArgumentOutOfRangeException()
    {
        var builder = new MigrationCoordinationBuilder();

        var ex = Should.Throw<ArgumentOutOfRangeException>(() => builder.WithMaxParallelism(0));
        ex.ParamName.ShouldBe("maxParallelism");
    }

    [Fact]
    public void WithMaxParallelism_Negative_ThrowsArgumentOutOfRangeException()
    {
        var builder = new MigrationCoordinationBuilder();

        var ex = Should.Throw<ArgumentOutOfRangeException>(() => builder.WithMaxParallelism(-5));
        ex.ParamName.ShouldBe("maxParallelism");
    }

    [Fact]
    public void WithMaxParallelism_ValidValue_ReturnsBuilder()
    {
        var builder = new MigrationCoordinationBuilder();

        var result = builder.WithMaxParallelism(8);

        result.ShouldBeSameAs(builder);
    }

    #endregion

    #region WithPerShardTimeout Guards

    [Fact]
    public void WithPerShardTimeout_Zero_ThrowsArgumentOutOfRangeException()
    {
        var builder = new MigrationCoordinationBuilder();

        var ex = Should.Throw<ArgumentOutOfRangeException>(() => builder.WithPerShardTimeout(TimeSpan.Zero));
        ex.ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void WithPerShardTimeout_Negative_ThrowsArgumentOutOfRangeException()
    {
        var builder = new MigrationCoordinationBuilder();

        var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
            builder.WithPerShardTimeout(TimeSpan.FromMinutes(-1)));
        ex.ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void WithPerShardTimeout_ValidValue_ReturnsBuilder()
    {
        var builder = new MigrationCoordinationBuilder();

        var result = builder.WithPerShardTimeout(TimeSpan.FromMinutes(10));

        result.ShouldBeSameAs(builder);
    }

    #endregion

    #region OnShardMigrated Guards

    [Fact]
    public void OnShardMigrated_Null_ThrowsArgumentNullException()
    {
        var builder = new MigrationCoordinationBuilder();

        var ex = Should.Throw<ArgumentNullException>(() => builder.OnShardMigrated(null!));
        ex.ParamName.ShouldBe("callback");
    }

    [Fact]
    public void OnShardMigrated_ValidCallback_ReturnsBuilder()
    {
        var builder = new MigrationCoordinationBuilder();

        var result = builder.OnShardMigrated((_, _) => { });

        result.ShouldBeSameAs(builder);
    }

    #endregion

    #region WithDriftDetection Guards

    [Fact]
    public void WithDriftDetection_Null_ThrowsArgumentNullException()
    {
        var builder = new MigrationCoordinationBuilder();

        var ex = Should.Throw<ArgumentNullException>(() => builder.WithDriftDetection(null!));
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void WithDriftDetection_ValidAction_ReturnsBuilder()
    {
        var builder = new MigrationCoordinationBuilder();

        var result = builder.WithDriftDetection(opts => opts.IncludeColumnDiffs = true);

        result.ShouldBeSameAs(builder);
    }

    #endregion
}
