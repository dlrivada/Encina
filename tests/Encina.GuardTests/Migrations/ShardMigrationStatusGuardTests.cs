using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="ShardMigrationStatus"/> record constructor validation.
/// ShardId validates non-null/non-whitespace with <see cref="ArgumentException"/>.
/// </summary>
public sealed class ShardMigrationStatusGuardTests
{
    [Fact]
    public void Constructor_NullShardId_ThrowsArgumentException()
    {
        var act = () => new ShardMigrationStatus(null!, MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1));

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("ShardId");
    }

    [Fact]
    public void Constructor_EmptyShardId_ThrowsArgumentException()
    {
        var act = () => new ShardMigrationStatus(string.Empty, MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1));

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("ShardId");
    }

    [Fact]
    public void Constructor_WhitespaceShardId_ThrowsArgumentException()
    {
        var act = () => new ShardMigrationStatus("   ", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1));

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("ShardId");
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var status = new ShardMigrationStatus("shard-0", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(5));

        status.ShardId.ShouldBe("shard-0");
        status.Outcome.ShouldBe(MigrationOutcome.Succeeded);
        status.Duration.ShouldBe(TimeSpan.FromSeconds(5));
        status.Error.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ValidParametersWithError_CreatesInstance()
    {
        var error = EncinaErrors.Create("TEST_ERROR", "Something failed");
        var status = new ShardMigrationStatus("shard-1", MigrationOutcome.Failed, TimeSpan.FromSeconds(2), error);

        status.ShardId.ShouldBe("shard-1");
        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        status.Error.ShouldNotBeNull();
    }
}
