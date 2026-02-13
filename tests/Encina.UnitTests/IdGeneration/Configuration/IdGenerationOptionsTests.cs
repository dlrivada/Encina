using Encina.IdGeneration.Configuration;

namespace Encina.UnitTests.IdGeneration.Configuration;

/// <summary>
/// Unit tests for <see cref="IdGenerationOptions"/>.
/// </summary>
public sealed class IdGenerationOptionsTests
{
    [Fact]
    public void Defaults_AllStrategiesDisabled()
    {
        var options = new IdGenerationOptions();

        options.SnowflakeEnabled.ShouldBeFalse();
        options.UlidEnabled.ShouldBeFalse();
        options.UuidV7Enabled.ShouldBeFalse();
        options.ShardPrefixedEnabled.ShouldBeFalse();
    }

    [Fact]
    public void UseSnowflake_EnablesSnowflake()
    {
        var options = new IdGenerationOptions();

        options.UseSnowflake();

        options.SnowflakeEnabled.ShouldBeTrue();
    }

    [Fact]
    public void UseSnowflake_WithConfigure_AppliesConfiguration()
    {
        var options = new IdGenerationOptions();

        options.UseSnowflake(o => o.MachineId = 42);

        options.SnowflakeEnabled.ShouldBeTrue();
        options.SnowflakeOptions.MachineId.ShouldBe(42L);
    }

    [Fact]
    public void UseUlid_EnablesUlid()
    {
        var options = new IdGenerationOptions();

        options.UseUlid();

        options.UlidEnabled.ShouldBeTrue();
    }

    [Fact]
    public void UseUuidV7_EnablesUuidV7()
    {
        var options = new IdGenerationOptions();

        options.UseUuidV7();

        options.UuidV7Enabled.ShouldBeTrue();
    }

    [Fact]
    public void UseShardPrefixed_EnablesShardPrefixed()
    {
        var options = new IdGenerationOptions();

        options.UseShardPrefixed();

        options.ShardPrefixedEnabled.ShouldBeTrue();
    }

    [Fact]
    public void UseShardPrefixed_WithConfigure_AppliesConfiguration()
    {
        var options = new IdGenerationOptions();

        options.UseShardPrefixed(o => o.Format = ShardPrefixedFormat.UuidV7);

        options.ShardPrefixedEnabled.ShouldBeTrue();
        options.ShardPrefixedOptions.Format.ShouldBe(ShardPrefixedFormat.UuidV7);
    }

    [Fact]
    public void FluentChaining_ReturnsThis()
    {
        var options = new IdGenerationOptions();

        var result = options
            .UseSnowflake()
            .UseUlid()
            .UseUuidV7()
            .UseShardPrefixed();

        result.ShouldBeSameAs(options);
        options.SnowflakeEnabled.ShouldBeTrue();
        options.UlidEnabled.ShouldBeTrue();
        options.UuidV7Enabled.ShouldBeTrue();
        options.ShardPrefixedEnabled.ShouldBeTrue();
    }
}
