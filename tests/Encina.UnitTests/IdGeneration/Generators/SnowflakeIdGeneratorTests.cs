using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.IdGeneration.Generators;

/// <summary>
/// Unit tests for <see cref="SnowflakeIdGenerator"/>.
/// </summary>
public sealed class SnowflakeIdGeneratorTests
{
    private static SnowflakeOptions DefaultOptions(long machineId = 1) => new()
    {
        MachineId = machineId
    };

    // ────────────────────────────────────────────────────────────
    //  StrategyName
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StrategyName_ReturnsSnowflake()
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());
        generator.StrategyName.ShouldBe("Snowflake");
    }

    // ────────────────────────────────────────────────────────────
    //  Generate (parameterless)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_ReturnsRight_WithNonEmptyId()
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.IsEmpty.ShouldBeFalse(), _ => { });
    }

    [Fact]
    public void Generate_BatchOf1000_ProducesUniqueIds()
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());
        var ids = new System.Collections.Generic.HashSet<long>();

        for (var i = 0; i < 1000; i++)
        {
            var result = generator.Generate();
            result.IsRight.ShouldBeTrue();
            result.Match(id => ids.Add(id.Value).ShouldBeTrue(), _ => { });
        }

        ids.Count.ShouldBe(1000);
    }

    [Fact]
    public void Generate_ConsecutiveIds_AreMonotonicallyIncreasing()
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());
        long previousValue = 0;

        for (var i = 0; i < 100; i++)
        {
            var result = generator.Generate();
            result.IsRight.ShouldBeTrue();
            result.Match(
                id =>
                {
                    id.Value.ShouldBeGreaterThan(previousValue);
                    previousValue = id.Value;
                },
                _ => { });
        }
    }

    [Fact]
    public void Generate_EmbedsMachineIdInBits()
    {
        const long machineId = 42;
        var options = DefaultOptions(machineId);
        var generator = new SnowflakeIdGenerator(options);

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var extractedMachineId = (id.Value >> options.SequenceBits) & ((1L << options.ShardBits) - 1);
                extractedMachineId.ShouldBe(machineId);
            },
            _ => { });
    }

    // ────────────────────────────────────────────────────────────
    //  Generate (with shard)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithValidShardId_ReturnsRight()
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());

        var result = generator.Generate("5");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Generate_WithShardId_EmbedsShardInBits()
    {
        var options = DefaultOptions();
        var generator = new SnowflakeIdGenerator(options);

        var result = generator.Generate("7");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var extractedShard = (id.Value >> options.SequenceBits) & ((1L << options.ShardBits) - 1);
                extractedShard.ShouldBe(7L);
            },
            _ => { });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Generate_WithNullOrEmptyShardId_ReturnsLeft(string? shardId)
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());

        var result = generator.Generate(shardId!);

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("Invalid shard ID"));
    }

    [Fact]
    public void Generate_WithNonNumericShardId_ReturnsLeft()
    {
        var generator = new SnowflakeIdGenerator(DefaultOptions());

        var result = generator.Generate("abc");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Generate_WithOutOfRangeShardId_ReturnsLeft()
    {
        var options = DefaultOptions();
        var generator = new SnowflakeIdGenerator(options);

        var result = generator.Generate("9999");

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  ExtractShardId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractShardId_Roundtrip_ReturnsOriginalShardId()
    {
        var options = DefaultOptions();
        var generator = new SnowflakeIdGenerator(options);

        var generateResult = generator.Generate("42");
        generateResult.IsRight.ShouldBeTrue();

        generateResult.Match(
            id =>
            {
                var extractResult = generator.ExtractShardId(id);
                extractResult.IsRight.ShouldBeTrue();
                extractResult.Match(
                    shardId => shardId.ShouldBe("42"),
                    _ => { });
            },
            _ => { });
    }

    // ────────────────────────────────────────────────────────────
    //  Clock drift
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithClockDriftBeyondTolerance_ReturnsLeft()
    {
        // Use a custom TimeProvider that can simulate backward clock movement
        var backwardClockProvider = new BackwardClockTimeProvider();
        var options = DefaultOptions();
        options.ClockDriftToleranceMs = 5;
        var generator = new SnowflakeIdGenerator(options, backwardClockProvider);

        // Generate first ID to set _lastTimestamp (at normal time)
        generator.Generate().IsRight.ShouldBeTrue();

        // Move clock backward beyond tolerance
        backwardClockProvider.MoveBackward(TimeSpan.FromMilliseconds(100));

        var result = generator.Generate();

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("Clock drift detected"));
    }

    /// <summary>
    /// Custom TimeProvider that supports moving time backward for clock drift testing.
    /// </summary>
    private sealed class BackwardClockTimeProvider : TimeProvider
    {
        private TimeSpan _offset = TimeSpan.Zero;

        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + _offset;

        public void MoveBackward(TimeSpan duration) => _offset -= duration;
    }

    // ────────────────────────────────────────────────────────────
    //  Custom bit allocation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithCustomBitAllocation_ProducesValidIds()
    {
        var options = new SnowflakeOptions
        {
            MachineId = 0,
            TimestampBits = 42,
            ShardBits = 5,
            SequenceBits = 16
        };
        var generator = new SnowflakeIdGenerator(options);

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Generate_WithZeroShardBits_StillWorks()
    {
        var options = new SnowflakeOptions
        {
            MachineId = 0,
            TimestampBits = 51,
            ShardBits = 0,
            SequenceBits = 12
        };
        var generator = new SnowflakeIdGenerator(options);

        var result = generator.Generate();

        result.IsRight.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  TimeProvider injection
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_UsesInjectedTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var options = DefaultOptions();
        var generator = new SnowflakeIdGenerator(options, timeProvider);

        var result1 = generator.Generate();
        result1.IsRight.ShouldBeTrue();

        // Advance time and generate another
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var result2 = generator.Generate();
        result2.IsRight.ShouldBeTrue();

        // Second ID should be greater (different timestamp)
        var id1 = result1.Match(id => id.Value, _ => 0L);
        var id2 = result2.Match(id => id.Value, _ => 0L);
        id2.ShouldBeGreaterThan(id1);
    }
}
