using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.IdGeneration.Generators;

/// <summary>
/// Unit tests for <see cref="ShardPrefixedIdGenerator"/>.
/// </summary>
public sealed class ShardPrefixedIdGeneratorTests
{
    private static ShardPrefixedOptions DefaultOptions(ShardPrefixedFormat format = ShardPrefixedFormat.Ulid) => new()
    {
        Format = format
    };

    // ────────────────────────────────────────────────────────────
    //  StrategyName
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StrategyName_ReturnsShardPrefixed()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());
        generator.StrategyName.ShouldBe("ShardPrefixed");
    }

    // ────────────────────────────────────────────────────────────
    //  Generate (parameterless — not supported)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Parameterless_ReturnsLeft()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var result = generator.Generate();

        result.IsLeft.ShouldBeTrue();
        result.Match(_ => { }, error => error.Message.ShouldContain("Invalid shard ID"));
    }

    // ────────────────────────────────────────────────────────────
    //  Generate (with shard)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_WithValidShardId_ReturnsRight()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var result = generator.Generate("shard-01");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                id.ShardId.ShouldBe("shard-01");
                id.Sequence.ShouldNotBeNullOrWhiteSpace();
                id.IsEmpty.ShouldBeFalse();
            },
            _ => { });
    }

    [Fact]
    public void Generate_WithUlidFormat_ProducesUlidSequence()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions(ShardPrefixedFormat.Ulid));

        var result = generator.Generate("shard-01");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                id.Sequence.Length.ShouldBe(UlidId.StringLength);
            },
            _ => { });
    }

    [Fact]
    public void Generate_WithUuidV7Format_ProducesGuidSequence()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions(ShardPrefixedFormat.UuidV7));

        var result = generator.Generate("shard-01");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                Guid.TryParse(id.Sequence, out _).ShouldBeTrue();
            },
            _ => { });
    }

    [Fact]
    public void Generate_WithTimestampRandomFormat_ProducesTimestampDashHex()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions(ShardPrefixedFormat.TimestampRandom));

        var result = generator.Generate("shard-01");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                id.Sequence.ShouldContain("-");
                var parts = id.Sequence.Split('-');
                parts.Length.ShouldBe(2);
                long.TryParse(parts[0], out _).ShouldBeTrue();
            },
            _ => { });
    }

    [Fact]
    public void Generate_BatchOf1000_ProducesUniqueIds()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());
        var ids = new System.Collections.Generic.HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            var result = generator.Generate("shard-01");
            result.IsRight.ShouldBeTrue();
            result.Match(id => ids.Add(id.ToString()).ShouldBeTrue(), _ => { });
        }

        ids.Count.ShouldBe(1000);
    }

    [Fact]
    public void Generate_ToString_ContainsDelimiter()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var result = generator.Generate("shard-01");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id => id.ToString().ShouldContain(":"),
            _ => { });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Generate_WithNullOrEmptyShardId_ReturnsLeft(string? shardId)
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var result = generator.Generate(shardId!);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Generate_WithShardIdContainingDelimiter_ReturnsLeft()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var result = generator.Generate("shard:01");

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  ExtractShardId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractShardId_Roundtrip_ReturnsOriginalShardId()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var generateResult = generator.Generate("my-shard");
        generateResult.IsRight.ShouldBeTrue();

        generateResult.Match(
            id =>
            {
                var extractResult = generator.ExtractShardId(id);
                extractResult.IsRight.ShouldBeTrue();
                extractResult.Match(
                    shardId => shardId.ShouldBe("my-shard"),
                    _ => { });
            },
            _ => { });
    }

    [Fact]
    public void ExtractShardId_EmptyId_ReturnsLeft()
    {
        var generator = new ShardPrefixedIdGenerator(DefaultOptions());

        var result = generator.ExtractShardId(ShardPrefixedId.Empty);

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  TimeProvider injection
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_UsesInjectedTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);
        var generator = new ShardPrefixedIdGenerator(DefaultOptions(ShardPrefixedFormat.TimestampRandom), timeProvider);

        var result = generator.Generate("shard-01");

        result.IsRight.ShouldBeTrue();
        result.Match(
            id =>
            {
                var timestamp = id.Sequence.Split('-')[0];
                long.Parse(timestamp, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(fixedTime.ToUnixTimeMilliseconds());
            },
            _ => { });
    }
}
