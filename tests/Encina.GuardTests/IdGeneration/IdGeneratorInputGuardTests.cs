using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;
using Microsoft.Extensions.Time.Testing;

namespace Encina.GuardTests.IdGeneration;

/// <summary>
/// Guard tests for the argument handling of the four id generators: invalid options throw,
/// invalid shard ids come back as a <c>Left</c> error, and the optional time provider is honoured.
/// </summary>
public sealed class IdGeneratorInputGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    // ---- SnowflakeIdGenerator ----

    [Fact]
    public void SnowflakeIdGenerator_Constructor_BitsNotSummingTo63_ThrowsArgumentException()
    {
        var options = new SnowflakeOptions { TimestampBits = 40 };

        var act = () => new SnowflakeIdGenerator(options);

        Should.Throw<ArgumentException>(act).Message.ShouldContain("63");
    }

    [Fact]
    public void SnowflakeIdGenerator_Constructor_NonPositiveSequenceBits_ThrowsArgumentException()
    {
        var options = new SnowflakeOptions { TimestampBits = 53, ShardBits = 10, SequenceBits = 0 };

        var act = () => new SnowflakeIdGenerator(options);

        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1024)]
    public void SnowflakeIdGenerator_Constructor_MachineIdOutOfRange_ThrowsArgumentException(long machineId)
    {
        var options = new SnowflakeOptions { MachineId = machineId };

        var act = () => new SnowflakeIdGenerator(options);

        Should.Throw<ArgumentException>(act).Message.ShouldContain("MachineId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-numeric")]
    [InlineData("-1")]
    [InlineData("1024")]
    public void SnowflakeIdGenerator_Generate_InvalidShardId_ReturnsLeft(string? shardId)
    {
        var generator = new SnowflakeIdGenerator(new SnowflakeOptions(), new FakeTimeProvider(Now));

        var result = generator.Generate(shardId!);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void SnowflakeIdGenerator_Generate_ValidShardId_ReturnsRightAndRoundTripsShard()
    {
        var generator = new SnowflakeIdGenerator(new SnowflakeOptions(), new FakeTimeProvider(Now));

        var id = generator.Generate("1023").IfLeft(default(SnowflakeId));

        generator.ExtractShardId(id).IfLeft(string.Empty).ShouldBe("1023");
    }

    // ---- ShardPrefixedIdGenerator ----

    [Fact]
    public void ShardPrefixedIdGenerator_Generate_Parameterless_ReturnsLeft()
    {
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions(), new FakeTimeProvider(Now));

        generator.Generate().IsLeft.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("shard:01")]
    public void ShardPrefixedIdGenerator_Generate_InvalidShardId_ReturnsLeft(string? shardId)
    {
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions(), new FakeTimeProvider(Now));

        generator.Generate(shardId!).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ShardPrefixedIdGenerator_ExtractShardId_EmptyId_ReturnsLeft()
    {
        var generator = new ShardPrefixedIdGenerator(new ShardPrefixedOptions(), new FakeTimeProvider(Now));

        generator.ExtractShardId(ShardPrefixedId.Empty).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ShardPrefixedIdGenerator_Generate_UnsupportedFormat_ThrowsInvalidOperationException()
    {
        var options = new ShardPrefixedOptions { Format = (ShardPrefixedFormat)999 };
        var generator = new ShardPrefixedIdGenerator(options, new FakeTimeProvider(Now));

        Action act = () => _ = generator.Generate("shard-01");

        Should.Throw<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData(ShardPrefixedFormat.Ulid)]
    [InlineData(ShardPrefixedFormat.UuidV7)]
    [InlineData(ShardPrefixedFormat.TimestampRandom)]
    public void ShardPrefixedIdGenerator_Generate_EveryFormat_UsesInjectedShardId(ShardPrefixedFormat format)
    {
        var options = new ShardPrefixedOptions { Format = format };
        var generator = new ShardPrefixedIdGenerator(options, new FakeTimeProvider(Now));

        var id = generator.Generate("shard-01").IfLeft(default(ShardPrefixedId));

        id.ShardId.ShouldBe("shard-01");
        id.Sequence.ShouldNotBeNullOrWhiteSpace();
    }

    // ---- UlidIdGenerator / UuidV7IdGenerator ----

    [Fact]
    public void UlidIdGenerator_Generate_WithFakeTimeProvider_EncodesProvidedTime()
    {
        var generator = new UlidIdGenerator(new FakeTimeProvider(Now));

        var id = generator.Generate().IfLeft(default(UlidId));

        id.GetTimestamp().ShouldBe(Now);
    }

    [Fact]
    public void UuidV7IdGenerator_Generate_WithFakeTimeProvider_EncodesProvidedTime()
    {
        var generator = new UuidV7IdGenerator(new FakeTimeProvider(Now));

        var id = generator.Generate().IfLeft(default(UuidV7Id));

        id.GetTimestamp().ShouldBe(Now);
    }

    [Fact]
    public void UlidIdGenerator_Generate_WithoutTimeProvider_ReturnsRight()
    {
        new UlidIdGenerator().Generate().IsRight.ShouldBeTrue();
    }

    [Fact]
    public void UuidV7IdGenerator_Generate_WithoutTimeProvider_ReturnsRight()
    {
        new UuidV7IdGenerator().Generate().IsRight.ShouldBeTrue();
    }
}
